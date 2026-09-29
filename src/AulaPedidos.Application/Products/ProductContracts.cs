using AulaPedidos.Application.Common;
using AulaPedidos.Application.Messaging;
using AulaPedidos.Domain.Entities;

namespace AulaPedidos.Application.Products;

public sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, Guid Version)
{
    public static ProductDto From(Product product) => new(product.Id, product.Sku, product.Name,
        product.Price, product.CreatedAt, product.UpdatedAt, product.Version);
}

public sealed record CreateProductCommand(string Sku, string Name, decimal Price) : IRequest<Result<ProductDto>>;
public sealed record UpdateProductCommand(Guid Id, string Sku, string Name, decimal Price, Guid Version) : IRequest<Result<ProductDto>>;
public sealed record DeleteProductCommand(Guid Id, Guid Version) : IRequest<Result<Unit>>;
public sealed record GetProductQuery(Guid Id) : IRequest<Result<ProductDto>>;
public sealed record ListProductsQuery(PageRequest Page) : IRequest<Result<Page<ProductDto>>>;
