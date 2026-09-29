namespace AulaPedidos.Domain;

public sealed class DomainValidationException(string message) : Exception(message);
public sealed class DomainConflictException(string message) : Exception(message);
