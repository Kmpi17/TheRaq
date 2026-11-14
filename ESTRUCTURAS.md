# Estructuras y mundo

Cuaderno del mundo del juego. Aquí se anota **todo lo que se decide sobre el mundo**: estilo, zonas, edificios, props y las peticiones o comentarios sobre cómo debe sentirse cada sitio. Se consulta antes de construir algo nuevo y se actualiza cada vez que se decide o cambia algo.

- Cómo está hecho cada cosa en código: [Editor/README](Assets/_Project/Editor/README.md).
- Este archivo dice **qué** hay en el mundo y **por qué**, no cómo se programa.

## Reglas generales

- **Época y lugar:** Southside de Chicago, hacia 2012, escena drill. Invierno: cielo gris, niebla, nieve sucia, farolas de sodio naranjas.
- **Aspecto:** low-poly con aire de PS2. Las texturas se generan en código (`ProceduralTextures`), son de baja resolución y van algo gastadas.
- **Todo es ficticio.** No se usan nombres de bandas, raperos ni personas reales, ni calles o bloques reales que identifiquen hechos reales. Los grafitis son garabatos abstractos y los letreros no llevan logotipos reales.
- **No se copia nada de GTA** (ni assets ni mapas). Las capturas de `img/` sirven solo como referencia de ambiente.
- **Invierno, pero no muerto:** la vegetación mezcla árboles pelados, con hoja verde, nevados y con hoja seca de invierno. Quedan restos de nieve en las zonas de sombra y en los bordillos, y hay maleza alta en las zonas descuidadas.
- **Barrios de casas, no cuadrículas:** las zonas de casas bajas se trazan con calles irregulares: calles sin salida, quiebros, pasajes y una red de callejones traseros (vallas de madera, garajes, cubos, maleza, postes con cables). Las parcelas varían de ancho y retranqueo, y hay solares vacíos y alguna casa tapiada.
- **Armonía entre zonas vecinas:** una zona nueva usa los mismos materiales que la de al lado (ladrillo con ventanas, acera de losas, césped seco, valla baja de hierro, farolas de sodio), aunque sus edificios sean de otro tipo. Las alturas cambian poco a poco entre zonas.
- **Coches de la época, sin marcas:** modelos genéricos de finales de los 90 y los 2000 (sedán grande, coupé, todoterreno, furgoneta), con matrículas en blanco y sin logotipos. Muchos llevan llantas cromadas. Los aparcados en invierno tienen a veces nieve encima, óxido, una puerta de otro color o una ventana tapada con plástico.
- **Calles de negocios con aire:** los locales (barbería, tienda de comida y licores, lavandería, pollo frito, cambio de cheques, iglesia de local…) van **sueltos**, de 1–2 plantas, con huecos, solares o un aparcamiento pequeño entre ellos; nunca una fila continua de fachadas pegadas. Los letreros dicen lo que se vende con palabras genéricas (BARBER SHOP, FOOD & LIQUOR), sin nombres propios ni marcas.
- **Ambiente callejero:** las zonas de barrio tienen que sentirse vividas y descuidadas, con basura, manchas, grafitis, vallas, muebles tirados y cables. Ninguna zona transitable debe quedar vacía.
- **Cómo se trabaja este archivo:** antes de hablar o tocar cualquier estructura se leen este archivo y los README relacionados (sobre todo `Assets/_Project/Editor/README.md`) y se miran el plano `mapa/mapa.png` y las imágenes de `img/estructures/` (y `img/`). Cada imagen nueva se añade a la tabla de *Referencias visuales*. Toda regla nueva que salga se apunta en esta sección.

## Referencias visuales (`img/estructures/`)

Las imágenes están en `img/estructures/`. `gang.png` sigue en `img/`.

| Imagen | Qué se toma de ella |
|---|---|
| `oblock.png` | Torres de ladrillo rojo de 8 plantas, bloques bajos de 3 plantas con tejado gris, calle curva con doble línea amarilla, árboles pelados, vallas de hierro negras y cables de luz. |
| `bus.png` | Acera ancha de hormigón agrietado, parada de bus con marquesina, autobús urbano, postes de madera con cables, pilonas rojas y casas de ladrillo de 2 plantas con ventanas blancas. |
| `gang.png` | Fachada de ladrillo de cerca: ventanas con marco blanco y alféizar, balcones de hormigón y césped gastado. |
| `apartaments.jfif` | Parte trasera de bloques de ladrillo de 3 plantas: porches y escaleras de madera exteriores en zigzag, poste de luz con muchos cables, contenedores verdes y cubos azules, valla de madera, coche aparcado en un solar de tierra y hierba seca con basura. |
| `houses.png` | Calle de casas bajas: casitas de ladrillo de 1–2 plantas con tejado a dos aguas mirando a la calle, greystones de piedra gris con escalinata y barandilla, vallas bajas, árboles pelados grandes y niebla. |
| `apartaments2.jpg` | Bloque de apartamentos de 3 pisos de ladrillo oscuro: miradores de arriba abajo, cornisas de piedra, sótano con ventanas de bloque de vidrio, portal de piedra, vallas bajas de hierro y césped seco con basura. |
| `backparts.png` | Callejón trasero de noche: asfalto roto, vallas de tablones altas, garajes y cobertizos, cubos de basura, maleza junto a las vallas y postes con cables. |
| `block2.png` | Calle agrietada con montones de nieve en la acera, bloques de 3 pisos de ladrillo claro, valla de malla, contenedor y árboles pelados enormes. |
| `area1.webp` | Vista aérea de un barrio real: manzanas irregulares que mezclan casas y bloques, muchos solares vacíos, aparcamientos y árboles. Se toma solo la idea de trama desordenada. |
| `projects.png` | Torre alta de ladrillo con la esquina en chaflán y la planta baja abierta (idea para futuras torres). |
| `trainavenue.avif` | Tren elevado (tipo "L") sobre la calle: pórticos de acero con riostras en X, tablero con traviesas y raíles, pasarela con barandilla, vagones de acero plateado; debajo, calle con coches aparcados, acera con valla y césped. |
| `thetowers1.webp`, `thetowers2.jfif`, `thetowers3.webp` | Torres altas de ladrillo detrás de filas de casas adosadas de 2 plantas, con tejado gris, bajantes y puertas de colores tapiadas; césped gastado con caminos de tierra, tendederos, un sofá naranja tirado y canasta suelta. Solo se toman los edificios y el ambiente, no las personas. |

## Mapa (metros, Y arriba)

**Plano detallado: [`mapa/mapa.png`](mapa/mapa.png).** Es la vista cenital a escala, con cuadrícula de 20 m, coordenadas y leyenda, y la referencia para ir modificando el mapa. Se genera con `python mapa/generar_mapa.py` y se regenera cada vez que cambia la disposición del mundo. Lo que sale al azar (árboles del césped, solares, basura) no aparece con su posición exacta. Abajo, el esquema rápido:

```
                z+  ┌────────────────────┐ ┌────────────────────┐
                    │ Barrio casas oeste │ │  Barrio de casas   │
                    │ x∈[-30,100]        │ │  x∈[120,250]       │
                    │ z∈[102,226]        │ │  z∈[90,214]        │
                    ├────────────────────┤ └────────────────────┘
  two-flats z=±99   │ Negocios z∈[90,102]│
  (norte: x ≤ −44)  └────────────────────┘
   ┌──────────────────────┐ avenida ┌────────────────────┐
   │ Manzana central      │ x=110   │ Barrio de viviendas│
   │ (torres + adosadas)  │         │                    │
   │  x∈[-100,100]        │         │  x∈[120,250]       │
   │  z∈[-70,70]          │         │  z∈[-88,88]        │
   └──────────────────────┘         └────────────────────┘
                z−
```

### Avenida principal y tren elevado (x = 110)
La avenida que separa la manzana central de los barrios es la avenida principal. Recorre todo el mapa de norte a sur y por encima va el **tren elevado** (`trainavenue.avif`), que es decorativo y no se puede usar.

- **Estructura:** pórticos de acero gris verdoso (alguno oxidado) cada 15 m, apoyados en las aceras de los dos lados para que la calzada quede libre debajo (6.6 m de gálibo).
  - Donde cruza una calle no hay pórtico: z = ±80, la calle del barrio de viviendas y las calles A y C de los dos barrios de casas.
  - Al norte de la calle norte hay acera en los dos lados de la avenida (la del oeste, x 100–103, se puso con el barrio de casas oeste).
- **Tablero:** vigas de acero, traviesas de madera con huecos (se ve el cielo a través de ellas), dos vías, pasarelas de rejilla y barandillas.
- **Estación** entre z = 4 y z = 68, junto a la entrada del barrio de viviendas:
  - un andén a cada lado, con borde amarillo, marquesina, fluorescentes, bancos, cartel sin texto y papelera;
  - una escalera de acero que baja a cada acera.
- **Trenes:** dos trenes de 4 vagones de acero plateado con franja granate y sin logotipos.
  - Uno va hacia el norte por la vía este y el otro hacia el sur por la vía oeste.
  - Cada uno se para unos 10 s en la estación, sigue hasta perderse en la niebla y vuelve a pasar al rato.

### Manzana central: conjunto de torres (x −100–100, z −70–70)
Conjunto de vivienda pública al estilo de `thetowers1-3`: torres altas detrás de filas de casas adosadas, con patios de césped gastado. Usa los mismos materiales que el barrio de viviendas de al lado (ladrillo con ventanas, acera de losas, césped seco, valla de hierro y farolas), para que las dos zonas casen.

- **Torres** de ladrillo de 27×15 m, con cantos de forjado de hormigón vistos en cada planta, pilastras en las esquinas, cornisa, caseta de ascensor, depósito y antena:
  - **T1** (−62, 40): 12 plantas, con el portal al norte.
  - **T2** (0, 2): 14 plantas, con el portal al sur, hacia el eje y el aparcamiento P2. Delante hay una cámara de vigilancia.
  - **T3** (65, 10): 11 plantas, con el portal al este, hacia la avenida y la escalera de la estación.
  - En todas: balcones apilados en la fachada trasera, cerrados con malla metálica y con ropa tendida en algunos; portal con marquesina y luz; ventanas tapiadas (muchas en la planta baja); antenas parabólicas; grafitis abajo; contenedores en una losa al lado; maleza al pie y nieve a la sombra.
- **Casas adosadas** de 2 plantas (6 m) y unos 6.5 m por casa, con tejado gris a dos aguas. El ladrillo cambia de tono por parejas y los portales van de dos en dos. Cada casa tiene puerta de color (o tapiada con contrachapado), tejadillo, escalón, camino propio, bajante, a veces aire acondicionado o parabólica, y detrás patio de hormigón y cubo de basura. Hay 5 filas:
  - dos al oeste (z 8–17 y z −31…−22), que se miran a través del eje, con valla baja propia;
  - dos en la calle norte (x −17…27 y 42…82);
  - una mirando a la avenida (x 81–90, z −47…−9).
- **Aparcamientos** en los huecos, con entrada desde la calle sur:
  - **P1** (x −92…−50) y **P3** (x 36…76), de dos filas;
  - **P2** (x −22…22), delante de T2, con fila doble y un paso de peatones pintado hasta el portal.
  - Tienen plazas pintadas de 2.6×5 m, topes de hormigón y coches en más o menos la mitad de las plazas. Alguna plaza está ocupada por la nieve que apartó la quitanieves. Hay manchas de aceite, charcos y dos farolas en el borde norte.
- **Eje peatonal E–O** (z = −7) que cruza toda la manzana, con farolas de parque y bancos con papelera cada 10 m donde hay sitio. Otros caminos van desde cada torre a la acera.
- **Patios:** losa con una canasta portátil, tendederos con ropa, un sofá naranja tirado, atajos de barro por el césped, árboles variados, placas y montones de nieve, bolsas y basura.
- **Valla baja de hierro** alrededor de la manzana, abierta donde llegan los caminos y en las entradas de los aparcamientos.
- Acera de 3 m y calzada de 14 m alrededor. Calles E–O en z = ±80 y N–S en x = ±110.

### Alrededores
- Filas de casas de 2 plantas (two-flats) al otro lado de las calles norte y sur. Al norte solo quedan al oeste de x = −40; de ahí a la avenida están los negocios y el barrio de casas oeste, y al este de la avenida el barrio de casas.
- Tienda de esquina con letrero azul y coches aparcados.

### Barrio de viviendas (este, x 120–250)
Es la primera zona con texturas y detalle, y la que marca el listón para el resto.

- **Calle** E–O de 12 m en z = 0. Sale de la avenida este y acaba en una fila de pilonas rojas. Lleva doble línea amarilla, bordes blancos, grietas, charcos, manchas de aceite y alcantarillas.
- **Aceras** de losas de hormigón de 3 m, con manchas y basura en la cuneta.
- **Edificios:**
  - 6 bloques bajos de 3 plantas (24×9×12 m) en z = ±23, con tejado plano de alero.
  - 4 torres de 8 plantas (30×24×15 m) en z = ±62, con cornisa y caseta de ascensor.
  - Todos son de ladrillo rojo con ventanas de marco blanco (algunas fachadas con aire acondicionado) y tienen portal con marquesina.
- **Accesos a las torres:**
  - Al empezar el camino: pilonas de hormigón y un tablón de anuncios.
  - A lo largo: barandilla baja a los lados, farolas de parque y bancos a mitad de camino.
  - Delante del portal, una explanada con jardineras, bancos, aparcabicis, papelera y una cámara de vigilancia con luz azul.
- **Cancha de baloncesto** (232, 58): pista con líneas gastadas, canastas con red de cadenas y malla metálica con puerta hacia la calle.
- **Plaza** (232, −58): muro con grafitis, bancos y mesa de picnic.
- **Otros:**
  - Vallas de hierro entre la acera y el césped, parada de bus a la entrada y postes de madera con cables (y unas zapatillas colgadas).
  - Contenedores con bolsas, un sofá y un colchón, y un coche oxidado sobre bloques.
  - Arbustos, calvas de barro y atajos donde la gente cruza el césped.
- **Vegetación de invierno:**
  - **Árboles variados:** unos pelados, otros con hoja verde, otros con la copa nevada (y nieve debajo) y otros con la hoja seca marrón y ocre que aguanta en invierno.
  - **Restos de nieve sucia:** montones en el bordillo, un montón grande contra las pilonas del final de la calle, placas sueltas en el césped, nieve a la sombra detrás de los edificios y ventisqueros junto a la valla.
  - **Maleza alta de zona abandonada:** en la franja del fondo del barrio, al final de la calle, alrededor del coche abandonado y en 3 solares descuidados. Hay matojos más bajos al pie de la valla y de las fachadas traseras, y hierbajos en las grietas de la acera.

### Barrio de casas (norte, x 120–250, z 90–214)
Casas bajas y apartamentos de 3 pisos, con calles que no forman cuadrícula y muchas partes traseras. Se entra desde la avenida por las calles A y C.

- **Calles** de 10 m con aceras de 2.5 m:
  - **A** (z = 122): entra desde la avenida y acaba en la calle B.
  - **B** (x = 212): va de norte a sur y es **sin salida por los dos extremos**, con barrera de hormigón, pilonas y nieve apilada.
  - **C**: entra desde la avenida en z = 188 y hace un **quiebro** de 10 m hacia el sur (en x ≈ 168) antes de llegar a B en z = 176.
- **Callejones traseros** de 5 m, de asfalto roto con grava y barro:
  - el del sur (z ≈ 95), pegado a la valla del barrio de viviendas;
  - el del centro (z ≈ 150);
  - un pasaje norte–sur hasta la calle C;
  - el del norte (z ≈ 211);
  - el del este (x ≈ 244);
  - un pasaje entre los bloques del este.

  Forman un circuito por detrás de las casas, con vallas de tablones, garajes (con grafitis en algunas puertas), cubos azules y negros, bolsas, charcos, maleza en los bordes y postes de madera con cables.
- **Casas bajas** en filas a lo largo de A, C1 y C2, con anchos de 6.5–8.5 m, retranqueos distintos y pasillos entre ellas:
  - **casitas de ladrillo** (rojo, oscuro o amarillo) de 2 plantas, con tejado de tejas a dos aguas y el hastial hacia la calle;
  - **greystones** de piedra gris de 2–3 plantas, con tejado plano, cornisa y escalinata con barandilla;
  - en general: escalones de entrada, a veces valla baja de hierro y árbol delante, y detrás un patio con valla de madera y, si cabe, un garaje que da al callejón;
  - alguna casa está **tapiada**, con contrachapado en las ventanas y maleza.
- **Apartamentos de 3 pisos** (9 m), en la calle C (al sur del tramo oeste) y a lo largo de B por el este:
  - ladrillo con miradores de arriba abajo, cornisa de piedra, zócalo con ventanas de bloque de vidrio, portal de piedra y valla baja;
  - detrás, **porches de madera de 3 alturas con escaleras en zigzag**, y un patio de hormigón con contenedor, bolsas y algún coche.
- **Solares vacíos** con malla metálica (a veces caída), maleza alta, un colchón, neumáticos y nieve. Uno grande está entre los bloques del este.
- **Tienda de esquina** (182, 190), en el quiebro de la calle C: ladrillo amarillo, escaparate con rejas, letrero rojo sin texto y un grafiti en el lateral.
- **Otros:**
  - farolas de sodio en las aceras y coches aparcados;
  - montones de nieve en el bordillo y placas en el césped;
  - basura en las aceras;
  - vallas de madera cerrando el barrio por el norte, el este y el sur.

### Barrio de casas oeste y franja de negocios (x −30–100, z 90–226)
Continuación del barrio de casas al otro lado de la vía. Tiene **la misma estructura** (las mismas calles, callejones, filas de casas, apartamentos, solar del este y tienda de esquina), girada 180° alrededor de la avenida:

- Se entra desde la avenida (x = 103) por la calle **A'** (z ≈ 194) y por la **C'** (z ≈ 128, con el quiebro). La **B'** (x ≈ 8) es sin salida por los dos extremos.
- El callejón que en el barrio del este es el del norte aquí queda al sur (z 102.5–107.5), detrás de los negocios. Su valla de madera se abre en el extremo oeste, donde el callejón del oeste (x ≈ −24) sigue por un **pasaje** hasta la calle norte.
- Las casas salen al azar con otra semilla, así que no son las mismas que en el este.

**Franja de negocios** en el lado que da a la manzana central (calle norte, z 90–102), con acera ancha de 6 m delante. Los locales están **sueltos y con aire** entre ellos. De oeste a este:

1. **Iglesia de local** "NEW HOPE CHURCH" (−18 a −6): lunas tapadas con cortina blanca, cruz en el pretil, cartel de horario.
2. **Lavandería** "COIN LAUNDRY" (−2 a 8): toldo azul, escaparate iluminado.
3. **Aparcamiento de clientes** (10 a 26): 6 plazas al fondo, malla, cartel "CUSTOMER PARKING ONLY" y farola. Entrada desde la calle.
4. **Barbería** "BARBER SHOP" (28 a 37), 2 plantas de piedra gris: poste de barbería con espiral, toldo negro, persiana medio bajada.
5. **Pollo y pescado frito** "CHICKEN & FISH" (42 a 54): rejas, toldo rojo, extractor en el tejado, manchas de grasa y un mural en la pared que da al solar.
6. **Solar** vallado con malla y maleza (54 a 64).
7. **Cambio de cheques y móviles** "CHECKS CASHED" (64 a 74), 2 plantas: rejas y cartel verde de "CELL PHONES".
8. **Tienda de comida y licores** "FOOD & LIQUOR" en la esquina con la avenida (79 a 99): rejas, carteles de ofertas, arcón de hielo en la acera y letrero "GROCERY" hacia la avenida.

Todos tienen letrero luminoso (se ve de noche y con niebla), cartel de neón OPEN (menos la iglesia) y, detrás, un patio de servicio con puerta de chapa, contenedor o cubo y bolsas. En la calle: farolas de sodio, papeleras, coches aparcados, nieve en el bordillo y basura.

## Catálogo de estructuras

| Estructura | Medidas | Dónde | Notas |
|---|---|---|---|
| Bloque bajo | 24×9×12 m, 3 plantas | Barrio de viviendas | Tejado plano con alero, portal hacia la calle. |
| Torre | 30×24×15 m, 8 plantas | Barrio de viviendas | Detrás de los bloques bajos, en los huecos entre ellos. Camino largo hasta la acera. |
| Two-flat | 10×8×14 m (o 5 m de alto) | Alrededores | Todavía en color liso. |
| Tienda de esquina | 16×4.5×14 m | Alrededores | Letrero azul sin texto. |
| Cancha | 12×20 m + 1.5 m de margen | Barrio de viviendas | Malla de 3.5 m. |
| Parada de bus | 3.7×2.4×1.6 m | Acera norte del barrio | Marquesina con banco y anuncio. |
| Casita de ladrillo | 6.5–8.5 × 6 × 8–10.5 m, tejado a dos aguas | Barrio de casas | Hastial a la calle, escalones, garaje detrás si cabe. |
| Greystone | 6.5–8.5 × 6–9 × 8–10.5 m | Barrio de casas | Piedra gris, cornisa, escalinata de 0.9 m con barandilla. |
| Apartamentos de 3 pisos | 14–24 × 9 × ≤13 m | Barrio de casas | Miradores, portal de piedra, porches traseros de madera. |
| Garaje | 5.6×2.8×6 m | Barrio de casas | Puerta al callejón, tejado plano o a dos aguas. |
| Tienda de esquina (casas) | 12×4.6×11 m | Barrio de casas, (182, 190) | Escaparate con rejas, letrero rojo. |
| Torre del conjunto | 27×15 m, 11–14 plantas (33–42 m) | Manzana central | Forjados vistos, balcones con malla detrás, portal con marquesina. |
| Fila de adosadas | 38–46 × 9 m, 2 plantas (6 m) | Manzana central | Casas de ~6.5 m, tejado gris a dos aguas, portales por parejas. |
| Aparcamiento | 40–44 × 17 o 34 m | Manzana central | Plazas de 2.6×5 m, entrada de 8 m desde la calle sur. |
| Coche aparcado | 4.9–5.4 × 1.95–2 m | Calles y aparcamientos | Sedán, coupé, todoterreno o furgoneta al azar, en el sentido de la marcha en la calle. |
| Local de negocio | 9–20 × 7.5 m, 1 planta (4.6 m) o 2 (7.4 m) | Franja de negocios | Frente pintado, escaparate, letrero luminoso con texto genérico, rejas o persiana, toldo según el tipo. |
| Aparcamiento de clientes | 16 × 14.5 m | Franja de negocios | 6 plazas, malla, entrada desde la calle norte. |
| Pórtico del tren elevado | 2 pilares de 6.6 m, luz de 16.4 m | Avenida principal, cada 15 m | Sin pórtico donde cruzan calles. |
| Estación del tren elevado | 2 andenes de 64 m a 9.6 m de altura | Avenida principal, z 4–68 | Marquesinas, bancos y una escalera a cada acera. |
| Tren elevado | 4 vagones de 14.6×2.9 m | Vías en x 106.5 y 113.5 | Acero plateado con franja granate, sin logotipos. Se mueve. |

## Peticiones y comentarios

Registro de lo que se ha pedido sobre el mundo, del más antiguo al más reciente. Se usa para no repetir errores y mantener el tono.

- **2026-11-14**: Texturas como las de `img/` en una parte del mundo: ladrillo y calles. → Se creó el barrio de viviendas con texturas generadas.
- **2026-11-14**: «Más detalle en las calles: bancos, árboles, vallas, arbustos, suciedad, canastas… rollo muy callejero». → Se añadió el detalle callejero del barrio.
- **2026-11-14**: «Los caminos de entrada a los bloques grandes se sienten muy vacíos». → Se añadieron explanadas, barandillas, farolas, bancos, tablón, pilonas, cámara y más basura en los accesos a las torres. *Regla que sale de aquí:* los recorridos largos a pie necesitan algo cada 8–10 m.
- **2026-11-14**: Llevar este archivo con todo lo que se diga sobre el mundo.
- **2026-11-14**: «Siempre que hablemos de estructuras, leer los README o .md relacionados para tener el contexto; si se implementa una regla nueva, actualizarlos». → Regla añadida en `CLAUDE.md` y en *Reglas generales*.
- **2026-11-14**: «Hablando de estructuras, también hay que ver las imágenes de referencia de `img/`». → Añadido a la regla. Se revisó `apartaments.jfif`, que no estaba en la tabla, y se añadió.
- **2026-11-14**: «Aunque sea invierno, que algunos árboles tengan hojas, unos con nieve y otros con color de invierno. Restos de nieve en algunas zonas y hierba o vegetación más alta, como de zonas un poco abandonadas». Se pidió para la zona avanzada (el barrio de viviendas). → Se añadieron árboles variados, restos de nieve y maleza alta (`PrototypeSceneBuilder.Vegetation.cs`). *Regla que sale de aquí:* el invierno no deja todo pelado; se mezclan aspectos de árbol y se deja maleza en lo descuidado.
- **2026-11-14**: «Hacer una imagen de cómo va quedando el mapa, para usarla de referencia al ir modificándolo». → Se creó `mapa/mapa.png` con su generador `mapa/generar_mapa.py`. *Regla que sale de aquí:* el plano se actualiza con cada cambio de disposición del mundo.
- **2026-11-14**: «Al norte del barrio de viviendas, casas bajas junto con apartamentos de tres pisos. Las calles deben ser más laberínticas y no tan organizadas, porque tienen partes traseras». Fotos nuevas en `img/estructures/`. → Se creó el barrio de casas (`PrototypeSceneBuilder.Houses.cs`), con calles sin salida, un quiebro y una red de callejones traseros, y se quitaron los two-flats que había en ese sitio. *Regla que sale de aquí:* los barrios de casas no son cuadrículas.
- **2026-11-14**: «Una avenida principal con el tren por encima (solo decorativo)». Fotos nuevas: `trainavenue.avif` y `thetowers1-3`. → Se propuso la avenida de x = 110, y el usuario eligió esa avenida y un tren **que se mueva**. Se construyeron el tren elevado y la estación (`PrototypeSceneBuilder.Train.cs`, con el movimiento en `ElevatedTrain`).
- **2026-11-14**: «En la manzana central, un bloque parecido a las imágenes de The Towers, pero en armonía con el de al lado, y en algunos huecos aparcamientos». → La manzana central pasó a ser un conjunto de 3 torres con filas de casas adosadas, patios y 3 aparcamientos (`PrototypeSceneBuilder.Towers.cs`), con los materiales del barrio de viviendas. *Regla que sale de aquí:* una zona nueva comparte materiales con la vecina.
- **2026-11-14**: «Mejorar los assets de los coches». → Los coches dejaron de ser dos cajas: hay 4 modelos low-poly con silueta (sedán, coupé, todoterreno, furgoneta), ruedas con llanta y desgaste de invierno (`PrototypeSceneBuilder.Cars.cs`). El coche que se conduce es el sedán, con llantas cromadas y faros encendidos. *Regla que sale de aquí:* coches de la época, sin marcas.
- **2026-11-14**: «Continuar el barrio de casas al otro lado de la vía, con la misma estructura, pero que la parte que da al conjunto de torres sean negocios (barber, market, etc.) que no estén muy apiñados». → Se construyó la copia oeste del barrio de casas (el mismo trazado girado 180°) y, delante, una franja de 6 negocios sueltos con un aparcamiento y un solar entre ellos (`PrototypeSceneBuilder.Shops.cs`). También se puso la acera oeste de la avenida y se quitaron los two-flats que había en ese sitio. *Regla que sale de aquí:* calles de negocios con aire.

## Pendiente e ideas

- Pasar los two-flats de los alrededores al mismo nivel de texturas y detalle que el barrio de viviendas.
- Tejados a cuatro aguas en los bloques bajos, como en `oblock.png`.
- Balcones de hormigón en las fachadas (`gang.png`).
- Calles curvas.
- Autobús urbano circulando (`bus.png`).
- Llevar la vegetación de invierno (árboles variados, nieve y maleza) a los two-flats.
- Traseras de los bloques del barrio de viviendas con porches de madera como los del barrio de casas (`apartaments.jfif`).
- Torre con la esquina en chaflán (`projects.png`).
- Locales en la acera oeste de la avenida principal, bajo el tren (tienda de licores, comida rápida, peluquería; letreros sin marcas reales).
- Coches: más modelos (pickup, coche de policía genérico), cristales rotos y tráfico que circule.
- Sonido del tren al pasar y chispas en los raíles.
- Manzana central: planta baja abierta con pilares en alguna torre, galerías exteriores y coches que entren y salgan de los aparcamientos.
