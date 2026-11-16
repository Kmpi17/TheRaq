# Pendiente e ideas

- NPC ([NPCS.md](../../NPCS.md)): hecho lo básico (sitios y horario de tiendas). Faltan sitios en los porches traseros y más sitios a cubierto para la lluvia (marquesinas, portales de Kessler), y pasos de cebra en la malla para que los civiles crucen por ellos.
- Más detalle en las calzadas de los barrios de casas: ya tienen alcantarillas, sumideros, STOP y pasos de cebra en las bocas (2026-11-16), pero siguen con la textura `Road` sencilla, sin parches ni grietas selladas como las calles grandes.
- Detalle vial ([calles.md](calles.md)): tráfico que respete los semáforos y NPC que crucen con el muñeco; postes con cables también en las calles de barrio que no tienen callejón; señales de aparcamiento (NO PARKING / TOW ZONE), de dirección única y de zona escolar junto al instituto; cables que crucen la calle hasta las casas.

- Pasar los two-flats de los alrededores al mismo nivel de texturas y detalle que el barrio de viviendas.
- Tejados a cuatro aguas en los bloques bajos, como en `oblock.png`.
- Balcones de hormigón en las fachadas (`gang.png`).
- Casa del jugador: comprar ropa en tiendas para el armario, reaparecer en casa al morir, más muebles que se usen (tele, cama para pasar la noche) y mejorar el piso con la fama.
- Calles curvas.
- Autobús urbano circulando (`bus.png`).
- Llevar la vegetación de invierno (árboles variados, nieve y maleza) a los two-flats.
- Traseras de los bloques del barrio de viviendas con porches de madera como los del barrio de casas (`apartaments.jfif`).
- Torre con la esquina en chaflán (`projects.png`).
- Locales en la acera oeste de la avenida principal, bajo el tren (tienda de licores, comida rápida, peluquería; letreros sin marcas reales).
- Coches: más modelos (pickup, coche de policía genérico), cristales rotos y tráfico que circule.
- Sonido del tren al pasar y chispas en los raíles.
- Autopista: rampas de entrada y salida para poder circular, y coches que frenen y cambien de carril.
- Instituto: luces reales en las torres de focos de noche, interior del gimnasio, y peatones (alumnos) a la salida.
- Estudio: sonido del beat al grabar, mejoras compradas con la fama (micro, monitores) y misiones del rango siguiente.
- Personajes (NPC) en la zona de reunión y en el memorial.
- Rellenar el suroeste vacío (al sur de la calle z = −80, entre la autopista y los two-flats).
- Cocheras: algún tren que entre o salga por la rampa, vigilante (NPC) en la caseta y misiones dentro (robar piezas, pintar un vagón).
- Manzana central: planta baja abierta con pilares en alguna torre, galerías exteriores y coches que entren y salgan de los aparcamientos.

## Profundidad y zonas nuevas (propuesta del 2026-11-16, sin elegir)
- **Huecos del plano:** suroeste (x −275…−120, z −90…−300) → zona industrial abandonada (naves, chatarrería, chimenea); sureste (x 120…300, z −90…−300) → parque grande con lago helado y caseta, o centro comercial pequeño con aparcamiento; borde este (x 250…300) → terraplén del tren de mercancías con puentes sobre las calles, que cierra el mapa.
- **Hitos que se ven de lejos** (uno por zona, para orientarse y dar escala): depósito de agua sobre un tejado, campanario de iglesia, chimenea de fábrica, vallas publicitarias junto a la autopista, antena de telefonía.
- **Fondo fuera del mundo** (x/z más allá de ±300, sin colliders ni scripts): silueta de rascacielos inventados al norte (de noche, ventanas encendidas), tejados bajos en cajas sin detalle y calles que siguen rectas con farolas hasta perderse en la niebla (que acaba a 260 m de día).
- **Sin sobrecargar:** densidad desigual (zonas llenas y respiros: solares, parques, aparcamientos), detalle pequeño solo donde se anda, LOD e impostores para lo lejano, y lo de fondo como estático sin luces reales.

## Ampliación a 1 km × 1 km (pedida el 2026-11-16, reparto propuesto)
- **Sur** (z −300…−500, y los huecos del suroeste): zona industrial con naves, chatarrería y chimenea, una vía de mercancías en terraplén (E–O) con ramales a las fábricas y enlace con las cocheras, y una estación nueva del elevado.
- **Este** (x 300…500): parque grande con lago helado; centro comercial con aparcamiento en el hueco del sureste, junto a la avenida.
- **Oeste** (x −500…−300, al otro lado de la autopista, por los puentes de z ±80) y **norte** (z 300…500): barrios nuevos de casas y bloques, con un parque de barrio.
- Fases: 1) infraestructura (límite del mundo en una constante, suelo, avenidas y elevado hasta el borde, autopista, plano); 2) sur industrial; 3) este; 4) norte y oeste.
- **Hecho (2026-11-16): el norte** (`barrio-norte.md`), hasta `WorldNorthZ` = 500. Los otros bordes siguen en ±300: el sur, el este y el oeste quedan pendientes. El terraplén del norte puede ser la misma vía de mercancías que baje por el este hasta la zona industrial del sur.

## Barrio norte: lo que queda (2026-11-16)
- Probar en el editor: abrir Unity, *GTAChiraq > Construir escena prototipo* y pasear por el norte (pasos inferiores, rotonda, aparcamiento del centro comercial, NPC en los sitios de paso nuevos).
- Opcional: facción para el barrio norte (ahora libre) y sus `HabitualSpots`.
