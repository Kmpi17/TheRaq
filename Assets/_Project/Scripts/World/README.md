# World: hora, tiempo y elementos decorativos que se mueven

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

## `WeatherSystem`: hora del día y tiempo
Va en el objeto `Tiempo` (lo crea `PrototypeSceneBuilder.Weather.cs`). Cada fotograma avanza la hora, lleva los episodios de tiempo y aplica el aspecto de `DayCycleMath.Look`: sol o luna (intensidad, color, giro), ambiente, niebla, colores del cielo (instancia del material `CieloGris`), farolas (`NightLight.SetLevel`) y partículas.

| Campo | Por defecto | Qué hace |
|---|---|---|
| `sun` | — | La luz direccional "Sol nublado". De día es el sol (40° a mediodía, gira con la hora); de noche, la luna. La intensidad pasa por 0 en el cambio, así que las sombras no saltan. |
| `skyMaterial`, `rainMaterial`, `snowMaterial` | — | Se copian en instancias al empezar: el asset no se toca. |
| `startHour` | 12 | Hora al empezar. |
| `cycleSeconds` | 1200 | Segundos reales por día de juego (20 min). |
| `seed` | 0 | Semilla del tiempo; 0 es al azar cada partida. La partida empieza siempre seca. |
| `rainRate`, `snowRate` | 3500, 1300 | Gotas y copos por segundo a tope. |

- **API:** `Instance`, `Hour`, `Current` (`Weather`), `Rain`/`Snow` (0–1, con la transición), `SetHour(h)`, `ForceWeather(w, fuerza, instant)`, `Describe()` ("14:30  ·  Nieve", lo muestra el HUD bajo el dinero) y `Preview(h, lluvia, nieve, cámara)` para las capturas en el editor.
- **Para probar en el editor:** el menú del componente (los tres puntos del inspector) tiene *Adelantar 3 horas*, *Poner de noche (23:00)*, *Poner de día (12:00)*, *Forzar lluvia*, *Forzar nieve* y *Despejar*.
- **Partículas:** dos emisores en caja (lluvia de 44 m, a 16 m sobre la cámara; nieve de 56 m, a 12 m) que siguen a la cámara, 8 m por delante. Simulan en mundo y mueren al chocar con cualquier collider salvo el jugador, así que no llueve dentro de los edificios ni bajo los tejados. La lluvia son billboards estirados por la velocidad (~20 m/s, algo de viento) y la nieve copos que bajan a ~1.5 m/s con ruido. No les da la luz: su brillo baja de noche (`ParticleTint`) y los copos se tiñen algo de naranja por las farolas.

## `DayCycleMath` y `WeatherMath` (lógica pura)
- `DayCycleMath.Look(hora, lluvia, nieve)` devuelve un `WorldLook`. A mediodía y en seco da exactamente la luz original del mundo. `Daylight(h)` vale 0 de noche y 1 de día (amanece de 5:30 a 7:30, anochece de 18:30 a 20:30). Las farolas (`LampFactor`, 0–1) se encienden con el día aún a 0.9 de claridad y están del todo a 0.45. La lluvia oscurece y cierra la niebla (hasta ~160 m); la nieve la blanquea y la cierra más (~130 m). También `HourAt(s, inicio)` y `Clock(h)` ("HH:MM").
- `WeatherMath`: cadena de Markov de episodios `Nublado`/`Lluvia`/`Nieve`. `Next(actual, azar)`: tras nublado, 30 % lluvia, 40 % nieve y 30 % sigue seco; la lluvia pasa a nieve un 20 % de las veces y la nieve a lluvia un 10 %. Nunca repite lluvia ni nieve. `Duration`: nublado 5–12 min, lluvia 2–6 min, nieve 3–7 min. `Strength`: de 0.45 (chispea) a 1. `Approach`: entra y sale en 40 s. A la larga: ~70 % seco, ~20 % nieve, ~10 % lluvia.
- Se testean en `Tests/EditMode/WeatherTests.cs`; el componente, en `Tests/PlayMode/WeatherSystemPlayTests.cs`.

## `NightLight`
Va en las luces de calle. Su intensidad va de `dayLevel` (0: apagada y desactivada) a `nightLevel` (7) veces la de la escena, según cuánto es de noche. Hace falta tanto porque URP atenúa con la distancia al cuadrado. `NightLight.SetLevel(noche)` actualiza todas las activas y solo recorre la lista cuando el nivel cambia.

**Limitaciones del tiempo:**
- No hay charcos que se formen ni nieve que cuaje.
- No hay sonido, rayos ni viento que mueva cosas.
- Las ventanas de los edificios no se encienden de noche.
- La lluvia no se ve iluminada por las farolas.

## Capas y marcas de obstáculos
- `GameLayers`: capa 2 (`Ignore Raycast`, el jugador), capa 6 (`Vehiculo`, todos los coches conducibles) y capa 7 (`Npc`, "Personaje": los NPC). La cámara ignora las tres; la escalada, la cobertura y las balas solo la 2.
- `SolidVisual`: en un objeto cuyos colliders no tienen `Renderer` al lado porque el modelo está en los hijos (los coches). `SolidVisual.IsVisible(collider)` es la regla común de escalada y cobertura (Renderer o `SolidVisual`; ni triggers ni WheelCollider). `SolidVisual.IsSeeThrough(collider)` dice si se ve a través: `NoCover`, o material con `_AlphaClip` (verjas, mallas, barandillas caladas).
- `NoClimb`: no se escala aunque se vea (un pretil).
- `NoCover`: no sirve de cobertura aunque se vea y sea opaco.
