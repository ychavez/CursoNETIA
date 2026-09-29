using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AulaPedidos.Api.Operations;
using AulaPedidos.Api.Security;
using AulaPedidos.Application;
using AulaPedidos.Infrastructure;
using AulaPedidos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerOpenApiTransformer>());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCourseAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCourseTelemetry(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
        }));
});
var app = builder.Build();
if (args.Contains("--outbox-status", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AulaPedidosDbContext>();
    var status = await db.OutboxMessages.AsNoTracking().OrderByDescending(message => message.OccurredAt).Take(20)
        .Select(message => new { message.Id, message.Type, message.Attempts, message.NextAttemptAt,
            message.ProcessedAt, message.DeadLetteredAt, message.LastErrorCode }).ToListAsync();
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(status,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    return;
}
// Schema changes are an explicit operation, never an automatic production side effect.
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AulaPedidosDbContext>().Database.MigrateAsync();
    return;
}
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "AulaPedidos API v1"));
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers().RequireRateLimiting("api");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
    }
}).AllowAnonymous();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}
await app.RunAsync();
public partial class Program;
