# Clase 8 — Eventos, Outbox, Adapter, Retry, Circuit Breaker y Cache Aside (240 minutos)

## Objetivo

Persistir intención de notificar junto con el pedido, entregar mediante adaptador, tolerar fallos transitorios y comprender duplicados. Distinguir caché de fuente de verdad y demostrar invalidación. La referencia soporta **una instancia** del dispatcher y caché en memoria de proceso.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «¿Qué pasa si guardamos el pedido y se cae el proceso antes de notificar? Ésta es la grieta que vamos a estudiar.» | Dibujar dos escrituras y marcar ventanas de fallo. |
| 15–40 | «Outbox guarda la intención en la misma transacción local. La red sigue siendo otra operación.» | Abrir SaveChanges del contexto y OutboxMessage; comprobar agregado + mensaje persistidos juntos. |
| 40–65 | «Después de enviar puede fallar el guardado del acuse. Por eso la entrega puede repetirse.» | Simular cronología POST correcto/crash/reenviar; identificar EventId como clave de deduplicación. |
| 65–90 | «Adapter nos permite cambiar transporte sin meter HTTP en negocio. El consumidor conserva responsabilidad por idempotencia.» | Mostrar IOrderNotificationPublisher y adaptador HTTP; revisar receptor mock persistente. |
| 90–115 | «Retry recupera fallos transitorios; un timeout no demuestra que el servidor no procesó. Reintentar POST exige contrato de idempotencia.» | Abrir política HTTP: 2 retries, backoff/jitter, timeouts. Distinguir reintento HTTP del reintento de outbox. |
| 115–140 | «Circuit Breaker deja de insistir durante una degradación. Necesita suficientes muestras y no abre necesariamente en el primer fallo.» | Explicar cerrado/abierto/semiabierto, umbral y recuperación. Generar fallos controlados o revisar test/configuración si tiempo de muestreo no se alcanza. |
| 140–165 | «Cache Aside carga al fallar caché e invalida al escribir. No debemos calcular precio del pedido desde un DTO obsoleto.» | Seguir GetProductHandler/cache/repositorio; modificar precio y comprobar invalidación. |
| 165–210 | «Van a demostrar que un pedido existe aunque falle notificación y que la intención se recupera.» | Laboratorio L08: detener/reanudar receptor, inspeccionar intentos y proceso; dos identidades siguen protegidas. |
| 210–230 | «Al escalar a dos workers aparecen carreras nuevas. Nuestra demo no tiene reclamo SQL multiworker.» | Diseñar brechas: claim/lease, retención, dead-letter, métricas y coherencia caché. |
| 230–240 | «Un patrón resuelve un problema concreto y deja otros visibles.» | Ticket: ventana de duplicado y dónde deduplicar de forma durable. |

## Ensayo de fallo controlado

1. Iniciar API local y receptor mediante scripts. Crear un pedido y ver evento entregado.
2. Detener **sólo** el receptor con Ctrl+C. Crear otro pedido. La respuesta de creación sigue siendo 201 si la transacción local funciona.
3. Observar logs del worker y tabla OutboxMessages: Attempts, NextAttemptAt, LastErrorCode, ProcessedAt y DeadLetteredAt. En SQLite ejecutar `scripts/outbox-status.ps1` desde la referencia; muestra las últimas20 filas de metadatos sin payload ni secretos y no necesita instalar una consola de base. En Compose, `docker compose --env-file .env exec api dotnet AulaPedidos.Api.dll --outbox-status` consulta el proveedor del contenedor.
4. Reiniciar receptor antes de agotar máximo de intentos y observar entrega posterior; el backoff puede retrasarla.
5. Para simular fallos sin cerrar proceso:

```powershell
.\scripts\run-notifications.ps1 -FailuresBeforeSuccess 2
# En otro ensayo, separado del anterior:
.\scripts\run-notifications.ps1 -AlwaysFail
```

Detener la ejecución anterior antes de iniciar la siguiente. Para recuperar, reiniciar receptor sin switches. Un mensaje dead-letter no se reanuda automáticamente: requiere diagnóstico y una operación controlada de reenvío; no borrar su historial.
6. Inspeccionar `Infrastructure/DependencyInjection.cs` para política real y `Outbox/OutboxOptions.cs` para intervalos. Para observar el circuito, iniciar receptor con `-AlwaysFail` y crear seis pedidos seguidos (mismo producto válido y sujeto autorizado). Cada envío puede producir hasta tres intentos HTTP; la configuración exige cinco muestras y ratio0.5 en30s, con apertura10s. Revisar logs/LastErrorCode de mensajes siguientes buscando evidencia de rechazo por circuito (`BrokenCircuitException`, según la excepción propagada). Si el ritmo/configuración no alcanza ventana, medir peticiones antes de concluir. Reiniciar receptor sin flags, esperar ventana de recuperación y revisar pendientes; no afirmar que dos errores siempre abren el circuito.
7. Crear un producto, leerlo dos veces, cambiar precio con Version actual y leerlo otra vez. El DTO final debe mostrar el nuevo valor. Para crear pedido el handler consulta persistencia, no caché de lectura.

**Prompt:**

> Traza pedido y outbox en AulaPedidosDbContext, OutboxDispatcher y HttpOrderNotificationPublisher. Enumera ventanas de caída antes/después de POST y SaveChanges. Explica cuáles pierden datos, cuáles duplican entrega y qué evita EventId. No afirmes exactamente una vez. Propón pruebas con publicador fake y almacenamiento real sin sleeps.

## Laboratorio L08 y solución

**Consigna:** implementar persistencia outbox y worker/adaptador con opciones validadas; demostrar un fallo recuperable, deduplicación del receptor y caché invalidada tras actualizar. Diseñar, sin necesidad de implementar, un cambio para varias instancias.

**Aceptación:** mensaje persistido con pedido; intento fallido mantiene intención; token de cancelación no se traga como error ordinario; reintento acotado; credencial de notificador externa; entrega repetida no repite el efecto en receptor; limitación multiworker documentada.

**Solución:** `Infrastructure/Outbox/{OutboxMessage,OutboxDispatcher,OutboxWorker,OutboxOptions}.cs`, `Persistence/AulaPedidosDbContext.cs`, `Notifications/HttpOrderNotificationPublisher.cs`, `DependencyInjection.cs`, `Caching/MemoryProductCache.cs` y `tools/AulaPedidos.NotificationsMock`. Worker singleton crea scope para dispatcher/DbContext. Receptor guarda EventId en SQLite con unicidad: la deduplicación de esta demo sobrevive al reinicio mientras se preserve esa base. La política productiva de retención y efectos externos sigue pendiente.

**Extensión:** documentar carrera de caché: una lectura iniciada antes de una escritura puede volver a insertar un valor viejo después de invalidación. TTL limita duración, no elimina la carrera. Diseñar generación/versionado de claves o caché distribuida coordinada según requisitos.

## Preguntas y recuperación

- **¿Outbox entrega exactamente una vez?** No; protege intención y admite entrega repetida.
- **¿Retry corrige 400?** En general es permanente; revisar política real y no reintentar indiscriminadamente.
- **¿Más retries mejoran disponibilidad siempre?** Pueden multiplicar carga y latencia; ajustar presupuestos.
- **¿Caché evita consultar datos para cobrar?** No debe sustituir fuente consistente al fijar precio histórico.

No hay entrega: comprobar worker Enabled, receptor, clave y NextAttemptAt. 401/403 receptor: claves no coinciden; ejecutar scripts con mismo material local, no imprimirlas. No abre circuito: revisar volumen/ventana, no inventar estado a partir de un error. Dead-letter: estudiar causa y plan de reenvío, no borrar filas. Si un demo tarda, usar tests deterministas para evidencia y dejar la red como observación adicional.
