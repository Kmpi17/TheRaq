# Player: el personaje a pie

Todo va en el objeto `Jugador` (lo monta `BuildPlayer` en el generador). El modelo visual está en el hijo `Modelo`, que la cobertura agacha y que se tumba al morir. El cuerpo (`BuildPlayerBody`) va por **articulaciones**, para animarlo, y por **piezas**, para vestirlo. Las piezas cuelgan hacia −Y de su articulación:

```
Modelo
└─ Cuerpo (pelvis, 0.92 m) ─ Pelvis, Cinturon, Hebilla
   ├─ CaderaIzq/Der ─ Muslo, BolsilloCargo · Rodilla ─ Espinilla, Bajo (del pantalón), Caña (botas) · Tobillo ─ Pie, Suela, Puntera
   └─ Columna ─ Torso, Faldon y CuelloCamiseta (camiseta XL), Capucha (capucha caída y bolsillo), Cordones, CuelloAlto,
      │         Chaleco, AberturaChaleco, CuelloCamisa, Botonera, Cuello, Cadena (eslabones y Colgante)
      ├─ Craneo ─ Cabeza (con la cara pintada), Nariz, PuntaNariz, Orejas,
      │           Gorra, Gorro, Pescador, Panuelo, Gafas (cristales y Montura)
      └─ HombroIzq/Der ─ Deltoide, Brazo, Manga · Codo ─ Antebrazo · Mano ─ Palma, Dedos, Pulgar
```

La pinta sale de la referencia `img/gang.png` (solo la ropa y las proporciones, no las personas): camiseta blanca XL larga y ancha, vaqueros claros anchos amontonados en el tobillo, botas de trabajo color trigo, chaleco negro encima de la camiseta, polo de rayas, camisa vaquera, gorro de pescador, pañuelo de cabeza, gafas de sol y cadena.

La forma también sigue a la referencia: casi todas las piezas son **mallas facetadas por anillos** (`LoftMesh`, en `PrototypeSceneBuilder.Body.cs`), no cápsulas ni esferas: torso cuadrado y estrecho de cintura, faldón de la camiseta que cae recto hasta medio muslo, mangas anchas hasta el codo, brazos largos con la manga de la sudadera holgada recogida en el puño, hombros y pecho algo llenos, **manos grandes** (como en PS2; las pequeñas parecían de muñeco), gorra con la copa sobre la frente y por encima de las orejas, frente alto y visera larga algo caída (antes bajaba hasta las orejas y, en negro, parecía un casco), vaqueros rectos con el bajo amontonado en ondas, botas grandes y cabeza ovalada de 12 lados con mandíbula y barbilla adelantadas. La cara (ojos, cejas, labios, bigote, perilla, sombra de barba y pelo rapado) va **pintada** en la textura `CaraPersonaje` (`ProceduralTextures.Face`, sin teñir); solo la nariz y las orejas son de malla. Las mallas tienen U alrededor (0.5 = frente) y V de abajo arriba, como esperan las texturas. `Widen` sigue funcionando porque escala la pieza en X y Z.

La ropa lleva textura de tejido y la piel una textura con grano (ver *Ropa*). La pistola (`Pistola`) sigue colgando de `Modelo` y la mano derecha la agarra por IK.

## Documentación por tema (`Docs/`)

Este README es el índice; el detalle de cada sistema está en [`Docs/`](Docs/). Lo nuevo se escribe en el archivo del tema.

| Archivo | Qué explica |
|---|---|
| [movimiento.md](Docs/movimiento.md) | `PlayerMovement` (inercia, esprint, agacharse, caída) y `MovementMath` |
| [vida-y-hud.md](Docs/vida-y-hud.md) | `PlayerVitals` (energía, muerte y reaparición) y `PlayerHud` |
| [escalar-y-cobertura.md](Docs/escalar-y-cobertura.md) | `PlayerClimb` y `PlayerCover` |
| [ropa.md](Docs/ropa.md) | `Clothing` y `PlayerOutfit`: prendas, texturas y cómo se visten los NPC |
| [animacion.md](Docs/animacion.md) | `PlayerAnimator` y `AnimationMath`: animación procedural en código |
