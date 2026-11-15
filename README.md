# GTAChiraq

Juego de mundo abierto 3D en tercera persona (género tipo GTA San Andreas), ambientado en el Southside de Chicago en los años 2010 y en la escena drill. Hecho con **Unity 6 LTS + URP**.

> Personajes y lugares **ficticios**, inspirados en la época. No se usan nombres ni parecidos de personas reales.

## Puesta en marcha

1. Instala Unity Hub y Unity 6 LTS.
2. En Hub, ve a **Add > Add project from disk** y elige esta carpeta.
3. Cuando el editor pregunte si quieres activar el nuevo sistema de input, di **Yes** (el editor se reinicia).
4. En el menú, pulsa **GTAChiraq > Construir escena prototipo** y luego **Play**. La escena (`Assets/_Project/Scenes/Prototype.unity`) **no está en el repositorio**: pesa más de 100 MB y se genera siempre desde el código, así que hay que construirla después de clonar y cada vez que cambie el generador.

| Acción | Teclado / ratón | Mando |
|---|---|---|
| Moverse / acelerar y girar | WASD o flechas | Stick izquierdo |
| Cámara | Ratón | Stick derecho |
| Esprintar (gasta energía) | Shift izquierdo | B / Círculo |
| Escalar, saltar una valla o cruzar un coche / freno de mano | Espacio | A / Cruz · RB |
| Cubrirse / salir de la cobertura | Q | RB |
| Apuntar | Clic derecho | LT |
| Pegar / disparar | Clic izquierdo | RT |
| Recargar | R | X / Cuadrado |
| Cambiar de arma | Rueda del ratón | Cruceta ← → |
| Inventario (cámara lenta) | Tab | Select |
| Subir o bajar del coche · grabar en el estudio · ascensor, armario y caja fuerte de casa | F | Y / Triángulo |
| Robar un coche aparcado (forzar la puerta) | Mantener F | Mantener Y / Triángulo |
| Soltar o recuperar el ratón | Esc / clic | — |

## Mundo

[`ESTRUCTURAS.md`](ESTRUCTURAS.md) es el cuaderno del mundo: estilo, zonas, estructuras y todo lo que se ha pedido sobre cómo debe sentirse cada sitio.

## Mapa del código

Todo lo propio está en `Assets/_Project/`. Cada carpeta tiene su propio README con los detalles.

| Carpeta | Qué hay | README |
|---|---|---|
| `Scripts/Input/` | `GameInput`: todas las acciones de input, definidas en código | [→](Assets/_Project/Scripts/Input/README.md) |
| `Scripts/Player/` | `PlayerMovement` (con inercia, esprint con energía y daño por caída), `PlayerVitals` (energía, regeneración, muerte y reaparición), `PlayerClimb` (escalar, sin salto), `PlayerCover` (cobertura), `PlayerHud`, `PlayerOutfit` y `Clothing` (ropa con textura, con gafas y cadena; pinta según `img/gang.png`), `PlayerAnimator` (animación procedural: andar, correr, cobertura, escalar, pistola, puñetazos); lógica pura en `MovementMath` y `ClimbMath` | [→](Assets/_Project/Scripts/Player/README.md) |
| `Scripts/Camera/` | `ThirdPersonCamera`: cámara orbital con colisión | [→](Assets/_Project/Scripts/Camera/README.md) |
| `Scripts/Vehicles/` | `VehicleController`: coche con WheelColliders. `VehicleLock`: cerradura de los coches ajenos, que se fuerza para robarlos | [→](Assets/_Project/Scripts/Vehicles/README.md) |
| `Scripts/Interaction/` | `VehicleEntry`: subir y bajar del coche, y forzar la puerta de los ajenos. `Interactable` y `PlayerInteractor`: lo que se usa con F a pie y los viajes en ascensor con fundido | [→](Assets/_Project/Scripts/Interaction/README.md) |
| `Scripts/Home/` | Casa del jugador: `Elevator` (ascensor del portal de T2 al piso y vuelta), `Wardrobe` (armario: cambiarse de ropa), `SafeBox` y `Stash` (caja fuerte para dinero, armas y objetos) | [→](Assets/_Project/Scripts/Home/README.md) |
| `Scripts/World/` | `ElevatedTrain` y `TrainMotion`: tren elevado decorativo que circula por la avenida. `HighwayCar` y `HighwayLoop`: tráfico decorativo de la autopista hundida. `GameLayers`, `SolidVisual`, `NoClimb`, `NoCover`: capas y marcas de obstáculos para escalar y cubrirse | [→](Assets/_Project/Scripts/World/README.md) |
| `Scripts/Combat/` | `Health` (vida y chaleco de cualquier cosa herible), `PlayerCombat` (puños con combo y pistola: apuntar, dispersión, retroceso, recarga), `TrainingDummy` (dianas), `ProceduralSfx` (sonidos provisionales). Lógica pura: `HealthPool`, `StaminaPool`, `CombatMath` | [→](Assets/_Project/Scripts/Combat/README.md) |
| `Scripts/Inventory/` | `Inventory` (casillas, lógica pura), `ItemCatalog` (objetos en código), `PlayerInventory` (dinero, arma equipada, cargadores y panel con Tab), `ItemPickup` (objetos en el suelo) | [→](Assets/_Project/Scripts/Inventory/README.md) |
| `Scripts/Music/` | `RecordingStudio`: micro del estudio casero (se graba pulsando F a tiempo). `StudioSession` y `MusicCareer`: sesión de grabación y progresión musical (temas, fama y rangos), en lógica pura | [→](Assets/_Project/Scripts/Music/README.md) |
| `Editor/` | `PrototypeSceneBuilder` (clase `partial`, un archivo por zona; ver *Zonas del mundo*): genera la escena prototipo, el URP y los materiales. `PrototypeSceneBuilder.Body.cs`: cuerpo del jugador (piezas de anillos facetados con ropa y cara pintadas). `ProceduralTextures`: texturas generadas en código. `IngameScreenshots`: capturas de cada zona en `ingame/` | [→](Assets/_Project/Editor/README.md) |
| `mapa/` (raíz) | `mapa.png`: plano cenital del mundo con coordenadas, que sirve de referencia para modificarlo. `generar_mapa.py` lo regenera (Python + Pillow) y copia las medidas del generador. Las casas las lee de `edificios_casas.json`, que Unity escribe al construir la escena | [→](ESTRUCTURAS.md) |
| `Tests/` | Tests EditMode y PlayMode | [→](Assets/_Project/Tests/README.md) |
| `ingame/` (raíz) | Capturas de cada zona, versionadas: `ingame/<elemento>/vNN_<cambio>/` con un `cambio.txt`. `ingame/personaje/`: el modelo; `ingame/animaciones/`: hojas de fotogramas del grabador de animaciones (`AnimationCapturePlayTests`) | [→](Assets/_Project/Editor/README.md) |
| `img/` (raíz) | Imágenes de referencia (`estructures/`) y capturas de bugs (`estrcuturebugs/`) | [→](ESTRUCTURAS.md) |
| `Scenes/`, `Materials/`, `Textures/`, `Settings/` | Los crea el generador; no se editan a mano | — |

## Zonas del mundo

La escena se construye desde el menú **GTAChiraq > Construir escena prototipo**. Cada zona está en su archivo de `Assets/_Project/Editor/` (el plano completo, en [`mapa/mapa.png`](mapa/mapa.png)):

| Zona | Archivo | Qué hay |
|---|---|---|
| Calles grandes | `PrototypeSceneBuilder.cs`, `.MainRoads.cs` | Calles de 14 m en z = ±80 y x = ±110, con textura gastada, cruces con stop y paso de cebra, alcantarillas, sumideros y nieve |
| Manzana central | `.Towers.cs` | 3 torres, casas adosadas, patios y 3 aparcamientos |
| Casa del jugador | `.Apartment.cs` | Vestíbulo dentro de T2 con ascensor al piso de la planta 14 (ventanas reales a la ciudad): salón, cocina, cama, armario y caja fuerte |
| Barrio de viviendas | `PrototypeSceneBuilder.cs`, `.StreetProps.cs`, `.Vegetation.cs` | Bloques y torres de ladrillo, cancha, plaza, parada de bus y detalle callejero |
| Barrio de casas (este y copia oeste) | `.Houses.cs` | Casas bajas, greystones y apartamentos de 3 pisos con callejones, garajes, porches traseros y solares |
| Ampliación oeste y norte | `.HousesWestExt.cs`, `.HousesNorth.cs` | Más barrio de casas hasta la calle x = −110 y hasta el borde norte |
| Oeste lejano | `.FarWest.cs` | Barrio de casas con gasolinera, negocios, cancha y descampado |
| Autopista hundida | `.Highway.cs` | Trinchera de 7 m en el borde oeste (x −300…−275) con puentes en z = ±80, túnel al norte y tráfico |
| Instituto | `.School.cs` | Edificio de 3 plantas, ala, gimnasio, campo de fútbol americano con pista y gradas, aparcamiento y autobuses |
| Negocios | `.Shops.cs` | Locales sueltos con letreros genéricos frente a la calle norte |
| Tren elevado | `.Train.cs` | Vía elevada sobre la avenida, estación y trenes que circulan |
| Cocheras | `.RailYard.cs` | Playa de vías al sur del inicio: rampa desde el elevado, nave, trenes aparcados, contenedores, valla con concertina |
| Coches | `.Cars.cs` | Modelos low-poly para los aparcados, el que se conduce y el abandonado |

## Capturas y plano

Con el editor **cerrado** (Unity no deja abrir el proyecto dos veces):

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe"
# Regenera la escena y captura los elementos indicados (sin -capturar, todos) en una versión nueva
& $unity -batchmode -projectPath . -executeMethod GTAChiraq.Editor.IngameScreenshots.BuildAndCapture -capturar oeste_lejano,calles_grandes -cambio "lo que cambió" -quit -logFile capturas.log
```

Con el editor abierto: **GTAChiraq > Capturas in-game (todas)**. El plano se regenera con `python mapa/generar_mapa.py` (necesita Pillow) después de construir la escena, porque las casas salen de `mapa/edificios_casas.json`.

## Flujo en tiempo de juego

```
GameInput.Actions (singleton)
   ├─► PlayerMovement ──► CharacterController      (a pie; Locked = otra habilidad manda)
   │      ├─ PlayerClimb (Espacio): toma el control y recorre el obstáculo
   │      ├─ PlayerCover (Q): toma el control y se mueve pegado a la pared
   │      └─ PlayerVitals: energía; al morir bloquea y reaparece
   ├─► PlayerCombat ──► Health de lo que golpea    (puños / pistola de PlayerInventory)
   │      └─ ThirdPersonCamera.SetAiming / AddRecoil / AimRay
   ├─► PlayerInventory (Tab, rueda) ──► GameUi.MenuOpen bloquea el resto
   ├─► ThirdPersonCamera ──► sigue a su target     (jugador o coche)
   ├─► VehicleController ──► WheelColliders        (solo si HasDriver)
   ├─► PlayerInteractor (F a pie): ascensor (fundido y teletransporte), armario, caja fuerte
   └─► VehicleEntry (F, si no hay nada de lo anterior a mano)
          ├─ coche ajeno (VehicleLock): mantener F para forzar la puerta; al abrirse, entra
          ├─ entrar: desactiva el control a pie, emparenta el jugador al coche,
          │          SetDriver(true), cámara en modo Vehicle
          └─ salir:  desemparenta, coloca al jugador en el ExitPoint,
                     reactiva el control a pie, cámara en modo OnFoot
```

## Assemblies

| asmdef | Ruta | Depende de |
|---|---|---|
| `GTAChiraq.Runtime` | `Scripts/` | Unity.InputSystem |
| `GTAChiraq.Editor` | `Editor/` (solo editor) | Runtime, URP Runtime, SRP Core |
| `GTAChiraq.Tests.EditMode` | `Tests/EditMode/` | Runtime, NUnit |
| `GTAChiraq.Tests.PlayMode` | `Tests/PlayMode/` | Runtime, InputSystem (+TestFramework), NUnit |

## Tests

Desde el editor: **Window > General > Test Runner**. Para lanzarlos desde la terminal, mira `CLAUDE.md` o [Tests/README](Assets/_Project/Tests/README.md).

## Estado

Hecho (prototipo, hito 1):
- Movimiento a pie, cámara, conducción y subir y bajar del coche.
- Mundo de unos 600 × 400 m con las zonas de *Zonas del mundo*: manzana central, barrio de viviendas, barrios de casas con sus ampliaciones, oeste lejano con gasolinera, negocios, autopista hundida, instituto, tren elevado con estación y calles grandes texturizadas.
- Texturas generadas en código (ladrillo con ventanas, asfalto, aceras, césped seco, letreros) y detalle callejero (vallas, canchas, basura, grafitis, nieve, coches aparcados).

Pendiente:
- NPCs y peatones (NavMesh).
- NPCs con modelo y animación (el jugador ya tiene cuerpo articulado, ropa con textura y animación procedural básica).
- Armas y combate.
- Misiones.
- HUD.
- Ampliar el mapa (al sur de la calle sur y al este del barrio de viviendas solo hay asfalto liso).
- Ajustar la estética con las referencias visuales.
