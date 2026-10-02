using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AulaPedidos.Api.Operations;

public sealed class BearerOpenApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "AulaPedidos";
        document.Info.Version = "v1";
        document.Info.Description = "Laboratorio de arquitectura .NET con Copilot. Genera un token con dotnet run --project tools/AulaPedidos.CourseTools -- token y pégalo en Authorize.";
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
            }
        };
        foreach (var path in document.Paths.Where(path => path.Key.StartsWith("/api/", StringComparison.Ordinal)))
        foreach (var operation in path.Value.Operations!.Values)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
        }
        return Task.CompletedTask;
    }
}
