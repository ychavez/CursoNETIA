using AulaPedidos.Domain.Entities;
using AulaPedidos.Infrastructure.Notifications;
using AulaPedidos.Infrastructure.Outbox;
using AulaPedidos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AulaPedidos.IntegrationTests;

internal sealed class PersistenceFixture : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public PersistenceTimeProvider Clock { get; } = new();

    public static async Task<PersistenceFixture> CreateAsync()
    {
        var fixture = new PersistenceFixture();
        await fixture.connection.OpenAsync();
        await using var db = fixture.CreateContext();
        // Ejercita las migraciones reales; EnsureCreated ocultaría fallos de migración.
        await db.Database.MigrateAsync();
        return fixture;
    }

    public SqliteAulaPedidosDbContext CreateContext() => new(
        new DbContextOptionsBuilder<SqliteAulaPedidosDbContext>().UseSqlite(connection).Options, Clock);

    public OutboxDispatcher CreateDispatcher(AulaPedidosDbContext db, IOrderNotificationPublisher publisher, OutboxOptions? options = null) =>
        new(db, publisher, Clock, Options.Create(options ?? new OutboxOptions()), NullLogger<OutboxDispatcher>.Instance);

    public async Task<Guid> SeedOrderAsync()
    {
        await using var db = CreateContext();
        var product = Product.Create("DEMO-001", "Producto de prueba", 123.45m, Clock.GetUtcNow());
        db.Products.Add(product);
        var order = Order.Create("cliente-privado", [OrderItem.Create(product, 2)], Clock.GetUtcNow());
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    public ValueTask DisposeAsync() => connection.DisposeAsync();
}

internal sealed class PersistenceTimeProvider : TimeProvider
{
    private DateTimeOffset now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan duration) => now += duration;
}

internal sealed class PersistencePublisher(Func<OrderNotification, CancellationToken, Task>? publish = null) : IOrderNotificationPublisher
{
    public List<OrderNotification> Messages { get; } = [];

    public Task PublishAsync(OrderNotification notification, CancellationToken cancellationToken)
    {
        Messages.Add(notification);
        return publish?.Invoke(notification, cancellationToken) ?? Task.CompletedTask;
    }
}
