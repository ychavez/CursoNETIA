# Clase 6 — Identidad, autorización y seguridad (240 minutos)

## Objetivo

Proteger contratos y recursos con JWT, roles y permisos; eliminar identidad temporal del lab; demostrar rechazo de acceso ajeno, entradas peligrosas y secretos en lugares incorrectos. El token de aula se genera con la herramienta C# local y no constituye un login productivo.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «Nuestra API funciona; todavía debemos demostrar quién puede hacer cada operación.» | Dibujar tabla de actores y recursos. Abrir docs/seguridad.md. |
| 15–40 | «Autenticar identifica; autorizar permite acciones; comprobar propiedad limita el recurso. Son tres controles distintos.» | Mostrar token validado, política y comparación de dueño en handler. |
| 40–65 | «JWT firmado no significa contenido secreto. Decodificar no valida firma, audiencia ni vigencia.» | Explicar header/payload/firma con valores ficticios, configuración de emisor/audiencia y expiración. No enviar tokens a sitios externos. |
| 65–90 | «La herramienta C# usa una clave de aula fuera del repositorio. En producción el emisor será un proveedor OIDC.» | Revisar AuthenticationExtensions y el comando token de CourseTools sin proyectar valores. Mostrar rechazo de modo demo fuera de Development. |
| 90–115 | «El permiso orders.read no concede leer cualquier GUID. Un identificador difícil de adivinar no es un control de acceso.» | Crear pedido de A, consultar/cancelar con B y comprobar 403. Admin tampoco se convierte en dueño automáticamente. |
| 115–140 | «Un body puede traer campos que no esperamos. Sólo usamos el DTO y la identidad del token.» | Mostrar sobreasignación, validación y errores sanitizados; probar cantidad inválida y CustomerId ajeno en JSON sin efecto de identidad. |
| 140–165 | «OWASP ayuda a ordenar amenazas; no es una casilla de cumplimiento que se aprueba por usar JWT.» | Taller amenazas: BOLA, inyección, recursos ilimitados, configuración, dependencias y servicios externos. |
| 165–210 | «Cada pareja deberá producir una prueba que impediría volver a introducir un acceso indebido.» | Laboratorio L06 con dos sujetos, permiso insuficiente y token ausente. Copilot propone review; alumno confirma cada hallazgo. |
| 210–230 | «La revisión de paquetes y secretos forma parte del ciclo. No silenciamos un aviso para conseguir verde.» | Mostrar auditoría de dependencias, `.gitignore` y diff; señalar que se seleccionaron versiones corregidas en referencia. |
| 230–240 | «Seguridad exige evidencia y límites. Escriban un riesgo que siga pendiente antes de producción.» | Ticket: diferencia 401/403, dueño y emisor. |

## Demostración reproducible

1. Iniciar referencia con secretos locales preparados por `setup` y `dotnet run --project tools/AulaPedidos.CourseTools -- run-api`.
2. Token A y B con distintos `Subject`, ambos Student; token Admin para crear catálogo.
3. Crear producto con Admin y pedido con A usando el id real del producto.
4. Consultar ese pedido con B. La implementación actual devuelve **403**; recurso inexistente devuelve **404**. Una política de ocultar existencia podría uniformar 404, pero exige cambiar contrato/tests conscientemente.
5. Intentar escritura de catálogo como Student y verificar 403; quitar header y verificar 401.
6. Mostrar que CustomerId del comando sale de `User.FindFirst("sub")` y no de `CreateOrderInput`.
7. Revisar logs: no deben incluir token ni secreto.

```console
dotnet run --project tools/AulaPedidos.CourseTools -- token --role Student --subject alumno-a
dotnet run --project tools/AulaPedidos.CourseTools -- token --role Student --subject alumno-b
```

Trabajar en una copia local no versionada de `requests/AulaPedidos.http`. Colocar el token de `alumno-a` en `studentToken` y el de `alumno-b` en `otherStudentToken`. Crear primero un producto como Admin y copiar su `id` a `productId`; después crear un pedido como `alumno-a` y copiar su `id` a `orderId`. Ejecutar **Otro subject: 403**: la petición usa `otherStudentToken` y debe rechazar el acceso. En Swagger se puede repetir cambiando el token de **Authorize** antes de consultar el mismo pedido. No proyectar tokens ni guardar la copia local en Git.

**Prompt:**

> Revisa OrdersController, OrderHandlers y AuthenticationExtensions. Busca una forma de que alumno-b lea o cancele el pedido de alumno-a. No cambies código. Para cada hallazgo distingue confirmado e hipotético y propone test HTTP. Verifica de dónde sale CustomerId y qué claims valida la política. No solicites secretos.

## Laboratorio L06 y solución

**Consigna:** agregar autenticación/políticas reales al lab, retirar el sujeto fijo de clase 4 y probar la matriz 401, 403 por permiso, 403 por dueño, 404 por ausencia y éxito propio. Registrar amenaza, control, prueba y riesgo pendiente.

**Aceptación:** configuración sensible externa; ningún endpoint entrega tokens de roles arbitrarios; firma/emisor/audiencia/vigencia activados; propietario comprobado en caso de uso; entrada no define identidad. No basta probar que Admin puede crear un producto.

**Solución:** `Api/Security/AuthenticationExtensions.cs`, atributos de `Controllers/`, `Application/Orders/OrderHandlers.cs`, `dotnet run --project tools/AulaPedidos.CourseTools -- token` y tests de integración. Orden del pipeline: autenticación antes de autorización; rate limiting por sujeto requiere identidad disponible. Las escrituras de catálogo combinan rol Admin y permiso; pedidos mantienen propiedad.

**Extensión:** preparar token de pruebas con permiso específico faltante dentro del host de tests para demostrar composición de políticas. No modificar la herramienta para regalar permisos en un entorno compartido. Discutir CORS, CSRF y TLS según tipo de cliente sin habilitar `AllowAnyOrigin` por conveniencia.

## Preguntas y recuperación

- **¿Admin debe leer todo?** Sólo si el requisito lo autoriza. En esta referencia conserva frontera de dueño.
- **¿JWT cifra el payload?** No necesariamente; no incluir secretos o datos personales innecesarios.
- **¿User-secrets es una bóveda?** No; es comodidad local fuera del repositorio.
- **¿403 significa token inválido?** Habitualmente token válido sin autorización; inválido/ausente produce401.

401 inesperado: revisar entorno, reloj, emisor/audiencia y regenerar token; no desactivar validaciones. 403 con token válido: comprobar claims y sujeto, luego política. Secretos en diff: retirar, rotar si se expusieron y revisar historial según proceso de empresa. Tests que sólo usan una identidad: añadir B antes de aceptar. Si falla Copilot, usar matriz manual y peer review con mismos controles.
