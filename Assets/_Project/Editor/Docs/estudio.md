# Vida del bloque: `PrototypeSceneBuilder.Studio.cs`
`BuildTowersYard` llama a estas funciones justo después de la losa de la canasta, antes que las farolas y bancos del eje, para que esos respeten su sitio. Usan una semilla propia (1017), así que no mueven el resto del bloque.
- **Estudio** (`BuildStudio`, objeto `Estudio_Sotano_T1`):
  - Hueco de la escalera: `StudioStairwell` (x −80.3…−77.7, z 40…48.5), al oeste de T1 y al norte de sus contenedores. Se recorta del `Suelo` en `BuildStreets` (con `RectMinus`) y del césped en `BuildTowersGround`.
  - La escalera baja hacia el sur hasta `StudioFloor` (−3.2), con peldaños macizos y una rampa invisible (`RampaEscalera`, la misma pendiente que la estación: `StairRunPerRise`). Las paredes del hueco son verticales y arriba hay una barandilla con colisión (`LimiteHueco`) por tres lados.
  - Sala: `StudioRoom` (x −77.5…−63, z 38…49.5). El techo es el `Suelo` (−0.2) con un panel acústico debajo. La puerta está en la pared oeste, que comparte con el hueco.
  - Cabina de voz en la esquina noreste, con el micro. Junto al micro va `Micro_Grabacion`, el `RecordingStudio`: su `pulseLight` es el `FocoCabina`, que late con el beat.
- **Memorial** (`BuildMemorial`): centro (−17.5, −4.6), con un tramo de malla (`FenceSegment`) en z −3.4. Las cosas (velas, peluches, flores, globos) son primitivas sin colisión. La foto es una silueta; los carteles usan `SignMat` ("RIP", "LONG LIVE").
- **Zona de reunión** (`BuildHangout`): 6.3 m al sur de la losa de la canasta. Sillas con colisión en el asiento, barbacoa de bidón (con colisión, brasas emisivas, luz y humo de esferas), altavoz con aros LED, nevera y vasos.
- Capturas: elementos `estudio`, `memorial` y `reunion`. El plano lo dibuja en la sección *Vida del bloque* de `generar_mapa.py`.
