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
- **Ninguna acera atraviesa una calzada:** las tiras de acera largas se cortan (`StripMinus`) donde las cruza cualquier calle; un bordillo de 15 cm en mitad de un cruce es un fallo.
- **Calles rectas y con continuidad:** las calles nuevas siguen en línea recta las que ya hay (misma x o misma z) y, si cruzan la avenida, lo hacen enfrente de la calle del otro lado. Lo irregular del barrio sale de las calles sin salida, los pasajes y los callejones, no de quiebros en las calles nuevas.
- **Escaleras que llevan a algún sitio:** toda escalera, porche o rellano acaba en una puerta (nunca delante de una ventana de la textura), y lo que parece transitable se puede subir a pie.
- **Puertas y ventanas que encajan:** una puerta sobre una fachada de ladrillo va siempre en el centro de un vano de la textura y su marco tapa la ventana entera; nunca deja medias ventanas asomando ni se mete en una imposta, cornisa o dintel. Los hastiales y antepechos son de ladrillo liso, para que el tejado no corte ventanas. Las barandillas van sobre el borde de los escalones y apoyadas en postes, nunca flotando.
- **Ambiente callejero:** las zonas de barrio tienen que sentirse vividas y descuidadas, con basura, manchas, grafitis, vallas, muebles tirados y cables. Ninguna zona transitable debe quedar vacía.
- **Escalar y cubrirse:** el personaje escala (hasta 2.4 m) y se cubre detrás de cualquier cosa **visible** con collider. Los límites que no se deben pasar se hacen con colliders invisibles (`ColliderBox`) o con `NoClimb` en el objeto. Al pasar al otro lado de algo, el suelo no puede estar más de 3 m por debajo, así que los pretiles sobre desniveles grandes (autopista) ya no se pueden saltar. Las vallas de 1.8–2.4 m se trepan; si una tiene que cerrar el paso, debe ser más alta o llevar `NoClimb`. **Todas las vallas se pueden sortear** (las bajas se saltan, las altas se trepan), aunque sean tablas de 3 cm. **Lo que deja ver a través no cubre:** verjas de barrotes, mallas y barandillas caladas (material con recorte, o `NoCover`) no sirven de cobertura; las esquinas de las casas, los muros, las vallas de tablones y los coches, sí. Los coches se cruzan por encima con Espacio.
- **Interiores dentro del edificio:** los edificios por fuera son cajas sin interior. Cuando hace falta uno (la casa del jugador), se construye dentro del propio edificio, que se deja hueco solo ahí (fachada con las celdas abiertas y collider en trozos, `TowerOpenings`). Si tiene ventanas, son huecos reales con cristal, para que se vea la ciudad. Entre plantas se va en ascensor, que teletransporta con fundido a negro. (Antes las salas iban escondidas bajo el edificio; se cambió el 2026-11-15 para tener vistas.)
- **Comprobar que cabe:** el mundo va de x −300 a 300 y de z −300 a 300 (el plano rotula todo). Antes de construir una zona se comprueba que lo pedido cabe a escala en el sitio indicado; si no cabe, se pregunta qué hacer en vez de encogerlo o moverlo sin decir nada.
- **Escala de juego en lo grande:** las instalaciones que a escala real no caben en una manzana (campos de deporte, pistas) pueden ir algo reducidas, siempre que se lean bien: el campo de fútbol americano del instituto usa 0.76 m por yarda.
- **El inicio, con personalidad:** lo que se ve desde donde empieza el jugador (la acera sur de la manzana central) no puede ser relleno genérico: va algo reconocible y con juego, como las cocheras del tren.
- **Hora y tiempo:** el día es el de siempre (cielo cubierto de invierno, luz fría y suave) y no se toca. Un día dura 20 min reales (`WeatherSystem`, 50 s por hora): amanece de 5:30 a 7:30 y anochece de 18:30 a 20:30. El cielo es un degradado gris cuyo horizonte es siempre el color de la niebla, para que lo lejano se funda con él. De noche el cielo y la niebla tiran al naranja del sodio de la ciudad y las farolas hacen charcos de luz. Llueve o nieva al azar, pero casi siempre está seco (~70 % nublado, ~20 % nieve, ~10 % lluvia; episodios de 2–7 min que entran y salen en 40 s). **Toda luz de calle nueva** (farola, foco, letrero exterior) lleva `NightLight`: apagada de día, encendida al anochecer. Las de interiores, fuegos y el túnel no lo llevan y se quedan siempre encendidas.
- **Texturas de personaje pintadas:** la ropa y la piel del personaje imitan las texturas de PS2 de `gang.png`: la forma la da la textura, no la trama. Pliegues y arrugas con cresta clara y surco oscuro, sombra horneada, costuras, pespuntes y desgaste, a baja resolución (128 px) y casi blancas para que el color lo ponga la prenda. Nada de tramas finas que de lejos se vean lisas.
- **Forma del personaje:** mallas low-poly facetadas (por anillos), no primitivas redondas de Unity. Proporciones de PS2 como en `gang.png`: cabeza pequeña y ovalada, torso cuadrado, brazos largos y finos, piernas largas, ropa ancha que cae recta y pies grandes. Los rasgos de la cara se pintan en la textura; solo la nariz y las orejas llevan malla.
- **Gente en el mundo:** cada sitio donde deba haber gente (esquina, banco, portal, puerta de tienda, cancha, bidón) lleva un `NpcSpot` desde el generador (`PrototypeSceneBuilder.Npc.cs`): los bancos y el bidón con fuego lo reciben solos; las esquinas habituales de cada grupo y los sitios de paso de civiles van en las tablas `HabitualSpots` y `CivilianSpots` (siempre **en la acera o el césped**, no en la calzada). Todo local nuevo hecho con `Storefront` tiene horario (`ShopHours`): de noche, persiana bajada y sin OPEN, salvo la tienda de licores.
- **Cómo se trabaja este archivo:** antes de hablar o tocar cualquier estructura se leen este archivo y los README relacionados (sobre todo `Assets/_Project/Editor/README.md`) y se miran el plano `mapa/mapa.png` y las imágenes de `img/estructures/` (y `img/`). Cada imagen nueva se añade a la tabla de *Referencias visuales*. Toda regla nueva que salga se apunta en esta sección.

## Referencias visuales (`img/estructures/`)

Las imágenes están en `img/estructures/`. `gang.png` sigue en `img/`.

| Imagen | Qué se toma de ella |
|---|---|
| `oblock.png` | Torres de ladrillo rojo de 8 plantas, bloques bajos de 3 plantas con tejado gris, calle curva con doble línea amarilla, árboles pelados, vallas de hierro negras y cables de luz. |
| `bus.png` | Acera ancha de hormigón agrietado, parada de bus con marquesina, autobús urbano, postes de madera con cables, pilonas rojas y casas de ladrillo de 2 plantas con ventanas blancas. |
| `gang.png` | Fachada de ladrillo de cerca: ventanas con marco blanco y alféizar, balcones de hormigón y césped gastado. **Personaje:** cómo viste la gente (no las personas ni sus caras): camiseta blanca XL larga y ancha, vaqueros claros anchos amontonados en el tobillo, botas de trabajo color trigo, chaleco negro encima de la camiseta, polo de rayas de colores, camisa vaquera, pantalón caqui, gorra, gorro de pescador, pañuelo de cabeza, gafas de sol y cadena; cuerpos delgados y hombros anchos, de aire PS2. |
| `apartaments.jfif` | Parte trasera de bloques de ladrillo de 3 plantas: porches y escaleras de madera exteriores en zigzag, poste de luz con muchos cables, contenedores verdes y cubos azules, valla de madera, coche aparcado en un solar de tierra y hierba seca con basura. |
| `houses.png` | Calle de casas bajas: casitas de ladrillo de 1–2 plantas con tejado a dos aguas mirando a la calle, greystones de piedra gris con escalinata y barandilla, vallas bajas, árboles pelados grandes y niebla. |
| `apartaments2.jpg` | Bloque de apartamentos de 3 pisos de ladrillo oscuro: miradores de arriba abajo, cornisas de piedra, sótano con ventanas de bloque de vidrio, portal de piedra, vallas bajas de hierro y césped seco con basura. |
| `backparts.png` | Callejón trasero de noche: asfalto roto, vallas de tablones altas, garajes y cobertizos, cubos de basura, maleza junto a las vallas y postes con cables. |
| `block2.png` | Calle agrietada con montones de nieve en la acera, bloques de 3 pisos de ladrillo claro, valla de malla, contenedor y árboles pelados enormes. |
| `area1.webp` | Vista aérea de un barrio real: manzanas irregulares que mezclan casas y bloques, muchos solares vacíos, aparcamientos y árboles. Se toma solo la idea de trama desordenada. |
| `projects.png` | Torre alta de ladrillo con la esquina en chaflán y la planta baja abierta (idea para futuras torres). |
| `trainavenue.avif` | Tren elevado (tipo "L") sobre la calle: pórticos de acero con riostras en X, tablero con traviesas y raíles, pasarela con barandilla, vagones de acero plateado; debajo, calle con coches aparcados, acera con valla y césped. |
| `abandonedParcelVegetacion.png` | Solar de esquina abandonado en una calle de casas: hierba larga sin segar por todo, acera de hormigón que lo cruza, árboles grandes y matorral al fondo (uno medio seco), poste de cartel torcido junto al bordillo y postes de luz con cables detrás. Se toma para los solares con vegetación (`OvergrownLot`), en versión de invierno. |
| `thetowers1.webp`, `thetowers2.jfif`, `thetowers3.webp` | Torres altas de ladrillo detrás de filas de casas adosadas de 2 plantas, con tejado gris, bajantes y puertas de colores tapiadas; césped gastado con caminos de tierra, tendederos, un sofá naranja tirado y canasta suelta. Solo se toman los edificios y el ambiente, no las personas. |

## Mapa (metros, Y arriba)

**Plano detallado: [`mapa/mapa.png`](mapa/mapa.png).** Es la vista cenital a escala, con cuadrícula de 20 m, coordenadas y leyenda, y la referencia para ir modificando el mapa. Se genera con `python mapa/generar_mapa.py` y se regenera cada vez que cambia la disposición del mundo. Lo que sale al azar (árboles del césped, solares, basura) no aparece con su posición exacta. Abajo, el esquema rápido:

```
        z+  ┌───────────────────────────────┐ ┌────────────────────┐
            │ Ampliación norte (oeste)      │ │ Ampliación norte   │
            │ x∈[-103,100] z∈[224,296]      │ │ (este) z∈[214,296] │
            ├──────────┬────────────────────┤ ├────────────────────┤
            │ Ampliac. │ Barrio casas oeste │ │  Barrio de casas   │
            │ oeste    │ x∈[-30,100]        │ │  x∈[120,250]       │
            │ x∈[-103, │ z∈[102,226]        │ │  z∈[90,214]        │
            │   -30]   ├────────────────────┤ └────────────────────┘
  Oeste     │ z∈[90,   │ Negocios z∈[90,102]│
  lejano    │   226]   │                    │
 x∈[-300,   └──────────┴────────────────────┘
   -120], z∈[90,298]: gasolinera, negocios, cancha, descampado
 ┌───┬─────────────┐ ┌──────────────────────┐ avenida ┌────────────────────┐
 │ A │ Instituto   │ │ Manzana central      │ x=110   │ Barrio de viviendas│
 │ u │ x∈[-275,    │ │ (torres + adosadas)  │         │                    │
 │ t │   -120]     │ │  x∈[-100,100]        │         │  x∈[120,250]       │
 │ o │ z∈[-70,70]  │ │  z∈[-70,70]          │         │  z∈[-88,88]        │
 └───┴─────────────┘ └──────────────────────┘         └────────────────────┘
 Autopista hundida x∈[-300,-275]: de z −300 a z 90 (túnel debajo del oeste lejano)
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
  - una escalera de acero que baja a cada acera (z 10 → 25), **por la que se sube a pie**: pendiente suave, barandillas a los dos lados y rellano arriba. Desde el andén no se puede bajar a las vías.
- **Trenes:** dos trenes de 4 vagones de acero plateado con franja granate y sin logotipos.
  - Uno va hacia el norte por la vía este y el otro hacia el sur por la vía oeste.
  - Cada uno se para unos 10 s en la estación, sigue hasta perderse en la niebla y vuelve a pasar al rato.

### Manzana central: conjunto de torres (x −100–100, z −70–70)
*Facción: Ash Block, el bloque del jugador (complejo "Ashgrove Homes"; ver [NPCS.md](NPCS.md)).*

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
- **Vida del bloque** (`PrototypeSceneBuilder.Studio.cs`), lo que hace de la manzana "el bloque":
  - **Estudio casero en el sótano de T1**, el centro de la progresión musical. Se baja por una escalera de hormigón con barandilla en el césped, al oeste de la torre (x −80…−78, z 40–48.5, al norte de los contenedores). Abajo está la sala (x −77.5…−63, z 38–49.5, a 3.2 m bajo el suelo): moqueta, paneles de espuma, techo acústico, LED morado, luz roja de grabación sobre la puerta, mesa de mezclas con dos pantallas, monitores y teclado, silla de oficina, sofá naranja, nevera, caja de discos y altavoz grande. En la esquina noreste hay una cabina de voz con cristal y el micro con antipop: ahí se graba (ver `Scripts/Music/`).
  - **Casa del personaje en T2**: el portal de T2 (el del sur, hacia el eje y P2) está abierto, con la puerta de cristal abierta hacia dentro y el telefonillo al lado. Se entra a pie a un **vestíbulo** de 10 × 6.7 m: baldosa, zócalo verde, buzones metálicos (dos forzados, propaganda por el suelo), puerta de la escalera con luz de salida, avisos, radiador, silla rota, basura, cámara en el techo, grafitis y dos fluorescentes (uno casi fundido). En la pared del fondo, dos **ascensores**: el de la izquierda con un cartel de averiado; el de la derecha sube con F (fundido a negro) al **piso**, en la esquina sureste de la **última planta** (la 14ª, a 39 m), de 8.4 × 7.7 m. Tiene dos ventanas al sur (el inicio, P2, la calle y las cocheras) y una al este (el tren elevado), huecos reales en la fachada con marco blanco y cristal: se ve la ciudad de verdad. Dentro: entrada con el ascensor, felpudo y zapatillas; salón con sofá oscuro mirando a la tele de la pared oeste, alfombra granate, mesa baja con caja de pizza y vasos rojos, consola y altavoces; un **sillón de cara a la ventana sur** para mirar la ciudad; cocina americana en la pared norte; cama con edredón azul junto a la ventana este, armario con espejo y cajas de zapatillas, sudadera tirada y la **caja fuerte** en el rincón noroeste. Ahí se cambia de ropa (armario) y se guardan dinero, armas y objetos (caja fuerte). Para bajar, el mismo ascensor.
  - **Memorial** en el eje E–O, al oeste de T2 (x ≈ −17.5, z −4.6, junto a la farola de x −15): un tramo de malla con la foto enmarcada (sin cara: una silueta), carteles "RIP" y "LONG LIVE", 16 velas de oración encendidas, peluches, ramos de flores, una botella y globos atados. No se construye nada más.
  - **Zona de reunión** al sur de la losa de la canasta (−26, 23.7): seis sillas de plástico blancas y verdes en corro, barbacoa de bidón con brasas, carne y humo, altavoz portátil con aros LED, nevera portátil, vasos rojos y una bolsa de carbón.
- **Patios:** losa con una canasta portátil, tendederos con ropa, un sofá naranja tirado, atajos de barro por el césped, árboles variados, placas y montones de nieve, bolsas y basura.
- **Valla baja de hierro** alrededor de la manzana, abierta donde llegan los caminos y en las entradas de los aparcamientos.
- Acera de 3 m y calzada de 14 m alrededor. Calles E–O en z = ±80 y N–S en x = ±110.
- **Calles grandes** (las cuatro de 14 m): 4 carriles con doble línea amarilla, bordes blancos y separadores discontinuos; asfalto gastado de invierno (rodadas, sal, grietas selladas con alquitrán, parches de obras, baches, pintura borrada). Las calles norte y sur acaban en la avenida (al este está el barrio de viviendas), así que los cruces de la avenida son en T. En los cruces (±110, ±80): separadores continuos al llegar, línea de stop en los carriles que entran y paso de cebra de barras anchas en cada lado. Tapas de alcantarilla, sumideros en el bordillo, nieve sucia apartada contra el bordillo, charcos y aceite donde paran los coches.

### Cocheras del tren elevado (x −74…100, z −91.5…−222)
*Tierra de nadie: escondite y atajo, con grafitis de todas las facciones.*

Al sur de la calle z = −80, justo enfrente del inicio del jugador, donde antes había una fila de two-flats lisos (`PrototypeSceneBuilder.RailYard.cs`). Es una playa de vías de grava cerrada con valla, el sitio donde duermen los trenes del elevado de la avenida. Quedan dos two-flats en la esquina oeste (con la calle x = −110).

- **Rampa de acceso**: sale de la vía oeste del elevado (x 106.5, justo al sur del cruce de z = −80) y baja en diagonal hacia el suroeste hasta el fondo de la playa (78, −206). Arriba va en viaducto de acero, con pórticos cada 12 m; por debajo de 2.6 m, sobre un terraplén con muros de contención, al que se puede subir.
- **Escalera de agujas** al fondo: una vía casi E–O, de (78, −206) a (−30, −214), de la que salen todas las demás, con palancas de cambio, señales enanas y una señal alta al pie de la rampa.
- **Vías de estacionamiento**: diez vías N–S (x 8 a 57.5, cada 5.5 m) con traviesas, tercer carril con cubierta de madera y topes rojos en el extremo norte (z −100). Hay **7 trenes aparcados** de 2 o 4 vagones, con las luces apagadas y grafitis en algunos costados, que dejan pasillos para cruzar a pie. Los vagones tienen colisión: sirven de cobertura.
- **Nave de mantenimiento** (x −34…−6, z −160…−104): ladrillo hasta 3 m y chapa ondulada encima, tejado a dos aguas de chapa, ventanas altas corridas. Por el sur entran tres vías por portones de 5 × 6.5 m (uno con la persiana a medio bajar); puerta de personal al este, hacia el camino. Dentro: solera de hormigón, un tren de 2 vagones en la vía central, pasarela de trabajo, banco, bidones, ruedas sueltas y dos lámparas encendidas. Grafitis en la fachada oeste y en la sur.
- **Patio de contenedores** al oeste (x −72…−42): 7 filas × 8 columnas de huecos con pilas de 0 a 3 contenedores de 12 m (arriba, a veces de 6 m) en seis colores, chapa ondulada con chorretones de óxido y barras de cierre. Pasillos N–S de 5 m entre bloques y de 3 m entre filas. Al pie de algunas pilas hay **palés apilados** para subir encima; una carretilla elevadora parada; un **contenedor abierto** con un colchón dentro (escondite).
- **Entrada** frente al inicio del jugador (x −1…7): pórtico con el letrero "RAIL YARD", puerta corredera abierta, barrera medio levantada, **caseta del vigilante** con luz y carteles "NO TRESPASSING" en la valla. De ahí sale un **camino de servicio** de asfalto hacia el sur, entre la nave y las vías.
- **Valla** de malla de 2.2 m con **concertina** encima (se puede trepar, como todas), con un agujero al oeste (hacia los patios de los two-flats) y otro al sur, con la malla arrancada colgando.
- **Resto**: oficina prefabricada sobre bloques al norte de la nave, con un coche aparcado y un contenedor de basura; **cuatro torres de focos** de 16 m con luz real; traviesas viejas, carriles y ruedas apiladas, bidones oxidados y montones de balasto en la franja libre del este; nieve apartada junto al camino, maleza al pie de la valla y hierbajos entre las vías.
- **Aceras** nuevas a los lados de la playa, a lo largo de las calles x = ±110, que siguen hacia el sur.

### Alrededores
- Filas de casas de 2 plantas (two-flats) al otro lado de la calle sur, menos enfrente de la manzana central, donde están las cocheras (quedan dos en la esquina oeste). Al norte ya no quedan: al oeste de la calle x = −110 está el oeste lejano; de ahí a la avenida, la ampliación oeste, los negocios y el barrio de casas oeste, y al este de la avenida el barrio de casas.
- Tienda de esquina con letrero azul y coches aparcados.

### Barrio de viviendas (este, x 120–250)
*Facción: Kessler Courts, set aliado de Ash Block al otro lado de la vía. Frontera con Larkin en el callejón sur del barrio de casas.*

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
*Facción: Larkin Street, enemigo del bloque y de Kessler (también su ampliación norte-este).*

Casas bajas y apartamentos de 3 pisos, con calles que no forman cuadrícula y muchas partes traseras. Se entra desde la avenida por las calles A y C.

- **Calles** de 10 m con aceras de 2.5 m:
  - **A** (z = 122): entra desde la avenida y acaba en la calle B.
  - **B** (x = 212): va de norte a sur; es **sin salida por el sur** (barrera de hormigón, pilonas y nieve apilada) y por el norte sigue recta como **G** en la ampliación norte.
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
- **Apartamentos de 3 pisos** (9 m de fachada + 0.75 m de antepecho de ladrillo liso), en la calle C (al sur del tramo oeste) y a lo largo de B por el este:
  - ladrillo con miradores de arriba abajo, cornisa de piedra, zócalo con ventanas de bloque de vidrio, portal de piedra y valla baja;
  - detrás, **porches de madera de 3 alturas con escalera en zigzag por la que se sube a pie**: una terraza por piso con su **puerta trasera** (la escalera siempre llega a una puerta, nunca a una ventana), barandillas de balaustres y una torre de escalera de tramos de medio piso con rellanos; detrás, un patio de hormigón con contenedor, bolsas y algún coche.
- **Solares vacíos** con malla metálica (a veces caída), maleza alta, un colchón, neumáticos y nieve. Uno grande está entre los bloques del este.
- **Tienda de esquina** (182, 190), en el quiebro de la calle C: ladrillo amarillo, escaparate con rejas, letrero rojo sin texto y un grafiti en el lateral.
- **Otros:**
  - farolas de sodio en las aceras y coches aparcados;
  - montones de nieve en el bordillo y placas en el césped;
  - basura en las aceras;
  - vallas de madera cerrando el barrio por el norte, el este y el sur.

### Barrio de casas oeste y franja de negocios (x −30–100, z 90–226)
*Facción: Dunmore (también las ampliaciones oeste y norte-oeste), el rival más cercano de Ash Block. La calle norte es la frontera; la franja de negocios está en disputa y la tienda de licores es la esquina más valiosa.*

Continuación del barrio de casas al otro lado de la vía. Tiene **la misma estructura** (las mismas calles, callejones, filas de casas, apartamentos, solar del este y tienda de esquina), girada 180° alrededor de la avenida:

- Se entra desde la avenida (x = 103) por la calle **A'** (z ≈ 194) y por la **C'** (z ≈ 128, con el quiebro). La **B'** (x ≈ 8) es sin salida por el sur (junto a los negocios) y por el norte sigue recta hasta la calle F de la ampliación norte.
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

### Oeste lejano (x −300…−120, z 90–298, al otro lado de la calle x = −110)
*Facción: Crestline, los mayores, set aliado de Ash Block y enemigo de Dunmore.*

Barrio de casas con negocios, gasolinera, cancha y descampado (`PrototypeSceneBuilder.FarWest.cs`). Sustituye a los two-flats lisos que había al norte de la calle norte.

- **Calles:** D, E, F y H **siguen rectas** las de la ampliación oeste y la norte cruzando la calle x = −110 (ahí son cruces de cuatro brazos) y acaban en la calle **J** (x −262…−252), que va de la calle norte al borde norte. Los callejones siguen los n2 y n3 de la ampliación oeste y acaban en la cancha y en el descampado.
- **Gasolinera** en la esquina de la calle norte con la calle x = −110 (x −160…−120, z 90–124): explanada de asfalto con dos entradas desde la calle norte y una desde la x = −110, marquesina blanca de 22 × 12 m con franja roja y letreros "GAS", dos islas con cuatro surtidores, tienda "FOOD MART" al fondo (rejas, arcón de hielo, jaula de bombonas, cartel "ATM INSIDE"), poste de precios en diagonal hacia el cruce (REGULAR, PLUS, DIESEL con números de LED naranjas, sin marca), máquina de aire, contenedor, manchas de aceite y nieve apartada.
- **Negocios** sueltos frente a la calle norte (misma línea de fachada que la franja de negocios, z 93): alitas "WINGS & FRIES", casa de empeños "PAWN SHOP" (2 plantas, "WE BUY GOLD"), tienda de belleza "BEAUTY SUPPLY", un solar vallado, neumáticos "TIRE SHOP" (pilas de neumáticos en la acera) y una lavandería.
- **Cancha de baloncesto** (la misma que la del barrio de viviendas) entre D y E, junto a J, con árboles y farola; el callejón de detrás de las casas acaba en ella.
- **Descampado** entre E y F, junto a J (≈ 45 × 52 m): tierra y barro con charcos, hierba a rodales, montones de escombros y ladrillos, malla con un tramo caído, coche abandonado, palés, neumáticos, sofá, colchón, carro de la compra volcado, bidón quemado con brasas y cajas de leche alrededor, cartel "FOR SALE", árboles pelados, senderos de barro y nieve.
- **Casas y apartamentos** en el resto, con solares con vegetación y vacíos; al oeste de J, una fila de parcelas hondas que llega a la valla del borde del mundo.

### Autopista hundida (borde oeste, x −300…−275)
Autopista en trinchera, al estilo de las del Southside, pegada al borde oeste del mundo (`PrototypeSceneBuilder.Highway.cs`). Va de norte a sur y las calles z = ±80 pasan por encima en puente.

- **Trinchera** de 7 m de hondo y 25 m de ancho, con **muros de contención** de hormigón a los dos lados: juntas cada 4 m, chorretones de agua y óxido, mechinales, eflorescencias, la parte de abajo negra de lo que salpican los coches y grafitis.
- **Calzadas:** dos sentidos de 3 carriles (hacia el sur por el oeste y hacia el norte por el este), arcenes con bandas sonoras, línea amarilla junto a la mediana y blanca en el borde, asfalto gastado con sal y grietas selladas. En el centro, una **mediana de hormigón** tipo New Jersey con farolas altas de doble brazo cada 40 m.
- **Puentes** en z = 80 y z = −80: llevan la calle grande y las dos aceras. Por debajo, vigas, una pila en la mediana y, en los bordes, pretil de hormigón con malla.
- **Túnel** al norte del puente de z = 80: la autopista se mete debajo del oeste lejano. Boca con dintel de hormigón, fluorescentes y oscuridad al fondo. Al sur, la trinchera sigue hasta perderse en la niebla.
- **Pórtico** en z −30 con carteles verdes sin nombres reales (EXPRESSWAY SOUTH / NORTH, EXIT ONLY). Basura, bolsas y nieve sucia en los arcenes, manchas y un coche averiado con el triángulo puesto.
- **Tráfico** decorativo: coches de la época por los seis carriles, con los faros encendidos, que salen de la niebla y entran en el túnel (y al revés).
- Bordes: pretil y malla en los dos muros, así que no se puede saltar dentro. Todavía no hay rampas para entrar con el coche.

### Instituto (x −275…−120, z −70…70)
*Zona neutral: coinciden chavales de todos los barrios.*

Instituto público entre la autopista y la calle x = −110, y entre las calles z = ±80 (`PrototypeSceneBuilder.School.cs`). Usa los materiales de los barrios (acera de losas, césped seco, valla de hierro, malla, farolas de sodio), con ladrillo claro como el de muchas escuelas de la ciudad.

- **Edificio principal** de 3 plantas mirando a la calle norte: ladrillo claro con ventanas, cornisa y antepecho de ladrillo liso. Portal de piedra con tres puertas de cristal y escalinata con barandillas. Encima, un remate con la placa "HIGH SCHOOL" (sin nombre real) y "EST 1961". Rejas en las ventanas de la planta baja, alguna ventana tapada con contrachapado, letrero de "ALL VISITORS REPORT TO MAIN OFFICE" y climatizadores en el tejado.
- **Ala** de 2 plantas hacia el patio y **gimnasio** de ladrillo liso con una banda de bloque de vidrio arriba, letrero "GYMNASIUM" y puertas dobles rojas.
- **Campo de fútbol americano con pista** de 6 calles: hierba de invierno amarillenta con calvas de barro, zonas de anotación granates, líneas, hashes y números, placas de nieve. Porterías amarillas, banquillos, cuatro torres de focos, marcador HOME/GUEST y malla alrededor. El campo va a escala de juego (0.76 m por yarda) para que quepa con la pista.
- **Gradas** de aluminio de 7 filas con palco de prensa; se sube andando hasta arriba.
- **Aparcamiento** de profesores con salida a la calle x = −110 (STAFF PARKING ONLY), coches, nieve apartada en las esquinas y **dos autobuses escolares** amarillos sin marcas.
- **Patio trasero** de losas con bancos y mesa de picnic, zona de servicio con contenedores, palés y muelle de carga, mástil con bandera, cartel de bienvenida ("WELCOME BACK", "GAME FRIDAY"), cámara de vigilancia, árboles, grafitis, basura y atajos de barro.

### Ampliación norte de los barrios de casas (z hasta 298, los dos lados de la avenida)
Sigue los barrios de casas hacia el norte hasta el borde del mundo (`PrototypeSceneBuilder.HousesNorth.cs`). Las filas nuevas dan la espalda a los callejones que antes eran el borde norte (a4 en el este; a1' y el n3 de la ampliación oeste en el oeste), así que esas vallas se quitaron.

Las calles son **rectas y continúan las que ya había**:
- **F / Fe** (z 248–258): una sola calle recta desde la calle x = −110 hasta la calle G, cruzando la avenida (al oeste se llama F; al este, Fe).
- **H** (z 288–298): calle del borde norte, también de la calle x = −110 a G, cruzando la avenida.
- **B'** (x 3–13) sigue recta desde la copia oeste hasta F (ya no es sin salida por el norte).
- **B** (x 207–217) sigue recta hacia el norte como **G**, cruzando el callejón a4, hasta H (ya no es sin salida por el norte).

Este (x 120–250): apartamentos y casas al sur de Fe (hasta el callejón a4), casas con garaje al norte de Fe (dando la espalda a H), apartamentos y un solar a lo largo de G, y el callejón del este que sigue hasta z 285.5. Oeste (x −103–100): casas, dos bloques de apartamentos y tres solares con vegetación a los dos lados de F, y un pasaje norte–sur (x −42…−37) entre F y H. Donde F/Fe y H cruzan la avenida no hay pórtico del tren y la acera de la avenida se corta.

### Ampliación oeste del barrio de casas (x −103…−30, z 90–226)
Sigue la copia oeste hacia la calle x = −110, con el mismo tipo de casas pero un trazado propio (`PrototypeSceneBuilder.HousesWestExt.cs`):

- Dos calles **sin salida** que entran desde la calle x = −110: la **D** (z 126.5–136.5) y la **E** (z 180.5–190.5), con barrera, pilonas y nieve apilada al fondo (x ≈ −36). Acera nueva en el lado este de la calle x = −110.
- Tres **callejones** que continúan los de la copia: el de detrás de los negocios (z 102.5–107.5), uno entre D y E (z 156.5–161.5) y el del borde norte (z 219–224). Llegan al callejón del oeste de la copia (x ≈ −24); su valla de madera se abre en esos tres sitios.
- **Filas**: casas que dan a la calle norte (z = 90, donde antes había 5 two-flats lisos), casas a los dos lados de D y E, y dos bloques de apartamentos de 3 pisos al norte de E.
- **Solares abandonados con vegetación** (`abandonedParcelVegetacion.png`): uno en la esquina de D con la calle x = −110 y otro en medio de E sur, más los que salgan al azar. Sin valla, hierba larga por todo, árboles grandes y matorral al fondo, algún árbol seco, losas rotas del camino de la casa que hubo, un atajo de barro en diagonal y un poste de cartel torcido.

## Catálogo de estructuras

| Estructura | Medidas | Dónde | Notas |
|---|---|---|---|
| Bloque bajo | 24×9×12 m, 3 plantas | Barrio de viviendas | Tejado plano con alero, portal hacia la calle. |
| Torre | 30×24×15 m, 8 plantas | Barrio de viviendas | Detrás de los bloques bajos, en los huecos entre ellos. Camino largo hasta la acera. |
| Two-flat | 10×8×14 m (o 5 m de alto) | Alrededores | Todavía en color liso. |
| Tienda de esquina | 16×4.5×14 m | Alrededores | Letrero azul sin texto. |
| Nave de mantenimiento | 28 × 56 × 8 m (11 m en la cumbrera) | Cocheras | Ladrillo y chapa, tres portones con vías, se entra. |
| Contenedor | 2.44 × 2.59 × 12.2 (o 6.1) m | Cocheras | Pilas de hasta 3; palés al pie para subir. |
| Vagón aparcado | 2.9 × 4 × 14.6 m | Cocheras | El del elevado, apagado; trenes de 2 o 4. |
| Cancha | 12×20 m + 1.5 m de margen | Barrio de viviendas y oeste lejano | Malla de 3.5 m. |
| Parada de bus | 3.7×2.4×1.6 m | Acera norte del barrio | Marquesina con banco y anuncio. |
| Casita de ladrillo | 6.5–8.5 × 6 × 8–10.5 m, tejado a dos aguas | Barrio de casas | Hastial a la calle, escalones, garaje detrás si cabe. |
| Greystone | 6.5–8.5 × 6–9 × 8–10.5 m | Barrio de casas | Piedra gris, cornisa, escalinata de 0.6 m con barandilla. |
| Apartamentos de 3 pisos | 14–24 × 9.75 × ≤13 m | Barrio de casas | Miradores, portal de piedra, porches traseros de madera. |
| Solar con vegetación | 7–22 m de frente | Barrios de casas y ampliación oeste | Abierto, hierba alta, árboles grandes y matorral al fondo, losas rotas, atajo de barro. |
| Garaje | 5.6×2.8×6 m | Barrio de casas | Puerta al callejón, tejado plano o a dos aguas. |
| Tienda de esquina (casas) | 12×4.6×11 m | Barrio de casas, (182, 190) | Escaparate con rejas, letrero rojo. |
| Torre del conjunto | 27×15 m, 11–14 plantas (33–42 m) | Manzana central | Forjados vistos, balcones con malla detrás, portal con marquesina. |
| Fila de adosadas | 38–46 × 9 m, 2 plantas (6 m) | Manzana central | Casas de ~6.5 m, tejado gris a dos aguas, portales por parejas. |
| Aparcamiento | 40–44 × 17 o 34 m | Manzana central | Plazas de 2.6×5 m, entrada de 8 m desde la calle sur. |
| Coche aparcado | 4.9–5.4 × 1.95–2 m | Calles y aparcamientos | Sedán, coupé, todoterreno o furgoneta al azar, en el sentido de la marcha en la calle. |
| Local de negocio | 9–20 × 7.5 m, 1 planta (4.6 m) o 2 (7.4 m) | Franja de negocios y oeste lejano | Frente pintado, escaparate, letrero luminoso con texto genérico, rejas o persiana, toldo según el tipo. |
| Aparcamiento de clientes | 16 × 14.5 m | Franja de negocios | 6 plazas, malla, entrada desde la calle norte. |
| Gasolinera | Parcela 40 × 34 m; marquesina 22 × 12 m a 5.2 m | Oeste lejano, esquina calle norte / x = −110 | 4 surtidores en 2 islas, tienda FOOD MART de 17 m, poste de precios sin marca. |
| Descampado | ≈ 45 × 52 m | Oeste lejano, junto a J | Barro, escombros, malla caída, coche abandonado, bidón quemado. |
| Autopista hundida | 25 m de ancho, 7 m de hondo | Borde oeste, x −300…−275 | 2 × 3 carriles, mediana, muros de hormigón, puentes en z = ±80 y túnel al norte. |
| Puente sobre la autopista | 25 × 20 m, tablero de 1.2 m | z = ±80 | Vigas, pila en la mediana, pretil con malla. |
| Instituto (edificio principal) | 76 × 18 × 10.5 m, 3 plantas | Instituto | Ladrillo claro, portal de piedra, remate con placa. |
| Gimnasio | 24 × 34 × 10.8 m | Instituto | Ladrillo liso, banda de bloque de vidrio. |
| Campo de fútbol americano con pista | 133 × 65 m (campo 91 × 40.5 m) | Instituto | Escala 0.76 m/yd, pista de 6 calles. |
| Gradas | 50 × 6 m, 7 filas | Instituto | Aluminio, palco de prensa, se suben a pie. |
| Autobús escolar | 11.5 × 2.45 × 3 m | Aparcamiento del instituto | Amarillo, "SCHOOL BUS", sin marcas. |
| Estudio casero (sótano de T1) | 14.5 × 11.5 × 3 m, escalera de 2.6 m | Manzana central, oeste de T1 | Cabina de voz con micro; ahí se graban los temas. |
| Memorial | ≈ 3.2 × 2.3 m | Eje E–O, x ≈ −17.5 | Malla con foto, velas, peluches, flores y globos. |
| Zona de reunión | ≈ 9 × 3.6 m | Sur de la canasta (−26, 23.7) | Sillas de plástico, barbacoa de bidón, altavoz. |
| Pórtico del tren elevado | 2 pilares de 6.6 m, luz de 16.4 m | Avenida principal, cada 15 m | Sin pórtico donde cruzan calles. |
| Estación del tren elevado | 2 andenes de 64 m a 9.6 m de altura | Avenida principal, z 4–68 | Marquesinas, bancos y una escalera a cada acera. |
| Tren elevado | 4 vagones de 14.6×2.9 m | Vías en x 106.5 y 113.5 | Acero plateado con franja granate, sin logotipos. Se mueve. |

## Peticiones y comentarios

Registro de lo que se ha pedido sobre el mundo, del más antiguo al más reciente. Se usa para no repetir errores y mantener el tono.

- **2026-11-15:** «implementa todo esto» (NPC y facciones, por fases; los enemigos te atacan en su territorio, los aliados defienden su zona, Dunmore y Larkin aliados entre sí). → Implementado en `Scripts/NPC/`. *Para el mundo:* sitios de NPC en esquinas habituales, bancos, parada de bus, portales, puerta de cada local, cancha, plaza, memorial y bidón del descampado; los locales cierran de 21:00 a 8:00 con la persiana bajada (la FOOD & LIQUOR nunca), y la malla de navegación se genera con la escena. Capturas en `ingame/npc/v01_primeros_npc/`.

- **2026-11-15:** «¿qué facciones me recomiendas y cuáles serían sus barrios?» → Propuesta aceptada (detalle en [NPCS.md](NPCS.md), *Facciones y territorios*): Ash Block (manzana central, el jugador), Kessler Courts (barrio de viviendas, rival principal al otro lado de la vía), Larkin Street (casas del noreste, aliado de Kessler), Dunmore (casas del oeste, tregua frágil) y Crestline (oeste lejano, los mayores, neutral). Zonas neutrales: franja de negocios (la tienda de licores, en disputa), avenida y estación (frontera), instituto y cocheras. El sur queda para una sexta facción.
- **2026-11-15:** «como el mapa se tiene que ampliar, Kessler Courts es un set aliado y el oeste lejano también; los otros no». → Ash Block, Kessler y Crestline son aliados; Dunmore y Larkin, enemigos (Dunmore, el rival más cercano, frontera en la calle norte; Larkin, frontera con Kessler en el callejón z ≈ 95). La avenida y la estación pasan a ser paso seguro. Las zonas nuevas del mapa traerán más facciones rivales.
- **2026-11-15:** Comportamiento de los NPC según la hora y el tiempo (de día civiles y pandilleros tranquilos, de noche solo abre la tienda de licores y hay más incursiones) y **lugares icónicos o esquinas habituales** de cada grupo, aunque también se les encuentre por ahí. → Diseño en [NPCS.md](NPCS.md), sin programar todavía. *Para el mundo:* las esquinas, porches, la puerta de la tienda de licores, la zona de reunión y el memorial tendrán puntos de actividad (`ActivitySpot`), y algunos deben estar **a cubierto** para la lluvia.
- **2026-11-15:** «implementa un sistema de tiempo, el día está bien como está, habría que meter noche, lluvia y nieve, también un fondo de cielo más o menos grisáceo, y los ciclos de día y noche duran 20 min; la lluvia y la nieve son aleatorias pero que sea algo normal». → `WeatherSystem` con día de 20 min, noche con farolas de sodio (apagadas de día), lluvia y nieve de partículas al azar (casi siempre seco) y cielo gris degradado. Para que las farolas iluminen el suelo, el renderer pasa a Forward+ (con Forward solo 4 luces por objeto y las aceras largas se quedaban a oscuras). Capturas en `ingame/tiempo/`. *Regla:* las luces de calle llevan `NightLight` (ver *Hora y tiempo*).
- **2026-11-15:** «en la torre donde vive el protagonista quiero un interior que será un portal y desde ahí se coge el ascensor para ir a la casa; la casa en el piso más alto y que se vea la ciudad desde una ventana». → Vestíbulo en la planta baja de T2 (buzones, escalera, fluorescentes, dos ascensores) y piso en la esquina sureste de la planta 14, con dos ventanas al sur y una al este con cristal y vistas reales. La torre se construye con huecos (`TowerOpenings`). *Regla:* los interiores que necesitan vistas van dentro del edificio, no escondidos aparte.
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
- **2026-11-14**: «Permite subir a la estación de tren». → Las escaleras de la estación pasaron de decorativas a transitables (rampa invisible bajo los peldaños, 32° de pendiente, barandillas con colisión) y el andén tiene un límite en el borde de las vías.
- **2026-11-14**: «Mejorar el apartamento de 3 plantas con escaleras: que se pueda subir por las escaleras y que cada escalera dé a una puerta, no a una ventana». → El porche trasero se rehízo: terraza por piso con puerta (colocada sobre un vano de la fachada, así tapa su ventana), barandilla de balaustres y escalera en zigzag de dos bandas con rellanos, transitable (rampas y barandillas con colisión). *Regla que sale de aquí:* toda escalera o porche lleva a una puerta, y lo que parece transitable se puede pisar.
- **2026-11-14**: «No me deja subir por las escaleras, se queda bloqueado en las esquinas». → Las paredes invisibles de los lados de los tramos eran cajas inclinadas cuyas esquinas invadían los rellanos; se cambiaron por paredes verticales escalonadas que acaban en el borde del rellano (porches y estación).
- **2026-11-15**: «Amplía las zonas de las casas a la izquierda, añade una gasolinera, negocios por ahí, y algún descampado y basket». → Oeste lejano al otro lado de la calle x = −110 (`PrototypeSceneBuilder.FarWest.cs`): D, E, F y H siguen rectas hasta la nueva calle J; gasolinera en la esquina con la calle norte; alitas, empeños, belleza, neumáticos y lavandería frente a la calle norte; cancha de baloncesto; un descampado grande; casas, apartamentos y solares. Se quitaron los two-flats lisos del norte.
- **2026-11-15**: «Mejora las texturas de las carreteras principales». → Las cuatro calles grandes dejan de ser asfalto liso con una línea amarilla: textura `MainRoad` (4 carriles, rodadas, sal, grietas selladas, parches, baches, pintura gastada) en tres variantes, llegadas a los cruces con stop y paso de cebra, cruces sin pintura, y alcantarillas, sumideros, nieve en el bordillo, charcos y aceite (`PrototypeSceneBuilder.MainRoads.cs`). De paso se arregló que las aceras de enfrente (tiras de 600 m) atravesaban la avenida y la calle x = −110 con un bordillo en mitad de la calzada. *Regla:* ninguna acera atraviesa una calzada.
- **2026-11-15**: «Quiero que conectes esta carretera (marcado con la línea roja); tienen que tener cohesión y estar rectas» (`lineasrojascarreteraconectan.png`, sobre el plano). → Calle H en el borde norte, de la calle x = −110 a G; B' sigue recta hasta F; B sigue recta como G hasta H (se quitaron las barreras de esos extremos); F deja de tener quiebro y es una sola calle recta alineada con Fe al otro lado de la avenida. *Regla que sale de aquí:* calles rectas y con continuidad.
- **2026-11-15**: «Toda la parte de barrio de casas vamos a ampliarla más para adelante, es decir, en vertical». (Se había empezado a pedir más detalle en las carreteras; el usuario lo cambió por esto, así que el detalle de las calzadas queda pendiente). → Ampliación norte de los dos lados hasta z = 296 (`PrototypeSceneBuilder.HousesNorth.cs`): calles Fe y G en el este, calle F con quiebro y pasaje en el oeste, apartamentos, casas y solares con vegetación; se quitaron las vallas del antiguo borde norte.
- **2026-11-15**: «Amplía el barrio de casas oeste más a la izquierda; hay una imagen de parcela abandonada con vegetación, quiero que metas alguna que otra» (`abandonedParcelVegetacion.png`). → Ampliación oeste hasta la calle x = −110 (`PrototypeSceneBuilder.HousesWestExt.cs`): calles sin salida D y E, tres callejones que siguen los de la copia, casas frente a la calle norte en lugar de los two-flats lisos, dos bloques de apartamentos y solares con vegetación (`OvergrownLot`): dos fijos en la ampliación y, en todos los barrios de casas, la mitad de los solares al azar. La captura nueva `housewindowstop.png` (ventanas del hastial cortadas por el tejado) ya quedaba resuelta con los hastiales de ladrillo liso.
- **2026-11-15**: Capturas de fallos en `img/estrcuturebugs/` («mira las imágenes y arréglalos; aprovecha para mirar otros bugs parecidos en los edificios»): puerta de la greystone metida en la imposta y con barandillas flotando, portal del apartamento tapando media ventana y casa amarilla con la puerta sobre una ventana y ventanas del hastial cortadas por el tejado. → Puertas centradas en un vano con marco que tapa la ventana (casas, adosadas, puerta trasera de los locales), portal y miradores de los apartamentos alineados con los vanos, escalinata de la greystone más baja con barandilla con postes, hastiales de ladrillo liso con ventana de desván, y frente pintado en la tienda de esquina (asomaban trozos de ventana entre escaparate y letrero). *Regla que sale de aquí:* puertas y ventanas que encajan.
- **2026-11-15**: «Falta la valla y el edificio debería ser un pelín más alto para que encaje la puerta» (porche trasero del apartamento de 3 pisos). → Barandilla en el último rellano de la escalera, donde quedaba un hueco sobre el tramo de abajo, y antepecho de ladrillo liso de 0.75 m encima del 3.er piso para que la puerta trasera de arriba no asome por encima del muro.
- **2026-11-15**: «Pegada al borde oeste (x −200 a −175): una autopista hundida tipo Dan Ryan, en trinchera y con puentes en z = 80 y z = −80. En el resto (x −170 a −120): un instituto con campo de fútbol americano, pista y aparcamiento». El borde del mundo está en x −300 (el plano solo rotulaba desde −200) y en 50 m de ancho no cabían el campo con la pista, el edificio y el aparcamiento; se le preguntó y eligió poner la autopista en el borde real. → Autopista en trinchera en x −300…−275 (`PrototypeSceneBuilder.Highway.cs`) con puentes en z = ±80, túnel bajo el oeste lejano y tráfico decorativo (`HighwayCar`), e instituto en x −275…−120 (`PrototypeSceneBuilder.School.cs`). Se añaden las cifras a la fuente de los letreros y el plano rotula ahora desde x −300. *Reglas que salen de aquí:* comprobar que cabe y escala de juego en lo grande.
- **2026-11-15**: «Estudio casero en el sótano o en un piso de la torre T1 o T2. Puede ser el centro de la progresión musical. Manzana central (x −100 a 100, z −80 a 80): el "O-Block". Memorial en el eje peatonal E-O (cerca de x 0, z 0): globos, velas, peluches y una foto en la valla. Zona de reunión en el patio con la canasta (x −30, z 30): sillas de plástico, una barbacoa y un altavoz». Se pidió comprobar las coordenadas. (0, 0) cae dentro de T2 y el eje no tiene valla, así que el memorial va en el eje junto a T2, en x ≈ −17.5, con un tramo de malla propio para la foto. La reunión va al sur de la losa de la canasta, que está en (−26, 30). El estudio va en el sótano de T1: las torres no tienen interiores, y un sótano con escalera propia es lo más barato. La escalera se movió al norte de los contenedores de T1, porque en el plano chocaba con ellos. → `PrototypeSceneBuilder.Studio.cs` y la mecánica de grabación (`Scripts/Music/`).
- **2026-11-15**: «En una de las dos torres vamos a permitir acceder como a un portal donde habrá un ascensor que te lleva a un apartamento, un tp, donde de momento el personaje vivirá; ahí la idea es poder personalizar tu personaje con la ropa que tienes y una caja fuerte donde meter tu dinero o tu arma». En la manzana hay 3 torres; se le preguntó y eligió **T2**, y para la ropa, un **armario básico** (3–5 prendas por parte, sin tiendas todavía). → Ascensor en el portal de T2 con fundido a negro a un piso escondido bajo la torre (`PrototypeSceneBuilder.Apartment.cs`, `Scripts/Home/`): armario para cambiarse (el personaje pasa a tener cuerpo por piezas: gorra o gorro, sudadera, plumífero o camiseta, vaqueros o chándal, zapatillas o botas) y caja fuerte para dinero, armas y objetos, que se guarda entre partidas. *Regla que sale de aquí:* interiores aparte.
- **2026-11-15**: «Las vallas las debe poder sortear; las vallas que son verjas no tiene sentido que pueda cubrirse, en cambio en las esquinas de casas o coches sí. Tiene que poder sortear coches también, y los coches que no sean suyos los puede robar con una mecánica como de forzar». Las vallas no se saltaban porque el rayo que mide la altura caía 0.2 m pasada la cara y las vallas tienen 3 cm. → La altura se mide también pegada a la cara; las verjas, mallas y barandillas caladas (material con recorte) ya no cubren; los coches cubren y se cruzan por encima (`SlideOver`); todos los coches aparcados se pueden conducir y se roban manteniendo F para forzar la puerta (`VehicleLock`, a veces salta la alarma). *Regla que sale de aquí:* ampliada la de escalar y cubrirse.
- **2026-11-15**: Para la franja de two-flats al sur de la calle z = −80 (delante del inicio del jugador), el usuario propone una estación de tren o cocheras, contenedores o un parque grande. Ahora solo hay una fila de cajas lisas y, detrás, nada. Se le recomendaron unas **cocheras del tren elevado**, enlazadas con la línea de la avenida, con un rincón de contenedores, y dijo «pues hazlo». → Cocheras (`PrototypeSceneBuilder.RailYard.cs`): rampa desde la vía oeste del elevado, nave de mantenimiento, diez vías con trenes aparcados, escalera de agujas, patio de contenedores con palés para subir y un contenedor-escondite, entrada con caseta y barrera frente al inicio, valla con concertina y agujeros, torres de focos. Se quitaron los two-flats de enfrente de la manzana (quedan dos en la esquina oeste). *Regla que sale de aquí:* lo que está a la vista del inicio del jugador debe tener personalidad propia.
- **2026-11-15**: «Mejora los modelos del personaje; tienes una imagen de referencia llamada gang». No es una estructura, pero la imagen está en `img/`. → Se toma solo la ropa y las proporciones (nadie real, ni caras ni nombres). El cuerpo gana manos con palma, dedos y pulgar, hombros más anchos, mandíbula, bigote y perilla, cinturón y bajos del pantalón; prendas nuevas: camisetas XL de colores con faldón largo, polo de rayas, chaleco con camiseta, camisa vaquera, vaqueros claros anchos, pantalón caqui, gorro de pescador, pañuelo, y dos partes nuevas que pueden ir vacías, **cara** (gafas de sol) y **cuello** (cadenas). La ropa de inicio no cambia (sudadera, por el invierno); todo lo nuevo está en el armario del piso.
- **2026-11-15**: Mecánicas del personaje (no son estructuras, pero afectan a cómo se usan): «el personaje no salta, escala; se cubre en x estructuras». Espacio escala o salta por encima de vallas, muros y bordes, y Q cubre tras cualquier obstáculo visible de 0.85 m o más (coberturas bajas, agachado). Zona de prácticas con dianas y objetos en la acera sur de la manzana central (x −8.5…10, z −71.6), junto al inicio del jugador. *Regla que sale de aquí:* escalar y cubrirse.
- **2026-11-15**: «El diseño de personaje no me convence, tiene que ser la textura parecida a la img de gang». → Las texturas de ropa (`ProceduralTextures.Cloth`) pasan de tramas finas de 64 px (canalé, sarga) a texturas **pintadas** de 128 px como las de PS2: pliegues con cresta clara y surco oscuro, sombra horneada a manchas, costuras con pespunte y desgaste. Camiseta con caídas en diagonal y pliegues del faldón; vaquero con hebra vertical, desgaste claro y el bajo amontonado en arrugas en el tobillo; sudadera de felpa con puño de canalé; botas de nobuk con rozaduras y cuello acolchado. *Regla que sale de aquí:* texturas de personaje pintadas.
- **2026-11-15**: «La forma no me convence, más como en la img». → El cuerpo deja las cápsulas y esferas (aire de muñeco) y pasa a mallas facetadas por anillos, de proporciones PS2 como en `gang.png`: torso cuadrado y estrecho de cintura, camiseta XL que cae recta hasta medio muslo con mangas anchas hasta el codo, brazos largos y delgados, vaqueros anchos y rectos con el bajo amontonado en ondas, botas grandes con la puntera levantada, y cabeza ovalada con mandíbula y la cara **pintada** en la textura (se quitan los ojos, cejas, boca y barba de cubitos). *Regla que sale de aquí:* forma del personaje.
- **2026-11-15**: «Quiero que lo pruebes para que veas las animaciones del personaje, al cubrirse, andar, correr, apuntar, etc., y las hagas más realistas y arregles los bugs». No es una estructura, pero toca cómo se usan las coberturas y las vallas. → Se graban las animaciones con un grabador automático (`AnimationCapturePlayTests`, hojas en `ingame/animaciones/`). Arreglado: Q a un metro de la pared no entraba en cobertura; apuntando o en cobertura las piernas daban pasos hacia delante al ir de lado o hacia atrás. Más realista: trote, apuntar a la altura de la barbilla siguiendo la mira, cobertura alta de lado contra la pared, manos en el borde al escalar y una mano apoyada al saltar vallas.

## Pendiente e ideas

- NPC ([NPCS.md](NPCS.md)): hecho lo básico (sitios y horario de tiendas). Faltan sitios en los porches traseros y más sitios a cubierto para la lluvia (marquesinas, portales de Kessler), y pasos de cebra en la malla para que los civiles crucen por ellos.
- Más detalle en las calzadas de los barrios de casas (las calles grandes ya se hicieron el 2026-11-15): siguen con la textura `Road` sencilla, sin parches, alcantarillas ni pasos de cebra en las bocas.

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
