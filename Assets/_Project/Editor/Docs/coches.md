# Coches: `PrototypeSceneBuilder.Cars.cs`
Coches low-poly genéricos de finales de los 90 y los 2000, **sin marcas ni logotipos** y con matrículas en blanco. Referencias: los coches de `apartaments.jfif` y `trainavenue.avif`.

- **Modelos** (`CarStyle` y `Spec`): `Sedan` (5.1 m, el grande de tracción trasera), `Coupe` (4.95 m, de dos puertas, sin montante central), `Suv` (4.9 m, alto, con barras de techo) y `Van` (5.4 m, furgoneta con puerta corredera).
- **`CarModel(parent, name, pos, yaw, style, paint, rng, wheels, weathered, lightsOn, collider)`**: el morro va hacia +Z local y el suelo queda en y = 0.
  - La carrocería y el habitáculo son mallas extruidas desde el perfil lateral (`ProfileMesh`, sombreado plano). El habitáculo tiene dos submallas: el cristal y el techo en el color de la pintura. Las mallas se guardan en caché por modelo (`carMeshes`).
  - Debajo de la línea de los pasos de rueda van faldones entre las ruedas, y el paso de rueda es una caja oscura para que no se vea a través.
  - Lleva montantes, moldura de cintura, moldura lateral, retrovisores, juntas de puertas y tiradores. También paragolpes y calandra (cromados en el sedán y el coupé), faros, pilotos (verticales en el todoterreno y la furgoneta), matrículas y antena (en el sedán).
  - Con `weathered`, puede llevar nieve en el techo (35 %, a veces también en el capó), óxido junto a los pasos de rueda (15 %), una puerta en imprimación (10 %) o una ventana tapada con plástico (6 %).
  - Tiene dos `BoxCollider`: la parte baja y el habitáculo.
- **`CarWheel`**: neumático, llanta con borde y fondo oscuro, 5 radios y buje. El eje de giro es el X del pivote, como pide `VehicleController`. Las llantas son cromadas (45 %, 15 % en furgonetas), grises o negras.
- **`ParkedCar(parent, ground, yaw, rng)`**: modelo al azar (sedán 40 %, coupé 15 %, todoterreno 30 % y furgoneta 15 %) y pintura al azar. Mantiene el convenio de antes: el largo va a lo largo de X local antes de girar, y con yaw 0 el morro mira a +X. Los aparcados en la calle van en el sentido de la marcha (se circula por la derecha).
- El coche abandonado del barrio (`BuildWreck`) es el sedán en `PinturaOxido`, sin ruedas, sobre bloques y con el capó abierto.

| Material | Notas |
|---|---|
| `Carroceria*` (Negra, Blanca, Granate, Azul, Plata, Verde, Beige, Gris), `CarroceriaJugador` | Brillo 0.45–0.65 y algo metálicos. |
| `CristalCoche`, `CromoCoche`, `PlasticoNegro`, `LlantaGris`, `Neumatico`, `PasoRueda` | |
| `FaroCoche`, `FaroCocheEncendido` (emisivo), `PilotoTrasero`, `Matricula` | |
| `Imprimacion`, `PinturaOxido`, `NieveCoche`, `PlasticoVentana` | Desgaste. |
