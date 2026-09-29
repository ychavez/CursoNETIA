using System.Security.Cryptography;
using System.Text;
using AulaPedidos.NotificationsMock;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16 * 1024);
builder.Services.AddProblemDetails();
builder.Services.AddOptions<NotificationReceiverOptions>().Bind(builder.Configuration.GetSection("Notifications"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.SharedKey), "Configura Notifications:SharedKey mediante user-secrets o variable de entorno.")
    .ValidateOnStart();
builder.Services.AddOptions<SimulationOptions>().Bind(builder.Configuration.GetSection("Simulation"))
    .Validate(options => options.FailuresBeforeSuccess >= 0, "Simulation:FailuresBeforeSuccess debe ser >= 0.")
    .ValidateOnStart();
builder.Services.AddSingleton<FailureSimulation>();
builder.Services.AddSingleton<ReceiptStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks().AddCheck<ReceiptHealthCheck>("receipts", tags: ["ready"]);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("AulaPedidos.NotificationsMock"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"])) tracing.AddOtlpExporter();
    });

var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });

var notifications = app.MapGroup("/notifications");
notifications.AddEndpointFilter(async (context, next) =>
{
    var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<NotificationReceiverOptions>>().Value;
    var header = context.HttpContext.Request.Headers["X-Notifications-Key"];
    if (header.Count != 1) return Results.Unauthorized();
    // Hashear primero produce arreglos de tamaño fijo incluso si difiere la longitud de la clave.
    var expected = SHA256.HashData(Encoding.UTF8.GetBytes(options.SharedKey));
    var supplied = SHA256.HashData(Encoding.UTF8.GetBytes(header[0] ?? ""));
    if (!CryptographicOperations.FixedTimeEquals(expected, supplied)) return Results.Unauthorized();
    return await next(context);
});

notifications.MapPost("/", async (OrderNotification notification, ReceiptStore store, FailureSimulation failure,
    ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    if (notification.EventId == Guid.Empty || notification.OrderId == Guid.Empty || notification.Total <= 0 ||
        notification.Total > 50_000_000_000m || decimal.Round(notification.Total, 2) != notification.Total || notification.OccurredAt == default)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["notification"] = ["EventId, OrderId, fecha y total monetario deben ser válidos."] });

    if (failure.ShouldFail())
    {
        logger.LogWarning("Fallo simulado de recepción para {EventId}", notification.EventId);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    var result = await store.AcceptAsync(notification, cancellationToken);
    if (result == ReceiptResult.Conflict)
        return Results.Conflict(new { code = "EventIdPayloadMismatch", eventId = notification.EventId });
    var duplicate = result == ReceiptResult.Duplicate;
    logger.LogInformation("Evento {EventId} recibido; duplicado {Duplicate}", notification.EventId, duplicate);
    return Results.Json(new { eventId = notification.EventId, duplicate }, statusCode: duplicate ? StatusCodes.Status200OK : StatusCodes.Status202Accepted);
});

notifications.MapGet("/{eventId:guid}", async (Guid eventId, ReceiptStore store, CancellationToken cancellationToken) =>
{
    var receipt = await store.GetAsync(eventId, cancellationToken);
    return receipt is null ? Results.NotFound() : Results.Ok(receipt);
});

await app.Services.GetRequiredService<ReceiptStore>().InitializeAsync(CancellationToken.None);
await app.RunAsync();
