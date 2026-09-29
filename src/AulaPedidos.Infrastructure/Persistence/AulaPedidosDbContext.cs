using System.Text.Json;
using AulaPedidos.Application.Abstractions;
using AulaPedidos.Domain.Entities;
using AulaPedidos.Domain.Events;
using AulaPedidos.Infrastructure.Notifications;
using AulaPedidos.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AulaPedidos.Infrastructure.Persistence;

public class AulaPedidosDbContext(DbContextOptions options, TimeProvider timeProvider)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AulaPedidosDbContext).Assembly);

        if (Database.IsSqlServer())
        {
            // Los identificadores opacos del JWT son sensibles a mayúsculas/acentos.
            // La collation predeterminada de SQL Server suele no serlo.
            modelBuilder.Entity<Order>().Property(order => order.CustomerId).UseCollation("Latin1_General_100_BIN2");
        }

        if (Database.IsSqlite())
        {
            // SQLite no ordena/compara DateTimeOffset de forma nativa. UTC ticks mantiene
            // precisión y permite filtrar NextAttemptAt en SQL, sin evaluar en memoria.
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(
                        value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero)));
                else if (property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, long?>(
                        value => value.HasValue ? value.Value.UtcTicks : null,
                        value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null));
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.Entity is Product or Order))
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            if (entry.State == EntityState.Added) entry.Property("CreatedAt").CurrentValue = now;
            entry.Property("UpdatedAt").CurrentValue = now;
            entry.Property("Version").CurrentValue = Guid.NewGuid();
        }

        var aggregates = ChangeTracker.Entries<Order>()
            .Select(entry => entry.Entity).Where(order => order.DomainEvents.Count > 0).ToArray();
        foreach (var domainEvent in aggregates.SelectMany(order => order.DomainEvents))
        {
            if (domainEvent is not OrderSubmitted submitted)
                throw new InvalidOperationException($"No existe mapeo de outbox para {domainEvent.GetType().Name}.");

            // No duplicar entradas tracked si SaveChanges falla y el llamador vuelve a intentarlo.
            if (OutboxMessages.Local.Any(message => message.Id == submitted.Id)) continue;
            var notification = new OrderNotification(submitted.Id, submitted.OrderId, submitted.Total, submitted.OccurredAt);
            OutboxMessages.Add(new OutboxMessage
            {
                Id = submitted.Id,
                Type = "order.submitted.v1",
                Payload = JsonSerializer.Serialize(notification),
                OccurredAt = submitted.OccurredAt,
                NextAttemptAt = now
            });
        }

        // EF Core garantiza atomicidad de UNA llamada relacional a SaveChanges: el pedido
        // y sus mensajes outbox se confirman juntos o ambos se revierten.
        var affected = await base.SaveChangesAsync(cancellationToken);
        foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();
        return affected;
    }
}

public sealed class SqliteAulaPedidosDbContext(DbContextOptions<SqliteAulaPedidosDbContext> options, TimeProvider timeProvider)
    : AulaPedidosDbContext(options, timeProvider);

public sealed class SqlServerAulaPedidosDbContext(DbContextOptions<SqlServerAulaPedidosDbContext> options, TimeProvider timeProvider)
    : AulaPedidosDbContext(options, timeProvider);
