# Clase 4 — Contratos HTTP y APIs mantenibles (240 minutos)

## Objetivo y entrada

Entrada: casos de uso, mediador y dobles de persistencia. Salida: API local con contratos explícitos, versión en ruta, respuestas coherentes y OpenAPI. La referencia completa está protegida; el laboratorio inicial de esta clase se ejecuta sólo en loopback con adaptador temporal en memoria y recibe seguridad en clase 6. Nunca publicarlo en ese estado.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «El caso de uso no conoce HTTP. La API adapta solicitudes y resultados sin repetir la regla.» | Pedir a un alumno localizar la regla de precio y dibujar el límite HTTP. |
| 15–40 | «REST comunica intención mediante recursos, métodos, estados y enlaces. Un 200 con un error escondido complica al cliente.» | Clasificar GET, POST, PUT, DELETE; 201+Location, 204, 400, 401, 403, 404 y 409. |
| 40–65 | «El contrato público cambia más lentamente que una entidad interna. Un DTO controla qué entra y qué sale.» | Abrir ProductInput/UpdateProductInput y ProductDto. Señalar que el cliente no asigna CreatedAt ni Total. |
| 65–95 | «Validez de JSON, forma del DTO y regla de negocio son controles distintos.» | Implementar controlador de productos y DataAnnotations. Provocar JSON mal formado y precio fuera de rango. |
| 95–120 | «Una excepción interna no debe mostrar stack trace al cliente. El operador sí necesita correlación.» | Revisar ResultExtensions y ApiExceptionHandler; traducir errores conocidos a ProblemDetails. |
| 120–145 | «Versionar es mantener contratos. Escribir v1 en la ruta no resuelve compatibilidad automáticamente.» | Mostrar `/api/v1`, OpenAPI y Swagger. Diseñar en papel un cambio incompatible que exigiría v2. |
| 145–165 | «CancellationToken permite abandonar trabajo cuando ya no se necesita la respuesta.» | Seguir token desde controller a mediator/repository. Mostrar por qué `.Result` bloquea. |
| 165–210 | «Implementen una pareja lectura/escritura y demuestren un fallo que el cliente pueda interpretar.» | Laboratorio L04 con pruebas HTTP. Revisar esquema real generado. |
| 210–230 | «La documentación automática sólo describe lo que configuramos y puede omitir errores.» | Copilot documenta rutas desde código; comparar con llamadas reales y corregir omisiones. |
| 230–240 | «Nuestro contrato ya puede probarse desde fuera. Mañana cambiará el adaptador de datos.» | Ticket: estado para recurso ausente, versión obsoleta y error inesperado. |

## Demostración con referencia

1. Iniciar API de referencia con `scripts/run-local.ps1` y notificador cuando se requieran pedidos.
2. Generar token local y guardarlo sólo en variable de sesión.
3. Crear producto, leer respuesta y header Location; consultar id recibido.
4. Intentar precio 0 y comprobar 400; buscar GUID inexistente y comprobar 404.
5. Abrir `src/AulaPedidos.Api/Controllers/ProductsController.cs` y seguir llamada al mediador.
6. Mostrar `Operations/ApiExceptionHandler.cs`: fallo inesperado sanitizado, validación/conflicto diferenciados.
7. Inspeccionar `/openapi/v1.json` y Swagger; comparar DTO real y estados. La documentación puede necesitar metadata adicional: es un punto de revisión, no prueba de completitud.

```powershell
$adminToken = .\scripts\token.ps1 -Role Admin -Subject instructor
$headers = @{ Authorization = "Bearer $adminToken" }
$inputJson = @{ sku = 'CURSO-API'; name = 'Producto del aula'; price = 120.50 } | ConvertTo-Json
$product = Invoke-RestMethod http://localhost:5080/api/v1/products -Method Post -Headers $headers -ContentType 'application/json' -Body $inputJson
Invoke-RestMethod "http://localhost:5080/api/v1/products/$($product.id)" -Headers $headers
```

Cambiar SKU si ya existe; no limpiar la base para ocultar el conflicto. PowerShell lanza excepción ante estados 4xx: inspeccionar respuesta o usar archivo HTTP/Swagger para ver código y body.

**Prompt:**

> Revisa ProductsController y ResultExtensions. Sin modificar handlers de negocio, documenta una tabla de endpoints con request, status, respuesta y autenticación. Después identifica metadata OpenAPI ausente comparando con el código. No inventes un endpoint de login. Propón una prueba HTTP para 400, 404 y 409.

## Laboratorio L04 y solución

**Consigna:** en laboratorio añadir controllers de catálogo y consulta/creación de pedidos, registros del mediador, manejo de errores y OpenAPI. Usar el repositorio de memoria temporal documentado en el recorrido hasta clase 5. Probar un camino válido, JSON inválido y versión obsoleta de recurso.

**Aceptación:** dominio fuera del controller; DTO limita entrada; POST devuelve 201 con ubicación; DELETE válido devuelve 204; errores conocidos conservan semántica; la ruta v1 es explícita. No exigir un `CustomerId` confiable del body: en esta fase se utiliza una identidad de aula fija sólo en lab y se elimina en clase 6.

**Solución:** `Api/Controllers/ProductsController.cs`, `OrdersController.cs`, `ResultExtensions.cs`, `Operations/ApiExceptionHandler.cs` y registros de `Program.cs`. Versión se transporta en DTO para PUT/cancel y query en DELETE. La referencia obtiene CustomerId del claim `sub`, que se explica en clase 6. `/api/v2` no existe: proponer una estrategia en ADR sin afirmar que ya está implementada.

**Extensión:** añadir metadata de respuestas faltante y verificar que el OpenAPI efectivamente la refleja. Un comentario XML sin configuración puede no cambiar el documento.

## Preguntas y recuperación

- **¿Swagger y OpenAPI son lo mismo?** OpenAPI describe el contrato; Swagger UI permite explorarlo de forma interactiva.
- **¿Por qué 409?** Hay un conflicto con el estado actual, como una versión obsoleta o SKU reservado.
- **¿Un GUID inválido siempre produce 400?** Un constraint de ruta puede no emparejar y devolver 404; comprobar contrato real.
- **¿Puedo devolver entidades EF?** Aumenta acoplamiento, sobreexposición y problemas de serialización; usar DTO explícito.

404 inesperado: revisar ruta y constraint antes de cambiar handler. 401 en referencia: generar token con scripts y revisar entorno; no quitar `[Authorize]`. 500 por DI: registrar el adaptador/handler pendiente. Puerto ocupado: detener la otra instancia. Si el lab no queda integrado, recuperar controllers y tipos de errores desde referencia junto con sus dependencias, marcando seguridad como pendiente hasta clase 6; usar referencia para demostración externa.
