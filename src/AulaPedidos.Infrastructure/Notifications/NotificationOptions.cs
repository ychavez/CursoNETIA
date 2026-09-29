namespace AulaPedidos.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5099/";
    public string SharedKey { get; set; } = "";
}
