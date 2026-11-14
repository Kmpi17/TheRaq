# Vehicles: `VehicleController.cs`

Coche físico: `Rigidbody` (`[RequireComponent]`) con `WheelCollider` por rueda.

## Estado
- `HasDriver`: solo con conductor se lee el input. Sin conductor las ruedas quedan frenadas con `brakeTorque`.
- `SetDriver(bool)`: lo llama `VehicleEntry`.
- `ExitPoint`: dónde aparece el jugador al bajar. Si no está asignado, es el propio transform.
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
Masa 1400 kg y centro de masas en (0, 0.4, 0). Ruedas de radio 0.38 m, suspensión de 0.25 m, muelle 35000 y amortiguador 4500.

## Modelo
El modelo visual del coche del prototipo es el sedán de `Editor/PrototypeSceneBuilder.Cars.cs` (`CarModel` y `CarWheel`), y la física es la de este script. Las ruedas visuales son pivotes con el eje X como eje de giro; sus radios dejan ver que giran.

## Crear un coche
- **Desde código de editor**: añade el componente y llama a `Configure(Wheel[], Transform exitPoint)`. Es lo que hace `PrototypeSceneBuilder.BuildCar`.
- **A mano**: rellena `wheels` y `exitPoint` en el inspector.

En los dos casos, pon el coche en la capa 2 (ver Camera/README).
