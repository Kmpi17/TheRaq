# NPC: diseño del comportamiento

Cuaderno de diseño de los personajes no jugadores: cómo deben comportarse, por qué y cómo se hace. El código está en [`Scripts/NPC/`](Assets/_Project/Scripts/NPC/README.md) (detalle, números y limitaciones en su README).

**Estado (2026-11-15): implementado por fases** en [`Scripts/NPC/`](Assets/_Project/Scripts/NPC/README.md). Ya funcionan la población por hora y tiempo, los sitios habituales y de paso (`NpcSpot`, el `ActivitySpot` de abajo), las tiendas que cierran, los territorios, la reacción de enemigos (mirar → encarar → disparar) y aliados (defienden su zona), los civiles que huyen y las incursiones nocturnas. Falta: coches con NPC, policía, misiones de facción y la ropa como disfraz. Se lee antes de tocar cualquier NPC. Depende del sistema de hora y tiempo (`WeatherSystem`, `DayCycleMath`, `WeatherMath`; ver [World/README](Assets/_Project/Scripts/World/README.md)).

Todo es ficticio: grupos, bloques, personajes y lugares inventados, sin nombres de bandas reales ni parecidos con personas reales (ver `CLAUDE.md`).

Este archivo es solo el **índice**; el contenido está separado por tema en [`docs/npcs/`](docs/npcs/). Lo nuevo se escribe en el archivo del tema.

| Archivo | Qué tiene |
|---|---|
| [peticiones.md](docs/npcs/peticiones.md) | Lo que pide el usuario sobre los NPC (con fecha). Se añade aquí cada petición nueva |
| [investigacion.md](docs/npcs/investigacion.md) | Qué dice la realidad (comportamiento, mercados al aire libre…) y fuentes |
| [diseno.md](docs/npcs/diseno.md) | Cómo se hace: población, aparición, sitios, utilidad, incursiones, tiendas, reacciones y orden de trabajo |
| [facciones.md](docs/npcs/facciones.md) | Facciones, territorios, alianzas, colores y esquinas habituales |
