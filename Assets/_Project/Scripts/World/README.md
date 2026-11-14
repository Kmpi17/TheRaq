# World: elementos decorativos que se mueven

## `ElevatedTrain`
Tren elevado decorativo, sin colisiones y sin poderse usar. Mueve su `GameObject` en línea recta de `start` a `end`, mirando en esa dirección. Lo crea y configura `PrototypeSceneBuilder.Train.cs` con `Configure(from, to, station, initialDistance)`.

| Campo | Por defecto | Qué hace |
|---|---|---|
| `start`, `end` | — | Extremos del recorrido, en coordenadas del mundo. Tienen que quedar más allá de la niebla. |
| `stationDistance` | 100 | Distancia desde `start` a la que se para el centro del tren. |
| `maxSpeed` | 16 m/s | Velocidad de crucero. |
| `acceleration` | 1.1 m/s² | Se usa para arrancar y para frenar. |
| `dwellSeconds` | 10 s | Tiempo parado en la estación. |
| `layoverSeconds` | 20 s | Espera al final, fuera de la vista, antes de volver a empezar. |
| `startDistance` | 0 | Dónde empieza. Sirve para que los dos trenes no vayan sincronizados. |

El tren **no puede ser estático**. El generador lo crea fuera de `Mundo`, que se marca como estático.

## `TrainMotion` (lógica pura)
`TrainMotion.Step(state, dt, route)` avanza un `TrainState` y devuelve el nuevo estado. Las fases son:
- `Running`: el tren arranca a velocidad de crucero y frena con `v = √(2·a·d)` (mínimo 0.6 m/s) hasta pararse justo en la estación.
- `Dwelling`: espera en la estación.
- `Waiting`: espera al final del recorrido y vuelve a empezar en la distancia 0.

No depende de la escena; se testea en `Tests/EditMode/TrainMotionTests.cs`.

**Limitaciones:**
- Solo hay una estación por recorrido.
- El recorrido es una línea recta, sin curvas.
- No hay sonido.
