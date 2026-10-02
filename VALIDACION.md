# Validación de la entrega

Evidencia previa: 29 de septiembre de 2026. Entorno: Windows, SDK .NET 10.0.401, runtime 10.0.12. Se conserva la evidencia histórica y se registra más abajo la validación de las utilidades C# del 1 de octubre de 2026.

## Ejecutado en la entrega previa

| Comprobación | Resultado |
|---|---|
| Restauración con lockfiles | Correcta |
| Build de solución Debug y Release | Sin errores ni advertencias en la ejecución final |
| Preparación local de la entrega inicial | Claves locales, User Secrets, restauración, build y migración SQLite correctos |
| Verificación de la entrega inicial | 60 pruebas unitarias + 25 de integración aprobadas: **85 en total** |
| Consistencia de modelo SQLite y SQL Server | Sin cambios pendientes de migración |
| Migraciones SQLite sobre base nueva | Ejecutadas por setup y pruebas relacionales |
| Script SQL Server idempotente | Generado en artifacts/sqlserver-migrations.sql; no aplicado a servidor |
| API real + receptor local | Arranque en 5080/5099, health y Swagger HTTP 200 |
| Flujo HTTP completo de la entrega inicial | 401 sin token, creación de producto/pedido, total servidor, 403 dueño ajeno, identidad ordinal y cancelación |
| Worker real de outbox | Cuatro eventos persistidos y entregados; ProcessedAt confirmado consultando el estado de la outbox |
| Receptor HTTP | 401 sin clave, fallo simulado503, nuevo202, duplicado200, payload diferente409; deduplicación tras reinicio |
| Resiliencia HTTP real con transporte de prueba | Retry 503/503/202 conserva EventId; circuito abierto deja de enviar; 400 no se reintenta |
| Generador de laboratorio | Generó otro directorio y su solución vacía compiló |
| Duración de guiones | 10 secuencias contiguas de240min; total2400min |
| Auditoría NuGet directa/transitiva | Ninguna vulnerabilidad reportada por las fuentes disponibles en esa consulta |

Los informes TRX y Cobertura están en `artifacts/tests/` (ignorado por Git). Unitarias e integración generan informes distintos; no sumar sus porcentajes ni presentarlos como una cobertura combinada. La cobertura no acredita corrección por sí sola.

Durante la construcción se detectaron y corrigieron problemas de validación MVC de records, acceso inicial a Swagger, comparación ordinal de clientes SQL Server, invalidación después del commit y dependencias con avisos de seguridad. Los tests correspondientes evitan varias de esas regresiones.

## Actualización: repositorio genérico

Se añadió `IRepository<T>` y su base EF Core `Repository<T>`, con puertos específicos para productos y pedidos. Los handlers que sólo necesitan operaciones comunes ya consumen la interfaz genérica. Los alias de DI conservan la misma instancia scoped y la carga de las líneas del pedido.

`dotnet test AulaPedidos.slnx --no-restore` aprobó **91 pruebas: 60 unitarias y 31 de integración**. Las seis nuevas pruebas relacionales cubren resolución por DI, carga completa del agregado, commit explícito con outbox, seguimiento/concurrencia, borrado lógico, lectura por lote y cancelación.

La comprobación de cambios pendientes de modelo pasó para SQLite y SQL Server. Este refactor no requiere una migración nueva.

## Migración de las utilidades a C#: 1 de octubre de 2026

Las ocho utilidades anteriores se sustituyeron por `tools/AulaPedidos.CourseTools`, una aplicación de consola C# sin paquetes externos. Se actualizaron CI, las instrucciones de Copilot, las guías, las peticiones HTTP y la presentación de 64 diapositivas.

| Comprobación nueva | Resultado observado |
|---|---|
| `verify --coverage` | Restauración bloqueada, compilación Release y **113 pruebas aprobadas: 82 unitarias y 31 de integración**, sin pruebas omitidas |
| Regresiones de CourseTools | 22 pruebas sobre firma y permisos JWT, identidad ordinal, vigencia, entradas inválidas y protección del destino del laboratorio |
| Consistencia de EF | Sin cambios pendientes para SQLite y SQL Server |
| `setup` en una copia aislada | Configuró User Secrets exclusivos, conservó las claves locales, compiló y creó la base SQLite con sus migraciones |
| `setup --secrets-only` repetido | Los archivos de credenciales conservaron exactamente sus valores |
| `token` | Admin y Student emitidos con identidad exacta; rol desconocido rechazado |
| `run-api`, `run-notifications` y `smoke` | Readiness, 401 sin token, altas 201, total 251, aislamiento y cancelación ajena 403, identidad ordinal, versión incorrecta 409 y cancelación correcta |
| Outbox y salida de procesos | Receptor configurado con dos fallos simulados; entrega posterior confirmada con `outbox-status`; salida de procesos hijos visible |
| `new-lab` | Esqueleto y herramienta C# compilables, UserSecretsId distintos, sin copiar claves ni aplicación resuelta; destinos existentes o internos rechazados |
| Preparación del esqueleto | `setup --skip-database` terminó correctamente en el laboratorio generado; la migración espera a que se implemente el módulo de datos |
| Presentación | Archivo validado y renderizado con 64 diapositivas; comandos .NET en la diapositiva 9 y fuentes de las notas actualizadas |

El ensayo HTTP usó una copia local con datos sintéticos y User Secrets independientes. Se detuvieron los procesos iniciados para la validación. No se ejecutó Docker ni un motor SQL Server real en esta actualización; esos límites continúan vigentes.

## Preparado, sin ejecución local

**Docker no está disponible en la terminal de este equipo.** No se ejecutaron aquí build/arranque de contenedores, migraciones contra un motor SQL Server real, ni exportación al dashboard de Aspire.

Se entregan Dockerfiles, Compose y un job de CI que ejecuta contenedores, espera readiness y prueba el flujo HTTP contra SQL Server. La primera publicación disparó el workflow, pero **GitHub impidió iniciar los jobs por un bloqueo de facturación de la cuenta**. Las migraciones SQL Server se generaron, inspeccionaron y comprobaron contra el modelo; eso no equivale a probar su ejecución en el proveedor real.

La configuración del harness se contrastó con documentación oficial; no se ejecutó una sesión de GitHub Copilot dentro del Visual Studio del usuario. El instructor debe realizar la comprobación de References/prompts/agentes indicada en [preparación](instructor/00-preparacion.md) en cada estación.

## Repetir comprobaciones

```console
dotnet run --project tools/AulaPedidos.CourseTools -- setup
dotnet run --project tools/AulaPedidos.CourseTools -- verify --coverage
# En otras terminales:
dotnet run --project tools/AulaPedidos.CourseTools -- run-notifications
dotnet run --project tools/AulaPedidos.CourseTools -- run-api
# Desde una tercera:
dotnet run --project tools/AulaPedidos.CourseTools -- smoke
dotnet run --project tools/AulaPedidos.CourseTools -- outbox-status
```

Para la parte pendiente, detener primero servicios locales y ejecutar Docker Desktop en modo contenedores Linux:

```console
docker compose --env-file .env config --quiet
docker compose --env-file .env up --build -d
dotnet run --project tools/AulaPedidos.CourseTools -- smoke
docker compose exec api dotnet AulaPedidos.Api.dll --outbox-status
docker compose down
```

No publicar claves ni convertir un ensayo de laptop en garantía de producción. Ver [límites operativos](docs/produccion.md).
