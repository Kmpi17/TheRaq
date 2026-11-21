# Vehicles: `VehicleController.cs` y `VehicleLock.cs`

Coche físico: `Rigidbody` (`[RequireComponent]`) con `WheelCollider` por rueda.

## Estado
- `HasDriver`: solo con conductor se lee el input. Sin conductor las ruedas quedan frenadas con `brakeTorque`.
- `SetDriver(bool)`: lo llama `VehicleEntry`.
- `ExitPoint`: dónde aparece el jugador al bajar. Si no está asignado, es el propio transform.
- `All`: lista estática de los coches activos (se apuntan en `OnEnable`), para que `VehicleEntry` busque el más cercano.
- `ForwardSpeed`: velocidad con signo en m/s a lo largo de `forward` (usa `Rigidbody.linearVelocity`, la API de Unity 6).

## Conducción (`FixedUpdate`)
Petición del usuario (2026-11-17): «cuesta mucho girar». El coche lleva ayudas tipo arcade (como un GTA), con los números en `VehicleMath` (lógica pura):
- `Move.y` es el acelerador y `Move.x` la dirección. Si el acelerador va en contra del sentido de la marcha (|v| > 1 m/s), **frena** en vez de acelerar; casi parado, ese mismo input pone marcha atrás.
- **Volante suavizado** (`VehicleMath.SmoothSteer`): con el teclado va al tope en ~0.3 s (`steerRate` 3.5/s) y vuelve al centro o cambia de lado más deprisa (`steerReturnRate` 6/s). `Steer` lo expone.
- **Ángulo de las ruedas** (`SteerLimit`): 36° parado, bajando hasta 15° a 32 m/s (antes, 32° → 10° a 30 m/s).
- **Agarre**: rigidez de los neumáticos más alta (`forwardGrip` 1.6, delanteras 2.1, traseras 1.9) y, con el freno de mano, las traseras bajan a 0.7 para derrapar.
- **Ayuda al giro** (`yawAssist`): empuja la rotación del coche hacia la que pide el volante según el modelo de bicicleta (`TargetYawRate`: v·tan(δ)/batalla, como mucho 2.3 rad/s). Sin ella el coche se iba de morro. No actúa con el freno de mano (para poder cruzarlo) ni casi parado.
- **Agarre lateral** (`lateralGrip` 2.2/s): frena el deslizamiento de lado; con el freno de mano, muy poco.
- **Apoyo aerodinámico** (`downforce`, ∝ v²) y **barras estabilizadoras** (`antiRoll` 7000) en cada eje: menos balanceo y menos vuelcos.
- `Handbrake` aplica `handbrakeTorque` solo a las ruedas marcadas con `handbrake`.
- `SetAutoInput(move, handbrake)` / `ClearAutoInput()`: conducir sin teclado (los tests y, más adelante, conductores NPC).
- En `Update`, los visuales de las ruedas copian `GetWorldPose` de su collider.

## Sonido
Todo con `ProceduralSfx` (generado en código), en `AudioSource` 3D del coche:
- **Motor** (`EngineLoop`): arranca al subir el conductor y se apaga al bajar; el tono sigue el régimen de una caja automática de 4 marchas (`VehicleMath.EngineRpm`, cambia cada 9 m/s) y el volumen, el acelerador.
- **Derrape** (`SkidLoop`): según lo que patinan las ruedas (`Slip`, de `WheelHit`).
- **Golpes** (`Crash`): al chocar a más de 2.5 m/s, más fuerte cuanto más rápido.
- **Puertas** (`CarDoor`) al subir y bajar, y **claxon** (`HornLoop`) mientras se mantiene **H** (`GameInput.Horn`).
- Los coches del tráfico llevan su propio motor, más bajo y solo de cerca (`TrafficCar`).

## Romper cosas
Al chocar, si lo que toca es un `Breakable` (vallas, señales, farolas…) y va lo bastante rápido, lo rompe y recupera casi toda la velocidad que llevaba antes del golpe. Ver `Scripts/Destruction/README.md`.

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
