using AulaPedidos.Application.Abstractions;
using AulaPedidos.Application.Common;
using AulaPedidos.Application.Orders;
using AulaPedidos.Domain.Entities;
using Moq;

namespace AulaPedidos.UnitTests.Application;

public sealed class OrderHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Theory]
    [InlineData(" alice")]
    [InlineData("alice ")]
    [InlineData("alice\t")]
    public async Task Ambiguous_customer_identity_is_rejected_before_any_database_access(string customerId)
    {
        var result = await new CreateOrderHandler(_products.Object, _orders.Object, _unitOfWork.Object)
            .Handle(new(customerId, [new(Guid.NewGuid(), 1)]), default);
        var list = await new ListOrdersHandler(_orders.Object).Handle(new(customerId, new()), default);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(ErrorKind.Validation, list.Error!.Kind);
        _products.VerifyNoOtherCalls();
        _orders.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_calculates_total_from_authoritative_catalog_and_persists_once()
    {
        var first = Product.Create("SKU-1", "Producto uno", 9.99m);
        var second = Product.Create("SKU-2", "Producto dos", 20m);
        _products.Setup(repo => repo.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);
        var result = await new CreateOrderHandler(_products.Object, _orders.Object, _unitOfWork.Object)
            .Handle(new("alice", [new(first.Id, 2), new(second.Id, 3)]), default);
        Assert.Equal(79.98m, result.Value.Total);
        Assert.Equal("alice", result.Value.CustomerId);
        Assert.Equal(OrderStatus.Submitted, result.Value.Status);
        _products.Verify(repo => repo.GetByIdsAsync(It.Is<IReadOnlyCollection<Guid>>(ids =>
            ids.Count == 2 && ids.Contains(first.Id) && ids.Contains(second.Id)), It.IsAny<CancellationToken>()), Times.Once);
        _products.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _orders.Verify(repo => repo.Add(It.Is<Order>(order => order.DomainEvents.Count == 1)), Times.Once);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Duplicate_lines_are_rejected_before_database_access()
    {
        var productId = Guid.NewGuid();
        var result = await new CreateOrderHandler(_products.Object, _orders.Object, _unitOfWork.Object)
            .Handle(new("alice", [new(productId, 1), new(productId, 2)]), default);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        _products.VerifyNoOtherCalls();
        _orders.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Invalid_quantity_is_rejected_before_database_access(int quantity)
    {
        var result = await new CreateOrderHandler(_products.Object, _orders.Object, _unitOfWork.Object)
            .Handle(new("alice", [new(Guid.NewGuid(), quantity)]), default);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        _products.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Missing_product_prevents_partial_order_persistence()
    {
        var product = Product.Create("ABC", "Producto", 1);
        _products.Setup(repo => repo.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        var result = await new CreateOrderHandler(_products.Object, _orders.Object, _unitOfWork.Object)
            .Handle(new("alice", [new(product.Id, 1), new(Guid.NewGuid(), 1)]), default);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        _orders.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_rejects_another_customer_even_when_order_exists()
    {
        var order = AnOrder();
        _orders.Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var result = await new GetOrderHandler(_orders.Object).Handle(new(order.Id, "bob"), default);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public async Task Customer_ownership_comparison_is_case_sensitive()
    {
        var order = AnOrder();
        _orders.Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var result = await new GetOrderHandler(_orders.Object).Handle(new(order.Id, "Alice"), default);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Cancel_rejects_another_customer_without_mutation()
    {
        var order = AnOrder();
        _orders.Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var result = await new CancelOrderHandler(_orders.Object, _unitOfWork.Object)
            .Handle(new(order.Id, "bob", order.Version), default);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Cancel_rejects_stale_version_without_mutation()
    {
        var order = AnOrder();
        _orders.Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var result = await new CancelOrderHandler(_orders.Object, _unitOfWork.Object)
            .Handle(new(order.Id, "alice", Guid.NewGuid()), default);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Cancel_already_cancelled_order_returns_conflict_without_commit()
    {
        var order = AnOrder();
        order.Cancel();
        _orders.Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var result = await new CancelOrderHandler(_orders.Object, _unitOfWork.Object)
            .Handle(new(order.Id, "alice", order.Version), default);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Owner_can_cancel_and_receives_new_version()
    {
        var order = AnOrder();
        var oldVersion = order.Version;
        _orders.Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var result = await new CancelOrderHandler(_orders.Object, _unitOfWork.Object)
            .Handle(new(order.Id, "alice", oldVersion), default);
        Assert.Equal(OrderStatus.Cancelled, result.Value.Status);
        Assert.NotEqual(oldVersion, result.Value.Version);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_queries_repository_using_authenticated_customer()
    {
        var pageRequest = new PageRequest();
        var order = AnOrder();
        _orders.Setup(repo => repo.ListByCustomerAsync("alice", pageRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Page<Order>([order], 1, 1, 20));
        var result = await new ListOrdersHandler(_orders.Object).Handle(new("alice", pageRequest), default);
        Assert.Equal("alice", Assert.Single(result.Value.Items).CustomerId);
        Assert.Equal(1, result.Value.TotalCount);
        _orders.Verify(repo => repo.ListByCustomerAsync("alice", pageRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Order AnOrder() => Order.Create("alice", [OrderItem.Create(Product.Create("ABC", "Producto", 5), 2)]);
}
