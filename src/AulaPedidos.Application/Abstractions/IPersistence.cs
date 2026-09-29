using AulaPedidos.Application.Common;
using AulaPedidos.Application.Products;
using AulaPedidos.Domain.Entities;

namespace AulaPedidos.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludingId, CancellationToken cancellationToken = default);
    Task<Page<Product>> ListAsync(PageRequest page, CancellationToken cancellationToken = default);
    void Add(Product product);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Page<Order>> ListByCustomerAsync(string customerId, PageRequest page, CancellationToken cancellationToken = default);
    void Add(Order order);
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
