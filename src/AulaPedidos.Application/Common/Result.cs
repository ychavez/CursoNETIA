namespace AulaPedidos.Application.Common;

public enum ErrorKind { Validation, NotFound, Conflict, Forbidden }

public sealed record Error(string Code, string Message, ErrorKind Kind)
{
    public static Error Validation(string message) => new("validation", message, ErrorKind.Validation);
    public static Error NotFound(string entity) => new("not_found", $"No se encontró {entity}.", ErrorKind.NotFound);
    public static Error Conflict(string message) => new("conflict", message, ErrorKind.Conflict);
    public static Error Forbidden() => new("forbidden", "No tienes acceso a este pedido.", ErrorKind.Forbidden);
}

// Result describe fallos esperados; las excepciones técnicas se resuelven en el borde HTTP.
public sealed class Result<T>
{
    private readonly T? _value;
    private Result(bool isSuccess, T? value, Error? error)
        => (IsSuccess, _value, Error) = (isSuccess, value, error);

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Un resultado fallido no tiene valor.");
    public Error? Error { get; }
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}

public readonly record struct Unit
{
    public static Unit Value => default;
}
