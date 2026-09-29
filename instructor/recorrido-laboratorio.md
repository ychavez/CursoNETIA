# Recorrido desde el esqueleto hasta la solución final

## Dos espacios, dos propósitos

**Referencia** es el repositorio entregado completo. Se usa para demostraciones, validación y contraste de soluciones. **Laboratorio** es un directorio hermano creado por `scripts/New-LabWorkspace.ps1`; allí los alumnos implementan con Copilot. No editar referencia para simular fallos. No hay tags, ramas ni commits de checkpoints preexistentes; los estados se construyen y guardan durante la edición real del curso.

```powershell
# Desde la raíz de la referencia, en PowerShell 7.4+:
.\scripts\New-LabWorkspace.ps1 -Destination ..\AulaPedidos-lab
Set-Location ..\AulaPedidos-lab
dotnet tool restore
dotnet restore AulaPedidos.slnx --locked-mode
dotnet build AulaPedidos.slnx
```

El script exige destino nuevo fuera de la referencia y no sobrescribe trabajo. Copia solución, configuración de compilación, proyectos/paquetes, harness y docs; crea dos Program mínimos con `/health/live`. No incluye entidades, handlers, persistencia ni tests finales. Al comenzar no hay catálogo, seguridad de negocio ni evidencias; que compile sólo confirma el esqueleto.

La carpeta instructor permanece en la referencia. Crear en lab `evidencias/`, copiar allí las plantillas que se utilicen y conservarlas sin secretos. Los scripts de setup completo esperan infraestructura final; **no ejecutarlos contra el esqueleto inicial**.

## Checkpoints verificables

| Checkpoint | Implementación/archivos | Qué demostrar antes de continuar |
|---|---|---|
| C01: entorno | Esqueleto, harness y bitácora | Build; `/health/live`; referencias de Copilot |
| C02: dominio | `Domain/Entities`, `DomainExceptions.cs`; eventos pueden adelantarse como tipos pasivos | Reglas de precio, líneas y cancelación; no dependencia externa |
| C03: aplicación | `Application/Common`, `Abstractions`, `Messaging`, `Products`, `Orders`, DI; `Domain/Events` | Handlers con fakes, autorización de dueño y Result |
| C04: HTTP local | Controllers, ResultExtensions, manejo global de errores, OpenAPI; adaptación temporal de datos | Contratos, estados y errores por HTTP en loopback |
| C05: persistencia | Infrastructure/Persistence, contextos/factorías, migraciones, repositorios; registros DI mínimos | Datos tras reiniciar, filtros y concurrencia |
| C06: seguridad | Api/Security, configuración externa, políticas y claims; scripts locales revisados | Matriz 401/403/404 y dueño validado |
| C07: calidad | Tests Domain/Application/API/EF y bitácora de regresión | Rojo antes, verde después, reporte de cobertura |
| C08: integración externa | Outbox, Notifications, Caching, worker y receptor | Fallo/reanudación, deduplicación y caché |
| C09: operación | Dockerfiles, Compose, OTel, HealthChecks, scripts/CI | Contenedores, volumen, señales y diagnóstico |
| C10: entrega | Feature final + ADR + evidencia + plan de producción | Rúbrica y defensa individual |

## Cómo construir cada incremento sin dependencias futuras

### Clases 2–3

Crear primero tipos del dominio y tests unitarios. Order en la referencia ya emite OrderSubmitted: si se copia como apoyo en clase2, copiar también `Domain/Events` y explicar que son datos cuyo procesamiento llegará después. Application se prueba con repositorios fake en tests, por lo que no necesita EF ni servidor. Los puertos de caché pueden existir sin implementación; los tests de handler usan un doble explícito o un adaptador de memoria mínimo.

### Clase 4: adaptador temporal consciente

Para ejecutar HTTP antes de clase5, crear en lab `src/AulaPedidos.Infrastructure/Teaching/InMemoryPersistence.cs` con implementaciones sencillas de IProductRepository/IOrderRepository/IUnitOfWork y diccionarios de datos sintéticos. El objetivo es cumplir las firmas existentes, paginación y filtrado por cliente; registrar una sola instancia por puerto para conservar datos entre peticiones. Registrar además `IProductCache` con un adaptador temporal sin almacenamiento: Get devuelve null y Set/Remove completan sin efecto, respetando CancellationToken. Los handlers ya necesitan ese puerto aunque Cache Aside se explique en clase8. No simular concurrencia relacional ni llamar a esto una base de producción. Pedir a Copilot el diseño antes de implementarlo y limitar acceso a loopback.

Crear Program de clase4 con controllers, AddApplication, registros del adaptador, manejo global de errores y OpenAPI. Todavía no copiar el Program final entero: referencia contiene seguridad, telemetría y EF que el lab no construyó. Los controllers finales sirven de guía; mientras no esté la autenticación, omitir sus atributos **únicamente en lab** y usar un sujeto de demostración fijo en el controller de pedidos. Escribir un comentario visible `TEMPORAL CLASE 4: retirar en clase 6`. No añadir un CustomerId público al DTO. Esta condición limita explícitamente el checkpoint: no es una API segura ni desplegable.

**Alternativa si el tiempo no alcanza:** ejecutar tests HTTP con un host de pruebas y dobles registrados, mantener `/health/live` como API del lab y demostrar los endpoints desde referencia. Registrar qué integración queda pendiente para clase5. No presentar ausencia de errores de compilación como implementación de HTTP terminada.

### Clase 5: reemplazar adaptador sin cambiar contratos

Crear contextos, configuraciones y repositorios EF. Registrar DbContext scoped y puertos; retirar repositorios Teaching del host, conservando caché temporal hasta clase8. La referencia de `AulaPedidosDbContext.cs` ya incluye persistencia outbox: incorporar `Outbox/OutboxMessage.cs` y `Notifications/OrderNotification.cs` como modelos pasivos ahora; el contexto serializa ese contrato y la migración necesita la tabla. Dejar worker/cliente HTTP para clase8. Si el alumno prefiere agregarlos en clase8, generará una segunda migración explícita por proveedor; no editar una migración ya aplicada para ocultar evolución.

Para este checkpoint registrar sólo servicios existentes; no copiar `DependencyInjection.cs` completo hasta disponer de Notifications/Outbox/Caching. Crear base local exclusiva y usar factorías de diseño; aplicar su migración. Configuración del proveedor se incorpora ahora, mientras configuración JWT queda para clase6. No mezclar migraciones generadas por alumno con copias iniciales de referencia en la misma base.

### Clase 6: cerrar atajos de identidad

Crear AuthenticationExtensions y configuración desde la referencia, comprobar cada parámetro y política. Eliminar sujeto fijo y comentario temporal. Restaurar atributos de autorización en controllers. Copiar desde referencia `scripts/setup.ps1`, `token.ps1`, `run-local.ps1` y archivos de configuración necesarios **después de entender sus precondiciones**; el setup completo compila solución y migra SQLite, por lo que debe ajustarse al estado del lab si faltan componentes o ejecutarse con la referencia como demostración. Mantener secretos y bases del lab separados. El generador actual asigna automáticamente UserSecretsId únicos al laboratorio y distintos entre Api y receptor, y copia NuGet.Config con la fuente aprobada nuget.org. Si se copian los .csproj manualmente en lugar de usar el generador, asignar identificadores exclusivos antes de setup para no sobrescribir configuración de la referencia. Nunca copiar `.local/secrets.json` ni `.env` de otra persona.

### Clases 7–9

Completar tests significativos, después outbox/worker/adaptador/caché y receptor. Integrar entonces DependencyInjection y Program finales, revisando qué añadió cada bloque. Copiar/revisar scripts restantes y contenedores al llegar a operación. Antes de verificar con `--locked-mode`, conservar versiones del material; si una modificación justificada cambia paquetes, actualizar lockfiles mediante restore normal y revisar diff antes de volver a modo locked.

El manifiesto de herramientas está en **`dotnet-tools.json` en la raíz**. No inventar `.config/dotnet-tools.json`. La solución incluye el receptor mock, cuyo Program inicial también debe reemplazarse al implementar clase8.

## Guardar progreso propio

Al final de cada clase: ejecutar criterio de salida, guardar `evidencias/claseNN.md` y hacer commit local si el participante usa Git. El commit se crea realmente en esa máquina; no dar al grupo un SHA ficticio. Se puede trabajar sin Git guardando una copia de los archivos relevantes en otro directorio nuevo. No copiar `bin`, `obj`, secretos ni bases con datos.

## Recuperar sin borrar trabajo

1. Identificar el primer error real y el checkpoint al que pertenece.
2. Guardar el diff y bitácora; no resetear cambios de la pareja.
3. Comparar archivo específico con referencia en Visual Studio; entender sus dependencias antes de copiar.
4. Recuperar el conjunto mínimo: por ejemplo Domain+Events para clase2 o Result+Contracts+Mediator para clase3. Un archivo que referencia tipos futuros no es una recuperación completa.
5. Para clase4/5, usar la demo completa de referencia si integrar dependencias consumiría el laboratorio, y registrar un pendiente concreto del alumno.
6. Si hace falta una solución final compilable para operación, crear un directorio hermano nuevo con la referencia completa mediante Git clone local si hay commits reales, o copia de fuente revisada excluyendo `.git`, `.local`, `.env`, bases, bin/obj y artifacts. No sobrescribir el lab incompleto. El alumno realiza allí el desafío final y declara la ayuda recibida.
7. Repetir la prueba del defecto corregido y después los checks afectados. No copiar la suite final y afirmar que las pruebas fueron diseñadas por el estudiante.

El instructor evalúa aprendizaje por explicación y extensión independiente. Copiar como recuperación es válido si se declara; no acredita por sí solo un objetivo.
