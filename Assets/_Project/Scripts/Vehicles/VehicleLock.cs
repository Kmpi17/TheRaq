using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Cerradura de un coche que no es del jugador. Cerrado, no se sube sin más: hay que forzar la puerta manteniendo F
    /// (<see cref="VehicleEntry"/>) durante <see cref="ForceSeconds"/>. Si se suelta antes, se empieza de cero. Al
    /// abrirse queda abierto para siempre (el coche ya es suyo) y, a veces, salta la alarma.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public sealed class VehicleLock : MonoBehaviour
    {
        [SerializeField] bool locked = true;
        [SerializeField] float forceSeconds = 2.5f;
        [Range(0f, 1f)] [SerializeField] float alarmChance = 0.35f;
        [SerializeField] float alarmSeconds = 10f;

        float elapsed, nextRattle;
        AudioSource audioSource;
        static AudioClip rattle, alarm;

        public bool Locked => locked;
        public float ForceSeconds => forceSeconds;
        /// <summary>Lo que lleva forzado, de 0 a 1.</summary>
        public float Progress => locked ? Mathf.Clamp01(elapsed / Mathf.Max(0.01f, forceSeconds)) : 1f;
        public bool AlarmOn => audioSource && audioSource.isPlaying && audioSource.loop;

        /// <summary>Lo usan el generador de escena y los tests.</summary>
        public void Configure(bool isLocked, float seconds, float chanceOfAlarm)
        {
            locked = isLocked;
            forceSeconds = seconds;
            alarmChance = chanceOfAlarm;
            elapsed = 0f;
        }

        /// <summary>Un paso de forzar la cerradura. Devuelve true cuando ya está abierta.</summary>
        public bool Force(float dt)
        {
            if (!locked) return true;
            elapsed += dt;
            if (Application.isPlaying && Time.time >= nextRattle)
            {
                nextRattle = Time.time + 0.3f;
                Play(rattle ??= ProceduralSfx.Click(), loop: false);
            }
            if (elapsed < forceSeconds) return false;
            locked = false;
            elapsed = 0f;
            if (Application.isPlaying && Random.value < alarmChance) StartAlarm();
            return true;
        }

        /// <summary>Se soltó F o se interrumpió: lo forzado se pierde.</summary>
        public void ResetForce() => elapsed = 0f;

        void StartAlarm()
        {
            Play(alarm ??= ProceduralSfx.CarAlarm(), loop: true);
            CancelInvoke(nameof(StopAlarm));
            Invoke(nameof(StopAlarm), alarmSeconds);
        }

        void StopAlarm()
        {
            if (audioSource) audioSource.Stop();
        }

        void Play(AudioClip clip, bool loop)
        {
            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 4f;
                audioSource.maxDistance = 60f;
                audioSource.playOnAwake = false;
            }
            if (loop)
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Play();
            }
            else audioSource.PlayOneShot(clip, 0.7f);
        }
    }
}
