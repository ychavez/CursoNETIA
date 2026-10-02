# Clase 9 — Docker y observabilidad con OpenTelemetry (240 minutos)

## Objetivo

Construir contenedores multietapa, ejecutar SQL Server/API/receptor/dashboard con Compose, verificar red y persistencia y diagnosticar usando logs, métricas, trazas y health checks. Compose es un entorno de aula; el despliegue productivo exige decisiones adicionales.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «Que funcione en Visual Studio es un checkpoint. Ahora haremos explícitos runtime, configuración y dependencias.» | Dibujar servicios y puertos internos/host. |
| 15–40 | «Imagen es una plantilla; contenedor es una instancia; volumen conserva datos fuera de su capa desechable.» | Leer Dockerfile y .dockerignore. Comparar SDK de build con runtime final. |
| 40–65 | «Multietapa reduce herramientas en runtime; no garantiza una imagen segura por sí sola.» | Revisar restore/publish, usuario, copias y variables; construir imagen. |
| 65–90 | «Dentro de un contenedor localhost es ese contenedor. Compose resuelve servicios por nombre.» | Revisar `sqlserver`, `notifications`, `dashboard` y API 8080/host5080. Validar configuración sin imprimir secretos. |
| 90–115 | «La migración es una operación explícita previa. Arrancar la API no debe hacer DDL arbitrario en producción.» | Arrancar Compose; seguir sqlserver healthy → migrate completado → api. |
| 115–140 | «Logs cuentan eventos, métricas agregan señales y trazas conectan operaciones. Ninguna señal reemplaza a las otras.» | Generar pedido y localizar log estructurado, contador y traza en dashboard. |
| 140–165 | «OpenTelemetry instrumenta y exporta; el dashboard almacena/muestra en este entorno. Sin exporter configurado no aparecen datos allí.» | Abrir CourseTelemetry y endpoint OTLP; seguir spans ASP.NET/HttpClient y outbox. |
| 165–205 | «Cada pareja debe diagnosticar una falla con hipótesis y evidencia, preservando los datos.» | Laboratorio L09: reinicio con volumen, receptor caído o readiness DB; recuperar. |
| 205–225 | «Liveness pregunta si el proceso vive; readiness si puede atender. Reiniciar por cada fallo externo puede empeorar un incidente.» | Demostrar live/ready; discutir alertas, PII, cardinalidad y retención. |
| 225–240 | «Conservaremos esta evidencia para la revisión final y separaremos lo observado de lo supuesto.» | Guardar capturas sanitizadas y runbook de diagnóstico. |

## Demostración y comandos

Detener API/receptor locales antes de reutilizar puertos. Desde la raíz referencia ya preparada:

```console
docker compose --env-file .env config --quiet
docker compose --env-file .env up --build -d
docker compose --env-file .env ps
docker compose --env-file .env logs --tail 50 api
dotnet run --project tools/AulaPedidos.CourseTools -- smoke
```

Ejecutar las peticiones de `/health/live` y `/health/ready` en `requests/AulaPedidos.http` e interpretar sus respuestas. No ejecutar `docker compose config` sin `--quiet` en la proyección: puede expandir secretos. Abrir dashboard en `http://localhost:18888`; la configuración anónima está limitada a loopback para el aula. SQL Server publica host14333 hacia servicio1433; la API usa nombre `sqlserver`, no ese puerto de host.

**Persistencia:** crear recurso, guardar id, ejecutar `docker compose --env-file .env restart api` y consultarlo otra vez. Luego reiniciar SQL Server, esperar readiness y comprobar que volumen conserva datos. `down` sin `-v` preserva volúmenes; `down -v` los elimina y no se usa durante el ensayo con evidencia.

**Observabilidad:** en dashboard elegir servicio AulaPedidos.Api. Crear al menos un pedido nuevo; localizar contador `aulapedidos.orders.created`, span `orders.create` y log de id/partidas. Una métrica puede tardar un intervalo de exportación: comprobar configuración antes de esperar sin hipótesis. El worker genera spans `outbox.dispatch` y `outbox.publish`; la notificación produce una llamada HTTP distribuida. La referencia no persiste contexto de traza de creación dentro del mensaje outbox: no afirmar que ambas operaciones asíncronas forman automáticamente una sola traza continua. Relacionarlas mediante EventId/OrderId y proponer propagación controlada como extensión.

**Prompt:**

> La API responde pero no vemos trazas en el dashboard. Con compose.yaml, CourseTelemetry y logs sanitizados, separa hipótesis de red, exportador, protocolo y tráfico inexistente. Da una comprobación para cada una. No desactives seguridad ni cambies múltiples variables a la vez. Tras confirmar causa, propón el cambio mínimo.

## Laboratorio L09 y solución

**Consigna:** escribir Dockerfile multietapa y Compose de lab siguiendo la referencia; configurar variables externas, volumen y dependencias; demostrar recurso persistente, salud y tres señales. Provocar un fallo de receptor o DB de lab y escribir un runbook de recuperación.

**Aceptación:** build desde fuente; runtime separado; secretos fuera de imagen/Git; servicios por DNS Compose; migración explícita; datos sobreviven reinicio; estado health interpretado; log/traza/métrica reales y sanitizados. Si Docker falta en una laptop, realizar en pareja sobre estación habilitada y cada participante explica una señal; registrar la limitación local.

**Solución:** `Dockerfile`, `.dockerignore`, `compose.yaml`, Dockerfile del receptor, `Api/Operations/CourseTelemetry.cs`, `DatabaseHealthCheck.cs` y `Program.cs`. Configurar `OTEL_EXPORTER_OTLP_ENDPOINT` dentro de red Compose como `http://dashboard:18889`. Desde host, publicación4317 apunta a OTLP gRPC18889. No confundir UI18888 con endpoint de ingestión.

**Extensión:** diseñar alerta sobre crecimiento de outbox pendiente y tasa de errores; evitar dimensiones de alta cardinalidad como OrderId en métricas. Las trazas/logs sí pueden contener identificadores técnicos revisados según política.

## Preguntas y recuperación

- **¿Docker hace productivo al sistema?** Empaqueta ejecución; no decide identidad, backups, alta disponibilidad ni operación.
- **¿Health healthy prueba todas las funciones?** Sólo los checks implementados; smoke valida otro alcance.
- **¿Un log sin formato basta para correlacionar?** Puede ayudar, pero campos estructurados y contexto facilitan buscar y relacionar.
- **¿La ausencia de trazas prueba ausencia de tráfico?** También puede fallar exportación/muestreo/backend.

API no inicia: revisar primero migrate y SQL health. Puerto ocupado: detener proceso correcto, no todos los contenedores. Dashboard vacío: comprobar exporter/protocolo/nombre de servicio y generar tráfico. SQL no acepta conexión: esperar health y revisar configuración sin imprimir contraseña. Nunca resolver reinicio borrando volúmenes del compañero. Si Docker no puede ejecutarse por política, conservar Dockerfile/Compose validado y recuperar ejecución en host autorizado.
