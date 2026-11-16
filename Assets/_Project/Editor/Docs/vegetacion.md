# Vegetación de invierno: `PrototypeSceneBuilder.Vegetation.cs`
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
