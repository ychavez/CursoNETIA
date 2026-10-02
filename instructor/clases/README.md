# Material de clases

[Abrir la presentación para alumnos (PowerPoint)](00-presentacion-alumnos.pptx).

La presentación contiene **64 diapositivas**. Usa las diapositivas **1–3** para presentar el curso, el proyecto y la forma de trabajo. Cada módulo ocupa seis láminas consecutivas: resumen, explicación de conceptos y cuatro láminas de desarrollo práctico. La diapositiva **64** cierra con la entrega final.

El desarrollo explica cómo funcionan los componentes, por qué se toman ciertas decisiones y qué errores conviene detectar. Combina fragmentos de código editables, diagramas y ejemplos de AulaPedidos. Las notas del presentador amplían la explicación oral, proponen preguntas y recogen las fuentes del repositorio o documentación oficial.

Los fragmentos literales identifican su archivo. Los fragmentos simplificados están marcados como **Esquema didáctico** y las notas aclaran qué omiten. El filtro por estado del módulo 10 se identifica como **propuesta de ejercicio**, pendiente de implementar; no es una función ya incorporada a la referencia.

Abre el resumen al comenzar cada sesión, explica los conceptos y alterna las cuatro láminas de desarrollo con el código completo y la práctica del guion correspondiente.

| Módulo | Resumen | Conceptos | Desarrollo práctico | Temas |
|---|---:|---:|---:|---|
| 1. Copilot y su contexto | 4 | 5 | 6–9 | Harness, selección de contexto, criterios de aceptación, agentes y comprobaciones |
| 2. Arquitectura por capas | 10 | 11 | 12–15 | Clean Architecture, referencias de proyectos, invariantes, SOLID e inyección de dependencias |
| 3. Patrones y repositorios | 16 | 17 | 18–21 | CQRS, Mediator, `IRepository<T>`, unidad de trabajo, Result y eventos |
| 4. API y contratos | 22 | 23 | 24–27 | Recorrido HTTP, DTO, validación, errores, REST y contrato OpenAPI |
| 5. Datos con EF Core | 28 | 29 | 30–33 | ORM, relaciones, seguimiento de cambios, consultas acotadas, migraciones y concurrencia |
| 6. Seguridad y permisos | 34 | 35 | 36–39 | JWT, validación de identidad, políticas, permisos y pertenencia del pedido |
| 7. Calidad y pruebas | 40 | 41 | 42–45 | Unitarias, integración HTTP, mocks, límites del sistema y regresión |
| 8. Respuesta ante fallos | 46 | 47 | 48–51 | Outbox, transacción, adaptador HTTP, reintentos, Circuit Breaker e invalidación de caché |
| 9. Docker y diagnóstico | 52 | 53 | 54–57 | Imagen, contenedor, Compose, orden de arranque, OpenTelemetry y diagnóstico |
| 10. Integración y entrega | 58 | 59 | 60–63 | Propuesta de filtro por estado, criterios entre capas, CI, ADR y recuperación |
