# Combat: vida, energía, golpes y disparos

## Lógica pura (`CombatCore.cs`)
- `DamageInfo`: cantidad, tipo (`Melee`, `Bullet`, `Fall`), punto, dirección y quién lo hace.
- `HealthPool`: vida y chaleco. El chaleco absorbe el 70 % (`ArmorAbsorb`) de golpes y disparos hasta gastarse; las caídas lo ignoran. Muerto no se cura.
- `StaminaPool`: energía.
  - Se recupera a `RegenRate` (22/s) tras `RegenDelay` (0.9 s) sin gastar.
  - A 0 queda **agotado** (`Exhausted`: no esprinta ni puede gastar) hasta volver al 30 %.
  - `Boost(s)` multiplica la recuperación por 2.5 (bebida energética).
- `CombatMath.Spread(base, aiming, speed, bloom)`: dispersión en grados. ×2.5 desde la cadera, y más al moverse o al encadenar tiros.
- `CombatMath.Scatter(dir, angle, disc)`: desvía un tiro dentro de ese cono.

## `Health.cs`
Va en todo lo que se puede herir. `TakeDamage(DamageInfo)` lanza los eventos `Damaged` y `Died`; además están `Heal`, `AddArmor` y `Revive`. Los golpes buscan el componente con `GetComponentInParent<Health>()` en el collider que tocan.

## `PlayerCombat.cs`
Depende del arma equipada en `PlayerInventory` (null = puños).
- **Puños** (clic izquierdo):
  - Combo de 3 golpes (12, 12 y un gancho de 24). Si se pulsa durante un golpe, el siguiente se encadena; si no, el combo se corta a los 0.45 s.
  - Se orienta solo al objetivo más cercano (2.5 m, ±70°) y da un paso.
  - Cada golpe gasta 7 de energía; agotado pega la mitad.
  - El golpe es un OverlapSphere delante del pecho y empuja los Rigidbody.
- **Pistola**:
  - Clic derecho apunta: `ThirdPersonCamera.SetAiming` pone la vista al hombro, y el personaje va despacio y mira a la cámara.
  - Clic izquierdo dispara, semiautomática (`FireInterval`). Desde la cadera también, con más dispersión: el personaje gira hacia la mira y la mantiene 0.6 s.
  - Puntería: el punto se busca con `AimRay`, el centro de la pantalla. El tiro sale del cañón con la dispersión de `CombatMath`. Un `Linecast` del pecho al cañón evita disparar a través de una pared pegada.
  - Cada tiro suma bloom y da retroceso a la cámara. Un collider llamado `Cabeza` recibe ×2.5.
  - R recarga desde el inventario (`ReloadSeconds`). Con el cargador vacío, clic y recarga sola. Cambiar de arma cancela la recarga.
  - Agujeros de bala (los 40 últimos) y fogonazo con luz.
- En cobertura sin apuntar no dispara ni pega.
- Sonido: `ProceduralSfx`, que es ruido generado en código (provisional).

## `TrainingDummy.cs`
Diana de prácticas: se balancea al recibir golpes, cae al morir y se levanta a los 5 s. El generador pone tres junto al inicio (`PrototypeSceneBuilder.Training.cs`).

Tests: `Tests/EditMode/CharacterSystemsTests.cs`.

## Pendiente
- Enemigos o NPC que usen `Health` y disparen (la cobertura ya funciona contra rayos).
- Animaciones de verdad (ahora el puño y la pistola son primitivas que se mueven).
- Más armas: basta con añadirlas a `ItemCatalog`. Las automáticas necesitarían `IsPressed` en vez de `WasPressedThisFrame`.

## Datos para la animación
`PlayerCombat` expone `WeaponRaised` (apuntando o justo tras disparar desde la cadera), `Punching`, `PunchExtend` (0–1, cuánto ha salido el puño), `PunchSide` (+1 derecha, −1 izquierda) y `PunchIsHook`. Los usa `PlayerAnimator` para mover los brazos. El campo `fist` (un puño suelto que salía al pegar) ya no lo pone el generador: con brazos animados no hace falta, y sin él el combate funciona igual.
