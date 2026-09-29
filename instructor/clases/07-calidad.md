# Clase 7 — Pruebas, refactorización y debugging con IA (240 minutos)

## Objetivo

Construir evidencia de reglas, persistencia y contrato HTTP; usar dobles donde ayuden; medir cobertura sin confundirla con calidad; reproducir un bug, repararlo y conservar una regresión. La suite final sirve de referencia, no sustituye el razonamiento del estudiante.

## Guion cronometrado

| Minutos | Qué DECIR | Qué HACER / evidencia |
|---|---|---|
| 0–15 | «Un test valioso protege una decisión o un riesgo. Contar tests sin leer sus asserts puede darnos falsa seguridad.» | Pedir riesgos del pedido: cantidades, dueño, precio histórico, versión y persistencia. |
| 15–40 | «Una prueba unitaria aísla una unidad de comportamiento; una de integración comprueba colaboración real.» | Clasificar ejemplos por capa y coste. Dibujar la matriz riesgo/prueba. |
| 40–65 | «Un mock ayuda a observar un límite. Si simula una base completa, puede ocultar precisamente el defecto que buscamos.» | Comparar fake de repositorio con SQLite relacional. No usar EF InMemory para demostrar restricciones SQL. |
| 65–95 | «Primero escribimos el escenario y el resultado; después dejamos que Copilot produzca código de prueba.» | Generar tests parametrizados de cantidad/precio; revisar cada caso y assert. |
| 95–125 | «Una regresión demuestra el fallo antes de la reparación. Si siempre estuvo verde, puede no estar probando el bug.» | En copia de lab introducir un fallo de límite; test rojo, diagnóstico, reparación, verde. |
| 125–150 | «Refactor significa conservar comportamiento. Si cambió un estado HTTP, no es sólo limpieza.» | Aplicar prompt refactor a un handler; inspeccionar diff y correr tests de caracterización. |
| 150–175 | «La cobertura indica líneas visitadas, no requisitos satisfechos. Un assert vacío puede cubrir mucho.» | Ejecutar cobertura, localizar informe Cobertura y contrastar una rama no probada. |
| 175–215 | «Cada pareja debe demostrar una regresión y una revisión que descubra un test insuficiente.» | Laboratorio L07 con reporte, bitácora y ejecución repetible. |
| 215–230 | «Un pipeline debe rechazar lo mismo que nuestra máquina detecta, con dependencias reproducibles.» | Revisar CI, locked restore, build, tests y auditoría. Explicar no suprimir avisos por rutina. |
| 230–240 | «El siguiente paso será probar comportamiento bajo fallos de red y entrega repetida.» | Ticket: qué bug podría sobrevivir a 100% de líneas. |

## Demostración guiada

1. Abrir tests de Domain/Application y escoger una invariante con riesgo real.
2. Escribir la expectativa en lenguaje natural antes del prompt: «cantidad 0 se rechaza sin crear pedido».
3. Pedir tabla de casos y luego código. Corregir tests que comprueban sólo `NotNull`.
4. En **laboratorio** cambiar temporalmente una condición de límite para permitir 0. Ejecutar prueba y observar rojo. No introducir este fallo en la referencia.
5. Adjuntar a Copilot sólo test fallido, método y stack trace sanitizado. Pedir hipótesis, no cambios masivos.
6. Reparar y correr prueba focalizada; después suite afectada. Restaurar el cambio deliberado aunque la clase se interrumpa.
7. Abrir `tests/AulaPedidos.IntegrationTests/ApiFactory.cs` y explicar host real + base relacional aislada; no mockear el controller.
8. Ejecutar verificación con cobertura en referencia ya configurada:

```powershell
.\scripts\verify.ps1 -Coverage
Get-ChildItem .\artifacts\tests -Recurse -Filter coverage.cobertura.xml
```

En laboratorio sin scripts finales, usar:

```powershell
dotnet test AulaPedidos.slnx --collect 'XPlat Code Coverage' --results-directory artifacts/tests
```

El informe cubre instrumentación de los proyectos/configuración ejecutados. Registrar exclusiones y proveedores; no presentar porcentaje como cobertura de todos los requisitos. La raíz incluye `dotnet-tools.json`; `dotnet tool restore` usa ese manifiesto.

**Prompt de tests:**

> Lee Order.Create y sus tests existentes. Propón casos de límites, duplicados y precio histórico sin repetir escenarios equivalentes. Para cada test explica qué defecto detectaría. Genera sólo los faltantes, usa el framework ya instalado y ejecuta la suite. No cambies dominio para acomodar una expectativa incorrecta.

**Prompt de debugging:**

> Este test falló tras el cambio adjunto. Separa hechos de hipótesis, identifica la comprobación mínima y la causa raíz. No retires el assert ni aumentes tolerancia sin razón de negocio. Repara únicamente el defecto confirmado y conserva la regresión.

## Laboratorio L07 y solución

**Consigna:** agregar una prueba unitaria de operación fallida sin mutación parcial, una integración HTTP de usuario ajeno y una persistencia de versión concurrente. Introducir/reparar un defecto temporal en lab. Refactorizar un bloque sin alterar contrato y entregar evidencia antes/después.

**Aceptación:** tests aislados y deterministas; falla roja observada; tests verdes al final; no dependencias del orden o sleeps arbitrarios; revisión de diff; cobertura interpretada con al menos una brecha real.

**Solución:** referencia en `tests/AulaPedidos.UnitTests/` y `tests/AulaPedidos.IntegrationTests/{ApiFactory,ApiTests,PersistenceFixture,PersistenceTests,ArchitectureTests}.cs`. Dobles en puertos externos y entidades reales para reglas. Dos DbContext con misma fila y versiones iniciales sirven para demostrar conflicto; SQLite tiene límites que deben declararse. Test HTTP con dos JWT distintos verifica propiedad de verdad.

**Extensión:** mutación manual de una condición y comprobar que la suite la detecta; si no, identificar aserción/caso faltante. No introducir una herramienta de mutation testing a mitad del curso sólo para obtener otra cifra.

## Preguntas y recuperación

- **¿Mock, stub y fake son iguales?** Son dobles con distintos propósitos: comportamiento/preparación, verificación de interacción o implementación simplificada; elegir por necesidad.
- **¿Hay que probar privados?** Preferir comportamiento público; un privado difícil de alcanzar puede indicar diseño o caso insuficiente.
- **¿Todo test debe ser unitario por velocidad?** No; HTTP, EF, migraciones y autenticación requieren integración.
- **¿Un fallo intermitente se resuelve reintentando test?** Primero investigar tiempo, estado compartido, red y orden; repetir puede ocultarlo.

Tests sin descubrir: revisar SDK/test project y atributos. Base compartida entre tests: aislar fixtures/datos y tiempos de vida. Captura de excepción incorrecta: comprobar que se ejecuta el async. Cobertura cero: revisar colector/configuración antes de concluir falta de tests. La solución final debe quedar verde; no dejar el fallo pedagógico pendiente.
