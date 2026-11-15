using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Sesión de grabación en el estudio: suena el beat y hay que pulsar Interact a tiempo en cada golpe.
    /// Lógica pura (sin escena) para testearla en EditMode.
    /// </summary>
    public sealed class StudioSession
    {
        public const int CountInBeats = 4; // una cuenta de entrada antes del primer golpe

        public readonly float BeatInterval;
        public readonly int Beats;
        public readonly float Window;
        readonly bool[] hit;

        public int Hits { get; private set; }
        public int Misses { get; private set; }

        public StudioSession(float beatsPerMinute, int beats, float window)
        {
            BeatInterval = 60f / Mathf.Max(1f, beatsPerMinute);
            Beats = Mathf.Max(1, beats);
            Window = Mathf.Clamp(window, 0.01f, BeatInterval / 2f);
            hit = new bool[Beats];
        }

        /// <summary>Momento (segundos desde que empieza la sesión) del golpe <paramref name="i"/>.</summary>
        public float BeatTime(int i) => (i + CountInBeats) * BeatInterval;

        public float Duration => BeatTime(Beats - 1) + BeatInterval;

        public bool Finished(float time) => time >= Duration;

        /// <summary>Índice del golpe más cercano a <paramref name="time"/> (puede ser &lt; 0 durante la cuenta de entrada).</summary>
        public int NearestBeat(float time) => Mathf.RoundToInt(time / BeatInterval) - CountInBeats;

        /// <summary>Registra una pulsación. Devuelve true si cae a tiempo en un golpe que aún no se había acertado.</summary>
        public bool Tap(float time)
        {
            int i = NearestBeat(time);
            if (i >= 0 && i < Beats && !hit[i] && Mathf.Abs(time - BeatTime(i)) <= Window)
            {
                hit[i] = true;
                Hits++;
                return true;
            }
            Misses++;
            return false;
        }

        /// <summary>Nota de la sesión entre 0 y 1: aciertos sobre golpes, y cada fallo resta medio acierto.</summary>
        public float Score => Mathf.Clamp01((Hits - 0.5f * Misses) / Beats);
    }

    /// <summary>
    /// Progresión musical del jugador: temas grabados y fama. La fama sube con cada tema según la nota de la sesión
    /// y marca el rango (de "Desconocido" a "Leyenda del Southside"). Se guarda en PlayerPrefs.
    /// </summary>
    public sealed class MusicCareer
    {
        public static readonly int[] RankFame = { 0, 100, 300, 700, 1500 };
        public static readonly string[] RankNames = { "Desconocido", "Del bloque", "Conocido en el barrio", "Sonando en la ciudad", "Leyenda del Southside" };
        const string TracksKey = "career.tracks", FameKey = "career.fame";

        public int Tracks { get; private set; }
        public int Fame { get; private set; }

        public MusicCareer(int tracks = 0, int fame = 0)
        {
            Tracks = Mathf.Max(0, tracks);
            Fame = Mathf.Max(0, fame);
        }

        public int RankIndex
        {
            get
            {
                int rank = 0;
                for (int i = 0; i < RankFame.Length; i++)
                    if (Fame >= RankFame[i]) rank = i;
                return rank;
            }
        }

        public string Rank => RankNames[RankIndex];

        /// <summary>Fracción [0, 1] del camino hasta el rango siguiente (1 en el último).</summary>
        public float ProgressToNext
        {
            get
            {
                int r = RankIndex;
                if (r >= RankFame.Length - 1) return 1f;
                return Mathf.InverseLerp(RankFame[r], RankFame[r + 1], Fame);
            }
        }

        /// <summary>Fama que da un tema con nota <paramref name="score"/> (0–1): de 5 a 100, y 25 más si la sesión es casi perfecta.</summary>
        public static int FameFor(float score)
        {
            score = Mathf.Clamp01(score);
            return Mathf.RoundToInt(5f + 95f * score * score) + (score >= 0.9f ? 25 : 0);
        }

        /// <summary>Suma un tema grabado y devuelve la fama ganada.</summary>
        public int RecordTrack(float score)
        {
            int gained = FameFor(score);
            Tracks++;
            Fame += gained;
            return gained;
        }

        public static MusicCareer Load() => new(PlayerPrefs.GetInt(TracksKey, 0), PlayerPrefs.GetInt(FameKey, 0));

        public void Save()
        {
            PlayerPrefs.SetInt(TracksKey, Tracks);
            PlayerPrefs.SetInt(FameKey, Fame);
            PlayerPrefs.Save();
        }
    }
}
