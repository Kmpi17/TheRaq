# Camera: `ThirdPersonCamera.cs`

Cámara orbital propia (no usa Cinemachine) que va en `Main Camera`. Se actualiza en `LateUpdate`, después de que se muevan el jugador y el coche.

## Funcionamiento
1. Acumula `yaw` y `pitch` a partir de `LookMouse × mouseSensitivity` y `LookStick × stickSpeed × dt`. El pitch se limita según el perfil.
2. **En modo `Vehicle`**: si no se mueve la cámara durante `autoAlignDelay` s, el yaw vuelve solo hacia la parte trasera del coche a `autoAlignSpeed` °/s.
3. Calcula el pivote (`target.position + up × height`) y lanza un `SphereCast` hacia atrás. Si choca con una pared, acerca la cámara.
4. Gestiona el cursor: se bloquea al empezar, Esc lo suelta y un clic lo vuelve a bloquear.

## API
- `SetTarget(Transform target, Mode mode)`: la usa `VehicleEntry` para cambiar entre el jugador (`OnFoot`) y el coche (`Vehicle`).

## Perfiles (`Profile`)

| Perfil | distance | height | pitch |
|---|---|---|---|
| `onFoot` | 4 m | 1.6 m | −30…70° |
| `vehicle` | 7.5 m | 2 m | −10…60° |

## Colisión
`collisionMask` ignora por defecto la capa 2 (`Ignore Raycast`). El generador pone al jugador y a los coches en esa capa para que la cámara no choque con ellos. Si añades personajes o vehículos nuevos, ponlos también en la capa 2 o ajusta la máscara.
