using AulaPedidos.Domain.Abstractions;

namespace AulaPedidos.Domain.Entities;

public sealed class Product : IAggregateRoot
{
    private Product() { }

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid Version { get; private set; }

    public static Product Create(string sku, string name, decimal price, DateTimeOffset? now = null)
    {
        var product = new Product { Id = Guid.NewGuid(), CreatedAt = now ?? DateTimeOffset.UtcNow };
        product.Update(sku, name, price, product.CreatedAt);
        return product;
    }

    public void Update(string sku, string name, decimal price, DateTimeOffset? now = null)
    {
        if (IsDeleted) throw new DomainConflictException("No se puede modificar un producto eliminado.");
        var normalizedSku = NormalizeSku(sku);
        var normalizedName = name?.Trim() ?? string.Empty;
        if (normalizedName.Length is < 3 or > 120)
            throw new DomainValidationException("El nombre debe contener entre 3 y 120 caracteres.");
        var roundedPrice = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        if (price <= 0 || price > 1_000_000 || roundedPrice < 0.01m)
            throw new DomainValidationException("El precio debe ser al menos 0.01 y como máximo 1000000.");

        // Validamos todo antes de cambiar estado para que una operación fallida sea atómica.
        Sku = normalizedSku;
        Name = normalizedName;
        Price = roundedPrice;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        Version = Guid.NewGuid();
    }

    public void SoftDelete(DateTimeOffset? now = null)
    {
        if (IsDeleted) throw new DomainConflictException("El producto ya está eliminado.");
        IsDeleted = true;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        Version = Guid.NewGuid();
    }

    public static string NormalizeSku(string sku)
    {
        var normalized = sku?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is < 3 or > 32 || normalized.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            throw new DomainValidationException("El SKU debe tener de 3 a 32 letras ASCII, números o guiones.");
        return normalized;
    }
}
