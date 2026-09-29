# Repositorio genérico `IRepository<T>`

`IRepository<T>` reúne las operaciones comunes de persistencia de los agregados `Product` y `Order`. Los repositorios especializados heredan ese contrato y añaden las consultas propias del negocio. Application depende de interfaces; Infrastructure implementa las consultas con EF Core.

## Contrato y responsabilidades

| Componente | Responsabilidad |
|---|---|
| `Domain/Abstractions/IAggregateRoot.cs` | Identidad `Guid Id` de una raíz de agregado. `Product` y `Order` lo implementan. |
| `Application/Abstractions/IRepository.cs` | Contrato genérico para buscar por identidad, consultar un lote y agregar. |
| `Application/Abstractions/IPersistence.cs` | Consultas especializadas de producto/pedido y contrato de unidad de trabajo. |
| `Infrastructure/Persistence/Repository.cs` | Implementación EF reutilizable y consulta base extensible. |
| `Infrastructure/Persistence/Repositories.cs` | Comportamiento específico de `ProductRepository` y `OrderRepository`. |
| `Infrastructure/DependencyInjection.cs` | Registro scoped y resolución coherente de interfaces genéricas y específicas. |

`T` debe ser una clase que implemente `IAggregateRoot`. `OrderItem` pertenece a `Order` y no recibe un repositorio independiente. La interfaz de dominio no depende de EF y no añade columnas: introducir este contrato no requiere una migración de esquema.

El contrato tiene tres operaciones:

```csharp
public interface IRepository<T> where T : class, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    void Add(T entity);
}
```

- `GetByIdAsync` devuelve la entidad con seguimiento para ejecutar métodos de dominio y guardar sus cambios. Devuelve `null` si la consulta no encuentra el agregado.
- `GetByIdsAsync` consulta un lote sin seguimiento. Es útil para cargar el catálogo al crear un pedido y evita una consulta por producto. No garantiza el orden de los identificadores recibidos.
- `Add` registra un agregado nuevo en el contexto. La escritura ocurre cuando se llama a `IUnitOfWork.SaveChangesAsync`.

Las consultas conservan los filtros globales. `IProductRepository` añade validación de SKU y listado paginado; `IOrderRepository` añade listado paginado por cliente. No hay un listado global genérico de pedidos. Los detalles de consulta, incluido `IQueryable`, permanecen dentro de Infrastructure.

## Cómo inyectarlo

Este ejemplo muestra la mecánica de creación en Application; los handlers reales también coordinan validación, conflictos, permisos y caché según su caso de uso:

```csharp
using AulaPedidos.Application.Abstractions;
using AulaPedidos.Domain.Entities;

public sealed class CrearProductoDemo(
    IRepository<Product> products,
    IUnitOfWork unitOfWork)
{
    public async Task<Guid> CrearAsync(CancellationToken cancellationToken)
    {
        var product = Product.Create(
            $"DEMO-{Guid.NewGuid():N}"[..32], "Producto de demostración", 125m);
        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
```

Inyecta `IProductRepository` cuando necesites `SkuExistsAsync` o `ListAsync`; inyecta `IOrderRepository` para `ListByCustomerAsync`. Ambas interfaces conservan las operaciones heredadas de `IRepository<T>`.

La aplicación ya usa esta inyección: `GetProductHandler` y `DeleteProductHandler` reciben `IRepository<Product>`; `CreateOrderHandler` recibe `IRepository<Product>` e `IRepository<Order>`; `GetOrderHandler` y `CancelOrderHandler` reciben `IRepository<Order>`. Los listados y las operaciones que verifican SKU mantienen los puertos específicos.

Para modificar una entidad, cárgala con seguimiento, ejecuta su operación de dominio y guarda con la misma unidad de trabajo:

```csharp
var product = await products.GetByIdAsync(id, cancellationToken);
if (product is null) return;

product.Update(product.Sku, "Nombre actualizado", product.Price);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

El borrado lógico sigue la misma secuencia con `product.SoftDelete()`. No se necesita un método genérico `Update` ni `Remove`. El seguimiento detecta cambios; los métodos de dominio protegen invariantes. `GetByIdsAsync` sirve para lectura: modificar sus resultados no incorpora esas modificaciones al contexto automáticamente.

En los handlers HTTP se mantienen la comprobación de versión, los resultados de error y la invalidación de caché después de guardar. Para pedidos se comprueba la pertenencia antes de devolver datos o cancelar; tener un repositorio genérico no concede acceso al recurso.

## Agregados completos y registro DI

La consulta base de `Repository<T>` se puede especializar dentro de Infrastructure. `OrderRepository` incluye `Items` para que una carga por identidad o por lote devuelva las líneas del pedido. Se comprueba desde un contexto nuevo, donde no hay entidades previas que puedan ocultar la ausencia del `Include`.

Los registros de `IRepository<Product>` y `IRepository<Order>` apuntan a las mismas instancias scoped que sus interfaces específicas. Todos usan el mismo `AulaPedidosDbContext` que `IUnitOfWork`. Así, el pedido y su outbox se persisten en la transacción que ya controla el contexto.

Se registran los tipos concretos admitidos. No existe un registro abierto automático de `IRepository<>`: cada agregado exige decidir su carga completa. Esto también evita tener dos implementaciones candidatas del mismo agregado al resolver una colección de repositorios.

Para introducir una raíz nueva:

1. Implementar `IAggregateRoot`, sus invariantes y su configuración EF.
2. Si no necesita consultas o navegaciones especiales, registrar `services.AddScoped<IRepository<NuevoAgregado>, Repository<NuevoAgregado>>();`.
3. Si necesita cargar navegaciones o consultas propias, crear un repositorio especializado que herede de `Repository<NuevoAgregado>`; configurar su consulta base y registrar la interfaz genérica como alias de esa misma instancia scoped.
4. Crear la migración correspondiente al nuevo modelo y probar la persistencia real. El contrato genérico por sí solo no modifica el esquema.

## Demostración para el instructor

En [clase 3](../instructor/clases/03-patrones.md), diseñar el contrato y practicar su uso con dobles. En [clase 5](../instructor/clases/05-datos.md), implementar EF y demostrar estos comportamientos:

1. Agregar un producto por `IRepository<Product>`; comprobar que una nueva lectura desde otro contexto aún no lo encuentra antes de guardar.
2. Ejecutar `IUnitOfWork.SaveChangesAsync` y volver a consultarlo desde otro contexto.
3. Cargar por identidad, modificar con `Update` y guardar; comprobar los valores persistidos.
4. Ejecutar `SoftDelete`, guardar y volver a consultar por identidad **en el mismo contexto**: debe devolver `null`. No reemplazar esta consulta por un `FindAsync` que pueda devolver una entidad borrada ya rastreada.
5. Cargar un pedido por `IRepository<Order>` desde un scope nuevo y comprobar que contiene sus líneas. Repetir por lote.
6. Seguir la creación de un pedido hasta la unidad de trabajo y verificar tanto el pedido como su mensaje outbox.

**Prompt de práctica:**

> Lee IRepository, IAggregateRoot, Repository, los repositorios especializados y su registro DI. Explica qué métodos conservan seguimiento y dónde se confirma la transacción. Propón una prueba relacional que detecte un producto borrado ya rastreado y otra que detecte un pedido cargado sin líneas. Mantén las comprobaciones de dueño, versión y caché de los handlers. No expongas IQueryable en Application ni añadas borrado físico.

Las pruebas usan SQLite relacional con las migraciones existentes. La validación del proveedor SQL Server sigue siendo parte del recorrido de persistencia del curso.
