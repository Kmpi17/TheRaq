# NPC: diseño del comportamiento

Cuaderno de diseño de los personajes no jugadores: cómo deben comportarse, por qué y cómo se hace.

**Estado (2026-11-15): implementado por fases** en [`Scripts/NPC/`](Assets/_Project/Scripts/NPC/README.md) (detalle, números y limitaciones en su README). Ya funcionan la población por hora y tiempo, los sitios habituales y de paso (`NpcSpot`, el `ActivitySpot` de abajo), las tiendas que cierran, los territorios, la reacción de enemigos (mirar → encarar → disparar) y aliados (defienden su zona), los civiles que huyen y las incursiones nocturnas. Falta: coches con NPC, policía, misiones de facción y la ropa como disfraz. Se lee antes de tocar cualquier NPC. Depende del sistema de hora y tiempo (`WeatherSystem`, `DayCycleMath`, `WeatherMath`; ver [World/README](Assets/_Project/Scripts/World/README.md)).

Todo es ficticio: grupos, bloques, personajes y lugares inventados, sin nombres de bandas reales ni parecidos con personas reales (ver `CLAUDE.md`).

## 1. Lo que pide el usuario (2026-11-15)

| Momento | Civiles | Pandilleros | Tiendas | Incursiones de bloques rivales |
|---|---|---|---|---|
| **Día** | Muchos y de todo tipo | Vendiendo o pasando el rato | Abiertas | Probabilidad **baja** |
| **Noche** | Casi ninguno | **Más activos**: venden más y hacen incursiones | Cerradas, **salvo las de licores** | Probabilidad **alta** |
| **Lluvia / nieve** | Todavía menos; los que hay se refugian | Parecido, pero las reglas siguen siendo las del día o de la noche | Igual que sin lluvia | Siguen la regla de día o de noche, algo más bajas |

- **Lugares icónicos y esquinas habituales:** cada grupo tiene sus sitios (una esquina, la puerta de la tienda de licores, la zona de reunión, el memorial, un porche), donde se le suele encontrar. Pero no son exclusivos: también se le puede encontrar por otras partes del barrio.
- El objetivo es que el comportamiento se sienta **humano y creíble**, no un decorado que se repite.

## 2. Qué dice la realidad

Investigación complementaria para que el ritmo del barrio se parezca al de verdad. Se usa como guía de diseño, no como manual.

- **Teoría de las actividades rutinarias:** la actividad humana sigue ritmos diarios, semanales y estacionales, y los conflictos se concentran donde hay más paso. → En el juego, las horas y los sitios con más gente son también los de más tensión.
- **Mercados callejeros al aire libre** (estudios de Cincinnati y guía del POP Center):
  - La actividad se concentra desde primera hora de la tarde hasta la noche.
  - Hay picos en torno a los días 1 y 15 de cada mes (días de cobro).
  - Varía con la estación.
  - Los grupos estables mantienen sus sitios durante mucho tiempo, y los conflictos entre grupos son lo que acaba en violencia en la calle.

  → Los pandilleros "vendiendo" van poco por la mañana, más por la tarde y al máximo por la noche. Sus esquinas son estables. Las incursiones nacen de rivalidades entre bloques.
- **El tiempo y la calle** (Tompson y Bowers 2015; Sommer et al. 2019; GeoHealth 2018):
  - La lluvia reduce sobre todo lo que se hace fuera *por gusto*, más de noche y en fin de semana.
  - El buen tiempo saca a la gente a la calle.
  - Los inviernos suaves tienen más actividad (y más crimen) que los fríos.

  → Con lluvia o nieve caen mucho los civiles que pasean y bastante menos los que "tienen que" estar fuera (los que venden o van a la tienda). Los grupos no desaparecen: se ponen a cubierto en porches, portales, bajo el tren o en la puerta de la tienda de licores. Al despejar, la calle se vuelve a llenar poco a poco, no de golpe.
- **Otros juegos de mundo abierto** (Neverness to Everness):
  - La densidad de gente depende de la hora y del tipo de zona (comercial, residencial, de ocio). De noche baja mucho y la mayoría de las tiendas cierran.
  - Con nieve, la gente se mete en tiendas y estaciones; una tormenta vacía un barrio, que luego se repuebla poco a poco.
  - Los NPC no siguen bucles fijos: deciden según la hora, el tiempo, la cercanía del jugador y lo que pasa cerca, y su reacción crece con la gravedad de lo que hace el jugador.
- **IA ambiental en la industria** (Černý et al.):
  - Los objetos inteligentes y las áreas inteligentes son la forma habitual de dar "vida diaria" a muchos NPC sin que cada uno tenga que saber hacerlo todo.
  - Es la parte de la IA de juegos menos desarrollada y la que más vida da al mundo.

## 3. Cómo se haría

Propuesta técnica, siguiendo el estilo del proyecto: la lógica pura va aparte y se testea en EditMode (como `DayCycleMath` y `WeatherMath`), y lo que se coloca en el mundo lo pone el generador de la escena.

### Población: cuántos y de qué tipo
- **Tabla de población:** para cada tipo de zona (torres, casas, negocios, avenida, parque) y franja horaria (mañana, tarde, noche, madrugada) da cuántos NPC de cada tipo hay. Tipos: civil paseando, civil con recado, vecino en su porche, pandillero pasando el rato, pandillero vendiendo, cliente.
- **Factor de tiempo:** se aplica con `WeatherSystem.Rain` y `Snow`. Cada tipo lleva su propio factor (paseantes ×0.2 con lluvia fuerte; los que venden, ×0.8 y a cubierto).
- **Transición suave** entre franjas, usando `DayCycleMath.Daylight`, y al cambiar el tiempo, usando los 40 s de `WeatherMath.Approach`, para que nadie aparezca ni desaparezca de golpe.
- **Calendario opcional:** día de la semana y día del mes, para los picos de los días de cobro y el fin de semana.

### Aparición alrededor del jugador
- Los NPC aparecen en un anillo de ~40–120 m alrededor del jugador, **fuera de la vista** (detrás de la cámara, tras una esquina o dentro de la niebla), y se retiran cuando quedan lejos y no se ven.
- Hay un tope de NPC activos (por ejemplo 30–40). Se reutilizan los objetos (pool) en vez de crearlos y destruirlos.
- **LOD de la IA:** los cercanos piensan cada fotograma; los lejanos, cada pocos segundos y sin animación fina.
- Los grupos de las esquinas habituales existen aunque el jugador no esté cerca, aunque sea solo como dato: al acercarse, siguen ahí. Así el barrio tiene continuidad.

### Sitios y actividades: objetos y zonas inteligentes
- Un componente, por ejemplo `ActivitySpot`, en esquinas, bancos, porches, la barbacoa, la canasta, la puerta de la tienda de licores, los portales y el memorial. Anuncia:
  - qué actividades ofrece: sentarse, pasar el rato, vender, refugiarse, hacer cola, jugar, recordar en el memorial;
  - para qué tipos de NPC;
  - en qué horario;
  - cuántas plazas tiene;
  - si es **a cubierto** (para la lluvia y la nieve).
- El NPC no sabe hacer cada cosa: va al sitio y el sitio le dice dónde ponerse y qué animación toca.
- **Zonas inteligentes** para grupos: una esquina admite de 3 a 5 personas y reparte los papeles. Uno vigila la calle, otros hablan apoyados en la valla y alguien entra y sale de la tienda.

### Elegir qué hacer: utilidad con algo de azar
- Cada NPC puntúa las actividades disponibles según:
  - su tipo y sus necesidades (aburrimiento, dinero, frío, cansancio);
  - la hora y el tiempo (con lluvia, lo que está a cubierto sube mucho);
  - la distancia;
  - lo llenos que estén los sitios.
- Elige entre las mejores con un poco de azar, para que no se vea mecánico.
- **Esquinas habituales con preferencia, no exclusivas:** cada grupo tiene sus sitios con mucho peso y otros secundarios con menos. Por eso "suelen estar ahí", pero a veces aparecen en otra parte.
- Hay pequeñas rutinas que dan personalidad: ir a la tienda y volver, saludar a otro del grupo, cambiar de esquina al anochecer, recogerse en el portal cuando empieza a llover.

### Incursiones de bloques rivales: un "director"
- Un director del barrio tira cada pocos minutos la probabilidad de una incursión. Es **baja de día y alta de noche**, algo menor con lluvia o nieve y mayor si hace poco hubo un conflicto (rivalidad que se acumula).
- Es lógica pura con su test, como `WeatherMath`.
- Si sale una incursión:
  - un grupo rival aparece fuera de la vista y va hacia una esquina del bloque;
  - los civiles que queden huyen;
  - los pandilleros del bloque reaccionan;
  - el HUD puede avisar con poco, por ejemplo un coche que pasa despacio.
- Entre incursiones hay un tiempo mínimo, para que no se encadenen.

### Tiendas
- Cada negocio tiene horario. De día abre; de noche cierra con la persiana bajada y el letrero apagado.
- **La tienda de licores no cierra.** De noche es el punto de reunión principal y el refugio de la lluvia.
- Cuando se programe, el horario se puede enganchar a `NightLight`/`WeatherSystem` igual que las farolas.

### Movimiento y reacciones
- **NavMesh** para andar por aceras, cruces y callejones. El paquete `com.unity.ai.navigation` ya está en el proyecto, y la NavMesh se generaría al construir la escena.
- Los civiles cruzan por los pasos y evitan las zonas oscuras de noche.
- **Reacciones graduadas:**
  - Un empujón: miran o protestan.
  - Un arma a la vista: los civiles se apartan.
  - Disparos: los civiles huyen y los pandilleros se cubren o responden.
- Se recuerda lo que pasó durante un rato: la esquina se queda vacía después de un tiroteo.

### Orden propuesto para programarlo
*(Hecho del 1 al 6 en la primera fase; ver el estado arriba.)*
1. La lógica pura: tabla de población, factores de hora y tiempo, puntuación de utilidad y el director de incursiones, con sus tests.
2. `ActivitySpot` y su colocación en el generador (esquinas, porches, tienda de licores, zona de reunión, memorial).
3. Un NPC básico: modelo de piezas como el jugador, NavMesh, ir a un sitio y hacer la actividad.
4. La aparición y retirada alrededor del jugador, el pool y el LOD de la IA.
5. Los grupos en esquinas, las incursiones y las reacciones a la violencia.
6. Los horarios de las tiendas.

## 4. Facciones y territorios (2026-11-15)

Todo es inventado: los nombres de los grupos y de los complejos no corresponden a bandas, bloques ni personas reales. **El mapa se va a ampliar**, así que aparecerán más facciones (sobre todo rivales) en las zonas nuevas.

**Decisión del usuario:** Kessler Courts y Crestline son **sets aliados** de Ash Block; Larkin Street y Dunmore, no. Quedan dos bandos:
- **Bloque del jugador:** Ash Block, Kessler y Crestline.
- **Rivales:** Dunmore y Larkin.

| Facción | Barrio (coordenadas) | Carácter | Relación con el jugador |
|---|---|---|---|
| **Ash Block** (complejo "Ashgrove Homes") | Manzana central: torres T1–T3, adosadas, aparcamientos (x −100…100, z −70…70) | El bloque del jugador. Jóvenes y ruidosos, con la música como salida (estudio del sótano de T1) | Propia |
| **Kessler Courts** | Barrio de viviendas este (x 120–250, z −88…88): bloques bajos, 4 torres, cancha y plaza | Set aliado al otro lado de la vía. La avenida y la estación son paso seguro entre los dos bloques | **Aliado** |
| **Crestline** | Oeste lejano (x −300…−120, z 90–298): gasolinera, empeños, descampado, cancha | Los mayores, organizados y discretos; les importa el negocio. Dan trabajos y respaldo | **Aliado** |
| **Dunmore** | Barrio de casas oeste y ampliaciones oeste y norte (x −103…100, z 102–298): calles A', B', C', D, E y F | Vecinos del norte, pegados a la manzana central por la calle norte y la franja de negocios. Queda entre Ash Block y Crestline | **Enemigo** (el rival más cercano) |
| **Larkin Street** | Barrio de casas norte-este y su ampliación (x 120–250, z 90–298): calles A, B, C, G y Fe, y callejones | Casas bajas, porches traseros y callejones, pegado al norte de Kessler | **Enemigo** |

**Relaciones** (para el director de incursiones):
- Ash Block ↔ Dunmore: guerra abierta, la probabilidad de incursión más alta. Están a una calle (la norte, z = 80).
- Ash Block ↔ Larkin: hostil, media. Larkin tiene que cruzar la avenida o el barrio de Kessler.
- Kessler ↔ Larkin: hostiles. Comparten frontera en el callejón sur del barrio de casas (z ≈ 95, contra la valla de Kessler), así que Kessler recibe incursiones de Larkin y puede pedir ayuda al jugador.
- Crestline ↔ Dunmore: hostiles. Dunmore queda encerrado entre Crestline (oeste) y Ash Block (sur).
- Dunmore ↔ Larkin: propuesta, aliados entre sí (los dos enemigos del bloque). Se cruzan por las calles F/Fe y H, que atraviesan la avenida al norte.
- Ash Block, Kessler y Crestline: no se atacan; sus miembros se saludan y pueden ayudar si hay jaleo cerca.
- Las facciones de las zonas nuevas del mapa se añadirán aquí.

**Fronteras y zonas en disputa:**
- **Calle norte (z = 80) y franja de negocios** (z 90–102): la frontera con Dunmore. La tienda **FOOD & LIQUOR** de la esquina con la avenida es la esquina más valiosa del mapa, porque de noche es lo único abierto, y está en disputa con Dunmore.
- **Callejón sur del barrio de casas este** (z ≈ 95): la frontera Kessler–Larkin.
- **Cruces de F/Fe y H con la avenida** (z 248–298): por donde pasan Dunmore y Larkin de un lado a otro.
- **Avenida y estación:** zona segura entre Ash Block y Kessler (ya no es frontera).
- **Instituto** (x −275…−120, z −70…70): neutral, coinciden chavales de todos los barrios. A la salida, por la tarde, hay roces con los de Dunmore.
- **Cocheras** (al sur de z −80): tierra de nadie; escondite, atajo y sitio para pintar trenes, con grafitis de todos.
- **Sur** (two-flats de la calle sur y el suroeste vacío) y **las zonas nuevas**: libres para las facciones que vengan con la ampliación.

**Esquinas y lugares habituales** (los `ActivitySpot` con preferencia de cada grupo):
- **Ash Block:** zona de reunión (barbacoa y canasta), memorial, portal de T2, aparcamiento P2 y la puerta de la tienda de licores (de noche).
- **Kessler:** cancha (232, 58), plaza de los grafitis (232, −58), parada de bus de la entrada y pie de la escalera este de la estación.
- **Crestline:** gasolinera (FOOD MART), casa de empeños, bidón del descampado y cancha del oeste.
- **Dunmore:** barbería, la esquina de B' sin salida junto a los negocios, el solar de la franja de negocios y su tienda de esquina.
- **Larkin:** tienda de esquina (182, 190), quiebro de la calle C, porches traseros y el solar grande del este.

**Identificación visual:**
- Cada grupo lleva un color de acento en la ropa (`Clothing.cs`/`PlayerOutfit`), elegido para no copiar los de bandas reales: Ash Block gris y blanco, Kessler granate, Crestline negro, Dunmore mostaza, Larkin verde oscuro.
- También se distinguen por el estilo: Crestline va más arreglado y los jóvenes llevan sudadera y pasamontañas.
- No se usan señas de manos, símbolos ni nombres reales.

## 5. Fuentes
- [Neverness to Everness: comportamiento de los NPC (wiki)](https://thegameswiki.com/neverness-to-everness/wiki/npc-behavior)
- [Černý et al., *Smart Areas* (2014)](https://scitepress.org/PublishedPapers/2014/49211/pdf/index.html)
- [Using Behavior Objects to Manage Complexity in Virtual Worlds](https://ar5iv.labs.arxiv.org/html/1508.00377)
- [Tompson y Bowers, *Testing time-sensitive influences of weather on street robbery* (Crime Science, 2015)](https://discovery-pp.ucl.ac.uk/id/eprint/1468280/1/s40163-015-0022-9.pdf)
- [Sommer et al., calor, lluvia y violencia en Boston (2019)](https://pmc.ncbi.nlm.nih.gov/articles/PMC6663090)
- [Inviernos suaves y crimen (GeoHealth, 2018; resumen)](https://www.weathernationtv.com/news/study-shows-warmer-winter-temperatures-linked-to-increased-crime)
- [POP Center: mercados de droga al aire libre (Madensen y Mills)](https://popcenter.asu.edu/sites/default/files/madensen.mills_.drug_markets4.pdf)
- [Ratcliffe: estacionalidad y patrones temporales](https://www.jratcliffe.net/_files/ugd/f5df24_021111afef4642fb9b65a3f295ddacfd.pdf)
- [Evidence-Based Policing: Open-Air Drug Markets](https://www.evidence-basedpolicing.org/open-air-drug-markets/)
