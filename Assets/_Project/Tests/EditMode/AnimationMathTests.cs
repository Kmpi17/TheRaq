using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    public class AnimationMathTests
    {
        [Test]
        public void Gait_StandingStillIsNeutral()
        {
            var p = AnimationMath.Gait(1.3f, 0f, 0f);
            Assert.That(p.HipL, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(p.KneeR, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(p.ShoulderL, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(p.Bob, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Gait_LegsOpposeAndArmsSwingAgainstTheirLeg()
        {
            var p = AnimationMath.Gait(Mathf.PI / 2f, 1f, 0f); // pierna izquierda delante del todo
            Assert.Less(p.HipL, 0f, "Izquierda delante (ángulo negativo).");
            Assert.That(p.HipR, Is.EqualTo(-p.HipL).Within(1e-4f));
            Assert.Greater(p.ShoulderL, 0f, "El brazo izquierdo va atrás cuando la pierna izquierda va delante.");
            Assert.Less(p.ShoulderR, 0f);
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
            var walk = AnimationMath.Gait(Mathf.PI / 2f, 1f, 0f);
            var sprint = AnimationMath.Gait(Mathf.PI / 2f, 1f, 1f);
            Assert.Greater(Mathf.Abs(sprint.HipL), Mathf.Abs(walk.HipL));
            Assert.Less(sprint.ElbowL, walk.ElbowL, "Codos más doblados al esprintar.");
            Assert.Greater(sprint.Lean, walk.Lean);
        }

        [Test]
        public void AdvancePhase_OneCyclePerStride()
        {
            float phase = 0f;
            for (int i = 0; i < 100; i++) phase = AnimationMath.AdvancePhase(phase, AnimationMath.WalkStride, 0f, 0.01f);
            // Un segundo a una zancada por segundo: vuelve casi al principio.
            Assert.That(Mathf.Min(phase, 2f * Mathf.PI - phase), Is.LessThan(0.01f));
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
