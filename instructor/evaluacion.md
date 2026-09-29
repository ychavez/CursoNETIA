# Evaluación y rúbrica de 100 puntos

## Criterios de aprobación

80/100 o más, defensa individual y todos los controles críticos satisfechos. Si el curso es corporativo sin calificación formal, usar los mismos criterios como diagnóstico de transferencia. Se evalúan tanto el incremento final como la evidencia acumulada de las diez clases.

| Dimensión | Puntos | Evidencia para puntaje completo |
|---|---:|---|
| Arquitectura y dominio | 15 | Fronteras reales, invariantes y ADR con alternativa/coste |
| Patrones y consistencia | 15 | Explica Repository/CQRS/Mediator/Result/eventos/outbox y aplica su semántica; reconoce entrega repetida |
| HTTP y persistencia | 15 | DTO/estados/versión, validación, migración, relaciones, soft delete y concurrencia |
| Seguridad | 15 | 401/403 y acceso ajeno, identidad confiable, permisos y secretos externos |
| Tests y calidad | 15 | Unitarias e integración pertinentes, regresión roja/verde, coverage interpretada, revisión de dependencias |
| Docker y operación | 10 | Imagen/Compose, persistencia, señales OTel, salud y diagnóstico con evidencia |
| Uso profesional de IA | 10 | Harness comprobado, prompts con criterios, revisión de diff y rechazo razonado |
| Comunicación y producción | 5 | Defensa clara y riesgos con dueño, rollback y validación pendiente |
| **Total** | **100** | |

## Escala por dimensión

- **100% de los puntos:** ejecuta, explica y prueba sin contradicciones; evidencia reproducible y límites claros.
- **75%:** resultado correcto con una omisión no crítica o ayuda puntual; sabe explicar la corrección.
- **50%:** parcialmente implementado, explicación incompleta o evidencia limitada al camino feliz.
- **25%:** copia visible sin explicación, tests triviales o demo que no se reproduce.
- **0%:** ausente, evidencia fabricada o comportamiento contrario al requisito.

Redondear al medio punto más cercano y justificar con un hecho concreto. No penalizar formato del prompt, estilo verbal ni elección de modelo si el resultado está validado. El consumo de tokens no es una métrica de aprendizaje.

## Controles críticos (bloquean aprobación hasta corregir)

1. No hay secretos ni tokens en archivos entregados o evidencia pública.
2. Un usuario no puede consultar/cancelar pedidos de otro.
3. Firma/emisor/audiencia/vigencia no fueron desactivados para lograr éxito.
4. Pedido e intención outbox no se presentan como consistentes si se guardan de forma separada sin garantía.
5. No se borraron tests ni se falsificaron ejecuciones para presentar verde.
6. El alumno identifica modo de aula, limitación multiworker/caché y brechas de producción.

## Examen de conceptos (usar dentro de la defensa, sin sumar puntos duplicados)

| Pregunta | Respuesta orientativa y dimensión |
|---|---|
| 1. ¿Dónde va la regla de precio y por qué? | Domain, porque debe sobrevivir a cambios de transporte/persistencia. Arquitectura. |
| 2. ¿Puede Api referenciar Infrastructure? | Sí en composición; no justifica que negocio dependa de DbContext. Arquitectura. |
| 3. ¿CQRS requiere dos bases? | No; la referencia separa solicitudes con almacenamiento compartido. Patrones. |
| 4. ¿Qué aporta el mediador? | Resolución/despacho de solicitud a handler; no define reglas ni garantiza calidad. Patrones. |
| 5. ¿Cuándo usar Result y cuándo excepción? | Fallos esperados explícitos vs fallos inesperados; no esconder todos en400. HTTP. |
| 6. ¿Por qué DTO en vez de entidad? | Contrato controlado, evita acoplamiento/sobreasignación. HTTP/seguridad. |
| 7. ¿Qué garantiza la migración? | Evolución de esquema reproducible, no backup ni recuperación automática de datos. Datos. |
| 8. ¿Por qué dos usuarios son esenciales en tests? | Detectan autorización de recursos, no sólo login. Seguridad. |
| 9. ¿Qué diferencia401 y403? | Credenciales ausentes/no válidas vs acceso no autorizado con identidad válida. Seguridad. |
| 10. ¿Qué falla si repetimos un POST tras timeout? | El servidor pudo haber procesado; se necesita idempotencia. Patrones. |
| 11. ¿Outbox elimina duplicados? | No; consumidor deduplica EventId con persistencia. Patrones. |
| 12. ¿Para qué circuito abierto? | Reducir llamadas durante degradación, tras muestras/umbrales; no reparar servicio. Patrones. |
| 13. ¿Qué pasa con caché en dos APIs? | Memorias distintas, invalidación no coordinada y posible obsolescencia. Operación. |
| 14. ¿100% cobertura demuestra corrección? | No; visitar líneas no verifica todos los requisitos ni asserts. Calidad. |
| 15. ¿Qué prueba SQLite no sustituye? | Semántica/plan/concurrencia del proveedor SQL Server destino. Calidad/datos. |
| 16. ¿Por qué localhost falla entre contenedores? | Refiere al mismo contenedor; usar nombre de servicio/red. Operación. |
| 17. ¿Readiness y liveness iguales? | Capacidad de atender dependencias vs proceso vivo. Operación. |
| 18. ¿Qué hace OpenTelemetry? | Instrumenta/recolecta/exporta señales; necesita backend para consultarlas. Operación. |
| 19. ¿Cómo comprobar instrucciones Copilot? | References más inspección de resultados/tests; promesa del modelo insuficiente. IA. |
| 20. ¿Qué falta para producción? | Respuesta contextual: OIDC, secretos, backup, escala, SLO, rollback y evidencia. Entrega. |

## Evidencia y retroalimentación

Cada feedback usa «observé X, produce riesgo Y, mejora Z y demuestra con prueba W». Ejemplo: «TotalCount no aplica estado; la página informa más pedidos de los que puede devolver; reutiliza el predicado y agrega test con dos estados».

El instructor conserva rúbrica por persona aunque el código sea de pareja. Si una persona sólo observó, pedirle una modificación menor o diagnóstico propio. Recuperación: tarea acotada sobre criterio faltante, test negativo y defensa; el resto del puntaje se conserva. Completar la plantilla de entrega final con resultados reales y limitaciones del entorno.
