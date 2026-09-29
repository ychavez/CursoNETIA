# ADR 001: monolito con capas y casos de uso

- Estado: aceptada para el curso.
- Contexto: 40 horas, un equipo, una aplicación de catálogo/pedidos y necesidad de enseñar fronteras verificables.
- Alternativas: una API con toda la lógica en endpoints reduce archivos pero dificulta aislar reglas; microservicios multiplican despliegues y consistencia antes de existir esa necesidad.
- Decisión: una API desplegable con Domain, Application, Infrastructure y Api; dependencias hacia el dominio y composición explícita.
- Consecuencias: permite probar negocio sin servidor y sustituir adaptadores. Requiere mantener disciplina; carpetas por sí solas no garantizan fronteras. CQRS no implica bases separadas.
- Evidencia: referencias de proyectos y tests de reglas/casos de uso.
- Revisar cuando exista un dominio con equipo y ciclo de despliegue independientes, no sólo al crecer el número de clases.
