using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Mobiliario urbano que un coche tumba: señales de STOP y de velocidad, farolas, semáforos, bocas de incendio,
    /// bancos, paradas de autobús. Va en la raíz del objeto (con sus colliders en los hijos). Al romperlo se vuelve un
    /// cuerpo físico que sale empujado y cae; las farolas se apagan y la boca de incendio echa un chorro de agua.
    /// El generador lo deja fuera del static batching (<c>MarkStatic</c>), porque tiene que poder moverse.
    /// </summary>
    public sealed class BreakableProp : Breakable
    {
        public enum Kind { Metal, Wood, Hydrant }

        [SerializeField] Kind kind = Kind.Metal;
        [SerializeField] float mass = 60f;
        [Tooltip("Material de las partículas del chorro (solo bocas de incendio).")]
        [SerializeField] Material sprayMaterial;

        static AudioClip metalClip, woodClip, sprayClip;

        public void Configure(Kind propKind, float propMass, Material spray = null)
        {
            kind = propKind;
            mass = propMass;
            sprayMaterial = spray;
        }

        protected override void Break(Vector3 velocity, Vector3 point, Collider[] ignore)
        {
            // Las luces (farolas) se apagan para siempre: se apaga el objeto, con su NightLight.
            foreach (var l in GetComponentsInChildren<Light>()) l.gameObject.SetActive(false);

            var colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
                if (c is MeshCollider m) m.convex = true;
            if (colliders.Length == 0)
            {
                var bounds = new Bounds(transform.position, Vector3.zero);
                foreach (var r in GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = transform.InverseTransformPoint(bounds.center);
                box.size = bounds.size;
            }

            var right = Vector3.Cross(Vector3.up, velocity).normalized;
            float side = Mathf.Sign(Vector3.Dot(transform.position - point, right));
            // Se tumba hacia donde iba el coche: giro alrededor del eje horizontal perpendicular a la marcha.
            var spin = Vector3.Cross(Vector3.up, velocity.normalized) * 3.5f + Random.insideUnitSphere;
            Debris.Launch(gameObject, mass, BreakMath.LaunchVelocity(velocity, right, side, 0.55f), spin, ignore);

            if (!metalClip)
            {
                metalClip = ProceduralSfx.MetalClang();
                woodClip = ProceduralSfx.WoodBreak();
                sprayClip = ProceduralSfx.WaterSpray();
            }
            Play(kind == Kind.Wood ? woodClip : metalClip, point, 1f);
            if (kind == Kind.Hydrant) Spray(transform.position);
        }

        /// <summary>Chorro de agua donde estaba la boca de incendio, unos 30 s.</summary>
        void Spray(Vector3 at)
        {
            var go = new GameObject("ChorroAgua");
            go.transform.SetPositionAndRotation(at + Vector3.up * 0.3f, Quaternion.Euler(-90f, 0f, 0f));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 30f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startColor = new Color(0.82f, 0.88f, 0.95f, 0.55f);
            main.gravityModifier = 1f;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 90f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 7f;
            shape.radius = 0.08f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (sprayMaterial) r.sharedMaterial = sprayMaterial;
            ps.Play();

            var audio = go.AddComponent<AudioSource>();
            audio.clip = sprayClip;
            audio.loop = true;
            audio.spatialBlend = 1f;
            audio.minDistance = 4f;
            audio.maxDistance = 45f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.volume = 0.6f;
            audio.Play();
            Destroy(go, 32f);
        }
    }
}
