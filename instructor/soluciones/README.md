# Soluciones orientativas y uso de la referencia

Las soluciones se describen al final de cada guion y se contrastan con los archivos completos de `src/` y `tests/`. No hay una única redacción correcta ni una respuesta de Copilot que deba copiarse. Aceptar una alternativa si cumple la regla, respeta dependencias y demuestra comportamiento con evidencia.

| Clase | Archivos principales de referencia | Evidencia mínima |
|---|---|---|
| 1 | `.github/copilot-instructions.md`, `instructions/`, `prompts/`, `agents/` | Referencias visibles + bitácora de un rechazo |
| 2 | `src/AulaPedidos.Domain/Entities/Product.cs`, `Order.cs`, `OrderItem.cs` | Invariantes y cero referencias de infraestructura |
| 3 | `Application/Messaging/`, `Common/Result.cs`, `Products/`, `Orders/`, `Abstractions/IPersistence.cs` | Caso de uso y fallo de negocio verificables |
| 4 | `src/AulaPedidos.Api/Program.cs` y endpoints/servicios asociados | HTTP válido, inválido y contrato OpenAPI |
| 5 | `Infrastructure/Persistence/` y `Migrations/` | Esquema reproducible + consulta + conflicto |
| 6 | Configuración/servicios de seguridad Api y `scripts/token.ps1` | 401/403 y acceso a recurso ajeno rechazado |
| 7 | `tests/AulaPedidos.UnitTests`, `tests/AulaPedidos.IntegrationTests` | Regresión roja antes y verde después |
| 8 | `Infrastructure/Outbox`, `Notifications`, `Caching` | Fallo controlado y recuperación sin perder intención |
| 9 | `Dockerfile`, `compose.yaml`, configuración OTel Api | Persistencia + traza + readiness |
| 10 | Proyecto completo y `docs/produccion.md` | Defensa de diseño y rúbrica |

Los nombres de carpetas aquí abreviados se resuelven dentro de `src/AulaPedidos.Application` o `src/AulaPedidos.Infrastructure`. Consultar [recorrido](../recorrido-laboratorio.md) para recuperar una clase sin suponer tags ni commits preexistentes.
