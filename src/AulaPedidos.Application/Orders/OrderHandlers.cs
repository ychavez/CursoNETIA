using AulaPedidos.Application.Abstractions;
using AulaPedidos.Application.Common;
using AulaPedidos.Application.Messaging;
using AulaPedidos.Domain;
using AulaPedidos.Domain.Entities;

namespace AulaPedidos.Application.Orders;

public sealed class CreateOrderHandler(IRepository<Product> products, IRepository<Order> orders, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (!OrderValidation.IsCustomerValid(request.CustomerId))
            return Result<OrderDto>.Failure(Error.Validation("El cliente no es válido."));
        if (request.Items is null || request.Items.Count is < 1 or > 50
            || request.Items.Any(item => item is null || item.ProductId == Guid.Empty || item.Quantity is < 1 or > 100))
            return Result<OrderDto>.Failure(Error.Validation("Incluye de 1 a 50 productos y cantidades entre 1 y 100."));
        if (request.Items.Select(item => item.ProductId).Distinct().Count() != request.Items.Count)
            return Result<OrderDto>.Failure(Error.Validation("Cada producto debe aparecer una sola vez por pedido."));

        // Una sola consulta para todas las líneas evita un SELECT por producto (N+1).
        // La fuente del precio es el repositorio, nunca el cliente ni un valor obsoleto de caché.
        var productIds = request.Items.Select(line => line.ProductId).ToArray();
        var catalog = (await products.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);
        var items = new List<OrderItem>();
        foreach (var line in request.Items)
        {
            if (!catalog.TryGetValue(line.ProductId, out var product) || product.IsDeleted)
                return Result<OrderDto>.Failure(Error.NotFound($"el producto {line.ProductId}"));
            items.Add(OrderItem.Create(product, line.Quantity));
        }
        var order = Order.Create(request.CustomerId, items);
        orders.Add(order);
        // Infrastructure escribe pedido y outbox en una misma transacción.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<OrderDto>.Success(OrderDto.From(order));
    }
}

public sealed class CancelOrderHandler(IRepository<Order> orders, IUnitOfWork unitOfWork)
    : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty || request.Version == Guid.Empty || !OrderValidation.IsCustomerValid(request.CustomerId))
            return Result<OrderDto>.Failure(Error.Validation("Se requieren pedido, cliente y versión válidos."));
        var order = await orders.GetByIdAsync(request.Id, cancellationToken);
        if (order is null) return Result<OrderDto>.Failure(Error.NotFound("el pedido"));
        if (!string.Equals(order.CustomerId, request.CustomerId, StringComparison.Ordinal))
            return Result<OrderDto>.Failure(Error.Forbidden());
        if (order.Version != request.Version)
            return Result<OrderDto>.Failure(Error.Conflict("El pedido cambió; recarga su versión antes de cancelar."));
        try { order.Cancel(); }
        catch (DomainConflictException exception) { return Result<OrderDto>.Failure(Error.Conflict(exception.Message)); }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<OrderDto>.Success(OrderDto.From(order));
    }
}

public sealed class GetOrderHandler(IRepository<Order> orders) : IRequestHandler<GetOrderQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty || !OrderValidation.IsCustomerValid(request.CustomerId))
            return Result<OrderDto>.Failure(Error.Validation("Se requieren pedido y cliente válidos."));
        var order = await orders.GetByIdAsync(request.Id, cancellationToken);
        if (order is null) return Result<OrderDto>.Failure(Error.NotFound("el pedido"));
        if (!string.Equals(order.CustomerId, request.CustomerId, StringComparison.Ordinal))
            return Result<OrderDto>.Failure(Error.Forbidden());
        return Result<OrderDto>.Success(OrderDto.From(order));
    }
}

public sealed class ListOrdersHandler(IOrderRepository orders)
    : IRequestHandler<ListOrdersQuery, Result<Page<OrderDto>>>
{
    public async Task<Result<Page<OrderDto>>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        if (!OrderValidation.IsCustomerValid(request.CustomerId) || request.Page is null || !request.Page.IsValid)
            return Result<Page<OrderDto>>.Failure(Error.Validation("Cliente o paginación inválidos: página 1..1000000, tamaño 1..100."));
        var page = await orders.ListByCustomerAsync(request.CustomerId, request.Page, cancellationToken);
        return Result<Page<OrderDto>>.Success(new Page<OrderDto>(page.Items.Select(OrderDto.From).ToList(),
            page.TotalCount, page.PageNumber, page.PageSize));
    }
}

internal static class OrderValidation
{
    internal static bool IsCustomerValid(string customerId) => !string.IsNullOrWhiteSpace(customerId)
        && customerId.Length <= 100 && string.Equals(customerId, customerId.Trim(), StringComparison.Ordinal);
}
