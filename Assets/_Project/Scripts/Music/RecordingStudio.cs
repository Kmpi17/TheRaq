using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Micrófono del estudio casero: el centro de la progresión musical. Con el jugador cerca, Interact (F) empieza una
    /// sesión: cuenta de entrada y un beat; hay que pulsar F en cada golpe (ver <see cref="StudioSession"/>). Al
    /// acabar, el tema suma fama a la <see cref="MusicCareer"/>, que se guarda. Si el jugador se aleja, la sesión se
    /// pierde. Dibuja su propia interfaz con OnGUI (aviso, golpes, nota y rango).
    /// </summary>
    public class RecordingStudio : MonoBehaviour
    {
        [SerializeField] float radius = 1.8f;
        [Tooltip("Golpes por minuto que hay que marcar (la mitad del tempo del beat, para que se pueda seguir).")]
        [SerializeField] float beatsPerMinute = 72f;
        [SerializeField] int beats = 16;
        [Tooltip("Margen para acertar un golpe, en segundos.")]
        [SerializeField] float window = 0.16f;
        [SerializeField] Light pulseLight;

        Transform player;
        MusicCareer career;
        StudioSession session;
        float sessionStart;
        string message;
        float messageUntil;
        float lastTapTime = -10f;
        bool lastTapHit;

        public MusicCareer Career => career;
        public bool Recording => session != null;

        void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
            career = MusicCareer.Load();
        }

        bool PlayerNear()
        {
            if (!player || !player.gameObject.activeInHierarchy) return false;
            var cc = player.GetComponent<CharacterController>();
            if (cc && !cc.enabled) return false; // dentro de un coche
            return (player.position - transform.position).sqrMagnitude < radius * radius;
        }

        void Update()
        {
            bool near = PlayerNear();
            bool pressed = GameInput.Actions.Interact.WasPressedThisFrame();

            if (session == null)
            {
                if (near && pressed)
                {
                    session = new StudioSession(beatsPerMinute, beats, window);
                    sessionStart = Time.time;
                }
                SetPulse(0f);
                return;
            }

            float t = Time.time - sessionStart;
            if (!near)
            {
                session = null;
                Say("Sesión perdida: te fuiste del micro", 3f);
                return;
            }
            if (pressed)
            {
                lastTapHit = session.Tap(t);
                lastTapTime = Time.time;
            }

            // La luz del estudio late con el beat.
            float phase = Mathf.Repeat(t / session.BeatInterval, 1f);
            SetPulse(Mathf.Clamp01(1f - phase * 3f));

            if (session.Finished(t))
            {
                float score = session.Score;
                int gained = career.RecordTrack(score);
                career.Save();
                Say($"Tema grabado · nota {Mathf.RoundToInt(score * 100f)} % · +{gained} de fama · {career.Rank}", 5f);
                session = null;
            }
        }

        void SetPulse(float amount)
        {
            if (pulseLight) pulseLight.intensity = 1.2f + 2.5f * amount;
        }

        void Say(string text, float seconds)
        {
            message = text;
            messageUntil = Time.time + seconds;
        }

        void OnGUI()
        {
            if (career == null) return;
            var box = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            float w = 520f, x = (Screen.width - w) / 2f, y = Screen.height - 150f;

            if (session != null)
            {
                float t = Time.time - sessionStart;
                int beat = session.NearestBeat(t);
                string count = beat < 0 ? $"Cuenta… {-beat}" : $"Golpe {Mathf.Min(beat + 1, session.Beats)}/{session.Beats}";
                string feedback = Time.time - lastTapTime < 0.35f ? (lastTapHit ? "  ¡A tiempo!" : "  Fuera de tiempo") : "";
                GUI.Box(new Rect(x, y, w, 40f), $"GRABANDO · pulsa F en cada golpe · {count}{feedback}", box);
                // Barra que se llena hasta cada golpe.
                float phase = Mathf.Repeat(t / session.BeatInterval, 1f);
                GUI.Box(new Rect(x, y + 46f, w * phase, 14f), GUIContent.none);
                GUI.Box(new Rect(x, y + 64f, w, 30f), $"Aciertos {session.Hits} · Fallos {session.Misses}", box);
                return;
            }

            if (Time.time < messageUntil)
                GUI.Box(new Rect(x, y, w, 40f), message, box);
            else if (PlayerNear())
                GUI.Box(new Rect(x, y, w, 40f), "Pulsa F para grabar un tema", box);
            else return;

            GUI.Box(new Rect(x, y + 46f, w, 30f),
                $"{career.Rank} · {career.Tracks} temas · {career.Fame} de fama · {Mathf.RoundToInt(career.ProgressToNext * 100f)} % al siguiente rango", box);
        }
    }
}
