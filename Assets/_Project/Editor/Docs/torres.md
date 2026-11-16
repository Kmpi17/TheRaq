# Manzana central: `PrototypeSceneBuilder.Towers.cs`
`BuildTowersBlock` se llama después de `BuildHousesDistrict`, porque usa los materiales de `PrepareHouses` y `PrepareVegetation` y los del barrio de viviendas (`LoadMat`). Referencias: `img/estructures/thetowers1-3`. Todo lo aleatorio sale de `System.Random(2013)`.

- **Suelo** (`BuildTowersGround`):
  - `CespedSeco` en toda la manzana menos los aparcamientos, troceado con `RectMinus`;
  - acera `AceraBarrio` de 3 m alrededor, cortada con `StripMinus` en las entradas;
  - entradas de asfalto (`Callejon`) a `LotTop` = 0.03.
- **Caminos** (`TowersWalk`, `AceraBarrio`, 0.12 de alto y sin collider): se recortan contra los ya puestos para que no se solapen. Son estos:
  - el eje E–O en z −8.2…−5.8;
  - de T2 a P2 y de P2 a la acera sur;
  - de T1 a la acera norte;
  - de T3 a la acera este y de T3 al eje.
- **Torres** (`HighRise`, 27×15 m, 3 m por planta), con el portal en la cara −Z local. Con un `TowerOpenings` (solo T2) la torre se construye hueca por dentro: `OpenFacade` hace la fachada en láminas de vanos y plantas enteros dejando abiertas las celdas de `Holes` (cara 0 = −Z, 1 = +X, 2 = +Z, 3 = −X), `SubtractBounds` parte el collider macizo en cajas `Volumen` que dejan libres las salas de `Voids` (la raíz lleva `SolidVisual`), el zócalo va en anillo y, con `OpenPortal`, el portal es un hueco con la puerta abierta. Tablones y parabólicas no tapan los huecos:
  - **T1** (−62, 40): yaw 180, 12 plantas, `FachadaLadrillo`.
  - **T2** (0, 2): yaw 0, 14 plantas, `FachadaLadrilloAire`.
  - **T3** (65, 10): yaw −90, 11 plantas, `ApartamentoLadrillo`.
  - Cada una tiene:
    - zócalo, forjados vistos en cada planta, pilastras, cornisa, tejado, caseta, depósito y antena;
    - portal con marco, puerta de cristal, marquesina, luz emisiva y escalón;
    - dos pilas de balcones en la cara +Z (x ±6) con `MallaMetalica`, pasamanos y a veces ropa;
    - ventanas tapiadas (25 % en la planta baja y 4 % arriba), 6 parabólicas (`SatelliteDish`), grafitis, losa con 2 contenedores, maleza y nieve.
- **Adosadas** (`Rowhouses`):
  - El origen está en el centro de la fachada y el +Z local va hacia dentro. Tienen 9 m de fondo, 2 plantas, `n = round(largo / 6.5)` casas y `GableRoof` con `ridgeAlongX`.
  - El tono del ladrillo cambia por parejas y los portales van juntos en la medianera.
  - Cada casa tiene rellano, escalón, puerta de color o tapiada (12 %) en el vano de la casa más cercano a la medianera (`FacadeDoor`), tejadillo, camino de `yard` m, bajante, a veces aire acondicionado o parabólica, puerta trasera, patio de hormigón y un `TrashCart`.
  - Con `frontFence`, llevan valla baja propia; solo la tienen las que dan al eje, porque las demás ya tienen la del perímetro.
  - Las filas son:
    - (−65, 8) yaw 0 y (−65, −22) yaw 180, de 46 m, con jardín de 13.8 m hasta el eje;
    - (5, 60) yaw 180 de 44 m y (62, 60) yaw 180 de 40 m;
    - (90, −28) yaw −90 de 38 m.
- **Aparcamientos** (`ParkingLot`), con las medidas en `TowersLots` y las entradas en `TowersDriveways`:
  - Plazas de 2.6×5 m, pasillos de 7 m y, si el fondo llega a 33 m, fila doble en el centro.
  - Se quitan las plazas que pisan la entrada o el paso de peatones (`TowersCrossing`).
  - En cada plaza hay un tope; el 55 % tiene `ParkedCar` y el 8 % un montón de nieve. Las líneas de `LineaAparcamiento` solo se pintan junto a plazas que existen.
  - Hay manchas de `Aceite` y dos `StreetLight` en el césped del borde norte (con la base bajada a `LawnTop − SidewalkTop`).
- **Patios** (`BuildTowersYard`):
  - cámara de vigilancia delante de T2;
  - losa con `PortableHoop` en (−26, 30);
  - 5 `Clothesline` y `OldSofa` en (50, −30);
  - `DesirePath` (manchas de `Barro` encadenadas);
  - en el eje, `ParkLamp` al norte y `Bench` con papelera al sur cada 10 m, solo donde hay sitio libre.
- **Valla del perímetro** (`BuildTowersFence`, `FenceWithGates`): `VallaHierro` a 0.6 m del borde del césped. Se abre en cada punto de `towersGates` (los caminos que llegan a la acera, incluidos los de cada adosada) y en las entradas.
- **Clima y basura** (`BuildTowersWeather`): 34 `DistrictTree`, 70 placas de nieve, montones junto al eje, maleza junto a la valla, basura en caminos y aparcamientos, bolsas y charcos.
- `towersOccupied` guarda lo que ocupa cada cosa, para que el relleno aleatorio no caiga encima (`TowersFree`, `TowersRandomPoint`). `FrameRect` pasa un rectángulo local de una fila o torre a coordenadas del mundo.

Materiales nuevos (color liso): `LineaAparcamiento`, `AntenaParabolica`, `AparatoAire`, `PuertaRoja`, `PuertaVerde`, `PuertaAzul`, `Ropa*`, `SofaNaranja`, `BasePlastico`, `TableroPortatil`, `AroCanastaPortatil` y `LuzPortalTorre` (emisivo).

El plano (`mapa/generar_mapa.py`) copia esta disposición a mano en las constantes `T_*`.
