# Guía del instructor: .NET empresarial con GitHub Copilot, 40 horas

Este paquete contiene una aplicación de referencia completa y un recorrido para construirla durante clase. El curso está dirigido a desarrolladores, arquitectos, líderes y equipos de software con C# básico/intermedio, orientación a objetos, SQL y nociones de REST. La IA participa en todo el ciclo: diseño, código, refactor, pruebas, diagnóstico, consultas, documentación y revisión. El criterio técnico se evalúa con evidencia, no por cantidad de código generado.

Abre la [presentación para alumnos (PowerPoint)](clases/00-presentacion-alumnos.pptx), ampliada a **64 diapositivas**. Las primeras tres introducen el curso; cada módulo ocupa seis láminas: resumen, conceptos y cuatro de desarrollo práctico. La diapositiva 64 presenta la entrega final. Consulta el [índice de diapositivas](clases/README.md) para localizar cada bloque.

El desarrollo profundiza en mecanismos, decisiones y ejemplos del repositorio con fragmentos de código editables y diagramas. Las notas del presentador contienen explicación oral, errores habituales, preguntas para el grupo y fuentes técnicas. Alterna las láminas con el código completo y el laboratorio del guion; usa las notas para preparar la explicación, sin leerlas como monólogo.

Los fragmentos literales señalan su archivo y los simplificados se marcan como **Esquema didáctico**, con sus omisiones explicadas en las notas. El filtro por estado del módulo 10 es una **propuesta de ejercicio**, pendiente de implementar en el laboratorio; no se presenta como una función incorporada a la solución de referencia.

## Duración y organización

**10 clases de 240 minutos efectivos = 2,400 minutos = 40 horas.** Los descansos no se descuentan del contenido. Se recomiendan dos pausas de 10 minutos por clase, después del minuto 80 y 160: duración de calendario de 4h20 por sesión. Si la empresa exige bloques de exactamente 4h con pausas incluidas, deberá añadir 200 minutos de sesiones para conservar las 40 horas de formación.

| Clase | Tema y resultado | Guion |
|---|---|---|
| 1 | Entorno, uso responsable de IA y harness probado | [01](clases/01-ia-harness.md) |
| 2 | Clean Architecture, SOLID, DI y reglas del dominio | [02](clases/02-arquitectura.md) |
| 3 | Repository, CQRS, Mediator, Result y eventos | [03](clases/03-patrones.md) |
| 4 | REST, contratos, validación, errores, versión y OpenAPI | [04](clases/04-api.md) |
| 5 | EF Core, migraciones, relaciones, auditoría y performance | [05](clases/05-datos.md) |
| 6 | JWT, roles/permisos, autorización de recursos y OWASP | [06](clases/06-seguridad.md) |
| 7 | Unitarias/integración, mocks, regresión, cobertura y refactor | [07](clases/07-calidad.md) |
| 8 | Outbox, Adapter, Retry, Circuit Breaker y Cache Aside | [08](clases/08-resiliencia.md) |
| 9 | Docker, Compose, OpenTelemetry, salud y diagnóstico | [09](clases/09-operacion.md) |
| 10 | Integración, desafío final, revisión y preparación productiva | [10](clases/10-entrega.md) |

Cada guion contiene intervalos contiguos, discurso sugerido, acciones, comandos, resultados, laboratorio, soluciones, preguntas y recuperación. Los tiempos incluyen preguntas y práctica; no se pide leer todo el discurso como un monólogo. El instructor ejecuta primero una demostración corta, verbaliza su decisión y cede el teclado al grupo.

## Antes de impartir

1. Completar [preparación](00-preparacion.md) con 48 horas de antelación y ensayar desde una computadora limpia.
2. Aplicar [diagnóstico](diagnostico.md) y revisar experiencia/grupos.
3. Leer [recorrido incremental](recorrido-laboratorio.md): la solución entregada es referencia; la carpeta del alumno se construye paso a paso.
4. Revisar [matriz de cobertura](mapa-cobertura.md), [evaluación](evaluacion.md) y [troubleshooting](problemas-frecuentes.md).
5. Confirmar versiones y comportamiento del Copilot del aula con [harness](../docs/harness-copilot.md). No prometer pantallas idénticas si cambia Visual Studio.

## Política pedagógica

- Cada participante conserva su bitácora y al menos un rechazo razonado de una propuesta IA.
- Trabajar en parejas conductor/revisor; cambiar cada 25 minutos. Ambos deben explicar un flujo sin Copilot.
- Un alumno que termina temprano trabaja una extensión, mientras otro recupera el checkpoint; no quitar pruebas ni seguridad para avanzar.
- El instructor muestra errores deliberados sólo en el laboratorio y los revierte después de la regresión.
- No usar datos, tokens ni repositorios de clientes. Las demostraciones se hacen con datos sintéticos.
- Un fallo de internet activa la alternativa manual; no convierte la clase en una sesión de instalaciones.

## Entregables del participante

Repositorio/laboratorio ejecutable, harness adaptado, ADR, pruebas de comportamiento, reporte de cobertura con límites, evidencia Docker/telemetría, revisión de seguridad, bitácora de IA y plan de producción. La solución de referencia sirve para contrastar después de intentar el ejercicio; no se califica copiarla.

## Materiales de apoyo

[Arquitectura](../docs/arquitectura.md) · [Seguridad](../docs/seguridad.md) · [Producción](../docs/produccion.md) · [Soluciones](soluciones/README.md) · [Bitácora](plantillas/bitacora-ia.md) · [Ficha de entrega](plantillas/entrega-final.md).

La semblanza del instructor se completa con información real antes de ofrecer el curso. No se han inventado credenciales, experiencia, certificaciones ni clientes.
