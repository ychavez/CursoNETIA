using Microsoft.Extensions.Options;

namespace AulaPedidos.NotificationsMock;

// Copia explícita de un contrato de integración v1. El receptor no referencia Domain/Infrastructure.
public sealed record OrderNotification(Guid EventId, Guid OrderId, decimal Total, DateTimeOffset OccurredAt);
public sealed record NotificationReceipt(Guid EventId, Guid OrderId, decimal Total, DateTimeOffset OccurredAt, DateTimeOffset ReceivedAt);
public enum ReceiptResult { Accepted, Duplicate, Conflict }

public sealed class NotificationReceiverOptions
{
    public string SharedKey { get; set; } = "";
}

public sealed class SimulationOptions
{
    public bool AlwaysFail { get; set; }
    public int FailuresBeforeSuccess { get; set; }
}

public sealed class FailureSimulation(IOptions<SimulationOptions> options)
{
    private long attempts;
    public bool ShouldFail() => options.Value.AlwaysFail || Interlocked.Increment(ref attempts) <= options.Value.FailuresBeforeSuccess;
}
