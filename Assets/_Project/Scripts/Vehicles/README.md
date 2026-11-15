# Vehicles: `VehicleController.cs` y `VehicleLock.cs`

Coche físico: `Rigidbody` (`[RequireComponent]`) con `WheelCollider` por rueda.

## Estado
- `HasDriver`: solo con conductor se lee el input. Sin conductor las ruedas quedan frenadas con `brakeTorque`.
- `SetDriver(bool)`: lo llama `VehicleEntry`.
- `ExitPoint`: dónde aparece el jugador al bajar. Si no está asignado, es el propio transform.
- `All`: lista estática de los coches activos (se apuntan en `OnEnable`), para que `VehicleEntry` busque el más cercano.
- `ForwardSpeed`: velocidad con signo en m/s a lo largo de `forward` (usa `Rigidbody.linearVelocity`, la API de Unity 6).

## Conducción (`FixedUpdate`)
- `Move.y` es el acelerador y `Move.x` la dirección.
- Si el acelerador va en contra del sentido de la marcha (|v| > 1 m/s), **frena** en vez de acelerar. Una vez casi parado, ese mismo input pone marcha atrás.
- El ángulo de giro se reduce con la velocidad: pasa de `maxSteerAngle` (32°) a `steerAngleAtSpeed` (10°) a 30 m/s.
- `Handbrake` aplica `handbrakeTorque` solo a las ruedas marcadas con `handbrake`.
- En `Update`, los visuales de las ruedas copian `GetWorldPose` de su collider.

## Ruedas (`Wheel`)

| Campo | Significado |
|---|---|
| `collider` | El `WheelCollider` |
| `visual` | Pivote visual; su eje X tiene que ser el eje de giro (el cilindro va rotado 90° en Z dentro del pivote) |
| `steer` / `drive` / `handbrake` | Si gira, si recibe tracción y si le afecta el freno de mano |

El coche del prototipo tiene **tracción trasera**: dirección en las delanteras, tracción y freno de mano en las traseras.

## Valores del prototipo
Masa según el modelo (sedán 1400 kg, coupé 1350, todoterreno 1900, furgoneta 2000) y centro de masas en (0, 0.4, 0). Ruedas de radio 0.38 m, suspensión de 0.25 m, muelle 35000 y amortiguador 4500.

## Modelo
Todos los coches que se conducen salen de `PrototypeSceneBuilder.MakeDrivable` (en `Editor/PrototypeSceneBuilder.Cars.cs`): el del jugador y **todos los aparcados**. El modelo visual del coche del jugador es el sedán de `Editor/PrototypeSceneBuilder.Cars.cs` (`CarModel` y `CarWheel`), y la física es la de este script. Las ruedas visuales son pivotes con el eje X como eje de giro; sus radios dejan ver que giran.

## Crear un coche
- **Desde código de editor**: añade el componente y llama a `Configure(Wheel[], Transform exitPoint)`. Es lo que hace `PrototypeSceneBuilder.MakeDrivable`.
- **A mano**: rellena `wheels` y `exitPoint` en el inspector.

En los dos casos, pon el coche en la **capa 6 (`Vehiculo`, `GameLayers.Vehicle`)**: la cámara no choca con él, pero se escala, se cubre y recibe balas. Si sus colliders no están en objetos con `Renderer`, añade `SolidVisual` en la raíz para que cuenten como visibles.

## `VehicleLock.cs`
Cerradura de los coches que no son del jugador (los aparcados la llevan; el del jugador, no). Cerrado, no se sube sin más: `VehicleEntry` llama a `Force(dt)` mientras se mantiene F, y devuelve true al abrirse tras `forceSeconds` (2.5 s). `ResetForce()` pierde lo forzado. `Progress` (0–1) para la barra del HUD. Al abrirse queda abierto y, con `alarmChance` (35 %), suena la alarma (`ProceduralSfx.CarAlarm`) `alarmSeconds` (10 s). `Configure(locked, seconds, alarmChance)` lo usan el generador y los tests.
