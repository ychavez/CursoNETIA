using AulaPedidos.Application.Abstractions;
using AulaPedidos.Application.Products;
using Microsoft.Extensions.Caching.Memory;

namespace AulaPedidos.Infrastructure.Caching;

/// <summary>Cache-aside local; el caso de uso consulta la caché, carga el repositorio y guarda el DTO.</summary>
public sealed class MemoryProductCache(IMemoryCache cache) : IProductCache
{
    private static string Key(Guid id) => $"products:v1:{id:N}";

    public Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(cache.Get<ProductDto>(Key(id)));
    }

    public Task SetAsync(ProductDto product, TimeSpan expiration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        cache.Set(Key(product.Id), product, expiration);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        cache.Remove(Key(id));
        return Task.CompletedTask;
    }
}
