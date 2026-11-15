# Camera: `ThirdPersonCamera.cs`

Cámara orbital propia (no usa Cinemachine) que va en `Main Camera`. Se actualiza en `LateUpdate`, después de que se muevan el jugador y el coche.

## Funcionamiento
1. Acumula `yaw` y `pitch` a partir de `LookMouse × mouseSensitivity` y `LookStick × stickSpeed × dt`. El pitch se limita según el perfil.
2. **En modo `Vehicle`**: si no se mueve la cámara durante `autoAlignDelay` s, el yaw vuelve solo hacia la parte trasera del coche a `autoAlignSpeed` °/s.
3. Calcula el pivote (`target.position + up × height`) y lanza un `SphereCast` hacia atrás. Si choca con una pared, acerca la cámara.
4. Gestiona el cursor: se bloquea al empezar, Esc lo suelta y un clic lo vuelve a bloquear.

## API
- `SetTarget(Transform target, Mode mode)`: la usa `VehicleEntry` para cambiar entre el jugador (`OnFoot`) y el coche (`Vehicle`).
- `SetAiming(bool)`: solo a pie. Pasa en unos 0.15 s (`aimBlendSpeed`) al perfil `aim`, al hombro: el pivote se aparta `aimShoulder` (0.6 m) a la derecha, sin meterse en paredes. El campo de visión baja a `aimFieldOfView` (46°) y la sensibilidad a `aimSensitivity` (×0.6).
- `AddRecoil(up, side)`: retroceso de un disparo, en grados.
- `AimRay`: rayo por el centro de la pantalla, donde está la mira.
- `Yaw` y `AimBlend`.
- Con `GameUi.MenuOpen` no se mueve la vista ni se bloquea el cursor.

## Perfiles (`Profile`)

| Perfil | distance | height | pitch |
|---|---|---|---|
| `onFoot` | 4 m | 1.6 m | −30…70° |
| `vehicle` | 7.5 m | 2 m | −10…60° |
| `aim` | 1.9 m | 1.55 m | −50…60° |

## Colisión
`collisionMask` ignora por defecto la capa 2 (`Ignore Raycast`, el jugador) y la 6 (`Vehiculo`, todos los coches conducibles), para que la cámara no choque con ellos (`GameLayers`). La escalada, la cobertura y las balas sí ven la capa 6. Si añades personajes, ponlos en la capa 2; los vehículos, en la 6.
