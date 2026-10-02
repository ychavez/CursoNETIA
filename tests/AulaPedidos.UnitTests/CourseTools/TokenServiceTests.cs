using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AulaPedidos.CourseTools;

namespace AulaPedidos.UnitTests.CourseTools;

public sealed class TokenServiceTests : IDisposable
{
    // Deliberately synthetic test data; never read the repository's local credentials.
    private const string SigningKey = "synthetic-course-tests-only-not-a-real-secret-2026";
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("aulapedidos-token-tests-");

    public TokenServiceTests()
    {
        var local = Path.Combine(directory.FullName, ".local");
        Directory.CreateDirectory(local);
        File.WriteAllText(Path.Combine(local, "secrets.json"), JsonSerializer.Serialize(new
        {
            JwtKey = SigningKey,
            NotificationsKey = "synthetic-notifications-key",
            SqlPassword = "Synthetic-only9!"
        }));
    }

    [Theory]
    [InlineData("Student", "alumno", 1)]
    [InlineData("Student", "ALUMNO", 60)]
    [InlineData("Admin", "Instructor-ñ", 30)]
    public void Issued_token_has_verifiable_signature_exact_identity_and_only_role_permissions(string role, string subject, int minutes)
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var token = TokenService.Create(directory.FullName, role, subject, minutes);
        var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var segments = token.Split('.');
        Assert.Equal(3, segments.Length);
        using var header = JsonDocument.Parse(DecodeBase64Url(segments[0]));
        using var payload = JsonDocument.Parse(DecodeBase64Url(segments[1]));

        Assert.Equal("HS256", header.RootElement.GetProperty("alg").GetString());
        Assert.Equal("JWT", header.RootElement.GetProperty("typ").GetString());
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SigningKey));
        var expectedSignature = hmac.ComputeHash(Encoding.ASCII.GetBytes(segments[0] + "." + segments[1]));
        Assert.True(CryptographicOperations.FixedTimeEquals(expectedSignature, DecodeBase64Url(segments[2])));

        var claims = payload.RootElement;
        Assert.Equal("AulaPedidos.Demo", claims.GetProperty("iss").GetString());
        Assert.Equal("AulaPedidos.Api", claims.GetProperty("aud").GetString());
        Assert.Equal(subject, claims.GetProperty("sub").GetString());
        Assert.Equal(role, claims.GetProperty("role").GetString());
        var issuedAt = claims.GetProperty("iat").GetInt64();
        Assert.InRange(issuedAt, before, after);
        Assert.Equal(issuedAt, claims.GetProperty("nbf").GetInt64());
        Assert.Equal(issuedAt + minutes * 60L, claims.GetProperty("exp").GetInt64());
        Assert.True(Guid.TryParse(claims.GetProperty("jti").GetString(), out var tokenId));
        Assert.NotEqual(Guid.Empty, tokenId);

        string[] expectedPermissions = role == "Admin"
            ? ["catalog.write", "orders.read", "orders.write"]
            : ["orders.read", "orders.write"];
        var permissions = claims.GetProperty("permission").EnumerateArray()
            .Select(permission => permission.GetString()).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expectedPermissions, permissions);
    }

    public static TheoryData<string?> InvalidSubjects => new()
    {
        null, "", " ", "\t", " alumno", "alumno ", "\u00a0alumno", new string('a', 101)
    };

    [Theory]
    [MemberData(nameof(InvalidSubjects))]
    public void Invalid_subject_is_rejected_instead_of_normalized(string? subject)
    {
        Assert.Throws<ArgumentException>(() => TokenService.Create(directory.FullName, "Student", subject!, 30));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("Owner")]
    public void Unrecognized_role_cannot_issue_a_token(string? role)
    {
        Assert.Throws<ArgumentException>(() => TokenService.Create(directory.FullName, role!, "alumno", 30));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(61)]
    public void Lifetime_outside_the_lab_contract_is_rejected(int minutes)
    {
        Assert.Throws<ArgumentException>(() => TokenService.Create(directory.FullName, "Student", "alumno", minutes));
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var standard = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(standard.PadRight((standard.Length + 3) / 4 * 4, '='));
    }

    public void Dispose() => directory.Delete(recursive: true);
}
