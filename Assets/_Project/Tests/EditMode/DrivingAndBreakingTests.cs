using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Lógica pura de la conducción (<see cref="VehicleMath"/>), de lo que se rompe (<see cref="BreakMath"/>) y de lo que
    /// llevan los NPC encima (<see cref="NpcLoot"/>).
    /// </summary>
    public class DrivingAndBreakingTests
    {
        // ---------- VehicleMath ----------

        [Test]
        public void SteerLimit_ShrinksWithSpeedButNeverBelowTheMinimum()
        {
            Assert.AreEqual(36f, VehicleMath.SteerLimit(0f, 36f, 15f, 32f), 1e-3f);
            Assert.AreEqual(15f, VehicleMath.SteerLimit(32f, 36f, 15f, 32f), 1e-3f);
            Assert.AreEqual(15f, VehicleMath.SteerLimit(60f, 36f, 15f, 32f), 1e-3f);
            Assert.AreEqual(VehicleMath.SteerLimit(10f, 36f, 15f, 32f), VehicleMath.SteerLimit(-10f, 36f, 15f, 32f), 1e-3f, "marcha atrás igual");
        }

        [Test]
        public void SmoothSteer_TurnsGraduallyAndReturnsFaster()
        {
            float s = 0f;
            s = VehicleMath.SmoothSteer(s, 1f, 0.1f, 3.5f, 6f);
            Assert.AreEqual(0.35f, s, 1e-4f, "va hacia el tope poco a poco");
            for (int i = 0; i < 10; i++) s = VehicleMath.SmoothSteer(s, 1f, 0.1f, 3.5f, 6f);
            Assert.AreEqual(1f, s, 1e-4f, "y llega");
            s = VehicleMath.SmoothSteer(s, 0f, 0.1f, 3.5f, 6f);
            Assert.AreEqual(0.4f, s, 1e-4f, "al soltar vuelve más deprisa");
            s = VehicleMath.SmoothSteer(0.5f, -1f, 0.1f, 3.5f, 6f);
            Assert.AreEqual(-0.1f, s, 1e-4f, "cambiar de lado también va deprisa");
        }

        [Test]
        public void TargetYawRate_FollowsTheBicycleModel()
        {
            float r = VehicleMath.TargetYawRate(10f, 20f, 2.8f, 10f);
            Assert.AreEqual(10f * Mathf.Tan(20f * Mathf.Deg2Rad) / 2.8f, r, 1e-4f);
            Assert.Greater(r, 0f, "ruedas a la derecha: gira a la derecha");
            Assert.Less(VehicleMath.TargetYawRate(-5f, 20f, 2.8f, 10f), 0f, "marcha atrás: al revés");
            Assert.AreEqual(2.3f, VehicleMath.TargetYawRate(40f, 36f, 2.8f, 2.3f), 1e-4f, "limitado");
            Assert.AreEqual(0f, VehicleMath.TargetYawRate(0f, 36f, 2.8f, 2.3f), 1e-4f, "parado no gira");
            Assert.AreEqual(14f / 20f, VehicleMath.TargetYawRate(20f, 20f, 2.8f, 2.3f, 14f), 1e-4f, "rápido, lo limita el agarre (v·ω ≤ 14)");
        }

        [Test]
        public void EngineRpm_ClimbsInEachGearAndDropsWhenItShifts()
        {
            float low = VehicleMath.EngineRpm(1f, 0f, 4, 9f, out int g1);
            float high = VehicleMath.EngineRpm(8.5f, 0f, 4, 9f, out int g1b);
            float shifted = VehicleMath.EngineRpm(9.5f, 0f, 4, 9f, out int g2);
            Assert.AreEqual(1, g1);
            Assert.AreEqual(1, g1b);
            Assert.AreEqual(2, g2);
            Assert.Greater(high, low, "sube en la marcha");
            Assert.Less(shifted, high, "baja al cambiar");
            Assert.Greater(VehicleMath.EngineRpm(0f, 1f, 4, 9f, out _), VehicleMath.EngineRpm(0f, 0f, 4, 9f, out _), "acelerando parado, sube");
            float top = VehicleMath.EngineRpm(80f, 1f, 4, 9f, out int g4);
            Assert.AreEqual(4, g4);
            Assert.LessOrEqual(top, 1f);
        }

        // ---------- BreakMath ----------

        static float Covered(List<BreakMath.Span> a, List<BreakMath.Span> b)
        {
            float sum = 0f;
            foreach (var s in a) sum += s.Length;
            foreach (var s in b) sum += s.Length;
            return sum;
        }

        [Test]
        public void SplitFence_OpensAGapWhereTheCarHits()
        {
            var kept = new List<BreakMath.Span>();
            var debris = new List<BreakMath.Span>();
            BreakMath.SplitFence(20f, 2f, 2.8f, 0.6f, 1.5f, kept, debris);
            Assert.AreEqual(2, kept.Count, "quedan los dos lados");
            Assert.AreEqual(-10f, kept[0].From, 1e-4f);
            Assert.AreEqual(0.6f, kept[0].To, 1e-4f);
            Assert.AreEqual(3.4f, kept[1].From, 1e-4f);
            Assert.AreEqual(10f, kept[1].To, 1e-4f);
            Assert.AreEqual(2, debris.Count, "2.8 m en trozos de 1.5 como mucho");
            foreach (var s in debris) Assert.LessOrEqual(s.Length, 1.5f + 1e-4f);
            Assert.AreEqual(20f, Covered(kept, debris), 1e-3f, "entre todo, la valla entera");
        }

        [Test]
        public void SplitFence_ShortLeftoversFlyWithTheGap()
        {
            var kept = new List<BreakMath.Span>();
            var debris = new List<BreakMath.Span>();
            BreakMath.SplitFence(10f, -4.5f, 2.8f, 0.6f, 1.5f, kept, debris);
            Assert.AreEqual(1, kept.Count, "el trocito del extremo sale volando");
            Assert.AreEqual(-3.1f, kept[0].From, 1e-4f);
            Assert.AreEqual(-5f, debris[0].From, 1e-4f);
            Assert.AreEqual(10f, Covered(kept, debris), 1e-3f);

            BreakMath.SplitFence(2f, 0f, 2.8f, 0.6f, 1.5f, kept, debris);
            Assert.AreEqual(0, kept.Count, "una valla corta se va entera");
            Assert.AreEqual(2f, Covered(kept, debris), 1e-3f);
        }

        [Test]
        public void LaunchVelocity_GoesForwardUpAndToItsSide()
        {
            var v = BreakMath.LaunchVelocity(new Vector3(0f, 0f, 20f), Vector3.right, 1f);
            Assert.Greater(v.z, 10f);
            Assert.Greater(v.y, 1f);
            Assert.Greater(v.x, 0f);
            Assert.Less(BreakMath.LaunchVelocity(new Vector3(0f, 0f, 20f), Vector3.right, -1f).x, 0f);
        }

        // ---------- NpcLoot ----------

        [Test]
        public void Loot_ArmedNpcCarriesHisGunAndBullets()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var s = NpcLoot.Roll(NpcRole.Gang, armed: true, raider: false, bulletsLeft: 7, armorLeft: 0f, new System.Random(seed));
                Assert.AreEqual(1, s.Items.Count(ItemCatalog.Pistol));
                Assert.GreaterOrEqual(s.Items.Count(ItemCatalog.Ammo9mm), 7, "al menos lo que le quedaba en el cargador");
                Assert.AreEqual(0, s.Items.Count(ItemCatalog.Vest));
            }
        }

        [Test]
        public void Loot_CiviliansCarryLessMoneyAndNoGuns()
        {
            int civ = 0, gang = 0;
            for (int seed = 0; seed < 300; seed++)
            {
                var c = NpcLoot.Roll(NpcRole.Civilian, false, false, 0, 0f, new System.Random(seed));
                Assert.AreEqual(0, c.Items.Count(ItemCatalog.Pistol));
                civ += c.Money;
                gang += NpcLoot.Roll(NpcRole.Gang, false, false, 0, 0f, new System.Random(seed)).Money;
            }
            Assert.Greater(gang, civ * 2, "los pandilleros llevan bastante más dinero");
            Assert.Greater(civ, 0);
        }

        [Test]
        public void Loot_VestOnlyIfHalfOfItIsLeft()
        {
            Assert.AreEqual(1, NpcLoot.Roll(NpcRole.Gang, true, true, 0, 100f, new System.Random(1)).Items.Count(ItemCatalog.Vest));
            Assert.AreEqual(0, NpcLoot.Roll(NpcRole.Gang, true, true, 0, 30f, new System.Random(1)).Items.Count(ItemCatalog.Vest));
        }
    }
}
