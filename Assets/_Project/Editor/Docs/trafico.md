# Tráfico, tren de mercancías y ambiente: `Traffic.cs`, `Freight.cs` y `Ambient.cs`
Lo que se mueve por la ciudad. Se construye al final de `Build()` (tren y pasos a nivel, luego tráfico y luego ambiente), después de la malla de navegación (los coches no la tapan) y fuera de `Mundo` (que es estático). Código de juego: [`Scripts/Traffic/`](../../Scripts/Traffic/README.md) y `Scripts/World/` (`FreightTrain`, `LevelCrossing`).

## Tráfico de la ciudad (`PrototypeSceneBuilder.Traffic.cs`, `BuildCityTraffic`)
Objeto `Trafico_Ciudad` con `TrafficNetwork`, `TrafficPopulation`, la reserva de coches (`Coches`), el autobús y sus paradas.
- **La red sale sola de `NamedStreets()`**: cada rectángulo de 7 m de ancho o más es una calle (`TrafficStreet`). Donde se cruzan (o se tocan, con 0.6 m de margen) dos calles perpendiculares hay un nodo en el cruce de los ejes; los extremos que no están a menos de 7.5 m de un cruce son nodos sueltos. Entre nodos seguidos de una calle van dos tramos, uno por sentido.
  - Toda calle nueva que se añada a `NamedStreets()` entra sola en el tráfico. Si no es recta, hay que añadirla a mano con puntos intermedios, como la curva de S WHARF RD (arco de `WharfCenter`/`WharfRadius` entre los dos tramos rectos).
  - **Bulevar** (W KINGSLEY BLVD): cada calzada va en un sentido (la norte hacia el oeste) y el carril va por el centro de la calzada.
  - **Quiebros** de W HOLLIS ST y W CROWELL ST: un tramo norte–sur de 12 m que une las dos mitades.
  - **Carriles:** 1.9 m a la derecha del eje en las calles grandes (el carril de dentro: el de fuera tiene coches aparcados) y 1.7 m en las de barrio.
  - **Velocidades:** 13 m/s en las grandes, 10 en las de barrio, 12 en el bulevar y 7 en la curva.
- **Qué hay al final de cada tramo** (`GateFor`): semáforo en los cuatro cruces de las calles grandes (los `TrafficLight` de `BuildTrafficSignals`, que apunta cada cruce en `signalJunctions`). En los demás: la calle grande tiene preferencia y la de barrio para; en una T para quien llega por el pie; si se cruzan dos calles de barrio, STOP en todas (como en Chicago).
- **Salidas:** los extremos en x = −300 (las calles z = ±80 sobre la autopista) y en el borde norte (S MARLOW AVE y S CALDER AVE, que siguen hasta z = 520, dentro de los pasos inferiores).
- **Coches** (`TrafficPoolSize` = 34, como mucho `TrafficMaxCars` = 30 a la vez): `CarModel` con los faros encendidos, y la mitad con nieve. `MakeTrafficVehicle` les pone caja de colisión, `Rigidbody` cinemático, capa `Vehiculo`, intermitentes apagados (`Intermitente`) y `TrafficCar`. Salen desactivados.
- **Autobús de la línea 52** (`BuildCityBus`, `CityBus`): blanco con franja azul y letrero `52 ASHGROVE` encendido. Da la vuelta a la manzana central (W ASHGROVE ST → S MARLOW AVE → W BRAXTON ST → S CALDER AVE, en sentido contrario a las agujas del reloj) y para en cuatro paradas (poste con la placa `BUS 52` y banco), una por lado, donde `SpotFree` deja sitio en la acera.

## Tren de mercancías y pasos a nivel (`PrototypeSceneBuilder.Freight.cs`, `BuildFreightTraffic`)
- **`TrenMercancias`:** una locomotora genérica en cada punta (`Locomotive`: morro corto, cabina, capó largo, franja amarilla, faros y luces de cuneta, sin logotipos) y 12 vagones al azar: cerrados (con grafitis), cisternas, tolvas y bateas con chatarra; 240 m en total. Una caja de colisión de todo el tren, en la capa `Vehiculo`. Va por la vía norte (`FreightTracksZ[0]`; por eso las cisternas paradas están ahora en la vía sur) de x −310 a 310 a 9 m/s; espera 15 s al empezar y luego 45–120 s entre pasadas, en sentidos alternos.
- **`PasosNivel_Barreras`:** por cada paso (`FreightCrossings`) una `LevelCrossing` con:
  - las dos barreras (pivote junto al mecanismo; levantadas apuntan arriba y bajadas cruzan el carril que llega),
  - y las luces encendidas (dos por poste, que se alternan; las fijas de `BuildSouthStreetDetail` son rojo oscuro).

  Se construye **antes** que el tráfico y apunta cada paso en `levelCrossings`: `BuildCityTraffic` se lo pone (`Rail`, `RailPoint`, `RailHalf`) a los tramos norte–sur que cruzan la vía en su x.

## Ambiente (`PrototypeSceneBuilder.Ambient.cs`, `BuildAmbient`)
Objeto `Ambiente` con sistemas de partículas (material `Vapor`, textura `ProceduralTextures.Puff`):
- **Vapor** en una de cada cinco alcantarillas (`SteamEvery`), en orden de posición para que salga siempre en las mismas.
- **Humo** de las tres chimeneas de la otra orilla del canal: penachos grandes y lentos que el viento lleva hacia el este.

## Pruebas
- `Tests/EditMode/TrafficMathTests.cs` y `FreightRunTests.cs`: la lógica pura.
- `Tests/PlayMode/TrafficPlayTests.cs`: en la escena, que los coches circulen, no se metan unos en otros ni se salten los rojos, y que el autobús dé la vuelta; y que el paso a nivel de S CALDER AVE se cierre con el tren, que ningún coche entre en la vía y que las barreras vuelvan a subir.
- `Tests/PlayMode/TrafficCapturePlayTests.cs` (a mano): capturas en `ingame/trafico/`.
