namespace AulaPedidos.Domain.Abstractions;

/// <summary>Raíz de agregado que puede recuperarse y persistirse mediante un repositorio.</summary>
public interface IAggregateRoot
{
    Guid Id { get; }
}
