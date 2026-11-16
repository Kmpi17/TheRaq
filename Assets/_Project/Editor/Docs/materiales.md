# Paleta y materiales
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

# Helpers
- `Building(parent, name, base, size, wall)`: crea los muros, la cornisa y una franja oscura de ventanas por planta (cada 3.2 m).
- `ProjectBuilding(...)`: edificio del barrio con muros texturizados, tejado con alero (bajo) o cornisa y caseta (torre), portal, marquesina, escalón y camino.
- `TexturedBox(name, parent, pos, size, material, tile, collider, sidesOnly)`: caja con malla propia (`BoxMesh`) y UVs en metros. Cada cara repite la textura un número **entero** de veces (se redondea), así que las ventanas cuadran en las esquinas; en los laterales la V empieza en el suelo. Las mallas quedan guardadas dentro de la escena.
- `BareTree(parent, pos, bark, rng, scale[, out height])`: árbol pelado con tronco y 5 ramas. Devuelve la raíz; la sobrecarga con `out` da la altura del tronco (la usa `DistrictTree` para poner la copa).
- `Box`/`Primitive`: crean primitivas con material y, si se pide, quitan el collider.
- `SetReference`, `SetLayerRecursively`, `MarkStatic`, `AddSceneToBuild`, `EnsureFolder`.
