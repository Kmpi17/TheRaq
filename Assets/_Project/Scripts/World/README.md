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

## `HighwayCar` y `HighwayLoop`
Coche decorativo de la autopista hundida, sin colisiones. Recorre en línea recta su carril de `start` a `end` a velocidad constante y, al llegar al final, vuelve a empezar. Lo crea y configura `PrototypeSceneBuilder.Highway.cs` (`BuildHighwayTraffic`) con `Configure(from, to, metersPerSecond, initialDistance)`. Los coches van fuera de `Mundo`, como los trenes.

| Campo | Por defecto | Qué hace |
|---|---|---|
| `start`, `end` | — | Extremos del carril. Tienen que quedar fuera de la vista: el sur, en la niebla (z −295); el norte, dentro del túnel (z 124). |
| `speed` | 22 m/s | Velocidad constante. |
| `startDistance` | 0 | Dónde empieza, para que los coches de un carril vayan separados. |

`HighwayLoop.Advance(distance, delta, length)` es la lógica pura: vuelve al principio con `Mathf.Repeat` y siempre devuelve un valor en [0, largo). Se testea en `Tests/EditMode/HighwayLoopTests.cs`.

**Limitaciones:** no frenan ni cambian de carril y no se ven entre sí (pueden solaparse si sus velocidades son distintas). El salto del final al principio se hace fuera de la vista.

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

## Capas y marcas de obstáculos
- `GameLayers`: capa 2 (`Ignore Raycast`, el jugador) y capa 6 (`Vehiculo`, todos los coches conducibles). La cámara ignora las dos; la escalada, la cobertura y las balas solo la 2.
- `SolidVisual`: en un objeto cuyos colliders no tienen `Renderer` al lado porque el modelo está en los hijos (los coches). `SolidVisual.IsVisible(collider)` es la regla común de escalada y cobertura (Renderer o `SolidVisual`; ni triggers ni WheelCollider). `SolidVisual.IsSeeThrough(collider)` dice si se ve a través: `NoCover`, o material con `_AlphaClip` (verjas, mallas, barandillas caladas).
- `NoClimb`: no se escala aunque se vea (un pretil).
- `NoCover`: no sirve de cobertura aunque se vea y sea opaco.
