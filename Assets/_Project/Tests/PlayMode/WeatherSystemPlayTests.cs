using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    public class WeatherSystemPlayTests
    {
        [UnityTest]
        public IEnumerator DrivesSunFogAndStreetLights()
        {
            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            var lampGo = new GameObject("Farola");
            var lamp = lampGo.AddComponent<Light>();
            lamp.intensity = 4f;
            lampGo.AddComponent<NightLight>();

            var go = new GameObject("Tiempo");
            go.SetActive(false);
            var weather = go.AddComponent<WeatherSystem>();
            typeof(WeatherSystem).GetField("sun", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(weather, sun);
            go.SetActive(true);
            yield return null;

            Assert.AreEqual(12f, weather.Hour, 0.1f, "empieza a mediodía");
            Assert.AreEqual(0.7f, sun.intensity, 0.01f);
            Assert.IsFalse(lamp.enabled, "de día las farolas están apagadas");
            Assert.AreEqual(260f, RenderSettings.fogEndDistance, 0.5f);

            weather.SetHour(23f);
            yield return null;
            Assert.IsTrue(lamp.enabled, "de noche se encienden");
            Assert.Greater(lamp.intensity, 4f);
            Assert.Less(sun.intensity, 0.2f);

            weather.ForceWeather(Weather.Nieve, 1f, instant: true);
            yield return null;
            Assert.Greater(weather.Snow, 0.9f);
            Assert.Less(RenderSettings.fogEndDistance, 150f, "con nieve se ve menos lejos");
            StringAssert.Contains("Nieve", weather.Describe());

            Object.Destroy(go);
            Object.Destroy(lampGo);
            Object.Destroy(sun.gameObject);
        }
    }
}
