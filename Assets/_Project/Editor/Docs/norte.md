# Barrio norte: `PrototypeSceneBuilder.North.cs` y `.NorthBuildings.cs`
`BuildNorthBand(world)` (objeto `Barrio_Norte`) se llama justo después de `BuildTowersBlock`, porque usa los materiales de casas, negocios y torres. Va de z 298 a `WorldNorthZ` (500), en coordenadas del mundo. Ficha de la zona: [barrio-norte.md](../../../../docs/estructuras/zonas/barrio-norte.md).

## Trazado (`North.cs`)
- **Constantes:** `BoulevardZ` 395, `BoulevardLane` 8, `BoulevardMedian` 6, `BoulevardHalf` 11; `PrescottZ0/Z1` 486/496; `NorthWallZ` 499.5; `EmbankmentTop` 5.8; `NorthEdgeX` 300; `NorthSpans` (los tres tramos entre las calles grandes). La diagonal: `DiagC` 421, `DiagHalf` 5 y `DiagLine(across) = DiagC + across·√2`, que da el valor de x + z de la línea paralela al eje desplazada `across` metros.
- **Rectángulos:** `NbJ`, `NbGSouth/North`, `NbWebleySouth/North`, `NbCourt`, `NbBulb` (+ `NbBulbRadius`, `NbBulbSquare`), `NbMallLot` y sus tres entradas, y `NbTowerLot`/`NbTowerDriveway`.
- **Integración con el resto:** `NorthBandAvenueCutsEast/West` cortan las aceras de las calles grandes (`BuildStreets`); `NorthBandMouths()` va a `MainRoadMouths`; `NorthBandStreets()` va a `NamedStreets()` (el bulevar entra como dos calzadas con el mismo nombre). `TrainCrossings` deja sin pórtico z 318, 395, 439.5 y 491.
- **Tres `BuildHousesPatch`** (oeste 3001, centro 3003, este 3005), cada uno con calles, callejones, aceras, céspedes, filas (`North*BandRows`) y su callback de vida (`North*BandLife`): farolas, tiendas sueltas (`NorthShop`), postes con cables, coches y lo propio de cada parte.
- Después: `BuildBoulevard`, `BuildEmbankment`, `BuildNorthSkyline` y `ExportHousesFootprints()`.

## Piezas (`NorthBuildings.cs`)
- **`Prism(name, parent, polygon, y0, y1, mat, topTile, sideTile, uAxis, uvOrigin, collider, top, sides)`:** prisma de base poligonal convexa con malla propia, UV en metros (arriba según `uAxis`, en los lados a lo largo del perímetro) y `MeshCollider` convexo. Sirve para todo lo que no va alineado con los ejes (diagonal, cuña, rotonda). `Ccw`, `OffsetPoly` (desplaza hacia fuera con esquinas a inglete), `FacePanel` (panel delante de un lado) y `Pyramid`.
- **`RecordPolygon(kind, corners, height, occupy)`:** huella para el plano y rectángulo ocupado (`housesOccupied`).
- **`NorthShop(parent, kind, front, facing, width, floors, rng)`:** un `Storefront` en un marco girado para que la fachada quede en `front` mirando a `facing`; conserva `ShopLife` (horario y sitios de NPC). `VacantUnit` es el local vacío (FOR LEASE).
- **`BuildDiagonal`** (calzada con la textura de calle girada 45°, aceras, esquinas, cuña de césped), **`BuildFlatiron`**, **`BuildStripMall`**, **`BuildNorthPark`** (+ `ParkPath`, `Playground` y el `ParkLamp` de StreetProps), **`BuildCulDeSac`**, **`CourtyardBlock`**, **`NorthTowerBlock`** (`HighRise` girado 90°, `ParkingLot`, `Court`), **`NorthChurch`** (`GableRoof`, `PlainBrick`), `Billboard`, `RoadClosed`, `ShopCart`.
- **`BuildBoulevard`:** calzadas con `ProceduralTextures.Road(..., oneWay: true)` (discontinua blanca), medianas con árboles y farolas dobles, y asfalto liso en los huecos de los cruces.
- **`BuildEmbankment`** + **`Underpass`:** muro con `RetainingWall`, cuerpo del terraplén, grava, vías, vagones (`Corrugated`), árboles y maleza; pasos inferiores en x = ±110 con fondo negro con collider. Su luz no lleva `NightLight` (siempre encendida, está a oscuras).
- **`BuildNorthSkyline`:** siluetas sin colliders de z 548 a 800.
- **`BuildNorthStreetDetail`** (lo llama `BuildStreetDetail` al final): semáforos en los dos cruces del bulevar con las calles grandes y STOP con placas en los dos extremos de la diagonal. En `BuildStreetApproaches`, las calzadas del bulevar no llevan STOP: en las calles pequeñas tienen preferencia y en las grandes solo llevan paso de cebra y línea de detención en el extremo al que se llega.

## Otros cambios para el norte
- `WorldNorthZ` (PrototypeSceneBuilder.cs): suelo, calles grandes, aceras de enfrente, tejado del túnel de la autopista y vía del elevado llegan hasta ahí (`TrainTrackEndZ` 540).
- `ShopKind` suma `Dollar`, `Phones`, `Pizza` y `Tax`.
- NPC: sitios de paso en `CivilianSpots` (bulevar, centro comercial, parque, patio, iglesia, torre).
- Huellas para el plano: `edificio`, `torre`, `iglesia`, `centro`, además de las de casas y tiendas. Capturas: elemento `norte`.
