---
applyTo: "src/AulaPedidos.Infrastructure/**/*.cs"
---

Implementa puertos definidos en Application. Mantén transacción de agregado y outbox en la misma base de datos. EF debe filtrar soft delete, auditar con UTC y proyectar consultas sin seguimiento si sólo leen. Verifica diferencias SQLite/SQL Server. El worker debe resolver servicios scoped dentro de un scope. Reintentos acotados y cancelables; caché invalidada después de persistir. No presentes outbox de un solo worker ni caché local como solución horizontal completa.

Reutiliza `Repository<T>` y especializa su consulta base para cargar agregados completos: pedidos incluyen sus líneas. Respeta filtros en consultas genéricas; no uses `FindAsync` como sustituto cuando pueda devolver borrados ya rastreados. Registra cada `IRepository<T>` como alias scoped de su repositorio específico cuando exista, con el mismo contexto que `IUnitOfWork`; no agregues un registro genérico abierto que omita las navegaciones requeridas. `Add` no guarda automáticamente.
