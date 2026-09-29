# Persistencia e integraciones

`AddInfrastructure(configuration)` registra EF Core, repositorios concretos, Unit of Work,
cache-aside en memoria, un cliente HTTP resiliente y un trabajador outbox. Los casos de uso
dependen de contratos de Application; EF Core y HTTP quedan detrás de esos contratos.

## Base de datos y migraciones

`Database:Provider` admite `Sqlite` (predeterminado) y `SqlServer`.
`ConnectionStrings:Database` contiene la conexión. SQL Server exige una cadena configurada;
SQLite usa `Data Source=aula-pedidos.db` por defecto. Las credenciales pertenecen a user-secrets
o variables de entorno, nunca al repositorio.

Los contextos derivados comparten el modelo y mantienen migraciones/snapshots independientes:
`SqliteAulaPedidosDbContext` y `SqlServerAulaPedidosDbContext`. No se debe aplicar una migración
SQLite a SQL Server ni usar `EnsureCreated` junto con migraciones. Los comandos de desarrollo
usan el contexto explícito. La aplicación resuelve el proveedor como `AulaPedidosDbContext`.

Los importes usan `decimal(18,2)` en SQL Server. SQLite guarda `decimal` con precisión textual:
no es un sustituto de validar traducción SQL, restricciones y rendimiento en SQL Server.
Las fechas SQLite usan ticks UTC para comparar/ordenar en el servidor; SQL Server usa
`datetimeoffset`. La auditoría se fija al guardar con `TimeProvider`. `Version` es un Guid
administrado por la aplicación y configurado como token de concurrencia optimista.

Los productos tienen filtro global de borrado lógico. La comprobación de SKU incluye borrados
para respetar el índice único; una consulta previa no sustituye a la restricción en la base.
Las líneas de pedido conservan nombre y precio históricos aunque cambie el producto.
SQL Server usa collation `Latin1_General_100_BIN2` para `CustomerId`, porque un identificador
opaco del JWT distingue mayúsculas y acentos. Dominio, casos de uso y validación del JWT
rechazan espacios en los extremos. Crear un pedido carga todos sus productos activos en
una consulta por IDs (`GetByIdsAsync`), evitando una consulta adicional por cada línea.

## Atomicidad e idempotencia

`SaveChangesAsync` convierte `OrderSubmitted` en `order.submitted.v1` y agrega la fila outbox
antes de una única llamada relacional a `SaveChangesAsync`. Pedido, líneas y evento se confirman
en la misma transacción automática. Los eventos se limpian sólo después de un guardado exitoso.
Si la persistencia falla, se debe desechar el scope/contexto para la siguiente petición.

`OutboxDispatcher.DispatchBatchAsync` selecciona pendientes, publica y persiste el resultado de
cada mensaje. `ProcessedAt` indica entrega; `DeadLetteredAt` requiere intervención después del
límite; `NextAttemptAt` persiste el backoff exponencial acotado. Los logs incluyen EventId,
intento y código de error; nunca payloads ni datos personales. `LastErrorCode` tampoco almacena
mensajes de excepciones ni cuerpos HTTP. El mensaje de integración contiene únicamente
`eventId`, `orderId`, `total` y `occurredAt`.

La entrega es **at-least-once, no exactly-once**. Si el receptor confirma y el emisor cae antes
de guardar `ProcessedAt`, el mismo evento se enviará otra vez. El receptor demo debe persistir
la deduplicación por `EventId` y el efecto local en una sola transacción. Una tabla inbox por sí
sola no vuelve atómico un efecto externo como enviar un correo. El corte por dead-letter limita
los intentos; una entrega finalmente exitosa requiere que la dependencia se recupere o que un
operador corrija la causa y reprograme el mensaje.

`Notifications:BaseUrl` usa `http://localhost:5099/` para el receptor local; fuera del entorno
local use HTTPS. `Notifications:SharedKey` es obligatorio y viaja en `X-Notifications-Key`.
El cliente usa `Microsoft.Extensions.Http.Resilience`: dos reintentos HTTP por despacho,
backoff exponencial con jitter, timeout y circuit breaker. Los reintentos de POST son válidos
aquí únicamente por el contrato de deduplicación durable del receptor. Cinco despachos con
dos reintentos HTTP permiten como máximo quince solicitudes por mensaje, salvo caídas del
proceso antes de persistir el contador. No se multiplican capas de retry sin límite.

**Límite deliberado de la clase:** sólo una instancia de worker por base. Esta demo no tiene
claim/lease de filas y no es segura para repartir trabajo entre varias réplicas. Antes de
escalar hay que agregar adquisición atómica con expiración y token de fencing, o sustituir el
despacho por un mecanismo de cola apropiado. La deduplicación sigue siendo necesaria.

## Caché

La caché guarda DTOs, nunca entidades seguidas por EF. Los casos de uso invalidan después de
guardar; el TTL limita la vida de lecturas antiguas. `IMemoryCache` es local a un proceso: en
varias réplicas harían falta invalidación compartida o caché distribuida. Incluso en un proceso,
la carrera lectura/escritura puede reinsertar un valor antiguo tras invalidar; este ejemplo
acepta consistencia eventual limitada por TTL, no promete coherencia fuerte.

## Referencias verificadas

- [EF Core: transacciones](https://learn.microsoft.com/ef/core/saving/transactions)
- [EF Core: concurrencia optimista](https://learn.microsoft.com/ef/core/saving/concurrency)
- [EF Core: varios proveedores y migraciones](https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers)
- [Limitaciones SQLite](https://learn.microsoft.com/ef/core/providers/sqlite/limitations)
- [Resiliencia HTTP y métodos que modifican estado](https://learn.microsoft.com/dotnet/core/resilience/http-resilience)
