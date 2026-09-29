namespace AulaPedidos.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public int PollIntervalSeconds { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int BaseDelaySeconds { get; set; } = 2;
    public int MaxDelaySeconds { get; set; } = 60;
}
