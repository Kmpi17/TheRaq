# 4. Facciones y territorios (2026-11-15)

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
- **Sur** (two-flats de la calle sur y el suroeste vacío) y **las zonas nuevas**: libres para las facciones que vengan con la ampliación. El **barrio norte** (z 298…500: bulevar, KINGSLEY PLAZA, HOLLOWAY PARK, bloque con patio, torre suelta) ya está construido y sin facción; de momento solo tiene sitios de paso para civiles.

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
