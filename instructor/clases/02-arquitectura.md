# Clase 2 — Clean Architecture, SOLID, DI y dominio (240 minutos)

## Objetivo y entrada

Entrada: esqueleto compilable y harness probado. Salida: Product, Order y OrderItem con reglas verificables; referencias de proyectos dirigidas hacia Domain; ADR de una decisión. El dominio puede ejecutarse sin servidor web ni EF.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «Ayer definimos cómo colaborar con IA. Hoy convertimos las reglas en fronteras que el compilador ayuda a sostener.» | Revisar ticket previo y abrir referencias de proyectos en VS. |
| 15–40 | «La arquitectura organiza decisiones y dependencias. Las carpetas no protegen nada si una entidad conoce HttpContext.» | Dibujar Domain ← Application ← Infrastructure, composición en Api. Pedir ubicar precio, SQL, JWT y DTO. |
| 40–65 | «Dependencia de compilación y llamada en ejecución no son lo mismo. Una interfaz permite que negocio use persistencia sin importar EF.» | Mostrar `.csproj` y navegación hacia interfaces. Demostrar con un ejemplo verbal de repositorio concreto. |
| 65–85 | «SOLID sirve para razonar cambios. Una interfaz por clase no equivale a buen diseño.» | Explicar cinco principios con ejemplos de `docs/arquitectura.md`; pedir una violación concreta de SRP. |
| 85–110 | «Una entidad válida debe permanecer válida después de cada operación. Validar todo antes de asignar evita cambios parciales al fallar.» | Implementar Product en lab: SKU normalizado, nombre, precio decimal, setters restringidos. Comparar con entidad final después del intento. |
| 110–135 | «El precio del catálogo puede cambiar; el precio histórico de la línea no. El pedido protege sus transiciones.» | Diseñar OrderItem y Order. Mostrar lista encapsulada, cantidades 1..100, máximo 50 líneas, no repetidas y cancelación. |
| 135–155 | «La configuración describe el entorno; no debe cambiar las reglas de negocio ni meter secretos en el dominio.» | Explicar opciones, entorno Development/Production y DI. Revisar lifetimes scoped/singleton/transient con DbContext como ejemplo posterior. |
| 155–195 | «Cada pareja implementará una regla y un caso que la intente romper.» | Laboratorio L02. Acompañar pruebas de precio y cancelación; usar Copilot para proponer casos, no para decidir límites. |
| 195–220 | «Una segunda lectura busca contradicciones, no elogios. Pidan al revisor una forma de violar la invariante.» | Usar agente revisor o prompt; ejecutar pruebas y revisar diff. Escribir ADR 001 adaptado. |
| 220–235 | «Podemos cambiar de base de datos sin cambiar el concepto de pedido, pero habrá trabajo en persistencia y pruebas.» | Defensa de dos parejas: diagrama y alternativa descartada. |
| 235–240 | «La siguiente clase conectará las reglas con los casos de uso.» | Ticket: dónde ubicar envío de correo y por qué. |

## Demostración guiada

1. Revisar que Domain no tiene `ProjectReference`; Application referencia Domain; Infrastructure referencia Application/Domain; Api compone dependencias. El esqueleto ya provee estas referencias: no añadir referencias circulares.
2. Crear en lab `Entities/Product.cs` y las excepciones de dominio. Pedir a Copilot propuesta antes de editar.
3. Probar precio 0, 0.001, 0.01 y 1,000,001; preguntar qué ocurre después del redondeo a dos decimales. Referencia acepta precio efectivo desde 0.01 y tope 1,000,000; validar antes de mutar.
4. Crear OrderItem con precio copiado del Product, cantidad 1..100 y total calculado.
5. Crear Order con dueño, 1..50 líneas únicas, estado Submitted/Cancelled y Version. Dejar el evento para clase 3 si aún no está implementado; anotar explícitamente ese pendiente.
6. Usar un test unitario para demostrar que cancelar dos veces no modifica de nuevo el agregado.

```powershell
dotnet build src/AulaPedidos.Domain/AulaPedidos.Domain.csproj
dotnet test tests/AulaPedidos.UnitTests/AulaPedidos.UnitTests.csproj
```

El primer comando valida referencias/sintaxis. El segundo sólo aporta evidencia si el alumno creó tests relevantes; cero tests no equivale a éxito del ejercicio.

**Prompt:**

> Implementemos únicamente Product en el laboratorio. Lee instrucciones Domain. Reglas: SKU 3..32 caracteres ASCII alfanuméricos o guion, normalizado; nombre 3..120; precio decimal redondeado a 2 decimales y válido entre 0.01 y 1,000,000; borrado lógico impide actualización. Explica el orden de validación para evitar mutación parcial y propón pruebas. No agregues EF ni DTOs HTTP.

## Laboratorio L02 y solución

**Consigna:** completar Product/OrderItem/Order, escribir cuatro pruebas de reglas y un ADR sobre encapsulación. Requisitos de pedido: dueño no vacío hasta 100 caracteres y sin espacios en los extremos, una a cincuenta líneas válidas, productos sin duplicar, cantidad 1..100, total calculado y cancelación sólo desde Submitted.

**Aceptación:** no hay I/O, EF ni HTTP en Domain; setters no permiten saltar reglas; pruebas de borde y operación fallida; el total se basa en valores históricos. Explicar por qué el DTO de entrada no puede controlar Total.

**Solución:** contrastar `src/AulaPedidos.Domain/Entities/Product.cs`, `OrderItem.cs`, `Order.cs` y `DomainExceptions.cs`. Usar factoría/constructor protegido y métodos con intención; `decimal` para precio. La solución referencia valida todas las entradas antes de asignar, genera una versión nueva al cambiar y evita setters públicos. No se exige copiar exactamente esos nombres privados.

**Extensión:** simular actualización inválida después de un estado válido y afirmar que nombre, SKU, precio y Version permanecen iguales. Es un test más significativo que comprobar sólo que se lanzó una excepción.

## Preguntas y recuperación

- **¿Domain puede lanzar una excepción?** Sí, una excepción propia expresa una violación; Application/Api deciden su representación externa. No debe lanzar una excepción HTTP.
- **¿Todas las validaciones se duplican?** El transporte valida forma; dominio protege invariantes independientemente del origen. Puede haber solapamiento deliberado, documentado.
- **¿Singleton es más rápido?** Su tiempo de vida no se elige por intuición. Estado compartido y dependencias scoped requieren cuidado.
- **¿Un monolito puede estar bien diseñado?** Sí; despliegue y organización interna son decisiones distintas.

Errores frecuentes: `double` para dinero; colección pública mutable; setters que omiten invariantes; `DbContext` en Domain; referencias circulares. Recuperar el último archivo compilable de la carpeta del participante, no un tag inexistente. Si faltan bases de C#, hacer una entidad y sus pruebas con la pareja y recuperar Order desde referencia explicando cada regla antes de seguir.
