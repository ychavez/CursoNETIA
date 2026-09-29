namespace AulaPedidos.Domain.Events;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredAt { get; }
}

public sealed record OrderSubmitted(Guid Id, DateTimeOffset OccurredAt, Guid OrderId,
    decimal Total, string CustomerId) : IDomainEvent;
