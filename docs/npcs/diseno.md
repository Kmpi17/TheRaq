# 3. Cómo se haría

Propuesta técnica, siguiendo el estilo del proyecto: la lógica pura va aparte y se testea en EditMode (como `DayCycleMath` y `WeatherMath`), y lo que se coloca en el mundo lo pone el generador de la escena.

## Población: cuántos y de qué tipo
- **Tabla de población:** para cada tipo de zona (torres, casas, negocios, avenida, parque) y franja horaria (mañana, tarde, noche, madrugada) da cuántos NPC de cada tipo hay. Tipos: civil paseando, civil con recado, vecino en su porche, pandillero pasando el rato, pandillero vendiendo, cliente.
- **Factor de tiempo:** se aplica con `WeatherSystem.Rain` y `Snow`. Cada tipo lleva su propio factor (paseantes ×0.2 con lluvia fuerte; los que venden, ×0.8 y a cubierto).
- **Transición suave** entre franjas, usando `DayCycleMath.Daylight`, y al cambiar el tiempo, usando los 40 s de `WeatherMath.Approach`, para que nadie aparezca ni desaparezca de golpe.
- **Calendario opcional:** día de la semana y día del mes, para los picos de los días de cobro y el fin de semana.

## Aparición alrededor del jugador
- Los NPC aparecen en un anillo de ~40–120 m alrededor del jugador, **fuera de la vista** (detrás de la cámara, tras una esquina o dentro de la niebla), y se retiran cuando quedan lejos y no se ven.
- Hay un tope de NPC activos (por ejemplo 30–40). Se reutilizan los objetos (pool) en vez de crearlos y destruirlos.
- **LOD de la IA:** los cercanos piensan cada fotograma; los lejanos, cada pocos segundos y sin animación fina.
- Los grupos de las esquinas habituales existen aunque el jugador no esté cerca, aunque sea solo como dato: al acercarse, siguen ahí. Así el barrio tiene continuidad.

## Sitios y actividades: objetos y zonas inteligentes
- Un componente, por ejemplo `ActivitySpot`, en esquinas, bancos, porches, la barbacoa, la canasta, la puerta de la tienda de licores, los portales y el memorial. Anuncia:
  - qué actividades ofrece: sentarse, pasar el rato, vender, refugiarse, hacer cola, jugar, recordar en el memorial;
  - para qué tipos de NPC;
  - en qué horario;
  - cuántas plazas tiene;
  - si es **a cubierto** (para la lluvia y la nieve).
- El NPC no sabe hacer cada cosa: va al sitio y el sitio le dice dónde ponerse y qué animación toca.
- **Zonas inteligentes** para grupos: una esquina admite de 3 a 5 personas y reparte los papeles. Uno vigila la calle, otros hablan apoyados en la valla y alguien entra y sale de la tienda.

## Elegir qué hacer: utilidad con algo de azar
- Cada NPC puntúa las actividades disponibles según:
  - su tipo y sus necesidades (aburrimiento, dinero, frío, cansancio);
  - la hora y el tiempo (con lluvia, lo que está a cubierto sube mucho);
  - la distancia;
  - lo llenos que estén los sitios.
- Elige entre las mejores con un poco de azar, para que no se vea mecánico.
- **Esquinas habituales con preferencia, no exclusivas:** cada grupo tiene sus sitios con mucho peso y otros secundarios con menos. Por eso "suelen estar ahí", pero a veces aparecen en otra parte.
- Hay pequeñas rutinas que dan personalidad: ir a la tienda y volver, saludar a otro del grupo, cambiar de esquina al anochecer, recogerse en el portal cuando empieza a llover.

## Incursiones de bloques rivales: un "director"
- Un director del barrio tira cada pocos minutos la probabilidad de una incursión. Es **baja de día y alta de noche**, algo menor con lluvia o nieve y mayor si hace poco hubo un conflicto (rivalidad que se acumula).
- Es lógica pura con su test, como `WeatherMath`.
- Si sale una incursión:
  - un grupo rival aparece fuera de la vista y va hacia una esquina del bloque;
  - los civiles que queden huyen;
  - los pandilleros del bloque reaccionan;
  - el HUD puede avisar con poco, por ejemplo un coche que pasa despacio.
- Entre incursiones hay un tiempo mínimo, para que no se encadenen.

## Tiendas
- Cada negocio tiene horario. De día abre; de noche cierra con la persiana bajada y el letrero apagado.
- **La tienda de licores no cierra.** De noche es el punto de reunión principal y el refugio de la lluvia.
- Cuando se programe, el horario se puede enganchar a `NightLight`/`WeatherSystem` igual que las farolas.

## Movimiento y reacciones
- **NavMesh** para andar por aceras, cruces y callejones. El paquete `com.unity.ai.navigation` ya está en el proyecto, y la NavMesh se generaría al construir la escena.
- Los civiles cruzan por los pasos y evitan las zonas oscuras de noche.
- **Reacciones graduadas:**
  - Un empujón: miran o protestan.
  - Un arma a la vista: los civiles se apartan.
  - Disparos: los civiles huyen y los pandilleros se cubren o responden.
- Se recuerda lo que pasó durante un rato: la esquina se queda vacía después de un tiroteo.

## Orden propuesto para programarlo
*(Hecho del 1 al 6 en la primera fase; ver el estado arriba.)*
1. La lógica pura: tabla de población, factores de hora y tiempo, puntuación de utilidad y el director de incursiones, con sus tests.
2. `ActivitySpot` y su colocación en el generador (esquinas, porches, tienda de licores, zona de reunión, memorial).
3. Un NPC básico: modelo de piezas como el jugador, NavMesh, ir a un sitio y hacer la actividad.
4. La aparición y retirada alrededor del jugador, el pool y el LOD de la IA.
5. Los grupos en esquinas, las incursiones y las reacciones a la violencia.
6. Los horarios de las tiendas.
