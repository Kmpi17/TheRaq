# `PlayerVitals.cs`
Energía (`StaminaPool`, ver `Scripts/Combat/`) y salud del jugador (con `Health`).
- La vida baja se recupera sola hasta el 25 % tras 8 s sin daño.
- Al morir: sale de la cobertura, bloquea el movimiento y tumba el `Modelo`. A los `respawnSeconds` (5) reaparece donde empezó, con la vida y la energía llenas, y paga la factura del hospital (`hospitalBill`, 100 $, o lo que tenga).

## `PlayerHud.cs`
HUD provisional con OnGUI:
- barras de vida, chaleco y energía (naranja si está agotado, verde con la bebida energética);
- arma, balas (cargador / reserva) y dinero;
- mira cuyo hueco es la dispersión real, y marca de impacto (roja si mata);
- barra de recarga;
- avisos de escalar, cruzar coches, cubrirse, agachado (*Agachado · C: levantarse*, y *No hay sitio para levantarse*) y subir o robar un coche (con la barra de forzar la puerta);
- hora y tiempo (`WeatherSystem`) y, debajo, el barrio en el que estás (`NpcPopulation.TerritoryLabel`: verde si es tuyo o aliado, rojo si es enemigo, gris si es zona neutral);
- avisos de los NPC arriba en el centro (te han reconocido, incursión);
- destello rojo al recibir daño y pantalla de *HOSPITALIZADO*.
