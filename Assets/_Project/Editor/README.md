# Editor: `PrototypeSceneBuilder` y `ProceduralTextures`

Este README es el **índice**: arriba va lo general (archivos, orden de `Build()` y disposición) y el detalle de cada parte está en [`Docs/`](Docs/), un archivo por tema. Lo nuevo se escribe en el archivo del tema; la ficha de diseño de cada zona está en [`docs/estructuras/zonas/`](../../../docs/estructuras/zonas/).

`PrototypeSceneBuilder` es una clase `partial` repartida en varios archivos:
- `PrototypeSceneBuilder.cs`: escena, calles, alrededores y barrio de viviendas.
- `PrototypeSceneBuilder.StreetProps.cs`: detalle callejero del barrio de viviendas.
- `PrototypeSceneBuilder.Vegetation.cs`: árboles, restos de nieve y maleza del barrio de viviendas.
- `PrototypeSceneBuilder.Houses.cs`: barrio de casas, al norte del barrio de viviendas.
- `PrototypeSceneBuilder.HousesWestExt.cs`: ampliación de la copia oeste del barrio de casas hasta la calle x = −110.
- `PrototypeSceneBuilder.HousesNorth.cs`: ampliación de los barrios de casas hacia el norte (z hasta 298), a los dos lados de la avenida.
- `PrototypeSceneBuilder.North.cs` y `.NorthBuildings.cs`: barrio norte (z 298…500): bulevar, diagonal, mini centro comercial, parque, edificio en cuña, iglesia, torre suelta y terraplén del tren de mercancías con el horizonte detrás.
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
- `PrototypeSceneBuilder.StreetDetail.cs`: detalle vial de toda la ciudad (nombres de calle, semáforos, STOP, pasos de cebra, flechas, postes con cables, límites de velocidad y alcantarillado).

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
   - `BuildStreetDetail`: detalle vial (ver *Docs/detalle-vial.md*). Va después de todo porque comprueba con la física que cada poste cabe; los semáforos quedan fuera de `Mundo`.
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

## Documentación por tema (`Docs/`)

| Archivo | Tema |
|---|---|
| [npc.md](Docs/npc.md) | Gente: `PrototypeSceneBuilder.Npc.cs` (malla de navegación, `NpcSpot`, horario de tiendas) |
| [materiales.md](Docs/materiales.md) | Paleta, materiales y helpers comunes |
| [texturas.md](Docs/texturas.md) | `ProceduralTextures.cs`: texturas generadas en código |
| [calles.md](Docs/calles.md) | Calles grandes: `MainRoads.cs` |
| [detalle-vial.md](Docs/detalle-vial.md) | Detalle vial: `StreetDetail.cs` (nombres, semáforos, STOP, cables, alcantarillado) |
| [props-calle.md](Docs/props-calle.md) | Detalle callejero: `StreetProps.cs` |
| [vegetacion.md](Docs/vegetacion.md) | Vegetación de invierno: `Vegetation.cs` |
| [casas.md](Docs/casas.md) | Barrio de casas y sus copias y ampliaciones: `Houses.cs`, `HousesWestExt.cs`, `HousesNorth.cs` |
| [negocios.md](Docs/negocios.md) | Franja de negocios: `Shops.cs` |
| [torres.md](Docs/torres.md) | Manzana central: `Towers.cs` |
| [estudio.md](Docs/estudio.md) | Vida del bloque: `Studio.cs` |
| [practicas.md](Docs/practicas.md) | Zona de prácticas: `Training.cs` |
| [coches.md](Docs/coches.md) | Coches: `Cars.cs` |
| [tren.md](Docs/tren.md) | Tren elevado: `Train.cs` |
| [cocheras.md](Docs/cocheras.md) | Cocheras: `RailYard.cs` |
| [oeste-lejano.md](Docs/oeste-lejano.md) | Oeste lejano: `FarWest.cs` |
| [norte.md](Docs/norte.md) | Barrio norte: `North.cs`, `NorthBuildings.cs` (incluye `Prism`, para formas no alineadas con los ejes) |
| [autopista.md](Docs/autopista.md) | Autopista hundida: `Highway.cs` |
| [instituto.md](Docs/instituto.md) | Instituto: `School.cs` |
| [tiempo.md](Docs/tiempo.md) | Hora y tiempo: `Weather.cs` |
| [capturas.md](Docs/capturas.md) | Capturas in-game: `IngameScreenshots.cs` |

La casa del jugador (`Apartment.cs`) y el cuerpo (`Body.cs`) están descritos arriba, en la lista de archivos; el cuerpo, con más detalle, en [Scripts/Player/README](../Scripts/Player/README.md).
