# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

Juego de mundo abierto 3D en tercera persona (género tipo GTA San Andreas), ambientado en el Southside de Chicago en los años 2010 y en la escena drill. Motor: **Unity 6 (LTS) + URP**. El usuario habla español: código y comentarios en español donde ya lo estén, identificadores en inglés.

Contenido: personajes y lugares **ficticios** inspirados en la época. No se usan nombres, caras ni parecidos de raperos o personas reales (sobre todo fallecidos o víctimas reales), ni nombres reales de calles o bloques si identifican hechos reales. Tampoco se recrean assets, personajes ni mapas de GTA.

Estética por defecto: low-poly con aire de PS2, invierno de Chicago (cielo gris, niebla, nieve sucia, farolas de sodio naranjas). La paleta y los colores están en `Assets/_Project/Editor/PrototypeSceneBuilder.cs`.

## Mundo: `ESTRUCTURAS.md`

`ESTRUCTURAS.md` (en la raíz) es el cuaderno del mundo: estilo, zonas, edificios, props y lo que el usuario pide sobre cómo debe sentirse cada sitio. Cada vez que el usuario diga algo del mundo (una petición, una queja, una preferencia de estilo) o se añada o cambie una zona o estructura, **anótalo ahí**: actualiza la sección que toque y añade una línea con fecha en *Peticiones y comentarios*.

**Regla obligatoria (petición del usuario):** siempre que se hable de estructuras (edificios, zonas, calles, props, el mapa), aunque sea solo una pregunta, **lee antes** los `.md` relacionados para tener el contexto: como mínimo `ESTRUCTURAS.md` y `Assets/_Project/Editor/README.md` (cómo se generan en código), y el README de cualquier otra carpeta que toque. **Mira el plano actual `mapa/mapa.png`** (vista cenital con coordenadas; es la referencia para decidir dónde va cada cambio) y **las imágenes de referencia de `img/estructures/`** (y `img/`). Las `.jfif` hay que copiarlas como `.jpg` al scratchpad, y las `.webp` convertirlas a `.png` con Pillow, para poder verlas; si hay alguna que no esté en la tabla *Referencias visuales* de `ESTRUCTURAS.md`, añádela con lo que se toma de ella. Si de la conversación sale una **regla nueva** (de estilo, de diseño o de forma de trabajar), escríbela en *Reglas generales* de `ESTRUCTURAS.md` (o aquí, en `CLAUDE.md`, si es una regla de forma de trabajar) y en el README afectado si cambia cómo está hecho algo.

**Plano del mapa:** cada vez que cambie la disposición del mundo (edificios, calles, zonas o props fijos), actualiza las constantes de `mapa/generar_mapa.py` para que coincidan con `PrototypeSceneBuilder*.cs`, regenera la imagen con `python mapa/generar_mapa.py` y mírala antes de dar el trabajo por terminado. Las casas del barrio de casas salen de `mapa/edificios_casas.json`, que se escribe al construir la escena en Unity: después de regenerar la escena, vuelve a ejecutar el script.

## Estructura y arquitectura

**Para ubicarte rápido, lee primero los README.** `README.md` en la raíz tiene el mapa del código y el flujo en tiempo de juego. Cada carpeta de `Assets/_Project/` (Scripts/<Sistema>/, Editor/, Tests/) tiene su `README.md` con la API, los parámetros y las limitaciones. **Si cambias o añades una implementación, actualiza el README de esa carpeta y la tabla del README raíz.**

Todo el código propio está en `Assets/_Project/`, en el namespace `GTAChiraq`, con assembly definitions:
- `Scripts/` → `GTAChiraq.Runtime` (depende de `Unity.InputSystem`). Hay una subcarpeta por sistema (`Player/`, `Camera/`, `Vehicles/`, `Interaction/`, `Input/`, `World/`).
- `Editor/` → `GTAChiraq.Editor`, herramientas de editor.
- `Tests/EditMode/`, `Tests/PlayMode/` → asmdefs de test.

Piezas clave y cómo encajan:
- **Input**: `GameInput` define todas las acciones en código (no hay `.inputactions`). Se accede siempre con `GameInput.Actions`, un singleton perezoso. `GameInput.Reset()` lo destruye; los tests PlayMode con `InputTestFixture` deben llamarlo en Setup y en TearDown.
- **A pie**: `PlayerMovement` (CharacterController, movimiento relativo al yaw de la cámara). La matemática pura está en `MovementMath`, para testearla en EditMode.
- **Coches**: `VehicleController` (Rigidbody + WheelColliders, tracción trasera). Solo lee input si `HasDriver`; sin conductor queda frenado. Usa `Rigidbody.linearVelocity` (API de Unity 6).
- **Subir y bajar del coche**: `VehicleEntry` va en el jugador. Al entrar, emparenta el jugador al coche y desactiva su CharacterController, PlayerMovement y renderers (el GameObject sigue activo para poder leer F y salir). Además cambia la cámara a modo vehículo.
- **Cámara**: `ThirdPersonCamera` es una órbita propia (no Cinemachine) con perfiles a pie y en vehículo, y colisión por SphereCast. Ignora la capa 2 (`Ignore Raycast`), donde están el jugador y los coches.
- **Tren elevado**: `ElevatedTrain` (`Scripts/World/`) mueve los trenes decorativos de la avenida. La lógica pura está en `TrainMotion` (testeada en EditMode). Los trenes se generan fuera de `Mundo` porque `Mundo` se marca como estático.
- **Barrios de casas y negocios**: `PrototypeSceneBuilder.Houses.cs` construye el barrio de casas dos veces (`BuildHousesVariant`): en su sitio y girado 180° al oeste de la avenida (`HousesWestOrigin`). Sus tablas de rectángulos están en coordenadas locales del barrio. Delante de la copia oeste va la franja de negocios (`PrototypeSceneBuilder.Shops.cs`).
- **Manzana central**: `PrototypeSceneBuilder.Towers.cs` (torres, adosadas, aparcamientos). Se construye después del barrio de viviendas y del de casas porque reutiliza sus materiales (`LoadMat` y los estáticos de `PrepareHouses`/`PrepareVegetation`).
- **Escena**: `Prototype.unity` **se genera** desde el menú *GTAChiraq > Construir escena prototipo* (`PrototypeSceneBuilder`). Para cambiar el mundo de prueba, edita el generador y regenera la escena; no edites la escena a mano, porque se sobrescribe. El generador también crea el URP asset si no hay ninguno y los materiales en `Assets/_Project/Materials/`.

## Comandos

No hay CLI de build propio: se trabaja en el editor de Unity. Para ejecutar tests desde la terminal (con el editor de este proyecto **cerrado**):

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe"
& $unity -batchmode -projectPath E:\gtachiraq -runTests -testPlatform EditMode -testResults results-editmode.xml
& $unity -batchmode -projectPath E:\gtachiraq -runTests -testPlatform PlayMode -testResults results-playmode.xml
# Un solo test o clase:
& $unity -batchmode -projectPath E:\gtachiraq -runTests -testPlatform EditMode -testFilter "GTAChiraq.Tests.VehicleEntryTests" -testResults results.xml
```

En el editor: *Window > General > Test Runner*.

`Packages/manifest.json` marca `com.unity.inputsystem` como `testable`. Hace falta para `InputTestFixture` en los tests PlayMode.
