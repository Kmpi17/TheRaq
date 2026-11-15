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
- `PlayerMovementPlayTests` (`InputTestFixture`): se crea un teclado virtual, se mantiene W 0.5 s y se comprueba que el jugador avanza más de 1 m.
- `BackPorchPlayTests`: en la escena `Prototype`, lleva al jugador por la escalera en zigzag del primer `PorcheTrasero` (con su CharacterController, sin input) hasta la puerta del tercer piso y comprueba que llega a 7.07 m. Usa las medidas de `BackPorch` (`PrototypeSceneBuilder.Houses.cs`).
- `StationStairsPlayTests`: carga la escena generada `Prototype` (hay que haberla regenerado), lleva al jugador al pie de cada escalera de la estación y lo mueve con su CharacterController (sin input) hasta el andén; comprueba que llega a 9.6 m. Si cambian las medidas de la estación en `PrototypeSceneBuilder.Train.cs`, actualiza sus constantes.
- `AnimationCapturePlayTests` (PlayMode, **explícito**: no corre con el resto, hay que filtrarlo por nombre): no comprueba nada, es el **grabador de animaciones**. Carga `Prototype`, monta un plató en el aire (suelo, muro alto de 2.3 m, muro bajo de 1 m y valla de 1.2 m), maneja al jugador con teclado y ratón simulados (quieto, correr, esprintar, frenar, tres puñetazos, pistola en reposo, apuntar, apuntar andando hacia delante, de lado y hacia atrás, disparar, correr con la pistola, cobertura alta y baja moviéndose de lado y asomándose, saltar la valla y escalar el muro) y guarda fotogramas desde un lado, desde 3/4 por delante y con la cámara del juego en `ingame/animaciones/<carpeta>/` (carpeta en la variable de entorno `GTACHIRAQ_CAPTURA`). Los guarda en PPM, porque el proyecto no tiene el módulo de conversión de imágenes en tiempo de juego; se juntan en hojas PNG por acción fuera de Unity. Escribe en el registro una línea `[Grabador]` por acción (posición, velocidad, entrada, si apunta, si está bloqueado) para ver si la acción se hizo de verdad. Con `InputTestFixture`, las pulsaciones avanzan antes `currentTime` y van de una en una por fotograma: si no, el sistema de entrada descarta eventos y las teclas se quedan pulsadas.
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
- `AnimationMathTests` (EditMode): quieto, la pose es neutra; las piernas van en oposición y cada brazo contra su pierna; al esprintar, más amplitud, codos más doblados y más inclinación; una vuelta de fase por zancada; el IK de dos huesos llega al objetivo (la pistola al apuntar) con el codo hacia el polo y se estira hacia uno lejano.
- `HomeTests` (EditMode): ropa de inicio, ponerse y quitarse prendas (solo cabeza, cara y cuello pueden ir sin nada; se empieza sin gafas ni cadena), guardar y leer la ropa (y arreglar un texto roto; una partida vieja también tiene las prendas nuevas), pasar montones a la caja fuerte (lo que quepa), dinero y contenido de la caja tras guardarlos, y `Interactable.FindNearest`.
- `VehicleLockTests` (EditMode): forzar la cerradura abre solo tras el tiempo entero, soltar pierde lo forzado; `SolidVisual` y `NoCover`.
