using AulaPedidos.Domain.Abstractions;

namespace AulaPedidos.Application.Abstractions;

/// <summary>
/// Operaciones compartidas por repositorios de agregados. El commit pertenece a IUnitOfWork.
/// Las consultas específicas, la autorización y las reglas de negocio quedan fuera de este contrato.
/// </summary>
public interface IRepository<T> where T : class, IAggregateRoot
{
    /// <summary>Obtiene un agregado con seguimiento para aplicar cambios mediante sus métodos de dominio.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lee un lote sin seguimiento. Sólo devuelve IDs existentes que respeten los filtros de consulta.</summary>
    Task<IReadOnlyList<T>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Registra un agregado nuevo; no confirma cambios en la base de datos.</summary>
    void Add(T entity);
}
