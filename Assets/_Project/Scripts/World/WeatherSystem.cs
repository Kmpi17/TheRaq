using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Hora del día y tiempo. Avanza la hora (un día en <see cref="cycleSeconds"/> s reales), elige episodios de tiempo
    /// al azar (<see cref="WeatherMath"/>) y aplica cada fotograma el aspecto de <see cref="DayCycleMath.Look"/>: sol o
    /// luna, ambiente, niebla, cielo degradado gris (<c>GTAChiraq/CieloGris</c>) y farolas (<see cref="NightLight"/>).
    /// La lluvia y la nieve son partículas que siguen a la cámara y mueren al chocar con techos y suelo.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        [SerializeField] Light sun;
        [SerializeField] Material skyMaterial;
        [SerializeField] Material rainMaterial;
        [SerializeField] Material snowMaterial;
        [Tooltip("Hora al empezar (0–24).")]
        [SerializeField] float startHour = 12f;
        [Tooltip("Segundos reales que dura un día de juego.")]
        [SerializeField] float cycleSeconds = DayCycleMath.CycleSeconds;
        [Tooltip("Semilla del tiempo (0: al azar cada partida).")]
        [SerializeField] int seed;
        [Tooltip("Gotas por segundo con lluvia a tope.")]
        [SerializeField] float rainRate = 3500f;
        [Tooltip("Copos por segundo con nieve a tope.")]
        [SerializeField] float snowRate = 1300f;

        public static WeatherSystem Instance { get; private set; }

        public float Hour { get; private set; }
        public Weather Current { get; private set; } = Weather.Nublado;
        /// <summary>Lluvia y nieve actuales (0–1); suben y bajan poco a poco.</summary>
        public float Rain { get; private set; }
        public float Snow { get; private set; }
        public WorldLook LastLook { get; private set; }

        System.Random rng;
        float episodeLeft, strength;
        Material skyInstance, rainInstance, snowInstance;
        ParticleSystem rainFx, snowFx;
        Camera cam;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int TopId = Shader.PropertyToID("_Top"), HorizonId = Shader.PropertyToID("_Horizon"), BottomId = Shader.PropertyToID("_Bottom");

        float Roll() => (float)rng.NextDouble();

        void Awake()
        {
            Instance = this;
            rng = seed != 0 ? new System.Random(seed) : new System.Random();
            Hour = startHour;
            // La partida empieza seca: el primer episodio es nublado.
            Current = Weather.Nublado;
            episodeLeft = WeatherMath.Duration(Current, Roll());
            EnsureVisuals(false);
            Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (skyInstance) Destroy(skyInstance);
            if (rainInstance) Destroy(rainInstance);
            if (snowInstance) Destroy(snowInstance);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Hour = Mathf.Repeat(Hour + dt / cycleSeconds * 24f, 24f);

            episodeLeft -= dt;
            if (episodeLeft <= 0f) StartEpisode(WeatherMath.Next(Current, Roll()));

            Rain = WeatherMath.Approach(Rain, Current == Weather.Lluvia ? strength : 0f, dt);
            Snow = WeatherMath.Approach(Snow, Current == Weather.Nieve ? strength : 0f, dt);
            Apply();
        }

        void LateUpdate() => FollowCamera();

        void StartEpisode(Weather next)
        {
            Current = next;
            strength = WeatherMath.Strength(next, Roll());
            episodeLeft = WeatherMath.Duration(next, Roll());
        }

        // ---------- Para probar desde el inspector (menú del componente) ----------

        public void SetHour(float hour) { Hour = Mathf.Repeat(hour, 24f); Apply(); }

        /// <summary>Fuerza un episodio de tiempo; con <paramref name="instant"/> llega de golpe, sin la transición.</summary>
        public void ForceWeather(Weather weather, float force = 1f, bool instant = false)
        {
            Current = weather;
            strength = weather == Weather.Nublado ? 0f : Mathf.Clamp01(force);
            episodeLeft = WeatherMath.Duration(weather, rng?.NextDouble() is double r ? (float)r : 0.5f);
            if (instant)
            {
                Rain = weather == Weather.Lluvia ? strength : 0f;
                Snow = weather == Weather.Nieve ? strength : 0f;
            }
            Apply();
        }

        [ContextMenu("Adelantar 3 horas")] void SkipHours() => SetHour(Hour + 3f);
        [ContextMenu("Poner de noche (23:00)")] void MakeNight() => SetHour(23f);
        [ContextMenu("Poner de día (12:00)")] void MakeDay() => SetHour(12f);
        [ContextMenu("Forzar lluvia")] void ForceRain() => ForceWeather(Weather.Lluvia);
        [ContextMenu("Forzar nieve")] void ForceSnow() => ForceWeather(Weather.Nieve);
        [ContextMenu("Despejar")] void ForceClear() => ForceWeather(Weather.Nublado);

        /// <summary>"14:30 · Nieve" para el HUD.</summary>
        public string Describe()
        {
            string w = Rain > 0.05f ? "Lluvia" : Snow > 0.05f ? "Nieve" : "Nublado";
            return $"{DayCycleMath.Clock(Hour)}  ·  {w}";
        }

        /// <summary>
        /// Editor (capturas): aplica una hora y un tiempo sin estar en Play, con las partículas ya caídas. No guarda
        /// nada: los objetos que crea no se guardan con la escena.
        /// </summary>
        public void Preview(float hour, float rain, float snow, Camera viewer)
        {
            Hour = Mathf.Repeat(hour, 24f);
            Rain = Mathf.Clamp01(rain);
            Snow = Mathf.Clamp01(snow);
            cam = viewer;
            EnsureVisuals(true);
            foreach (var l in FindObjectsByType<NightLight>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                l.Apply(DayCycleMath.Look(Hour, Rain, Snow).LampFactor);
            Apply();
            FollowCamera();
            foreach (var fx in new[] { rainFx, snowFx })
                if (fx) fx.Simulate(fx == snowFx ? 8f : 2f, true, true);
        }

        // ---------- Aplicar el aspecto ----------

        void Apply()
        {
            var look = DayCycleMath.Look(Hour, Rain, Snow);
            LastLook = look;
            if (sun)
            {
                sun.intensity = look.SunIntensity;
                sun.color = look.SunColor;
                sun.transform.rotation = look.SunRotation;
                sun.enabled = look.SunIntensity > 0.001f;
            }
            RenderSettings.ambientLight = look.Ambient;
            RenderSettings.fogColor = look.Fog;
            RenderSettings.fogStartDistance = look.FogStart;
            RenderSettings.fogEndDistance = look.FogEnd;
            if (skyInstance)
            {
                skyInstance.SetColor(TopId, look.SkyTop);
                skyInstance.SetColor(HorizonId, look.SkyHorizon);
                skyInstance.SetColor(BottomId, look.SkyBottom);
            }
            if (Application.isPlaying) NightLight.SetLevel(look.LampFactor);
            var tint = new Color(look.ParticleTint, look.ParticleTint, look.ParticleTint * 1.05f, 1f);
            if (rainInstance) rainInstance.SetColor(BaseColorId, tint);
            if (snowInstance) snowInstance.SetColor(BaseColorId, tint + new Color(0.08f, 0.07f, 0.05f, 0f) * look.LampFactor); // copos algo anaranjados por las farolas
            if (cam) cam.backgroundColor = look.Fog;

            SetRate(rainFx, Rain * rainRate);
            SetRate(snowFx, Snow * snowRate);
        }

        static void SetRate(ParticleSystem fx, float rate)
        {
            if (!fx) return;
            var emission = fx.emission;
            emission.rateOverTime = rate;
        }

        void FollowCamera()
        {
            if (!cam) cam = Camera.main;
            if (!cam) return;
            // Un poco por delante de la cámara (y más si va en coche) para que caiga donde se mira.
            var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            var basePos = cam.transform.position + forward * 8f;
            if (rainFx) rainFx.transform.position = basePos + Vector3.up * 16f;
            if (snowFx) snowFx.transform.position = basePos + Vector3.up * 12f;
        }

        // ---------- Cielo y partículas ----------

        void EnsureVisuals(bool editorPreview)
        {
            if (!skyInstance && skyMaterial)
            {
                skyInstance = new Material(skyMaterial) { name = "CieloGris (instancia)", hideFlags = HideFlags.DontSave };
                RenderSettings.skybox = skyInstance;
            }
            var flags = editorPreview ? HideFlags.DontSave : HideFlags.None;
            if (!rainFx && rainMaterial)
            {
                rainInstance = new Material(rainMaterial) { hideFlags = HideFlags.DontSave };
                rainFx = BuildRain(flags);
            }
            if (!snowFx && snowMaterial)
            {
                snowInstance = new Material(snowMaterial) { hideFlags = HideFlags.DontSave };
                snowFx = BuildSnow(flags);
            }
        }

        ParticleSystem NewEmitter(string name, HideFlags flags, Material material, float boxSize, int maxParticles)
        {
            var go = new GameObject(name) { hideFlags = flags };
            go.transform.SetParent(transform, false);
            var fx = go.AddComponent<ParticleSystem>();
            fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = fx.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.maxParticles = maxParticles;
            main.playOnAwake = true;
            main.loop = true;

            var shape = fx.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(boxSize, 0.5f, boxSize);

            var emission = fx.emission;
            emission.rateOverTime = 0f;

            // Mueren al chocar con techos, toldos y suelo: no llueve dentro de los edificios ni bajo el tren.
            var collision = fx.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.lifetimeLoss = 1f;
            collision.collidesWith = ~(1 << GameLayers.IgnoreRaycast);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 0.05f; // las que pasan junto a la cámara no tapan media pantalla
            return fx;
        }

        ParticleSystem BuildRain(HideFlags flags)
        {
            var fx = NewEmitter("Lluvia", flags, rainInstance, 44f, 6000);
            var main = fx.main;
            main.startLifetime = 1.15f;
            main.startSize = 0.025f;
            main.startColor = new Color(0.72f, 0.76f, 0.82f, 0.4f);

            var velocity = fx.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-19f, -22f);
            velocity.z = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);

            var renderer = fx.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.03f; // gotas de ~0.6 m: la estela que se ve en un fotograma
            renderer.lengthScale = 1f;
            fx.Play();
            return fx;
        }

        ParticleSystem BuildSnow(HideFlags flags)
        {
            var fx = NewEmitter("Nieve", flags, snowInstance, 56f, 14000);
            var main = fx.main;
            main.startLifetime = 9f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(1f, 1f, 1f, 0.9f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var velocity = fx.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.4f, 1f);
            velocity.y = new ParticleSystem.MinMaxCurve(-1.2f, -1.9f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.3f);

            // Los copos van de lado a lado al caer.
            var noise = fx.noise;
            noise.enabled = true;
            noise.strength = 0.7f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.2f;

            var renderer = fx.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            fx.Play();
            return fx;
        }
    }
}
