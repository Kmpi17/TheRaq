# `ProceduralTextures.cs`
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
