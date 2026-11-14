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

**PlayMode**
- `PlayerMovementPlayTests` (`InputTestFixture`): se crea un teclado virtual, se mantiene W 0.5 s y se comprueba que el jugador avanza más de 1 m.
- `StationStairsPlayTests`: carga la escena generada `Prototype` (hay que haberla regenerado), lleva al jugador al pie de cada escalera de la estación y lo mueve con su CharacterController (sin input) hasta el andén; comprueba que llega a 9.6 m. Si cambian las medidas de la estación en `PrototypeSceneBuilder.Train.cs`, actualiza sus constantes.
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
