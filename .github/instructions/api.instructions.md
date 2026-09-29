---
applyTo: "src/AulaPedidos.Api/**/*.cs"
---

Handlers HTTP delgados; DTOs sin entidades EF. Declara versión, estados HTTP y OpenAPI coherentes. Autenticación antes de autorización. Políticas por permisos más protección de propietario. No conviertas toda excepción en 400. Expón ProblemDetails sanitizado y correlación de trazas. Emisión de tokens de aula sólo en Development; nunca habilitarla en producción para resolver un fallo. No registrar Authorization ni secretos.
