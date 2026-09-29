---
applyTo: "src/AulaPedidos.Application/**/*.cs"
---

Modela comandos y consultas mediante el mediador y Result existentes. Un handler coordina un caso de uso; no recibe HttpContext ni conoce DbContext concreto. Usa puertos pequeños, DTOs explícitos, CancellationToken y límites de paginación. Recibe identidad obtenida por el endpoint desde el token validado, nunca del body como fuente confiable. Separa validación sintáctica, reglas de negocio y autorización de recursos.

Usa `IRepository<T>` para operaciones comunes de raíces `IAggregateRoot`; conserva interfaces específicas para SKU y listados por cliente. `GetByIdAsync` permite mutaciones de dominio con seguimiento; `GetByIdsAsync` es lectura sin seguimiento. Confirma mediante `IUnitOfWork`; mantén comprobaciones de dueño y versión e invalidación de caché. No expongas `IQueryable` ni añadas borrado físico genérico.
