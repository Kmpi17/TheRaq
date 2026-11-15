using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    public class AnimationMathTests
    {
        [Test]
        public void Gait_StandingStillIsNeutral()
        {
            var p = AnimationMath.Gait(1.3f, 0f, 0f, 0f);
            Assert.That(p.HipL, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(p.KneeR, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(p.ShoulderL, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(p.Bob, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Gait_LegsOpposeAndArmsSwingAgainstTheirLeg()
        {
            float leftForward = 0.88f * 2f * Mathf.PI; // pierna izquierda delante del todo, a punto de apoyar
            var p = AnimationMath.Gait(leftForward, 1f, 0f, 0f);
            Assert.Less(p.HipL, -15f, "Izquierda delante (ángulo negativo).");
            Assert.Greater(p.HipR, 0f, "Derecha detrás, despegando.");
            Assert.Greater(p.ShoulderL, 0f, "El brazo izquierdo va atrás cuando la pierna izquierda va delante.");
            Assert.Less(p.ShoulderR, 0f);
            // La derecha hace lo mismo medio ciclo después.
            var q = AnimationMath.Gait(leftForward + Mathf.PI, 1f, 0f, 0f);
            Assert.That(q.HipR, Is.EqualTo(p.HipL).Within(1e-3f));
            Assert.That(q.KneeR, Is.EqualTo(p.KneeL).Within(1e-3f));
        }

        [Test]
        public void Gait_FollowsRealGait_HipFlexesMoreThanItExtendsAndKneeBendsInSwing()
        {
            float minHip = 0f, maxHip = 0f, maxKneeStance = 0f, maxKneeSwing = 0f;
            for (int i = 0; i < 100; i++)
            {
                float u = i / 100f;
                var p = AnimationMath.Gait(u * 2f * Mathf.PI, 1f, 0f, 0f);
                minHip = Mathf.Min(minHip, p.HipL);
                maxHip = Mathf.Max(maxHip, p.HipL);
                if (u < 0.5f) maxKneeStance = Mathf.Max(maxKneeStance, p.KneeL);
                else maxKneeSwing = Mathf.Max(maxKneeSwing, p.KneeL);
            }
            Assert.That(-minHip, Is.InRange(22f, 32f), "Andando, la cadera se flexiona unos 25–30°.");
            Assert.That(maxHip, Is.InRange(6f, 14f), "…y se extiende unos 10°.");
            Assert.That(maxKneeStance, Is.InRange(12f, 24f), "Al cargar el peso la rodilla amortigua ~18°.");
            Assert.That(maxKneeSwing, Is.InRange(55f, 70f), "En el vuelo se dobla ~60°.");
        }

        [Test]
        public void Gait_FootPitchIsWhatTheAnkleGives()
        {
            var p = AnimationMath.Gait(0f, 1f, 0f, 0f); // la izquierda apoya el talón: punta arriba
            Assert.Less(p.FootL, -5f);
            Assert.That(p.HipL + p.KneeL + p.AnkleL, Is.EqualTo(p.FootL).Within(1e-3f));
        }

        [Test]
        public void Cyclic_PassesThroughKeysAndWraps()
        {
            var keys = new[] { 0f, 10f, 20f, 10f };
            Assert.That(AnimationMath.Cyclic(0.25f, keys), Is.EqualTo(10f).Within(1e-4f));
            Assert.That(AnimationMath.Cyclic(1.5f, keys), Is.EqualTo(20f).Within(1e-4f));
            Assert.That(AnimationMath.Cyclic(0.999f, keys), Is.EqualTo(0f).Within(0.2f));
        }

        [Test]
        public void WeightShift_HipGoesOverTheLoadedLegAndTheOtherKneeLoosens()
        {
            AnimationMath.WeightShift(-1f, out float sway, out float roll, out float knee);
            Assert.Less(sway, 0f, "Peso a la izquierda: la pelvis se va a la izquierda.");
            Assert.Less(roll, 0f, "…y cae del lado derecho.");
            Assert.Greater(knee, 5f);
            AnimationMath.WeightShift(0f, out sway, out _, out knee);
            Assert.That(sway, Is.EqualTo(0f));
            Assert.That(knee, Is.EqualTo(0f));
        }

        [Test]
        public void SwingAxis_SwingsTheLegTowardWhereItGoes()
        {
            Assert.That(Vector3.Distance(AnimationMath.SwingAxis(Vector3.forward), Vector3.right), Is.LessThan(1e-4f), "Hacia delante, el eje de siempre.");
            foreach (var dir in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left })
            {
                // Girar en negativo (paso adelante del ciclo) lleva el pie, que cuelga hacia −Y, hacia donde se va.
                var foot = Quaternion.AngleAxis(-30f, AnimationMath.SwingAxis(dir)) * Vector3.down;
                Assert.Greater(Vector3.Dot(foot, dir), 0.4f, $"Yendo hacia {dir}, el pie va hacia allí.");
            }
        }

        [Test]
        public void SideStepHip_TheInnerLegTakesAShortStepSoLegsDoNotCross()
        {
            // De lado hacia la derecha: la pierna izquierda que va hacia la derecha cruzaría por delante de la otra.
            float inner = AnimationMath.SideStepHip(-30f, -1f, Vector3.right);
            float outer = AnimationMath.SideStepHip(-30f, 1f, Vector3.right);
            Assert.Less(Mathf.Abs(inner), Mathf.Abs(outer));
            Assert.That(outer, Is.EqualTo(-30f));
            Assert.That(AnimationMath.SideStepHip(-30f, -1f, Vector3.forward), Is.EqualTo(-30f), "Hacia delante no cambia.");
        }

        [Test]
        public void Gait_SprintIsWiderAndBentMoreThanWalking()
        {
            float swing = 0.8f * 2f * Mathf.PI;
            var walk = AnimationMath.Gait(swing, 1f, 0f, 0f);
            var jog = AnimationMath.Gait(swing, 1f, 1f, 0f);
            var sprint = AnimationMath.Gait(swing, 1f, 1f, 1f);
            Assert.Greater(Mathf.Abs(sprint.HipL), Mathf.Abs(jog.HipL));
            Assert.Greater(Mathf.Abs(jog.HipL), Mathf.Abs(walk.HipL));
            Assert.Less(jog.ElbowL, -70f, "Trotando, codo a unos 90°.");
            Assert.Less(sprint.ElbowL, walk.ElbowL, "Codos más doblados al esprintar.");
            Assert.Greater(sprint.Lean, jog.Lean);
            Assert.Greater(jog.Lean, walk.Lean);
        }

        [Test]
        public void AdvancePhase_OneCyclePerStride()
        {
            float phase = 0f;
            for (int i = 0; i < 100; i++) phase = AnimationMath.AdvancePhase(phase, AnimationMath.WalkStride, 0f, 0f, 0.01f);
            // Un segundo a una zancada por segundo: vuelve casi al principio.
            Assert.That(Mathf.Min(phase, 2f * Mathf.PI - phase), Is.LessThan(0.01f));
            // Cadencias reales: trotando a 4.4 m/s, ~165 pasos/min; esprintando a 7.2, ~190 (no 270 como antes).
            Assert.That(4.4f / AnimationMath.Stride(1f, 0f) * 2f * 60f, Is.InRange(150f, 180f));
            Assert.That(7.2f / AnimationMath.Stride(1f, 1f) * 2f * 60f, Is.InRange(180f, 230f));
        }

        [Test]
        public void SolveTwoBone_ReachesATargetInRange()
        {
            var root = new Vector3(0.3f, 1.47f, 0f);
            var target = new Vector3(0.28f, 1.31f, 0.51f); // empuñadura de la pistola al apuntar
            AnimationMath.SolveTwoBone(root, target, 0.3f, 0.3f, new Vector3(0.5f, -0.6f, -1f), out var up, out var low);
            var end = root + up * 0.3f + low * 0.3f;
            Assert.That(Vector3.Distance(end, target), Is.LessThan(0.002f));
            Assert.That(up.magnitude, Is.EqualTo(1f).Within(1e-4f));
            var elbow = root + up * 0.3f;
            Assert.Less(elbow.z, Vector3.Lerp(root, target, 0.5f).z, "El codo se dobla hacia atrás (hacia el polo).");
        }

        [Test]
        public void SolveTwoBone_StretchesTowardsAFarTarget()
        {
            AnimationMath.SolveTwoBone(Vector3.zero, new Vector3(0f, 0f, 5f), 0.3f, 0.3f, Vector3.down, out var up, out var low);
            Assert.Greater(up.z, 0.99f);
            Assert.Greater(low.z, 0.99f);
        }
    }
}
