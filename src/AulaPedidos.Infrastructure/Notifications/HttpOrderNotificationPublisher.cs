using System.Net.Http.Json;

namespace AulaPedidos.Infrastructure.Notifications;

public sealed class HttpOrderNotificationPublisher(HttpClient httpClient) : IOrderNotificationPublisher
{
    public async Task PublishAsync(OrderNotification notification, CancellationToken cancellationToken)
    {
        // El receptor persiste EventId como clave única antes de responder. Sólo por eso
        // reintentamos este POST. Un timeout no demuestra que el receptor no haya procesado.
        using var response = await httpClient.PostAsJsonAsync("notifications", notification, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
