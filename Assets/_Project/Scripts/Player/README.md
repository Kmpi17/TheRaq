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

## `PlayerMovement.cs`
Movimiento realista pero arcade. Necesita un `CharacterController`; `PlayerVitals` y `Health` son opcionales (sin ellos no hay energía ni daño por caída, como en los tests).
- La velocidad horizontal va hacia la pedida con inercia (`MovementMath.Accelerate`): arranca con `acceleration`, frena con `deceleration` (más fuerte) y pasa de carrera a esprint más despacio (`sprintAcceleration`). En el aire casi no hay control (`airControl`).
- Gira rápido parado y más despacio cuanto más deprisa va (`MovementMath.TurnSpeed`, de `fastTurn` a `slowTurn`).
- Esprintar gasta energía (`sprintStaminaPerSecond`). Agotado no esprinta y va al 75 % de la carrera.
- **No salta.** Espacio es escalar (`PlayerClimb`).
- **Agacharse (C, o pulsar el stick izquierdo)**: alterna agachado y de pie. Solo se agacha en el suelo (vale haberlo tocado hace menos de 0.2 s, porque `isGrounded` parpadea). Agachado:
  - va a `crouchSpeed` (1.7 m/s, también apuntando) y no esprinta; pulsar Shift moviéndose lo levanta;
  - el CharacterController baja a `crouchHeight` (1.15 m, como en la cobertura baja), así que cabe por huecos bajos;
  - apunta mejor (dispersión ×0.7, `CombatMath.Spread`) y la cámara baja con él (`ThirdPersonCamera`);
  - solo se levanta si hay sitio encima (`Physics.CheckCapsule` hasta el alto de pie, con `standMask`); si no, el HUD avisa *No hay sitio para levantarse*.
  - Escalar (`PlayerClimb`, que mira el sitio con el alto de pie), morir, reaparecer y subir al coche lo ponen de pie (`ForceStand`). En cobertura manda `PlayerCover` (pone su alto y lo deja de pie al salir); al salir, `PlayerMovement` lo vuelve a agachar si seguía agachado.
- **Daño por caída**: al tocar suelo, `MovementMath.FallDamage` según la velocidad de impacto (hasta unos 4 m no hace nada) y frena en seco.
- Los empujones (`AddImpulse`) se suman y se apagan solos.
- **API para las otras habilidades:**
  - `Locked`: otra habilidad manda (escalar, cobertura, muerte) y aquí no se mueve nada.
  - `Aiming` + `AimYaw`: va a `walkSpeed`, no esprinta y mira hacia donde apunta la cámara.
  - `InputDirection`, `PlanarVelocity`, `IsSprinting`, `ResetMotion()`, `SetPlanarVelocity()`.
  - `Crouching`, `SetCrouching(bool)` (devuelve si quedó como se pidió; levantarse falla sin sitio), `ForceStand()`, `StandHeight`, `StandBlockedTime`.

| Campo | Defecto |
|---|---|
| `walkSpeed` (apuntando) / `runSpeed` / `sprintSpeed` | 2.2 / 4.4 / 7.2 m/s |
| `acceleration` / `deceleration` / `sprintAcceleration` | 16 / 24 / 6 m/s² |
| `fastTurn` / `slowTurn` | 900 / 280 °/s |
| `gravity` | −20 m/s² |
| `sprintStaminaPerSecond` | 12 |
| `crouchSpeed` / `crouchHeight` | 1.7 m/s / 1.15 m |

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
5. Comprobaciones (`RoomToLand`, `IsFloor`): que el punto de llegada sea **suelo de verdad** (a 22 cm en cruz también hay suelo a esa altura: el pasamanos de otra valla no vale; antes, al saltar una valla junto a una esquina, acababa de pie encima del tramo de al lado), que haya sitio arriba y en el punto de llegada (CheckCapsule), que no haya techo sobre la cabeza y, al pasar al otro lado, que el suelo no esté más de 3 m por debajo del borde (así no se salta a la trinchera de la autopista).
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
- avisos de escalar, cruzar coches, cubrirse, agachado (*Agachado · C: levantarse*, y *No hay sitio para levantarse*) y subir o robar un coche (con la barra de forzar la puerta);
- hora y tiempo (`WeatherSystem`) y, debajo, el barrio en el que estás (`NpcPopulation.TerritoryLabel`: verde si es tuyo o aliado, rojo si es enemigo, gris si es zona neutral);
- avisos de los NPC arriba en el centro (te han reconocido, incursión);
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
- **NPC:** `PlayerOutfit.Wear(ids, tonoDePiel)` viste un modelo con esas prendas sin guardarlas (`persist` apagado) y tiñe cara, cuello y manos (`TintSkin`) con el tono dado (1 = el del jugador). `Outfit.Of(ids)` hace un conjunto puesto con esas prendas. Prendas de facción (NPCS.md): `gorra_blanca`, `gorra_granate`, `gorra_verde`, `gorro_mostaza`, `sudadera_granate`, `camiseta_granate`, `sudadera_mostaza`, `sudadera_verde`; de civil: `sudadera_azul`, `plumifero_azul`, `abrigo_marron`. El jugador también las tiene en el armario.

## Animación: `PlayerAnimator.cs` y `AnimationMath.cs`
Animación procedural (sin Animator ni clips): `PlayerAnimator` gira las articulaciones de `Modelo` en `LateUpdate`, después de que el combate coloque la pistola, y cada articulación va hacia su pose con suavizado (`sharpness`). Rehecha el 2026-11-15 a partir de un vídeo del usuario (piernas tiesas, pataleo, brazos rígidos, sin peso ni personalidad) y de datos de la marcha real.

**Ciclo de paso** (`AnimationMath.Gait(phase, move, jog, sprint)`):
- Cada pierna sigue **curvas por claves** (`Cyclic`, Catmull-Rom con 10 claves por ciclo) de la marcha real, distintas al andar, trotar y esprintar: la cadera se flexiona ~25° y se extiende ~10° al andar (más hacia delante que hacia atrás), la rodilla amortigua ~18° al cargar el peso y se dobla ~60° en el vuelo (~100° trotando, ~128° esprintando, con el talón casi al glúteo), y el pie apoya el talón con la punta arriba y despega de punta (`FootL/R`: inclinación respecto al suelo; el tobillo la consigue compensando cadera y rodilla). La derecha va medio ciclo después.
- **Zancadas de cadencias reales** (`WalkStride` 1.7 m, `JogStride` 3.1, `SprintStride` 4.4: ~170 pasos/min trotando a 4.4 m/s y ~195 esprintando). Antes eran 1.4–2.6 m y el trote iba a ~270 pasos/min, un pataleo. La fase avanza con la distancia recorrida, así que los pies no patinan. Mezclas: `jog` de 2.6 a 4.4 m/s y `sprint` de 4.8 a 7.2 m/s; apuntando, agachado o en cobertura siempre se anda.
- Brazos contra su pierna, con el **codo retrasado** (se cierra al ir el brazo adelante y llega tarde: seguimiento), a ~90° trotando. Pelvis: **bota** (abajo en los dobles apoyos al andar; abajo en el apoyo y arriba en el vuelo al correr), **se va sobre la pierna de apoyo** (`SwayX`) y **cae del lado de la que vuela** (`PelvisRoll`); gira con la pierna que avanza y los hombros al revés (`SpineYaw`). Las caderas compensan el desplazamiento y la caída de la pelvis para que los pies no se muevan.
- **En cualquier dirección**: las piernas dan el paso hacia donde se mueve el cuerpo (`SwingAxis`), así que apuntando o en cobertura anda de lado y hacia atrás sin patinar. De lado, la pierna que cruzaría por delante da un paso corto (`SideStepHip`).

**Peso y personalidad** (encima del ciclo):
- **Inclinación**: todo el cuerpo (el `Modelo`, desde los pies) se inclina hacia dentro en las curvas (`bankPerTurn`, hasta 13°), hacia delante al arrancar y hacia atrás al frenar (por la aceleración, suavizada).
- **Aterrizaje**: tras caer, se hunde (caderas, rodillas, tronco) más cuanto más tiempo estuvo en el aire, y se recupera. **En el aire**, una pierna adelantada buscando el suelo y brazos algo abiertos.
- **Cabeza estable**: compensa la inclinación y el giro del tronco y mira al frente, con un leve cabeceo al paso.
- **Quieto**: carga el peso en una pierna (`AnimationMath.WeightShift`: la pelvis se va hacia ella y sube de ese lado, la otra rodilla se suelta, los hombros se inclinan al revés) y lo cambia cada 4.5–9 s; respira; de vez en cuando mira a un lado (18–35°) y vuelve. Brazos relajados, algo separados y con el codo un poco doblado; de base, algo cargado de hombros.
- `swagger` (0–1, 0.6): balanceo de caderas y hombros, cabeceo y brazos algo abiertos.

**Poses**:
- **Apuntando**: piernas abiertas con la izquierda adelantada y rodillas sueltas; la pistola va a la altura de la barbilla con los brazos casi estirados y gira alrededor del hombro con la mira (`PlayerCombat.AimPitch`). El pivote es el **hombro de verdad** (`PlayerAnimator.AimPivotY`): agachado o en cobertura baja la pistola baja con él (antes se quedaba a la altura de pie y, agachado, los brazos se iban por encima de la cabeza).
- **Pistola bajada**: va en la mano derecha (la coloca `PlayerAnimator`, cañón adelante y abajo) y ese brazo se balancea algo menos, con el codo más doblado. Antes la mano iba por IK a un punto fijo y el brazo iba tieso al correr.
- **Escalando** (`PlayerClimb.CurrentKind`, `EdgeY`, `EdgePoint`, `ClimbForward`, `Progress`): las dos manos agarran el borde por IK mientras los pies buscan apoyo en la pared; cerca del borde, una rodilla sube y el pecho se echa encima; ya arriba, se endereza. Al **saltar una valla o un coche**, la mano izquierda se apoya en lo alto, las piernas van recogidas y el otro brazo va **adelantado y algo arriba** (antes, en cruz). La pistola se oculta mientras se escala.
- **Agachado (C)**, fuera de la cobertura: pelvis 0.32 m más baja, muslos casi horizontales, un pie algo adelantado y las manos sobre las rodillas. Al moverse, el ciclo de andar va **encima** de esa pose (`hipSwing`), con pasos cortos (`CrouchStride`, 0.8 m por ciclo). Apuntando agachado, el tronco y la cabeza siguen la mira.
- **En cobertura** (alta: de lado contra la pared, brazos recogidos; asomándose, de cara; baja: agachado, con pasitos de lado) y **muerto** (brazos abiertos; `PlayerVitals` tumba el modelo).
- **IK de brazo** (`SolveTwoBone`, dos huesos de 0.3 m con el codo hacia abajo, atrás y fuera): las dos manos en la pistola al apuntar o recargar; en los **puñetazos**, el puño sale desde la guardia (el gancho, algo más alto) y la otra mano protege la cara; al **forzar un coche**, las dos manos forcejean con la puerta.
- En el coche no se anima (el modelo está oculto). Si el modelo no tiene articulaciones (uno antiguo), se apaga solo.
