# Scripts/NPC — la gente del barrio y las facciones

Implementa lo diseñado en [`NPCS.md`](../../../../NPCS.md) (reglas del usuario, investigación y facciones). Todo es
ficticio: ni los grupos, ni los nombres, ni la ropa copian bandas o personas reales.

## Piezas

| Archivo | Qué hace |
|---|---|
| `Factions.cs` | `Faction` (AshBlock, Kessler, Crestline, Dunmore, Larkin), `FactionCatalog`: bando de cada una, relación (mismo bando = aliado, distinto = enemigo, civiles = neutral), territorios (rectángulos x/z), centro de cada barrio y ropa al azar con el color de cada grupo. Lógica pura. |
| `NpcMath.cs` | Números de la vida del barrio: civiles y pandilleros según luz del día, lluvia y nieve; probabilidad de ir armado; probabilidad de incursión; horario de las tiendas; puntuación de un sitio; sospecha del enemigo; alcance de vista; dispersión de los tiros. Lógica pura. |
| `NpcSpot.cs` | Sitio inteligente: actividad (`Hangout`, `Sit`, `Sell`, `Errand`, `Wait`, `Mourn`, `Warm`), plazas, radio, cubierto, para civiles y/o grupo, habitual, facción (o la del territorio) y tienda de la que depende. `Claim` da una plaza; `FacingFrom` hacia dónde mirar. |
| `ShopHours.cs` | Horario de un local: de 8:00 a 21:00 (la tienda de licores, siempre). Cerrado apaga el OPEN y baja la persiana entera. Lee la hora de `WeatherSystem`. |
| `NpcAlerts.cs` | Disparos (se oyen a 45 m) y golpes (12 m) de los últimos 3 s, con quién y a quién. Los avisan `PlayerCombat` y `NpcBrain`. |
| `NpcBrain.cs` | La cabeza de un NPC (piensa cada 0.2 s, anda con `NavMeshAgent`). Estados: `Idle`, `Walking`, `AtSpot`, `Flee`, `Cower`, `Stare`, `Confront`, `Combat`, `Raid`, `Leave`, `Dead`. |
| `NpcAnimator.cs` | Animación procedural sobre el mismo cuerpo que el jugador (`AnimationMath`): paso según la velocidad del agente, peso en una pierna, mirar alrededor o a quien vigila; sentado, corro (brazos cruzados o gesticulando), vendiendo, calentándose en el bidón, agachado con las manos en la cabeza, apuntando con las dos manos, puñetazo y caída al morir. Coloca la pistola. |
| `NpcPopulation.cs` | Población alrededor del jugador e incursiones (ver abajo). `NpcPopulation.Instance`; `Spawn(...)` para tests y capturas; `TryRaid(force)`; `Toast(texto)` para el HUD; `TerritoryLabel`/`TerritoryColor`. |

## Reglas que se cumplen

- **Población** (cada 1 s): civiles ~22 de día y ~2 de madrugada; pandilleros ~8 de día y ~15 de noche; la lluvia y la
  nieve quitan muchos civiles y pocos pandilleros. Salen entre 35 y 110 m, **fuera de la vista**, en un `NpcSpot`
  (los grupos, de 1 a 3, en un sitio de su barrio y vestidos con su color). Se quitan a más de 140 m sin verse, al irse
  (`Leave`) en cuanto no se ven, y los muertos a los 40 s. Máximo 40 a la vez, 2 tandas por segundo.
- **Elegir sitio** (`NpcMath.SpotScore`): los de su grupo pesan ×3 (habituales ×6); los del enemigo, nunca; con
  precipitación, lo cubierto gana; más lejos y más lleno, menos. Un 15 % de las veces, pasear a un punto al azar.
  Se quedan 60–200 s (grupo), 15–60 s (civiles), 6–14 s (recados). Si la tienda cierra, se van.
- **Civiles:** huyen de disparos, golpes y de quien les apunta de cerca; se agachan con las manos en la cabeza unos
  segundos y se van.
- **Enemigos** (Dunmore, Larkin) **en su barrio:** si te ven (22 m de día, 30 de noche, con línea de vista) la sospecha
  sube (más rápido cerca y de noche). A 1 te miran, a 2.5 te encaran (se acercan a 6 m; aviso en el HUD) y a 4 sacan el
  arma. En el coche no te reconocen. **Fuera de su barrio** no te atacan salvo en incursión o si les provocas.
- **Aliados y propios** (Ash Block, Kessler, Crestline): no te atacan; si les disparas, te guardan rencor y responden
  (ellos y los de su bando que lo vean). Defienden su zona: disparan a los enemigos que entren en su territorio, a los
  de una incursión y a los que ya estén peleando cerca, y ayudan si un enemigo te dispara cerca de ellos.
- **Combate:** armados, se paran a disparar si te ven a menos de 20 m (si no, van a donde te vieron); ráfagas de 2–4
  tiros, cargador de 12 y 2.2 s de recarga, con el daño de la 9 mm (50 en el pecho; la zona la decide `Health`, ver
  `Combat/README`, *Vida realista por zonas*); dispersión según distancia, movimiento y
  pericia. Sin arma, puñetazos de 7. A los 10 s sin verte lo dejan. Muy heridos, a veces huyen. Al morir sueltan a
  veces balas.
- **Incursiones:** cada minuto, si estás en un barrio de tu bando, se tira `NpcMath.RaidChance` (~4 % de día, ~22 % de
  noche, menos con mal tiempo; mínimo 150 s entre una y otra). Llegan 3–4 armados del enemigo más cercano a 70–90 m,
  desde el lado de su barrio, hacia ti; aviso en el HUD. Se van a los 25 s si no encuentran pelea.
- **Tiendas:** cierran de 21:00 a 8:00 salvo la FOOD & LIQUOR; cerradas, persiana bajada y sin OPEN.

## Capas y montaje

- Los NPC van en la capa 7 **"Personaje"** (`GameLayers.Npc`). La cámara no choca con ella; la vista de los NPC
  (`Linecast`) ignora las capas 2 (jugador) y 7, y sus disparos sí dan al jugador.
- Cápsula hasta los hombros (1.5 m) y una esfera `Cabeza` en `Craneo` para los tiros a la cabeza (el nombre lo usa el
  combate).
- La plantilla `PlantillaNPC` (inactiva, bajo `Poblacion`) la crea el generador copiando el `Modelo` del jugador
  (`PrototypeSceneBuilder.Npc.cs`). La ropa se pone con `PlayerOutfit.Wear(ids, tonoDePiel)`, sin guardarla.
- La malla de navegación (`NavMeshSurface`, paquete `com.unity.ai.navigation`) se construye con la escena y se guarda
  en `Scenes/Prototype_NavMesh.asset` (no se versiona).

## Limitaciones y lo que falta

- Los civiles cruzan por donde sea (no hay pasos de cebra en la malla ni semáforos).
- No hay coches con NPC (drive-bys), policía, diálogos ni misiones de facción.
- La sospecha no tiene en cuenta tu ropa (podrías ir de incógnito con los colores del enemigo: idea pendiente).
- Los NPC no se cubren ni escalan; solo esquivan con el NavMeshAgent.
- Sin sonido de voces; el aviso de "te han reconocido" sale en el HUD.

## Tests

- EditMode `NpcTests`: relaciones, territorios, población día/noche y con tiempo, incursiones, horario, puntuación de
  sitios, sospecha, ropa de las facciones y plazas de un sitio.
- PlayMode `NpcPlayTests` (malla hecha en el momento): el enemigo en su barrio mira y luego dispara; el aliado no; el
  enemigo fuera de su barrio no; el civil huye de un disparo; disparar a un aliado le da rencor.
- PlayMode `NpcCapturePlayTests` (`[Explicit]`): capturas en la escena real a `ingame/npc/<GTACHIRAQ_CAPTURA>/`.

## Vida y chaleco
Los NPC usan el mismo daño por zonas que el jugador (`DamageModel`): un tiro en la cabeza los mata, dos en el pecho también, y en brazos o piernas aguantan seis. Al aparecer, `NpcPopulation.Spawn` les pone a veces chaleco antibalas (`NpcMath.VestChance`: civiles nunca, pandilleros armados 20 %, desarmados 5 %, en incursión 40 %), con `NpcMath.VestArmor` = 100, así que aguantan unos 5 tiros en el pecho. El chaleco todavía no se ve en el modelo.
