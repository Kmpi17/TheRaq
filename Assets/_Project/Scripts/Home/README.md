# Home: la casa del personaje

El piso del jugador, en la planta 14 de T2, al que se sube en ascensor desde el vestíbulo de la torre. Lo construye `Editor/PrototypeSceneBuilder.Apartment.cs`. Todo se usa con **F** a pie, a través de `PlayerInteractor` (ver `Scripts/Interaction/`).

## `Elevator.cs`
Puerta de ascensor (`Interactable`). F lleva al jugador a `Destination` con un fundido a negro (`PlayerInteractor.Teleport`). Hay dos:
- `Ascensor_Vestibulo`, delante de los ascensores del vestíbulo de T2 (mundo (2.7, 0.25, 0.6)), radio 1.7: sube al piso (`LlegadaPiso`, mirando al este).
- `Ascensor_Piso`, delante de las puertas del ascensor del piso, radio 1.3: baja al vestíbulo (`LlegadaVestibulo`, mirando al portal).

`Configure(destination, prompt, radius)` lo usa el generador.

## `MenuInteractable.cs`
Base de los muebles con panel. Al abrir: `GameUi.MenuOpen` (bloquea movimiento, cámara, combate e inventario) y cursor libre; el tiempo no se frena. Se cierra con F, Esc o si el jugador se aleja. `LastClosedFrame` evita que el mismo F que cierra vuelva a abrir algo o suba a un coche. Los paneles son OnGUI provisional.

## `Wardrobe.cs` (armario)
Panel con la ropa que tiene el jugador, por partes: cabeza (o nada), torso, piernas y pies. Un clic se la pone al momento (`PlayerOutfit.Outfit.Equip`). Mientras está abierto, el personaje mira a la cámara para verse de frente.

## `SafeBox.cs` (caja fuerte) y `Stash.cs`
- `Stash` (lógica pura): 16 casillas (`Inventory`) y dinero. `Transfer(from, slot, to)` pasa un montón entero (lo que quepa). `Serialize` / `Deserialize`.
- `SafeBox`: panel con el inventario a la izquierda y la caja a la derecha. Un clic en una casilla la pasa al otro lado; las armas se guardan descargadas (`PlayerInventory.UnloadForStorage`: las balas del cargador vuelven al inventario). Botones para guardar o sacar 10 $, 100 $ o todo.
- Lo de la caja no se pierde al morir (la factura del hospital cobra lo que se lleva encima). Se guarda entre partidas en PlayerPrefs (`casa.caja`) al cerrar el panel.

## Limitaciones
- El inventario del jugador no se guarda entre partidas, pero la caja y la ropa sí.
- No hay tiendas de ropa: el armario empieza con todo el catálogo (`ClothingCatalog`). El panel del armario va por partes (cabeza, torso, piernas, pies, cara y cuello), con las prendas en varias líneas si no caben y "Nada" en las partes que pueden ir vacías.
- Al morir se reaparece donde se empezó, no en casa.

Tests: `Tests/EditMode/HomeTests.cs`.
