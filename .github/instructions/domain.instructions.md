---
applyTo: "src/AulaPedidos.Domain/**/*.cs"
---

Mantén el dominio libre de HTTP, EF Core, logging y dependencias de infraestructura. Protege invariantes en operaciones de las entidades, con setters restringidos y colecciones encapsuladas. Los montos usan decimal; conserva el precio histórico de cada línea. Un evento de dominio expresa un hecho pasado y no envía correos ni realiza I/O. Incluye pruebas que fallen si se omite la invariante. No añadas interfaces a cada clase por costumbre.
