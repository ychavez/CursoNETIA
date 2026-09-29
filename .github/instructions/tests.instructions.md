---
applyTo: "tests/**/*.cs"
---

Usa Arrange/Act/Assert y nombres que expresen comportamiento. Cada test debe tener un riesgo y resultado observable. Prueba límites monetarios, transiciones, idempotencia, autorización, errores, persistencia y cancelación según el cambio. Usa dobles en los límites externos y base relacional para consultas; no mocks de IQueryable/DbSet. SQLite no valida semántica SQL Server. Un test no debe depender del orden ni de esperas arbitrarias. No falsees cobertura ni agregues asserts triviales.
