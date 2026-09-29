using AulaPedidos.Application.Abstractions;
using AulaPedidos.Application.Common;
using AulaPedidos.Application.Products;
using AulaPedidos.Domain.Entities;
using Moq;

namespace AulaPedidos.UnitTests.Application;

public sealed class ProductHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductCache> _cache = new();

    [Fact]
    public async Task Cache_hit_returns_dto_without_database_read()
    {
        var dto = ProductDto.From(Product.Create("ABC", "Producto", 2));
        _cache.Setup(cache => cache.GetAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var result = await new GetProductHandler(_products.Object, _cache.Object).Handle(new(dto.Id), default);
        Assert.Same(dto, result.Value);
        _products.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Cache_miss_loads_database_and_caches_for_two_minutes()
    {
        var product = Product.Create("ABC", "Producto", 2);
        _products.Setup(repo => repo.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var result = await new GetProductHandler(_products.Object, _cache.Object).Handle(new(product.Id), default);
        Assert.True(result.IsSuccess);
        _cache.Verify(cache => cache.SetAsync(It.Is<ProductDto>(dto => dto.Id == product.Id),
            TimeSpan.FromMinutes(2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Missing_product_is_not_cached()
    {
        var result = await new GetProductHandler(_products.Object, _cache.Object).Handle(new(Guid.NewGuid()), default);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        _cache.Verify(cache => cache.SetAsync(It.IsAny<ProductDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_duplicate_normalized_sku_returns_conflict_without_write()
    {
        _products.Setup(repo => repo.SkuExistsAsync("ABC", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await new CreateProductHandler(_products.Object, _unitOfWork.Object).Handle(new("  abc  ", "Producto", 2), default);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        _products.Verify(repo => repo.Add(It.IsAny<Product>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Invalid_product_is_rejected_before_database_access()
    {
        var result = await new CreateProductHandler(_products.Object, _unitOfWork.Object).Handle(new("ABC", "Producto", -1), default);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        _products.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_stale_version_returns_conflict_without_mutating_or_invalidating()
    {
        var product = Product.Create("ABC", "Original", 2);
        _products.Setup(repo => repo.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var result = await new UpdateProductHandler(_products.Object, _unitOfWork.Object, _cache.Object)
            .Handle(new(product.Id, "ABC", "Cambiado", 5, Guid.NewGuid()), default);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("Original", product.Name);
        _unitOfWork.VerifyNoOtherCalls();
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_commits_before_cache_invalidation()
    {
        var product = Product.Create("ABC", "Original", 2);
        var version = product.Version;
        var committed = false;
        _products.Setup(repo => repo.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => committed = true).ReturnsAsync(1);
        _cache.Setup(cache => cache.RemoveAsync(product.Id, It.IsAny<CancellationToken>()))
            .Callback(() => Assert.True(committed)).Returns(Task.CompletedTask);
        var result = await new UpdateProductHandler(_products.Object, _unitOfWork.Object, _cache.Object)
            .Handle(new(product.Id, "ABC", "Cambiado", 5, version), default);
        Assert.Equal("Cambiado", result.Value.Name);
        Assert.NotEqual(version, result.Value.Version);
        _cache.Verify(cache => cache.RemoveAsync(product.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Failed_commit_does_not_invalidate_cache()
    {
        var product = Product.Create("ABC", "Original", 2);
        _products.Setup(repo => repo.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new UpdateProductHandler(_products.Object, _unitOfWork.Object, _cache.Object)
            .Handle(new(product.Id, "ABC", "Cambiado", 5, product.Version), default));
        _cache.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Client_cancellation_after_commit_does_not_skip_cache_invalidation(bool delete)
    {
        var product = Product.Create("ABC", "Original", 2);
        using var cancellation = new CancellationTokenSource();
        _products.Setup(repo => repo.GetByIdAsync(product.Id, cancellation.Token)).ReturnsAsync(product);
        _unitOfWork.Setup(uow => uow.SaveChangesAsync(cancellation.Token))
            .Callback(cancellation.Cancel).ReturnsAsync(1);
        _cache.Setup(cache => cache.RemoveAsync(product.Id, CancellationToken.None)).Returns(Task.CompletedTask);

        if (delete)
        {
            var result = await new DeleteProductHandler(_products.Object, _unitOfWork.Object, _cache.Object)
                .Handle(new(product.Id, product.Version), cancellation.Token);
            Assert.True(result.IsSuccess);
        }
        else
        {
            var result = await new UpdateProductHandler(_products.Object, _unitOfWork.Object, _cache.Object)
                .Handle(new(product.Id, "ABC", "Cambiado", 5, product.Version), cancellation.Token);
            Assert.True(result.IsSuccess);
        }
        Assert.True(cancellation.IsCancellationRequested);
        _cache.Verify(cache => cache.RemoveAsync(product.Id, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Delete_marks_product_deleted_and_invalidates_cache()
    {
        var product = Product.Create("ABC", "Original", 2);
        _products.Setup(repo => repo.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        var result = await new DeleteProductHandler(_products.Object, _unitOfWork.Object, _cache.Object)
            .Handle(new(product.Id, product.Version), default);
        Assert.True(result.IsSuccess);
        Assert.True(product.IsDeleted);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(cache => cache.RemoveAsync(product.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task Invalid_pagination_never_queries_database(int number, int size)
    {
        var result = await new ListProductsHandler(_products.Object).Handle(new(new PageRequest(number, size)), default);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        _products.VerifyNoOtherCalls();
    }
}
