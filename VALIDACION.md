# Validación de la entrega

Fecha: 29 de septiembre de 2026. Entorno: Windows, PowerShell 7.4.20, SDK .NET 10.0.401, runtime 10.0.12.

## Ejecutado

| Comprobación | Resultado |
|---|---|
| Restauración con lockfiles | Correcta |
| Build de solución Debug y Release | Sin errores ni advertencias en la ejecución final |
| setup.ps1 | Claves locales, User Secrets, restauración, build y migración SQLite correctos |
| Entrega inicial: verify.ps1 -Coverage | 60 pruebas unitarias + 25 de integración aprobadas: **85 en total** |
| Consistencia de modelo SQLite y SQL Server | Sin cambios pendientes de migración |
| Migraciones SQLite sobre base nueva | Ejecutadas por setup y pruebas relacionales |
| Script SQL Server idempotente | Generado en artifacts/sqlserver-migrations.sql; no aplicado a servidor |
| API real + receptor local | Arranque en 5080/5099, health y Swagger HTTP 200 |
| smoke.ps1 | 401 sin token, creación de producto/pedido, total servidor, 403 dueño ajeno, identidad ordinal y cancelación |
| Worker real de outbox | Cuatro eventos persistidos y entregados; ProcessedAt confirmado mediante outbox-status.ps1 |
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

## Preparado, sin ejecución local

**Docker no está disponible en la terminal de este equipo.** No se ejecutaron aquí build/arranque de contenedores, migraciones contra un motor SQL Server real, ni exportación al dashboard de Aspire.

Se entregan Dockerfiles, Compose y un job de CI que ejecuta contenedores, espera readiness y prueba el flujo HTTP contra SQL Server. La primera publicación disparó el workflow, pero **GitHub impidió iniciar los jobs por un bloqueo de facturación de la cuenta**. Las migraciones SQL Server se generaron, inspeccionaron y comprobaron contra el modelo; eso no equivale a probar su ejecución en el proveedor real.

La configuración del harness se contrastó con documentación oficial; no se ejecutó una sesión de GitHub Copilot dentro del Visual Studio del usuario. El instructor debe realizar la comprobación de References/prompts/agentes indicada en [preparación](instructor/00-preparacion.md) en cada estación.

## Repetir comprobaciones

```powershell
./scripts/setup.ps1
./scripts/verify.ps1 -Coverage
# En otras terminales:
./scripts/run-notifications.ps1
./scripts/run-local.ps1
# Desde una tercera:
./scripts/smoke.ps1
./scripts/outbox-status.ps1
```

Para la parte pendiente, detener primero servicios locales y ejecutar Docker Desktop en modo contenedores Linux:

```powershell
docker compose --env-file .env config --quiet
docker compose --env-file .env up --build -d
./scripts/smoke.ps1
docker compose exec api dotnet AulaPedidos.Api.dll --outbox-status
docker compose down
```

No publicar claves ni convertir un ensayo de laptop en garantía de producción. Ver [límites operativos](docs/produccion.md).
