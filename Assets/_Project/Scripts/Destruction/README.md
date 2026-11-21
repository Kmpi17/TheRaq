# Destruction: lo que un coche rompe

Petición del usuario (2026-11-17): «si te chocas contra una valla o señal, que se destruya como en GTA». El coche del jugador (`VehicleController`) rompe vallas y mobiliario urbano al chocar rápido, y sigue casi sin frenar.

## Cómo funciona
- `VehicleController.OnCollisionEnter` busca un `Breakable` en lo que ha tocado (`GetComponentInParent`) y llama a `TryBreak(velocidad, punto, colliders del coche)` con la velocidad que llevaba **antes** del golpe (se guarda en cada `FixedUpdate`). Si se rompe, el coche recupera `SpeedKept` de esa velocidad (85 %; las vallas de hierro y malla, 80 %) y su giro.
- Por debajo de `minSpeed` (4 m/s; hierro y malla, 5) solo choca.
- Al romperse se avisa a `NpcAlerts.Hit` (los civiles de cerca se asustan) y suena `ProceduralSfx` (madera, metal o malla).

## `Breakable` (base)
`minSpeed`, `speedKept`, `SetStrength(min, kept)`, `Broken`, `BrokenCount` (para los tests) y `TryBreak`. Las clases hijas hacen `Break`.

## `BreakableFence`
Va en el panel de cada tramo de valla de `PrototypeSceneBuilder.FenceSegment` (madera, hierro y malla; casi todas las vallas del mapa). Guarda la repetición de la textura (`tile`), los postes y la barandilla de arriba.
- `BreakMath.SplitFence` abre un hueco de 2.8 m donde da el coche. El panel original se **esconde** (no se mueve: está en un lote estático) y se rehace en trozos con `ProceduralMeshes.Box`, la misma malla que usa el generador:
  - los de los lados siguen en pie con su `BreakableFence`, así que se pueden volver a romper;
  - los del hueco (y un lado de menos de 0.6 m) salen volando en trozos de 1.5 m como mucho, con su trozo de barandilla.
- Los postes que caen en el hueco se esconden y salen volando como cilindros sueltos.
- **No se rompen las vallas del borde del mundo** (`FenceOnWorldEdge` en el generador: |x| > 297, el muro norte y el muelle), porque ahí hacen de límite.

## `BreakableProp`
Va en la raíz de cada pieza de mobiliario: farolas (180 kg), semáforos (140), STOP (30), señales de velocidad (20), bocas de incendio (90), bancos (80, madera) y paradas del autobús 52 (70).
- Al romperse se vuelve un cuerpo físico (`Debris.Launch`) que sale empujado y se tumba hacia donde iba el coche.
- Las luces de las farolas se apagan para siempre (se apaga el objeto de la luz, con su `NightLight`).
- La boca de incendio echa un **chorro de agua** unos 30 s (partículas con el material `Vapor` y `ProceduralSfx.WaterSpray`).
- **Tiene que estar fuera del static batching** para poder moverse: `PrototypeSceneBuilder.MarkStatic` no marca nada que tenga un `BreakableProp` encima.

## `Debris`
Trozo suelto: `Rigidbody`, sin choque con el coche que lo rompió (`Physics.IgnoreCollision`) y en la capa 2 `Ignore Raycast`: la cámara no choca con él y las balas no lo ven. Hay como mucho 160; al pasar, se borran los más viejos.

## `BreakMath` (lógica pura) y `ProceduralMeshes`
- `SplitFence` (tramos que quedan y que vuelan, cubriendo la valla entera) y `LaunchVelocity` (hacia delante, arriba y hacia su lado). Se testea en `Tests/EditMode/DrivingAndBreakingTests.cs`.
- `ProceduralMeshes.Box`: caja con la textura repetida por metros (antes estaba en el generador, que ahora la llama). `ProceduralMeshes.Cylinder`: el cilindro de Unity.

## Pruebas
- EditMode `DrivingAndBreakingTests`: `SplitFence` y `LaunchVelocity`.
- PlayMode `BreakablePlayTests`: un coche a 16 m/s atraviesa una valla de hierro (se esconde el panel, quedan dos trozos en pie, vuelan trozos y el coche no frena en seco); a 2.5 m/s solo choca; a 14 m/s tumba un STOP.

## Limitaciones
- Solo rompe el coche del jugador (los del tráfico van por su carril y no chocan con nada).
- Lo roto no se arregla hasta que se vuelve a cargar la escena.
- No se rompen las vallas que no salen de `FenceSegment` (rejas sueltas de escaparates y ventanas), ni muros, bolardos, pilonas ni postes de la luz de madera (llevan cables).
- Los NPC que se sentaban en un banco tumbado siguen sentándose en su sitio.
