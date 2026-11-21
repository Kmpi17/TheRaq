# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

Juego de mundo abierto 3D en tercera persona (género tipo GTA San Andreas), ambientado en el Southside de Chicago en los años 2010 y en la escena drill. Motor: **Unity 6 (LTS) + URP**. El usuario habla español: código y comentarios en español donde ya lo estén, identificadores en inglés.

Contenido: personajes y lugares **ficticios** inspirados en la época. No se usan nombres, caras ni parecidos de raperos o personas reales (sobre todo fallecidos o víctimas reales), ni nombres reales de calles o bloques si identifican hechos reales. Tampoco se recrean assets, personajes ni mapas de GTA.

Estética por defecto: low-poly con aire de PS2, invierno de Chicago (cielo gris, niebla, nieve sucia, farolas de sodio naranjas). La paleta y los colores están en `Assets/_Project/Editor/PrototypeSceneBuilder.cs`.

## Mundo: `ESTRUCTURAS.md`

`ESTRUCTURAS.md` (en la raíz) es el índice del cuaderno del mundo: estilo, zonas, edificios, props y lo que el usuario pide sobre cómo debe sentirse cada sitio. El contenido está en `docs/estructuras/` (`reglas.md`, `mapa.md`, `referencias.md`, `catalogo.md`, `peticiones.md`, `pendiente.md` y una ficha por zona en `zonas/`). Cada vez que el usuario diga algo del mundo (una petición, una queja, una preferencia de estilo) o se añada o cambie una zona o estructura, **anótalo ahí**: actualiza la ficha o el archivo que toque y añade una línea con fecha en `docs/estructuras/peticiones.md`.

**Regla obligatoria (petición del usuario):** siempre que se hable de estructuras (edificios, zonas, calles, props, el mapa), aunque sea solo una pregunta, **lee antes** los `.md` relacionados para tener el contexto: como mínimo el índice `ESTRUCTURAS.md`, `docs/estructuras/reglas.md`, la ficha de la zona en `docs/estructuras/zonas/` y su archivo en `Assets/_Project/Editor/Docs/` (cómo se genera en código; índice en `Assets/_Project/Editor/README.md`), y el README de cualquier otra carpeta que toque. **Mira el plano actual `mapa/mapa.png`** (vista cenital con coordenadas; es la referencia para decidir dónde va cada cambio) y **las imágenes de referencia de `img/estructures/`** (y `img/`). Las `.jfif` hay que copiarlas como `.jpg` al scratchpad, y las `.webp` convertirlas a `.png` con Pillow, para poder verlas; si hay alguna que no esté en `docs/estructuras/referencias.md`, añádela con lo que se toma de ella. Si de la conversación sale una **regla nueva** (de estilo, de diseño o de forma de trabajar), escríbela en `docs/estructuras/reglas.md` (o aquí, en `CLAUDE.md`, si es una regla de forma de trabajar) y en el README afectado si cambia cómo está hecho algo.

**Capturas de fallos:** el usuario deja capturas de bugs de estructuras en `img/estrcuturebugs/`. Al arreglarlos, busca el mismo fallo en el resto de edificios y apunta la regla que salga en `docs/estructuras/reglas.md`.

**Plano del mapa:** cada vez que cambie la disposición del mundo (edificios, calles, zonas o props fijos), actualiza las constantes de `mapa/generar_mapa.py` para que coincidan con `PrototypeSceneBuilder*.cs`, regenera la imagen con `python mapa/generar_mapa.py` y mírala antes de dar el trabajo por terminado. Las casas del barrio de casas salen de `mapa/edificios_casas.json`, que se escribe al construir la escena en Unity: después de regenerar la escena, vuelve a ejecutar el script.

## Documentación separada por tema (regla del usuario, 2026-11-16)

Ningún `.md` crece sin límite, para que el contexto se recupere rápido leyendo solo lo que hace falta. Cada documento grande es un **índice corto** (qué hay y cuándo leer cada parte) más **un archivo por tema** en una carpeta al lado:
- `ESTRUCTURAS.md` → `docs/estructuras/` (y `zonas/`, una ficha por zona).
- `NPCS.md` → `docs/npcs/` (`peticiones`, `investigacion`, `diseno`, `facciones`).
- `Assets/_Project/Editor/README.md` → `Assets/_Project/Editor/Docs/` (uno por `PrototypeSceneBuilder.*.cs`).
- `Scripts/Player/README.md` → `Scripts/Player/Docs/`.

Lee el índice y luego solo los archivos del tema. Lo nuevo va al archivo del tema (o a uno nuevo, enlazado desde su índice), nunca al índice. Cuando un `.md` pase de unas 200 líneas o 20 KB, o mezcle temas que se consultan por separado, pártelo igual (en otra carpeta de `Scripts/`, con `Docs/` al lado de su README). Los `.md` dentro de `Assets/` llevan su `.meta` (lo crea Unity); se versionan los dos.

## Estructura y arquitectura

**Para ubicarte rápido, lee primero los README.** `README.md` en la raíz tiene el mapa del código y el flujo en tiempo de juego. Cada carpeta de `Assets/_Project/` (Scripts/<Sistema>/, Editor/, Tests/) tiene su `README.md` con la API, los parámetros y las limitaciones. **Si cambias o añades una implementación, actualiza el README de esa carpeta y la tabla del README raíz.**

Todo el código propio está en `Assets/_Project/`, en el namespace `GTAChiraq`, con assembly definitions:
- `Scripts/` → `GTAChiraq.Runtime` (depende de `Unity.InputSystem`). Hay una subcarpeta por sistema (`Player/`, `Camera/`, `Vehicles/`, `Interaction/`, `Input/`, `World/`).
- `Editor/` → `GTAChiraq.Editor`, herramientas de editor.
- `Tests/EditMode/`, `Tests/PlayMode/` → asmdefs de test.

Piezas clave y cómo encajan:
- **Input**: `GameInput` define todas las acciones en código (no hay `.inputactions`). Se accede siempre con `GameInput.Actions`, un singleton perezoso. `GameInput.Reset()` lo destruye; los tests PlayMode con `InputTestFixture` deben llamarlo en Setup y en TearDown.
- **A pie**: `PlayerMovement` (CharacterController, movimiento relativo al yaw de la cámara, con inercia y esprint que gasta energía). No hay salto: Espacio es `Climb` (`PlayerClimb`). C agacha (`PlayerMovement.Crouching`: CharacterController más bajo, más lento, mejor puntería; escalar, morir y el coche lo ponen de pie con `ForceStand`). Las habilidades que toman el control (escalar, cobertura `PlayerCover`, muerte en `PlayerVitals`) ponen `PlayerMovement.Locked`. La matemática pura está en `MovementMath`/`ClimbMath`, para testearla en EditMode. El modelo del jugador es de piezas con articulaciones (`BuildPlayerBody`) y lo anima `PlayerAnimator` en código (sin Animator ni clips; ciclo de paso e IK de brazo en `AnimationMath`). Si cambias los nombres de las piezas o articulaciones, cambia también `PlayerAnimator` y `PlayerOutfit`, que las buscan por nombre.
- **Combate e inventario**: `Health` en todo lo que se puede herir (el daño busca `GetComponentInParent<Health>()`). El daño es **por zonas** para todos los personajes (`DamageModel`, regla del usuario): la zona la saca `Health.ZoneAt` del punto de impacto y los huesos del modelo; una bala en la cabeza mata, dos en el pecho matan y en brazos y piernas hacen falta seis con la 9 mm. El daño de un arma es el de una bala en el pecho; el chaleco solo protege el tronco. Ningún disparo nuevo debe multiplicar por su cuenta según el collider. `PlayerCombat` pega o dispara según el arma equipada en `PlayerInventory`; los objetos y armas se definen en código en `ItemCatalog`. `GameUi.MenuOpen` (inventario abierto) bloquea movimiento, cámara y combate. Solo se escala o se cubre tras lo que se ve (`SolidVisual.IsVisible`: Renderer o `SolidVisual`; los colliders invisibles son límites) y no lleva `NoClimb`. No se cubre tras lo que se ve a través (material con recorte por alfa o `NoCover`). Los coches se cruzan por encima (`ClimbKind.SlideOver`).
- **Coches**: `VehicleController` (Rigidbody + WheelColliders, tracción trasera). Solo lee input si `HasDriver`; sin conductor queda frenado. Usa `Rigidbody.linearVelocity` (API de Unity 6). Todos los aparcados son conducibles (`MakeDrivable`) y llevan `VehicleLock`: se roban manteniendo F para forzar la puerta. Van en la capa 6 (`Vehiculo`, `GameLayers`), con `SolidVisual` en la raíz para que sus colliders cuenten como visibles al escalar y cubrirse.
- **Conducción y destrucción**: `VehicleController` lleva ayudas tipo arcade (volante suavizado, agarre, ayuda al giro limitada a 1.4 g, barras estabilizadoras; números en `VehicleMath`) y sonido. Al chocar rompe lo que lleve `Breakable` (`Scripts/Destruction/`): `BreakableFence` en cada panel de `FenceSegment` (salvo en el borde del mundo) y `BreakableProp` en la raíz del mobiliario (farolas, semáforos, señales, bocas de incendio, bancos, paradas). Lo que lleve `BreakableProp` queda fuera del static batching (`MarkStatic`); todo mobiliario nuevo que se pueda tumbar debe llevarlo.
- **Subir y bajar del coche**: `VehicleEntry` va en el jugador. Al entrar, emparenta el jugador al coche y desactiva su CharacterController, PlayerMovement y renderers (el GameObject sigue activo para poder leer F y salir). Además cambia la cámara a modo vehículo.
- **Lo que se usa con F a pie**: `Interactable` + `PlayerInteractor` (en el jugador). Con uno a mano, F es para él y no para el coche. La casa del jugador (`Scripts/Home/`): vestíbulo dentro de T2 (portal abierto) con un ascensor que sube con fundido al piso de la última planta, con ventanas reales a la ciudad (`PrototypeSceneBuilder.Apartment.cs`; la torre se construye hueca con `TowerOpenings`), con armario (`Wardrobe`, ropa de `Clothing.cs`/`PlayerOutfit`) y caja fuerte (`SafeBox`). Los paneles derivan de `MenuInteractable` y usan `GameUi.MenuOpen`.
- **Cámara**: `ThirdPersonCamera` es una órbita propia (no Cinemachine) con perfiles a pie y en vehículo, y colisión por SphereCast. Ignora la capa 2 (`Ignore Raycast`, el jugador) y la 6 (`Vehiculo`, los coches).
- **Hora y tiempo**: `WeatherSystem` (objeto `Tiempo`, `Scripts/World/`) lleva un día de 20 min, lluvia y nieve al azar y el cielo gris (shader `GTAChiraq/CieloGris`); la lógica pura está en `DayCycleMath`/`WeatherMath`. A mediodía en seco reproduce la luz original. Las luces de calle llevan `NightLight` (apagadas de día); toda luz de calle nueva debe llevarlo. El renderer es Forward+ para que las farolas iluminen las aceras largas. Las capturas salen a mediodía, salvo el elemento `tiempo`.
- **NPC y facciones** (`Scripts/NPC/`): `NpcPopulation` saca y quita gente alrededor del jugador según la hora y el tiempo y lanza incursiones; cada NPC es un clon de `PlantillaNPC` (cuerpo del jugador, capa 7 `Personaje`) con `NpcBrain` (NavMeshAgent), `NpcAnimator` y `PlayerOutfit.Wear`. Los sitios (`NpcSpot`) y el horario de los locales (`ShopHours`) los pone el generador (`PrototypeSceneBuilder.Npc.cs`), que también hace la malla de navegación. Facciones y territorios en `FactionCatalog`; la lógica pura en `NpcMath`. El diseño, las reglas del usuario y las facciones están en `NPCS.md`: léelo antes de tocar NPC y apunta ahí lo que pida el usuario sobre ellos. Todo disparo o golpe nuevo debe avisar a `NpcAlerts`. Los muertos dejan el cuerpo con lo que llevaban (`CorpseLoot`, se registra con F; nada al suelo).
- **Tráfico** (`Scripts/Traffic/`, generado por `PrototypeSceneBuilder.Traffic.cs`): la red (`TrafficNetwork`) sale sola de `NamedStreets()` (una calle nueva entra sola; una curva hay que unirla a mano con puntos intermedios); `TrafficCar` es cinemático, en la capa 6, y ve lo que tiene delante con un `SphereCast` (coches, jugador, NPC y tren); `TrafficPopulation` lo saca alrededor del jugador según la hora. El tren de mercancías (`FreightTrain`) cierra los pasos a nivel (`LevelCrossing`), y los tramos que cruzan la vía llevan su paso (`TrafficLinkData.Rail`). Lo que se mueve va fuera de `Mundo`.
- **Detalle vial** (`PrototypeSceneBuilder.StreetDetail.cs`): nombres de calle (`NamedStreets()`, toda calle nueva va ahí con su nombre inventado), semáforos con `TrafficLight` (`Scripts/World/`, lógica en `TrafficSignalMath`), STOP, pasos de cebra, flechas, postes con cables y alcantarillado. Va después de todas las zonas y comprueba con la física (`SpotFree`) que cada poste cabe. Diseño en `docs/estructuras/calles.md`.
- **Tren elevado**: `ElevatedTrain` (`Scripts/World/`) mueve los trenes decorativos de la avenida. La lógica pura está en `TrainMotion` (testeada en EditMode). Los trenes se generan fuera de `Mundo` porque `Mundo` se marca como estático.
- **Barrios de casas y negocios**: `PrototypeSceneBuilder.Houses.cs` construye el barrio de casas dos veces (`BuildHousesVariant`): en su sitio y girado 180° al oeste de la avenida (`HousesWestOrigin`). Sus tablas de rectángulos están en coordenadas locales del barrio. Delante de la copia oeste va la franja de negocios (`PrototypeSceneBuilder.Shops.cs`).
- **Manzana central**: `PrototypeSceneBuilder.Towers.cs` (torres, adosadas, aparcamientos). Se construye después del barrio de viviendas y del de casas porque reutiliza sus materiales (`LoadMat` y los estáticos de `PrepareHouses`/`PrepareVegetation`).
- **Escena**: `Prototype.unity` **se genera** desde el menú *GTAChiraq > Construir escena prototipo* (`PrototypeSceneBuilder`). Para cambiar el mundo de prueba, edita el generador y regenera la escena; no edites la escena a mano, porque se sobrescribe. No se versiona (está en `.gitignore`: pesa más de 100 MB, el límite de GitHub); se versiona su `.meta`. El generador también crea el URP asset si no hay ninguno y los materiales en `Assets/_Project/Materials/`.

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

Capturas de las zonas del juego, con el editor cerrado (regenera la escena antes de capturar). Van **versionadas por elemento**: `ingame/<elemento>/vNN_<cambio>/`. Cada vez que se cambia un elemento, se capturan solo los elementos afectados en una versión nueva, con una descripción corta del cambio:

```powershell
& $unity -batchmode -projectPath E:\gtachiraq -executeMethod GTAChiraq.Editor.IngameScreenshots.BuildAndCapture -capturar estacion -cambio "escaleras transitables" -quit -logFile capturas.log
```

En el editor: *GTAChiraq > Capturas in-game (todas)*. Si cambia el mundo, añade o ajusta las vistas en `IngameScreenshots.Views`.

**Unity abierto:** no pidas al usuario que cierre Unity para lanzar batchmode (build, tests o capturas). Copia el proyecto (con `Library`, sin `.git`) al scratchpad y lanza Unity en batchmode **sobre la copia** (ver `Assets/_Project/Tests/README.md`, *Con el editor abierto*): así se puede regenerar la escena, capturar, pasar tests y grabar animaciones sin tocar el editor del usuario. Trae a `ingame/` lo que valga. La comprobación rápida de compilación sigue siendo con Roslyn (`dotnet build` del `.csproj`; un archivo nuevo hay que añadirlo a mano al `.csproj`, que Unity regenera).

**Probar el personaje:** para ver animaciones (andar, correr, apuntar, cubrirse, escalar…) se usa el grabador `AnimationCapturePlayTests` sobre la copia y se miran las hojas de fotogramas; cada vez que se cambie una animación, se graba antes y después y se guardan las hojas en `ingame/animaciones/vNN_<cambio>/`.

`Packages/manifest.json` marca `com.unity.inputsystem` como `testable`. Hace falta para `InputTestFixture` en los tests PlayMode.
