using AulaPedidos.Application.Abstractions;
using AulaPedidos.Application.Common;
using AulaPedidos.Application.Messaging;
using AulaPedidos.Domain;
using AulaPedidos.Domain.Entities;

namespace AulaPedidos.Application.Products;

public sealed class CreateProductHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        Product product;
        try { product = Product.Create(request.Sku, request.Name, request.Price); }
        catch (DomainValidationException exception) { return Result<ProductDto>.Failure(Error.Validation(exception.Message)); }

        if (await products.SkuExistsAsync(product.Sku, null, cancellationToken))
            return Result<ProductDto>.Failure(Error.Conflict("El SKU ya existe, incluso si el producto está eliminado."));
        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProductDto>.Success(ProductDto.From(product));
    }
}

public sealed class UpdateProductHandler(IProductRepository products, IUnitOfWork unitOfWork, IProductCache cache)
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty || request.Version == Guid.Empty)
            return Result<ProductDto>.Failure(Error.Validation("Se requieren identificador y versión válidos."));
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null || product.IsDeleted) return Result<ProductDto>.Failure(Error.NotFound("el producto"));
        if (product.Version != request.Version)
            return Result<ProductDto>.Failure(Error.Conflict("El producto cambió; recarga su versión antes de actualizar."));
        try
        {
            // Normalizamos antes de verificar unicidad; la base de datos también aplica índice UNIQUE.
            var sku = Product.NormalizeSku(request.Sku);
            if (await products.SkuExistsAsync(sku, product.Id, cancellationToken))
                return Result<ProductDto>.Failure(Error.Conflict("El SKU ya existe."));
            product.Update(sku, request.Name, request.Price);
        }
        catch (DomainValidationException exception) { return Result<ProductDto>.Failure(Error.Validation(exception.Message)); }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        // El commit ya terminó: la desconexión del cliente no debe omitir la invalidación local.
        await cache.RemoveAsync(product.Id, CancellationToken.None);
        return Result<ProductDto>.Success(ProductDto.From(product));
    }
}

public sealed class DeleteProductHandler(IProductRepository products, IUnitOfWork unitOfWork, IProductCache cache)
    : IRequestHandler<DeleteProductCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty || request.Version == Guid.Empty)
            return Result<Unit>.Failure(Error.Validation("Se requieren identificador y versión válidos."));
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null || product.IsDeleted) return Result<Unit>.Failure(Error.NotFound("el producto"));
        if (product.Version != request.Version)
            return Result<Unit>.Failure(Error.Conflict("El producto cambió; recarga su versión antes de eliminar."));
        product.SoftDelete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(product.Id, CancellationToken.None);
        return Result<Unit>.Success(Unit.Value);
    }
}

public sealed class GetProductHandler(IProductRepository products, IProductCache cache)
    : IRequestHandler<GetProductQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty) return Result<ProductDto>.Failure(Error.Validation("El identificador no es válido."));
        var cached = await cache.GetAsync(request.Id, cancellationToken);
        if (cached is not null) return Result<ProductDto>.Success(cached);
        var product = await products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null || product.IsDeleted) return Result<ProductDto>.Failure(Error.NotFound("el producto"));
        var dto = ProductDto.From(product);
        await cache.SetAsync(dto, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<ProductDto>.Success(dto);
    }
}

public sealed class ListProductsHandler(IProductRepository products)
    : IRequestHandler<ListProductsQuery, Result<Page<ProductDto>>>
{
    public async Task<Result<Page<ProductDto>>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page is null || !request.Page.IsValid)
            return Result<Page<ProductDto>>.Failure(Error.Validation("Página: 1..1000000; tamaño: 1..100."));
        var page = await products.ListAsync(request.Page, cancellationToken);
        return Result<Page<ProductDto>>.Success(new Page<ProductDto>(page.Items.Select(ProductDto.From).ToList(),
            page.TotalCount, page.PageNumber, page.PageSize));
    }
}
