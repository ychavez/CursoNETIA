namespace AulaPedidos.Domain.Entities;

public sealed class OrderItem
{
    private OrderItem() { }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal Total => UnitPrice * Quantity;

    public static OrderItem Create(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (product.IsDeleted) throw new DomainValidationException("No se puede pedir un producto eliminado.");
        if (quantity is < 1 or > 100) throw new DomainValidationException("La cantidad debe estar entre 1 y 100.");
        // Snapshot: un cambio posterior de catálogo no cambia el precio pactado.
        return new OrderItem
        {
            Id = Guid.NewGuid(), ProductId = product.Id, Name = product.Name,
            UnitPrice = product.Price, Quantity = quantity
        };
    }
}
