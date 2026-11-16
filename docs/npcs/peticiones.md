# 1. Lo que pide el usuario (2026-11-15)

| Momento | Civiles | Pandilleros | Tiendas | Incursiones de bloques rivales |
|---|---|---|---|---|
| **Día** | Muchos y de todo tipo | Vendiendo o pasando el rato | Abiertas | Probabilidad **baja** |
| **Noche** | Casi ninguno | **Más activos**: venden más y hacen incursiones | Cerradas, **salvo las de licores** | Probabilidad **alta** |
| **Lluvia / nieve** | Todavía menos; los que hay se refugian | Parecido, pero las reglas siguen siendo las del día o de la noche | Igual que sin lluvia | Siguen la regla de día o de noche, algo más bajas |

- **Lugares icónicos y esquinas habituales:** cada grupo tiene sus sitios (una esquina, la puerta de la tienda de licores, la zona de reunión, el memorial, un porche), donde se le suele encontrar. Pero no son exclusivos: también se le puede encontrar por otras partes del barrio.
- El objetivo es que el comportamiento se sienta **humano y creíble**, no un decorado que se repite.

- **2026-11-16:** «el comportamiento de vida va a ser para personaje tanto enemigo como aliado, hay que hacerlo relativamente realista: 1 en la cabeza, muerte; dos en el pecho, lo mismo; en pierna y brazos hasta 6; luego ya haces el cálculo según las balas; si tienes chaleco aguantas más». → Daño por zonas (`DamageModel` en `Combat/`) para el jugador y todos los NPC. El daño del arma es el de una bala en el pecho (9 mm: 50); la cabeza mata siempre y brazos y piernas reciben ×0.34. El chaleco solo protege el tronco. Algunos pandilleros llevan chaleco (más en las incursiones).
