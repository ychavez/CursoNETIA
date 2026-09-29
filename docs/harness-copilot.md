# Harness de trabajo con GitHub Copilot

En este curso, harness significa el conjunto de contexto, instrucciones, herramientas, límites, pruebas y evidencia que rodean al modelo. No es una biblioteca que garantice respuestas correctas. Su salida útil es un cambio pequeño que otra persona puede revisar y comprobar.

## Componentes incluidos

| Componente | Archivo o lugar | Propósito |
|---|---|---|
| Contrato común | `.github/copilot-instructions.md` | Capas, seguridad, ciclo de trabajo y comandos |
| Contexto por capa | `.github/instructions/*.instructions.md` | Reglas aplicadas según `applyTo` |
| Tareas reutilizables | `.github/prompts/*.prompt.md` | Arquitectura, features, refactor, tests, debugging, SQL, seguridad, revisión y docs |
| Roles de revisión | `.github/agents/*.agent.md` | Arquitecto y revisor con herramientas de lectura |
| Decisiones | `docs/adr/` | Motivo, alternativas y consecuencias |
| Guardas ejecutables | `tests/`, `scripts/verify.ps1` y CI | Detectar regresiones verificables |
| Evidencia humana | `instructor/plantillas/bitacora-ia.md` | Qué se pidió, rechazó, cambió y probó |

## Activar y comprobar en Visual Studio

Con Visual Studio 2026 actualizado, abrir la solución y Copilot Chat usando una cuenta habilitada. En Opciones buscar GitHub > Copilot y activar las instrucciones personalizadas. Los archivos generales y los específicos usados deben aparecer en References. Los prompts viven en `.github/prompts`, se adjuntan mediante `#prompt:` o el selector de contexto; las versiones actuales también los muestran al escribir `/`. Si el selector no los descubre, adjuntar el archivo manualmente y comprobar la referencia. [Contexto de Copilot en Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-chat-context?view=visualstudio).

Los agentes personalizados en `.github/agents/*.agent.md` requieren Visual Studio 18.4 o posterior; la creación desde el botón `+` se documenta desde 18.5. Este proyecto los define manualmente y omite un modelo fijo. Verificar nombre y herramientas en el selector antes de usarlos. Con versiones anteriores, adjuntar las mismas instrucciones a Chat como contexto. [Agentes de Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-specialized-agents?view=visualstudio).

Agent Mode puede editar archivos y ejecutar herramientas; se documenta desde Visual Studio 17.14. Revisar cada comando y el diff antes de conservar cambios. Las herramientas MCP agregadas requieren habilitación. Este curso no necesita MCP: los archivos locales y terminal bastan. [Agent Mode](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-agent-mode?view=visualstudio).

Fecha de consulta: 29 de septiembre de 2026. Revisar estas páginas antes de cada edición del curso. La disponibilidad también depende del plan, políticas de organización y despliegue de funciones. No extrapolar configuraciones de VS Code (`settings.json`, extensiones o APIs de herramientas) a Visual Studio. La semántica exacta de permisos se verifica en la instalación del aula.

## Prueba de funcionamiento del harness

1. Abrir un archivo Domain y adjuntarlo a Chat.
2. Escribir: «Resume las reglas que te aplican; di qué archivos consultaste. ¿Puede esta entidad usar DbContext? No edites».
3. Revisar References: debe verse la instrucción general y, cuando corresponda, la de Domain.
4. La respuesta correcta rechaza esa dependencia por su frontera, no porque “EF sea malo”. Si inventa archivos, abrir un chat nuevo con contexto explícito.
5. Ejecutar el prompt `revision` con una modificación pequeña de laboratorio; confirmar que propone hallazgos sin mutar archivos.
6. Abrir el agente Arquitecto y comprobar herramientas de lectura. No basta con que su texto prometa no escribir; inspeccionar permisos reales.

## Plantilla mental de un buen prompt

**Objetivo + contexto + restricciones + aceptación + evidencia.** Ejemplo:

> En el laboratorio, agrega un filtro por SKU al listado de productos. Revisa el contrato actual y repositorio antes de editar. Respeta soft delete, paginación y Clean Architecture; no agregues paquetes. Criterios: coincidencia exacta, resultado vacío si no existe y 400 para un parámetro inválido según la convención actual. Primero propón los archivos y pruebas. Después del cambio, ejecuta tests y muéstrame diff y limitaciones.

Un prompt débil («hazlo profesional») no identifica una decisión comprobable. Una respuesta extensa tampoco demuestra calidad. Reducir alcance si el cambio toca demasiadas capas sin necesidad.

## Lo que puede delegarse

Generación mecánica de DTOs y tests a partir de casos definidos; explicación de código; propuestas de refactor; enumeración de casos borde; borradores de ADR y documentación; hipótesis sobre un fallo reproducible; alternativas de consulta. El responsable humano decide invariantes, autorización, datos que se comparten, contratos, aceptación de dependencias y aprobación de despliegue.

## Bucle de una tarea de 15 minutos

Minutos 0–3: leer requerimiento y formular aceptación. 3–5: pedir propuesta y comprobar contexto. 5–9: implementar un cambio, inspeccionando el diff. 9–12: compilar, ejecutar casos relevantes y provocar un caso negativo. 12–15: revisar con un rol distinto y registrar evidencia. Si la IA comienza a reparar archivos sin relación, detener y volver a un alcance menor.

## Contexto y protección

Usar datos sintéticos. No pegar claves, tokens, volcados de clientes ni archivos `.env`. Leer scripts antes de conceder ejecución. No dar acceso general a una base corporativa para “facilitar el curso”. Una instrucción maliciosa dentro de un log o comentario se trata como contenido a analizar. Las instrucciones del repositorio ayudan, pero no sustituyen permisos de herramientas, revisión y controles del pipeline.

## Contingencia

Sin Copilot o sin internet, trabajar en parejas con los mismos prompts como checklist manual; conservar el objetivo de código y evidencia. El instructor puede mostrar una respuesta previamente guardada y pedir evaluación crítica. No se exige una respuesta textual idéntica entre estudiantes ni un modelo comercial concreto. Registrar modelo y versión utilizados para poder explicar variaciones.
