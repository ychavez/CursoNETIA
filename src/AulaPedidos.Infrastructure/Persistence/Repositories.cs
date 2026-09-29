using AulaPedidos.Application.Abstractions;
using AulaPedidos.Application.Common;
using AulaPedidos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AulaPedidos.Infrastructure.Persistence;

public sealed class ProductRepository(AulaPedidosDbContext db) : Repository<Product>(db), IProductRepository
{
    public Task<bool> SkuExistsAsync(string sku, Guid? excludingId, CancellationToken cancellationToken) =>
        Context.Products.IgnoreQueryFilters().AnyAsync(product => product.Sku == sku && product.Id != excludingId, cancellationToken);

    public async Task<Page<Product>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var query = Query.AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var products = await query.OrderBy(product => product.Sku).Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new Page<Product>(products, count, page.PageNumber, page.PageSize);
    }
}

public sealed class OrderRepository(AulaPedidosDbContext db) : Repository<Order>(db), IOrderRepository
{
    protected override IQueryable<Order> Query => base.Query.Include(order => order.Items);

    public async Task<Page<Order>> ListByCustomerAsync(string customerId, PageRequest page, CancellationToken cancellationToken)
    {
        var query = Context.Orders.AsNoTracking().Where(order => order.CustomerId == customerId);
        var count = await query.CountAsync(cancellationToken);
        var orders = await query.Include(order => order.Items).OrderByDescending(order => order.CreatedAt).ThenBy(order => order.Id)
            .Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new Page<Order>(orders, count, page.PageNumber, page.PageSize);
    }
}
