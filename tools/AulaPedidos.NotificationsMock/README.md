# Receptor de notificaciones para la clase

Este servicio simula un consumidor HTTP independiente. Recibe un contrato v1 con `eventId`,
`orderId`, `total` y `occurredAt`; no conoce las entidades Domain ni contiene datos del cliente.

Configure `Notifications:SharedKey` igual que en la API, mediante user-secrets o variable de
entorno. El proyecto tiene UserSecretsId `AulaPedidos-CursoNETIA-Notifications` independiente.
`ConnectionStrings:Database` apunta a su propia SQLite; el valor por defecto es
`Data Source=receipts.db`. El script de preparación establece una ruta local explícita.

`dotnet run --project tools/AulaPedidos.NotificationsMock --launch-profile http` escucha en
`http://localhost:5099`. El encabezado `X-Notifications-Key` se valida con hashes de tamaño
fijo y `CryptographicOperations.FixedTimeEquals`. Para exponer el servicio fuera de localhost
se requiere HTTPS o un proxy TLS con acceso restringido.

| Ruta | Respuesta |
| --- | --- |
| POST `/notifications` | 202 primer recibo; 200 duplicado con `duplicate: true` |
| POST mismo EventId con datos diferentes | 409 `EventIdPayloadMismatch` |
| GET `/notifications/{eventId}` | Recibo persistido; exige la misma clave |
| GET `/health/live` | Salud del proceso |
| GET `/health/ready` | Conexión y tabla de recibos disponibles |

La PK `EventId` y una transacción SQLite evitan duplicar el efecto local (la fila del recibo),
incluso después de reiniciar el proceso. Se usa SQL parametrizado. La deduplicación y otros
efectos dentro de la MISMA base pueden participar en esa transacción; el envío de un correo
externo necesitaría otra outbox. Esto no convierte el transporte en exactly-once.

Para provocar fallos configure `Simulation:FailuresBeforeSuccess` (primeras N solicitudes
autorizadas y válidas devuelven 503) o `Simulation:AlwaysFail=true`. Los contadores de fallo
se reinician con el proceso; los recibos permanecen en SQLite. Quite el fallo para observar
la recuperación del outbox/circuit breaker. Las opciones se leen al inicio.

OpenTelemetry registra el span HTTP de entrada y continúa `traceparent`. Si configura
`OTEL_EXPORTER_OTLP_ENDPOINT`, exporta trazas al colector. No se registran claves ni payloads.

Verificación realizada: salud 200, clave ausente 401, fallo simulado 503, primer recibo 202,
duplicado 200, conflicto de datos 409 y duplicado 200 después de reiniciar el proceso.
