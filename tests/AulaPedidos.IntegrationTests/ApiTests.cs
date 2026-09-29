using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AulaPedidos.Application.Products;
using AulaPedidos.Application.Orders;
using AulaPedidos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AulaPedidos.IntegrationTests;
public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    public Task InitializeAsync() => factory.InitializeDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;
    private HttpClient Client(string subject = "student-a", string role = "Student")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(subject, role));
        return client;
    }
    private static async Task<ProductDto> CreateProduct(HttpClient admin, decimal price = 125.50m)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/products", new { sku = $"SKU-{Guid.NewGuid():N}"[..20], name = "Producto de prueba", price });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }
    [Fact]
    public async Task Protected_routes_require_authentication_and_an_admin_permission()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/products")).StatusCode);
        using var student = Client();
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync("/api/v1/products", new { sku = "ABC", name = "Nombre", price = 1 })).StatusCode);
        using var adminWithoutPermissions = factory.CreateClient();
        adminWithoutPermissions.DefaultRequestHeaders.Authorization = new("Bearer", factory.Token(role: "Admin", permissions: false));
        Assert.Equal(HttpStatusCode.Forbidden, (await adminWithoutPermissions.PostAsJsonAsync("/api/v1/products", new { sku = "ABC", name = "Nombre", price = 1 })).StatusCode);
    }
    [Theory]
    [InlineData("expired")]
    [InlineData("audience")]
    [InlineData("subject")]
    [InlineData("signature")]
    public async Task Invalid_tokens_are_rejected(string scenario)
    {
        var token = factory.Token(expired: scenario == "expired",
            audience: scenario == "audience" ? "another-api" : null, includeSubject: scenario != "subject");
        if (scenario == "signature")
        {
            var parts = token.Split('.');
            parts[2] = (parts[2][0] == 'a' ? "b" : "a") + parts[2][1..];
            token = string.Join('.', parts);
        }
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await client.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }
    [Fact]
    public async Task Validation_returns_problem_details_and_rejects_overposting_prices()
    {
        using var admin = Client(role: "Admin");
        var invalid = await admin.PostAsJsonAsync("/api/v1/products", new { sku = "", name = "x", price = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/products?pageSize=101")).StatusCode);
        var product = await CreateProduct(admin);
        var orderResponse = await admin.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = "attacker-chosen-user",
            items = new[] { new { productId = product.Id, quantity = 2, unitPrice = 0.01 } }
        });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = (await orderResponse.Content.ReadFromJsonAsync<OrderDto>(Json))!;
        Assert.Equal("student-a", order.CustomerId);
        Assert.Equal(251m, order.Total);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/v1/orders",
            new StringContent("{\"items\":null}", Encoding.UTF8, "application/json"))).StatusCode);
    }
    [Fact]
    public async Task Full_flow_enforces_ownership_concurrency_soft_delete_and_outbox_atomicity()
    {
        using var admin = Client("instructor", "Admin");
        var product = await CreateProduct(admin);
        using var owner = Client("owner");
        using var other = Client("other");
        var response = await owner.PostAsJsonAsync("/api/v1/orders", new { items = new[] { new { productId = product.Id, quantity = 2 } } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderDto>(Json))!;
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/v1/orders/{order.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", new { order.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", new { version = Guid.NewGuid() })).StatusCode);
        // Warm the cache, then verify invalidation after update and delete.
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/products/{product.Id}")).StatusCode);
        var update = new { sku = product.Sku, name = product.Name, price = 200m, version = product.Version };
        var updatedResponse = await admin.PutAsJsonAsync($"/api/v1/products/{product.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = (await updatedResponse.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.NotEqual(product.Version, updated.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/products/{product.Id}", update)).StatusCode);
        var cached = (await owner.GetFromJsonAsync<ProductDto>($"/api/v1/products/{product.Id}"))!;
        Assert.Equal(200m, cached.Price);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/products/{product.Id}?version={updated.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/v1/products/{product.Id}")).StatusCode);
        var historical = (await owner.GetFromJsonAsync<OrderDto>($"/api/v1/orders/{order.Id}", Json))!;
        Assert.Equal(251m, historical.Total);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", new { order.Version })).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AulaPedidosDbContext>();
        Assert.True((await db.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == product.Id)).IsDeleted);
        var messages = await db.OutboxMessages.ToListAsync();
        Assert.Single(messages, message => message.Payload.Contains(order.Id.ToString()));
    }
    [Fact]
    public async Task Duplicate_sku_is_a_conflict_and_case_normalized()
    {
        using var admin = Client(role: "Admin");
        var product = await CreateProduct(admin);
        var duplicate = await admin.PostAsJsonAsync("/api/v1/products", new { sku = product.Sku.ToLowerInvariant(), name = "Duplicado", price = 10m });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }
    [Fact]
    public async Task Health_openapi_and_explicit_version_are_available()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/swagger/index.html")).StatusCode);
        var openApi = await anonymous.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/v1/products", openApi);
        Assert.Contains("/api/v1/orders", openApi);
        Assert.Contains("securitySchemes", openApi);
        using var student = Client();
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/api/v2/products")).StatusCode);
    }
}
