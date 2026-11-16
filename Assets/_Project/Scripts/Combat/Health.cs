using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Vida de cualquier cosa que se pueda herir (jugador, NPC y dianas). Los golpes y disparos buscan este componente
    /// con GetComponentInParent en lo que tocan. En los personajes, el daño depende de dónde da
    /// (<see cref="DamageModel"/>): la zona sale del punto de impacto y de los huesos del modelo (cráneo, columna,
    /// brazos y piernas), así que vale también agachado, cubierto o en mitad de una animación.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float startArmor;

        HealthPool pool;
        public HealthPool Pool => pool ??= new HealthPool(maxHealth, startArmor);
        public bool IsDead => Pool.IsDead;
        public float LastDamageTime { get; private set; } = -100f;
        public BodyZone LastHitZone { get; private set; }

        public event Action<DamageInfo> Damaged;
        public event Action Died;

        // Huesos del modelo, por nombre (los de BuildPlayerBody; los NPC son copias del mismo cuerpo).
        Transform head, pelvis;
        readonly Transform[] shoulder = new Transform[2], elbow = new Transform[2], hand = new Transform[2];
        readonly Transform[] hip = new Transform[2], knee = new Transform[2], ankle = new Transform[2];
        bool bonesSearched;
        readonly List<BodySegment> segments = new();

        public float TakeDamage(DamageInfo info)
        {
            if (Pool.IsDead) return 0f;
            if (info.Kind != DamageKind.Fall && info.Zone == BodyZone.Unknown) info.Zone = ZoneAt(info.Point);
            float dealt = DamageModel.Apply(Pool, info.Amount, info.Kind, info.Zone);
            LastDamageTime = Time.time;
            LastHitZone = info.Zone;
            Damaged?.Invoke(info);
            if (Pool.IsDead) Died?.Invoke();
            return dealt;
        }

        public float Heal(float amount) => Pool.Heal(amount);
        public float AddArmor(float amount) => Pool.AddArmor(amount);

        /// <summary>Vuelve con la vida al máximo (sin chaleco).</summary>
        public void Revive() => pool = new HealthPool(maxHealth, 0f);

        /// <summary>Zona del cuerpo más cercana al punto. Sin huesos (una diana), <see cref="BodyZone.Unknown"/>, que cuenta como tronco.</summary>
        public BodyZone ZoneAt(Vector3 point)
        {
            FindBones();
            if (!head || !pelvis) return BodyZone.Unknown;
            segments.Clear();
            // La cabeza va del cráneo (la articulación está en la base, en el cuello) hasta la coronilla; el tronco, de la
            // pelvis a la altura de los hombros (unos 12 cm por debajo del cráneo), para que el cuello y la cara sean cabeza.
            segments.Add(new BodySegment(BodyZone.Head, head.position + head.up * 0.03f, head.position + head.up * 0.2f, 0.11f));
            segments.Add(new BodySegment(BodyZone.Torso, pelvis.position, head.position - head.up * 0.12f, 0.17f));
            for (int i = 0; i < 2; i++)
            {
                if (shoulder[i] && elbow[i]) segments.Add(new BodySegment(BodyZone.Arm, shoulder[i].position, elbow[i].position, 0.06f));
                if (elbow[i] && hand[i]) segments.Add(new BodySegment(BodyZone.Arm, elbow[i].position, hand[i].position, 0.05f));
                if (hip[i] && knee[i]) segments.Add(new BodySegment(BodyZone.Leg, hip[i].position, knee[i].position, 0.09f));
                if (knee[i] && ankle[i]) segments.Add(new BodySegment(BodyZone.Leg, knee[i].position, ankle[i].position, 0.07f));
            }
            return HitZoneMath.Classify(point, segments);
        }

        void FindBones()
        {
            if (bonesSearched) return;
            bonesSearched = true;
            string[] sides = { "Izq", "Der" };
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "Craneo": head = t; break;
                    case "Cuerpo": pelvis = t; break;
                }
                for (int i = 0; i < 2; i++)
                {
                    if (t.name == "Hombro" + sides[i]) shoulder[i] = t;
                    else if (t.name == "Codo" + sides[i]) elbow[i] = t;
                    else if (t.name == "Mano" + sides[i]) hand[i] = t;
                    else if (t.name == "Cadera" + sides[i]) hip[i] = t;
                    else if (t.name == "Rodilla" + sides[i]) knee[i] = t;
                    else if (t.name == "Tobillo" + sides[i]) ankle[i] = t;
                }
            }
        }
    }
}
