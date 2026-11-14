# Interaction: `VehicleEntry.cs`

Va en el **jugador**. Gestiona subir y bajar de los coches con `Interact` (F).

## Flujo
- **F estando a pie**: `FindNearest` busca el `VehicleController` libre más cercano dentro de `enterRange` (3.5 m) y llama a `EnterVehicle`.
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
- La búsqueda con `FindObjectsByType` al pulsar F vale para pocos coches. Con tráfico habrá que usar un registro de vehículos o un `OverlapSphere`.
