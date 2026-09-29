# Mapa de cobertura del temario solicitado

Origen: temario del archivo «Texto pegado.txt» proporcionado para diseñar el curso. Todas las áreas se trabajan; algunas garantías de producción se estudian mediante diseño/ensayo y quedan explícitamente fuera de la promesa de una referencia de aula. **D** = demostración guiada; **P** = práctica del alumno; **E** = evidencia evaluable; **T** = discusión/decisión teórica. Los tiempos están en los guiones; esta matriz no añade horas.

## Contexto, público y objetivos

| Requisito original | Tratamiento concreto | Evidencia |
|---|---|---|
| Empresas de software, TI, consultoras, startups; desarrolladores, arquitectos y líderes | Caso AulaPedidos y parejas con roles; instructor/README | Diagnóstico y escenario empresarial |
| Aplicaciones empresariales, productividad sin perder calidad | Diez clases de construcción + validación | Entrega completa con rúbrica |
| Windows, Visual Studio, SDK, Docker, SQL, GitHub, Copilot e internet | Preparación48h y plan de contingencia | Checklist técnico y versiones |
| POO/C#, SQL, REST e interés en IA/buenas prácticas | Diagnóstico y refuerzo previo | Diez preguntas y ejercicio inicial |
| Arquitectura, patrones, seguridad, testing, Docker, monitoreo | Clases2–9 y desafío10 | Código + evidencia por dimensión |
| Preparación para producción | Clase10; docs/produccion | Brechas con dueño y criterio de cierre |
| Semblanza del instructor | Espacio pendiente de información real en README instructor | No se inventan credenciales |

## IA aplicada al desarrollo

| Tema original | Clase / formato | Archivo o actividad verificable |
|---|---|---|
| Rol actual de IA | 1 T/D | Comparación propuesta plausible vs validada |
| Qué delegar y qué no | 1 P/E;10 E | Matriz y defensa individual |
| Riesgos de generación automática | 1,6,7 D/P/E | Regla contradictoria, revisión seguridad, regresión |
| Supervisión y validación humana | Todas P/E | Diff + tests + bitácora |
| Prompting efectivo | 1 P/E; resto aplicación | `.github/prompts`, objetivo/contexto/restricciones/aceptación/evidencia |
| Productividad y control | 1,7,10 T/P/E | Cambios pequeños y rechazos razonados |
| Uso práctico GitHub Copilot | Todas D/P | VS Chat, contexto y herramientas verificadas |
| Generación inicial de código | 2–6 P | Entidades, handlers, DTOs y adaptadores |
| Refactor asistido | 7 P/E | Tests antes/después, contrato preservado |
| Generación de unit tests | 2,3,7 P/E | Matriz de casos previa al código |
| Debugging con IA | 7,9 D/P/E | Hipótesis, reproducción, causa y regresión |
| Documentación automática | 4,10 P/E | Contrato comparado con código/HTTP |
| Optimización de consultas con IA | 5 P/E | Medición SQL y revisión de plan |
| Revisión técnica asistida | 1,6,7,10 P/E | Prompt revisión, agente y revisión entre pares |
| Harness, instrucciones, prompts y agentes (petición adicional) | 1 P/E, resto uso | docs/harness-copilot + `.github/` |

## Arquitectura y patrones

| Tema original | Clase / formato | Archivo o actividad verificable |
|---|---|---|
| Fundamentos Clean Architecture | 2 T/D/P/E | docs/arquitectura y referencias .csproj |
| Capas/responsabilidades | 2–4 P/E | Domain/Application/Infrastructure/Api |
| SOLID real | 2 T/P/E | Cinco principios aplicados, ADR |
| Organización empresarial | 2,10 P/E | Árbol de solución y defensa |
| Inyección de dependencias | 2,3,5,8 D/P | Registros, lifetimes y scopes del worker |
| Configuración por ambiente | 2,6,9 P/E | Development, variables y Production sin demo |
| Escalabilidad y mantenibilidad | 2,8,10 T/E | Medición, límites multiworker/caché y matriz producción |
| Repository Pattern | 3,5 P/E | IPersistence y Repositories |
| CQRS | 3 P/E | Commands/Queries con DB compartida |
| Mediator Pattern | 3 P/E | Messaging/Mediator y registro handlers |
| Adapter Pattern | 8 P/E | Notificador HTTP detrás de puerto |
| Result Pattern | 3,4 P/E | Common/Result y ResultExtensions |
| Domain Events | 3,8 P/E | OrderSubmitted y recolección de eventos |
| Outbox Pattern | 5,8 P/E | Transacción local, dispatcher y fila persistida |
| Retry Pattern | 8 D/P/E | Backoff HTTP/outbox y recuperación |
| Circuit Breaker | 8 D/P/E | Umbral, fallo controlado y estado observable |
| Cache Aside | 8 D/P/E | MemoryProductCache, TTL e invalidación |

## API y persistencia

| Tema original | Clase / formato | Archivo o actividad verificable |
|---|---|---|
| ASP.NET Core API | 4 P/E | Controllers y Program |
| Diseño de endpoints | 4 P/E | Productos/pedidos y métodos HTTP |
| DTOs y contratos | 3,4 P/E | Input/Dto y aislamiento del dominio |
| Validaciones | 2,3,4,6 P/E | Forma, invariantes, dueño y límites |
| Manejo global errores | 4 P/E | ApiExceptionHandler y ProblemDetails |
| Versionado API | 4 T/P/E | Ruta v1 existente y ejercicio cambio incompatible; v2 no implementada |
| Swagger/OpenAPI | 4 D/P/E | UI, documento y contraste con respuesta real |
| Buenas prácticas REST | 4 P/E | 201/Location,204,400,401,403,404,409 |
| Introducción EF Core | 5 D/P | DbContext, tracking y entidades |
| Code First/Migrations | 5 P/E | Contextos/migraciones duales y SQL revisado |
| Relaciones | 5 P/E | Product–OrderItem–Order, FK y navegación |
| Consultas eficientes | 5 P/E | Paginación, AsNoTracking, lote de productos |
| Índices/performance | 5 D/P/E | docs/rendimiento-sql.md, plan y lecturas |
| Transacciones | 5,8 P/E | Pedido+outbox y fallo controlado |
| Soft Delete | 2,5 P/E | Product.IsDeleted y filtro EF |
| Auditoría de datos | 2,5 P/E | CreatedAt/UpdatedAt, Version; límite auditoría inmutable |

## Seguridad y calidad

| Tema original | Clase / formato | Archivo o actividad verificable |
|---|---|---|
| JWT autenticación | 6 D/P/E | AuthenticationExtensions y token local |
| Roles/permisos | 6 P/E | Admin/Student, policies y claims |
| Protección endpoints | 6 P/E | Atributos, pipeline y tests negativos |
| Validación de entradas | 4,6 P/E | DTOs y cantidades/identificadores inválidos |
| Secretos seguros | 1,6,9 P/E | Exclusión Git, user-secrets, env y límites |
| Vulnerabilidades comunes | 6 P/E | BOLA, sobreasignación, inyección, recursos, dependencias |
| OWASP | 6 T/P/E | Threat model y matriz de controles |
| Seguridad productiva | 6,10 T/E | OIDC real, TLS, bóveda, rotación y runbook |
| Unit Testing | 2,3,7 P/E | Pruebas Domain/Application |
| Integration Testing | 5,6,7 P/E | API y EF relacional |
| Mocking | 3,7 P/E | Fakes/dobles de puertos con semántica |
| Regresión | 7 P/E | Rojo antes del fix, verde después |
| Cobertura | 7 P/E | Cobertura XML con alcance/límites |
| Casos borde | 2,3,6,7 P/E | Límites de precios, cantidades y estados |
| Validar código generado | Todas P/E | Diff, prueba negativa y bitácora |
| Calidad continua | 7,9,10 D/P/E | Verify/CI, locked restore y auditoría de paquetes |

## Contenedores, observabilidad e integración final

| Tema original | Clase / formato | Archivo o actividad verificable |
|---|---|---|
| Introducción contenedores | 9 T/D/P | Imagen vs contenedor vs volumen |
| Dockerfile .NET | 9 P/E | Dockerfile API/receptor |
| Multi-stage builds | 9 P/E | SDK build y runtime final |
| Docker Compose | 9 P/E | Servicios, depends_on y migración explícita |
| Variables entorno | 6,9 P/E | Configuración externa sin publicar secretos |
| Redes entre servicios | 9 P/E | DNS sqlserver/notifications/dashboard |
| Persistencia en contenedores | 9 P/E | Volúmenes y reinicio con datos |
| Despliegue productivo | 9,10 T/E | Ensayo staging, imagen/versionado, migración y rollback; sin publicación cloud automática |
| Fundamentos observabilidad | 9 T/D | Logs/métricas/trazas con distintos propósitos |
| Logs estructurados | 9 P/E | JSON console/campos y correlación |
| Trazas distribuidas | 9 P/E | ASP.NET/HttpClient/receptor; límites de frontera outbox |
| Métricas básicas | 9 P/E | Runtime/HTTP y contador de pedidos |
| Integración .NET | 9 P/E | CourseTelemetry, ActivitySource y Meter |
| Exportación telemetría | 9 P/E | OTLP y dashboard local |
| Health Checks | 9 P/E | live/ready y DB caída |
| Diagnóstico producción | 9,10 P/E | Incidente simulado/runbook sanitizado |
| OpenTelemetry | 9 P/E | Instrumentación/exporter/señales de referencia |
| Aplicación real integrando todos los temas | 10 E | Feature final, defensa, evidencia y plan producción |
| Diez preguntas de diagnóstico originales | Preparación y1 | instructor/diagnostico.md contiene las diez |

## Control final del instructor

No marcar un tema como acreditado sólo por mencionarlo en la pizarra. Registrar por grupo qué se demostró, qué practicaron y qué evidencia faltó. Una estación compartida puede resolver una limitación de Docker, pero no reemplaza la explicación individual. La rúbrica registra diferencias de entorno sin afirmar ejecuciones que no ocurrieron.
