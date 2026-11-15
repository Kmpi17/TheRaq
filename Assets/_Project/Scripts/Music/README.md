# Scripts/Music

Progresión musical del jugador. Su centro es el estudio casero del sótano de T1 (lo construye `PrototypeSceneBuilder.Studio.cs`).

## `RecordingStudio` (MonoBehaviour)
Va en `Micro_Grabacion`, junto al micro de la cabina de voz.
- Con el jugador a menos de `radius` (1.8 m), a pie, Interact (**F**) empieza una sesión. Si el jugador va en coche (su CharacterController está desactivado), no cuenta.
- La sesión empieza con una cuenta de entrada de 4 golpes y luego vienen `beats` golpes (16) a `beatsPerMinute` (72). En cada golpe hay que pulsar F, con un margen de `window` segundos (0.16).
- La luz `pulseLight` late con el beat. Si el jugador se aleja del micro, la sesión se pierde.
- Al acabar, el tema suma fama a la `MusicCareer` y se guarda en PlayerPrefs.
- La interfaz es `OnGUI`: el aviso para grabar, el contador de golpes, una barra hasta el siguiente golpe, aciertos y fallos, y después la nota, la fama ganada y el rango.
- Busca al jugador por la etiqueta `Player` en `Start`.

## `StudioSession` (lógica pura)
- `BeatTime(i)`: momento del golpe i, contando la cuenta de entrada.
- `NearestBeat(t)`: golpe más cercano a t.
- `Tap(t)`: devuelve true si t cae a tiempo en un golpe que aún no se había acertado; si no, suma un fallo.
- `Score`: (aciertos − 0.5 × fallos) / golpes, en [0, 1].
- `Duration` y `Finished(t)`: final de la sesión.

## `MusicCareer` (lógica pura)
- `Tracks` (temas grabados) y `Fame` (fama).
- `FameFor(score)`: 5 + 95 × nota², y 25 más si la nota es de 0.9 o más (de 5 a 125 por tema).
- Rangos (`RankFame` / `RankNames`): Desconocido (0), Del bloque (100), Conocido en el barrio (300), Sonando en la ciudad (700) y Leyenda del Southside (1500). `ProgressToNext` da la fracción hasta el rango siguiente.
- `Load()` y `Save()` usan PlayerPrefs (`career.tracks`, `career.fame`).

## Limitaciones
- Todavía no suena nada: el beat solo se ve en la luz y en la barra.
- La fama aún no desbloquea nada: es la base para misiones, mejoras del estudio o reconocimiento en la calle.
- La interfaz es OnGUI provisional.
- F es la misma tecla que la de subir al coche. En el sótano no hay coches, así que no chocan.

Tests: `Tests/EditMode/MusicCareerTests.cs`.
