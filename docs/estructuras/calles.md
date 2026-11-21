# Calles: nombres y detalle vial

La ciudad tiene que verse **llena de detalle de calle**, como una ciudad real: marcas viales, semáforos, cableado, alcantarillado, nombres de calle… (regla en [reglas.md](reglas.md), petición del 2026-11-16). Este archivo recoge qué hay y cómo se llaman las calles. El código está en [`Editor/Docs/detalle-vial.md`](../../Assets/_Project/Editor/Docs/detalle-vial.md).

## Nombres de calle

Todos **inventados**: no son calles reales de Chicago (tampoco las numeradas, como la 63rd, que identifican sitios reales). Siguen el formato de allí: `W` para las este–oeste y `S` para las norte–sur, con `ST`, `AVE` o `CT`. Algunas calles dan nombre a su facción, como pasa en Chicago con los sets: Larkin Street, Dunmore y Kessler Courts. La del sur lleva el nombre de Ashgrove Homes, el complejo del jugador.

| Calle (plano) | Nombre | Dónde | Facción |
|---|---|---|---|
| Calle norte, z = 80 | W BRAXTON ST | De x −300 a la avenida | Frontera Ash Block / Dunmore |
| Calle sur, z = −80 | W ASHGROVE ST | De x −300 a la avenida | Ash Block (el inicio está en su acera norte) |
| Avenida, x = 110 | S MARLOW AVE | Todo el mapa, bajo el tren | Paso seguro Ash Block / Kessler |
| Calle oeste, x = −110 | S CALDER AVE | Todo el mapa | Frontera con Crestline y el instituto |
| Calle del barrio de viviendas | W KESSLER CT | x 117–250, z = 0 | Kessler Courts |
| A | W LARKIN ST | Barrio de casas este | Larkin Street |
| B y G | S TENNEY AVE | Barrio de casas este, su ampliación norte y el barrio norte | Larkin |
| C (C1 y C2) | W HOLLIS ST | Barrio de casas este | Larkin |
| A' | W DUNMORE ST | Copia oeste | Dunmore |
| B' | S BRISCOE AVE | Copia oeste y su tramo hasta F | Dunmore |
| C' | W CROWELL ST | Copia oeste | Dunmore |
| D | W DORSEY ST | Ampliación oeste y oeste lejano | Dunmore / Crestline |
| E | W EMBRY ST | Ampliación oeste y oeste lejano | Dunmore / Crestline |
| F y Fe | W FENWICK ST | Cruza todo el norte (de x −252 a 207) | Dunmore / Larkin |
| H | W HARLAND ST | Borde norte del mapa | Dunmore / Larkin |
| J | S JUDSON AVE | Del muelle del sur a Prescott (oeste lejano, polígono y barrio norte) | Crestline |
| z = −319 | W GANTRY ST | Polígono del sur, de J al borde este | Libre |
| z = −475 | W DOCK ST | Polígono del sur, calle del muelle | Libre |
| Curva | S WHARF RD | Polígono del sur: de Gantry (x = −40) a la avenida (z = −410) | Libre |
| Bulevar, z = 395 | W KINGSLEY BLVD | Barrio norte, de borde a borde | Libre |
| Calle del borde norte, z = 491 | W PRESCOTT ST | Barrio norte, al pie del terraplén | Libre |
| x = −40 | S WEBLEY AVE | Barrio norte (centro) | Libre |
| Diagonal | S HALDANE AVE | Barrio norte: de la avenida (z ≈ 318) al bulevar | Libre |
| Calle sin salida | S MERRIN CT | Barrio norte (este), con rotonda | Libre |

Una calle que sigue al otro lado de un cruce conserva el nombre: F, Fe y la F del oeste lejano son la misma W FENWICK ST. Las calles nuevas se añaden a esta tabla y a `NamedStreets()`.

## Qué hay en las calles

- **Placas de nombre:** verdes con letras blancas y a doble cara, como en Chicago. Van en lo alto del poste del STOP, en cada boca de calle, y en dos esquinas de cada cruce con semáforos.
- **Semáforos** en los cuatro cruces de las calles grandes (x = ±110 con z = ±80). Hay un poste en cada esquina con un semáforo para cada dirección, que mira a los coches que llegan desde el otro lado, y semáforos de peatones (muñeco blanco y mano naranja). **Funcionan:** van de verde a ámbar, se quedan todo en rojo un momento y le dan paso a la otra dirección, y la mano naranja parpadea antes del cambio. Cada cruce va desfasado.
- **STOP:** donde una calle de barrio llega a otra calle, hay una señal octogonal en la esquina de la derecha (con trasera de aluminio gris) y una línea de detención blanca en el carril que llega.
- **Pasos de cebra** de barras anchas ("continental"), gastados donde pisan las ruedas. Van en la boca de cada calle de barrio que da a una calle grande y en las llegadas a los cruces de las calles grandes.
- **Flechas de carril** en las llegadas a los cruces de las calles grandes: giro a la izquierda en el carril de dentro y recto en el de fuera. En el cruce en T de la avenida, la flecha es de giro a la derecha.
- **Postes de madera con cables** cada ~34 m en la acera de fuera de las calles norte, sur y oeste (en la avenida está el tren). Llevan cruceta, tres cables de tendido, un cable bajo de teléfono con más comba y un transformador cada tres postes. Los cables pasan por encima de las bocas de calle y de los cruces.
- **Límites de velocidad** (SPEED LIMIT 30, el general de Chicago) cada ~120 m en las dos aceras de las calles grandes, mirando a los coches de ese lado.
- **Alcantarillado:** tapas de alcantarilla en los carriles y sumideros junto a los dos bordillos, en las calles grandes y en todas las de barrio.
- **Bocas de incendio** amarillas en la mitad de las esquinas de las bocas de calle.
- Nada se pone encima de otra cosa: cada poste o señal comprueba que cabe (farolas, árboles, vallas, coches) y, si no, se corre un poco o no se pone.

## Barrio norte: tipos de calle nuevos (2026-11-16)

- **Bulevar** (W KINGSLEY BLVD): dos calzadas de un sentido de 8 m (discontinua blanca, sin doble amarilla) y mediana de 6 m con árboles y farolas dobles, abierta en los cruces. Semáforos en los cruces con las calles grandes; en los demás, el bulevar tiene preferencia y llevan STOP las calles que llegan a él.
- **Diagonal** (S HALDANE AVE) a 45°: STOP con placas en sus dos extremos (`BuildNorthStreetDetail`).
- **Calle sin salida con rotonda** (S MERRIN CT).
- **Calles cortadas** con barreras de hormigón y ROAD CLOSED donde llegan al borde este del mundo.
- **Pasos inferiores** de las calles grandes bajo el terraplén del borde norte.

## Polígono del sur: calle curva y pasos a nivel (2026-11-17)

- **Calle curva** (S WHARF RD): tramo recto, cuarto de círculo de 45 m de radio en el eje y otro tramo recto. La curva se dibuja con `ArcStrip` (SouthBuildings.cs) y no entra en `NamedStreets` (solo los tramos rectos); lleva CURVE 15 MPH en las dos entradas.
- **Pasos a nivel** de la vía de mercancías en J y en las calles grandes: cruz de San Andrés con dos luces rojas, barrera levantada y línea de detención a cada lado (`BuildSouthStreetDetail`).
- Las calles que llegan al borde este (Gantry, Dock) se cortan con barreras y ROAD CLOSED, como en el norte. J y las calles grandes acaban en el muelle del canal.

## Tráfico (2026-11-17)

- Los coches circulan por **todas las calles de `NamedStreets()`** (y por la curva de S WHARF RD), por la derecha. Paran en rojo en los cuatro cruces con semáforo; en los demás, la calle grande tiene preferencia, en una T para quien llega por el pie y en los cruces de dos calles de barrio hay STOP en todas.
- No entran en calles sin salida ni cortadas (ROAD CLOSED, puentes del canal). Por el oeste (puentes sobre la autopista) y por el norte (pasos inferiores) salen y entran del mundo.
- **Autobús 52** (línea inventada): vuelta a la manzana central por W ASHGROVE ST, S MARLOW AVE, W BRAXTON ST y S CALDER AVE, con cuatro paradas (poste azul `BUS 52` y banco).
- **Pasos a nivel** de la vía de mercancías: se cierran de verdad cuando pasa el tren (luces, campana y barreras) y los coches esperan.
