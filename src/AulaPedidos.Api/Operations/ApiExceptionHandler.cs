using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AulaPedidos.Api.Operations;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var conflict = exception is DbUpdateConcurrencyException || exception is DbUpdateException
        {
            InnerException: SqlException { Number: 2601 or 2627 } or
            SqliteException { SqliteExtendedErrorCode: 1555 or 2067 }
        };
        var status = conflict ? 409 : 500;
        logger.LogError("Fallo {ExceptionType}; HTTP {Status}; trace {TraceId}",
            exception.GetType().Name, status, context.TraceIdentifier);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = conflict ? "El recurso cambió o entra en conflicto con un registro existente." : "Ocurrió un error inesperado.",
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
    }
}
