# Clase 5 — EF Core, SQL y consistencia (240 minutos)

## Objetivo

Sustituir persistencia temporal por EF Core mediante `Repository<T>` y sus especializaciones; reproducir esquema con migraciones, relaciones y restricciones; comprender consultas, auditoría, soft delete y concurrencia. SQLite habilita trabajo local; SQL Server se comprueba durante esta sesión en una estación preparada y se retoma en Docker.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «Guardar en memoria era un adaptador temporal. Hoy la información debe sobrevivir al proceso.» | Crear producto, reiniciar lab y comprobar pérdida temporal antes de reemplazar adaptador. |
| 15–40 | «DbContext rastrea una unidad de trabajo. El repositorio genérico comparte consultas, y cada agregado define qué necesita cargar. Todos guardan con el mismo contexto scoped.» | Implementar Repository<T>, heredar ProductRepository/OrderRepository y cargar Items en Order. Mostrar aliases genérico/específico y EntityConfigurations; dibujar Product–OrderItem–Order. |
| 40–65 | «Code First expresa modelo; una migración versiona cambio de esquema. Leer SQL es parte de aplicarla.» | Mostrar contextos SQLite/SQLServer y carpetas de migraciones. Listar y generar script para revisión. |
| 65–90 | «Las restricciones de base protegen carreras que una validación previa no puede cerrar.» | Revisar índice único SKU, FK y precisión decimal. Demostrar SKU duplicado y conflicto concurrente. |
| 90–115 | «Soft delete es una política de lectura; no es borrado legal. La consulta genérica también debe respetar el filtro, aunque el contexto ya rastree el producto.» | Cargar mediante IRepository<Product>, ejecutar SoftDelete y SaveChanges; consultar de nuevo en el mismo contexto y verificar null. Revisar CreatedAt, UpdatedAt y Version. |
| 115–140 | «La carga por identidad admite mutaciones; la carga por lote sólo lee. Un pedido debe conservar sus líneas al usar cualquiera de las dos.» | Mostrar GetByIdAsync con seguimiento y GetByIdsAsync sin seguimiento; cargar un pedido desde contexto nuevo e inspeccionar Items. Revisar paginación, orden, Include/N+1 y SQL. |
| 140–165 | «Un índice ayuda una forma de acceso y cuesta al escribir. Mediremos con datos y plan, no con el número de índices.» | SQL Server: consultar pedidos por cliente y fecha, inspeccionar plan real/lecturas. Registrar entorno y tamaño; no inventar mejoras. |
| 165–210 | «Ahora implementen repositorio, migración y prueba relacional. Debe sobrevivir a reiniciar.» | Laboratorio L05. Crear datos sintéticos y probar conflicto entre dos contextos. |
| 210–230 | «SQLite y SQL Server comparten EF, pero no son la misma base. La traducción y concurrencia deben verificarse en el proveedor destino.» | Ejecutar smoke en SQL Server o mostrar evidencia del instructor si el equipo está bloqueado; dejar recuperación asignada. |
| 230–240 | «Pedido y outbox se guardarán juntos; la clase 8 estudiará su entrega.» | Ticket: transacción local vs llamada HTTP remota. |

El procedimiento de conexión, datos de prueba, plan y lecturas está en [medición SQL Server](../../docs/rendimiento-sql.md). Ensayarlo antes de clase.

## Demostración y comandos

Desde referencia, después de setup:

El bloque 15–40 se distribuye así: cinco minutos para seguir contrato → implementación → contexto, diez para `Repository<T>` y sus especializaciones, cinco para el registro DI y cinco para relaciones EF. Abrir la [guía del repositorio genérico](../../docs/repositorio-generico.md) y seguir esta secuencia:

1. Mostrar `GetByIdAsync`, `GetByIdsAsync` y `Add` en la implementación común. Decir: «La búsqueda individual deja el agregado listo para modificar; el lote sirve para lectura. Ninguna operación confirma la transacción por su cuenta».
2. Mostrar la consulta base extensible dentro de Infrastructure y `Include(order => order.Items)` en OrderRepository. Decir: «Cargar la raíz de un pedido también requiere sus líneas».
3. Resolver `IRepository<Order>` e `IOrderRepository` dentro del mismo scope y comprobar que apuntan a la misma instancia. Explicar por qué se registran aliases por agregado y cómo registrar uno nuevo.
4. Mostrar el contexto compartido con `IUnitOfWork`; trazar Add → SaveChanges → pedido y outbox. Mantener la entrega remota para clase 8.
5. En el bloque 90–115, ejecutar la prueba de borrado y consulta en el mismo contexto. En el bloque 115–140, cargar desde un contexto nuevo para que el tracking previo no oculte un Include faltante.

El repositorio genérico cambia contratos y reutiliza código; no modifica el modelo persistido ni exige una migración nueva sobre la referencia.

```powershell
dotnet tool restore
dotnet ef migrations list --project src/AulaPedidos.Infrastructure --startup-project src/AulaPedidos.Api --context SqliteAulaPedidosDbContext
dotnet ef migrations script --project src/AulaPedidos.Infrastructure --startup-project src/AulaPedidos.Api --context SqliteAulaPedidosDbContext --output .local/migracion-sqlite.sql
dotnet ef migrations script --idempotent --project src/AulaPedidos.Infrastructure --startup-project src/AulaPedidos.Api --context SqlServerAulaPedidosDbContext --output .local/migracion-sqlserver.sql
```

No ejecutar scripts sobre una base empresarial. Leer DDL, FKs e índices. `--idempotent` del ejemplo es para SQL Server; no asumir soporte idéntico en SQLite. Si la factoría necesita configuración, revisar `DesignTimeFactories.cs` y configuración de entorno del README antes de ejecutar.

En el laboratorio, generar una migración **sólo cuando el alumno ya creó su modelo y aún no copió migraciones de referencia**:

```powershell
dotnet ef migrations add InitialLab --project src/AulaPedidos.Infrastructure --startup-project src/AulaPedidos.Api --context SqliteAulaPedidosDbContext --output-dir Persistence/Migrations/Sqlite
```

No regenerar `InitialLab` sobre la referencia ni crear una segunda migración inicial sobre el mismo modelo. Para SQL Server, repetir con su contexto y directorio correspondiente. Cada migración se inspecciona antes de aplicar.

**Prompt:**

> Lee IRepository.cs, Repository.cs, Repositories.cs y EntityConfigurations.cs. Analiza carga por identidad/lote y listado paginado de pedidos de un cliente: orden, filtros, tracking, Include, índices y SQL. Comprueba que el registro de IRepository<Order> carga líneas. Propón una mejora medible y la prueba de que no incluye pedidos ajenos ni productos borrados cuando corresponda. Distingue la inferencia de una medición. No ejecutes DDL ni agregues caché.

## Laboratorio L05 y solución

**Consigna:** implementar `Repository<T>` con EF y repositorios especializados, aplicar migración en una base exclusiva de lab, crear y volver a leer producto tras reiniciar; comprobar soft delete y un conflicto de versiones. Inyectar `IRepository<Order>` desde un scope nuevo y verificar que carga las líneas. Registrar plan/SQL y comparación razonada de SQLite/SQL Server.

**Aceptación:** relaciones con claves correctas; SKU único incluso ante concurrencia; auditoría actualizada; queries paginadas; dominio sin EF; ningún dato compartido afectado. La interfaz genérica conserva filtros y líneas del agregado; Add espera a la unidad de trabajo y las mutaciones cargadas por identidad se persisten. El alumno explica qué garantiza SaveChanges y qué no garantiza una llamada posterior a un servicio externo.

**Solución:** contrastar `Infrastructure/Persistence/AulaPedidosDbContext.cs`, `EntityConfigurations.cs`, `Repository.cs`, `Repositories.cs`, `Infrastructure/DependencyInjection.cs`, `DesignTimeFactories.cs` y migraciones de cada proveedor. Las interfaces genérica/específica resuelven la misma instancia scoped. `Version` es un GUID configurado como concurrency token y se renueva al mutar; no llamarlo rowversion de SQL Server. `HasQueryFilter` excluye borrados y la búsqueda usa una consulta que aplica el filtro. SKU se conserva único aun después del borrado en la referencia: discutir si el negocio quiere permitir reutilizarlo antes de cambiar índice.

**Optimización orientativa:** proyección de campos necesarios y filtro/paginación en SQL; `AsNoTracking` en lectura; índice según filtro/order. La referencia puede cargar datos completos para mapear DTO: identificarlo como candidato medible, no afirmar que toda consulta ya está optimizada. Crear pedidos obtiene productos por lote mediante GetByIdsAsync: comprobar el SQL y demostrar que el número de consultas no crece por cada línea.

## Preguntas, fallos y recuperación

- **¿Migrations reemplaza backup?** No. DDL inverso no recupera filas eliminadas.
- **¿Soft delete oculta todo siempre?** Sólo donde se respeta filtro; `IgnoreQueryFilters` requiere justificación y autorización.
- **¿Por qué no usar FindAsync directamente?** Puede devolver una entidad ya rastreada sin ejecutar la consulta filtrada. La prueba de producto borrado en el mismo contexto hace visible esa diferencia.
- **¿Por qué no se registra IRepository de cualquier T automáticamente?** Cada agregado puede requerir navegaciones distintas. El registro explícito conserva la carga especializada y evita duplicar candidatos al resolver una colección de repositorios.
- **¿Por qué una transacción no incluye correo?** Un servidor HTTP externo no participa automáticamente en la transacción local.
- **¿Un test SQLite prueba el plan SQL Server?** No; requiere proveedor y datos adecuados.

“More than one DbContext”: especificar `--context`. “No such table”: revisar base/proveedor y aplicar migración correcta. “Database locked”: revisar scopes y procesos; no borrar archivo sin respaldo de trabajo. Error de conexión SQL: distinguir proceso, puerto, credenciales y readiness. Si el modelo se desalineó, guardar archivos y crear otra base local vacía; nunca resetear una base compartida. Recuperar el conjunto Persistence+dependencias desde referencia si es necesario y documentar ayuda.
