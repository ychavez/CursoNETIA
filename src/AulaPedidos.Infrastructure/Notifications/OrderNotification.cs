namespace AulaPedidos.Infrastructure.Notifications;

/// <summary>Contrato de integración v1. No contiene datos personales del cliente.</summary>
public sealed record OrderNotification(Guid EventId, Guid OrderId, decimal Total, DateTimeOffset OccurredAt);

public interface IOrderNotificationPublisher
{
    Task PublishAsync(OrderNotification notification, CancellationToken cancellationToken);
}
