# `PlayerClimb.cs`
Espacio delante de un obstáculo (hacia donde se pide ir o hacia donde mira):
1. Un SphereCast a la altura de las rodillas busca una cara casi vertical.
2. **Coches** (lo tocado tiene un `VehicleController` en un padre): un rayo de vuelta desde el otro lado da el ancho, y unos rayos hacia abajo cada 0.3 m dan lo más alto del camino. Si no pasa de `ClimbMath.SlideMax` (1.6 m: capó, maletero o techo de un sedán) lo **cruza por encima** (`SlideOver`) hasta el suelo del otro lado. Las furgonetas y los todoterreno por el techo siguen con la escalada normal (suben encima).
3. Dos rayos hacia abajo dan la altura: uno a 1.5 cm de la cara (las vallas son tablas de 3 cm, y el de 0.2 m caía al otro lado, así que no se podían saltar) y otro a 0.2 m; vale el más alto. Otro, 0.75 m más adentro, dice si es grueso.
4. `ClimbMath.Classify`:
   - menos de 0.45 m o más de 2.4 m: nada;
   - grueso: **subir** encima;
   - fino de hasta 1.25 m: **saltar por encima**;
   - fino más alto: **trepar** al otro lado.
5. Comprobaciones (`RoomToLand`, `IsFloor`): que el punto de llegada sea **suelo de verdad** (a 22 cm en cruz también hay suelo a esa altura: el pasamanos de otra valla no vale; antes, al saltar una valla junto a una esquina, acababa de pie encima del tramo de al lado), que haya sitio arriba y en el punto de llegada (CheckCapsule), que no haya techo sobre la cabeza y, al pasar al otro lado, que el suelo no esté más de 3 m por debajo del borde (así no se salta a la trinchera de la autopista).
6. Gasta energía (`ClimbMath.StaminaCost`). Si no hay, avisa "Sin fuerzas".
7. Recorre el camino (pegarse, subir, pasar, bajar) con el CharacterController apagado, en `ClimbMath.Duration`. Al saltar por encima o cruzar un coche sale corriendo.

Solo se escala lo que se ve (`SolidVisual.IsVisible`: `Renderer` en el mismo objeto que el collider, o `SolidVisual` en él o en un padre, como los coches) y no lleva `NoClimb` en él ni en un padre (`PlayerClimb.IsClimbable`). Los colliders invisibles del generador (límites, rampas) no se escalan. La máscara solo ignora la capa 2 (el jugador): los coches (capa 6, `Vehiculo`) sí se escalan.

## `PlayerCover.cs`
Q junto a un obstáculo visible y **opaco** de al menos 0.85 m de alto (pared, esquina de casa, muro, valla de tablones, coche, contenedor). Lo que deja ver a través no cubre (`SolidVisual.IsSeeThrough`): material con recorte por alfa (verjas de barrotes, mallas, barandillas caladas) o `NoCover` en él o en un padre. Esas verjas sí se saltan con Espacio.
- Por debajo de 1.45 m es cobertura **baja**: se agacha (CharacterController y `Modelo` más bajos).
- Se mueve de lado pegado a la pared y se para en las esquinas (`AtEdge`). La normal se vuelve a tomar en cada fotograma, así que sigue paredes curvas.
- Se puede entrar desde tan lejos como la busca (`searchDistance`): mientras sigue en cobertura, la pared se comprueba con ese mismo alcance y el tirón (hasta 4 m/s) lo pega a ella. Antes se comprobaba a `radio + 0.8 m` y, si se pulsaba Q a un metro, entraba y salía en el mismo fotograma.
- En la cobertura alta, sin apuntar, el modelo se pone **de lado**: hombro contra la pared y mirando a lo largo de ella hacia donde se movió (lo hace `PlayerAnimator`; el `Jugador` sigue de cara a la pared).
- Apuntando (`Peeking`, lo pone `PlayerCombat`): en cobertura baja se levanta; en alta sale de lado `peekStep` (0.65 m) si está en una esquina.
- Se sale con Q, esprintando, tirando hacia atrás 0.3 s, si desaparece la pared, al subir a un coche o al morir.
- Las balas son rayos, así que la cobertura protege de verdad.
