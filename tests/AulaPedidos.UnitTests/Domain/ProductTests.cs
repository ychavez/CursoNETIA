using AulaPedidos.Domain;
using AulaPedidos.Domain.Entities;

namespace AulaPedidos.UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_normalizes_identity_and_rounds_money_away_from_zero()
    {
        var now = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var product = Product.Create("  net-101  ", "  Taller .NET  ", 10.005m, now);
        Assert.Equal("NET-101", product.Sku);
        Assert.Equal("Taller .NET", product.Name);
        Assert.Equal(10.01m, product.Price);
        Assert.Equal(now, product.CreatedAt);
        Assert.Equal(now, product.UpdatedAt);
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.NotEqual(Guid.Empty, product.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void Create_rejects_prices_outside_business_limits(decimal price)
        => Assert.Throws<DomainValidationException>(() => Product.Create("ABC", "Producto", price));

    [Fact]
    public void Create_rejects_price_that_rounds_to_zero()
        => Assert.Throws<DomainValidationException>(() => Product.Create("ABC", "Producto", 0.004m));

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("SKU con espacio")]
    [InlineData("NÚMERO")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567")]
    public void Create_rejects_invalid_sku(string sku)
        => Assert.Throws<DomainValidationException>(() => Product.Create(sku, "Producto", 1));

    [Theory]
    [InlineData(2)]
    [InlineData(121)]
    public void Create_rejects_name_outside_length_limits(int length)
        => Assert.Throws<DomainValidationException>(() => Product.Create("ABC", new string('a', length), 1));

    [Fact]
    public void Failed_update_preserves_all_previous_state()
    {
        var product = Product.Create("ABC", "Original", 5);
        var version = product.Version;
        Assert.Throws<DomainValidationException>(() => product.Update("CHANGED", "Otro nombre", -1));
        Assert.Equal("ABC", product.Sku);
        Assert.Equal("Original", product.Name);
        Assert.Equal(5, product.Price);
        Assert.Equal(version, product.Version);
    }

    [Fact]
    public void Delete_changes_version_and_disallows_further_updates()
    {
        var product = Product.Create("ABC", "Original", 5);
        var version = product.Version;
        product.SoftDelete();
        Assert.True(product.IsDeleted);
        Assert.NotEqual(version, product.Version);
        Assert.Throws<DomainConflictException>(() => product.Update("ABC", "Otro nombre", 10));
    }
}
