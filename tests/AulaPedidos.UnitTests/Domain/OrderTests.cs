using AulaPedidos.Domain;
using AulaPedidos.Domain.Entities;
using AulaPedidos.Domain.Events;

namespace AulaPedidos.UnitTests.Domain;

public sealed class OrderTests
{
    [Theory]
    [InlineData(" alice")]
    [InlineData("alice ")]
    [InlineData("alice\t")]
    [InlineData("\u00a0alice")]
    public void Customer_identity_with_boundary_whitespace_is_rejected_instead_of_normalized(string customerId)
    {
        var product = Product.Create("ABC", "Producto", 1);
        Assert.Throws<DomainValidationException>(() => Order.Create(customerId, [OrderItem.Create(product, 1)]));
    }

    [Fact]
    public void Order_captures_price_and_name_snapshot_and_emits_one_event()
    {
        var product = Product.Create("NET-101", "Curso .NET", 100.25m);
        var now = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var order = Order.Create("alice", [OrderItem.Create(product, 3)], now);
        product.Update("NET-101", "Nuevo curso", 500m);
        Assert.Equal(300.75m, order.Total);
        Assert.Equal("Curso .NET", Assert.Single(order.Items).Name);
        Assert.Equal(100.25m, Assert.Single(order.Items).UnitPrice);
        var domainEvent = Assert.IsType<OrderSubmitted>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, domainEvent.OrderId);
        Assert.Equal(order.Total, domainEvent.Total);
        Assert.Equal("alice", domainEvent.CustomerId);
        Assert.Equal(now, domainEvent.OccurredAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void OrderItem_rejects_invalid_quantity(int quantity)
    {
        var product = Product.Create("ABC", "Producto", 1);
        Assert.Throws<DomainValidationException>(() => OrderItem.Create(product, quantity));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void OrderItem_accepts_quantity_boundaries(int quantity)
    {
        var product = Product.Create("ABC", "Producto", 1_000_000);
        Assert.Equal(1_000_000m * quantity, OrderItem.Create(product, quantity).Total);
    }

    [Fact]
    public void Empty_order_and_duplicate_product_lines_are_invalid()
    {
        var product = Product.Create("ABC", "Producto", 1);
        Assert.Throws<DomainValidationException>(() => Order.Create("alice", []));
        Assert.Throws<DomainValidationException>(() => Order.Create("alice",
            [OrderItem.Create(product, 1), OrderItem.Create(product, 2)]));
    }

    [Fact]
    public void Order_cannot_contain_more_than_fifty_lines()
    {
        var lines = Enumerable.Range(1, 51).Select(i => OrderItem.Create(Product.Create($"SKU-{i}", "Producto", 1), 1));
        Assert.Throws<DomainValidationException>(() => Order.Create("alice", lines));
    }

    [Fact]
    public void OrderItem_cannot_use_deleted_product()
    {
        var product = Product.Create("ABC", "Producto", 1);
        product.SoftDelete();
        Assert.Throws<DomainValidationException>(() => OrderItem.Create(product, 1));
    }

    [Fact]
    public void Cancel_changes_state_and_version_but_preserves_total_and_rejects_second_cancel()
    {
        var order = Order.Create("alice", [OrderItem.Create(Product.Create("ABC", "Producto", 9.99m), 2)]);
        var version = order.Version;
        order.Cancel();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotEqual(version, order.Version);
        Assert.Equal(19.98m, order.Total);
        Assert.Throws<DomainConflictException>(() => order.Cancel());
    }
}
