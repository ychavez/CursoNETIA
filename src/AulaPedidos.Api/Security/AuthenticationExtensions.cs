using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace AulaPedidos.Api.Security;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCourseAuthentication(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var demo = configuration.GetValue<bool>("Jwt:DemoMode");
        if (demo && !environment.IsDevelopment())
            throw new InvalidOperationException("JWT de demostración sólo está permitido en Development.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = "sub";
            options.TokenValidationParameters.RoleClaimType = "role";
            options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
            if (demo)
            {
                var key = configuration["Jwt:SigningKey"];
                if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
                    throw new InvalidOperationException("Ejecuta scripts/setup.ps1 para configurar la clave de laboratorio.");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "AulaPedidos.Demo",
                    ValidateAudience = true, ValidAudience = "AulaPedidos.Api",
                    ValidateLifetime = true, RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = "sub", RoleClaimType = "role", ClockSkew = TimeSpan.FromSeconds(30)
                };
            }
            else
            {
                var authority = configuration["Jwt:Authority"];
                var audience = configuration["Jwt:Audience"];
                if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || uri.Scheme != "https" || string.IsNullOrWhiteSpace(audience))
                    throw new InvalidOperationException("Configura Jwt:Authority HTTPS y Jwt:Audience para tu proveedor OIDC.");
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = true;
            }
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var subject = context.Principal?.FindFirst("sub")?.Value;
                    if (string.IsNullOrWhiteSpace(subject) || subject.Length > 100 || subject != subject.Trim())
                        context.Fail("El token requiere un subject válido.");
                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy("catalog.write", policy => policy.RequireRole("Admin").RequireClaim("permission", "catalog.write"))
            .AddPolicy("orders.read", policy => policy.RequireClaim("permission", "orders.read"))
            .AddPolicy("orders.write", policy => policy.RequireClaim("permission", "orders.write"));
        return services;
    }
}
