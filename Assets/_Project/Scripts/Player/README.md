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

La forma también sigue a la referencia: casi todas las piezas son **mallas facetadas por anillos** (`LoftMesh`, en `PrototypeSceneBuilder.Body.cs`), no cápsulas ni esferas: torso cuadrado y estrecho de cintura, faldón de la camiseta que cae recto hasta medio muslo, mangas anchas hasta el codo, brazos largos y delgados, vaqueros rectos con el bajo amontonado en ondas, botas grandes y cabeza ovalada de 12 lados con mandíbula y barbilla adelantadas. La cara (ojos, cejas, labios, bigote, perilla, sombra de barba y pelo rapado) va **pintada** en la textura `CaraPersonaje` (`ProceduralTextures.Face`, sin teñir); solo la nariz y las orejas son de malla. Las mallas tienen U alrededor (0.5 = frente) y V de abajo arriba, como esperan las texturas. `Widen` sigue funcionando porque escala la pieza en X y Z.

La ropa lleva textura de tejido y la piel una textura con grano (ver *Ropa*). La pistola (`Pistola`) sigue colgando de `Modelo` y la mano derecha la agarra por IK.

## `PlayerMovement.cs`
Movimiento realista pero arcade. Necesita un `CharacterController`; `PlayerVitals` y `Health` son opcionales (sin ellos no hay energía ni daño por caída, como en los tests).
- La velocidad horizontal va hacia la pedida con inercia (`MovementMath.Accelerate`): arranca con `acceleration`, frena con `deceleration` (más fuerte) y pasa de carrera a esprint más despacio (`sprintAcceleration`). En el aire casi no hay control (`airControl`).
- Gira rápido parado y más despacio cuanto más deprisa va (`MovementMath.TurnSpeed`, de `fastTurn` a `slowTurn`).
- Esprintar gasta energía (`sprintStaminaPerSecond`). Agotado no esprinta y va al 75 % de la carrera.
- **No salta.** Espacio es escalar (`PlayerClimb`).
- **Daño por caída**: al tocar suelo, `MovementMath.FallDamage` según la velocidad de impacto (hasta unos 4 m no hace nada) y frena en seco.
- Los empujones (`AddImpulse`) se suman y se apagan solos.
- **API para las otras habilidades:**
  - `Locked`: otra habilidad manda (escalar, cobertura, muerte) y aquí no se mueve nada.
  - `Aiming` + `AimYaw`: va a `walkSpeed`, no esprinta y mira hacia donde apunta la cámara.
  - `InputDirection`, `PlanarVelocity`, `IsSprinting`, `ResetMotion()`, `SetPlanarVelocity()`.

| Campo | Defecto |
|---|---|
| `walkSpeed` (apuntando) / `runSpeed` / `sprintSpeed` | 2.2 / 4.4 / 7.2 m/s |
| `acceleration` / `deceleration` / `sprintAcceleration` | 16 / 24 / 6 m/s² |
| `fastTurn` / `slowTurn` | 900 / 280 °/s |
| `gravity` | −20 m/s² |
| `sprintStaminaPerSecond` | 12 |

## `PlayerVitals.cs`
Energía (`StaminaPool`, ver `Scripts/Combat/`) y salud del jugador (con `Health`).
- La vida baja se recupera sola hasta el 25 % tras 8 s sin daño.
- Al morir: sale de la cobertura, bloquea el movimiento y tumba el `Modelo`. A los `respawnSeconds` (5) reaparece donde empezó, con la vida y la energía llenas, y paga la factura del hospital (`hospitalBill`, 100 $, o lo que tenga).

## `PlayerClimb.cs`
Espacio delante de un obstáculo (hacia donde se pide ir o hacia donde mira):
1. Un SphereCast a la altura de las rodillas busca una cara casi vertical.
2. **Coches** (lo tocado tiene un `VehicleController` en un padre): un rayo de vuelta desde el otro lado da el ancho, y unos rayos hacia abajo cada 0.3 m dan lo más alto del camino. Si no pasa de `ClimbMath.SlideMax` (1.6 m: capó, maletero o techo de un sedán) lo **cruza por encima** (`SlideOver`) hasta el suelo del otro lado. Las furgonetas y los todoterreno por el techo siguen con la escalada normal (suben encima).
3. Dos rayos hacia abajo dan la altura: uno a 1.5 cm de la cara (las vallas son tablas de 3 cm, y el de 0.2 m caía al otro lado, así que no se podían saltar) y otro a 0.2 m; vale el más alto. Otro, 0.75 m más adentro, dice si es grueso.
4. `ClimbMath.Classify`:
   - menos de 0.45 m o más de 2.4 m: nada;
   - grueso: **subir** encima;
   - fino de hasta 1.25 m: **saltar por encima**;
   - fino más alto: **trepar** al otro lado.
5. Comprobaciones (`RoomToLand`): que haya sitio arriba y en el punto de llegada (CheckCapsule), que no haya techo sobre la cabeza y, al pasar al otro lado, que el suelo no esté más de 3 m por debajo del borde (así no se salta a la trinchera de la autopista).
6. Gasta energía (`ClimbMath.StaminaCost`). Si no hay, avisa "Sin fuerzas".
7. Recorre el camino (pegarse, subir, pasar, bajar) con el CharacterController apagado, en `ClimbMath.Duration`. Al saltar por encima o cruzar un coche sale corriendo.

Solo se escala lo que se ve (`SolidVisual.IsVisible`: `Renderer` en el mismo objeto que el collider, o `SolidVisual` en él o en un padre, como los coches) y no lleva `NoClimb` en él ni en un padre (`PlayerClimb.IsClimbable`). Los colliders invisibles del generador (límites, rampas) no se escalan. La máscara solo ignora la capa 2 (el jugador): los coches (capa 6, `Vehiculo`) sí se escalan.

## `PlayerCover.cs`
Q junto a un obstáculo visible y **opaco** de al menos 0.85 m de alto (pared, esquina de casa, muro, valla de tablones, coche, contenedor). Lo que deja ver a través no cubre (`SolidVisual.IsSeeThrough`): material con recorte por alfa (verjas de barrotes, mallas, barandillas caladas) o `NoCover` en él o en un padre. Esas verjas sí se saltan con Espacio.
- Por debajo de 1.45 m es cobertura **baja**: se agacha (CharacterController y `Modelo` más bajos).
- Se mueve de lado pegado a la pared y se para en las esquinas (`AtEdge`). La normal se vuelve a tomar en cada fotograma, así que sigue paredes curvas.
- Se puede entrar desde tan lejos como la busca (`searchDistance`): mientras sigue en cobertura, la pared se comprueba con ese mismo alcance y el tirón (hasta 4 m/s) lo pega a ella. Antes se comprobaba a `radio + 0.8 m` y, si se pulsaba Q a un metro, entraba y salía en el mismo fotograma.
- En la cobertura alta, sin apuntar, el modelo se pone **de lado**: hombro contra la pared y mirando a lo largo de ella hacia donde se movió (lo hace `PlayerAnimator`; el `Jugador` sigue de cara a la pared).
- Apuntando (`Peeking`, lo pone `PlayerCombat`): en cobertura baja se levanta; en alta sale de lado `peekStep` (0.65 m) si está en una esquina.
- Se sale con Q, esprintando, tirando hacia atrás 0.3 s, si desaparece la pared, al subir a un coche o al morir.
- Las balas son rayos, así que la cobertura protege de verdad.

## `PlayerHud.cs`
HUD provisional con OnGUI:
- barras de vida, chaleco y energía (naranja si está agotado, verde con la bebida energética);
- arma, balas (cargador / reserva) y dinero;
- mira cuyo hueco es la dispersión real, y marca de impacto (roja si mata);
- barra de recarga;
- avisos de escalar, cruzar coches, cubrirse y subir o robar un coche (con la barra de forzar la puerta);
- destello rojo al recibir daño y pantalla de *HOSPITALIZADO*.

## `MovementMath.cs`
Funciones puras, testeadas en `Tests/EditMode/MovementMathTests.cs` y `CharacterSystemsTests.cs`:
- `CameraRelative(input, cameraYaw)`: dirección horizontal en el mundo, con longitud ≤ 1.
- `Accelerate(current, target, acceleration, deceleration, dt)`, `TurnSpeed(...)`, `FallDamage(impactSpeed)`.
- `JumpVelocity`: ya no se usa (no hay salto). Se deja por si vuelve.
- `ClimbMath`: `Classify`, `ClassifyVehicle` (cruzar coches), `Duration` y `StaminaCost`, con las alturas límite en constantes.

La lógica nueva que no dependa del motor va aquí o en otra clase estática parecida, para poder testearla sin Play.

## Ropa: `Clothing.cs` y `PlayerOutfit.cs`
- `ClothingCatalog` (puro): prendas genéricas, sin marcas, por partes (`ClothingSlot`: cabeza, torso, piernas, pies, cara y cuello) y con un estilo (`ClothingStyle`) que decide qué piezas del modelo se ven.

  | Parte | Prendas (estilo) |
  |---|---|
  | Cabeza | gorra roja o negra, gorro de lana gris o negro, gorro de pescador blanco o beige, pañuelo de cabeza negro, o nada |
  | Torso | sudadera negra o gris (capucha), plumífero negro o beige (más ancho, con costuras), camiseta XL blanca, gris, amarilla o verde (ancha, faldón hasta medio muslo, brazos al aire), polo de rayas (los colores van en la textura), chaleco negro con camiseta blanca debajo, camisa vaquera (cuello y botonera) |
  | Piernas | vaqueros oscuros, negros o claros (anchos, amontonados en el tobillo), chándal gris (holgado, con puño), pantalón caqui (bolsillos a los lados) |
  | Pies | zapatillas blancas o negras, botas de trabajo (con caña; el pantalón se amontona encima) |
  | Cara | gafas de sol negras, doradas o blancas (se tiñe la montura), o nada |
  | Cuello | cadena de oro o de plata, cadena con colgante, o nada |

  Se empieza con todo (`StarterWardrobe`) y con gorra roja, sudadera negra, vaqueros oscuros y zapatillas blancas (`StarterOutfit`).
- `Outfit` (puro): prendas que tiene y cuál lleva en cada parte. `Equip(id)` (tiene que tenerla), `Unequip(slot)` (solo cabeza, cara y cuello: `ClothingCatalog.IsOptional`), `Give(id)`, evento `Changed`, `Serialize` / `Deserialize` (si el texto está roto, completa con la ropa de inicio; el armario básico se tiene siempre, así que las prendas nuevas del catálogo salen también en partidas viejas). Los estilos nuevos van al final de `ClothingStyle`, porque las texturas se indexan por ese orden.
- `PlayerOutfit` (en el jugador): carga y guarda el `Outfit` en PlayerPrefs (`jugador.ropa`) y lo pinta en `Modelo`: enciende o apaga piezas por nombre (capucha, bolsillo y cordones con sudadera; cuello alto con plumífero; faldón, cuello redondo, mangas cortas y brazos al aire con camiseta XL; chaleco con su abertura; cuello de solapas con polo y camisa, y botonera con camisa; bolsillos con caqui; caña con botas; gorros, gafas y cadena), las ensancha y les pone el color **y la textura del tejido** de su estilo con un `MaterialPropertyBlock` (`styleTextures`, una por `ClothingStyle`, y `skinTexture`, que pone el generador con `SetTextures`). Tejidos (128 px, pintados al estilo PS2 de `img/gang.png`: pliegues con cresta clara y surco oscuro, sombra horneada, costuras y desgaste; V=0 es el extremo de abajo de cada pieza): sarga de gorra y caqui con costuras de panel, felpa con puño de canalé abajo (sudadera, gorro, chándal), plumífero acolchado en franjas, algodón con caídas en diagonal y pliegues del faldón (camiseta), vaquero con hebra vertical, desgaste claro, costuras laterales y el bajo amontonado en arrugas, zapatilla con pespuntes y perforaciones, nobuk de bota con rozaduras y cuello acolchado, raso (pañuelo, gafas, cadena) y polo de rayas anchas de colores con pliegues. En el editor el generador viste el modelo con la ropa de inicio (`PreviewInEditor`, que guarda las escalas de reposo para que al arrancar no se ensanche dos veces) y salen los materiales `Ropa_<prenda>`. Se cambia de ropa en el armario de casa (`Scripts/Home/`).

## Animación: `PlayerAnimator.cs` y `AnimationMath.cs`
Animación procedural (sin Animator ni clips): `PlayerAnimator` gira las articulaciones de `Modelo` en `LateUpdate`, después de que el combate coloque la pistola, y cada articulación va hacia su pose con suavizado (`sharpness`).
- **Andar, correr y esprintar** con `AnimationMath.Gait`: piernas en oposición, rodillas que se doblan al pasar la pierna, pie casi plano, brazos al revés que su pierna, bote y giro de la pelvis, contragiro de la columna e inclinación hacia delante. La fase avanza con la distancia recorrida (`AdvancePhase`: una zancada de 1.4 m andando y 2.6 m esprintando), así que los pies no patinan. De andar a esprintar se mezcla con la velocidad (`runSpeed` 4.4, `sprintSpeed` 7.2); rodillas, codos e inclinación suben con la raíz de la mezcla, para que el trote (la velocidad normal) se vea como trote y no como un andar rápido.
- **En cualquier dirección**: las piernas dan el paso hacia donde se mueve el cuerpo (`AnimationMath.SwingAxis`: el ciclo gira en el eje perpendicular al movimiento, en el espacio del cuerpo), así que apuntando o en cobertura anda de lado y hacia atrás sin patinar. De lado, la pierna que cruzaría por delante da un paso corto (`SideStepHip`). El tronco solo se inclina y se tuerce al ir hacia delante, y los brazos casi no se balancean de lado.
- **Apuntando**: piernas abiertas con la izquierda adelantada y rodillas sueltas; la pistola va a la altura de la barbilla con los brazos casi estirados y gira alrededor del hombro con la mira (`PlayerCombat.AimPitch`), y el tronco y la cabeza la siguen. En reposo, la pistola apunta al suelo delante.
- **Quieto**: respira (la columna se mece) y los brazos se balancean un poco.
- **Escalando** (`PlayerClimb.CurrentKind`, `EdgeY`, `EdgePoint`, `ClimbForward`, `Progress`): las dos manos agarran el borde por IK (si queda lejos, el brazo se estira hacia él) mientras los pies buscan apoyo en la pared; cerca del borde, una rodilla sube y el pecho se echa encima; ya arriba, se endereza. Al **saltar una valla o un coche**, la mano izquierda se apoya en lo alto, las piernas van recogidas y el otro brazo se abre para equilibrarse. La pistola se oculta mientras se escala.
- **En el aire**, **en cobertura** (alta: de lado contra la pared, brazos recogidos; asomándose, de cara; baja: agachado de verdad, con las rodillas dobladas y pasitos de lado) y **muerto** (brazos abiertos; `PlayerVitals` tumba el modelo).
- **IK de brazo** (`AnimationMath.SolveTwoBone`, dos huesos de 0.3 m con el codo hacia abajo, atrás y fuera): la mano derecha empuña la pistola siempre que se lleva y la izquierda la sujeta al apuntar o recargar; en los **puñetazos**, el puño sale desde la guardia hacia delante (el gancho, algo más alto) y la otra mano protege la cara (`PlayerCombat.PunchExtend`, `PunchSide`, `PunchIsHook`); al **forzar un coche**, las dos manos forcejean con la puerta.
- En el coche no se anima (el modelo está oculto). Si el modelo no tiene articulaciones (uno antiguo), se apaga solo.
