# Hora y tiempo: `PrototypeSceneBuilder.Weather.cs`
`BuildWeather` (después de `BuildMainRoadDetails`) crea el objeto `Tiempo`, fuera de `Mundo`, con `WeatherSystem`. Le pasa el sol (`RenderSettings.sun`), el cielo y dos materiales de partículas transparentes (URP Particles/Unlit, que se desvanecen a menos de 1.2–4 m de la cámara): `Lluvia`, sin textura, y `Copo`, con `ProceduralTextures.Snowflake()`. `SkyMaterial()` crea `CieloGris.mat`.

Las luces de calle llevan `NightLight` desde donde se crean: `StreetLight` (farolas), `ParkLamp` (StreetProps), las farolas de la autopista (Highway) y las torres de focos de las cocheras (RailYard). Las demás (interiores, fuegos, túnel, faros) no lo llevan y se quedan siempre encendidas. **Una luz de calle nueva tiene que llevar `NightLight`.**
