---
applyTo: "src/AulaPedidos.Application/**/*.cs"
---

Modela comandos y consultas mediante el mediador y Result existentes. Un handler coordina un caso de uso; no recibe HttpContext ni conoce DbContext concreto. Usa puertos pequeños, DTOs explícitos, CancellationToken y límites de paginación. Recibe identidad obtenida por el endpoint desde el token validado, nunca del body como fuente confiable. Separa validación sintáctica, reglas de negocio y autorización de recursos.
