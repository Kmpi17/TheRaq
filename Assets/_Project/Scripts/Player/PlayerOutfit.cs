using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq
{
    /// <summary>
    /// Va en el jugador. Guarda su <see cref="Outfit"/> (en PlayerPrefs, clave "jugador.ropa") y lo pinta en el hijo
    /// <c>Modelo</c>: busca las piezas por nombre (las crea el generador), enciende o apaga las que dependen del estilo
    /// (capucha, faldón de camiseta XL, chaleco, botonera, cuellos, mangas cortas, bolsillos de pantalón, caña de bota,
    /// gorros, gafas y cadena), ensancha las holgadas y les pone el color y la textura del tejido con un
    /// MaterialPropertyBlock, encima del material "Ropa_&lt;prenda&gt;" que pone el generador.
    /// </summary>
    public sealed class PlayerOutfit : MonoBehaviour
    {
        const string PrefsKey = "jugador.ropa";
        static readonly Color Skin = new(0.36f, 0.24f, 0.17f);
        /// <summary>La camiseta que va debajo del chaleco.</summary>
        static readonly Color UnderTee = new(0.9f, 0.9f, 0.88f);
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        [Tooltip("Guarda la ropa entre partidas. Los tests lo apagan.")]
        [SerializeField] bool persist = true;
        [Tooltip("Textura de tejido de cada estilo, en el orden de ClothingStyle (la pone el generador).")]
        [SerializeField] Texture2D[] styleTextures = new Texture2D[0];
        [SerializeField] Texture2D skinTexture;
        // Escala de reposo de las piezas, guardada al vestir el modelo en el editor (PreviewInEditor): así, al arrancar,
        // ensanchar no se acumula sobre lo que ya ensanchó el editor.
        [SerializeField, HideInInspector] string[] restNames = new string[0];
        [SerializeField, HideInInspector] Vector3[] restScales = new Vector3[0];

        readonly Dictionary<string, Transform> parts = new();
        readonly Dictionary<Transform, Vector3> baseScale = new();
        MaterialPropertyBlock block;
        bool initialized;

        public Outfit Outfit { get; private set; }

        /// <summary>Lo usa el generador de escena.</summary>
        public void SetTextures(Texture2D[] byStyle, Texture2D skin)
        {
            styleTextures = byStyle;
            skinTexture = skin;
        }

        /// <summary>
        /// Lo usa el generador: viste el modelo con la ropa de inicio en el editor (piezas encendidas y holguras), para
        /// que salga bien en la escena y en las capturas, y guarda las escalas de reposo.
        /// </summary>
        public void PreviewInEditor()
        {
            Outfit = Outfit.Starter();
            Init();
            var names = new List<string>();
            var scales = new List<Vector3>();
            foreach (var kv in parts)
            {
                names.Add(kv.Key);
                scales.Add(baseScale[kv.Value]);
            }
            restNames = names.ToArray();
            restScales = scales.ToArray();
            Apply();
        }

        void Awake()
        {
            Outfit = persist ? Outfit.Deserialize(PlayerPrefs.GetString(PrefsKey, "")) : Outfit.Starter();
            Outfit.Changed += OnChanged;
            Init();
            Apply();
        }

        void Init()
        {
            if (initialized) return;
            initialized = true;
            var model = transform.Find("Modelo");
            if (model)
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                    if (!parts.ContainsKey(t.name))
                    {
                        parts[t.name] = t;
                        baseScale[t] = t.localScale;
                    }
            if (restNames != null && restScales != null)
                for (int i = 0; i < Mathf.Min(restNames.Length, restScales.Length); i++)
                    if (parts.TryGetValue(restNames[i], out var t))
                        baseScale[t] = restScales[i];
        }

        void OnDestroy()
        {
            if (Outfit != null) Outfit.Changed -= OnChanged;
        }

        void OnChanged()
        {
            Apply();
            if (persist)
            {
                PlayerPrefs.SetString(PrefsKey, Outfit.Serialize());
                PlayerPrefs.Save();
            }
        }

        Texture2D TextureOf(ClothingDef def)
        {
            int i = (int)def.Style;
            return styleTextures != null && i < styleTextures.Length ? styleTextures[i] : null;
        }

        /// <summary>Pinta la ropa puesta en el modelo.</summary>
        public void Apply()
        {
            var head = ClothingCatalog.Get(Outfit.Equipped(ClothingSlot.Head));
            var torso = ClothingCatalog.Get(Outfit.Equipped(ClothingSlot.Torso));
            var legs = ClothingCatalog.Get(Outfit.Equipped(ClothingSlot.Legs));
            var feet = ClothingCatalog.Get(Outfit.Equipped(ClothingSlot.Feet));
            var face = ClothingCatalog.Get(Outfit.Equipped(ClothingSlot.Face));
            var neck = ClothingCatalog.Get(Outfit.Equipped(ClothingSlot.Neck));
            string[] sides = { "Izq", "Der" };

            // Cabeza: gorra, gorro de lana, gorro de pescador o pañuelo; o nada (se ve el pelo rapado).
            var headStyle = head?.Style;
            Show("Gorra", headStyle == ClothingStyle.Cap);
            Show("Gorro", headStyle == ClothingStyle.Beanie);
            Show("Pescador", headStyle == ClothingStyle.BucketHat);
            Show("Panuelo", headStyle == ClothingStyle.Durag);
            if (head != null)
            {
                string piece = head.Style switch
                {
                    ClothingStyle.Cap => "Gorra",
                    ClothingStyle.Beanie => "Gorro",
                    ClothingStyle.BucketHat => "Pescador",
                    _ => "Panuelo",
                };
                Paint(piece, head.Color, TextureOf(head));
            }

            // Cara y cuello: gafas de sol (se tiñe la montura) y cadena (con colgante o sin él).
            Show("Gafas", face != null);
            if (face != null) Paint("Montura", face.Color, null);
            Show("Cadena", neck != null);
            Show("Colgante", neck is { Style: ClothingStyle.ChainPendant });
            if (neck != null) Paint("Cadena", neck.Color, null);

            // Torso. Sudadera: capucha, bolsillo y cordones. Plumífero: más ancho, acolchado y con cuello alto. Camiseta
            // XL: ancha y larga (faldón hasta medio muslo), con manga corta. Chaleco: encima de una camiseta XL blanca.
            // Polo: manga corta y cuello. Camisa: manga larga, cuello y botonera.
            if (torso != null)
            {
                var tex = TextureOf(torso);
                var style = torso.Style;
                bool vest = style == ClothingStyle.Vest;
                bool shortSleeve = style is ClothingStyle.Tee or ClothingStyle.Polo or ClothingStyle.Vest;
                bool longTee = style is ClothingStyle.Tee or ClothingStyle.Vest;
                // Lo que se ve del cuerpo: la camiseta de debajo en el chaleco.
                var bodyColor = vest ? UnderTee : torso.Color;
                var bodyTex = vest ? TextureOf(ClothingCatalog.Get("camiseta_blanca")) : tex;

                Show("Capucha", style == ClothingStyle.Hoodie);
                Show("Cordones", style == ClothingStyle.Hoodie);
                Show("CuelloAlto", style == ClothingStyle.Puffer);
                Show("Faldon", longTee);
                Show("CuelloCamiseta", longTee);
                Show("CuelloCamisa", style is ClothingStyle.Polo or ClothingStyle.Shirt);
                Show("Botonera", style == ClothingStyle.Shirt);
                Show("Chaleco", vest);
                Show("AberturaChaleco", vest);
                Widen("Torso", style == ClothingStyle.Puffer ? 1.14f : longTee ? 1.07f : 1f);

                Paint("Torso", bodyColor, bodyTex);
                Paint("Faldon", bodyColor, bodyTex);
                Paint("CuelloCamiseta", bodyColor, bodyTex);
                Paint("AberturaChaleco", bodyColor, bodyTex);
                Paint("Capucha", torso.Color, tex);
                Paint("CuelloAlto", torso.Color, tex);
                Paint("CuelloCamisa", torso.Color, tex);
                Paint("Botonera", torso.Color * 0.8f, tex);
                Paint("Chaleco", torso.Color, tex);
                foreach (var s in sides)
                {
                    Show("Manga" + s, shortSleeve);
                    Paint("Manga" + s, bodyColor, bodyTex);
                    Paint("Deltoide" + s, bodyColor, bodyTex);
                    Paint("Brazo" + s, shortSleeve ? Skin : torso.Color, shortSleeve ? skinTexture : tex);
                    Paint("Antebrazo" + s, shortSleeve ? Skin : torso.Color, shortSleeve ? skinTexture : tex);
                    Widen("Manga" + s, longTee ? 1.12f : 1f);
                    Widen("Brazo" + s, style == ClothingStyle.Puffer ? 1.25f : 1f);
                    Widen("Antebrazo" + s, style == ClothingStyle.Puffer ? 1.2f : 1f);
                }
            }

            // Piernas (y la pelvis): vaqueros anchos amontonados en el tobillo, chándal holgado con puño, caqui con
            // bolsillos a los lados.
            if (legs != null)
            {
                var tex = TextureOf(legs);
                float loose = legs.Style switch
                {
                    ClothingStyle.Jeans => 1.14f,
                    ClothingStyle.Khakis => 1.12f,
                    ClothingStyle.Sweatpants => 1.12f,
                    _ => 1f,
                };
                Paint("Pelvis", legs.Color, tex);
                foreach (var s in sides)
                {
                    foreach (var piece in new[] { "Muslo" + s, "Espinilla" + s })
                    {
                        Widen(piece, loose);
                        Paint(piece, legs.Color, tex);
                    }
                    Widen("Bajo" + s, legs.Style == ClothingStyle.Sweatpants ? 0.85f : 1f);
                    Paint("Bajo" + s, legs.Color, tex);
                    Show("BolsilloCargo" + s, legs.Style == ClothingStyle.Khakis);
                    Paint("BolsilloCargo" + s, legs.Color, tex);
                }
            }

            // Pies: las botas llevan caña (el pantalón se amontona encima).
            if (feet != null)
                foreach (var s in sides)
                {
                    Show("Cana" + s, feet.Style == ClothingStyle.Boots);
                    Paint("Cana" + s, feet.Color, TextureOf(feet));
                    Paint("Pie" + s, feet.Color, TextureOf(feet));
                }
        }

        void Show(string part, bool on)
        {
            if (parts.TryGetValue(part, out var t)) t.gameObject.SetActive(on);
        }

        void Widen(string part, float factor)
        {
            if (!parts.TryGetValue(part, out var t)) return;
            var s = baseScale[t];
            t.localScale = new Vector3(s.x * factor, s.y, s.z * factor);
        }

        /// <summary>Pinta la pieza y todo lo que cuelga de ella (sin pasar a otra articulación).</summary>
        void Paint(string part, Color color, Texture2D texture)
        {
            if (!parts.TryGetValue(part, out var t)) return;
            block ??= new MaterialPropertyBlock();
            block.Clear();
            block.SetColor(BaseColor, color);
            if (texture) block.SetTexture(BaseMap, texture);
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.SetPropertyBlock(block);
        }
    }
}
