using System.Diagnostics;
using System.Text.Json;
using AulaPedidos.Infrastructure.Notifications;
using AulaPedidos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AulaPedidos.Infrastructure.Outbox;

/// <summary>
/// Consumidor de outbox para UNA instancia. La demo no implementa leases/claim SQL;
/// ejecutar varios dispatchers requiere agregarlo. La entrega es at-least-once.
/// </summary>
public sealed class OutboxDispatcher(
    AulaPedidosDbContext db,
    IOrderNotificationPublisher publisher,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger)
{
    private static readonly ActivitySource ActivitySource = new("AulaPedidos.Outbox");

    public async Task<int> DispatchBatchAsync(CancellationToken cancellationToken = default)
    {
        using var batchActivity = ActivitySource.StartActivity("outbox.dispatch", ActivityKind.Consumer);
        var now = timeProvider.GetUtcNow();
        var configuration = options.Value;
        var messages = await db.OutboxMessages
            .Where(message => message.ProcessedAt == null && message.DeadLetteredAt == null && message.NextAttemptAt <= now)
            .OrderBy(message => message.NextAttemptAt).ThenBy(message => message.Id)
            .Take(configuration.BatchSize).ToListAsync(cancellationToken);
        var delivered = 0;
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var deliveryActivity = ActivitySource.StartActivity("outbox.publish", ActivityKind.Producer);
            deliveryActivity?.SetTag("messaging.message.id", message.Id);
            deliveryActivity?.SetTag("messaging.message.type", message.Type);
            message.Attempts++;
            deliveryActivity?.SetTag("messaging.delivery.attempt", message.Attempts);
            try
            {
                if (message.Type != "order.submitted.v1") throw new InvalidOperationException("UnknownEventType");
                var notification = JsonSerializer.Deserialize<OrderNotification>(message.Payload)
                    ?? throw new JsonException("InvalidNotificationPayload");
                if (notification.EventId != message.Id) throw new JsonException("EventIdMismatch");

                await publisher.PublishAsync(notification, cancellationToken);
                message.ProcessedAt = timeProvider.GetUtcNow();
                message.LastErrorCode = null;
                delivered++;
                deliveryActivity?.SetStatus(ActivityStatusCode.Ok);
                logger.LogInformation("Outbox {EventId} entregado en intento {Attempt}", message.Id, message.Attempts);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Guardar sólo tipo/código, nunca mensaje HTTP, payload, credenciales o datos privados.
                message.LastErrorCode = exception is HttpRequestException httpException && httpException.StatusCode.HasValue
                    ? $"HTTP_{(int)httpException.StatusCode.Value}"
                    : exception.GetType().Name;
                deliveryActivity?.SetStatus(ActivityStatusCode.Error, message.LastErrorCode);
                if (message.Attempts >= configuration.MaxAttempts)
                {
                    message.DeadLetteredAt = timeProvider.GetUtcNow();
                    logger.LogWarning("Outbox {EventId} enviado a dead-letter tras {Attempt} intentos: {ErrorCode}",
                        message.Id, message.Attempts, message.LastErrorCode);
                }
                else
                {
                    var delay = Math.Min(configuration.MaxDelaySeconds, configuration.BaseDelaySeconds * Math.Pow(2, message.Attempts - 1));
                    message.NextAttemptAt = timeProvider.GetUtcNow().AddSeconds(delay);
                    logger.LogWarning("Outbox {EventId} reintentará en {DelaySeconds}s: {ErrorCode}",
                        message.Id, delay, message.LastErrorCode);
                }
            }

            // Si el proceso cae después del POST y antes de este SaveChanges, volverá a
            // enviarse el mismo EventId. El receptor debe deduplicarlo de forma durable.
            await db.SaveChangesAsync(cancellationToken);
        }
        batchActivity?.SetTag("messaging.batch.message_count", messages.Count);
        batchActivity?.SetTag("messaging.batch.delivered_count", delivered);
        return delivered;
    }
}
