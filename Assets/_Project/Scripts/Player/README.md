# Player: movimiento a pie

## `PlayerMovement.cs`
Necesita un `CharacterController` (`[RequireComponent]`). Cada `Update`:
1. Lee `Move` y lo convierte en una dirección relativa al yaw de la cámara (`MovementMath.CameraRelative`).
2. Elige la velocidad: `walkSpeed`, o `sprintSpeed` si se mantiene `Sprint`.
3. Si está en el suelo, fija la velocidad vertical en −2 (así `isGrounded` no parpadea en rampas) y salta si se pulsa `Jump`.
4. Aplica la gravedad y llama a `controller.Move`.
5. Gira el cuerpo hacia la dirección de movimiento a `turnSpeed` °/s.

| Campo | Defecto | Notas |
|---|---|---|
| `walkSpeed` | 4 m/s | |
| `sprintSpeed` | 7.5 m/s | |
| `jumpHeight` | 1.2 m | |
| `gravity` | −20 m/s² | Más fuerte que la real para que el salto se sienta más firme |
| `turnSpeed` | 720 °/s | |
| `cameraTransform` | — | Si está vacío usa `Camera.main`; si no hay cámara, usa los ejes del mundo (útil en tests) |

`VehicleEntry` **desactiva** este componente al subir al coche. `OnEnable` pone a cero la velocidad vertical.

## `MovementMath.cs`
Funciones puras y estáticas, testeadas en `Tests/EditMode/MovementMathTests.cs`:
- `CameraRelative(Vector2 input, float cameraYaw)`: devuelve la dirección horizontal en el mundo, con longitud ≤ 1.
- `JumpVelocity(float height, float gravity)`: devuelve `√(2·h·−g)`.

La lógica nueva que no dependa del motor (cálculos, reglas) va aquí o en otra clase estática similar, para poder testearla sin Play.
