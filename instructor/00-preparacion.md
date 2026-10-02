# Preparación de la edición y ensayo técnico

## 48 horas antes

Solicitar Windows con permisos para instalar, Visual Studio 2026 actualizado con carga **ASP.NET y desarrollo web**, .NET 10 SDK compatible con `global.json`, Git y GitHub Copilot habilitado por cuenta/organización. Las herramientas del curso están escritas en C# y se ejecutan con `dotnet`. Para clases 5 y 9: Docker Desktop en modo contenedores Linux/WSL2 y recursos suficientes para SQL Server; reservar al menos 16 GB de RAM en el equipo es una recomendación práctica, no un requisito oficial universal. Validar licencias corporativas de cada producto con el área responsable.

No depender de un trial ni de un modelo específico. Cada participante debe enviar sólo confirmación de acceso; nunca su contraseña o token. Descargar/restaurar antes del aula cuando la red corporativa tenga proxy. Preparar equipos en parejas y una estación demostrativa con Docker funcional.

## Inspección de la referencia

Abrir una terminal en la raíz del material y revisar `global.json` en el editor:

```console
dotnet --info
git --version
docker version
dotnet run --project tools/AulaPedidos.CourseTools -- setup
dotnet run --project tools/AulaPedidos.CourseTools -- verify
```

Si Docker no está disponible, registrar ese bloqueo: las clases iniciales usan SQLite, pero la evidencia de contenedores se recupera en una estación habilitada. Si la política impide restaurar paquetes o ejecutar las herramientas, solicitar la configuración autorizada por TI.

Abrir `AulaPedidos.slnx` con Visual Studio. Verificar SDK objetivo y proyecto Api como inicio. La solución referencia debe compilar antes de preparar el laboratorio. Ejecutar en terminal separada:

```console
dotnet run --project tools/AulaPedidos.CourseTools -- run-notifications
```

En otra terminal:

```console
dotnet run --project tools/AulaPedidos.CourseTools -- run-api
```

En una tercera:

```console
dotnet run --project tools/AulaPedidos.CourseTools -- smoke
```

Abrir las peticiones `/health/live` y `/health/ready` de `requests/AulaPedidos.http` y comprobar sus respuestas. Revisar `http://localhost:5080/swagger` y el contrato `http://localhost:5080/openapi/v1.json` en Development. El smoke modifica sólo datos de demostración: usar una base local exclusiva. Anotar el resultado real y la fecha; una ejecución previa no prueba el estado de otra computadora.

## Preparar el laboratorio incremental

```console
dotnet run --project tools/AulaPedidos.CourseTools -- new-lab --destination ../AulaPedidos-lab
cd ../AulaPedidos-lab
dotnet build AulaPedidos.slnx
```

El directorio destino debe ser nuevo. Abrir su solución en una segunda instancia de Visual Studio y etiquetar las ventanas como **REFERENCIA** y **LABORATORIO**. No iniciar dos APIs en el mismo puerto. `new-lab` genera el esqueleto e incluye CourseTools; no copia toda la implementación final. Leer [recorrido](recorrido-laboratorio.md) antes de distribuir.

## Ensayo de Copilot, 15 minutos

Seguir [harness](../docs/harness-copilot.md). Confirmar acceso a Chat, instrucciones visibles en References, prompt reutilizable y agente de revisión cuando la versión lo soporte. Registrar versión exacta de VS, modelo elegido y política de herramientas. Una referencia detectada no garantiza obediencia: pedir una modificación que contradiga una frontera en modo de análisis y comprobar que la respuesta la identifica.

## Ensayo de Docker y telemetría

Consultar README y `.env.example`; preparar `.env` local con secretos de laboratorio sin versionarlo. Ejecutar `docker compose --env-file .env config --quiet` para validar sin imprimir configuración resuelta con secretos. Construir y arrancar `docker compose --env-file .env up --build -d`. Verificar servicios, health checks y el dashboard local. No abrir el dashboard anónimo a la red; sólo loopback. Guardar capturas sanitizadas como contingencia de demostración, sin presentarlas como ejecución actual.

## Lista del día anterior

- [ ] Referencia compilada y tests ejecutados; resultado registrado.
- [ ] Nuevo laboratorio arranca vacío y se reconoce su alcance.
- [ ] Acceso Copilot y referencias de instrucciones verificados.
- [ ] Dos identidades de aula y escenario de acceso ajeno ensayados.
- [ ] Pedido con notificador detenido deja evidencia outbox y se recupera.
- [ ] Compose y puertos libres; volumen con datos sintéticos.
- [ ] Logs, traza y métrica identificables en backend local.
- [ ] Material offline y contingencia de red disponibles.
- [ ] Lista de asistentes/diagnóstico y parejas preparada.
- [ ] No hay credenciales proyectadas ni datos de clientes.

## Inicio de cada sesión, fuera de los 240 minutos

Abrir el checkpoint propio de la sesión anterior, verificar compilación y detener procesos sobrantes. Mostrar agenda y criterio de salida. Repetir sólo checks afectados por cambios de entorno; no consumir media clase reinstalando herramientas.
