# Game Design Document — The Maze

Desarrollo individual: Juan Arese. Estado vigente al 20/10/2026.

## Concepto y objetivo

Juego local de estrategia por turnos para dos personas. Cada jugador construye una secuencia visual de acciones para recorrer un laberinto procedural. Gana quien entra primero en la casilla de salida o reduce a cero la vida del rival.

El desafío central consiste en planificar una ruta, administrar el turno y decidir entre avanzar, esperar y atacar. Se conserva una presentación sobria, modelos tridimensionales y cámara diagonal de tercera persona.

## Preparación y escenario

Desde el menú principal, Jugar abre la selección de tamaño. Se ofrecen 4×4, 6×6 y 8×8. La primera elección recomendada es 6×6 y se recuerda la última elegida. Reiniciar conserva el tamaño y genera otro laberinto.

El generador visita las celdas y elimina paredes entre vecinas mediante un recorrido aleatorio en profundidad. Se ubican ambos jugadores en extremos de un mismo borde y una salida en el lado opuesto. Los caminos conectan todas las celdas. Los poderes no aparecen sobre las casillas iniciales ni sobre la salida.

| Tamaño | Phase | Radar verdadero |
|---|---:|---:|
| 4×4 | 1 | 1 |
| 6×6 | 1 | 1 |
| 8×8 | 2 | 2 |

## Turnos y presupuesto

Comienza el jugador A. El jugador activo dispone de 30 segundos para preparar una ruta de hasta 8 acciones ejecutadas y como máximo un disparo. El rival no edita su ruta durante ese turno. Cada jugador conserva su propia secuencia entre turnos.

Finalizar y ejecutar valida toda la ruta y la realiza paso a paso, con un intervalo de 0,25 segundos. Durante la ejecución se bloquea la edición, se indica el paso activo y se mantiene la cámara del mismo jugador. Luego se alterna el turno.

Una ruta vacía, una repetición sin contenido o una ruta fuera del presupuesto no se ejecutan. Una ruta demasiado larga permanece en el editor para corregirla; no se recorta automáticamente. Pasar turno y el vencimiento del reloj omiten la ejecución y entregan el turno al rival.

## Acciones visuales

| Acción | Resultado |
|---|---|
| Arriba | Intentar avanzar una casilla al norte de la grilla. |
| Abajo | Intentar avanzar una casilla al sur. |
| Izquierda | Intentar avanzar una casilla al oeste. |
| Derecha | Intentar avanzar una casilla al este. |
| Esperar | Mantener posición y consumir un paso. |
| Repetir | Ejecutar su contenido entre 1 y 8 veces; admite anidamiento. |
| Disparar | Calcular una casilla objetivo a partir del radar y resolver impacto o fallo. |

Las direcciones son fijas respecto del laberinto, independientes de la orientación del personaje y de la cámara. Al intentar moverse se actualiza la orientación, incluso si una pared o el límite bloquean el avance. Un intento bloqueado consume su paso. Los jugadores no se bloquean entre sí y pueden coincidir en una casilla.

Repetir no consume un paso adicional como contenedor: cuentan todas sus acciones expandidas. Por ejemplo, Repetir 3 [Arriba, Derecha] consume seis acciones. Repetir 2 [Disparar] resulta inválido porque excede el disparo permitido.

## Panel y edición

La bandeja inferior reúne la paleta y la secuencia. Los iconos se añaden por arrastre o clic; se pueden ordenar y eliminar. La fila tiene desplazamiento horizontal con barra, flechas y rueda. Al arrastrar cerca de un borde se desplaza automáticamente.

Seleccionar Repetir abre un editor de cantidad y contenido. Mantener un arrastre sobre una repetición visible la abre para insertar acciones. Se pueden trasladar bloques entre filas; no se permite introducir un bloque dentro de sí mismo o de un descendiente. Vaciar limpia la lista que se está editando.

El editor admite hasta 64 bloques guardados y hasta seis niveles interiores de repetición. El presupuesto de ejecución sigue siendo de ocho pasos. El disparo permite hasta 32 ajustes ordenados de radar; esos ajustes son configuración de una acción y no movimientos del personaje.

## Poderes

Los poderes se recogen al pasar por su casilla y quedan pendientes hasta el siguiente turno propio. Los poderes activos que no se utilizan vencen al terminar ese turno, ya sea por ejecución, pase o tiempo. Los recogidos durante una ruta permanecen pendientes para el turno siguiente.

**Phase:** cubo celeste. Se habilita en el movimiento elegido. Solo se consume cuando ese movimiento atraviesa un muro y el poder está disponible. No permite salir de la grilla. En un camino libre se avanza normalmente sin consumirlo; si falta el poder y existe un muro, el avance queda bloqueado.

**Radar verdadero:** esfera magenta. Se habilita en un disparo elegido y consulta la posición y orientación actuales del rival. Esa consulta consume el poder. Si se solicita sin tenerlo, se usa el radar histórico y se informa la sustitución.

## Radar, disparo y salud

El radar normal usa la posición y orientación anteriores al último turno ejecutado del rival. Al comienzo usa la posición inicial. Pasar turno o agotar el tiempo sin ejecutar no actualiza ese historial.

El disparo parte de esa referencia. Los ajustes pueden girar su orientación o desplazar la casilla objetivo hacia adelante, atrás, izquierda o derecha respecto de la orientación registrada. Se aplican en el orden de sus fichas. No trasladan al personaje.

El ataque acierta únicamente si la casilla calculada coincide con la casilla actual del rival. No se limita por distancia ni por paredes. Un objetivo fuera del mapa produce un fallo. Tanto acertar como fallar consumen el único disparo del turno.

Cada personaje comienza con 100 puntos de vida; un impacto resta 20. Se muestra una barra sobre el modelo y la vida del jugador activo en el panel.

## Cámara, pausa y modos

Siempre hay una cámara habilitada que sigue al jugador activo y ocupa toda la pantalla. El panel muestra su identidad, tiempo o progreso de ejecución, salud y disponibilidad de poderes.

Escape pausa la partida y el tiempo de planificación. Los controles de movimiento se ocultan durante la pausa. Desde Opciones o Instrucciones, Escape vuelve a Pausa sin reanudar el reloj. Reanudar cierra los menús y continúa la partida.

Continuo alterna las rutas guardadas de ambos jugadores. Se detiene cuando la siguiente ruta está vacía o es inválida. Detener permite terminar la secuencia ya iniciada y evita la continuación automática.

## Final y navegación

Entrar en la casilla de salida o llevar al rival a cero de vida finaliza la partida inmediatamente. Se muestra el ganador y se detiene el tiempo. El resultado permite reiniciar o volver al menú principal. No se necesita avanzar fuera del borde para ganar.

El menú ofrece Jugar, Opciones, Instrucciones y Salir. El volumen de la música se configura mediante un deslizador. Se mantienen las músicas del proyecto original: “Lets go back”, de Patrick Patrikios, para el menú, y “Soaring”, de The Grey Room / Golden Palms, para el juego.

## Plataforma y validación

Proyecto Unity 6000.2.2f1 con C#, URP, Unity UI, TMP e Input System. Entrega jugable para Windows de 64 bits. No hay multijugador en red ni guardado/carga de partidas en curso; la preferencia de tamaño se conserva.

La entrega aprobó las baterías descritas en `verificacion/README.md`. El presupuesto de acciones y el límite de disparos son decisiones de diseño implementadas, no parámetros de equilibrio validados mediante partidas prolongadas entre personas.
