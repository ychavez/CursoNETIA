# Diagnóstico y ajuste del grupo

Enviar antes de la primera clase. No determina quién “sirve” para programar; permite organizar parejas y refuerzo. Las respuestas 1–8 se puntúan 0 = no lo conozco, 1 = lo he visto, 2 = lo he usado con ayuda, 3 = lo uso de forma autónoma. Las 9–10 son abiertas.

1. ¿Cuál es tu experiencia con C# y .NET? Da un ejemplo de async o una clase que hayas escrito.
2. ¿Has trabajado con Visual Studio? ¿Puedes crear, depurar y ejecutar una solución?
3. ¿Conoces APIs REST? Explica la diferencia entre GET y POST con un ejemplo.
4. ¿Has utilizado Entity Framework? ¿Qué es una migración?
5. ¿Conoces Repository o CQRS? Explica uno sin repetir sus siglas.
6. ¿Has trabajado con Docker? Diferencia imagen, contenedor y volumen.
7. ¿Has utilizado GitHub Copilot? Describe una sugerencia que decidiste rechazar.
8. ¿Has implementado pruebas unitarias? ¿Qué comportamiento comprobaste?
9. ¿Qué te interesa aprender de arquitectura de software?
10. ¿Qué esperas lograr con este curso en tu trabajo?

## Comprobación práctica, 20 minutos

Entregar una función que calcula total de líneas y pedir: explicar entradas, identificar una cantidad negativa, escribir un caso borde y decidir dónde validar. No exigir instalar un framework nuevo. Evaluar 0–2 en lectura, regla, prueba y explicación. Esto verifica fundamentos que una autoevaluación puede sobreestimar.

## Interpretación

0–8 de autoevaluación: preparar repaso de C# básico, interfaces, async, HTTP y Git antes del curso; asignar pareja de apoyo. 9–16: ruta estándar con guiones. 17–24: ruta estándar más extensiones de concurrencia, observabilidad y evaluación de tradeoffs. El diagnóstico práctico puede modificar la agrupación. No enseñar arquitectura avanzada sobre fundamentos ausentes sin apoyo adicional.

## Preguntas de comprobación y respuestas orientativas

- **¿Una interfaz garantiza diseño limpio?** No; importan dirección de dependencias y semántica del contrato.
- **¿Un test que pasa demuestra ausencia de bugs?** Demuestra que ese escenario pasó bajo esas condiciones.
- **¿GET debería cambiar stock?** No; una lectura no debe producir ese efecto de negocio.
- **¿async ejecuta cualquier operación más rápido?** No; permite no bloquear mientras espera I/O.
- **¿Una migración es un backup?** No; describe cambios de esquema y no recupera por sí sola datos eliminados.
- **¿Copilot puede aprobar su propio cambio?** Puede revisarlo, pero la aceptación sigue siendo responsabilidad del equipo y su evidencia.

Repetir las preguntas clave al terminar para que el participante reconozca progreso con ejemplos de su proyecto.
