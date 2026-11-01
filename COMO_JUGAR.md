# The Maze: controles visuales

Abrir `ejecutable_actualizado/theMaze.exe` y pulsar **Jugar**. Elegir **4×4**, **6×6** o **8×8** y confirmar **Iniciar partida**. 6×6 es la opción inicial recomendada; se recuerda la última elección. Reiniciar conserva el tamaño elegido y genera otro laberinto.

## Planificar y ejecutar

Solo se muestra la cámara del jugador activo y una bandeja inferior. La cámara se conserva durante toda su ejecución y cambia al pasar el turno.

- Arrastrar los iconos a **Secuencia** o pulsarlos para añadirlos. La secuencia se conserva para el siguiente turno del mismo jugador.
- Arrastrar los bloques para cambiar el orden. La marca verde indica dónde se insertarán. Se pueden trasladar entre la ruta principal y las repeticiones.
- Usar la barra horizontal, las flechas o la rueda sobre la secuencia para recorrer rutas largas. Al arrastrar cerca de un borde, la secuencia se desplaza automáticamente.
- Pulsar **×** para eliminar un bloque; **Vaciar** vacía la lista que se está editando.
- Hay **30 segundos para planificar**, un máximo de **8 acciones ejecutadas** y **1 disparo por turno**. Las repeticiones cuentan todos sus pasos. Una ruta que supera el límite se conserva para corregirla, pero no se ejecuta ni se recorta.
- **Finalizar y ejecutar** realiza la secuencia completa, con 0,25 segundos entre acciones. La acción actual se resalta y se desplaza a la vista.
- **Pasar turno** omite la ejecución. Al agotarse el tiempo también cambia el turno sin ejecutar.
- **Continuo** alterna las rutas guardadas de A y B; se detiene si la siguiente ruta está vacía o es inválida. **Detener** permite terminar la ejecución que ya empezó.
- **Escape** pausa la ejecución y el tiempo de edición. Desde Opciones o Instrucciones vuelve al menú de pausa y mantiene el reloj detenido. **Reanudar** (o Escape desde Pausa) cierra los menús y continúa la partida. Los controles de movimiento se ocultan durante la pausa.

## Acciones y poderes

Los cuatro movimientos son direcciones fijas del laberinto, independientes de la cámara. Una pared o límite bloquea el paso y consume la acción. **Esperar** consume una acción permaneciendo quieto.

Seleccionar **Repetir** abre su editor, donde se elige una cantidad de 1 a 8 y se añaden acciones. Se pueden anidar repeticiones. Mantener un bloque arrastrado sobre una repetición la abre para insertar dentro. El botón **Repetición** permite volver al bloque padre.

Los poderes se recogen al pasar por su casilla, están disponibles al comenzar el próximo turno propio y vencen al terminar ese turno por ejecución, tiempo o pase. Los poderes recogidos durante la ejecución quedan pendientes para el próximo turno.

- **Phase**, cubo celeste: seleccionar un movimiento y activar **Phase en esta acción**. Solo se consume si ese movimiento atraviesa un muro; no permite salir del mapa. Se puede decidir en qué movimiento de la ruta usarlo.
- **Radar verdadero**, esfera magenta: seleccionar el disparo y activar **Radar verdadero** para consultar la posición y orientación actuales del rival. Si el poder no está disponible se informa y se usa el radar normal.

## Disparo, vida y victoria

El radar normal utiliza la posición y orientación anteriores al último turno ejecutado del rival. Al inicio usa su posición inicial. Los ajustes del disparo desplazan esa casilla objetivo o giran su orientación de referencia; no mueven al personaje. Los botones **Girar radar**, **Radar adelante/atrás/izquierda/derecha** añaden ajustes ordenados, y pulsar un ajuste lo elimina.

Un disparo que coincide con la casilla actual del rival resta **20 de sus 100 puntos de vida**. Se conserva el funcionamiento de disparo por casilla, sin restricción de distancia o paredes, limitado a un disparo por turno. Un fallo también consume el disparo.

Se gana alcanzando la casilla señalizada **SALIDA** o llevando la vida del rival a cero. Al terminar se puede reiniciar o volver al menú principal.

## Archivos y compilación

Las rutas de esta sección se refieren a la carpeta principal de la entrega.

`theMaze/` contiene el proyecto actualizado de Unity 6000.2.2f1. `theMaze.zip` contiene una copia limpia del mismo proyecto. `ejecutable_actualizado/` contiene la versión final correspondiente a esta entrega. Los resultados de pruebas están en `verificacion/`.

`COMPILAR.ps1` compila una versión final del proyecto en una copia nueva, sin comas en la ruta para evitar un fallo de UnityLinker. Publica la carpeta completa en `ejecutable_actualizado/` y conserva la versión anterior en un respaldo. Cada compilación usa fuentes y salida nuevas para no conservar archivos eliminados. Las pruebas de reglas se ejecutan antes de compilar.

Para generar una versión con pruebas de integración, usar `COMPILAR.ps1 -Desarrollo`. Con `-SoloCompilar` se conserva la entrega actual y se devuelve la ubicación de la compilación de prueba. La versión de desarrollo admite:

- `-maze-smoke-test`: comprueba las mecánicas y guarda resultados y capturas en `verificacion/` junto al ejecutable.
- `-maze-regression-test`: comprueba pausa y submenús, editores, arrastre, desplazamiento, presupuestos y acceso al menú en tres resoluciones. También juega 24 recorridos hasta la salida. Guarda los resultados en `verificacion_regresiones/`; `-maze-regression-output "ruta"` permite elegir otra carpeta.

Las capturas de desarrollo muestran una pequeña marca de Unity que no aparece en la versión final. Los ejecutores de pruebas y la función de teletransporte de diagnóstico quedan excluidos de la versión final.
