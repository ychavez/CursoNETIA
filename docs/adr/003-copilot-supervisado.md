# ADR 003: Copilot como colaborador supervisado

- Estado: aceptada.
- Contexto: acelerar generación, análisis y pruebas sin delegar decisiones de negocio y seguridad a una salida probabilística.
- Decisión: instrucciones versionadas, prompts pequeños, herramientas acotadas, revisión humana y verificaciones ejecutables. No fijar un modelo del plan individual.
- Alternativas: aceptar sugerencias sin evidencia es rápido sólo en apariencia; prohibir IA evita aprender su uso profesional.
- Consecuencias: el equipo debe invertir en criterios y tests. Las instrucciones pueden incumplirse; la revisión y los controles técnicos detectan parte de esos fallos. La bitácora conserva tanto rechazos como aceptaciones.
- Evidencia: cada laboratorio entrega prompt, diff, prueba negativa y explicación propia.
- Revisar cuando cambien políticas, disponibilidad o capacidades de Visual Studio.
