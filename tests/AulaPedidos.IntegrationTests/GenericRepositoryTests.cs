using AulaPedidos.Application.Abstractions;
using AulaPedidos.Domain.Entities;
using AulaPedidos.Infrastructure;
using AulaPedidos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AulaPedidos.IntegrationTests;

public sealed class GenericRepositoryTests
{
    [Fact]
    public async Task Generic_and_specific_ports_share_one_scoped_repository_and_complete_order_graph()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var orderId = await fixture.SeedOrderAsync();
        using var provider = CreateProvider(fixture);
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var orders = services.GetRequiredService<IRepository<Order>>();
        var products = services.GetRequiredService<IRepository<Product>>();

        Assert.Same(orders, services.GetRequiredService<IOrderRepository>());
        Assert.Same(orders, services.GetRequiredService<OrderRepository>());
        Assert.Same(orders, Assert.Single(services.GetServices<IRepository<Order>>()));
        Assert.Same(products, services.GetRequiredService<IProductRepository>());
        Assert.Same(products, services.GetRequiredService<ProductRepository>());
        Assert.Same(products, Assert.Single(services.GetServices<IRepository<Product>>()));

        var order = await orders.GetByIdAsync(orderId);
        Assert.NotNull(order);
        Assert.Equal(2, Assert.Single(order.Items).Quantity);
        Assert.Equal(246.90m, order.Total);

        await using var otherScope = provider.CreateAsyncScope();
        Assert.NotSame(orders, otherScope.ServiceProvider.GetRequiredService<IRepository<Order>>());
        Assert.NotSame(products, otherScope.ServiceProvider.GetRequiredService<IRepository<Product>>());
    }

    [Fact]
    public async Task Generic_add_defers_commit_and_shares_transaction_with_order_and_outbox()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        using var provider = CreateProvider(fixture);
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var product = Product.Create("GENERIC-001", "Producto genérico", 15.50m);
        var order = Order.Create("cliente-generico", [OrderItem.Create(product, 2)]);
        var eventId = Assert.Single(order.DomainEvents).Id;

        services.GetRequiredService<IRepository<Product>>().Add(product);
        services.GetRequiredService<IRepository<Order>>().Add(order);
        await using (var beforeCommit = fixture.CreateContext())
        {
            Assert.Equal(0, await beforeCommit.Products.CountAsync());
            Assert.Equal(0, await beforeCommit.Orders.CountAsync());
            Assert.Equal(0, await beforeCommit.OutboxMessages.CountAsync());
        }

        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        Assert.Same(services.GetRequiredService<AulaPedidosDbContext>(), unitOfWork);
        await unitOfWork.SaveChangesAsync();

        await using var verification = fixture.CreateContext();
        Assert.Equal(product.Id, (await verification.Products.SingleAsync()).Id);
        Assert.Equal(31m, (await verification.Orders.SingleAsync()).Total);
        Assert.Equal(eventId, (await verification.OutboxMessages.SingleAsync()).Id);
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public async Task Generic_lookup_tracks_domain_changes_for_unit_of_work_and_keeps_concurrency_token()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await fixture.SeedOrderAsync();
        Guid productId;
        await using (var seed = fixture.CreateContext()) productId = (await seed.Products.SingleAsync()).Id;
        using var provider = CreateProvider(fixture);
        await using var firstScope = provider.CreateAsyncScope();
        await using var staleScope = provider.CreateAsyncScope();
        var current = await firstScope.ServiceProvider.GetRequiredService<IRepository<Product>>().GetByIdAsync(productId);
        var stale = await staleScope.ServiceProvider.GetRequiredService<IRepository<Product>>().GetByIdAsync(productId);
        Assert.NotNull(current);
        Assert.NotNull(stale);
        var originalVersion = current.Version;
        Assert.Equal(EntityState.Unchanged, firstScope.ServiceProvider.GetRequiredService<AulaPedidosDbContext>().Entry(current).State);

        current.Update(current.Sku, "Cambio mediante dominio", 200m);
        await firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.NotEqual(originalVersion, current.Version);
        stale.Update(stale.Sku, "Cambio obsoleto", 300m);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());

        await using var verification = fixture.CreateContext();
        Assert.Equal(200m, (await verification.Products.SingleAsync()).Price);
    }

    [Fact]
    public async Task Generic_lookup_excludes_soft_deleted_product_even_in_the_same_tracking_scope()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        using var provider = CreateProvider(fixture);
        await using var scope = provider.CreateAsyncScope();
        var products = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var product = Product.Create("GENERIC-DEL", "Producto borrado", 20m);
        products.Add(product);
        await unitOfWork.SaveChangesAsync();
        Assert.Same(product, await products.GetByIdAsync(product.Id));

        product.SoftDelete();
        await unitOfWork.SaveChangesAsync();

        // Regresión: FindAsync devolvería la entidad local sin aplicar el filtro SQL.
        Assert.Null(await products.GetByIdAsync(product.Id));
        Assert.Empty(await products.GetByIdsAsync([product.Id]));
        var db = scope.ServiceProvider.GetRequiredService<AulaPedidosDbContext>();
        Assert.True((await db.Products.IgnoreQueryFilters().SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task Generic_batch_preserves_order_graph_without_tracking_and_ignores_missing_or_duplicate_ids()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var orderId = await fixture.SeedOrderAsync();
        using var provider = CreateProvider(fixture);
        await using var scope = provider.CreateAsyncScope();
        var orders = scope.ServiceProvider.GetRequiredService<IRepository<Order>>();

        var order = Assert.Single(await orders.GetByIdsAsync([orderId, orderId, Guid.NewGuid()]));

        Assert.Equal(orderId, order.Id);
        Assert.Single(order.Items);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<AulaPedidosDbContext>().ChangeTracker.Entries());
        Assert.Null(await orders.GetByIdAsync(Guid.NewGuid()));
        Assert.Empty(await orders.GetByIdsAsync([]));
    }

    [Fact]
    public async Task Cancelled_reads_propagate_cancellation_including_an_empty_batch()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        using var provider = CreateProvider(fixture);
        await using var scope = provider.CreateAsyncScope();
        var products = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => products.GetByIdAsync(Guid.NewGuid(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => products.GetByIdsAsync([], cancellation.Token));
    }

    private static ServiceProvider CreateProvider(PersistenceFixture fixture)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Notifications:SharedKey"] = "test-notifications-only",
            ["Outbox:Enabled"] = "false"
        }).Build();
        var services = new ServiceCollection().AddLogging().AddInfrastructure(configuration);
        // Se conservan los registros reales de repositorios y UoW, cambiando únicamente
        // la conexión del contexto por la base relacional aislada de la prueba.
        services.RemoveAll<AulaPedidosDbContext>();
        services.AddScoped<AulaPedidosDbContext>(_ => fixture.CreateContext());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
