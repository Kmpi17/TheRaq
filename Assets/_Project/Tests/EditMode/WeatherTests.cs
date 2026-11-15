using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    public class WeatherTests
    {
        static void AssertColor(Color expected, Color actual, string what)
        {
            Assert.AreEqual(expected.r, actual.r, 0.002f, what + " (r)");
            Assert.AreEqual(expected.g, actual.g, 0.002f, what + " (g)");
            Assert.AreEqual(expected.b, actual.b, 0.002f, what + " (b)");
        }

        static float Brightness(Color c) => (c.r + c.g + c.b) / 3f;

        [Test]
        public void NoonWithoutWeather_IsTheOriginalDayLook()
        {
            var look = DayCycleMath.Look(12f, 0f, 0f);
            Assert.AreEqual(1f, look.Daylight, 1e-4f);
            Assert.AreEqual(0.7f, look.SunIntensity, 1e-4f);
            AssertColor(new Color(0.8f, 0.84f, 0.92f), look.SunColor, "sol");
            AssertColor(new Color(0.45f, 0.47f, 0.52f), look.Ambient, "ambiente");
            AssertColor(new Color(0.56f, 0.58f, 0.62f), look.Fog, "niebla");
            Assert.AreEqual(25f, look.FogStart, 1e-3f);
            Assert.AreEqual(260f, look.FogEnd, 1e-3f);
            Assert.AreEqual(0f, look.LampFactor, 1e-4f, "de día las farolas están apagadas");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(40f, -35f, 0f), look.SunRotation), 0.01f);
            AssertColor(look.Fog, look.SkyHorizon, "el horizonte del cielo es la niebla");
        }

        [Test]
        public void TwentyMinutesIsAFullDay()
        {
            Assert.AreEqual(12f, DayCycleMath.HourAt(0f, 12f), 1e-4f);
            Assert.AreEqual(0f, DayCycleMath.HourAt(600f, 12f), 1e-3f);
            Assert.AreEqual(12f, DayCycleMath.HourAt(1200f, 12f), 1e-3f);
            Assert.AreEqual(13f, DayCycleMath.HourAt(50f, 12f), 1e-3f, "una hora de juego son 50 s");
        }

        [Test]
        public void NightIsDarkWithBrighterStreetLights()
        {
            var day = DayCycleMath.Look(12f, 0f, 0f);
            var night = DayCycleMath.Look(23f, 0f, 0f);
            Assert.AreEqual(0f, night.Daylight, 1e-4f);
            Assert.Less(Brightness(night.Ambient), Brightness(day.Ambient) * 0.5f);
            Assert.Less(Brightness(night.Fog), Brightness(day.Fog) * 0.4f);
            Assert.Less(night.SunIntensity, 0.2f, "de noche la luz direccional es la luna");
            Assert.AreEqual(1f, night.LampFactor, 1e-4f);
            Assert.Less(night.ParticleTint, 0.5f, "de noche la lluvia no brilla");
            Assert.Less(Brightness(night.SkyTop), Brightness(night.SkyHorizon), "el horizonte brilla con la ciudad");
        }

        [Test]
        public void LightChangesSmoothlyThroughDuskAndDawn()
        {
            var prev = DayCycleMath.Look(0f, 0f, 0f);
            for (float h = 0.01f; h <= 24f; h += 0.01f)
            {
                var look = DayCycleMath.Look(h, 0f, 0f);
                Assert.Less(Mathf.Abs(look.SunIntensity - prev.SunIntensity), 0.02f, $"salto de la luz a las {h:0.00}");
                Assert.Less(Mathf.Abs(Brightness(look.Ambient) - Brightness(prev.Ambient)), 0.01f, $"salto del ambiente a las {h:0.00}");
                prev = look;
            }
        }

        [Test]
        public void RainDarkensAndSnowWhitensTheFog()
        {
            var clear = DayCycleMath.Look(12f, 0f, 0f);
            var rain = DayCycleMath.Look(12f, 1f, 0f);
            var snow = DayCycleMath.Look(12f, 0f, 1f);
            Assert.Less(Brightness(rain.Fog), Brightness(clear.Fog));
            Assert.Less(rain.FogEnd, clear.FogEnd);
            Assert.Less(rain.SunIntensity, clear.SunIntensity);
            Assert.Greater(Brightness(snow.Fog), Brightness(clear.Fog));
            Assert.Less(snow.FogEnd, rain.FogEnd, "con nieve se ve menos lejos");
        }

        [Test]
        public void Clock_FormatsHours()
        {
            Assert.AreEqual("12:00", DayCycleMath.Clock(12f));
            Assert.AreEqual("07:30", DayCycleMath.Clock(7.5f));
            Assert.AreEqual("00:15", DayCycleMath.Clock(24.25f));
        }

        [Test]
        public void Weather_NeverRepeatsRainOrSnowAndDurationsAreSensible()
        {
            for (float r = 0f; r < 1f; r += 0.01f)
            {
                Assert.AreNotEqual(Weather.Lluvia, WeatherMath.Next(Weather.Lluvia, r));
                Assert.AreNotEqual(Weather.Nieve, WeatherMath.Next(Weather.Nieve, r));
                Assert.That(WeatherMath.Duration(Weather.Lluvia, r), Is.InRange(120f, 360f));
                Assert.That(WeatherMath.Duration(Weather.Nieve, r), Is.InRange(180f, 420f));
                Assert.That(WeatherMath.Duration(Weather.Nublado, r), Is.InRange(300f, 720f));
                Assert.That(WeatherMath.Strength(Weather.Lluvia, r), Is.InRange(0.45f, 1f));
            }
            Assert.AreEqual(0f, WeatherMath.Strength(Weather.Nublado, 0.9f));
        }

        [Test]
        public void Weather_InTheLongRunIsMostlyDry()
        {
            var rng = new System.Random(7);
            var w = Weather.Nublado;
            float dry = 0f, rain = 0f, snow = 0f;
            for (int i = 0; i < 20000; i++)
            {
                float t = WeatherMath.Duration(w, (float)rng.NextDouble());
                if (w == Weather.Nublado) dry += t; else if (w == Weather.Lluvia) rain += t; else snow += t;
                w = WeatherMath.Next(w, (float)rng.NextDouble());
            }
            float total = dry + rain + snow;
            Assert.That(dry / total, Is.InRange(0.6f, 0.8f), "casi siempre seco");
            Assert.That(snow / total, Is.InRange(0.12f, 0.28f), "nieva a menudo (invierno)");
            Assert.That(rain / total, Is.InRange(0.05f, 0.18f), "llueve de vez en cuando");
        }

        [Test]
        public void Weather_FadesInOverFortySeconds()
        {
            float v = 0f;
            for (int i = 0; i < 20; i++) v = WeatherMath.Approach(v, 1f, 1f);
            Assert.AreEqual(0.5f, v, 1e-4f);
            for (int i = 0; i < 40; i++) v = WeatherMath.Approach(v, 1f, 1f);
            Assert.AreEqual(1f, v, 1e-4f);
        }
    }
}
