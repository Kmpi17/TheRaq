using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    public class CharacterSystemsTests
    {
        // ---------- Vida ----------

        [Test]
        public void ArmorAbsorbsMostDamageUntilSpent()
        {
            var h = new HealthPool(100f, armor: 20f);
            Assert.That(h.Damage(20f), Is.EqualTo(6f).Within(1e-4f)); // 14 al chaleco
            Assert.That(h.Armor, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(h.Damage(20f), Is.EqualTo(14f).Within(1e-4f)); // solo quedaban 6 de chaleco
            Assert.That(h.Armor, Is.EqualTo(0f));
        }

        [Test]
        public void FallsIgnoreArmorAndDeadCannotHeal()
        {
            var h = new HealthPool(50f, armor: 100f);
            Assert.That(h.Damage(30f, armorProtects: false), Is.EqualTo(30f));
            Assert.That(h.Heal(100f), Is.EqualTo(30f)); // no pasa del máximo
            h.Damage(500f, false);
            Assert.That(h.IsDead, Is.True);
            Assert.That(h.Heal(10f), Is.EqualTo(0f));
        }

        // ---------- Energía ----------

        [Test]
        public void StaminaExhaustsAtZeroAndRecoversAtThirty()
        {
            var s = new StaminaPool(100f);
            s.Drain(150f);
            Assert.That(s.Exhausted, Is.True);
            Assert.That(s.CanSprint, Is.False);
            Assert.That(s.TrySpend(5f), Is.False);
            s.Restore(29f);
            Assert.That(s.Exhausted, Is.True);
            s.Restore(1f);
            Assert.That(s.Exhausted, Is.False);
        }

        [Test]
        public void StaminaRegeneratesOnlyAfterDelayAndFasterWithBoost()
        {
            var s = new StaminaPool(100f);
            s.Drain(50f);
            s.Tick(s.RegenDelay * 0.5f);
            Assert.That(s.Current, Is.EqualTo(50f));
            s.Tick(s.RegenDelay * 0.5f); // ahora se cumple el retraso
            s.Tick(1f);
            float normal = s.Current - 50f;
            Assert.That(normal, Is.GreaterThan(0f));

            var b = new StaminaPool(100f);
            b.Drain(95f);
            b.Boost(10f);
            b.Tick(b.RegenDelay);
            float start = b.Current;
            b.Tick(0.2f);
            Assert.That(b.Current - start, Is.EqualTo(b.RegenRate * b.BoostMultiplier * 0.2f).Within(1e-3f));
        }

        // ---------- Movimiento ----------

        [Test]
        public void AccelerateReachesTargetAndBrakesFaster()
        {
            var v = Vector3.zero;
            var target = Vector3.forward * 4f;
            v = MovementMath.Accelerate(v, target, 10f, 20f, 0.1f);
            Assert.That(v.z, Is.EqualTo(1f).Within(1e-4f));
            for (int i = 0; i < 10; i++) v = MovementMath.Accelerate(v, target, 10f, 20f, 0.1f);
            Assert.That(v, Is.EqualTo(target));
            v = MovementMath.Accelerate(v, Vector3.zero, 10f, 20f, 0.1f);
            Assert.That(v.z, Is.EqualTo(2f).Within(1e-4f)); // frena con la deceleración
        }

        [Test]
        public void TurnsSlowerAtSpeedAndFallsHurtOnlyFromHigh()
        {
            Assert.That(MovementMath.TurnSpeed(0f, 7f, 300f, 900f), Is.EqualTo(900f));
            Assert.That(MovementMath.TurnSpeed(7f, 7f, 300f, 900f), Is.EqualTo(300f));
            float fourMetres = Mathf.Sqrt(2f * 20f * 4f);
            Assert.That(MovementMath.FallDamage(fourMetres), Is.EqualTo(0f));
            Assert.That(MovementMath.FallDamage(20f), Is.GreaterThan(50f));
        }

        [Test]
        public void ClimbClassification()
        {
            Assert.That(ClimbMath.Classify(0.3f, true), Is.EqualTo(ClimbKind.None));
            Assert.That(ClimbMath.Classify(1.0f, false), Is.EqualTo(ClimbKind.Vault));
            Assert.That(ClimbMath.Classify(1.8f, false), Is.EqualTo(ClimbKind.ClimbOver));
            Assert.That(ClimbMath.Classify(1.0f, true), Is.EqualTo(ClimbKind.ClimbUp));
            Assert.That(ClimbMath.Classify(3f, true), Is.EqualTo(ClimbKind.None));
            Assert.That(ClimbMath.StaminaCost(ClimbKind.ClimbOver, 2f), Is.GreaterThan(ClimbMath.StaminaCost(ClimbKind.Vault, 1f)));
        }

        [Test]
        public void VehicleSlideOverClassification()
        {
            Assert.That(ClimbMath.ClassifyVehicle(0.3f), Is.EqualTo(ClimbKind.None));
            Assert.That(ClimbMath.ClassifyVehicle(1.0f), Is.EqualTo(ClimbKind.SlideOver)); // capó
            Assert.That(ClimbMath.ClassifyVehicle(1.45f), Is.EqualTo(ClimbKind.SlideOver)); // techo de un sedán
            Assert.That(ClimbMath.ClassifyVehicle(2.05f), Is.EqualTo(ClimbKind.None)); // techo de una furgoneta: se sube
            Assert.That(ClimbMath.Duration(ClimbKind.SlideOver, 1.4f), Is.GreaterThan(0f));
        }

        // ---------- Disparo ----------

        [Test]
        public void HipFireSpreadsMoreAndScatterStaysInCone()
        {
            Assert.That(CombatMath.Spread(1f, false, 0f, 0f), Is.GreaterThan(CombatMath.Spread(1f, true, 0f, 0f)));
            Assert.That(CombatMath.Spread(1f, true, 4f, 2f), Is.GreaterThan(CombatMath.Spread(1f, true, 0f, 0f)));
            var dir = new Vector3(0.3f, 0.1f, 1f).normalized;
            foreach (var disc in new[] { Vector2.zero, Vector2.right, new Vector2(-0.7f, 0.7f), Vector2.down })
                Assert.That(Vector3.Angle(dir, CombatMath.Scatter(dir, 5f, disc)), Is.LessThanOrEqualTo(5.01f));
        }
    }
}
