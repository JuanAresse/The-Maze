FLUJO GENERAL DEL PROYECTO THEMAZE (Unity - C#)

1. Inicio y Generación del Laberinto
   - El juego inicia, y el script principal encargado de la generación del escenario es "MazeGenerator".
   - Este script construye una grilla de "MazeCell" instanciando cada celda del laberinto. Define las entradas, salidas y distribuye los powerups (como “Phase” y “TrueRadar”) en celdas aleatorias.
   - Además, MazeGenerator crea los personajes ("Character") en las posiciones iniciales y asigna las cámaras a cada jugador.

2. Interfaz y Control de Menús
   - El script "ControlMenu" gestiona los paneles de menú, opciones, instrucciones, pausa y navegación entre escenas. Interactúa con la UI y detecta eventos como presionar “ESC” para pausar.

3. Movimiento y Comandos
   - Los jugadores pueden ingresar scripts de movimiento usando la UI. Estos scripts son interpretados por "MovementParser", que traduce comandos de texto en acciones (mover, disparar, repetir, etc.).
   - El "TurnBasedManager" gestiona los turnos: determina cuándo puede editar el jugador, ejecuta los movimientos según el script, alterna turnos y controla el tiempo límite para editar.

4. Acciones en el Laberinto
   - Cada vez que un "Character" se mueve, se actualiza su posición y facing. Si entra en una celda con una "ExitZone", el juego detecta que el jugador salió exitosamente.
   - Los personajes pueden recoger "Powerup", que les permite habilidades especiales en su siguiente turno.
   - Los personajes tienen barras de vida administradas por "PlayerHealth". Si reciben daño y su vida llega a cero, el juego termina.

5. Cámaras y Visuales
   - Cada jugador tiene una cámara propia gestionada por "CamaraTerceraPersona", la cual sigue al player correspondiente desde una perspectiva diagonal suave.
   - El script "CameraController" ajusta la cámara principal para centrar y encuadrar todo el laberinto en pantalla.
   - "LogCameraTargets" sirve para depuración y registrar información de cámaras y render textures en la escena.

6. Game Over y Feedback
   - Al finalizar la partida (salida o muerte de un personaje), "GameOverManager" muestra la interfaz de fin de juego, indicando el ganador, y permite reiniciar o volver al menú principal.
   - "ControlVolumen" gestiona el volumen de la música y lo sincroniza con la UI.
   - Toda la lógica interactúa con la UI mediante paneles, botones y campos visuales.

-------------------------------------------------
## EXPLICACIÓN DE CADA SCRIPT

1. MazeGenerator
   - Construye el laberinto instanciando MazeCell como grilla 2D.
   - Define la entrada, salida, y asigna celdas para spawneo de personajes y powerups.
   - Asocia las cámaras y el manager de turnos a los personajes.
   - Llama a TurnBasedManager para iniciar la gestión del flujo de turnos.

2. MazeCell
   - Cada objeto representa una celda del laberinto; controla paredes (activas/inactivas).
   - Posee métodos para limpiar o consultar el estado de sus muros, y marca si fue visitada.

3. Character
   - Representa a cada jugador; maneja posición, facing, movimiento, visuales, y recolección de powerups.
   - Al moverse, consulta sus celdas y notifica al TurnBasedManager.
   - Puede ejecutar comandos desde la UI: moverse, teletransportarse, disparar a otra celda (usando radar), y recoger powerups.
   - Controla el facing y el modelo visual que lo representa.

4. TurnBasedManager
   - Gestiona la lógica de turnos entre los dos jugadores.
   - Permite editar instrucciones/script para cada personaje, ejecuta los movimientos, alterna turnos, y determina el final de la partida por salida o por vida.
   - Administra la UI para cada jugador (panel de comandos, botones de ejecución/continuo).
   - Controla el tiempo de edición y la interacción con los powerups y daño.

5. Powerup
   - Se adjunta a objetos de powerup; permite que el player los recoja (Fase, Radar verdadero).
   - Notifica al Character y al manager y se autoelimina al ser recogido.
   - Permite a los jugadores atravesar muros (“Phase”) o ver la posición/oponente exacta (“TrueRadar”).

6. PlayerHealth
   - Adjunta a los personajes, controla la vida actual y máximo.
   - Muestra la barra de vida como UI en el mundo.
   - Si el life baja a 0, avisa al manager que el personaje ha perdido.

7. MovementParser
   - Convierte los scripts de texto en listas de acciones Unity que el Character puede ejecutar (incluye operaciones como Repeat, Shoot, etc.).

8. ExitZone
   - Zona específica de salida en el laberinto.
   - Detecta cuando un Character entra en la salida y lo notifica al manager, finalizando la partida.

9. ControlMenu
   - Gestiona la UI de menús, opciones y pausa.
   - Se encarga de cargar escenas, activar/desactivar paneles, y navegación en la UI.

10. ControlVolumen
    - Conecta el slider de volumen de la UI con el AudioSource de la música de fondo.
    - Permite alterar el volumen del juego desde la interfaz.

11. CameraController
    - Centra y ajusta automáticamente la cámara para visualizar el laberinto completo.
    - Soporta modos ortográficos y perspectiva y suavizado del movimiento.

12. LogCameraTargets
    - Script para depuración: muestra información sobre cámaras y texturas renderizadas.

13. GameOverManager
    - Muestra la interfaz final de la partida (quién ganó y quién perdió).
    - Permite reiniciar la partida o volver al menú, mostrando los resultados mediante la UI.
    - Crea paneles y botones si no existen.

14. CamaraTerceraPersona
    - Controla el seguimiento de la cámara al jugador, manteniendo siempre un ángulo y distancia fija y suave.

-------------------------------------------------
## RESUMEN DE INTERACCIONES

- El ciclo principal es: MazeGenerator crea el entorno y personajes, TurnBasedManager coordina turnos y ejecuciones, y los scripts de movimiento son interpretados y ejecutados en los Character.
- El juego finaliza si un player sale por la ExitZone o pierde toda su vida (PlayerHealth). Esto lo gestiona GameOverManager.
- Todos los scripts de UI y volumen ofrecen un feedback claro para el jugador y un control total de la experiencia.
