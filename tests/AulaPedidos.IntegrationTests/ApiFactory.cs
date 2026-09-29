using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AulaPedidos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AulaPedidos.IntegrationTests;
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"aulapedidos-{Guid.NewGuid():N}.db");
    public string SigningKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:Database"] = $"Data Source={databasePath};Pooling=False",
            ["Jwt:DemoMode"] = "true", ["Jwt:SigningKey"] = SigningKey,
            ["Notifications:SharedKey"] = "test-notification-key",
            ["Outbox:Enabled"] = "false"
        }));
    }
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AulaPedidosDbContext>().Database.MigrateAsync();
    }
    public string Token(string subject = "student-a", string role = "Student", string? audience = null,
        bool expired = false, bool includeSubject = true, bool permissions = true)
    {
        var claims = new List<Claim> { new("role", role), new("jti", Guid.NewGuid().ToString()) };
        if (includeSubject) claims.Add(new Claim("sub", subject));
        if (permissions)
        {
            claims.Add(new Claim("permission", "orders.read"));
            claims.Add(new Claim("permission", "orders.write"));
            if (role == "Admin") claims.Add(new Claim("permission", "catalog.write"));
        }
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            "AulaPedidos.Demo", audience ?? "AulaPedidos.Api", claims,
            DateTime.UtcNow.AddHours(-1), expired ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(10),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256)));
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(databasePath)) File.Delete(databasePath);
    }
}
