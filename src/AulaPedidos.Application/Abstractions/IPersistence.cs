using AulaPedidos.Application.Common;
using AulaPedidos.Application.Products;
using AulaPedidos.Domain.Entities;

namespace AulaPedidos.Application.Abstractions;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> SkuExistsAsync(string sku, Guid? excludingId, CancellationToken cancellationToken = default);
    Task<Page<Product>> ListAsync(PageRequest page, CancellationToken cancellationToken = default);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Page<Order>> ListByCustomerAsync(string customerId, PageRequest page, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IProductCache
{
    Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetAsync(ProductDto product, TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
