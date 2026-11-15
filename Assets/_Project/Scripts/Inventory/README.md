# Inventory: objetos, dinero y armas

## Lógica pura (`Inventory.cs`)
- `ItemDef`: id, nombre, descripción, tipo (`Weapon`, `Ammo`, `Consumable`), máximo por montón y color del icono. Los consumibles llevan `Heal`, `Stamina`, `Armor` y `StaminaBoostSeconds`; las armas, sus datos de tiro.
- `ItemCatalog`: todos los objetos, en código.

  | id | Objeto | Montón | Efecto |
  |---|---|---|---|
  | `pistola` | Pistola 9 mm (prueba) | 1 | 25 de daño, cargador de 12, 0.22 s entre tiros, 70 m |
  | `municion_9mm` | Munición 9 mm | 120 | Se usa al recargar |
  | `botiquin` | Botiquín | 3 | +50 de vida |
  | `alitas` | Alitas fritas | 5 | +15 de vida, +40 de energía |
  | `bebida_energetica` | Bebida energética | 5 | Energía llena y recuperación ×2.5 durante 20 s |
  | `chaleco` | Chaleco | 2 | +50 de chaleco |
  | `dinero` | (no es un objeto) | — | Va a `PlayerInventory.Money` |

- `Inventory`: casillas fijas. `Add` llena primero los montones abiertos y luego las casillas vacías, y devuelve cuántos han cabido. Además: `Remove` (empieza por las últimas casillas), `RemoveAt`, `Count`, `Has`, `SpaceFor`, `Weapons()` y el evento `Changed`.

## `PlayerInventory.cs`
- Empieza con `startingItems` ("id:cantidad"; por defecto, la pistola cargada, 36 balas, un botiquín y 2 de alitas) y `startingMoney` (150 $).
- Guarda el arma equipada (`EquippedId`; null = puños) y las balas del cargador de cada arma (`Magazine`, `Reload`).
- **Tab** abre el panel: 12 casillas con icono de color y cantidad. Se elige una con el ratón y se usa, equipa o guarda, o se tira. Mientras está abierto, el tiempo va a 0.2× (`slowMotionScale`), el cursor queda libre y `GameUi.MenuOpen` bloquea movimiento, cámara y combate. En el coche no se abre.
- **Rueda del ratón / cruceta**: cambia entre puños y las armas que se llevan.
- `Use(slot)`: los consumibles solo se gastan si hacen falta (vida, chaleco o energía no llenos).
- `Drop(slot)`: tira el montón entero al suelo (`ItemPickup.Spawn`). Las balas del cargador de un arma tirada vuelven al inventario.
- `Pickup(id, count)`: lo llama `ItemPickup`; devuelve lo que ha cabido.
- `SpendMoney(amount)` y `AddMoney(amount)`.
- `UnloadForStorage(slot)`: antes de guardar un arma en la caja fuerte de casa, sus balas del cargador vuelven al inventario y deja de estar en la mano.
- Con otro menú abierto (armario, caja fuerte) Tab no abre el inventario.
- Avisos (`Message`), que el HUD muestra.

## `ItemPickup.cs`
Objeto en el suelo que gira y flota. Se recoge al pasar por encima a pie (radio de 1.1 m); si no cabe entero, queda lo que sobra. Lo que se tira tarda 1.5 s en poder recogerse.

Tests: `Tests/EditMode/InventoryTests.cs`.

## Pendiente
- Tiendas y venta (el dinero ya se puede gastar con `SpendMoney`).
- Guardar el inventario entre partidas.
- Iconos de verdad en vez de recuadros de color.
