using AulaPedidos.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace AulaPedidos.Api.Controllers;
public static class ResultExtensions
{
    public static ObjectResult ToProblem(this ControllerBase controller, Error error) => controller.Problem(
        statusCode: error.Kind switch
        {
            ErrorKind.Validation => 400, ErrorKind.NotFound => 404,
            ErrorKind.Conflict => 409, ErrorKind.Forbidden => 403, _ => 500
        }, title: error.Message, extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
