# Barrio de casas: `PrototypeSceneBuilder.Houses.cs`
`BuildHousesDistrict` (se llama en `Build()` justo después de `BuildProjectsDistrict`, porque reutiliza sus materiales y los de la vegetación). Zona: x ∈ [120, 250], z ∈ [90, 214].

**Dos copias.** `BuildHousesVariant(world, name, origin, yaw, seed, west)` construye el barrio entero bajo un objeto propio:
- `Barrio_Casas`: en su sitio (origen 0, semilla 1999).
- `Barrio_Casas_Oeste`: el mismo trazado girado 180° con origen `HousesWestOrigin` = (220, 0, 316), así que mundo = (220 − x, 316 − z). Queda en x ∈ [−30, 100], z ∈ [102, 226], con la entrada en la acera oeste de la avenida. Semilla 2001.

Todo lo de abajo (tablas, filas, `BuildHousesWeather`) está en coordenadas **locales** del barrio; el giro lo pone el objeto padre. Con `west`, `BuildHousesStreetLife` abre la valla del callejón de atrás de los negocios en el callejón a5 y cierra entera la del otro extremo, y abre la valla este (local) en `WestExtFenceGapsLocal`, donde entran los callejones de la ampliación. Después se llaman `BuildHousesWestExtension` y `BuildShopsStrip` y, al final, `ExportHousesFootprints` una sola vez.

`BuildHousesGround(parent, roads, alleys, sidewalks, lawns)` y `BuildHousesWeather(parent, rng, roads, alleys, sidewalks, lawns)` reciben las tablas, así sirven para las copias y para la ampliación.

**Ampliación norte** (`PrototypeSceneBuilder.HousesNorth.cs`, `BuildHousesNorth`, objeto `Barrio_Casas_Norte`). En coordenadas del mundo, hasta `HousesNorthMaxZ` = 298. Calles rectas que siguen las existentes: F/Fe (z 248–258) y H (z 288–298) de la calle x = −110 a G cruzando la avenida, B' prolongada de z 224 a F y B prolongada como G de z 202 a H. Dos trozos hechos con `BuildHousesPatch(parent, name, seed, roads, alleys, sidewalks, lawns, rowSpecs, streetLife)`, que pone suelo, filas (tuplas nombre, fachada, yaw, largo, fondo, tipo), vida de calle y tiempo:
- `Este` (semilla 2005): `NorthEastRoads` (Fe, G = x 207–217 de z 202 a 298, y el tramo este de H), el callejón del este (sigue a a5 hasta z 285.5), aceras de G cortadas en el callejón a4 y `NorthEastRows` (apartamentos y casas al sur de Fe hasta a4, casas y solar al norte de Fe hasta la acera de H, apartamentos y solar a lo largo de G).
- `Oeste` (semilla 2007): `NorthWestRoads` (F, la prolongación de B' en x 3–13 y el tramo oeste de H), el pasaje x −42…−37 entre F y H y `NorthWestRows` (al sur de F, fondos hasta los callejones a1'/n3; al norte, hasta la acera de H).
- `NorthAvenueRoadsEast`/`NorthAvenueRoadsWest`: F/Fe y H, que cortan las aceras de la avenida (`BuildStreets`, `BuildShopsStrip`); `TrainCrossings` incluye z 253 y 293.
- En `BuildHousesVariant`, el barrio del este no pone el césped del final de B (`HousesLawnBNorth`) y `BuildHousesStreetLife` solo pone barrera en el extremo de B que no sigue: el sur en el este y el sur del mundo (local 201.4) en la copia oeste.
- El barrio del este ya no pone valla en su borde norte (z 213.8), ni la copia oeste en el suyo (local z 91.2), ni la ampliación oeste en z 225.8: las parcelas nuevas tienen su valla trasera al otro lado del callejón.

**Ampliación oeste** (`PrototypeSceneBuilder.HousesWestExt.cs`, `BuildHousesWestExtension`, semilla 2003). En coordenadas del **mundo** (`housesFrame` = null): x ∈ [`WestExtMinX` = −103, −30], z ∈ [90, 226]. Tablas `WestExtRoads` (calles D y E, sin salida en `WestExtStreetEnd` = −36), `WestExtAlleys` (n1–n3, que llegan a x = −26 para unirse al callejón a5 de la copia), `WestExtSidewalks` (incluye la del lado este de la calle x = −110) y `WestExtLawns`. Filas: `CalleNorte` (frente a la calle norte, 12.5 m de fondo), `D_Sur`, `D_Norte_Solar` + `D_Norte`, `E_Sur_O` + `E_Sur_Solar` + `E_Sur_E`, `E_Norte_Apart` (37 m de apartamentos) y `E_Norte`. `BuildWestExtStreetLife`: farolas, postes con cables por los callejones, coches y barreras con pilonas al fondo de D y E.

**Trazado.** Lo definen cuatro tablas de rectángulos `Rect.MinMaxRect(x0, z0, x1, z1)`:
- `HousesRoads`: calzadas de 10 m. Son A, B (sin salida), C1, C2 y el quiebro Cq.
- `HousesAlleys`: callejones de 5 m, que quedan a `AlleyTop` = 0.12 m.
- `HousesSidewalks`: aceras de 2.5 m.
- `HousesLawns`: manzanas con césped.

`StripMinus` trocea cada acera donde la cruzan calzadas, callejones o aceras ya puestas, para que no haya solapes ni z-fighting. `BuildStreets` corta la acera de enfrente de la avenida donde entran A y C1.

**Filas de parcelas.** `Row(parent, name, frontCenter, yaw, length, depth, kind, rng)` crea un marco local:
- el origen está en el centro de la línea de fachada, junto a la acera;
- +Z apunta hacia dentro de la parcela (yaw 0: parcela al norte; 180: al sur; 90: al este);
- `depth` es la distancia hasta el callejón de atrás.

Tipos de fila:
- `Houses`: casas de 6.5–8.5 m con pasillos de 1.4–3.4 m entre ellas y un 12 % de solares (la mitad `OvergrownLot`, la otra mitad `VacantLot`).
- `Apartments`: uno o dos bloques de 3 pisos.
- `Vacant`: un solar vallado (`VacantLot`).
- `Overgrown`: un solar comido por la vegetación (`OvergrownLot`).

**Piezas:**
- `House`: casita de ladrillo con `GableRoof` (hastial de ladrillo liso con ventana de desván) o greystone (tejado plano, cornisa e imposta a 2.8 m, escalinata de 0.6 m con barandilla de hierro con postes sobre el borde de los escalones). La puerta va en el vano de una esquina (`FacadeBayCenter`) con `FacadeDoor`, por debajo de la imposta. Lleva escalones, camino y, a veces:
  - valla baja de hierro;
  - árbol (`DistrictTree`);
  - tapiada (`BoardUpWindows`), con contrachapado sobre cada ventana de la textura menos la del vano de la puerta.

  Detrás tiene valla de madera (`WoodFenceLine`), garaje si caben 14 m de fondo (`Garage`), cubos con ruedas en el borde del callejón (`TrashCart`) y maleza.
- `Apartment`: 9 m de fachada con ventanas (3 pisos) más un antepecho de ladrillo liso de 0.75 m (`AptParapetHeight`, textura `PlainBrick`; sin él la puerta trasera del 3.er piso asomaba por encima del muro), con miradores (cada uno centrado en un vano), zócalo con bloque de vidrio, portal de piedra (en el vano más cercano al centro que no choca con un mirador; dintel a 3.3 m, por debajo del alféizar del primer piso), valla baja, porches traseros (`BackPorch`) y patio de hormigón con contenedor y coche (`ParkedCar`). Deja 6.5 m detrás del edificio para el porche y el patio.
- `BackPorch` (marco local: fachada en z = 0, porche hacia +Z): 3 terrazas de 5.4 × 2 m a 1.07, 4.07 y 7.07 m (`PorchDeckTops`, los pisos de 3 m de la textura) con **puerta trasera** en `PorchDoorX`, y una torre de escalera de dos bandas (z 2.0–3.1 y 3.1–4.2) con tramos de medio piso (`PorchFlight`: 1.5 m en 2.4 m, 32°) que van y vuelven entre rellanos en x ±1.2…2.2; el rellano derecho a la altura de cada terraza es su entrada; en el último rellano, que no tiene tramo de salida, una barandilla cierra el hueco sobre la escalera de abajo. Se sube a pie: rampa invisible bajo los peldaños, paredes invisibles a los lados de cada tramo (`StairSideWall`: cajas verticales escalonadas que acaban justo en los extremos del tramo; con una caja inclinada sus esquinas se metían en los rellanos y el jugador se quedaba enganchado al girar) y barandillas de balaustres con colisión (`PorchRail`, textura `PorchRail` con alfa). `Apartment` coloca cada porche para que su puerta caiga en el centro de un vano de la fachada, así tapa la ventana de ese vano en los tres pisos. Lo comprueba `BackPorchPlayTests`.
- `VacantLot`: malla metálica (a veces con un tramo caído), maleza, colchón, neumáticos y nieve.
- `OvergrownLot`: solar abandonado con vegetación (`abandonedParcelVegetacion.png`): abierto a la acera, hierba alta densa (más baja en los 2.5 m de delante), un `DistrictTree` grande (escala 1.1–1.5) cada ~6 m en la mitad del fondo, a veces un `BareTree` seco, matorral (`Arbusto`/`ArbustoSeco`) a lo largo del fondo, losas rotas del camino de la casa que hubo, un atajo de `Barro` en diagonal (`DesirePath`), un poste de cartel torcido, bolsas y nieve. Se registra como `solar`.
- `CornerStore`: tienda en el quiebro de la calle C. Un frente pintado (`FrenteTienda`) tapa hasta 3.2 m las ventanas de la textura, que aquí va estirada a 4.6 m de alto.
- `BuildHousesStreetLife`:
  - farolas de sodio;
  - `PoleLine`: postes de madera con cruceta y 3 cables por los callejones;
  - coches aparcados;
  - barreras, pilonas y nieve en los dos extremos de B;
  - vallas de madera que cierran el barrio.
- `BuildHousesWeather`: maleza en los bordes de los callejones, charcos, barro, bolsas y nieve en ellos, montones en el bordillo, placas en el césped y basura en las aceras.
- `GableRoof(parent, wallTop, width, depth, rise, overhang, roof, wall, ridgeAlongX)`: malla de tejado a dos aguas. Los faldones tienen cara por abajo. Los hastiales usan el material del muro, salvo si es una fachada de ladrillo con ventanas: entonces usan su versión lisa (`plainBrick`), porque la textura de ventanas salía cortada por los faldones. Por defecto la cumbrera va a lo largo de Z, así que el hastial mira a la calle.
- **Puertas y vanos de la fachada.** La textura `BrickFacade` tiene una ventana por vano de `FacadeBay` (3 m, que `TexturedBox` redondea a un número entero de vanos por pared). Una puerta puesta a ojo deja medias ventanas asomando, así que toda puerta de planta baja sobre fachada de ladrillo se coloca con:
  - `BayWidth(width)` y `FacadeBayCenter(wallCx, width, want, margin)`: ancho de vano y centro del vano más cercano a `want`;
  - `FacadeDoor(root, x, bottom, faceZ, outward, bayW, doorW, doorH, leaf, frame, maxTop)`: marco que tapa entera la ventana del vano (47 % del ancho del vano, del alféizar a 2.48 m) y la hoja delante. Se usa en casas, adosadas (delante y detrás) y en la puerta trasera de los locales.
- **Ladrillo liso.** `PrepareHouses` crea, para cada fachada de ladrillo, un material `<Fachada>Liso` con `ProceduralTextures.PlainBrick` (256 × 64 px = 3 × `PlainBrickHeight` m, mismas hiladas, sin ventanas) y lo guarda en `plainBrick`. Lo usan los hastiales y el antepecho de los apartamentos.

**Huellas.** `RecordFootprint` guarda cada edificio en coordenadas del mundo. Sirve para dos cosas:
- `housesOccupied`, para no poner nieve encima. Este se guarda en el marco del barrio que se está construyendo (`housesFrame`), que es el de las tablas; con `housesFrame` = null, en el mundo;
- `ExportHousesFootprints`, que escribe `mapa/edificios_casas.json`; lo lee `mapa/generar_mapa.py` para dibujar las casas en el plano.

Semillas: `System.Random(1999)` (este) y `2001` (oeste).

| Material | Textura | Notas |
|---|---|---|
| `CasaLadrilloRojo`, `CasaLadrilloOscuro`, `CasaLadrilloAmarillo`, `CasaPiedraGris`, `ApartamentoLadrillo`, `ApartamentoLadrilloClaro` | `BrickFacade` con otros colores | tile 3 × 3 m |
| `TejasCasa` | `Shingles` | tile 2 m |
| `VallaMadera` | `WoodFence` (tablones) | 2 × 1.8 m |
| `Callejon` | `Alley` (asfalto roto, grava, barro, canaleta) | 5 m |
| `CalleCasas` | `Road` de 10 m | |

Con color liso: `PiedraCornisa`, `PuertaCasa`, `Contrachapado`, `MaderaPorche`, `PuertaGaraje`, `BloqueVidrio`, `Garaje*`, `CuboAzul`, `CuboNegro`, `PinturaAzulOscuro` y `LetreroRojo`. Reutiliza `AceraBarrio`, `CespedSeco`, `VallaHierro`, `MallaMetalica`, `TejadoAlquitran`, `Grafiti_*`, `Barro`, `Charco` y `Aceite` (`LoadMat`).
