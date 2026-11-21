# Mapa (metros, Y arriba)

**Plano detallado: [`mapa/mapa.png`](../../mapa/mapa.png).** Es la vista cenital a escala, con cuadrícula de 20 m, coordenadas y leyenda, y la referencia para ir modificando el mapa. Se genera con `python mapa/generar_mapa.py` y se regenera cada vez que cambia la disposición del mundo. Lo que sale al azar (árboles del césped, solares, basura) no aparece con su posición exacta. Abajo, el esquema rápido:

```
        z+  ═══ Terraplén del tren de mercancías (z 499.5), pasos inferiores en x = ±110 ═══
            Barrio norte, z∈[298,500], x∈[-300,300]: W PRESCOTT ST (z 491), bulevar W KINGSLEY
            BLVD (z 395), diagonal, KINGSLEY PLAZA x∈[-32,100] z∈[409,483], parque x∈[-100,-48],
            bloque con patio, torre suelta (262, 340), iglesia (250, 459), rotonda (160, 440)
            ┌───────────────────────────────┐ ┌────────────────────┐
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
 Autopista hundida x∈[-300,-275]: de z −440 a z 90 (túneles debajo del oeste lejano y, al sur, bajo el canal)
 Cocheras del tren elevado x∈[-99,100] z∈[-296,-91.5]: al sur de la calle z = −80, delante del inicio
 Polígono industrial del sur, z∈[-485,-90]: W GANTRY ST (z −319), W DOCK ST (z −475), vía de mercancías
 (z −460), curva S WHARF RD (centro (5,−365), r 45), desguace x∈[-247,-122], fábrica y chimenea x∈[-100,-48],
 almacén dentro de la curva, frigorífico x∈[140,240], estación sur del elevado z∈[-390,-330]
 ═══ Canal z∈[-510,-485]: puente levadizo en x = −110, puente fijo cortado en x = 110 ═══
                z−
```
