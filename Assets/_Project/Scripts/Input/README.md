# Input: `GameInput.cs`

Todas las acciones del juego, definidas **en código** con el Input System. No hay asset `.inputactions`.

## Uso
```csharp
var input = GameInput.Actions;            // singleton perezoso, ya activado
Vector2 move = input.Move.ReadValue<Vector2>();
if (input.Interact.WasPressedThisFrame()) { ... }
```

## Acciones

| Acción | Tipo | Bindings | Quién la usa |
|---|---|---|---|
| `Move` | Vector2 | WASD, flechas, stick izquierdo | `PlayerMovement`, `VehicleController` |
| `LookMouse` | Vector2 | `<Mouse>/delta` | `ThirdPersonCamera` (× sensibilidad) |
| `LookStick` | Vector2 | stick derecho | `ThirdPersonCamera` (× velocidad × dt) |
| `Jump` | Button | Espacio, buttonSouth | `PlayerMovement` |
| `Sprint` | Button | Shift izquierdo, buttonEast | `PlayerMovement` |
| `Interact` | Button | F, buttonNorth | `VehicleEntry` |
| `Handbrake` | Button | Espacio, rightShoulder | `VehicleController` |

La vista va en dos acciones separadas porque el ratón da un delta en píxeles y el stick da una velocidad. Por eso se escalan de forma distinta.

## Ciclo de vida
- `GameInput.Reset()` destruye las acciones; la siguiente llamada a `Actions` crea otras nuevas.
- También se resetea solo al entrar en Play (`RuntimeInitializeOnLoadMethod(SubsystemRegistration)`), aunque el domain reload esté desactivado.
- **Tests con `InputTestFixture`**: llama a `GameInput.Reset()` después de `base.Setup()` y antes de `base.TearDown()`. Si no, las acciones quedan atadas a dispositivos que ya no existen.

## Añadir una acción
1. Declara el campo `public readonly InputAction X;`.
2. Créalo en el constructor. Para botones, usa el helper `Button(name, bindings...)`.
3. Añádelo al array `all`, que se encarga de activarlo y destruirlo.
