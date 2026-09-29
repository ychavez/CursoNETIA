# Clase 10 — Integración, evaluación y preparación productiva (240 minutos)

## Objetivo

Entregar una modificación completa defendible con arquitectura, seguridad, datos, tests, Docker y observabilidad; explicar límites productivos y demostrar uso profesional de IA. La evaluación integra el curso, no exige rehacer desde cero todos los archivos en cuatro horas.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «Hoy presentarán una decisión y evidencia. No se evalúa velocidad de aceptar código de Copilot.» | Publicar rúbrica y entregables; asignar feature equivalente por pareja. |
| 15–35 | «Antes de editar escriban criterios observables y riesgos. El revisor debe poder decir si terminaron.» | Cada pareja redacta contrato, alcance y pruebas. Instructor valida dificultad equivalente. |
| 35–95 | «Construyan en pasos pequeños. Si la IA amplía alcance, vuelvan al requisito y registren la decisión.» | Implementación del desafío final con prompts, diff y pruebas. Revisar avance en minuto65. |
| 95–125 | «Probar el éxito no basta. Muestren entrada inválida, acceso indebido y persistencia.» | Ejecutar tests focalizados y suite; reparar defectos. Generar evidencia sin secretos. |
| 125–150 | «Una feature entregable necesita poder ejecutarse y observarse.» | Build Docker/smoke o estación compartida, traza/log/métrica y health. |
| 150–175 | «El revisor busca defectos concretos; cada comentario necesita escenario reproducible.» | Intercambiar revisión entre parejas usando prompt revisión; autor responde y aplica corrección justificada. |
| 175–200 | «Listo para producción depende del contexto de empresa. Vamos a identificar lo que falta y quién lo resolvería.» | Taller staging, OIDC, secretos, backups, migraciones, multiworker, caché, SLO y rollback. Completar matriz de producción. |
| 200–225 | «Cada miembro explicará una frontera y un fallo. La IA no responde por ustedes durante la defensa.» | Presentaciones breves con turno individual. Para grupos grandes, estaciones paralelas y rúbrica entre pares supervisada. |
| 225–235 | «Comparen sus respuestas con el diagnóstico inicial usando ejemplos del proyecto.» | Evaluación de conceptos y reflexión sobre una propuesta IA rechazada. |
| 235–240 | «La entrega incluye riesgos pendientes con dueño y criterio de cierre.» | Recoger ficha final y comunicar recuperación de criterios faltantes. |

## Desafío final estándar

Añadir **filtro opcional por estado al listado de pedidos propios** (`Submitted` o `Cancelled`) manteniendo paginación, autorización, orden y contrato existente cuando el filtro se omite. No crear otra base ni otro servicio.

**Criterios dados al participante:**

1. Sin filtro, comportamiento actual.
2. Filtro válido devuelve sólo pedidos de ese estado del sujeto autenticado.
3. Valor de estado inválido produce error de entrada documentado.
4. La paginación y TotalCount reflejan el mismo filtro.
5. Ningún filtro revela pedidos de otro usuario.
6. Query se filtra en base antes de paginar, sin traer toda la tabla.
7. Pruebas, OpenAPI/documentación y ADR breve actualizados.

**Prompt inicial:**

> Implementaremos filtro opcional de estado de pedidos propios. Lee contrato, handler, repositorio y tests antes de editar. Primero propón cambios por capa y matriz de aceptación indicada. Mantén v1 compatible cuando se omite filtro, no agregues paquetes y filtra antes de Count/Skip/Take. Tras implementar, ejecuta pruebas, muestra diff y riesgos no verificados. No despliegues ni cambies secretos.

## Solución orientativa del desafío

No está preimplementada en la referencia: es una extensión para demostrar transferencia. El instructor revisa estas decisiones:

1. `Api/Controllers/OrdersController.cs`: parámetro opcional validado y documentado; no acepta CustomerId del cliente.
2. `Application/Orders/OrderContracts.cs`: ListOrdersQuery lleva filtro opcional tipado.
3. `Application/Orders/OrderHandlers.cs`: valida y mantiene sujeto/paginación.
4. `Application/Abstractions/IPersistence.cs`: amplía lectura de pedidos sin contaminar dominio con HTTP.
5. `Infrastructure/Persistence/Repositories.cs`: compone Where por cliente y estado, aplica mismo predicado a Count e Items y orden estable antes de paginar.
6. Tests: mezcla estados/usuarios, omisión, inválido y páginas; comprobar que TotalCount no cuenta estados excluidos. No hace falta migración porque Status ya existe; un índice nuevo exige medición.
7. Documentar compatibilidad y límite. Copilot puede proponer un índice, pero no se acepta sólo porque lo recomendó.

**Alternativas equivalentes:** filtro exacto por SKU en catálogo, o métrica de backlog outbox con prueba y runbook. El instructor ajusta rubricación para asegurar autorización/persistencia/operación en la defensa aunque la feature elegida no las toque directamente.

## Evaluación en vivo

Abrir `instructor/evaluacion.md`. Cada pareja dispone de 5 minutos de demo + 3 de defensa. Con más de tres parejas, usar estaciones/revisores simultáneos dentro del bloque y recoger video corto sanitizado como evidencia complementaria; no alargar silenciosamente las 40 horas. Todos responden al menos una pregunta técnica individual.

**Preguntas para defensa:** ¿Por qué ese archivo pertenece a esa capa? ¿Qué prueba detecta acceso ajeno? ¿Qué ocurre si el receptor cae después de procesar y antes de confirmar? ¿Qué falta para dos réplicas? ¿Cómo sabes que la consulta mejoró? ¿Qué propuesta de IA rechazaste y con qué evidencia?

## Ensayo productivo

Leer `docs/produccion.md`; asignar responsable y evidencia esperada a cada brecha relevante. Diseñar orden de migración/despliegue y rollback compatible. El curso no publica automáticamente servicios en una nube ni adquiere recursos; el ensayo se hace en entorno de aula y se entrega plan concreto para infraestructura de la empresa.

## Cierre y recuperación

Se aprueba por rúbrica con controles críticos satisfechos. Si falta Docker en equipo personal pero se ejecutó con participación demostrable en estación autorizada, registrar esa condición. Si falta autorización de recursos o hay secretos, corregir antes de aprobar. Definir una recuperación acotada con caso, prueba y nueva defensa; no exigir rehacer todo lo ya demostrado.
