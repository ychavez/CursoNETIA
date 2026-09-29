using AulaPedidos.Domain.Entities;
using AulaPedidos.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaPedidos.Infrastructure.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.Property(product => product.Sku).HasMaxLength(32).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(120).IsRequired();
        builder.Property(product => product.Price).HasPrecision(18, 2);
        builder.Property(product => product.Version).IsConcurrencyToken();
        builder.HasIndex(product => product.Sku).IsUnique();
        builder.HasQueryFilter(product => !product.IsDeleted);
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.CustomerId).HasMaxLength(100).IsRequired();
        builder.Property(order => order.Total).HasPrecision(18, 2);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.Version).IsConcurrencyToken();
        builder.HasIndex(order => new { order.CustomerId, order.CreatedAt });
        builder.Ignore(order => order.DomainEvents);
        builder.HasMany(order => order.Items).WithOne().HasForeignKey("OrderId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Name).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(item => item.Total);
        builder.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Type).HasMaxLength(100).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.LastErrorCode).HasMaxLength(100);
        builder.HasIndex(message => new { message.ProcessedAt, message.DeadLetteredAt, message.NextAttemptAt });
    }
}
