using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AulaPedidos.CourseTools;

public static class TokenService
{
    public static string Create(string root, string role, string subject, int minutes)
    {
        if (role is not ("Admin" or "Student")) throw new ArgumentException("Role debe ser Admin o Student.");
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 100 || !string.Equals(subject, subject.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("Subject debe contener entre 1 y 100 caracteres, sin espacios exteriores.");
        if (minutes is < 1 or > 60) throw new ArgumentException("Minutes debe estar entre 1 y 60.");
        var secrets = SecretStore.Read(root);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string[] permissions = role == "Admin"
            ? ["orders.read", "orders.write", "catalog.write"]
            : ["orders.read", "orders.write"];
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = "AulaPedidos.Demo", aud = "AulaPedidos.Api", sub = subject, role,
            permission = permissions, iat = now, nbf = now, exp = now + minutes * 60L,
            jti = Guid.NewGuid().ToString()
        }));
        var unsigned = $"{header}.{payload}";
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secrets.JwtKey), Encoding.UTF8.GetBytes(unsigned));
        return $"{unsigned}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
