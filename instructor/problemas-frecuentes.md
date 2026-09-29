# Resolución de problemas para el profesor

Seguir **síntoma → hipótesis → comprobación → cambio mínimo → nueva evidencia**. Copilot puede ayudar a ordenar hipótesis después de sanitizar logs. No cambiar varias variables a la vez ni silenciar validaciones para continuar.

| Síntoma | Comprobar primero | Recuperación y criterio de cierre |
|---|---|---|
| Script no corre en Windows PowerShell | `$PSVersionTable.PSVersion`, usar `pwsh`7.4+ | Abrir terminal correcta y repetir script; no bajar política corporativa globalmente |
| SDK no coincide | `global.json`, `dotnet --list-sdks` | Instalar SDK compatible/aprobado; build exitoso |
| Restore locked falla | Cambio de paquetes, lockfile, red/proxy | Distinguir edición intencional vs entorno; actualizar lock sólo con revisión |
| Copilot no ve instrucciones | Raíz, extensión, opción VS, References | Adjuntar explícitamente/abrir nuevo Chat; comprobar respuesta y archivos |
| Agente no aparece | VS≥18.4 y frontmatter | Usar prompt de rol mientras se actualiza entorno autorizado |
| Puerto5080/5099 ocupado | API/receptor local y Compose simultáneos | Detener proceso identificado; no matar procesos de otros proyectos |
| 401 API | Entorno, firma/emisor/audiencia/expiración, token actual | Repetir setup/token del mismo proyecto; no desactivar validación |
| 403 esperado como sorpresa | Rol, permiso y sujeto propietario | Usar identidad correcta o corregir requisito; test negativo permanece |
| SKU duplicado | Datos previos y soft delete | Crear SKU nuevo o demostrar409; no borrar DB por rutina |
| Error “no such table” | Archivo DB efectivo y migración del proveedor | Aplicar esquema a base de lab correcta; repetir consulta |
| Más de un DbContext | Contexto CLI omitido | Especificar SqliteAulaPedidosDbContext o SqlServerAulaPedidosDbContext |
| Pending model changes | Snapshot vs modelo actual | Generar/revisar migración por proveedor; no suprimir warning |
| Concurrency conflict | Version que envió cliente y estado actual | Recargar, revisar intención y reenviar con versión actual; no sobrescribir silenciosamente |
| Tests pasan pero bug existe | Escenario/assert/doble usado | Agregar test observable; demostrar rojo antes de reparación |
| Tests intermitentes | Estado compartido, clock, red, orden | Aislar, usar tiempo controlable y límites; no añadir sleep al azar |
| Worker sin notificaciones | Enabled, receptor, clave, NextAttemptAt, dead-letter | Reparar causa y observar ProcessedAt; no borrar pendientes |
| Circuito no abre | Ventana, muestras mínimas, ratio, política | Generar ensayo controlado o test/configuración; no afirmar apertura sin evidencia |
| Producto desactualizado | TTL/invalidez y procesos distintos | Revisar invalidación/Version, documentar caché local y race |
| SQL Server contenedor unhealthy | Docker Linux, memoria, contraseña/config y logs sanitizados | Esperar readiness real y corregir causa; conservar volumen |
| API Compose no arranca | Servicio migrate terminó con0 | Revisar SQL readiness y DDL, no saltar dependencia |
| Dashboard sin datos | OTLP/protocolo, servicio y tráfico | Configurar exporter correcto, generar petición y localizar señal |
| Health ready falla, live pasa | DB/conexión; semántica distinta | Restaurar dependencia; no usar live como sustituto de ready |
| Evidencia contiene secreto | Alcance de exposición | Retirar material, rotar secreto si se expuso y seguir proceso del equipo |

## Cuando una demostración no funciona en cinco minutos

Nombrar el bloqueo y la hipótesis actual. Conservar error sanitizado para ejercicio de debugging. Pasar a la estación de referencia o test determinista del mismo comportamiento y asignar recuperación concreta. No afirmar que funcionó ni consumir toda la clase reinstalando herramientas. Una demostración fallida bien diagnosticada puede enseñar, pero el objetivo pendiente debe recuperarse.

## Puesta a cero segura

No hay un “borrar todo” recomendado. Detener procesos del laboratorio, guardar fuente/diff/evidencia, elegir un directorio/base nuevos y repetir configuración allí. El script New-LabWorkspace rechaza destinos existentes por esta razón. Eliminar volúmenes o bases sólo cuando se sabe que son descartables y se ha preservado el trabajo necesario.
