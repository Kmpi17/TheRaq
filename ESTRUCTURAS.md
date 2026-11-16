# Estructuras y mundo

Cuaderno del mundo del juego: estilo, zonas, edificios, props y las peticiones o comentarios sobre cómo debe sentirse cada sitio. Dice **qué** hay en el mundo y **por qué**; cómo se programa está en [Editor/README](Assets/_Project/Editor/README.md) y sus [Docs](Assets/_Project/Editor/Docs/).

Este archivo es solo el **índice**. El contenido está separado por tema en [`docs/estructuras/`](docs/estructuras/) para que cada consulta lea solo lo que necesita. Lo nuevo se escribe en el archivo del tema, no aquí.

## Qué leer

| Archivo | Qué tiene | Cuándo leerlo |
|---|---|---|
| [reglas.md](docs/estructuras/reglas.md) | Reglas generales de estilo, diseño y forma de trabajar | **Siempre**, antes de tocar o hablar de cualquier estructura |
| [mapa.md](docs/estructuras/mapa.md) | Esquema del mundo con coordenadas de cada zona (el detalle, en `mapa/mapa.png`) | Para ubicar un cambio |
| [referencias.md](docs/estructuras/referencias.md) | Tabla de imágenes de `img/estructures/` y qué se toma de cada una | Al mirar las referencias o añadir una imagen |
| [calles.md](docs/estructuras/calles.md) | Nombres de las calles y detalle vial (semáforos, STOP, pasos de cebra, cables, alcantarillado) | Al crear o cambiar una calle, y al diseñar una zona nueva |
| [catalogo.md](docs/estructuras/catalogo.md) | Catálogo de estructuras con medidas | Al crear o cambiar un tipo de edificio o prop |
| [peticiones.md](docs/estructuras/peticiones.md) | Registro con fecha de lo pedido sobre el mundo | Al anotar una petición nueva; para no repetir errores |
| [pendiente.md](docs/estructuras/pendiente.md) | Pendiente e ideas | Al planear trabajo nuevo |

## Zonas (`docs/estructuras/zonas/`)

Una ficha por zona. Al lado, el archivo de `Assets/_Project/Editor/Docs/` que explica cómo se genera.

| Zona | Ficha | Código |
|---|---|---|
| Avenida principal y tren elevado (x = 110) | [avenida.md](docs/estructuras/zonas/avenida.md) | [tren.md](Assets/_Project/Editor/Docs/tren.md), [calles.md](Assets/_Project/Editor/Docs/calles.md) |
| Manzana central: torres, adosadas, aparcamientos | [manzana-central.md](docs/estructuras/zonas/manzana-central.md) | [torres.md](Assets/_Project/Editor/Docs/torres.md), [estudio.md](Assets/_Project/Editor/Docs/estudio.md) |
| Cocheras del tren elevado | [cocheras.md](docs/estructuras/zonas/cocheras.md) | [cocheras.md](Assets/_Project/Editor/Docs/cocheras.md) |
| Alrededores (two-flats, tienda de esquina) | [alrededores.md](docs/estructuras/zonas/alrededores.md) | [Editor/README](Assets/_Project/Editor/README.md) (`BuildSurroundings`) |
| Barrio de viviendas (este) | [viviendas.md](docs/estructuras/zonas/viviendas.md) | [props-calle.md](Assets/_Project/Editor/Docs/props-calle.md), [vegetacion.md](Assets/_Project/Editor/Docs/vegetacion.md) |
| Barrio de casas (norte) | [casas.md](docs/estructuras/zonas/casas.md) | [casas.md](Assets/_Project/Editor/Docs/casas.md) |
| Barrio de casas oeste y franja de negocios | [casas-oeste-negocios.md](docs/estructuras/zonas/casas-oeste-negocios.md) | [casas.md](Assets/_Project/Editor/Docs/casas.md), [negocios.md](Assets/_Project/Editor/Docs/negocios.md) |
| Ampliación norte de los barrios de casas | [ampliacion-norte.md](docs/estructuras/zonas/ampliacion-norte.md) | [casas.md](Assets/_Project/Editor/Docs/casas.md) |
| Ampliación oeste del barrio de casas | [ampliacion-oeste.md](docs/estructuras/zonas/ampliacion-oeste.md) | [casas.md](Assets/_Project/Editor/Docs/casas.md) |
| Oeste lejano | [oeste-lejano.md](docs/estructuras/zonas/oeste-lejano.md) | [oeste-lejano.md](Assets/_Project/Editor/Docs/oeste-lejano.md) |
| Autopista hundida | [autopista.md](docs/estructuras/zonas/autopista.md) | [autopista.md](Assets/_Project/Editor/Docs/autopista.md) |
| Instituto | [instituto.md](docs/estructuras/zonas/instituto.md) | [instituto.md](Assets/_Project/Editor/Docs/instituto.md) |
| Barrio norte: bulevar, diagonal, mini centro comercial, parque, terraplén (z 298…500) | [barrio-norte.md](docs/estructuras/zonas/barrio-norte.md) | [norte.md](Assets/_Project/Editor/Docs/norte.md) |

Gente y facciones de cada zona: [NPCS.md](NPCS.md).

## Cómo se actualiza

- Petición o comentario sobre el mundo → una línea con fecha en `peticiones.md` y el cambio en la ficha de la zona (o en `reglas.md` si es una regla general).
- Zona nueva → ficha nueva en `zonas/`, fila en la tabla de arriba, su sitio en `mapa.md` y el plano regenerado.
- Imagen de referencia nueva → fila en `referencias.md`.
