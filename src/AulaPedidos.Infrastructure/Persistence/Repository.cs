using AulaPedidos.Application.Abstractions;
using AulaPedidos.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AulaPedidos.Infrastructure.Persistence;

/// <summary>Base EF Core reutilizable; comparte el DbContext scoped y su unidad de trabajo.</summary>
public class Repository<T>(AulaPedidosDbContext db) : IRepository<T> where T : class, IAggregateRoot
{
    protected AulaPedidosDbContext Context { get; } = db;

    // Cada agregado puede completar su grafo aquí. IQueryable permanece dentro de Infrastructure.
    protected virtual IQueryable<T> Query => Context.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        // Respeta el borrado lógico persistido incluso si la entidad sigue en el tracker.
        // FindAsync podría devolver primero una entidad local y saltarse esos filtros.
        Query.AsTracking().SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<T>> GetByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0) return [];
        return await Query.AsNoTracking().Where(entity => ids.Contains(entity.Id)).ToListAsync(cancellationToken);
    }

    public void Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Set<T>().Add(entity);
    }
}
