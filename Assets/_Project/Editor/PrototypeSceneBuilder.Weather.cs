using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GTAChiraq.Editor
{
    // Hora del día y tiempo: cielo gris degradado, WeatherSystem (sol/luna, niebla, lluvia y nieve) y las luces del
    // mundo que se encienden más de noche (NightLight).
    public static partial class PrototypeSceneBuilder
    {
        const string SkyShader = "GTAChiraq/CieloGris";

        /// <summary>Material del cielo (se crea en SetupLighting, antes de construir nada).</summary>
        static Material SkyMaterial()
        {
            string path = $"{MaterialsDir}/CieloGris.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(SkyShader);
            if (!shader) throw new System.InvalidOperationException($"No se encuentra el shader {SkyShader} (Assets/_Project/Shaders).");
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            var look = DayCycleMath.Look(12f, 0f, 0f);
            material.SetColor("_Top", look.SkyTop);
            material.SetColor("_Horizon", look.SkyHorizon);
            material.SetColor("_Bottom", look.SkyBottom);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Material transparente sin luz para partículas (gotas y copos).</summary>
        static Material ParticleMat(string name, Texture2D texture)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (!shader) throw new System.InvalidOperationException("No se encuentra el shader URP Particles/Unlit.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f); // transparente
            material.SetFloat("_Blend", 0f);   // alfa
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            // Se desvanecen pegadas a la cámara (las gotas que pasan a 30 cm se verían como barras).
            const float near = 1.2f, far = 4f;
            material.SetFloat("_CameraFadingEnabled", 1f);
            material.SetFloat("_CameraNearFadeDistance", near);
            material.SetFloat("_CameraFarFadeDistance", far);
            material.SetVector("_CameraFadeParams", new Vector4(near, 1f / (far - near), 0f, 0f));
            material.EnableKeyword("_FADING_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Crea "Tiempo" con <see cref="WeatherSystem"/> (fuera de "Mundo": no es estático). Las luces de calle
        /// (farolas, farolas del parque y de la autopista, focos de las cocheras) llevan <see cref="NightLight"/> desde
        /// donde se crean; las de interiores, fuegos y el túnel se quedan siempre encendidas.
        /// </summary>
        static void BuildWeather(Transform world)
        {
            var weather = new GameObject("Tiempo").AddComponent<WeatherSystem>();
            SetReference(weather, "sun", RenderSettings.sun);
            SetReference(weather, "skyMaterial", RenderSettings.skybox);
            SetReference(weather, "rainMaterial", ParticleMat("Lluvia", null));
            SetReference(weather, "snowMaterial", ParticleMat("Copo", ProceduralTextures.Snowflake()));
        }
    }
}
