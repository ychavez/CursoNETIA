# Seguridad y límites del laboratorio

## Modelo de identidad

La API valida JWT. En el entorno local la herramienta C# del curso genera tokens de práctica con una clave aleatoria local preparada por `setup`; no existe una pantalla de login ni un endpoint que entregue roles a quien los solicite. Generar un token en la computadora del desarrollador no representa autenticación empresarial.

```console
dotnet run --project tools/AulaPedidos.CourseTools -- token --role Admin --subject instructor
dotnet run --project tools/AulaPedidos.CourseTools -- token --role Student --subject alumno
```

Usa los valores sólo en **Authorize** de Swagger o en una copia local no versionada de `requests/AulaPedidos.http`; no pegarlos en chats ni guardarlos en Git. `dotnet run --project tools/AulaPedidos.CourseTools -- run-api` activa el modo local. Fuera del aula se configura `Authority` y `Audience` de un proveedor OIDC real, con usuarios, altas/bajas, MFA y políticas de organización. Leer `Program.cs` y la configuración para los nombres exactos antes de desplegar; no resolver un fallo habilitando el modo demo en Production.

## Tres preguntas por petición

1. **¿Quién eres?** Firma, emisor, audiencia y vigencia del JWT; un payload decodificado no es un token validado.
2. **¿Qué puedes hacer?** Roles Admin/Student y permisos `catalog.write`, `orders.write`, `orders.read`.
3. **¿Sobre qué recurso?** El dueño del pedido debe ser la identidad autenticada; un permiso no permite leer identificadores ajenos.

| Caso | Evidencia requerida |
|---|---|
| Sin token en ruta protegida | 401 |
| Token válido sin permiso | 403 |
| Pedido de otro usuario | 403 en la referencia; evaluar 404 si la política exige ocultar existencia |
| Versión de recurso obsoleta | Conflicto, sin pérdida silenciosa de datos |
| Datos de entrada inválidos | Error documentado, sin stack trace |
| Token manipulado/expirado | 401, sin acceso parcial |

## Controles y amenazas

| Riesgo | Control del curso | Validación que realiza el estudiante |
|---|---|---|
| BOLA/IDOR | Consulta y mutación ligadas al dueño | Token B contra pedido de A |
| Sobreasignación | DTOs explícitos | Intentar introducir owner/total/rol en el body |
| Inyección | LINQ y parámetros | Texto con comillas se trata como valor |
| Agotamiento de recursos | Paginación, límites, cancelación y timeouts | Tamaño de página fuera de rango |
| Secretos expuestos | user-secrets/local excluido de Git, entorno en Docker | Inspeccionar diff y salida sanitizada |
| Datos sensibles en errores | ProblemDetails y logging controlado | Fallo inesperado devuelve detalle genérico |
| Dependencias vulnerables | Revisión de paquetes y automatización | Interpretar resultados, no ignorarlos automáticamente |
| Confianza en IA | Revisión de diff y pruebas adversas | Rechazar cambio que retire autorización |

OWASP es una guía para modelar riesgos; completar una tabla no es una certificación. Referencias de consulta: [OWASP API Security Top 10](https://owasp.org/API-Security/editions/2023/en/0x11-t10/) y [validación de JWT en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

## Secretos y datos

User-secrets evita agregar secretos al repositorio, pero no es una bóveda cifrada de producción. `.local/secrets.json` es material de laboratorio y debe mantenerse fuera del control de versiones y de directorios compartidos. `.env` tampoco debe versionarse. Producción requiere un gestor de secretos, rotación, permisos mínimos y trazabilidad de acceso. La auditoría de EF no sustituye protección de datos, retención ni borrado legal.

## Criterios que bloquean la entrega

Una clave en Git; token en un log; acceso a pedido ajeno; emisor/audiencia/firma desactivados; modo demo habilitado en Production; migración destructiva sin plan; prueba de seguridad borrada para lograr verde. Corregir y volver a ejecutar la evidencia antes de presentar la aplicación.
