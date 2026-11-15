# Editor: `PrototypeSceneBuilder` y `ProceduralTextures`

`PrototypeSceneBuilder` es una clase `partial` repartida en varios archivos:
- `PrototypeSceneBuilder.cs`: escena, calles, alrededores y barrio de viviendas.
- `PrototypeSceneBuilder.StreetProps.cs`: detalle callejero del barrio de viviendas.
- `PrototypeSceneBuilder.Vegetation.cs`: árboles, restos de nieve y maleza del barrio de viviendas.
- `PrototypeSceneBuilder.Houses.cs`: barrio de casas, al norte del barrio de viviendas.
- `PrototypeSceneBuilder.HousesWestExt.cs`: ampliación de la copia oeste del barrio de casas hasta la calle x = −110.
- `PrototypeSceneBuilder.HousesNorth.cs`: ampliación de los barrios de casas hacia el norte (z hasta 298), a los dos lados de la avenida.
- `PrototypeSceneBuilder.FarWest.cs`: oeste lejano, al otro lado de la calle x = −110 (casas, gasolinera, negocios, cancha y descampado).
- `PrototypeSceneBuilder.Highway.cs`: autopista hundida en el borde oeste (x −300…−275), con puentes, túnel y tráfico.
- `PrototypeSceneBuilder.School.cs`: instituto entre la autopista y la calle x = −110.
- `PrototypeSceneBuilder.Training.cs`: zona de prácticas junto al inicio (dianas y objetos para recoger).
- `PrototypeSceneBuilder.Studio.cs`: vida del bloque en la manzana central (estudio en el sótano de T1, memorial y zona de reunión).
- `PrototypeSceneBuilder.MainRoads.cs`: calzadas texturizadas de las cuatro calles grandes (z = ±80, x = ±110), cruces, pasos de cebra y su detalle.
- `PrototypeSceneBuilder.Towers.cs`: manzana central (conjunto de torres, adosadas y aparcamientos).
- `PrototypeSceneBuilder.Train.cs`: tren elevado sobre la avenida principal.
- `PrototypeSceneBuilder.RailYard.cs`: cocheras del tren al sur de la calle z = −80 (ver *Cocheras*).
- `PrototypeSceneBuilder.Apartment.cs`: casa del jugador. Define `T2Center`, `T2Floors` y `T2Openings()` (los huecos que se le pasan a `HighRise` para T2). `BuildApartment` (desde `BuildTowersBlock`, después del estudio) construye:
  - **Vestíbulo** (`BuildLobby`, coordenadas locales de la torre): sala de x −5…5, z −7.2…−0.5, suelo a `LobbyFloor` 0.25 y techo a 2.75 m, por debajo del forjado de 2.89. Pared sur con el hueco del portal (2.6 × 2.6 m), buzones, escalera, dos ascensores y el `Elevator` que sube en `LobbyLiftCall`; telefonillo por fuera.
  - **Piso** (`BuildApartmentRoom`), con origen en `ApartmentCenter` (9, 39.12, −3.35) local de la torre: suelo encima del forjado de 39 m, sala de ±`AptHalfX` × ±`AptHalfZ` (4.2 × 3.85) y 2.7 m de alto. Las fachadas sur (z −4.15) y este (x 4.5) se hacen con `FacadeWall`: en cada celda abierta de la torre, ladrillo liso por fuera, pared pintada por dentro y una ventana de 2 m (alféizar a `AptSill` 0.75, dintel a `AptHead` 2.45) con marco y cristal `GlassMat` (URP transparente) con collider, `NoClimb` y `NoCover`. Ascensor en la pared oeste (el `Elevator` que baja), salón, sillón mirando a la ventana sur, cocina, cama, `Wardrobe`, `SafeBox` y tres luces sin sombras.
- `PrototypeSceneBuilder.Body.cs`: cuerpo del personaje (`BuildPlayerBody`) y `LoftMesh`/`Loft`, la malla low-poly por anillos (superelipses de N lados, sombreado suave, tapas opcionales; U alrededor con el frente en 0.5, V de abajo arriba) con la que se hace. `FrontZ` da el frente del torso a una altura, para apoyar bolsillos, botonera, cordones y cadena.
- `PrototypeSceneBuilder.Cars.cs`: modelos de coche (aparcados, el que se conduce y el abandonado).

Menú **GTAChiraq > Construir escena prototipo**. Genera `Assets/_Project/Scenes/Prototype.unity` desde cero. Se puede ejecutar todas las veces que quieras: sobrescribe la escena y reutiliza los materiales.

> La escena es un **producto generado**. Para cambiar el mundo de prueba, edita este script y regenera la escena; no la toques a mano.

## Qué hace `Build()`, en orden
0. **`PrepareCars()`**: materiales de los coches (los usan todas las zonas).
1. **`EnsureUrp()`**: si `GraphicsSettings.defaultRenderPipeline` no es URP, crea `Settings/URP_Renderer.asset` y `Settings/URP_Pipeline.asset` y los asigna.
2. **`SetupLighting()`**: la luz de mediodía de `DayCycleMath.Look(12, 0, 0)`: luz direccional fría y suave (cielo nublado), ambiente plano gris, niebla lineal de 25 a 260 m y el cielo gris degradado (`CieloGris.mat`, shader `GTAChiraq/CieloGris` de `Assets/_Project/Shaders/`), sin reflejos del cielo (`DefaultReflectionMode.Custom`). En juego la cambia `WeatherSystem` con la hora y el tiempo. `EnsureUrp()` pone el renderer en **Forward+** (sin límite de luces por objeto: con Forward, las aceras y calzadas largas solo recibían 4 farolas).
3. **Mundo** (todo bajo el objeto `Mundo` y marcado como estático):
   - `BuildStreets`: suelo de asfalto liso (de x −275 a 300: encima de la autopista, el suelo lo ponen los puentes y el techo del túnel), aceras de enfrente y, con `BuildMainRoads`, las calzadas texturizadas de las calles grandes (ver *Calles grandes*). La acera norte se corta en la entrada del aparcamiento de los negocios y en el pasaje al callejón (`ShopsLotDriveway`, `ShopsAlleyLink`). El césped y la acera de la manzana central los pone `BuildTowersBlock`.
   - `BuildSurroundings`: filas de casas de 2 plantas al otro lado de la calle sur (al norte ya no: está el oeste lejano; enfrente de la manzana central, tampoco: están las cocheras), una tienda de esquina con letrero y coches aparcados (se pueden robar).
   - `BuildProjectsDistrict`: barrio de viviendas **texturizado** al este de la avenida (ver abajo). Al final llama a `BuildDistrictProps` (detalle callejero).
   - `BuildHousesDistrict`: los dos barrios de casas (el del este y su copia oeste, girada 180°), la ampliación oeste, la ampliación norte y la franja de negocios (ver abajo).
   - `BuildTowersBlock`: manzana central (ver abajo). Va después de los dos barrios porque reutiliza sus materiales.
   - `BuildHighway`: autopista hundida (ver *Autopista hundida*).
   - `BuildSchool`: instituto (ver *Instituto*). Va después de `BuildHighway`.
   - `BuildElevatedTrain`: tren elevado y estación (ver abajo).
   - `BuildStreetLights`: farolas cada 30 m con luz puntual de sodio naranja, sin sombras.
   - `BuildSnow`: montones de nieve sucia junto al bordillo (salvo en las entradas de los aparcamientos).
   - `BuildMainRoadDetails`: alcantarillas, sumideros, nieve apartada y manchas en las calles grandes (va al final porque usa `Charco`, `Aceite` y `Suciedad`).
4. **`BuildPlayer`**: crea el objeto `Jugador` (tag `Player`, capa 2) con:
   - CharacterController;
   - los sistemas del personaje: `Health`, `PlayerVitals`, `PlayerMovement`, `PlayerClimb`, `PlayerCover`, `PlayerInventory`, `PlayerCombat`, `PlayerHud` y `VehicleEntry`;
   - `PlayerInteractor` (ascensor, armario y caja fuerte) y `PlayerOutfit` (ropa);
   - `PlayerAnimator` (animación procedural);
   - el hijo `Modelo`: el cuerpo de `BuildPlayerBody`, por articulaciones (pelvis, caderas, rodillas, tobillos, columna, cráneo, hombros y codos) y piezas (ver `Scripts/Player/README`), hecho con mallas por anillos (`PrototypeSceneBuilder.Body.cs`), con la cara y el pelo rapado pintados en `CaraPersonaje` (`ProceduralTextures.Face`) y los materiales `Ropa_<prenda>` de la ropa de inicio con su textura de tejido. Crea las texturas `Tejido_<estilo>` (`ProceduralTextures.Cloth`, 128 px pintadas con pliegues, costuras y desgaste; helpers `Fold` y `Mask`), `PielPersonaje` (`Skin`) y `PeloRapado` (`BuzzCut`) y se las pasa a `PlayerOutfit.SetTextures`; luego `PlayerOutfit.PreviewInEditor` lo viste con la ropa de inicio (piezas encendidas, holguras), la `Pistola` de prueba (oculta; su hijo `BocaCanon` es el `muzzle` de `PlayerCombat`) y el `Puno`, que sale al pegar.

   `BuildCamera` le pasa la cámara a `PlayerCombat`.
5. **`BuildCar`**: crea `Coche_Sedan` con el modelo de sedán de `Cars.cs` (pintura `CarroceriaJugador`, llantas cromadas, faros encendidos y dos focos) y lo pasa por `MakeDrivable` (`Cars.cs`): Rigidbody (masa según el modelo), `VehicleController`, `SolidVisual`, un WheelCollider por cada pivote `Rueda` del modelo y un `PuntoSalida` a la izquierda, todo en la capa 6 (`Vehiculo`). El modelo se baja para que su suelo coincida con el apoyo de las ruedas en reposo (`CarGroundY`: `0.4 − 0.25·(1 − 0.5) − radio`). Es del jugador: no lleva `VehicleLock`.

   Antes, justo después de construir las zonas y antes de `MarkStatic`, **`BuildDrivableParkedCars`** saca de `Mundo` todos los coches que puso `ParkedCar` (los apunta en `parkedCars`), los mete en `CochesAparcados` y los pasa por `MakeDrivable` con un `VehicleLock` cerrado: se pueden robar forzando la puerta. Ya no son estáticos (no entran en el static batching).
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

## Gente: `PrototypeSceneBuilder.Npc.cs`
Se llama al final de `Build()` (`BuildNpcs(world, player)`), cuando ya está todo el mundo, porque la malla de navegación tiene que verlo:
- **Sitios** (`NpcSpot`, bajo `Mundo/SitiosNPC`): las esquinas habituales de cada facción (`HabitualSpots`, con coordenadas del mundo sacadas de `NPCS.md` y del plano), los sitios de paso de los civiles en las aceras (`CivilianSpots`) y el memorial. `ScanProps` recorre el mundo y pone uno en cada `Banco` (sentarse; en la `ParadaBus`, esperar a cubierto) y en el `Bidon` con `Brasas` del descampado (calentarse, Crestline).
- **Locales:** `Storefront` llama a `ShopLife`: `ShopHours` (el OPEN pasa a llamarse `NeonOpen`; se añade una `PersianaCerrada` entera, oculta), un sitio de recado en la puerta (solo abierto) y, en la FOOD & LIQUOR, una esquina de venta del grupo del barrio (Ash Block en la franja de negocios).
- **Malla de navegación:** `NavMeshSurface` sobre los colliders de todo (sin las capas 2 y 7), vóxel de 0.2 m; se guarda en `Scenes/Prototype_NavMesh.asset` (no se versiona). Tarda unos segundos.
- **Plantilla y población:** `PlantillaNPC` (inactiva) copia el `Modelo` del jugador con la pistola, y lleva cápsula de 1.5 m, esfera `Cabeza`, `NavMeshAgent`, `Health`, `PlayerOutfit` (copiado del jugador con `persist` apagado), `NpcAnimator` y `NpcBrain`; todo en la capa 7 `Personaje` (`ProjectSettings/TagManager.asset`). Cuelga de `Poblacion` (`NpcPopulation`).
- El asmdef del editor referencia `Unity.AI.Navigation`.

## Paleta y materiales
Los materiales se crean con `Mat(name, color)` (URP/Lit) en `Assets/_Project/Materials/<name>.mat`. Si ya existen, se reutilizan y se les actualiza el color.

| Elementos | Materiales |
|---|---|
| Edificios | `Ladrillo`, `LadrilloOscuro`, `FachadaGris`, `FranjaVentanas`, `Cornisa` |
| Suelo | `Asfalto`, `Hormigon`, `NieveSucia`; calles grandes: `CalleGrande1`–`3`, `CalleGrandeLlegada`, `CalleGrandeCruce` (texturizados), `Hierro`, `RejillaSumidero` |
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

## Franja de negocios: `PrototypeSceneBuilder.Shops.cs`
`BuildShopsStrip(world)`, llamado desde `BuildHousesDistrict` después de las dos copias (usa sus materiales). Zona: x ∈ [−30, 100], z ∈ [90, 102], entre la acera norte y el callejón de atrás de la copia oeste.

- **Suelo** (`BuildShopsGround`): acera ancha hasta la fachada (`ShopsFront` = 93), césped seco detrás, patio de servicio de `Callejon` tras cada local, el pasaje `ShopsAlleyLink` (x −26…−21) que une el callejón a5 de la copia oeste con la calle, y la **acera oeste de la avenida** (`AvenueWestSidewalk`, x 100–103, z 90–300), cortada con `StripMinus` donde entran A' y C1' (`HousesWestRect` pasa un rectángulo local de la copia al mundo).
- **Locales** (`Shops`, de oeste a este: tipo, x0, x1, plantas). `Storefront(parent, kind, x0, x1, floors, rng)`: caja de ladrillo de 7.5 m de fondo (4.6 o 7.4 m de alto) con pretil; en la planta baja un panel pintado tapa las ventanas de la textura y lleva escaparate (`EscaparateLuz`, emisivo), zócalo, puerta, caja de persiana, rejas o persiana bajada (`PersianaMetalica`), letrero luminoso y cartel OPEN de neón. Detalles por tipo: cruz y cartel de horario (iglesia), toldo (lavandería, barbería, pollo), poste de barbería con textura en espiral, extractor, grasa y mural (pollo), cartel de móviles (cheques), letrero lateral "GROCERY", carteles de ofertas y arcón de hielo (tienda). Detrás: puerta de chapa en un vano (`FacadeDoor`), contenedor o cubo y bolsas.
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

## Cocheras: `PrototypeSceneBuilder.RailYard.cs`
`BuildRailYard` se llama justo después de `BuildElevatedTrain`, porque usa sus materiales (`AceroTren`, `BajosTren`, `Traviesas`, `Rail`…), y antes de `BuildDrivableParkedCars` (el coche de la oficina se puede robar). Semilla 1902.

- **Medidas**: recinto `YardMinX`…`YardMaxX` × `YardSouthZ`…`YardNorthZ` (x −74…100, z −222…−91.5), grava (`Ballast`) de 4 cm; raíles con la cara de arriba a `YardRailTop` = 0.36 m. Entrada `YardGateX0`…`YardGateX1`.
- **Vías** (`YardTrack`): lecho de balasto, traviesas con recorte, raíles y, en las de estacionamiento, tercer carril con cubierta. `StorageTracksX` (10 vías) van de `StorageNorthZ` a la escalera de agujas; `ShedTracksX` (3) salen de la nave. La escalera es la recta `LadderWest`–`LadderEast` y `LadderZ(x)` da dónde acaba cada vía.
- **Rampa** (`BuildYardLead`): la polilínea `YardLead`, del elevado (x 105.5, z −92, a `TrainRailTop`) al pie de la escalera, en tramos de 3 m con pendiente constante. Por encima de 2.6 m, vigas y pórticos de acero cada 4 tramos; por debajo, terraplén con `MuroContencion` (con collider).
- **Nave** (`BuildYardShed`): muros de `PlainBrick` hasta 3 m y chapa `Corrugated` encima, tejado a dos aguas (dos cajas giradas) y hastiales con `GableMesh` (prisma triangular). Portones en el sur alineados con `ShedTracksX`, puerta de personal en el este, luces puntuales dentro.
- **Trenes** (`BuildYardTrains`): `Train(..., cars)` de Train.cs con ventanas y faros apagados; cada vagón lleva un `BoxCollider` y el tren un `SolidVisual`.
- **Contenedores** (`BuildYardContainers`, `Container`): `ContainerColumnsX` × `ContainerRows` huecos con pilas de 0–3 al azar; el de la fila 2, columna 6 es hueco y abierto (escondite). Palés al pie de algunas pilas para escalarlas (un contenedor mide 2.59 m, más que lo que se escala de un salto).
- **Valla** (`BuildYardFence`): `FenceSegment` con `MallaMetalica` de 2.2 m y una concertina (cilindro de malla) encima; agujeros en el oeste (z −112…−114) y en el sur (x 28…30.2).
- **Entrada** (`BuildYardEntrance`) y **detalles** (`BuildYardProps`): pórtico con `SignMat` "RAIL YARD", carteles "NO TRESPASSING", barrera, caseta con luz, oficina prefabricada, `FloodTower` (4, con foco real sin sombras), señales, pilas de traviesas (`SolidVisual` y un `ColliderBox`), carriles, ruedas, bidones, balasto, nieve y maleza.
- Aceras nuevas a lo largo de x = ±101.5 hacia el sur (la oeste empieza en z −110, pasados los two-flats de la esquina).

## Oeste lejano: `PrototypeSceneBuilder.FarWest.cs`
`BuildFarWest` (objeto `Barrio_Oeste_Lejano`), llamado al final de `BuildHousesDistrict`, **después de `BuildShopsStrip`** (usa `Storefront` y sus materiales). x de `FarWestMinX` (−300) a `FarWestMaxX` (−120), z 90–298, en coordenadas del mundo con `BuildHousesPatch` (semilla 2011):
- `FarWestRoads`: J (`FarWestJ`, x −262…−252, z 87–298) y D, E, F, H de x −252 a −117 (siguen las de la ampliación oeste y la norte). `FarWestAlleys` siguen n2 y n3. `FarWestSidewalks`, `FarWestLawns` y `FarWestRows` (al oeste de J, filas con yaw 270: la parcela hacia −X).
- `FarWestStreetLife`: negocios (`FarWestShopsStrip`: `FarWestShops` con `Storefront` en la misma línea de fachada `ShopsFront`, solar `FarWestShopsVacant`), gasolinera (`GasStation`), cancha (`Court` en `FarWestCourtCenter`, puerta hacia D), descampado (`Wasteland` en `FarWestWasteland`), farolas, postes con cables, coches y valla del borde del mundo.
- `GasStation`: `GasLot` (x −160…−120, z 90–124) a `LotTop`, entradas `GasDriveways`, marquesina con colisión, islas, surtidores, tienda (`ShopKind.GasMart` en un marco desplazado para que la fachada quede en z 113 mirando a −Z), poste de precios girado −45° hacia el cruce.
- `BuildStreets` corta las aceras de enfrente con `FarWestNorthSidewalkCuts` (J y las entradas de la gasolinera) y `FarWestWestSidewalkCuts` (D, E, F, H y la entrada desde x = −110); `MainRoadMouths` incluye `FarWestRoads` y `GasDriveways`.
- `ShopKind` tiene los tipos nuevos `Wings`, `Pawn`, `Beauty`, `Tires` y `GasMart` (letreros, toldos, rejas y detalles propios en `Storefront`).
- `Court(parent, center, gateSide)` y `Wreck(parent, ground, yaw, rng)` (StreetProps.cs) son la cancha y el coche abandonado del barrio de viviendas, ahora con posición como parámetro.
- Huellas para el plano: `cancha`, `gasolinera`, `tienda` y `solar` (el descampado). Capturas: elemento `oeste_lejano`.

## Autopista hundida: `PrototypeSceneBuilder.Highway.cs`
`BuildHighway` (objeto `Autopista`, semilla 2016), llamado después de `BuildTowersBlock`. Trinchera de `HighwayDepth` = 7 m entre `HighwayMinX` (−300, el borde del mundo) y `HighwayMaxX` (−275), de z −300 (`HighwaySouthZ`, se pierde en la niebla) a `HighwayTunnelEndZ` = 130.
- **Calzadas** (`TexturedBox` con colisión, arriba en y = −7): dos de `HighwayCarriageway` = 11.1 m con la textura `Autopista` (`ProceduralTextures.Highway`, tile 11.1 × 12 m). La del oeste va hacia el sur y está girada 180°, para que la línea amarilla quede junto a la mediana en las dos. Mediana de hormigón tipo New Jersey de 0.8 m en `HighwayMedianX` = −287.5.
- **Muros de contención** de 1 m (`MuroContencion`, `ProceduralTextures.RetainingWall`, tile 8 × 7 m), de la calzada al nivel de la calle y en todo el largo.
- **Puentes** (`BuildHighwayBridges`) de las calles z = ±80: `HighwayBridgeSouth` (z −90…−70), con tablero de 1.2 m, y `HighwayBridgeNorth` (z 70…90), cuyo tablero es el principio del techo del túnel. Vigas cada 2.5 m, pila en la mediana (cabecera y 3 pilares) y pretil con malla en los bordes que dan a la trinchera. Encima van, como en el resto, la calzada de la calle grande, las aceras y su detalle.
- **Túnel** (`BuildHighwayTunnel`): techo de z 70 a 300 (es el suelo del oeste lejano), dintel en la boca (`HighwayTunnelZ` = 90), fluorescentes, una luz y un fondo negro en z 130, donde desaparecen los coches.
- **Bordes** (`BuildHighwayEdges`): pretil de 0.9 m y malla de 1.8 m encima de los dos muros en los tramos al aire libre (`HighwayOpenStretches`), así que no se puede saltar dentro.
- **Detalle** (`BuildHighwayFurniture`): farolas de doble brazo en la mediana cada 40 m; pórtico en z −30 con carteles verdes genéricos (EXPRESSWAY SOUTH / NORTH, EXIT ONLY); grafitis en los muros; basura, nieve y bolsas en los arcenes; manchas, y un coche averiado con triángulo.
- **Tráfico** (`BuildHighwayTraffic`, semilla 2017): 3–4 coches por carril con `HighwayCar` (`Scripts/World/`), del sur a dentro del túnel o al revés, a 19–25 m/s (el carril junto a la mediana es el rápido), con los faros encendidos y sin colisión. Van en `Trafico_Autopista`, fuera de `Mundo`.
- No hay rampas de entrada ni de salida: todavía no se puede circular por la autopista.

## Instituto: `PrototypeSceneBuilder.School.cs`
`BuildSchool` (objeto `Instituto`, semilla 2018), después de `BuildHighway`. Parcela `SchoolArea`: x −275…−120, z −70…70. `schoolOccupied` guarda lo ocupado, para que la nieve y la basura no caigan encima.
- **Suelo** (`BuildSchoolGround`):
  - aceras del lado del instituto en las calles z = ±80 (z ±70…73, de x −300 a −120, también sobre los puentes);
  - asfalto (`Callejon`) del aparcamiento, la bahía de autobuses, la zona de servicio y las dos salidas (`SchoolDriveways`: cortan la acera oeste de la calle x = −110 en `BuildStreets` y cuentan como bocas en `MainRoadMouths`);
  - césped en el resto (`RectMinus`), caminos de losas y valla de hierro por las tres calles.
- **Edificios** (`SchoolBlock`: muros con la fachada `FachadaInstituto` de ladrillo claro, antepecho de ladrillo liso, albardilla, cornisa y zócalo):
  - `SchoolMain` (x −252…−176, z 38…56): 3 plantas (9 m) y antepecho de 1.5 m. El portal de piedra tapa entera la planta baja de tres vanos y mide 3.6 m de alto, por debajo de las ventanas del primer piso. Lleva tres puertas de cristal, apliques, letrero de visitas, escalinata con barandillas y un remate de ladrillo sobre la entrada con la placa "HIGH SCHOOL" y "EST 1961". Además: rejas (`MallaMetalica`) en las ventanas de la planta baja, puerta trasera (`FacadeDoor`) con escalón, persiana y muelle de carga en la zona de servicio, algunas ventanas con contrachapado, climatizadores y grafitis.
  - `SchoolWing` (x −232…−214, z 20…38): 2 plantas, puerta al patio con tejadillo, y rejas.
  - `SchoolGym` (x −176…−152, z 22…56): 10 m de ladrillo liso, banda de bloque de vidrio con pilastras en las caras este y sur, letrero "GYMNASIUM" hacia la calle, puertas dobles rojas al este y al sur, y grafitis.
- **Campo** (`BuildSchoolField`), con centro en `SchoolFieldCenter` (−197, −35.5): una sola caja con la textura `CampoFutbol` (`ProceduralTextures.FootballField`, 2048 × 1024, que cubre `FieldTextureSize` = 133 × 65 m). El campo va a escala de juego (`FieldYard` = 0.76 m por yarda, 91 × 40.5 m) para que quepa con la pista: 6 calles de 1.1 m, rectas de 68 m y radio interior de 24.5 m. Lleva porterías amarillas de cuello de cisne, banquillos, 4 torres de focos (emisivas, sin luz real), un marcador HOME/GUEST junto a la autopista y malla de 2.4 m (`SchoolFieldFence`) con dos entradas desde el patio.
- **Gradas** (`SchoolBleachersBuild`, `SchoolBleachers`): 7 filas de aluminio de 50 m mirando al sur, con estructura, barandillas y palco de prensa. Se suben andando por una rampa invisible (`RampaGradas`, de unos 31°), y una barandilla con colisión cierra la parte de atrás.
- **Aparcamiento** (`BuildSchoolParking`): dos filas de 17 plazas mirando al pasillo central, topes, coches en la mitad, nieve, flechas de sentido único, farolas, cartel "STAFF PARKING ONLY" y dos autobuses escolares (`SchoolBus`, genéricos, con "SCHOOL BUS" y brazo de stop) en la bahía.
- **Patio y jardín** (`BuildSchoolYard`): mástil con bandera de franjas, cartel de bienvenida, aparcabicis, cámara de vigilancia, bancos y mesa de picnic en el patio trasero, contenedores y palés en la zona de servicio, 17 árboles (`DistrictTree`), farolas en las aceras del instituto, nieve, basura y atajos de barro.
- Capturas: elementos `autopista` e `instituto`. El plano copia las medidas en las constantes `HW_*` y `SC_*` de `generar_mapa.py`.

## Zona de prácticas: `PrototypeSceneBuilder.Training.cs`
`BuildTrainingArea` se llama después de la cámara y crea el objeto `Practica` en la raíz de la escena, fuera de "Mundo" porque se mueve. Todo va en la acera sur de la manzana central, en z −71.6:
- Tres **dianas** (`TrainingDummy` + `Health`) en x 5, 7.5 y 10, mirando al oeste, hacia el inicio. Tienen poste, torso, cabeza y círculo rojo; la esfera `Cabeza` cuenta como tiro en la cabeza.
- Una fila de **objetos** (`ItemPickup`) en `TrainingPickups`, de x −8.5 a −1: munición, botiquín, alitas, bebida energética, chaleco y 50 $.

## Vida del bloque: `PrototypeSceneBuilder.Studio.cs`
`BuildTowersYard` llama a estas funciones justo después de la losa de la canasta, antes que las farolas y bancos del eje, para que esos respeten su sitio. Usan una semilla propia (1017), así que no mueven el resto del bloque.
- **Estudio** (`BuildStudio`, objeto `Estudio_Sotano_T1`):
  - Hueco de la escalera: `StudioStairwell` (x −80.3…−77.7, z 40…48.5), al oeste de T1 y al norte de sus contenedores. Se recorta del `Suelo` en `BuildStreets` (con `RectMinus`) y del césped en `BuildTowersGround`.
  - La escalera baja hacia el sur hasta `StudioFloor` (−3.2), con peldaños macizos y una rampa invisible (`RampaEscalera`, la misma pendiente que la estación: `StairRunPerRise`). Las paredes del hueco son verticales y arriba hay una barandilla con colisión (`LimiteHueco`) por tres lados.
  - Sala: `StudioRoom` (x −77.5…−63, z 38…49.5). El techo es el `Suelo` (−0.2) con un panel acústico debajo. La puerta está en la pared oeste, que comparte con el hueco.
  - Cabina de voz en la esquina noreste, con el micro. Junto al micro va `Micro_Grabacion`, el `RecordingStudio`: su `pulseLight` es el `FocoCabina`, que late con el beat.
- **Memorial** (`BuildMemorial`): centro (−17.5, −4.6), con un tramo de malla (`FenceSegment`) en z −3.4. Las cosas (velas, peluches, flores, globos) son primitivas sin colisión. La foto es una silueta; los carteles usan `SignMat` ("RIP", "LONG LIVE").
- **Zona de reunión** (`BuildHangout`): 6.3 m al sur de la losa de la canasta. Sillas con colisión en el asiento, barbacoa de bidón (con colisión, brasas emisivas, luz y humo de esferas), altavoz con aros LED, nevera y vasos.
- Capturas: elementos `estudio`, `memorial` y `reunion`. El plano lo dibuja en la sección *Vida del bloque* de `generar_mapa.py`.

## Calles grandes: `PrototypeSceneBuilder.MainRoads.cs`
Las cuatro calles de 14 m alrededor de la manzana central (norte y sur en z = ±80, avenida y calle del oeste en x = ±110), de borde a borde del mundo (`MainRoadHalfLength` = 300). `MainRoadsList()` las da con su eje y dónde las cruzan las otras; `MainRoadPoint(alongX, center, t, across, y)` pasa de coordenadas de calle a mundo.
- **`BuildMainRoads`** (objeto `CallesGrandes`): cajas finas sin colisión (arriba en `MainRoadTop` = 0.01, por debajo de las calles de barrio, a 0.02, que tapan la boca) encima del `Suelo` liso. Por cada calle: tramos normales partidos en trozos de 42 m con una de las tres variantes `CalleGrande1`–`3` al azar (`MainRoadStretch`, para que los parches no se repitan cada 14 m), una **llegada** de 14 m a cada lado de cada cruce (`CalleGrandeLlegada`, girada para que la V de la textura apunte al cruce) y el cuadrado del **cruce** (`CalleGrandeCruce`, sin pintura).
- **`BuildMainRoadDetails`** (objeto `CallesGrandes_Detalle`, semilla 808): tapas de alcantarilla en el centro de un carril cada 40–60 m (fuera de los cruces), sumideros junto a los dos bordillos cada 35–50 m, nieve sucia apartada contra el bordillo, charcos junto al bordillo y manchas de aceite (más donde paran los coches antes del cruce).
- Circulación por la derecha: en la llegada, la línea de stop va solo en los carriles que entran al cruce.
- Las calles norte y sur **acaban en la avenida** (`end` en `MainRoadsList`): al este está el barrio de viviendas, así que (110, ±80) son cruces en T sin llegada al este. `MainRoadMouths()` da las bocas de calles y entradas (ensanchadas 1.5 m) donde no van sumideros ni nieve.
- En `BuildStreets`, las aceras de enfrente norte y sur se cortan donde pasan la avenida y la calle x = −110, y la del oeste donde pasan z = ±80 (antes eran tiras de 600 m que dejaban un bordillo atravesando la calzada).
- Capturas: elemento `calles_grandes` en `IngameScreenshots`.

## Hora y tiempo: `PrototypeSceneBuilder.Weather.cs`
`BuildWeather` (después de `BuildMainRoadDetails`) crea el objeto `Tiempo`, fuera de `Mundo`, con `WeatherSystem`. Le pasa el sol (`RenderSettings.sun`), el cielo y dos materiales de partículas transparentes (URP Particles/Unlit, que se desvanecen a menos de 1.2–4 m de la cámara): `Lluvia`, sin textura, y `Copo`, con `ProceduralTextures.Snowflake()`. `SkyMaterial()` crea `CieloGris.mat`.

Las luces de calle llevan `NightLight` desde donde se crean: `StreetLight` (farolas), `ParkLamp` (StreetProps), las farolas de la autopista (Highway) y las torres de focos de las cocheras (RailYard). Las demás (interiores, fuegos, túnel, faros) no lo llevan y se quedan siempre encendidas. **Una luz de calle nueva tiene que llevar `NightLight`.**

## Capturas in-game: `IngameScreenshots.cs`
Saca capturas de 1920×1080 de las distintas zonas, **agrupadas por elemento y por versión**. Todas salen a mediodía y sin lluvia (`WeatherSystem.Preview`), salvo el elemento `tiempo`, que captura cada vista con cada estado de `Weathers` (mediodía, atardecer, noche, lluvia y nieve de día y de noche). El primer fotograma se renderiza y se descarta, porque sale quemado. Usa una cámara temporal con los ajustes de la `Main Camera` (niebla, fondo, 60° de FOV) y `Camera.Render()` sobre un RenderTexture; los trenes salen donde los deja el generador, porque no se entra en Play.

- **Carpetas:** `ingame/<elemento>/vNN_<cambio>/`, con las imágenes y un `cambio.txt` (fecha y descripción). Cada vez que se cambia un elemento se capturan sus vistas en una versión nueva (NN = la última + 1), sin tocar las anteriores, para poder compararlas.
- **Elementos** (los define la primera columna de `Views`): `manzana_central`, `avenida_tren`, `estacion`, `barrio_viviendas`, `barrio_casas`, `negocios`, `barrio_casas_oeste`, `oeste_lejano`, `calles_grandes`, `autopista`, `instituto` y `aereas`. Para una zona nueva basta con añadir filas con otro nombre de elemento.
- Las vistas a pie van a ~2 m de alto, como la cámara en tercera persona.
- Menú *GTAChiraq > Capturas in-game (todas)*: una versión nueva de todos los elementos, con la escena guardada.
- Terminal (editor cerrado): `Unity.exe -batchmode -projectPath E:\gtachiraq -executeMethod GTAChiraq.Editor.IngameScreenshots.BuildAndCapture -capturar estacion,negocios -cambio "escaleras transitables" -quit` regenera la escena y captura solo esos elementos. Sin `-capturar`, todos. `CaptureFromCommandLine` hace lo mismo sin regenerar.

## `ProceduralTextures.cs`
Genera las texturas del barrio en código (no se copian assets de ningún juego) y las guarda como PNG en `Assets/_Project/Textures/` con wrap *Repeat*, bilinear y mipmaps. Se regeneran en cada build.

- `Ballast(name, seed)`: grava de vía (piedras de 4 px con junta, óxido y aceite), 128 px = `BallastTile` = 2 m. `Corrugated(name, seed)`: chapa ondulada casi blanca (se tiñe con el material) con chorretones de óxido, 64×128 px = `CorrugatedTileU` × `CorrugatedTileV` (1.2 × 2.6 m). Las dos, para las cocheras.
- `BrickFacade(name, brickColor, airConditioner, seed)`: 256 px = un vano de 3 m × una planta de 3 m. Ladrillo a soga (32 hiladas), hilada a sardinel sobre la ventana, alféizar de hormigón, marco blanco, cristal con reflejo, persiana y chorretones.
- `Road(name, roadWidth, seed)`: 512 px; U = ancho completo de la calle.
- `MainRoad(name, roadWidth, seed, MainRoadPaint)`: 512 px = ancho × ancho (14 × 14 m) de las calles grandes. U cruza la calle y V va a lo largo. Asfalto con rodadas pulidas, sal de invierno, grietas selladas con alquitrán, un parche de obras, a veces una zanja y baches; la pintura se borra con el ruido y donde pisan las ruedas, y no va sobre los parches. `Lanes`: bordes blancos, doble amarilla y separadores de carril discontinuos (trazos de 3 m cada 7). `Approach` (V = 1 hacia el cruce): separadores continuos, línea de stop en los carriles de la derecha (u > 0.5) y paso de cebra de barras anchas. `None`: el cruce, solo asfalto.
- `Sidewalk`, `DeadGrass`, `Roof`.
- Tren elevado: `Ties(name, seed)`, con 4 traviesas cada `TiesTile` = 2.4 m y huecos transparentes.
- Autopista: `Highway(name, width, seed)` (una calzada; `HighwayTile` = 12 m a lo largo) y `RetainingWall(name, seed)` (muro de contención, `WallTileWidth` × `WallTileHeight`).
- Instituto: `FootballField(name, seed)` (campo y pista en una sola textura; las medidas están en `FieldYard`, `FieldHalfLength`, `TrackHalfStraight`, `TrackInnerRadius`…).
- Barrio de casas: `WoodFence(name, seed)`, `Alley(name, seed)` y `Shingles(name, tint, seed)`. Las constantes `WoodFenceWidth/Height`, `AlleyTile` y `ShingleTile` dan los metros por repetición.
- Vegetación: `Foliage(name, leaf, accent, seed)`, `Snow(name, seed)` y `TallGrass(name, dry, green, seedHeads, seed)` (briznas curvadas con alfa, sin repetir).
- Porches: `PorchRail(name, wood, seed)` (barandilla de balaustres con alfa, 1 × 1 m).
- Negocios: `Sign(name, text, background, ink, seed)` (letrero de caja de luz con una fuente de píxeles 5×7 propia: mayúsculas, cifras, `&` y espacio; ancho según el texto), `Shutter(name, seed)` (persiana de lamas, 1 m por repetición) y `BarberPole(name)` (espiral roja, blanca y azul).
- Detalle callejero: `IronFence`, `ChainLink`, `Court(name, width, length, margin, seed)`, `Bush`, `Grime` (blanco con alfa: el color lo pone el material) y `Graffiti(name, seed)` (256×128, abstracto: no son letras ni nombres reales).
- Las texturas con alfa se importan con `alphaIsTransparency` y `mipMapsPreserveCoverage`, para que las vallas no desaparezcan de lejos. Las que no se repiten (manchas, grafitis) usan wrap *Clamp*.
- Las constantes `FacadeBay`, `FacadeFloor`, `SidewalkTile`, `GrassTile` y `RoofTile` dicen cuántos metros cubre una repetición; el generador de la escena las usa como `tile`.
- El ruido (`Fbm`, `ValueNoise`) es tileable y usa un hash con semilla, así que el resultado es siempre el mismo.

Para cambiar el aspecto: toca los colores y medidas de estas funciones y regenera la escena.
