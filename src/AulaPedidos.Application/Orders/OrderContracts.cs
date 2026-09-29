using AulaPedidos.Application.Common;
using AulaPedidos.Application.Messaging;
using AulaPedidos.Domain.Entities;

namespace AulaPedidos.Application.Orders;

public sealed record OrderItemDto(Guid ProductId, string Name, decimal UnitPrice, int Quantity, decimal Total);
public sealed record OrderDto(Guid Id, string CustomerId, IReadOnlyList<OrderItemDto> Items, decimal Total,
    OrderStatus Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, Guid Version)
{
    public static OrderDto From(Order order) => new(order.Id, order.CustomerId,
        order.Items.Select(item => new OrderItemDto(item.ProductId, item.Name, item.UnitPrice, item.Quantity, item.Total)).ToList(),
        order.Total, order.Status, order.CreatedAt, order.UpdatedAt, order.Version);
}

public sealed record OrderLineInput(Guid ProductId, int Quantity);
public sealed record CreateOrderCommand(string CustomerId, IReadOnlyList<OrderLineInput> Items) : IRequest<Result<OrderDto>>;
public sealed record CancelOrderCommand(Guid Id, string CustomerId, Guid Version) : IRequest<Result<OrderDto>>;
public sealed record GetOrderQuery(Guid Id, string CustomerId) : IRequest<Result<OrderDto>>;
public sealed record ListOrdersQuery(string CustomerId, PageRequest Page) : IRequest<Result<Page<OrderDto>>>;
