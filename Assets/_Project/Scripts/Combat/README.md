# Combat: vida, energía, golpes y disparos

## Lógica pura (`CombatCore.cs`)
- `DamageInfo`: cantidad, tipo (`Melee`, `Bullet`, `Fall`), punto, dirección y quién lo hace.
- `HealthPool`: vida y chaleco. El chaleco absorbe el 70 % (`ArmorAbsorb`) de golpes y disparos hasta gastarse; las caídas lo ignoran. Muerto no se cura. `Kill()` deja la vida a 0 (tiro en la cabeza).
- `BodyZone` (`Head`, `Torso`, `Arm`, `Leg`; `Unknown` sin huesos o en caídas) y `DamageModel`: el daño por zonas, igual para el jugador y para todos los NPC, aliados o enemigos (ver *Vida realista por zonas*).
- `HitZoneMath.Classify(punto, segmentos)`: la zona del segmento del cuerpo (`BodySegment`, entre dos articulaciones y con su grosor) más cercano al punto.
- `StaminaPool`: energía.
  - Se recupera a `RegenRate` (22/s) tras `RegenDelay` (0.9 s) sin gastar.
  - A 0 queda **agotado** (`Exhausted`: no esprinta ni puede gastar) hasta volver al 30 %.
  - `Boost(s)` multiplica la recuperación por 2.5 (bebida energética).
- `CombatMath.Spread(base, aiming, speed, bloom, crouched)`: dispersión en grados. ×2.5 desde la cadera, y más al moverse o al encadenar tiros; agachado, ×0.7 (`CrouchSpreadFactor`).
- `CombatMath.Scatter(dir, angle, disc)`: desvía un tiro dentro de ese cono.

## `Health.cs`
Va en todo lo que se puede herir. `TakeDamage(DamageInfo)` saca la zona del punto de impacto (`ZoneAt`, si no venía en `DamageInfo.Zone`), aplica `DamageModel` y lanza los eventos `Damaged` (con la zona en `info.Zone`) y `Died`; `LastHitZone` guarda la última. Además están `Heal`, `AddArmor` y `Revive`. Los golpes buscan el componente con `GetComponentInParent<Health>()` en el collider que tocan.

`ZoneAt(punto)` busca por nombre los huesos del modelo (`Craneo`, `Cuerpo`, `Hombro/Codo/Mano` y `Cadera/Rodilla/Tobillo` + `Izq`/`Der`, los de `BuildPlayerBody`) y arma los segmentos: la cabeza, del cráneo a la coronilla; el tronco, de la pelvis a los hombros; los brazos y las piernas, por tramos. Como usa las posiciones de cada momento, acierta también agachado, cubierto o animado. Sin huesos (las dianas) devuelve `Unknown`, que cuenta como tronco.

## Vida realista por zonas (`DamageModel`)
Petición del usuario (2026-11-16): vale para todo personaje, enemigo o aliado (y el jugador), y tiene que ser relativamente realista. El daño de un arma (`ItemDef.Damage`) es lo que hace **una bala en el pecho**; el resto sale de ahí:

| Zona | Bala | Con la 9 mm (50) y 100 de vida | Chaleco | Golpe (puño) |
|---|---|---|---|---|
| Cabeza | Mata siempre | 1 tiro | No protege | ×1.5 |
| Tronco (pecho, vientre) | ×1 | 2 tiros | Para el 70 % hasta gastarse | ×1 |
| Brazos y piernas | ×0.34 (`LimbBulletFactor`) | 6 tiros | No protege | ×0.6 |

- **Según las balas:** con un arma de más daño hacen falta menos tiros (una de 70 tumba en 5 en las piernas; una de 100 o más mata de uno en el pecho), y con una de menos, más. `DamageModel.ShotsToKill(daño, zona, vida, chaleco)` lo calcula.
- **Chaleco:** con 100 de chaleco, la 9 mm necesita 5 tiros en el pecho; con 50, 3. Los NPC lo llevan a veces (ver `Scripts/NPC/README.md`) y el jugador lo consigue con el objeto *Chaleco*.
- Las caídas no miran la zona ni el chaleco.

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
  - Cada tiro suma bloom y da retroceso a la cámara. El daño depende de dónde da (`DamageModel`, lo aplica `Health`).
  - R recarga desde el inventario (`ReloadSeconds`). Con el cargador vacío, clic y recarga sola. Cambiar de arma cancela la recarga.
  - Agujeros de bala (los 40 últimos) y fogonazo con luz.
- En cobertura sin apuntar no dispara ni pega.
- Sonido: `ProceduralSfx`, que es ruido generado en código (provisional). También hace los sonidos de la ciudad: `Horn` (claxon del tráfico), `TrainHorn` (bocina del tren de mercancías: largo, largo, corto, largo) y `CrossingBell` (campana del paso a nivel); los del coche (`EngineLoop`, `SkidLoop`, `Crash`, `HornLoop`, `CarDoor`); los de romper cosas (`MetalClang`, `WoodBreak`, `FenceRattle`, `WaterSpray`) y `Rummage` (registrar un cuerpo). Los bucles duran 1 s con frecuencias enteras, para empalmar sin chasquido.

## `TrainingDummy.cs`
Diana de prácticas: se balancea al recibir golpes, cae al morir y se levanta a los 5 s. El generador pone tres junto al inicio (`PrototypeSceneBuilder.Training.cs`).

Tests: `Tests/EditMode/CharacterSystemsTests.cs`.

## Pendiente
- Daño por zonas: que se note dónde da (cojear con tiros en la pierna, peor puntería con un brazo herido, sangrado que baja la vida poco a poco) y que el chaleco de los NPC se vea en el modelo.
- Enemigos o NPC que usen `Health` y disparen (la cobertura ya funciona contra rayos).
- Animaciones de verdad (ahora el puño y la pistola son primitivas que se mueven).
- Más armas: basta con añadirlas a `ItemCatalog`. Las automáticas necesitarían `IsPressed` en vez de `WasPressedThisFrame`.

## Avisos para los NPC
Cada disparo del jugador llama a `NpcAlerts.Gunshot(posición, jugador, aQuienApuntaba)` (aunque falle) y cada golpe que hace daño a `NpcAlerts.Hit`. Los NPC lo oyen (45 m y 12 m): los civiles huyen y los grupos responden o ayudan a su bando (`Scripts/NPC/`). Todo disparo o golpe nuevo debe avisar igual.

## Datos para la animación
`PlayerCombat` expone `WeaponRaised` (apuntando o justo tras disparar desde la cadera), `Punching`, `PunchExtend` (0–1, cuánto ha salido el puño), `PunchSide` (+1 derecha, −1 izquierda) y `PunchIsHook`. Los usa `PlayerAnimator` para mover los brazos. El campo `fist` (un puño suelto que salía al pegar) ya no lo pone el generador: con brazos animados no hace falta, y sin él el combate funciona igual.
