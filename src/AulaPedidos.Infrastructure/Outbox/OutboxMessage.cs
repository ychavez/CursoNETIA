namespace AulaPedidos.Infrastructure.Outbox;

/// <summary>Estado durable de entrega: pendiente, entregado o dead-letter.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? DeadLetteredAt { get; set; }
    public int Attempts { get; set; }
    public string? LastErrorCode { get; set; }
}
