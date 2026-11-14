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
| `Editor/` | `PrototypeSceneBuilder`: genera la escena prototipo, el URP y los materiales. `PrototypeSceneBuilder.StreetProps`: detalle callejero del barrio. `PrototypeSceneBuilder.Vegetation`: árboles de invierno variados, restos de nieve y maleza. `PrototypeSceneBuilder.Houses`: barrio de casas bajas y apartamentos de 3 pisos con callejones (al norte, y una copia girada al oeste de la avenida). `PrototypeSceneBuilder.Shops`: franja de negocios sueltos (barbería, tienda de licores, lavandería…) delante de la copia oeste. `PrototypeSceneBuilder.Train`: tren elevado sobre la avenida principal, con estación. `PrototypeSceneBuilder.Cars`: modelos de coche low-poly (sedán, coupé, todoterreno, furgoneta) para los aparcados, el que se conduce y el abandonado. `PrototypeSceneBuilder.Towers`: manzana central (3 torres, casas adosadas, patios y 3 aparcamientos). `IngameScreenshots`: capturas de cada zona en `ingame/`. `ProceduralTextures`: texturas del barrio | [→](Assets/_Project/Editor/README.md) |
| `mapa/` (raíz) | `mapa.png`: plano cenital del mundo con coordenadas, que sirve de referencia para modificarlo. `generar_mapa.py` lo regenera (Python + Pillow) y copia las medidas del generador. Las casas las lee de `edificios_casas.json`, que Unity escribe al construir la escena | [→](ESTRUCTURAS.md) |
| `Tests/` | Tests EditMode y PlayMode | [→](Assets/_Project/Tests/README.md) |
| `Scenes/`, `Materials/`, `Textures/`, `Settings/` | Los crea el generador; no se editan a mano | — |

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
- Movimiento a pie.
- Cámara.
- Conducción.
- Subir y bajar del coche.
- Manzana central: conjunto de torres con casas adosadas, patios y aparcamientos, con las texturas del barrio de viviendas.
- Barrio de viviendas con texturas (ladrillo con ventanas, calle con doble línea amarilla, aceras, césped seco) al este de la avenida.
- Detalle callejero en el barrio: vallas, cancha de baloncesto, plaza, parada de bus, postes con cables, arbustos, árboles, contenedores, basura, manchas, charcos y grafitis.

Pendiente:
- NPCs y peatones (NavMesh).
- Personajes con modelo y animación.
- Armas y combate.
- Misiones.
- HUD.
- Ampliar el mapa.
- Ajustar la estética con las referencias visuales.
