using System.Net;
using AulaPedidos.Application.Common;
using AulaPedidos.Domain.Entities;
using AulaPedidos.Infrastructure.Outbox;
using AulaPedidos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AulaPedidos.IntegrationTests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task Save_order_persists_lines_and_outbox_and_clears_domain_events()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await using var db = fixture.CreateContext();
        var product = Product.Create("SKU-001", "Producto de prueba", 10.15m);
        db.Products.Add(product);
        var order = Order.Create("cliente-1", [OrderItem.Create(product, 3)], fixture.Clock.GetUtcNow());
        var eventId = Assert.Single(order.DomainEvents).Id;
        db.Orders.Add(order);

        await db.SaveChangesAsync();

        Assert.Empty(order.DomainEvents);
        await using var verification = fixture.CreateContext();
        var stored = await verification.Orders.Include(entity => entity.Items).SingleAsync();
        Assert.Equal(30.45m, stored.Total);
        Assert.Equal(3, Assert.Single(stored.Items).Quantity);
        var message = await verification.OutboxMessages.SingleAsync();
        Assert.Equal(eventId, message.Id);
        Assert.Equal("order.submitted.v1", message.Type);
        Assert.DoesNotContain("cliente-1", message.Payload);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(0, message.Attempts);
    }

    [Fact]
    public async Task Failed_save_rolls_back_order_and_outbox_then_retry_does_not_duplicate_events()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await using var db = fixture.CreateContext();
        var existing = Product.Create("DUP-001", "Producto existente", 10m);
        db.Products.Add(existing);
        await db.SaveChangesAsync();
        var duplicate = Product.Create("DUP-001", "Producto duplicado", 20m);
        db.Products.Add(duplicate);
        var order = Order.Create("cliente-1", [OrderItem.Create(existing, 1)], fixture.Clock.GetUtcNow());
        db.Orders.Add(order);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        await using (var verification = fixture.CreateContext())
        {
            Assert.Equal(0, await verification.Orders.CountAsync());
            Assert.Equal(0, await verification.OutboxMessages.CountAsync());
            Assert.Equal(1, await verification.Products.CountAsync());
        }
        Assert.Single(order.DomainEvents);

        // Un reintento controlado de este test corrige el conflicto antes de guardar.
        // El middleware real desecha el scope cuando falla una petición.
        db.Entry(duplicate).State = EntityState.Detached;
        await db.SaveChangesAsync();
        Assert.Empty(order.DomainEvents);
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(1, await db.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Soft_delete_hides_product_preserves_unique_sku_and_updates_audit_and_version()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await using var db = fixture.CreateContext();
        var product = Product.Create("SOFT-001", "Producto auditable", 12.34m, DateTimeOffset.MinValue);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var originalVersion = product.Version;
        var createdAt = product.CreatedAt;
        Assert.Equal(fixture.Clock.GetUtcNow(), createdAt);
        fixture.Clock.Advance(TimeSpan.FromMinutes(10));
        product.SoftDelete();

        await db.SaveChangesAsync();

        await using var verification = fixture.CreateContext();
        var repository = new ProductRepository(verification);
        Assert.Null(await repository.GetByIdAsync(product.Id, CancellationToken.None));
        Assert.Empty((await repository.ListAsync(new PageRequest(), CancellationToken.None)).Items);
        Assert.True(await repository.SkuExistsAsync("SOFT-001", null, CancellationToken.None));
        var hidden = await verification.Products.IgnoreQueryFilters().SingleAsync();
        Assert.True(hidden.IsDeleted);
        Assert.Equal(createdAt, hidden.CreatedAt);
        Assert.Equal(fixture.Clock.GetUtcNow(), hidden.UpdatedAt);
        Assert.NotEqual(originalVersion, hidden.Version);
    }

    [Fact]
    public async Task Two_contexts_cannot_silently_overwrite_the_same_product()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await using (var seed = fixture.CreateContext())
        {
            seed.Products.Add(Product.Create("CON-001", "Producto inicial", 10m));
            await seed.SaveChangesAsync();
        }
        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var firstCopy = await first.Products.SingleAsync();
        var staleCopy = await second.Products.SingleAsync();
        firstCopy.Update(firstCopy.Sku, "Cambio ganador", 11m);
        await first.SaveChangesAsync();
        staleCopy.Update(staleCopy.Sku, "Cambio obsoleto", 99m);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var verification = fixture.CreateContext();
        var persisted = await verification.Products.SingleAsync();
        Assert.Equal("Cambio ganador", persisted.Name);
        Assert.Equal(11m, persisted.Price);
    }

    [Fact]
    public async Task Order_query_keeps_historical_prices_and_filters_customer()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await fixture.SeedOrderAsync();
        await using var db = fixture.CreateContext();
        var product = await db.Products.SingleAsync();
        product.Update(product.Sku, "Nombre actualizado", 999m);
        await db.SaveChangesAsync();
        var repository = new OrderRepository(db);

        var ownPage = await repository.ListByCustomerAsync("cliente-privado", new PageRequest(), CancellationToken.None);
        var otherPage = await repository.ListByCustomerAsync("otro-cliente", new PageRequest(), CancellationToken.None);

        var line = Assert.Single(Assert.Single(ownPage.Items).Items);
        Assert.Equal(123.45m, line.UnitPrice);
        Assert.Equal("Producto de prueba", line.Name);
        Assert.Empty(otherPage.Items);
    }

    [Fact]
    public async Task Batch_product_query_returns_only_requested_active_products_without_tracking()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        Guid activeId;
        Guid deletedId;
        await using (var db = fixture.CreateContext())
        {
            var active = Product.Create("BATCH-001", "Producto activo", 10m);
            var deleted = Product.Create("BATCH-002", "Producto eliminado", 20m);
            var unrelated = Product.Create("BATCH-003", "Producto no solicitado", 30m);
            deleted.SoftDelete();
            db.Products.AddRange(active, deleted, unrelated);
            await db.SaveChangesAsync();
            activeId = active.Id;
            deletedId = deleted.Id;
        }
        await using var verification = fixture.CreateContext();
        var repository = new ProductRepository(verification);

        var results = await repository.GetByIdsAsync([activeId, deletedId, Guid.NewGuid()], CancellationToken.None);

        Assert.Equal(activeId, Assert.Single(results).Id);
        Assert.Empty(verification.ChangeTracker.Entries<Product>());
    }

    [Fact]
    public async Task Order_customer_query_keeps_case_and_accents_distinct()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await fixture.SeedOrderAsync();
        await using var db = fixture.CreateContext();
        var repository = new OrderRepository(db);

        Assert.Single((await repository.ListByCustomerAsync("cliente-privado", new PageRequest(), CancellationToken.None)).Items);
        Assert.Empty((await repository.ListByCustomerAsync("CLIENTE-PRIVADO", new PageRequest(), CancellationToken.None)).Items);
        Assert.Empty((await repository.ListByCustomerAsync("cliénte-privado", new PageRequest(), CancellationToken.None)).Items);
    }

    [Fact]
    public async Task Dispatcher_marks_success_and_skips_processed_messages()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var orderId = await fixture.SeedOrderAsync();
        await using var db = fixture.CreateContext();
        var publisher = new PersistencePublisher();
        var dispatcher = fixture.CreateDispatcher(db, publisher);

        Assert.Equal(1, await dispatcher.DispatchBatchAsync());
        Assert.Equal(0, await dispatcher.DispatchBatchAsync());

        var notification = Assert.Single(publisher.Messages);
        Assert.Equal(orderId, notification.OrderId);
        Assert.Equal(246.90m, notification.Total);
        await using var verification = fixture.CreateContext();
        var message = await verification.OutboxMessages.SingleAsync();
        Assert.Equal(notification.EventId, message.Id);
        Assert.Equal(fixture.Clock.GetUtcNow(), message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.LastErrorCode);
    }

    [Fact]
    public async Task Dispatcher_persists_bounded_backoff_then_deadletters_without_logging_payload()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await fixture.SeedOrderAsync();
        await using var db = fixture.CreateContext();
        var publisher = new PersistencePublisher((_, _) => throw new HttpRequestException("SECRETO nunca persistir", null, HttpStatusCode.ServiceUnavailable));
        var dispatcher = fixture.CreateDispatcher(db, publisher, new OutboxOptions { MaxAttempts = 3, BaseDelaySeconds = 2, MaxDelaySeconds = 3 });

        Assert.Equal(0, await dispatcher.DispatchBatchAsync());
        var message = await db.OutboxMessages.SingleAsync();
        Assert.Equal(1, message.Attempts);
        Assert.Equal(fixture.Clock.GetUtcNow().AddSeconds(2), message.NextAttemptAt);
        Assert.Equal("HTTP_503", message.LastErrorCode);
        Assert.Equal(0, await dispatcher.DispatchBatchAsync());
        Assert.Single(publisher.Messages);

        fixture.Clock.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(0, await dispatcher.DispatchBatchAsync());
        Assert.Equal(2, message.Attempts);
        Assert.Equal(fixture.Clock.GetUtcNow().AddSeconds(3), message.NextAttemptAt);
        fixture.Clock.Advance(TimeSpan.FromSeconds(3));
        Assert.Equal(0, await dispatcher.DispatchBatchAsync());
        Assert.Equal(fixture.Clock.GetUtcNow(), message.DeadLetteredAt);
        Assert.Equal(3, message.Attempts);
        fixture.Clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal(0, await dispatcher.DispatchBatchAsync());
        Assert.Equal(3, publisher.Messages.Count);

        await using var verification = fixture.CreateContext();
        var persisted = await verification.OutboxMessages.SingleAsync();
        Assert.NotNull(persisted.DeadLetteredAt);
        Assert.Equal("HTTP_503", persisted.LastErrorCode);
        Assert.Null(persisted.ProcessedAt);
    }

    [Fact]
    public async Task Cancellation_is_propagated_and_does_not_consume_persisted_attempt()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await fixture.SeedOrderAsync();
        using var cancellation = new CancellationTokenSource();
        var publisher = new PersistencePublisher((_, token) =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });
        await using (var db = fixture.CreateContext())
        {
            var dispatcher = fixture.CreateDispatcher(db, publisher);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.DispatchBatchAsync(cancellation.Token));
        }

        await using var verification = fixture.CreateContext();
        var persisted = await verification.OutboxMessages.SingleAsync();
        Assert.Equal(0, persisted.Attempts);
        Assert.Null(persisted.ProcessedAt);
        Assert.Null(persisted.DeadLetteredAt);
    }

    [Fact]
    public async Task Crash_window_after_receiver_accepts_replays_same_event_id()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await fixture.SeedOrderAsync();
        using var cancellation = new CancellationTokenSource();
        var receiverInbox = new HashSet<Guid>();
        var publisher = new PersistencePublisher((notification, _) =>
        {
            receiverInbox.Add(notification.EventId);
            // Simula entrega exitosa y caída antes de persistir ProcessedAt en el emisor.
            if (receiverInbox.Count == 1 && !cancellation.IsCancellationRequested) cancellation.Cancel();
            return Task.CompletedTask;
        });
        await using (var firstContext = fixture.CreateContext())
        {
            var dispatcher = fixture.CreateDispatcher(firstContext, publisher);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.DispatchBatchAsync(cancellation.Token));
        }
        await using (var secondContext = fixture.CreateContext())
        {
            Assert.Equal(1, await fixture.CreateDispatcher(secondContext, publisher).DispatchBatchAsync());
        }

        Assert.Equal(2, publisher.Messages.Count);
        Assert.Equal(publisher.Messages[0].EventId, publisher.Messages[1].EventId);
        Assert.Single(receiverInbox); // el receptor falso modela deduplicación; la demo real la persiste.
    }
}
