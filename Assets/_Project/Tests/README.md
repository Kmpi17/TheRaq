# Tests

| Carpeta | asmdef | Plataforma |
|---|---|---|
| `EditMode/` | `GTAChiraq.Tests.EditMode` | Solo editor, sin Play |
| `PlayMode/` | `GTAChiraq.Tests.PlayMode` | Play, con input simulado |

## Qué se prueba

**EditMode**
- `MovementMathTests`
  - La dirección sigue el yaw de la cámara.
  - La diagonal se limita a longitud 1.
  - El salto alcanza la altura pedida.
- `VehicleEntryTests`
  - Al entrar se desactiva el control a pie y se toma el volante.
  - Al salir se restaura el control y el jugador aparece en el `ExitPoint`.
  - No se puede entrar a un coche ocupado.
  - `FindNearest` respeta el rango y la ocupación.
  - En EditMode no se ejecutan `Awake`/`Update`, así que se llama directamente a los métodos públicos.
- `TrainMotionTests`
  - El tren se para exactamente en la estación.
  - Nunca supera la velocidad máxima.
  - Después de la estación llega al final, espera y vuelve a empezar.
  - Si empieza pasada la estación, no vuelve atrás.
- `WeatherTests`
  - A mediodía y en seco, `DayCycleMath.Look` da la luz original del mundo (sol, ambiente, niebla 25–260 m) y las farolas están apagadas.
  - 20 minutos reales son un día (una hora de juego, 50 s).
  - De noche el ambiente y la niebla son oscuros, la luz es la luna, las farolas se encienden y la lluvia no brilla.
  - La luz no da saltos en ningún momento del día (amanecer y atardecer suaves).
  - La lluvia oscurece y cierra la niebla; la nieve la blanquea y la cierra más.
  - El tiempo nunca repite lluvia ni nieve seguidas, las duraciones están en su rango y a la larga casi siempre está seco (60–80 %).
  - Lluvia y nieve entran en 40 s.
- `HighwayLoopTests`
  - Los coches de la autopista avanzan y, al pasar el final, vuelven al principio.
  - Una distancia inicial mayor que el recorrido se ajusta, y nunca se sale de [0, largo).
  - Un recorrido de largo 0 se queda en 0.
- `CharacterSystemsTests`
  - El chaleco absorbe el 70 % hasta gastarse; las caídas no las para, la cura no pasa del máximo y muerto no se cura.
  - La energía deja agotado a 0 hasta el 30 %, se recupera solo tras el retraso y más rápido con la bebida.
  - `Accelerate` llega a la velocidad pedida y frena más fuerte de lo que acelera. Se gira más despacio a más velocidad, y las caídas de 4 m no hacen daño.
  - Clasificación de la escalada por altura y grosor.
  - La dispersión es mayor desde la cadera y moviéndose, y `Scatter` no se sale del cono.
- `InventoryTests`
  - Los montones se llenan antes de usar casillas nuevas.
  - Con el inventario lleno solo entra lo que cabe, y los ids desconocidos no entran.
  - `Remove` quita de varias casillas y vacía las que se quedan a 0.
  - Las armas se listan una vez, en el orden de las casillas.
  - `Changed` solo salta si algo cambia.
- `MusicCareerTests`
  - Una pulsación en el golpe, dentro del margen, cuenta como acierto. Durante la cuenta de entrada o fuera del margen, es un fallo.
  - El mismo golpe no se puede acertar dos veces.
  - Una sesión perfecta saca 1, y cada fallo resta medio acierto.
  - La fama va de 5 a 125 según la nota; los rangos suben con la fama, y en el último el progreso es 1.

**PlayMode**
- `PlayerMovementPlayTests` (`InputTestFixture`): se crea un teclado virtual, se mantiene W 0.5 s y se comprueba que el jugador avanza más de 1 m. Agacharse: C lo agacha (CharacterController más bajo), andando va a ~1.7 m/s y C otra vez lo levanta; con un techo a 1.4 m no puede levantarse (`SetCrouching(false)` devuelve `false`) y sin él, sí. Las teclas de estos tests van **a la cola** (`Press(key, queueEventOnly: true)`, avanzando antes `currentTime`) y se esperan dos fotogramas: así las procesa el bucle de Unity antes de los `Update`, como en juego, y `WasPressedThisFrame` se ve; con `Press` directo, el primer toque a veces se perdía.
- `NpcPlayTests`: un suelo plano con malla de navegación hecha en el momento (`NavMeshSurface.BuildNavMesh`), un jugador en la capa 2 y NPC sin cuerpo. El de Dunmore en su barrio primero mira o encara y luego dispara; el de Kessler no ataca; uno de Larkin fuera de su barrio (cocheras) no ataca; un civil huye de un disparo; disparar a uno de Ash Block le da rencor y pasa a pelear. Van a `Time.timeScale` 4.
- `NpcCapturePlayTests` (**explícito**): capturas de los NPC en la escena real (de día en la zona de reunión y por la calle, la cancha de Kessler, de noche en la tienda de licores y los locales cerrados, el de Dunmore encarándote y una incursión) en `ingame/npc/<GTACHIRAQ_CAPTURA>/`, en PPM, con un `.txt` por vista con la posición y el estado de cada NPC. Apaga la aparición automática y pone a mano los NPC, con el jugador cerca (a más de 140 m la población los quita).
- `TestScenes.Fresh()`: los tests que montan su propio mundo (movimiento, escalar, NPC) empiezan en una escena vacía (`[UnitySetUp]`), porque `BackPorchPlayTests` y `StationStairsPlayTests` dejan cargada la escena prototipo, con su jugador y sus NPC.
- `WeatherSystemPlayTests`: un `WeatherSystem` con sol y una farola (`NightLight`) empieza a mediodía con la luz original y la farola apagada; a las 23:00 la farola se enciende más fuerte y la luz baja; con nieve forzada la niebla se cierra y `Describe()` dice «Nieve».
- `BackPorchPlayTests`: en la escena `Prototype`, lleva al jugador por la escalera en zigzag del primer `PorcheTrasero` (con su CharacterController, sin input) hasta la puerta del tercer piso y comprueba que llega a 7.07 m. Usa las medidas de `BackPorch` (`PrototypeSceneBuilder.Houses.cs`).
- `StationStairsPlayTests`: carga la escena generada `Prototype` (hay que haberla regenerado), lleva al jugador al pie de cada escalera de la estación y lo mueve con su CharacterController (sin input) hasta el andén; comprueba que llega a 9.6 m. Si cambian las medidas de la estación en `PrototypeSceneBuilder.Train.cs`, actualiza sus constantes.
- `AnimationCapturePlayTests` (PlayMode, **explícito**: no corre con el resto, hay que filtrarlo por nombre): no comprueba nada, es el **grabador de animaciones**. Carga `Prototype`, monta un plató en el aire (suelo, muro alto de 2.3 m, muro bajo de 1 m y valla de 1.2 m), maneja al jugador con teclado y ratón simulados (quieto, correr, esprintar, frenar, quieto un buen rato (cambio de peso y mirar alrededor), correr en curva (la cámara gira sola: se ve la inclinación), agachado quieto, andando, de lado y apuntando, tres puñetazos, pistola en reposo, apuntar, apuntar andando hacia delante, de lado y hacia atrás, disparar, correr con la pistola, cobertura alta y baja moviéndose de lado y asomándose, saltar la valla y escalar el muro) y guarda fotogramas desde un lado, desde 3/4 por delante y con la cámara del juego en `ingame/animaciones/<carpeta>/` (carpeta en la variable de entorno `GTACHIRAQ_CAPTURA`). Los guarda en PPM, porque el proyecto no tiene el módulo de conversión de imágenes en tiempo de juego; se juntan en hojas PNG por acción fuera de Unity. Escribe en el registro una línea `[Grabador]` por acción (posición, velocidad, entrada, si apunta, si está bloqueado) para ver si la acción se hizo de verdad. Con `InputTestFixture`, las pulsaciones avanzan antes `currentTime` y van de una en una por fotograma: si no, el sistema de entrada descarta eventos y las teclas se quedan pulsadas.
  - Requiere que `com.unity.inputsystem` esté en `testables` de `Packages/manifest.json`.
  - Hay que llamar a `GameInput.Reset()` en Setup y en TearDown.

## Ejecutar
En el editor: **Window > General > Test Runner**.

Desde la terminal, con el editor de este proyecto **cerrado**:
```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe"
& $unity -batchmode -projectPath E:\gtachiraq -runTests -testPlatform EditMode -testResults results-editmode.xml
& $unity -batchmode -projectPath E:\gtachiraq -runTests -testPlatform PlayMode -testResults results-playmode.xml
# Un solo test o clase:
& $unity -batchmode -projectPath E:\gtachiraq -runTests -testPlatform EditMode -testFilter "GTAChiraq.Tests.VehicleEntryTests" -testResults results.xml
```
Los resultados se guardan en formato NUnit XML. Los `results*.xml` están en `.gitignore`.

**Con el editor abierto**: se copia el proyecto (sin `.git`, `Temp`, `Logs` ni `ingame`; con `Library`, para no reimportar) a otra carpeta y se lanza Unity en batchmode sobre la copia; el editor abierto no se toca. Así se regenera la escena, se capturan zonas, se pasan los tests y se graban las animaciones:
```powershell
robocopy E:\gtachiraq <copia> /MIR /XD .git Temp Logs obj ingame img mapa UserSettings
& $unity -batchmode -projectPath <copia> -executeMethod GTAChiraq.Editor.IngameScreenshots.BuildAndCapture -capturar personaje -cambio "..." -quit -logFile build.log
$env:GTACHIRAQ_CAPTURA = "v02_algo"
& $unity -batchmode -projectPath <copia> -runTests -testPlatform PlayMode -testFilter "GTAChiraq.Tests.AnimationCapturePlayTests" -testResults anim.xml -logFile anim.log
```
Luego se traen a `ingame/` las capturas que valgan. Después de tocar código, basta con volver a copiar `Assets/_Project`.

**Fallos conocidos (2026-11-15)**: `CharacterSystemsTests.StaminaExhaustsAtZeroAndRecoversAtThirty` (la energía no deja de estar agotada al 30 %), los tres de `ClimbPlayTests` (`Available` sale `None` delante del muro, la valla y el coche, también lanzados solos; en la escena real sí se salta y se escala) y `PlayerMovementPlayTests.HoldingW_MovesPlayerForward` cuando corre detrás de los tests que cargan `Prototype` (solo, pasa: la escena cargada se queda y le estorba).

- `ClimbPlayTests` (PlayMode): delante de un muro de 1 m y 0.3 m de grueso, Espacio salta por encima y el personaje acaba al otro lado, en el suelo. Lo mismo con una valla de 1.1 m y 3 cm de grueso (como las del generador) y con un coche de 2 m de ancho (`SlideOver`, colliders en la raíz con `SolidVisual`).
- `AnimationMathTests` (EditMode): quieto, la pose es neutra; las piernas van en oposición (la derecha repite a la izquierda medio ciclo después) y cada brazo contra su pierna; las curvas siguen la marcha real (andando, la cadera se flexiona 22–32° y se extiende 6–14°, la rodilla amortigua 12–24° al cargar y se dobla 55–70° en el vuelo) y el tobillo da la inclinación del pie pedida; `Cyclic` pasa por las claves y da la vuelta; `WeightShift` lleva la pelvis a la pierna cargada; trotar y esprintar tienen más amplitud, codos más doblados (~90° trotando) y más inclinación; una vuelta de fase por zancada y cadencias reales (150–180 pasos/min trotando, 180–230 esprintando); el IK de dos huesos llega al objetivo (la pistola al apuntar) con el codo hacia el polo y se estira hacia uno lejano.
- `HomeTests` (EditMode): ropa de inicio, ponerse y quitarse prendas (solo cabeza, cara y cuello pueden ir sin nada; se empieza sin gafas ni cadena), guardar y leer la ropa (y arreglar un texto roto; una partida vieja también tiene las prendas nuevas), pasar montones a la caja fuerte (lo que quepa), dinero y contenido de la caja tras guardarlos, y `Interactable.FindNearest`.
- `VehicleLockTests` (EditMode): forzar la cerradura abre solo tras el tiempo entero, soltar pierde lo forzado; `SolidVisual` y `NoCover`.
