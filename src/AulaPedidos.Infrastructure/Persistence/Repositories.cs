using AulaPedidos.Application.Abstractions;
using AulaPedidos.Application.Common;
using AulaPedidos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AulaPedidos.Infrastructure.Persistence;

public sealed class ProductRepository(AulaPedidosDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        await db.Products.AsNoTracking().Where(product => ids.Contains(product.Id)).ToListAsync(cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, Guid? excludingId, CancellationToken cancellationToken) =>
        db.Products.IgnoreQueryFilters().AnyAsync(product => product.Sku == sku && product.Id != excludingId, cancellationToken);

    public async Task<Page<Product>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var products = await query.OrderBy(product => product.Sku).Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new Page<Product>(products, count, page.PageNumber, page.PageSize);
    }

    public void Add(Product product) => db.Products.Add(product);
}

public sealed class OrderRepository(AulaPedidosDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Orders.Include(order => order.Items).SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<Page<Order>> ListByCustomerAsync(string customerId, PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().Where(order => order.CustomerId == customerId);
        var count = await query.CountAsync(cancellationToken);
        var orders = await query.Include(order => order.Items).OrderByDescending(order => order.CreatedAt).ThenBy(order => order.Id)
            .Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new Page<Order>(orders, count, page.PageNumber, page.PageSize);
    }

    public void Add(Order order) => db.Orders.Add(order);
}
