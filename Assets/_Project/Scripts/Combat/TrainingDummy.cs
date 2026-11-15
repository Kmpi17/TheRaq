using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Diana de prácticas: se balancea al recibir un golpe, cae al quedarse sin vida y se levanta a los pocos segundos.
    /// El objeto raíz va en el suelo (gira desde la base). Va fuera de "Mundo", porque se mueve.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField] float respawnSeconds = 5f;

        Health health;
        Quaternion rest;
        Vector3 axis = Vector3.right;
        float wobble, fall, downSince;
        bool down;

        void Awake()
        {
            health = GetComponent<Health>();
            rest = transform.localRotation;
            health.Damaged += OnDamaged;
            health.Died += () => { down = true; downSince = Time.time; };
        }

        void OnDamaged(DamageInfo info)
        {
            var dir = info.Direction;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = -transform.forward;
            axis = Vector3.Cross(Vector3.up, dir.normalized);
            wobble = Mathf.Min(wobble + 14f, 28f);
        }

        void Update()
        {
            if (down && Time.time - downSince > respawnSeconds)
            {
                down = false;
                health.Revive();
            }
            fall = Mathf.MoveTowards(fall, down ? 85f : 0f, (down ? 260f : 90f) * Time.deltaTime);
            wobble = Mathf.MoveTowards(wobble, 0f, 50f * Time.deltaTime);
            float swing = wobble * Mathf.Cos(Time.time * 18f);
            transform.localRotation = Quaternion.AngleAxis(fall + swing, axis) * rest;
        }
    }
}
