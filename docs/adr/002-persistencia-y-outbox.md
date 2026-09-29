# ADR 002: proveedores de aula y entrega de eventos

- Estado: aceptada con limitaciones explícitas.
- Contexto: algunos participantes no tendrán Docker listo al comenzar; se necesita practicar SQL Server y evitar perder una notificación después de guardar un pedido.
- Decisión: SQLite para puesta en marcha local, SQL Server para recorrido de contenedores, migraciones independientes; guardar agregado y mensaje outbox en transacción local. Entregar posteriormente mediante worker y adaptador HTTP.
- Alternativas: notificar antes de guardar puede generar mensajes de pedidos inexistentes; notificar después sin persistir intención puede perder mensajes; transacción distribuida agrega operación innecesaria al aula.
- Consecuencias: entrega al menos una vez, latencia eventual y necesidad de idempotencia. Tests SQLite no sustituyen validación SQL Server. Un worker demostrativo no resuelve reclamo seguro con varias réplicas.
- Evidencia: tests transaccionales, fila outbox pendiente y recuperación al reanudar notificador.
- Revisar al escalar trabajadores, incorporar broker real o exigir garantías operativas específicas.
