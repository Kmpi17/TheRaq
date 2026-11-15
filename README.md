# GTAChiraq

Juego de mundo abierto 3D en tercera persona (género tipo GTA San Andreas), ambientado en el Southside de Chicago en los años 2010 y en la escena drill. Hecho con **Unity 6 LTS + URP**.

> Personajes y lugares **ficticios**, inspirados en la época. No se usan nombres ni parecidos de personas reales.

## Puesta en marcha

1. Instala Unity Hub y Unity 6 LTS.
2. En Hub, ve a **Add > Add project from disk** y elige esta carpeta.
3. Cuando el editor pregunte si quieres activar el nuevo sistema de input, di **Yes** (el editor se reinicia).
4. En el menú, pulsa **GTAChiraq > Construir escena prototipo** y luego **Play**.

| Acción | Teclado / ratón | Mando |
|---|---|---|
| Moverse / acelerar y girar | WASD o flechas | Stick izquierdo |
| Cámara | Ratón | Stick derecho |
| Correr | Shift izquierdo | B / Círculo |
| Saltar / freno de mano | Espacio | A / Cruz · RB |
| Subir o bajar del coche | F | Y / Triángulo |
| Soltar o recuperar el ratón | Esc / clic | — |

## Mundo

[`ESTRUCTURAS.md`](ESTRUCTURAS.md) es el cuaderno del mundo: estilo, zonas, estructuras y todo lo que se ha pedido sobre cómo debe sentirse cada sitio.

## Mapa del código

Todo lo propio está en `Assets/_Project/`. Cada carpeta tiene su propio README con los detalles.

| Carpeta | Qué hay | README |
|---|---|---|
| `Scripts/Input/` | `GameInput`: todas las acciones de input, definidas en código | [→](Assets/_Project/Scripts/Input/README.md) |
| `Scripts/Player/` | `PlayerMovement` y `MovementMath`: movimiento a pie | [→](Assets/_Project/Scripts/Player/README.md) |
| `Scripts/Camera/` | `ThirdPersonCamera`: cámara orbital con colisión | [→](Assets/_Project/Scripts/Camera/README.md) |
| `Scripts/Vehicles/` | `VehicleController`: coche con WheelColliders | [→](Assets/_Project/Scripts/Vehicles/README.md) |
| `Scripts/Interaction/` | `VehicleEntry`: subir y bajar del coche | [→](Assets/_Project/Scripts/Interaction/README.md) |
| `Scripts/World/` | `ElevatedTrain` y `TrainMotion`: tren elevado decorativo que circula por la avenida | [→](Assets/_Project/Scripts/World/README.md) |
| `Editor/` | `PrototypeSceneBuilder` (clase `partial`, un archivo por zona; ver *Zonas del mundo*): genera la escena prototipo, el URP y los materiales. `ProceduralTextures`: texturas generadas en código. `IngameScreenshots`: capturas de cada zona en `ingame/` | [→](Assets/_Project/Editor/README.md) |
| `mapa/` (raíz) | `mapa.png`: plano cenital del mundo con coordenadas, que sirve de referencia para modificarlo. `generar_mapa.py` lo regenera (Python + Pillow) y copia las medidas del generador. Las casas las lee de `edificios_casas.json`, que Unity escribe al construir la escena | [→](ESTRUCTURAS.md) |
| `Tests/` | Tests EditMode y PlayMode | [→](Assets/_Project/Tests/README.md) |
| `ingame/` (raíz) | Capturas de cada zona, versionadas: `ingame/<elemento>/vNN_<cambio>/` con un `cambio.txt` | [→](Assets/_Project/Editor/README.md) |
| `img/` (raíz) | Imágenes de referencia (`estructures/`) y capturas de bugs (`estrcuturebugs/`) | [→](ESTRUCTURAS.md) |
| `Scenes/`, `Materials/`, `Textures/`, `Settings/` | Los crea el generador; no se editan a mano | — |

## Zonas del mundo

La escena se construye desde el menú **GTAChiraq > Construir escena prototipo**. Cada zona está en su archivo de `Assets/_Project/Editor/` (el plano completo, en [`mapa/mapa.png`](mapa/mapa.png)):

| Zona | Archivo | Qué hay |
|---|---|---|
| Calles grandes | `PrototypeSceneBuilder.cs`, `.MainRoads.cs` | Calles de 14 m en z = ±80 y x = ±110, con textura gastada, cruces con stop y paso de cebra, alcantarillas, sumideros y nieve |
| Manzana central | `.Towers.cs` | 3 torres, casas adosadas, patios y 3 aparcamientos |
| Barrio de viviendas | `PrototypeSceneBuilder.cs`, `.StreetProps.cs`, `.Vegetation.cs` | Bloques y torres de ladrillo, cancha, plaza, parada de bus y detalle callejero |
| Barrio de casas (este y copia oeste) | `.Houses.cs` | Casas bajas, greystones y apartamentos de 3 pisos con callejones, garajes, porches traseros y solares |
| Ampliación oeste y norte | `.HousesWestExt.cs`, `.HousesNorth.cs` | Más barrio de casas hasta la calle x = −110 y hasta el borde norte |
| Oeste lejano | `.FarWest.cs` | Barrio de casas con gasolinera, negocios, cancha y descampado |
| Negocios | `.Shops.cs` | Locales sueltos con letreros genéricos frente a la calle norte |
| Tren elevado | `.Train.cs` | Vía elevada sobre la avenida, estación y trenes que circulan |
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
   ├─► PlayerMovement ──► CharacterController      (a pie)
   ├─► ThirdPersonCamera ──► sigue a su target     (jugador o coche)
   ├─► VehicleController ──► WheelColliders        (solo si HasDriver)
   └─► VehicleEntry (F)
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
- Mundo de unos 600 × 400 m con las zonas de *Zonas del mundo*: manzana central, barrio de viviendas, barrios de casas con sus ampliaciones, oeste lejano con gasolinera, negocios, tren elevado con estación y calles grandes texturizadas.
- Texturas generadas en código (ladrillo con ventanas, asfalto, aceras, césped seco, letreros) y detalle callejero (vallas, canchas, basura, grafitis, nieve, coches aparcados).

Pendiente:
- NPCs y peatones (NavMesh).
- Personajes con modelo y animación.
- Armas y combate.
- Misiones.
- HUD.
- Ampliar el mapa (al sur de la calle sur y al este del barrio de viviendas solo hay asfalto liso).
- Ajustar la estética con las referencias visuales.
