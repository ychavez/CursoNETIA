using System.Net;
using System.Text.Json;
using AulaPedidos.Infrastructure;
using AulaPedidos.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;

namespace AulaPedidos.IntegrationTests;

public sealed class ResilienceTests
{
    [Fact]
    public async Task Typed_http_client_retries_two_transient_failures_and_preserves_event_id()
    {
        using var transport = new ScriptedTransport(attempt => attempt < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.Accepted);
        await using var services = CreateServices(transport);
        var publisher = services.GetRequiredService<IOrderNotificationPublisher>();
        var notification = NewNotification();

        await publisher.PublishAsync(notification, CancellationToken.None);

        Assert.Equal(3, transport.Attempts);
        Assert.All(transport.Notifications, observed => Assert.Equal(notification, observed));
        Assert.All(transport.Methods, method => Assert.Equal(HttpMethod.Post, method));
        Assert.All(transport.Addresses, address => Assert.Equal("http://notifications.test/notifications", address));
        Assert.All(transport.SharedKeys, key => Assert.Equal("test-key-only", key));
    }

    [Fact]
    public async Task Circuit_opens_after_five_transport_failures_and_rejects_next_publish_without_sending()
    {
        using var transport = new ScriptedTransport(_ => HttpStatusCode.ServiceUnavailable);
        await using var services = CreateServices(transport);
        var publisher = services.GetRequiredService<IOrderNotificationPublisher>();

        var exhausted = await Assert.ThrowsAsync<HttpRequestException>(() => publisher.PublishAsync(NewNotification(), CancellationToken.None));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, exhausted.StatusCode);
        Assert.Equal(3, transport.Attempts);

        // La primera llamada consumió tres solicitudes. Dos fallos adicionales alcanzan
        // MinimumThroughput=5; el breaker abre antes del siguiente reintento HTTP.
        await Assert.ThrowsAnyAsync<BrokenCircuitException>(() => publisher.PublishAsync(NewNotification(), CancellationToken.None));
        Assert.Equal(5, transport.Attempts);

        await Assert.ThrowsAnyAsync<BrokenCircuitException>(() => publisher.PublishAsync(NewNotification(), CancellationToken.None));
        Assert.Equal(5, transport.Attempts); // la dependencia queda protegida mientras el circuito está abierto.
    }

    [Fact]
    public async Task Permanent_bad_request_is_not_retried()
    {
        using var transport = new ScriptedTransport(_ => HttpStatusCode.BadRequest);
        await using var services = CreateServices(transport);
        var publisher = services.GetRequiredService<IOrderNotificationPublisher>();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => publisher.PublishAsync(NewNotification(), CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(1, transport.Attempts);
    }

    private static ServiceProvider CreateServices(HttpMessageHandler transport)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Notifications:BaseUrl"] = "http://notifications.test/",
            ["Notifications:SharedKey"] = "test-key-only",
            ["Outbox:Enabled"] = "false"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        // Registro REAL: mismo IHttpClientFactory y misma política que usa la aplicación.
        // Sólo se sustituye el transporte de red; no se reducen retries, umbrales ni tiempos.
        services.AddInfrastructure(configuration);
        services.AddHttpClient<IOrderNotificationPublisher, HttpOrderNotificationPublisher>()
            .ConfigurePrimaryHttpMessageHandler(() => transport);
        return services.BuildServiceProvider();
    }

    private static OrderNotification NewNotification() => new(Guid.NewGuid(), Guid.NewGuid(), 123.45m,
        new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));

    private sealed class ScriptedTransport(Func<int, HttpStatusCode> response) : HttpMessageHandler
    {
        public int Attempts { get; private set; }
        public List<OrderNotification?> Notifications { get; } = [];
        public List<HttpMethod> Methods { get; } = [];
        public List<string> Addresses { get; } = [];
        public List<string> SharedKeys { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            // ReadFromJsonAsync cerraría el stream reutilizado por el retry. El transporte
            // observa contenido bufferizado sin apropiarse del stream de la solicitud.
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            Notifications.Add(JsonSerializer.Deserialize<OrderNotification>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            Methods.Add(request.Method);
            Addresses.Add(request.RequestUri!.AbsoluteUri);
            SharedKeys.Add(request.Headers.GetValues("X-Notifications-Key").Single());
            return new HttpResponseMessage(response(Attempts));
        }
    }
}
