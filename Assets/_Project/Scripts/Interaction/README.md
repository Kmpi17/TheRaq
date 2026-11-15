# Interaction: `VehicleEntry.cs`

Va en el **jugador**. Gestiona subir y bajar de los coches con `Interact` (F).

## Flujo
- **A pie**: cada fotograma, `FindNearest` busca el `VehicleController` libre más cercano dentro de `enterRange` (3.5 m) en `VehicleController.All` (`Nearby`, para el aviso del HUD).
  - **F** con un coche abierto (el del jugador, o uno ya robado): `EnterVehicle`.
  - **F** con un coche ajeno (`VehicleLock` cerrado): empieza a **forzar la puerta** (`Forcing`, `ForceProgress`). Bloquea el movimiento y se gira hacia el coche. Hay que **mantener F** los `forceSeconds` del coche (2.5 s); suena un traqueteo. Si se suelta F, se abre el inventario, se muere o alguien se lleva el coche, se pierde lo forzado. Al terminar, la cerradura queda abierta para siempre, a veces salta la alarma (35 %, 10 s) y entra.
- **F conduciendo**: llama a `ExitVehicle`.

### `EnterVehicle(vehicle)`
No hace nada si ya está conduciendo o si el coche tiene conductor. Si no:
1. Marca el coche con `vehicle.SetDriver(true)`.
2. Desactiva el `CharacterController`, el `PlayerMovement` y todos los `Renderer` hijos. El GameObject **sigue activo** para poder detectar F y salir.
3. Emparenta el jugador al coche en la posición (0,0,0) local.
4. Cambia la cámara con `followCamera.SetTarget(vehicle, Vehicle)`.

### `ExitVehicle()`
1. Marca el coche con `SetDriver(false)`, con lo que queda frenado.
2. Desemparenta al jugador y lo coloca en `vehicle.ExitPoint`, solo con el yaw.
3. Reactiva el control a pie y los renderers.
4. Devuelve la cámara al jugador en modo `OnFoot`.

## API pública
- `CurrentVehicle`, `IsDriving`
- `EnterVehicle(VehicleController)`, `ExitVehicle()`
- `static FindNearest(Vector3 from, IEnumerable<VehicleController>, float range)`: es pura, se puede testear sin escena.

`followCamera` se busca con `FindAnyObjectByType<ThirdPersonCamera>()` si no está asignada.

## Limitaciones conocidas (prototipo)
- No comprueba si el `ExitPoint` está dentro de una pared.
- No hay animación de entrar o salir, ni puertas.
- La búsqueda recorre `VehicleController.All` cada fotograma (unos 50 coches). Con mucho tráfico convendrá un `OverlapSphere` o una rejilla.
- Robar no tiene consecuencias todavía (no hay policía ni testigos); la alarma solo suena.

## Bloqueos
No se sube al coche con el inventario abierto (`GameUi.MenuOpen`) ni con `PlayerMovement.Locked` (escalando, en cobertura o muerto). Al subir se apaga el CharacterController: la cobertura se suelta sola y el combate, la escalada y el HUD de mira dejan de actuar.

## `Interactable.cs` y `PlayerInteractor.cs`
Cosas que se usan con F a pie, aparte de los coches (ascensor, armario, caja fuerte: ver `Scripts/Home/`).
- `Interactable` (abstracta): `Radius`, `Prompt` (texto del aviso) e `Interact(player)`. Se apuntan en `Interactable.All`. `FindNearest(from, candidates)` da el más cercano dentro de su radio, en horizontal y a menos de 2 m de altura (es pura, se testea sin escena).
- `PlayerInteractor` (en el jugador): cada fotograma, a pie y sin menú, elige `Focused` (el HUD enseña su `Prompt`) y lo usa con F. `Teleport(destino)`: fundido a negro (0.35 s), mueve al jugador, gira la cámara (`ThirdPersonCamera.SetYaw`), 0.5 s en negro y vuelve la imagen; mientras, `PlayerMovement.Locked`.
- **Prioridad de F**: con un `Interactable` a mano, mientras se viaja o en el fotograma en que se usó (`UsedFrame`) o se cerró un panel (`MenuInteractable.LastClosedFrame`), `VehicleEntry` no hace nada.
