# Clase 3 — Casos de uso y patrones de aplicación (240 minutos)

## Objetivo

Conectar reglas con solicitudes de negocio sin acoplarlas a HTTP o EF. Resultado: CreateProduct/ListProducts/CreateOrder/GetOrder y cancelación modelados como comandos/consultas, `IRepository<T>` con repositorios especializados, mediador propio, Result y evento OrderSubmitted.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «Un producto correcto no basta: alguien debe coordinar cargar datos, aplicar reglas y guardar.» | Seguir un caso “crear pedido” en la pizarra sin infraestructura. |
| 15–40 | «Productos y pedidos comparten carga y alta. IRepository de T expresa ese contrato; SKU y pedidos de un cliente siguen siendo consultas específicas. La unidad de trabajo decide cuándo guardar.» | Crear IAggregateRoot e IRepository con GetByIdAsync/GetByIdsAsync/Add. Hacer heredar IProductRepository/IOrderRepository; conservar IUnitOfWork. Trazar uso genérico y especializado. |
| 40–65 | «CQRS aquí separa intención de leer y escribir. Seguimos con una sola base y un solo despliegue.» | Crear records de comandos/queries y DTOs. Contrastar `CreateOrderCommand` y `GetOrderQuery`. |
| 65–90 | «El mediador encuentra al handler de una solicitud. No decide reglas ni nos obliga a comprar otra biblioteca.» | Mostrar IRequest/IRequestHandler/IMediator y registro DI. Trazar una resolución paso a paso. |
| 90–115 | «No encontrar un producto es un resultado esperado. Perder conexión es un fallo inesperado. No deben confundirse en un catch que devuelve 400 a todo.» | Implementar Result/Error/Unit. Clasificar validación, no encontrado y conflicto. |
| 115–140 | «Crear un pedido captura precios vigentes y el dueño validado. El cliente no nos envía un total confiable.» | Implementar handler con repositorio fake, un producto por línea y token de cancelación. Revisar límite de líneas y duplicados. |
| 140–160 | «Un evento relata un hecho ya ocurrido: pedido enviado. No entrega un correo dentro de la entidad.» | Crear OrderSubmitted, capturarlo en Domain. Dibujar futura transacción outbox sin implementarla aún. |
| 160–205 | «Construyan un caso de uso completo y demuestren éxito y fallo con dobles controlados.» | Laboratorio L03. Rotar conductor y revisar dependencias. |
| 205–225 | «¿Qué garantía exige el puerto? Un doble útil debe mantener ese contrato.» | Mostrar fake de repositorio y revisar que devuelve ausencia de forma coherente. Ejecutar tests. |
| 225–240 | «La API de mañana adaptará HTTP a estas solicitudes. No moveremos aquí headers o códigos de estado.» | Recapitulación mediante un alumno que trace solicitud-handler-dominio-resultado. Ticket: evento vs comando. |

## Pasos de demostración

1. Crear `Application/Common/Result.cs` y `Pagination.cs`: éxito/error mutuamente excluyentes y página acotada.
2. Crear `Domain/Abstractions/IAggregateRoot.cs` con `Guid Id` y aplicarlo a Product/Order; explicar por qué OrderItem permanece dentro de Order. Crear `Application/Abstractions/IRepository.cs` con GetByIdAsync/GetByIdsAsync/Add y restricción a raíces de agregado. En `IPersistence.cs`, heredar las interfaces específicas y conservar sus consultas y la unidad de trabajo. Usar la [guía del repositorio genérico](../../docs/repositorio-generico.md).
3. Crear contratos y handler de CreateProduct. Para la clase, usar un fake en tests; no inventar que ya existe SQL.
4. Implementar mediador y registro `Application/DependencyInjection.cs`. Mostrar que falta de handler es un error de configuración detectable con integración, no una validación del usuario.
5. Implementar CreateOrder: validar CustomerId, cargar productos vigentes, producir OrderItem con precio del dominio, crear Order y guardar. Endpoint de clase 4 pasará identidad confiable; nunca tomar CustomerId del body HTTP.
6. Añadir `Domain/Events/OrderSubmitted.cs` y comprobar evento después de crear. No retirar evento antes de persistir correctamente.

Dentro del bloque 15–40, dedicar cinco minutos a identidad/agregado, ocho al contrato común, siete a las interfaces específicas y cinco a recorrer Add → SaveChanges. Decir: «T cambia el tipo sobre el que trabajamos; no elimina reglas de negocio. Add prepara el alta y la unidad de trabajo confirma. Cambiar un producto sigue pasando por Product.Update o SoftDelete». Mostrar una variable `IRepository<Product>` que recibe el fake de `IProductRepository`, llamar Add y comprobar que el fake de unidad de trabajo aún no guardó. El ejemplo de inyección completo está en la guía. En clase 5 se comprobarán seguimiento y consultas reales con EF.

```console
dotnet build src/AulaPedidos.Application/AulaPedidos.Application.csproj
dotnet test tests/AulaPedidos.UnitTests/AulaPedidos.UnitTests.csproj
```

**Prompt:**

> Lee IRepository, las interfaces especializadas y Result existentes. Implementa sólo el handler de consulta de pedido. Debe devolver NotFound si no existe y Forbidden si pertenece a otro cliente; no usar HTTP ni EF, y propagar CancellationToken. Primero dame tabla de escenarios. Añade pruebas con repositorio fake para propio, ajeno y ausente. Explica qué operaciones son comunes y por qué un GUID difícil de adivinar no sustituye autorización.

## Laboratorio L03 y solución

**Consigna:** definir `IRepository<T>`, extenderlo con los puertos específicos y completar GetOrder y CancelOrder utilizando la misma frontera de dueño; probar ausencia, dueño incorrecto, versión obsoleta y cancelación válida. Crear un evento al enviar pedido y explicar cómo persistir intención sin efectuar I/O en Domain.

**Aceptación:** Application no referencia Infrastructure; el contrato genérico acepta raíces de agregado y no expone EF/IQueryable; Result diferencia fallo esperado; el mediador invoca handler correcto; permiso/identidad no se inventan en el handler; pedido ajeno nunca se devuelve. El alumno distingue Add de SaveChanges y explica cuándo necesita la interfaz específica.

**Solución:** revisar `Application/Orders/OrderHandlers.cs`, `OrderContracts.cs`, `Messaging/Contracts.cs`, `Messaging/Mediator.cs`, `Common/Result.cs`, `Abstractions/IRepository.cs`, `Abstractions/IPersistence.cs`, `Domain/Abstractions/IAggregateRoot.cs` y `Domain/Events/OrderSubmitted.cs`. Comprobar dueño antes de exponer el DTO; comparar Version recibida con actual; persistir mutación y traducir conflictos. El fake de un repositorio especializado implementa también el contrato heredado. El fake de unidad de trabajo captura SaveChanges para comprobar que no se guarda ante un fallo.

**Extensión:** diseñar un escenario en que el mismo evento se reciba dos veces. La respuesta debe proponer identidad estable de evento y deduplicación; no asegurar “exactamente una vez”. La implementación completa se estudia en clase 8.

## Preguntas, errores y recuperación

- **¿CQRS obliga a usar dos bases?** No, se separan modelos/intenciones; separación física es otra decisión.
- **¿IRepository debe tener Update y Delete?** En este modelo las entidades cargadas por identidad se modifican mediante métodos de dominio; EF detectará los cambios y la unidad de trabajo guardará. El borrado de producto es lógico y un pedido se cancela. Un CRUD genérico no debe reemplazar esas reglas.
- **¿Result evita toda excepción?** No; hace explícitos fallos esperados y deja excepciones inesperadas al manejo global.
- **¿Evento y comando son iguales?** Comando pide realizar algo y puede rechazarse; evento afirma un hecho ocurrido.
- **¿Por qué no hacer mock de todas las clases?** Puede acoplar tests a implementación. Dobles útiles viven en límites y conservan semántica.

Si DI no resuelve el handler, revisar tipos genéricos y registro en Application; no duplicar lógica en endpoint. Si un fake hace pasar una regla imposible, corregir el fake o usar la entidad real. Si se atascan, recuperar los tipos de infraestructura de aplicación desde referencia (Result, contratos, mediador) y mantener al alumno responsable del handler y sus pruebas; registrar esa ayuda.
