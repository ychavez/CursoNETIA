---
applyTo: "src/AulaPedidos.Infrastructure/**/*.cs"
---

Implementa puertos definidos en Application. Mantén transacción de agregado y outbox en la misma base de datos. EF debe filtrar soft delete, auditar con UTC y proyectar consultas sin seguimiento si sólo leen. Verifica diferencias SQLite/SQL Server. El worker debe resolver servicios scoped dentro de un scope. Reintentos acotados y cancelables; caché invalidada después de persistir. No presentes outbox de un solo worker ni caché local como solución horizontal completa.
