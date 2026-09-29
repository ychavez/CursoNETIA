# Arquitectura de AulaPedidos

AulaPedidos administra catálogo y pedidos de una empresa. El proyecto es una referencia ejecutable para enseñar ingeniería con IA: una API modular desplegada como una unidad, con persistencia relacional y un servicio de notificaciones separado para practicar fallos de red. No incluye una interfaz comercial ni un proveedor real de identidad.

## Reglas del negocio

Un producto tiene identidad, SKU, nombre, precio y estado de borrado lógico. Un pedido pertenece a un usuario, contiene líneas con precio histórico y no acepta cantidades inválidas. Consultar pedidos requiere comprobar pertenencia; tener permiso de lectura no concede acceso a pedidos ajenos. Cambiar datos concurrentemente requiere detectar versiones obsoletas. Cancelar no equivale a borrar la historia. Consulte los métodos y tests del dominio para las restricciones exactas de la versión entregada.

## Dependencias de compilación

```mermaid
flowchart LR
  Api --> Application
  Api --> Infrastructure
  Infrastructure --> Application
  Application --> Domain
  Infrastructure --> Domain
```

Api puede referenciar Infrastructure para registrar implementaciones en el punto de composición. Los handlers de negocio no reciben esa referencia como atajo. Domain no tiene referencias a otros proyectos de la solución. La dirección de una llamada en tiempo de ejecución puede ir desde Application al repositorio concreto gracias a una interfaz; eso no invierte las referencias de compilación.

| Proyecto | Responsabilidad | Lo que debe quedar fuera |
|---|---|---|
| `src/AulaPedidos.Domain` | Entidades, reglas, estados y eventos | HTTP, EF, SQL, contenedores |
| `src/AulaPedidos.Application` | Casos de uso, contratos, puertos, Result y mediación | DbContext concreto, secretos, HttpContext |
| `src/AulaPedidos.Infrastructure` | EF, repositorios, adaptadores externos, outbox y caché | Decidir quién posee un pedido a partir del body |
| `src/AulaPedidos.Api` | Rutas, autenticación, políticas, errores, DI y telemetría | Cálculos de negocio duplicados |
| `tests/` | Evidencia de reglas, HTTP y persistencia | Pruebas que sólo reproducen el código |
| `tools/AulaPedidos.NotificationsMock` | Destino local para simular notificaciones | Datos o credenciales reales |

## Flujo principal

1. ASP.NET Core valida el token y una política comprueba el permiso.
2. Un DTO captura únicamente los campos aceptados por el contrato.
3. El endpoint envía una solicitud al mediador propio; éste resuelve el handler registrado.
4. Application obtiene identidad confiable, carga productos y coordina la operación.
5. Domain aplica invariantes y genera un hecho de negocio.
6. Infrastructure persiste el agregado y el mensaje outbox en una transacción local.
7. El endpoint traduce Result a un estado HTTP documentado.
8. Un worker entrega pendientes al adaptador de notificaciones. Puede repetir una entrega; el consumidor debe tolerarla.

## Patrones con un motivo concreto

| Patrón | Uso en AulaPedidos | Coste o límite que debe explicarse |
|---|---|---|
| Repository | Puertos de persistencia orientados al caso de uso | EF ya ofrece repositorio/unidad de trabajo; no se justifica un CRUD genérico sin valor |
| CQRS | Solicitudes de lectura separadas de escritura | Comparten base; no implica microservicios, event sourcing ni dos bases |
| Mediator | Desacopla transporte del handler por tipo | Registro explícito y resolución deben probarse; más indirección |
| Adapter | Notificador HTTP detrás de un puerto | Contrato externo, timeouts y semántica siguen siendo responsabilidad del equipo |
| Result | Éxitos/fallos de negocio expresados como datos | No reemplaza excepciones inesperadas ni justifica omitir logging |
| Domain Events | Hechos surgidos al cambiar un agregado | No enviar I/O durante una mutación de dominio |
| Outbox | Guardado conjunto de pedido y mensaje | Entrega al menos una vez, posibles duplicados y mensajes venenosos |
| Retry | Recuperación acotada de fallo transitorio | Amplifica carga; peligro de repetir efectos no idempotentes |
| Circuit Breaker | Suspende llamadas durante una degradación | No repara el servicio ni garantiza entrega por sí solo |
| Cache Aside | Lectura de catálogo con caché e invalidación | Caché local no coordina réplicas; no es fuente de verdad |

## SOLID aplicado

SRP: un handler coordina crear pedido y no emite tokens. OCP: otro notificador puede implementar el puerto sin alterar las reglas. LSP: un repositorio debe mantener los contratos de ausencia y cancelación; una implementación que lanza inesperadamente al no encontrar viola expectativas. ISP: puertos pequeños evitan que un lector dependa de escritura. DIP: las decisiones de negocio dependen de abstracciones propias, no del SDK del proveedor. No se evalúa SOLID contando interfaces.

## Datos y consistencia

SQL Server es el proveedor del recorrido Docker; SQLite reduce fricción en el arranque local. Tienen contextos/migraciones propios. Pasar tests SQLite no demuestra traducción SQL Server, bloqueo o rendimiento del servidor. El borrado lógico conserva historial, pero un filtro global no sustituye autorización ni una política de retención. Auditoría técnica de creación/modificación no equivale a un registro inmutable de cumplimiento.

Los montos se modelan como decimal. El precio del pedido se fija al crearlo para conservar su historia aunque cambie el catálogo. La concurrencia optimista permite rechazar una versión vieja; no garantiza por sí sola que cualquier nueva operación sea segura. Deben comprobarse también las invariantes del agregado y la transacción completa.

## Escalabilidad y mantenibilidad

Primero medir latencia, consultas, mensajes pendientes y errores; después decidir índices, caché o réplicas. La estructura admite reemplazar adaptadores y probar reglas aisladamente. No demuestra capacidad para una cantidad de usuarios no medida. Antes de escalar el worker se necesita reclamar mensajes atómicamente, tolerar duplicados y definir retención. Antes de escalar la API se revisan caché, límites, conexión a DB y coordinación de tareas. Ver [producción](produccion.md) y los [ADR](adr/).
