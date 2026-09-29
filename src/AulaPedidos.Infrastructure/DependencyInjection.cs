using AulaPedidos.Application.Abstractions;
using AulaPedidos.Infrastructure.Caching;
using AulaPedidos.Infrastructure.Notifications;
using AulaPedidos.Infrastructure.Outbox;
using AulaPedidos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Polly;

namespace AulaPedidos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connection = configuration.GetConnectionString("Database");
        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<SqliteAulaPedidosDbContext>(options => options.UseSqlite(connection ?? "Data Source=aula-pedidos.db"));
            services.AddScoped<AulaPedidosDbContext>(serviceProvider => serviceProvider.GetRequiredService<SqliteAulaPedidosDbContext>());
        }
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("SqlServer requiere ConnectionStrings:Database.");
            services.AddDbContext<SqlServerAulaPedidosDbContext>(options => options.UseSqlServer(connection));
            services.AddScoped<AulaPedidosDbContext>(serviceProvider => serviceProvider.GetRequiredService<SqlServerAulaPedidosDbContext>());
        }
        else throw new InvalidOperationException("Database:Provider debe ser Sqlite o SqlServer.");

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AulaPedidosDbContext>());
        services.AddMemoryCache();
        services.AddSingleton<IProductCache, MemoryProductCache>();

        services.AddOptions<NotificationOptions>().Bind(configuration.GetSection("Notifications"))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https", "Notifications:BaseUrl debe ser una URL HTTP(S) absoluta.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SharedKey), "Configura Notifications:SharedKey mediante user-secrets o variable de entorno.")
            .ValidateOnStart();
        services.AddHttpClient<IOrderNotificationPublisher, HttpOrderNotificationPublisher>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NotificationOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("X-Notifications-Key", options.SharedKey);
        })
        .AddStandardResilienceHandler(options =>
        {
            // El receptor demo persiste EventId como clave única. Mantener Retry para
            // POST sólo mientras ese contrato de idempotencia siga siendo válido.
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(200);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(10);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
        });

        services.AddOptions<OutboxOptions>().Bind(configuration.GetSection("Outbox"))
            .Validate(options => options.BatchSize is >= 1 and <= 100, "Outbox:BatchSize debe estar entre 1 y 100.")
            .Validate(options => options.PollIntervalSeconds is >= 1 and <= 60, "Outbox:PollIntervalSeconds debe estar entre 1 y 60.")
            .Validate(options => options.MaxAttempts is >= 1 and <= 10, "Outbox:MaxAttempts debe estar entre 1 y 10.")
            .Validate(options => options.BaseDelaySeconds >= 1 && options.MaxDelaySeconds >= options.BaseDelaySeconds && options.MaxDelaySeconds <= 3600,
                "Configura un backoff outbox entre 1 y 3600 segundos.")
            .ValidateOnStart();
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxWorker>();
        return services;
    }
}
