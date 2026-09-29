# Clase 3 — Casos de uso y patrones de aplicación (240 minutos)

## Objetivo

Conectar reglas con solicitudes de negocio sin acoplarlas a HTTP o EF. Resultado: CreateProduct/ListProducts/CreateOrder/GetOrder y cancelación modelados como comandos/consultas, puertos de persistencia, mediador propio, Result y evento OrderSubmitted.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «Un producto correcto no basta: alguien debe coordinar cargar datos, aplicar reglas y guardar.» | Seguir un caso “crear pedido” en la pizarra sin infraestructura. |
| 15–40 | «Repository abstrae operaciones que la aplicación necesita. EF ya abstrae mucha persistencia; nuestra interfaz se justifica por la frontera y las pruebas.» | Diseñar IProductRepository/IOrderRepository/IUnitOfWork con operaciones concretas; evitar IRepository genérico automático. |
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
2. Crear puertos en `Application/Abstractions/IPersistence.cs`; las firmas referencia son una guía concreta.
3. Crear contratos y handler de CreateProduct. Para la clase, usar un fake en tests; no inventar que ya existe SQL.
4. Implementar mediador y registro `Application/DependencyInjection.cs`. Mostrar que falta de handler es un error de configuración detectable con integración, no una validación del usuario.
5. Implementar CreateOrder: validar CustomerId, cargar productos vigentes, producir OrderItem con precio del dominio, crear Order y guardar. Endpoint de clase 4 pasará identidad confiable; nunca tomar CustomerId del body HTTP.
6. Añadir `Domain/Events/OrderSubmitted.cs` y comprobar evento después de crear. No retirar evento antes de persistir correctamente.

```powershell
dotnet build src/AulaPedidos.Application/AulaPedidos.Application.csproj
dotnet test tests/AulaPedidos.UnitTests/AulaPedidos.UnitTests.csproj
```

**Prompt:**

> Lee las interfaces y Result existentes. Implementa sólo el handler de consulta de pedido. Debe devolver NotFound si no existe y Forbidden si pertenece a otro cliente; no usar HTTP ni EF, y propagar CancellationToken. Primero dame tabla de escenarios. Añade pruebas con repositorio fake para propio, ajeno y ausente. Explica por qué un GUID difícil de adivinar no sustituye autorización.

## Laboratorio L03 y solución

**Consigna:** completar GetOrder y CancelOrder utilizando la misma frontera de dueño; probar ausencia, dueño incorrecto, versión obsoleta y cancelación válida. Crear un evento al enviar pedido y explicar cómo persistir intención sin efectuar I/O en Domain.

**Aceptación:** Application no referencia Infrastructure; Result diferencia fallo esperado; el mediador invoca handler correcto; permiso/identidad no se inventan en el handler; pedido ajeno nunca se devuelve.

**Solución:** revisar `Application/Orders/OrderHandlers.cs`, `OrderContracts.cs`, `Messaging/Contracts.cs`, `Messaging/Mediator.cs`, `Common/Result.cs`, `Abstractions/IPersistence.cs` y `Domain/Events/OrderSubmitted.cs`. Comprobar dueño antes de exponer el DTO; comparar Version recibida con actual; persistir mutación y traducir conflictos. El fake captura SaveChanges para comprobar que no se guarda ante un fallo.

**Extensión:** diseñar un escenario en que el mismo evento se reciba dos veces. La respuesta debe proponer identidad estable de evento y deduplicación; no asegurar “exactamente una vez”. La implementación completa se estudia en clase 8.

## Preguntas, errores y recuperación

- **¿CQRS obliga a usar dos bases?** No, se separan modelos/intenciones; separación física es otra decisión.
- **¿Result evita toda excepción?** No; hace explícitos fallos esperados y deja excepciones inesperadas al manejo global.
- **¿Evento y comando son iguales?** Comando pide realizar algo y puede rechazarse; evento afirma un hecho ocurrido.
- **¿Por qué no hacer mock de todas las clases?** Puede acoplar tests a implementación. Dobles útiles viven en límites y conservan semántica.

Si DI no resuelve el handler, revisar tipos genéricos y registro en Application; no duplicar lógica en endpoint. Si un fake hace pasar una regla imposible, corregir el fake o usar la entidad real. Si se atascan, recuperar los tipos de infraestructura de aplicación desde referencia (Result, contratos, mediador) y mantener al alumno responsable del handler y sus pruebas; registrar esa ayuda.
