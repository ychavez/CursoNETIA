using AulaPedidos.Domain.Events;

namespace AulaPedidos.Domain.Entities;

public enum OrderStatus { Submitted, Cancelled }

public sealed class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];
    private Order() { }

    public Guid Id { get; private set; }
    public string CustomerId { get; private set; } = string.Empty;
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid Version { get; private set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Order Create(string customerId, IEnumerable<OrderItem> items, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(customerId) || customerId.Length > 100
            || !string.Equals(customerId, customerId.Trim(), StringComparison.Ordinal))
            throw new DomainValidationException("El identificador del cliente debe contener de 1 a 100 caracteres, sin espacios en los extremos.");
        if (items is null) throw new DomainValidationException("El pedido debe contener productos.");
        var lines = items.ToList();
        if (lines.Count is < 1 or > 50 || lines.Any(item => item is null))
            throw new DomainValidationException("El pedido debe contener entre 1 y 50 líneas válidas.");
        if (lines.Select(item => item.ProductId).Distinct().Count() != lines.Count)
            throw new DomainValidationException("Cada producto debe aparecer una sola vez por pedido.");
        var timestamp = now ?? DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Status = OrderStatus.Submitted,
            CreatedAt = timestamp, UpdatedAt = timestamp, Version = Guid.NewGuid(),
            Total = lines.Sum(item => item.Total)
        };
        order._items.AddRange(lines);
        order._domainEvents.Add(new OrderSubmitted(Guid.NewGuid(), timestamp, order.Id, order.Total, customerId));
        return order;
    }

    public void Cancel(DateTimeOffset? now = null)
    {
        if (Status != OrderStatus.Submitted)
            throw new DomainConflictException("Sólo un pedido enviado puede cancelarse.");
        Status = OrderStatus.Cancelled;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        Version = Guid.NewGuid();
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
