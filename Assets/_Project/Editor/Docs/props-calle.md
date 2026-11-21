# Detalle callejero: `PrototypeSceneBuilder.StreetProps.cs`
`BuildDistrictProps` llama, en orden, a:
- `BuildFences`: valla de hierro entre acera y césped, abierta donde llegan los caminos.
- `BuildCourt`: pista con dos canastas (tablero, aro y red de cadenas), malla metálica de 3.5 m con puerta hacia la calle, bancos y papelera.
- `BuildPlaza`: suelo de losas, muro con grafitis, 4 bancos, mesa de picnic y papeleras (una volcada).
- `BuildWreck`: coche oxidado sin ruedas sobre bloques, con el capó abierto y neumáticos tirados.
- `BuildBusStop`: marquesina con banco y anuncio, y poste de parada.
- `BuildUtilityPoles`: postes de madera en la acera sur con cruceta, transformadores y 3 cables colgantes (`Wire`, un LineRenderer en parábola), más unas zapatillas colgadas.
- `BuildBushes`: arbustos delante de cada fachada (con huecos, algunos secos).
- `BuildTowerApproaches`: accesos a las torres. Al empezar el camino, pilonas de hormigón y tablón de anuncios. A lo largo, barandilla baja (`LowRail`), farolas de parque con luz (`ParkLamp`) y bancos sobre losa. Delante del portal, una explanada con jardineras (`Planter`), bancos, aparcabicis (`BikeRack`), papelera y cámara con luz azul (`SecurityCamera`). Usa su propio `System.Random(77)` y reserva su espacio en `occupied`. `BuildTrash` y `BuildGrime` añaden además basura, manchas y un charco en cada camino.
- `BuildLawnTrees`: 22 árboles grandes repartidos por el césped.
- `BuildDumpsters`: losa con 2 contenedores junto a cada torre, bolsas de basura, un sofá y un colchón.
- `BuildTrash`: papeleras en la acera (alguna volcada) y ~240 desperdicios (papeles, vasos, latas, bolsas, botellas) en la cuneta, la acera y el césped.
- `BuildGrime`: manchas en la acera, aceite y charcos en la calzada, calvas y atajos de barro en el césped, y alcantarillas.
- `BuildGraffiti`: grafitis en los laterales de los bloques bajos, en la planta baja de las torres y en el muro de la plaza.
- `BuildStreetFurniture`: bocas de incendio y pilonas rojas que cierran el final de la calle.
- `BuildSnowRemnants` y `BuildOvergrowth`: están en `Vegetation.cs` (ver abajo).

Para no meter árboles ni basura dentro de los edificios, `DistrictOccupiedAreas` devuelve los rectángulos ocupados (edificios, caminos, cancha, plaza, contenedores, coche) y `RandomLawnPoint` busca un punto libre en el césped.

Helpers: `EmissiveMat`, `PlaceLitter`, `FenceSegment` (tramo de valla entre dos puntos con postes y barandilla; el panel lleva `BreakableFence` con sus postes y barandilla, para que un coche lo rompa, salvo en el borde del mundo, `FenceOnWorldEdge`), `Bench` (con `BreakableProp`), `Bench`, `TrashCan`, `Dumpster`, `GarbageBag`, `Decal` (quad horizontal transparente), `WallGraffiti`, `Strut` (barra entre dos puntos), `CutoutMat` y `TransparentMat`.

Los números aleatorios usan semillas fijas (`System.Random(7/42/77/99/123/314/315/808/909/2012)`), así que regenerar la escena da siempre el mismo resultado.
