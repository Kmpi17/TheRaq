# Editor: `PrototypeSceneBuilder` y `ProceduralTextures`

`PrototypeSceneBuilder` es una clase `partial` repartida en varios archivos:
- `PrototypeSceneBuilder.cs`: escena, calles, alrededores y barrio de viviendas.
- `PrototypeSceneBuilder.StreetProps.cs`: detalle callejero del barrio de viviendas.
- `PrototypeSceneBuilder.Vegetation.cs`: árboles, restos de nieve y maleza del barrio de viviendas.
- `PrototypeSceneBuilder.Houses.cs`: barrio de casas, al norte del barrio de viviendas.
- `PrototypeSceneBuilder.Towers.cs`: manzana central (conjunto de torres, adosadas y aparcamientos).
- `PrototypeSceneBuilder.Train.cs`: tren elevado sobre la avenida principal.
- `PrototypeSceneBuilder.Cars.cs`: modelos de coche (aparcados, el que se conduce y el abandonado).

Menú **GTAChiraq > Construir escena prototipo**. Genera `Assets/_Project/Scenes/Prototype.unity` desde cero. Se puede ejecutar todas las veces que quieras: sobrescribe la escena y reutiliza los materiales.

> La escena es un **producto generado**. Para cambiar el mundo de prueba, edita este script y regenera la escena; no la toques a mano.

## Qué hace `Build()`, en orden
0. **`PrepareCars()`**: materiales de los coches (los usan todas las zonas).
1. **`EnsureUrp()`**: si `GraphicsSettings.defaultRenderPipeline` no es URP, crea `Settings/URP_Renderer.asset` y `Settings/URP_Pipeline.asset` y los asigna.
2. **`SetupLighting()`**: luz direccional fría y suave (cielo nublado), ambiente plano gris, niebla lineal de 25 a 260 m y sin skybox.
3. **Mundo** (todo bajo el objeto `Mundo` y marcado como estático):
   - `BuildStreets`: suelo de asfalto, aceras de enfrente y líneas amarillas. La acera norte se corta en la entrada del aparcamiento de los negocios y en el pasaje al callejón (`ShopsLotDriveway`, `ShopsAlleyLink`). El césped y la acera de la manzana central los pone `BuildTowersBlock`.
   - `BuildSurroundings`: filas de casas de 2 plantas al otro lado de las calles norte y sur, una tienda de esquina con letrero y coches aparcados estáticos.
   - `BuildProjectsDistrict`: barrio de viviendas **texturizado** al este de la avenida (ver abajo). Al final llama a `BuildDistrictProps` (detalle callejero).
   - `BuildHousesDistrict`: los dos barrios de casas (el del este y su copia oeste, girada 180°) y la franja de negocios (ver abajo).
   - `BuildTowersBlock`: manzana central (ver abajo). Va después de los dos barrios porque reutiliza sus materiales.
   - `BuildElevatedTrain`: tren elevado y estación (ver abajo).
   - `BuildStreetLights`: farolas cada 30 m con luz puntual de sodio naranja, sin sombras.
   - `BuildSnow`: montones de nieve sucia junto al bordillo (salvo en las entradas de los aparcamientos).
4. **`BuildPlayer`**: crea el objeto `Jugador` (tag `Player`, capa 2) con CharacterController, PlayerMovement, VehicleEntry, una cápsula y una visera que marca hacia dónde mira.
5. **`BuildCar`**: crea `Coche_Sedan` (capa 2) con Rigidbody de 1400 kg, el modelo de sedán de `Cars.cs` (pintura `CarroceriaJugador`, llantas cromadas, faros encendidos y dos focos), 4 WheelColliders con ruedas `CarWheel` y un `PuntoSalida` a la izquierda. El modelo se baja para que su suelo coincida con el apoyo de las ruedas en reposo (`0.4 − 0.25·(1 − 0.5) − radio`).
6. **`BuildCamera`**: crea `Main Camera` con ThirdPersonCamera y conecta las referencias con `SerializedObject`.
7. Guarda la escena y la pone la primera en Build Settings.

## Disposición (metros, Y arriba)
- Manzana central: x ∈ [−100, 100], z ∈ [−70, 70] (detalle en *Manzana central*).
- Acera: 3 m. Calzada: 14 m. Calles E–O en z = ±80 y N–S en x = ±110.
- Jugador en la acera sur (−10, 0.2, −71.5). Coche en la calzada sur, mirando hacia −X, a unos 6 m del jugador.
- Barrio de viviendas: x ∈ [120, 250], z ∈ [−88, 88]. Calle E–O de 12 m en z = 0, que sale de la avenida este (la acera de enfrente se corta ahí). Bloques bajos de 3 plantas (24×9×12 m) en z = ±23 y torres de 8 plantas (30×24×15 m) en z = ±62, con portal y camino hasta la acera. Lo controlan `DistrictMinX`, `DistrictMaxX`, `DistrictHalfZ`, `DistrictRoadWidth`, `DistrictWalkupsX`/`DistrictTowersX`, `DistrictWalkupZ`/`DistrictTowerZ` y `WalkupSize`/`TowerSize`.
- Barrio de casas: x ∈ [120, 250], z ∈ [90, 214]; su copia oeste, x ∈ [−30, 100], z ∈ [102, 226]; franja de negocios, x ∈ [−30, 100], z ∈ [90, 102].
- En el barrio: cancha de baloncesto en (232, 58), plaza en (232, −58) y coche abandonado en (241, −34) (constantes al principio de `StreetProps.cs`).
- Las constantes `BlockHalfX`, `BlockHalfZ`, `SidewalkWidth` y `RoadWidth` controlan esta disposición.

## Paleta y materiales
Los materiales se crean con `Mat(name, color)` (URP/Lit) en `Assets/_Project/Materials/<name>.mat`. Si ya existen, se reutilizan y se les actualiza el color.

| Elementos | Materiales |
|---|---|
| Edificios | `Ladrillo`, `LadrilloOscuro`, `FachadaGris`, `FranjaVentanas`, `Cornisa` |
| Suelo | `Asfalto`, `Hormigon`, `NieveSucia`, `LineaAmarilla` |
| Farolas | `Poste`, `LuzSodio` (emisiva), `LuzFarolaParque` y `LuzAzulCamara` (emisivas, en los accesos a las torres) |
| Jugador | `Sudadera`, `Gorra` |
| Coches | `Pintura*`, `CristalTintado`, `Neumatico` |
| Otros | `Corteza`, `Letrero` |

Los materiales con textura se crean con `TexturedMat(name, texture)` (color base blanco + `_BaseMap`):

| Material | Textura (`Assets/_Project/Textures/`) | Tile |
|---|---|---|
| `FachadaLadrillo`, `FachadaLadrilloAire` | ladrillo rojo con una ventana de guillotina; la segunda con aire acondicionado | 3 m × 3 m (un vano, una planta) |
| `CalleBarrio` | asfalto gris gastado con grietas, bordes blancos y doble línea amarilla | ancho de la calle (U cruza la calle) |
| `AceraBarrio` | losas de hormigón de 1.5 m con juntas y manchas | 3 m |
| `CespedSeco` | césped de invierno con calvas | 4 m |
| `TejadoAlquitran` | tela asfáltica | 4 m |

Además, `PuertaMetal` (color liso) para los portales.

Detalle callejero (`StreetProps.cs`):

| Material | Textura | Tipo |
|---|---|---|
| `VallaHierro` | barrotes con lanzas (2 m × 1.5 m) | recorte (`CutoutMat`) |
| `MallaMetalica` | malla de rombos con óxido (1 m) | recorte |
| `Grafiti_0..3` | garabatos abstractos con contorno y chorretones | recorte |
| `PistaBaloncesto` | asfalto con líneas de pista y zona pintada | opaco |
| `Arbusto`, `ArbustoSeco` | hojas de invierno (la seca es la misma textura tintada) | opaco |
| `Suciedad`, `Barro`, `Aceite`, `Charco` | `Mancha` (alfa) con tinte; el charco es muy liso para que brille | transparente (`TransparentMat`) |

Con color liso: `Hierro`, `PosteCanasta`, `Tablero`, `Aro`, `Cadena`, `PosteGalvanizado`, `MaderaBanco`, `MaderaPoste`, `Cable`, `Zapatilla`, `PinturaOxido`, `ContenedorVerde`, `PapeleraVerde`, `BolsaBasura`, `TelaSofa`, `Colchon`, `Basura*`, `BocaIncendio`, `PilonaRoja`.

Los colores globales (`FogColor`, `SodiumLight`) están arriba del script. Es el sitio donde ajustar la estética.

## Helpers
- `Building(parent, name, base, size, wall)`: crea los muros, la cornisa y una franja oscura de ventanas por planta (cada 3.2 m).
- `ProjectBuilding(...)`: edificio del barrio con muros texturizados, tejado con alero (bajo) o cornisa y caseta (torre), portal, marquesina, escalón y camino.
- `TexturedBox(name, parent, pos, size, material, tile, collider, sidesOnly)`: caja con malla propia (`BoxMesh`) y UVs en metros. Cada cara repite la textura un número **entero** de veces (se redondea), así que las ventanas cuadran en las esquinas; en los laterales la V empieza en el suelo. Las mallas quedan guardadas dentro de la escena.
- `BareTree(parent, pos, bark, rng, scale[, out height])`: árbol pelado con tronco y 5 ramas. Devuelve la raíz; la sobrecarga con `out` da la altura del tronco (la usa `DistrictTree` para poner la copa).
- `Box`/`Primitive`: crean primitivas con material y, si se pide, quitan el collider.
- `SetReference`, `SetLayerRecursively`, `MarkStatic`, `AddSceneToBuild`, `EnsureFolder`.

## Detalle callejero: `PrototypeSceneBuilder.StreetProps.cs`
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

Helpers: `EmissiveMat`, `PlaceLitter`, `FenceSegment` (tramo de valla entre dos puntos con postes y barandilla), `Bench`, `TrashCan`, `Dumpster`, `GarbageBag`, `Decal` (quad horizontal transparente), `WallGraffiti`, `Strut` (barra entre dos puntos), `CutoutMat` y `TransparentMat`.

Los números aleatorios usan semillas fijas (`System.Random(7/42/77/99/123/314/315/808/909/2012)`), así que regenerar la escena da siempre el mismo resultado.

## Vegetación de invierno: `PrototypeSceneBuilder.Vegetation.cs`
Solo en el barrio de viviendas.

- `PrepareVegetation()`: crea los materiales una vez por build (lo llama `BuildProjectsDistrict` antes de los árboles). Guarda las referencias en campos estáticos.
- `DistrictTree(parent, pos, bark, rng, scale, looks)`: llama a `BareTree` y, según `looks`, el árbol queda en uno de estos aspectos:
  - **pelado** (35 %);
  - **con hoja verde** (`ArbolHojas`, 20 %), con una copa de 6 esferas;
  - **nevado** (`ArbolNevado`, 20 %): la misma copa con un casquete de nieve encima de cada esfera y una placa de nieve en el suelo;
  - **con hoja seca de invierno** (`ArbolHojaSeca`, 25 %): una copa rala marrón y ocre, que deja ver las ramas.

  El aspecto usa otra semilla (`314` en los árboles de la acera y `315` en los del césped), así que los árboles no cambian de sitio.
- `BuildSnowRemnants` (semilla 909):
  - montones sucios en el bordillo;
  - un montón grande contra las pilonas del final de la calle;
  - unas 40 placas en el césped;
  - nieve a la sombra, detrás de cada edificio (placas y montones pegados a la fachada trasera);
  - ventisqueros junto a la valla.

  Helpers: `SnowMound` (media esfera aplastada, sin colisión ni sombra) y `SnowPatch` (calcomanía a `LawnTop + 0.009`, por encima del barro).
- `BuildOvergrowth` (semilla 808): maleza alta de zona abandonada, en estos sitios:
  - la franja del fondo del barrio, detrás de las torres;
  - el final de la calle (x > 243);
  - alrededor del coche abandonado;
  - 3 solares descuidados en el césped, que se reservan en `occupied`;
  - el pie de la valla y de las fachadas traseras;
  - las grietas de la acera.

  Cada matojo (`Clump`) son 3 quads cruzados a 60°, sin colisión.
- `NearPathGap(x, margin)`: indica si x cae en el hueco de la valla de algún camino.

| Material | Textura | Tipo |
|---|---|---|
| `CopaVerde`, `CopaInvierno` | `Foliage` (hoja verde oscura / marrón y ocre) | opaco |
| `NieveRestos` | `Snow` (blanco azulado con suciedad y grava) | opaco, algo brillante |
| `NieveCesped` | `ManchaNieve` (`Grime`) tintada de blanco | transparente |
| `HierbaAlta`, `Maleza` | `TallGrass` (paja y oliva; la maleza más oscura y con espigas) | recorte |

## Barrio de casas: `PrototypeSceneBuilder.Houses.cs`
`BuildHousesDistrict` (se llama en `Build()` justo después de `BuildProjectsDistrict`, porque reutiliza sus materiales y los de la vegetación). Zona: x ∈ [120, 250], z ∈ [90, 214].

**Dos copias.** `BuildHousesVariant(world, name, origin, yaw, seed, west)` construye el barrio entero bajo un objeto propio:
- `Barrio_Casas`: en su sitio (origen 0, semilla 1999).
- `Barrio_Casas_Oeste`: el mismo trazado girado 180° con origen `HousesWestOrigin` = (220, 0, 316), así que mundo = (220 − x, 316 − z). Queda en x ∈ [−30, 100], z ∈ [102, 226], con la entrada en la acera oeste de la avenida. Semilla 2001.

Todo lo de abajo (tablas, filas, `BuildHousesWeather`) está en coordenadas **locales** del barrio; el giro lo pone el objeto padre. Con `west`, `BuildHousesStreetLife` abre la valla del callejón de atrás de los negocios en el callejón a5 y cierra entera la del otro extremo. Después se llama a `BuildShopsStrip` y, al final, a `ExportHousesFootprints` una sola vez.

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
- `Houses`: casas de 6.5–8.5 m con pasillos de 1.4–3.4 m entre ellas y un 12 % de solares.
- `Apartments`: uno o dos bloques de 3 pisos.
- `Vacant`: un solar.

**Piezas:**
- `House`: casita de ladrillo con `GableRoof` o greystone (tejado plano, cornisa y escalinata con barandilla). Lleva escalones, puerta, camino y, a veces:
  - valla baja de hierro;
  - árbol (`DistrictTree`);
  - tapiada (`BoardUpWindows`), con contrachapado sobre cada ventana de la textura.

  Detrás tiene valla de madera (`WoodFenceLine`), garaje si caben 14 m de fondo (`Garage`), cubos con ruedas en el borde del callejón (`TrashCart`) y maleza.
- `Apartment`: 9 m de alto, con miradores, zócalo con bloque de vidrio, portal de piedra, valla baja, porches traseros de 3 alturas con escaleras en zigzag (`BackPorch`, `Slab`) y patio de hormigón con contenedor y coche (`ParkedCar`).
- `VacantLot`: malla metálica (a veces con un tramo caído), maleza, colchón, neumáticos y nieve.
- `CornerStore`: tienda en el quiebro de la calle C.
- `BuildHousesStreetLife`:
  - farolas de sodio;
  - `PoleLine`: postes de madera con cruceta y 3 cables por los callejones;
  - coches aparcados;
  - barreras, pilonas y nieve en los dos extremos de B;
  - vallas de madera que cierran el barrio.
- `BuildHousesWeather`: maleza en los bordes de los callejones, charcos, barro, bolsas y nieve en ellos, montones en el bordillo, placas en el césped y basura en las aceras.
- `GableRoof(parent, wallTop, width, depth, rise, overhang, roof, wall, ridgeAlongX)`: malla de tejado a dos aguas. Los faldones tienen cara por abajo y los hastiales usan el material del muro. Por defecto la cumbrera va a lo largo de Z, así que el hastial mira a la calle.

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

## Franja de negocios: `PrototypeSceneBuilder.Shops.cs`
`BuildShopsStrip(world)`, llamado desde `BuildHousesDistrict` después de las dos copias (usa sus materiales). Zona: x ∈ [−30, 100], z ∈ [90, 102], entre la acera norte y el callejón de atrás de la copia oeste.

- **Suelo** (`BuildShopsGround`): acera ancha hasta la fachada (`ShopsFront` = 93), césped seco detrás, patio de servicio de `Callejon` tras cada local, el pasaje `ShopsAlleyLink` (x −26…−21) que une el callejón a5 de la copia oeste con la calle, y la **acera oeste de la avenida** (`AvenueWestSidewalk`, x 100–103, z 90–300), cortada con `StripMinus` donde entran A' y C1' (`HousesWestRect` pasa un rectángulo local de la copia al mundo).
- **Locales** (`Shops`, de oeste a este: tipo, x0, x1, plantas). `Storefront(parent, kind, x0, x1, floors, rng)`: caja de ladrillo de 7.5 m de fondo (4.6 o 7.4 m de alto) con pretil; en la planta baja un panel pintado tapa las ventanas de la textura y lleva escaparate (`EscaparateLuz`, emisivo), zócalo, puerta, caja de persiana, rejas o persiana bajada (`PersianaMetalica`), letrero luminoso y cartel OPEN de neón. Detalles por tipo: cruz y cartel de horario (iglesia), toldo (lavandería, barbería, pollo), poste de barbería con textura en espiral, extractor, grasa y mural (pollo), cartel de móviles (cheques), letrero lateral "GROCERY", carteles de ofertas y arcón de hielo (tienda). Detrás: puerta de chapa, contenedor o cubo y bolsas.
- **Letreros**: `SignMat(name, text, bg, ink, seed, glow)` crea un material con `ProceduralTextures.Sign` como textura y mapa de emisión; `SignQuad(parent, center, normal, width, height, mat)` lo pone en un quad que se lee bien desde el lado de `normal`. El alto sale del ancho del local y de la proporción de la textura.
- **Aparcamiento** (`BuildShopsLot`): `ShopsLot` (x 10–26) con entrada `ShopsLotDriveway` desde la calle norte, 6 plazas al fondo con topes, coches o nieve, malla, cartel y farola.
- **Solar**: `VacantLot` del barrio de casas en `ShopsVacant` (x 54–64).
- **Calle** (`BuildShopsStreetLife`): farolas de sodio en la acera norte y en la acera oeste de la avenida (entre pórticos), coches aparcados hacia el oeste, nieve en el bordillo, papeleras, basura y maleza en los huecos.
- Las huellas van a `mapa/edificios_casas.json` como `tienda`, `solar` y `aparcamiento`. `BuildSurroundings` ya no pone two-flats al norte con x > −37. `TrainCrossings` incluye z = 128 y 194 (calles C1' y A').

Semilla: `System.Random(2010)`. Materiales nuevos: `EscaparateLuz`, `PersianaMetalica`, `Letrero_*`, `Neon_Open`, `PosteBarberia`, `Frente*`, `Toldo*`, `CortinaBlanca`, `CruzBlanca`, `Cartel*`, `ArconBlanco`, `ChapaGalvanizada`, `Cromado`, `AireAcondicionado`, `BasuraVaso`.

## Manzana central: `PrototypeSceneBuilder.Towers.cs`
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
- **Torres** (`HighRise`, 27×15 m, 3 m por planta), con el portal en la cara −Z local:
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
  - Cada casa tiene rellano, escalón, puerta de color o tapiada (12 %), tejadillo, camino de `yard` m, bajante, a veces aire acondicionado o parabólica, puerta trasera, patio de hormigón y un `TrashCart`.
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

## Coches: `PrototypeSceneBuilder.Cars.cs`
Coches low-poly genéricos de finales de los 90 y los 2000, **sin marcas ni logotipos** y con matrículas en blanco. Referencias: los coches de `apartaments.jfif` y `trainavenue.avif`.

- **Modelos** (`CarStyle` y `Spec`): `Sedan` (5.1 m, el grande de tracción trasera), `Coupe` (4.95 m, de dos puertas, sin montante central), `Suv` (4.9 m, alto, con barras de techo) y `Van` (5.4 m, furgoneta con puerta corredera).
- **`CarModel(parent, name, pos, yaw, style, paint, rng, wheels, weathered, lightsOn, collider)`**: el morro va hacia +Z local y el suelo queda en y = 0.
  - La carrocería y el habitáculo son mallas extruidas desde el perfil lateral (`ProfileMesh`, sombreado plano). El habitáculo tiene dos submallas: el cristal y el techo en el color de la pintura. Las mallas se guardan en caché por modelo (`carMeshes`).
  - Debajo de la línea de los pasos de rueda van faldones entre las ruedas, y el paso de rueda es una caja oscura para que no se vea a través.
  - Lleva montantes, moldura de cintura, moldura lateral, retrovisores, juntas de puertas y tiradores. También paragolpes y calandra (cromados en el sedán y el coupé), faros, pilotos (verticales en el todoterreno y la furgoneta), matrículas y antena (en el sedán).
  - Con `weathered`, puede llevar nieve en el techo (35 %, a veces también en el capó), óxido junto a los pasos de rueda (15 %), una puerta en imprimación (10 %) o una ventana tapada con plástico (6 %).
  - Tiene dos `BoxCollider`: la parte baja y el habitáculo.
- **`CarWheel`**: neumático, llanta con borde y fondo oscuro, 5 radios y buje. El eje de giro es el X del pivote, como pide `VehicleController`. Las llantas son cromadas (45 %, 15 % en furgonetas), grises o negras.
- **`ParkedCar(parent, ground, yaw, rng)`**: modelo al azar (sedán 40 %, coupé 15 %, todoterreno 30 % y furgoneta 15 %) y pintura al azar. Mantiene el convenio de antes: el largo va a lo largo de X local antes de girar, y con yaw 0 el morro mira a +X. Los aparcados en la calle van en el sentido de la marcha (se circula por la derecha).
- El coche abandonado del barrio (`BuildWreck`) es el sedán en `PinturaOxido`, sin ruedas, sobre bloques y con el capó abierto.

| Material | Notas |
|---|---|
| `Carroceria*` (Negra, Blanca, Granate, Azul, Plata, Verde, Beige, Gris), `CarroceriaJugador` | Brillo 0.45–0.65 y algo metálicos. |
| `CristalCoche`, `CromoCoche`, `PlasticoNegro`, `LlantaGris`, `Neumatico`, `PasoRueda` | |
| `FaroCoche`, `FaroCocheEncendido` (emisivo), `PilotoTrasero`, `Matricula` | |
| `Imprimacion`, `PinturaOxido`, `NieveCoche`, `PlasticoVentana` | Desgaste. |

## Tren elevado: `PrototypeSceneBuilder.Train.cs`
`BuildElevatedTrain` se llama en `Build()` después del barrio de casas. Va sobre la avenida principal (eje x = 110), de z −340 a 340, así que los extremos quedan escondidos en la niebla.

- **Pórticos** cada 15 m, de z −292.5 a 292.5. Cada uno tiene dos pilares de 6.6 m sobre las aceras (x 101.8 y 118.2) con zapata, una cartela y una viga transversal.
  - No se ponen pórticos donde cruza una calle (`TrainCrossings`: z ±80, 0, 122 y 188).
  - Uno de cada tres pilares es de acero oxidado.
- **Tablero continuo:**
  - 4 vigas longitudinales y 2 de borde;
  - traviesas con recorte (`Ties`) para ver a través;
  - raíles con la cara superior a `TrainRailTop` = 8.66 m;
  - pasarelas de rejilla y barandillas `VallaHierro`.
  - Las vías van en x 106.5 (hacia el sur) y 113.5 (hacia el norte).
- **Estación** (`BuildTrainStation`), de z 4 a 68: un andén lateral a cada lado (`PlatformTop` = 9.6 m) con:
  - borde amarillo, barandilla abierta junto a la escalera y pilares propios;
  - marquesina con fluorescentes emisivos, 3 bancos, cartel sin texto y papelera;
  - una escalera hasta la acera, por fuera del andén y bajando hacia el norte.

  Las escaleras se pueden subir a pie (`StationStairs`): 1.4 m de ancho y 32° de pendiente (`StairRunPerRise` = 1.6), por debajo del `slopeLimit` de 45° del CharacterController. Lo que se pisa es una rampa invisible (`ColliderBox`) bajo los peldaños; las barandillas de los lados y del rellano tienen colisión, y una pared invisible en el borde de cada andén impide bajar a las vías. Lo comprueba `StationStairsPlayTests`.
- **Trenes** (`BuildTrains`, `Train`): 2 trenes de 4 vagones de 14.6 m. Cada vagón tiene caja de acero, ventanas emisivas, franja granate, 4 puertas, bajos y bogies; en los extremos del tren van la luna y los faros, y entre vagones, fuelles.
  - Se crean en la raíz de la escena, **fuera de `Mundo`**, porque se mueven y no pueden ser estáticos.
  - Llevan `ElevatedTrain` (`Scripts/World/`), configurado para que paren con el centro en la estación. El del norte empieza en la distancia 120, para desincronizarlo del otro.

| Material | Notas |
|---|---|
| `AceroEstructura`, `AceroOxidado`, `Rail`, `Rejilla` | Estructura. |
| `Traviesas` | `Ties`, con recorte. |
| `AndenHormigon`, `BordeAnden`, `MarquesinaEstacion`, `LuzAnden` (emisivo) | Estación. |
| `AceroTren` (metálico), `BajosTren`, `VentanasTren` y `FaroTren` (emisivos), `PuertasTren`, `FranjaTren` | Trenes. |

## Capturas in-game: `IngameScreenshots.cs`
Saca capturas de 1920×1080 de las distintas zonas, **agrupadas por elemento y por versión**. Usa una cámara temporal con los ajustes de la `Main Camera` (niebla, fondo, 60° de FOV) y `Camera.Render()` sobre un RenderTexture; los trenes salen donde los deja el generador, porque no se entra en Play.

- **Carpetas:** `ingame/<elemento>/vNN_<cambio>/`, con las imágenes y un `cambio.txt` (fecha y descripción). Cada vez que se cambia un elemento se capturan sus vistas en una versión nueva (NN = la última + 1), sin tocar las anteriores, para poder compararlas.
- **Elementos** (los define la primera columna de `Views`): `manzana_central`, `avenida_tren`, `estacion`, `barrio_viviendas`, `barrio_casas`, `negocios`, `barrio_casas_oeste` y `aereas`. Para una zona nueva basta con añadir filas con otro nombre de elemento.
- Las vistas a pie van a ~2 m de alto, como la cámara en tercera persona.
- Menú *GTAChiraq > Capturas in-game (todas)*: una versión nueva de todos los elementos, con la escena guardada.
- Terminal (editor cerrado): `Unity.exe -batchmode -projectPath E:\gtachiraq -executeMethod GTAChiraq.Editor.IngameScreenshots.BuildAndCapture -capturar estacion,negocios -cambio "escaleras transitables" -quit` regenera la escena y captura solo esos elementos. Sin `-capturar`, todos. `CaptureFromCommandLine` hace lo mismo sin regenerar.

## `ProceduralTextures.cs`
Genera las texturas del barrio en código (no se copian assets de ningún juego) y las guarda como PNG en `Assets/_Project/Textures/` con wrap *Repeat*, bilinear y mipmaps. Se regeneran en cada build.

- `BrickFacade(name, brickColor, airConditioner, seed)`: 256 px = un vano de 3 m × una planta de 3 m. Ladrillo a soga (32 hiladas), hilada a sardinel sobre la ventana, alféizar de hormigón, marco blanco, cristal con reflejo, persiana y chorretones.
- `Road(name, roadWidth, seed)`: 512 px; U = ancho completo de la calle.
- `Sidewalk`, `DeadGrass`, `Roof`.
- Tren elevado: `Ties(name, seed)`, con 4 traviesas cada `TiesTile` = 2.4 m y huecos transparentes.
- Barrio de casas: `WoodFence(name, seed)`, `Alley(name, seed)` y `Shingles(name, tint, seed)`. Las constantes `WoodFenceWidth/Height`, `AlleyTile` y `ShingleTile` dan los metros por repetición.
- Vegetación: `Foliage(name, leaf, accent, seed)`, `Snow(name, seed)` y `TallGrass(name, dry, green, seedHeads, seed)` (briznas curvadas con alfa, sin repetir).
- Negocios: `Sign(name, text, background, ink, seed)` (letrero de caja de luz con una fuente de píxeles 5×7 propia: mayúsculas, `&` y espacio; ancho según el texto), `Shutter(name, seed)` (persiana de lamas, 1 m por repetición) y `BarberPole(name)` (espiral roja, blanca y azul).
- Detalle callejero: `IronFence`, `ChainLink`, `Court(name, width, length, margin, seed)`, `Bush`, `Grime` (blanco con alfa: el color lo pone el material) y `Graffiti(name, seed)` (256×128, abstracto: no son letras ni nombres reales).
- Las texturas con alfa se importan con `alphaIsTransparency` y `mipMapsPreserveCoverage`, para que las vallas no desaparezcan de lejos. Las que no se repiten (manchas, grafitis) usan wrap *Clamp*.
- Las constantes `FacadeBay`, `FacadeFloor`, `SidewalkTile`, `GrassTile` y `RoofTile` dicen cuántos metros cubre una repetición; el generador de la escena las usa como `tile`.
- El ruido (`Fbm`, `ValueNoise`) es tileable y usa un hash con semilla, así que el resultado es siempre el mismo.

Para cambiar el aspecto: toca los colores y medidas de estas funciones y regenera la escena.
