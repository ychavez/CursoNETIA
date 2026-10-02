# Laboratorio de consultas y planes en SQL Server

Este ejercicio de 25 minutos produce evidencia de lecturas y del plan real antes/después de un índice. Usa datos sintéticos en una tabla temporal de la sesión; no modifica tablas de AulaPedidos. Después se contrasta el experimento con la consulta real del repositorio. Un resultado favorable aquí no equivale a un benchmark de la aplicación completa.

## Preparar conexión y captura (5 minutos)

1. Ejecutar `dotnet run --project tools/AulaPedidos.CourseTools -- setup` y, desde la raíz de referencia, `docker compose up -d sqlserver`. Esperar que `docker compose ps` muestre SQL Server saludable. Docker Desktop debe ejecutar contenedores Linux.
2. Abrir SQL Server Management Studio en la computadora del instructor. Conectar a **`localhost,14333`**, autenticación SQL Server, usuario `sa`, usando la clave local generada en `.local/secrets.json` (`SqlPassword`). Introducirla sin proyectar ni copiarla a Copilot. La opción de confiar en el certificado del servidor se permite aquí sólo para este contenedor local. En un servidor corporativo se usan identidad/permisos y certificados aprobados.
3. Abrir **Nueva consulta**. Mantener la misma pestaña/conexión durante todo el experimento: la tabla `#PedidosCurso` pertenece a esa sesión y desaparece al cerrarla.
4. Ejecutar el bloque de preparación que sigue. Luego activar **Consulta > Incluir plan de ejecución real** (`Ctrl+M`) antes de ejecutar las consultas de medición. El plan real aparece después de ejecutar; el plan estimado no contiene las mismas mediciones. [Documentación de planes reales](https://learn.microsoft.com/en-us/sql/relational-databases/performance/display-an-actual-execution-plan?view=sql-server-ver17).

## Preparar 50,000 pedidos sintéticos (4 minutos)

Ejecutar el bloque una sola vez en una pestaña nueva. Si se necesita empezar nuevamente, abrir otra pestaña; no borrar datos de la aplicación.

```sql
SET NOCOUNT ON;

CREATE TABLE #PedidosCurso
(
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    CustomerId nvarchar(100) COLLATE Latin1_General_100_BIN2 NOT NULL,
    CreatedAt datetimeoffset NOT NULL,
    Total decimal(18,2) NOT NULL,
    Detalle nvarchar(200) NOT NULL
);

-- Cinco dígitos generan suficientes números sin depender de tablas de usuarios.
;WITH Digito AS
(
    SELECT n FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) d(n)
), Numeros AS
(
    SELECT a.n + 10*b.n + 100*c.n + 1000*d.n + 10000*e.n AS n
    FROM Digito a CROSS JOIN Digito b CROSS JOIN Digito c
    CROSS JOIN Digito d CROSS JOIN Digito e
)
INSERT INTO #PedidosCurso (Id, CustomerId, CreatedAt, Total, Detalle)
SELECT NEWID(),
       CONCAT(N'cliente-', n % 500),
       DATEADD(SECOND, n, CONVERT(datetimeoffset, '2026-01-01T00:00:00+00:00')),
       CONVERT(decimal(18,2), 10 + (n % 10000) / 100.0),
       REPLICATE(N'x', 150)
FROM Numeros
WHERE n < 50000;

SELECT COUNT(*) AS FilasTotales,
       COUNT(DISTINCT CustomerId) AS Clientes
FROM #PedidosCurso;
```

Resultado esperado: **50,000 filas y 500 clientes**. `Id` tiene índice de clave primaria; todavía no existe uno para el filtro por cliente. Los identificadores son aleatorios, pero el número de filas y la distribución por cliente son constantes.

## Obtener la línea base (5 minutos)

Con plan real activado, ejecutar este bloque tres veces seleccionándolo en SSMS. Guardar la segunda y tercera ejecución en una tabla de evidencia. No incluir el tiempo de generar datos ni el de crear el índice. No limpiar cachés del servidor con comandos administrativos.

```sql
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT Id, CustomerId, CreatedAt, Total
FROM #PedidosCurso
WHERE CustomerId = N'cliente-42'
ORDER BY CreatedAt DESC, Id
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
OPTION (RECOMPILE);

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
```

Abrir **Mensajes** y registrar lecturas lógicas, CPU y tiempo transcurrido de ejecución. Las lecturas lógicas cuentan páginas consultadas en memoria; no son el número de filas devueltas. El plan permite comparar filas leídas/devueltas, ordenamientos y accesos elegidos por el optimizador. [Significado de STATISTICS IO](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-statistics-io-transact-sql?view=sql-server-ver17).

Qué decir: «Devolvemos veinte filas, pero eso no significa que el motor haya leído únicamente veinte. Vamos a mirar el trabajo que necesitó para encontrarlas y ordenarlas».

## Agregar un índice y medir la misma consulta (5 minutos)

```sql
CREATE INDEX IX_PedidosCurso_Cliente_Fecha_Id
ON #PedidosCurso (CustomerId, CreatedAt DESC, Id)
INCLUDE (Total);
```

Volver a ejecutar **exactamente** el bloque de medición anterior tres veces y guardar la segunda/tercera ejecución. Registrar también si cambió el operador de acceso y si desapareció el ordenamiento. Se espera menor trabajo para este filtro selectivo, pero el resultado a entregar es la observación real, no una cifra prefijada.

| Escenario | Filas retornadas | Lecturas lógicas | CPU/tiempo | Operadores relevantes |
|---|---:|---:|---|---|
| Sin índice por cliente, ejecución 2 | | | | |
| Sin índice por cliente, ejecución 3 | | | | |
| Con índice, ejecución 2 | | | | |
| Con índice, ejecución 3 | | | | |

Qué decir: «El índice aprovecha filtro y orden de este caso. También ocupa espacio y cuesta mantenerlo en INSERT/UPDATE. No debemos agregarlo a todas las tablas ni trasladarlo sin medir la consulta real».

`OPTION (RECOMPILE)` reduce la influencia de un plan previo en esta comparación didáctica; no es una recomendación de incorporarlo a cada query de producción. El plan puede cambiar por versión de SQL Server, estadísticas, recursos o distribución. Una lectura física cero puede ser resultado de la caché de páginas, no ausencia de trabajo.

## Contrastar con AulaPedidos (6 minutos)

1. Iniciar el Compose completo de referencia, ejecutar `dotnet run --project tools/AulaPedidos.CourseTools -- smoke` contra su API y abrir una pestaña SSMS en la base **AulaPedidos**. El smoke crea un pedido del sujeto `alumno`.
2. Consultar únicamente los datos sintéticos existentes. El siguiente SQL reproduce el filtro y la página principal; la query EF completa también carga las líneas y realiza una consulta de conteo.

```sql
USE AulaPedidos;
GO
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

DECLARE @CustomerId nvarchar(100) = N'alumno';
SELECT Id, CustomerId, CreatedAt, Total, Status
FROM dbo.Orders
WHERE CustomerId = @CustomerId
ORDER BY CreatedAt DESC, Id
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;

SELECT name, collation_name
FROM sys.columns
WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'CustomerId';

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
```

3. Abrir `src/AulaPedidos.Infrastructure/Persistence/Repositories.cs`, `ListByCustomerAsync`, y comparar filtro, orden, `Include`, paginación y `AsNoTracking`. Abrir `EntityConfigurations.cs` para identificar el índice existente. El índice temporal incluyó columnas distintas: no afirmar que ése es ya el índice de la aplicación.
4. Para inspeccionar SQL generado, colocar un breakpoint después de construir `query` en ese repositorio y evaluar `query.ToQueryString()` en el depurador de Visual Studio. Esto muestra ese punto del LINQ; añadir mentalmente o inspeccionar también el `Include`/`OrderBy`/`Skip`/`Take` aplicado después. La consulta que se mide debe corresponder a la operación completa que se quiere optimizar.
5. Abrir `CreateOrderHandler`: `GetByIdsAsync` carga en una consulta los productos de todas las líneas. Mostrar el método del repositorio y compararlo con un `GetByIdAsync` dentro de un bucle. No confundir este acceso de catálogo con la consulta de listado de pedidos.

**Cierre:** entregar captura sanitizada del plan antes/después, tabla de mediciones, hipótesis confirmada o descartada y una limitación. Si SQL Server no está disponible, ejecutar las pruebas relacionales y leer el SQL generado como contingencia; dejar la medición como pendiente. El material no incluye cifras de rendimiento inventadas ni afirma que se haya ejecutado SQL Server en esta computadora de autoría.
