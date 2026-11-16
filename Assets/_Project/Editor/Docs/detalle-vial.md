# Detalle vial: `PrototypeSceneBuilder.StreetDetail.cs`

`BuildStreetDetail(world)` va justo después de `BuildMainRoadDetails`, cuando ya están todas las zonas. Pone el detalle de calle de toda la ciudad bajo `Mundo/DetalleVial`; los semáforos van aparte, en `Semaforos`, fuera de `Mundo`, porque cambian. El diseño y los nombres están en [`docs/estructuras/calles.md`](../../../../docs/estructuras/calles.md).

- **`NamedStreets()`**: lista de calles (`Street`: rectángulo de calzada en el mundo, nombre, si es grande y el ancho de su acera). Sale de las tablas de cada zona (`HousesRoads` y su copia con `HousesWestRect`, `WestExtRoads`, `NorthEastRoads`, `NorthWestRoads`, `FarWestRoads` y la calle del barrio de viviendas) y de las cuatro calles grandes. El quiebro `Cq` no cuenta como calle. **Toda calle nueva se añade aquí con su nombre.**
- **`BuildStreetApproaches`**: recorre los dos extremos de cada calle de barrio. Si medio metro más allá empieza otra calle perpendicular con otro nombre, es una llegada. Ahí pone:
  - un paso de cebra (`Crosswalk`, calcomanía transparente; solo si la otra es una calle grande), en los últimos metros de la calzada, del ancho de la acera que cruza;
  - una línea de detención en el carril de la derecha;
  - un `StopPost`: poste galvanizado con la señal (`StopSign`, cara y trasera con culling) y dos placas de nombre (`NameBlades`) encima, en la acera de la derecha;
  - con un 50 % de probabilidad, una boca de incendio (`Hydrant`) en la esquina de la izquierda.
- **`BuildTrafficSignals`**: en los cuatro cruces de las calles grandes, un `SignalPost` en cada esquina (a 7.9 m del centro, o más lejos si no cabe). Cada poste lleva un `SignalHead` para X y otro para Z (cuerpo, placa, viseras y lentes apagadas, más las encendidas: `Luz_Roja_X`, `Luz_Ambar_X`, `Luz_Verde_X`…) y un `PedestrianHead` para cada eje (`Peaton_Mano_X`, `Peaton_Anda_X`…). El cruce lleva un `TrafficLight` (ver [World/README](../../Scripts/World/README.md)), desfasado 11.5 s respecto al anterior. Las luces quedan como en el primer segundo del ciclo (verde X, rojo Z), para que se vean en las capturas.
- **`BuildLaneArrows`**: flechas (`LaneArrow`) a 9.5 m del cruce en los dos carriles que llegan. En el de dentro va el giro a la izquierda si hay calle a ese lado; en el de fuera, recto, o a la derecha si no se puede seguir recto (la T de la avenida).
- **`BuildMainRoadPoles`**: postes de madera (`UtilityPole`) cada 34 m en la acera de fuera de las calles norte, sur y oeste. Evitan los cruces, las bocas (`MainRoadMouths`) y lo que ocupe el sitio (se corren hasta 4 m). Llevan tres cables de tendido (`Wire`) y uno de teléfono más bajo; los vanos de más de 75 m no llevan cable.
- **`BuildSpeedLimitSigns`**: placa SPEED LIMIT 30 (`Placard`) cada 120 m en las dos aceras de las calles grandes, lejos de los cruces.
- **`BuildLocalSewers`**: tapas de alcantarilla cada 35–55 m y sumideros cada 28–42 m en cada bordillo de las calles de barrio. La calle de Kessler y las calles grandes ya tenían los suyos.

**Que nada se meta en otra cosa:** `SpotFree(suelo, radio)` hace un `Physics.CheckCapsule` de 0.55 a 3.2 m de alto, después de `Physics.SyncTransforms()`, contra todos los colliders ya construidos (farolas, árboles, vallas, coches aparcados, fachadas…). Lo que no tiene collider (basura, calcomanías) no cuenta.

**Texturas nuevas** en `ProceduralTextures`: `StopSign` (octógono con recorte por alfa), `Placard` (placa de varias líneas), `Crosswalk` (barras con transparencia, una por ancho de calle: `PasoCebra10` y `PasoCebra12`) y `LaneArrow` (recta, izquierda y derecha). Las placas de nombre usan `Sign` (como los letreros de las tiendas), con un poco de brillo para que se lean en la niebla.

**Semilla:** `System.Random(1212)`.

**Capturas:** elemento `detalle_vial` en `IngameScreenshots`.

**Limitaciones:** no hay tráfico que obedezca a los semáforos (los coches de la autopista van por su cuenta), los NPC no esperan al muñeco para cruzar, y en los cruces entre calles de barrio no hay paso de cebra (solo STOP y línea de detención).
