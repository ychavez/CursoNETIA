using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AulaPedidos.Api.Operations;
public static class CourseTelemetry
{
    public const string ServiceName = "AulaPedidos.Api";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);
    public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>("aulapedidos.orders.created");
    public static IServiceCollection AddCourseTelemetry(this IServiceCollection services, IConfiguration config)
    {
        var export = !string.IsNullOrWhiteSpace(config["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        services.AddOpenTelemetry().ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(traces =>
            {
                traces.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource(ServiceName, "AulaPedidos.Outbox");
                if (export) traces.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddMeter(ServiceName);
                if (export) metrics.AddOtlpExporter();
            });
        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName));
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            if (export) options.AddOtlpExporter();
        }));
        return services;
    }
}
