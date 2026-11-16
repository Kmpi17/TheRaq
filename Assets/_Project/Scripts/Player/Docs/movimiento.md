# `PlayerMovement.cs`
Movimiento realista pero arcade. Necesita un `CharacterController`; `PlayerVitals` y `Health` son opcionales (sin ellos no hay energía ni daño por caída, como en los tests).
- La velocidad horizontal va hacia la pedida con inercia (`MovementMath.Accelerate`): arranca con `acceleration`, frena con `deceleration` (más fuerte) y pasa de carrera a esprint más despacio (`sprintAcceleration`). En el aire casi no hay control (`airControl`).
- Gira rápido parado y más despacio cuanto más deprisa va (`MovementMath.TurnSpeed`, de `fastTurn` a `slowTurn`).
- Esprintar gasta energía (`sprintStaminaPerSecond`). Agotado no esprinta y va al 75 % de la carrera.
- **No salta.** Espacio es escalar (`PlayerClimb`).
- **Agacharse (C, o pulsar el stick izquierdo)**: alterna agachado y de pie. Solo se agacha en el suelo (vale haberlo tocado hace menos de 0.2 s, porque `isGrounded` parpadea). Agachado:
  - va a `crouchSpeed` (1.7 m/s, también apuntando) y no esprinta; pulsar Shift moviéndose lo levanta;
  - el CharacterController baja a `crouchHeight` (1.15 m, como en la cobertura baja), así que cabe por huecos bajos;
  - apunta mejor (dispersión ×0.7, `CombatMath.Spread`) y la cámara baja con él (`ThirdPersonCamera`);
  - solo se levanta si hay sitio encima (`Physics.CheckCapsule` hasta el alto de pie, con `standMask`); si no, el HUD avisa *No hay sitio para levantarse*.
  - Escalar (`PlayerClimb`, que mira el sitio con el alto de pie), morir, reaparecer y subir al coche lo ponen de pie (`ForceStand`). En cobertura manda `PlayerCover` (pone su alto y lo deja de pie al salir); al salir, `PlayerMovement` lo vuelve a agachar si seguía agachado.
- **Daño por caída**: al tocar suelo, `MovementMath.FallDamage` según la velocidad de impacto (hasta unos 4 m no hace nada) y frena en seco.
- Los empujones (`AddImpulse`) se suman y se apagan solos.
- **API para las otras habilidades:**
  - `Locked`: otra habilidad manda (escalar, cobertura, muerte) y aquí no se mueve nada.
  - `Aiming` + `AimYaw`: va a `walkSpeed`, no esprinta y mira hacia donde apunta la cámara.
  - `InputDirection`, `PlanarVelocity`, `IsSprinting`, `ResetMotion()`, `SetPlanarVelocity()`.
  - `Crouching`, `SetCrouching(bool)` (devuelve si quedó como se pidió; levantarse falla sin sitio), `ForceStand()`, `StandHeight`, `StandBlockedTime`.

| Campo | Defecto |
|---|---|
| `walkSpeed` (apuntando) / `runSpeed` / `sprintSpeed` | 2.2 / 4.4 / 7.2 m/s |
| `acceleration` / `deceleration` / `sprintAcceleration` | 16 / 24 / 6 m/s² |
| `fastTurn` / `slowTurn` | 900 / 280 °/s |
| `gravity` | −20 m/s² |
| `sprintStaminaPerSecond` | 12 |
| `crouchSpeed` / `crouchHeight` | 1.7 m/s / 1.15 m |

## `MovementMath.cs`
Funciones puras, testeadas en `Tests/EditMode/MovementMathTests.cs` y `CharacterSystemsTests.cs`:
- `CameraRelative(input, cameraYaw)`: dirección horizontal en el mundo, con longitud ≤ 1.
- `Accelerate(current, target, acceleration, deceleration, dt)`, `TurnSpeed(...)`, `FallDamage(impactSpeed)`.
- `JumpVelocity`: ya no se usa (no hay salto). Se deja por si vuelve.
- `ClimbMath`: `Classify`, `ClassifyVehicle` (cruzar coches), `Duration` y `StaminaCost`, con las alturas límite en constantes.

La lógica nueva que no dependa del motor va aquí o en otra clase estática parecida, para poder testearla sin Play.
