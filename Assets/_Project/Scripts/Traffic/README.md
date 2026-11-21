# Traffic: tráfico de la ciudad

Coches que circulan por las calles, paran en los semáforos y en los STOP, y un autobús urbano con paradas. Lo genera `PrototypeSceneBuilder.Traffic.cs` (objeto `Trafico_Ciudad`, fuera de `Mundo` porque se mueve); ver [`Editor/Docs/trafico.md`](../../Editor/Docs/trafico.md). Todo es cinemático: la física no mueve los coches, pero tienen colisión (capa `Vehiculo`).

## `TrafficNetwork`
La red de calles: nodos (cruces y extremos), radio de cada cruce, salidas, semáforo de cada nodo (`TrafficLight`) y tramos de un sentido (`TrafficLinkData`: de, a, puntos intermedios para las curvas, desplazamiento del carril, velocidad, qué hay al final —`Free`, `Signal` o `Stop`— y, si cruza la vía de mercancías, su paso a nivel: `Rail`, `RailPoint`, `RailHalf`).
- Al empezar calcula los carriles (`Lane`): el eje de la calle recortado al borde de cada cruce y desplazado a la derecha (`TrafficMath.OffsetRight`). Si el tramo cruza la vía, `RailStart` es la distancia a la que empieza el lecho y `RailClosedAhead(s)` dice si hay un paso cerrado por delante.
- **Callejones:** un tramo `LeadsToDeadEnd` si al final solo se puede dar media vuelta (o seguir por otros callejones). No se eligen nunca y no salen coches en ellos. Así quedan fuera las calles cortadas (ROAD CLOSED del borde este, los puentes del canal), las calles sin salida y las de dentro del barrio de viviendas.
- **Salidas:** los extremos en el borde del mundo (oeste, x = −300; norte, dentro de los pasos inferiores del terraplén, z = 520). Un coche que llega a una desaparece.
- `ChooseNext(lane, roll)` elige el siguiente tramo; `LinkBetween(a, b)` sirve para las rutas fijas; `Occupancy/Enter/Leave` cuentan los coches dentro de cada cruce (para los STOP).
- Con el objeto seleccionado, los gizmos pintan los carriles: verde con semáforo, amarillo con STOP, azul sin nada y rojo los callejones.

## `TrafficCar`
Un coche (o el autobús). En cada `FixedUpdate`:
1. **Velocidad objetivo:** el límite del tramo × su `temper` (cada conductor es algo más rápido o más lento); 5 m/s girando.
2. **Final del tramo:** en rojo frena hasta la línea; en ámbar para solo si le da tiempo (`TrafficMath.StopsForAmber`); en un STOP se para del todo, espera 1.2 s y sigue cuando el cruce está vacío. Al pasar la línea ya no se para.
3. **Antes de girar** baja a la velocidad de giro; el giro es una curva de Bézier entre el final de un carril y el principio del siguiente.
4. **Pasos a nivel:** con el paso cerrado (`LevelCrossing.Closed`) para antes de la vía; si la vía está justo al principio del tramo siguiente (W DOCK ST está a 15 m), espera en el cruce de antes. Si ya está encima, sigue para dejarla libre.
5. **Delante:** un `SphereCast` desde el morro (radio 0.85 m, tan largo como la distancia de frenada + 10 m) ve coches, el tren de mercancías, el jugador y los NPC, y frena para dejar 2.2 m.
6. Si el jugador le corta el paso más de 2.5 s, **pita** (`ProceduralSfx.Horn`).
7. Si oye **tiros** cerca (`NpcAlerts`), durante 12 s va un 45 % más rápido y se salta semáforos y STOP.
8. Si el **coche del jugador lo embiste** (más de 5 m/s), se queda parado con los intermitentes y avisa a `NpcAlerts.Hit`.

Se para con el morro 0.8 m antes de la línea y, si llega a la línea cerrada sin haber podido frenar (por ejemplo, al salir de un giro a un tramo muy corto), se queda ahí: no la pasa.

Las ruedas giran con la velocidad. El autobús lleva `route` (nodos en bucle) y `stops` (paradas en el carril): no elige al azar y espera `stopSeconds` en cada parada.

## `TrafficPopulation`
Mantiene activos `maxCars` × `TrafficMath.Density(hora)` coches de una reserva ya construida (desactivados). Cada 0.4 s:
- quita los que están a más de 235 m y no se ven, y los atascados o embestidos más de 25 s sin que se vean;
- saca hasta dos nuevos en carriles a 70–190 m del jugador, fuera de la vista (o a más de 150 m) y a más de 14 m de otro coche; nunca en tramos de menos de 40 m ni en los últimos 30 m antes de un cruce. Al empezar los saca todos de golpe, desde 20 m.

## `TrafficMath` (lógica pura)
`StoppingSpeed`, `FollowSpeed`, `StopsForAmber`, `Approach`, `Bezier`, `TurnControl`, `OffsetRight`, `ChooseNext` (nunca media vuelta ni callejones; recto pesa el doble que girar) y `Density` (madrugada ~0.2, horas punta de las 8 y las 17:30 al máximo). Se testea en `Tests/EditMode/TrafficMathTests.cs`; el tráfico en la escena, en `Tests/PlayMode/TrafficPlayTests.cs`.

**Limitaciones:**
- No adelantan ni cambian de carril, y los cruces sin semáforo no se reparten el paso (solo el `SphereCast`), así que en un STOP puede llegar alguien por la calle con preferencia mientras otro cruza.
- No se pueden robar en marcha: los coches del tráfico no llevan `VehicleController`.
- La diagonal del barrio norte (S HALDANE AVE) no está en la red, porque no es un rectángulo.
- Al llegar a la salida del borde oeste desaparecen de golpe (se ve si el jugador está allí).
