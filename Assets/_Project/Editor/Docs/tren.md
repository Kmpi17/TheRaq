# Tren elevado: `PrototypeSceneBuilder.Train.cs`
`BuildElevatedTrain` se llama en `Build()` después del barrio de casas. Va sobre la avenida principal (eje x = 110), de z −340 a 340, así que los extremos quedan escondidos en la niebla.

- **Pórticos** cada 15 m, de z −292.5 a 292.5. Cada uno tiene dos pilares de 6.6 m sobre las aceras (x 101.8 y 118.2) con zapata, una cartela y una viga transversal.
  - No se ponen pórticos donde cruza una calle (`TrainCrossings`: z ±80, 0, 122 y 188).
  - Uno de cada tres pilares es de acero oxidado.
- **Tablero continuo:**
  - 4 vigas longitudinales y 2 de borde;
  - traviesas con recorte (`Ties`) para ver a través;
  - raíles con la cara superior a `TrainRailTop` = 8.66 m;
  - pasarelas de rejilla y barandillas `VallaHierro`.
  - Las vías van en x 106.5 (hacia el sur) y 113.5 (hacia el norte).
- **Estación** (`BuildTrainStation`), de z 4 a 68: un andén lateral a cada lado (`PlatformTop` = 9.6 m) con:
  - borde amarillo, barandilla abierta junto a la escalera y pilares propios;
  - marquesina con fluorescentes emisivos, 3 bancos, cartel sin texto y papelera;
  - una escalera hasta la acera, por fuera del andén y bajando hacia el norte.

  Las escaleras se pueden subir a pie (`StationStairs`): 1.4 m de ancho y 32° de pendiente (`StairRunPerRise` = 1.6), por debajo del `slopeLimit` de 45° del CharacterController. Lo que se pisa es una rampa invisible (`ColliderBox`) bajo los peldaños; las barandillas de los lados y del rellano tienen colisión, y una pared invisible en el borde de cada andén impide bajar a las vías. Lo comprueba `StationStairsPlayTests`.
- **Trenes** (`BuildTrains`, `Train`): 2 trenes de 4 vagones de 14.6 m. Cada vagón tiene caja de acero, ventanas emisivas, franja granate, 4 puertas, bajos y bogies; en los extremos del tren van la luna y los faros, y entre vagones, fuelles.
  - Se crean en la raíz de la escena, **fuera de `Mundo`**, porque se mueven y no pueden ser estáticos.
  - Llevan `ElevatedTrain` (`Scripts/World/`), configurado para que paren con el centro en la estación. El del norte empieza en la distancia 120, para desincronizarlo del otro.

| Material | Notas |
|---|---|
| `AceroEstructura`, `AceroOxidado`, `Rail`, `Rejilla` | Estructura. |
| `Traviesas` | `Ties`, con recorte. |
| `AndenHormigon`, `BordeAnden`, `MarquesinaEstacion`, `LuzAnden` (emisivo) | Estación. |
| `AceroTren` (metálico), `BajosTren`, `VentanasTren` y `FaroTren` (emisivos), `PuertasTren`, `FranjaTren` | Trenes. |
