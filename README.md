# Curso .NET con IA y GitHub Copilot — AulaPedidos

Curso práctico de **40 horas efectivas**, organizado en **10 clases de 4 horas**. La solución de referencia implementa una API empresarial de catálogo y pedidos; la carpeta [instructor](instructor/README.md) contiene el guion para enseñarla paso a paso. Descansos fuera de las 40 horas.

## Empezar

Requisitos: Windows, PowerShell 7.4 o posterior, SDK .NET 10, Visual Studio 2026 con desarrollo web y GitHub Copilot habilitado. Docker Desktop con contenedores Linux para el módulo SQL Server/contenedores. Acceso a NuGet en la primera restauración. El recorrido local funciona con SQLite.

Desde la raíz:

```powershell
./scripts/setup.ps1
./scripts/verify.ps1 -Coverage
```

En terminales separadas:

```powershell
# Terminal 1: receptor de notificaciones de laboratorio
./scripts/run-notifications.ps1

# Terminal 2: API
./scripts/run-local.ps1

# Terminal 3: prueba completa HTTP
./scripts/smoke.ps1

# Token temporal para Swagger, sin el prefijo Bearer
./scripts/token.ps1 -Role Admin -Subject instructor
```

Abre [Swagger](http://localhost:5080/swagger), pulsa **Authorize** y pega el token. También puedes abrir [requests/AulaPedidos.http](requests/AulaPedidos.http) en Visual Studio. En F5 selecciona AulaPedidos.Api como proyecto de inicio y perfil http. setup configura su cadena SQLite y secretos mediante User Secrets.

setup es repetible: conserva datos y claves existentes. `.local/secrets.json` y `.env` contienen **credenciales desechables del laboratorio**, están ignorados por Git y no deben compartirse ni reutilizarse. No existe un endpoint de login inseguro: los JWT de aula se generan localmente y la API rechaza este modo fuera de Development.

## Qué incluye

| Carpeta | Uso |
|---|---|
| src/AulaPedidos.Domain | Entidades, reglas, eventos; sin EF ni HTTP |
| src/AulaPedidos.Application | Casos de uso CQRS, Mediator, Result y contratos |
| src/AulaPedidos.Infrastructure | EF Core, repositorios, migraciones de ambos motores, Cache Aside, outbox y resiliencia HTTP |
| src/AulaPedidos.Api | API v1, DTOs, validación, JWT, permisos, ProblemDetails, OpenAPI, health checks y OpenTelemetry |
| tools/AulaPedidos.NotificationsMock | Receptor con deduplicación persistente por EventId y fallos simulados |
| tests | Pruebas de reglas, mocks, API, arquitectura, persistencia, concurrencia y outbox |
| .github | Harness Copilot: instrucciones, prompts, agentes y CI |
| instructor | Preparación, diez guiones, ejercicios, soluciones, rúbrica y cobertura del temario |
| docs | Arquitectura, ADR, seguridad y operación |
| scripts / requests | Arranque, validación, laboratorio y ejemplos HTTP |

Patrones implementados: [Repository genérico `IRepository<T>` y repositorios especializados](docs/repositorio-generico.md), CQRS, Mediator, Adapter, Result, Domain Events, Outbox, Retry, Circuit Breaker y Cache Aside. Los pedidos conservan los precios históricos; las mutaciones exigen una versión; cada usuario consulta y cancela solamente sus propios pedidos.

## Dar el curso

Presenta el recorrido con la [presentación para alumnos (PowerPoint)](instructor/clases/00-presentacion-alumnos.pptx), que resume el proyecto y lo que veremos en cada clase.

1. Lee [instructor/README.md](instructor/README.md) y realiza la preparación.
2. Abre [AulaPedidos.slnx](AulaPedidos.slnx) como solución de referencia.
3. Crea un punto de partida independiente para cada alumno:
   `./scripts/New-LabWorkspace.ps1 -Destination ../AulaPedidos-lab`.
4. Sigue los diez guiones. Incluyen qué decir, qué hacer, prompts, resultados, preguntas, errores frecuentes y recuperación.
5. Usa la rúbrica final para comprobar comprensión y evidencia técnica; generar código con IA no basta.

El scaffolding del alumno contiene proyectos y paquetes compilables, pero **no copia la solución implementada ni pruebas resueltas**. El recorrido de instructor indica cómo introducir los componentes gradualmente y recuperar una clase desde la referencia sin depender de branches o commits inexistentes.

## Docker, SQL Server y telemetría

```powershell
# Detén primero los procesos locales que usan 5080/5099.
docker compose --env-file .env config --quiet
docker compose --env-file .env up --build -d
./scripts/smoke.ps1
docker compose logs -f api notifications
docker compose down
```

Compose levanta SQL Server Developer, un proceso de migración, API, receptor y dashboard de Aspire. Visita [dashboard local](http://localhost:18888) para logs, métricas y trazas. SQL Server escucha sólo en localhost:14333. API y receptor corren como usuario no root. Los volúmenes conservan los datos; `down` no los borra.

Para instrumentar los procesos locales con el dashboard de Docker, establece `$env:OTEL_EXPORTER_OTLP_ENDPOINT='http://localhost:4317'` antes de iniciarlos. El dashboard anónimo y HTTP son opciones exclusivas del laboratorio local.

## Validación y límites

Consulta [VALIDACION.md](VALIDACION.md) para los comandos y resultados ejecutados en esta entrega. CI incluye build, pruebas, consistencia de migraciones y arranque de contenedores; dejar el workflow configurado no significa que GitHub Actions ya lo haya ejecutado.

Esta solución está completa para el alcance docente, no es una certificación de preparación productiva. Antes de publicar: proveedor OIDC real, TLS y proxy confiable, secretos administrados, usuario SQL de mínimos privilegios, backups/restauración, pruebas de carga, deduplicación e idempotencia de comandos, cache distribuida y claim/lease de outbox para múltiples workers. SQL Server Developer sólo se usa para desarrollo. La outbox entrega **al menos una vez**, con deduplicación en el receptor; no promete exactly once. El catálogo usa caché local de duración limitada.

Referencias de plataforma: [Microsoft .NET](https://learn.microsoft.com/dotnet/core/releases-and-support), [ASP.NET Core JWT](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0), [personalización de Copilot en Visual Studio](https://learn.microsoft.com/visualstudio/ide/copilot-chat-context?view=visualstudio). Las fuentes ampliadas y decisiones están en docs.
