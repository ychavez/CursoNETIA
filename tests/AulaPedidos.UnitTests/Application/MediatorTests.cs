using AulaPedidos.Application;
using AulaPedidos.Application.Abstractions;
using AulaPedidos.Application.Messaging;
using AulaPedidos.Application.Products;
using AulaPedidos.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AulaPedidos.UnitTests.Application;

public sealed class MediatorTests
{
    [Fact]
    public async Task Mediator_resolves_registered_handler_and_forwards_cancellation_token()
    {
        var product = Product.Create("ABC", "Producto", 5);
        var products = new Mock<IProductRepository>();
        using var cancellation = new CancellationTokenSource();
        products.Setup(repo => repo.GetByIdAsync(product.Id, cancellation.Token)).ReturnsAsync(product);
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IRepository<Product>>(products.Object);
        services.AddSingleton(Mock.Of<IProductCache>());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new GetProductQuery(product.Id), cancellation.Token);
        Assert.Equal(product.Id, result.Value.Id);
        products.Verify(repo => repo.GetByIdAsync(product.Id, cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Mediator_does_not_start_work_after_cancellation()
    {
        var services = new ServiceCollection().AddApplication();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => mediator.Send(new GetProductQuery(Guid.NewGuid()), cancellation.Token));
    }
}
