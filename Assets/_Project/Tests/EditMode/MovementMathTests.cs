using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    public class MovementMathTests
    {
        [Test]
        public void CameraRelative_ForwardFollowsCameraYaw()
        {
            var direction = MovementMath.CameraRelative(Vector2.up, 90f);

            Assert.That(Vector3.Distance(direction, Vector3.right), Is.LessThan(0.0001f));
        }

        [Test]
        public void CameraRelative_DiagonalIsClampedToUnitLength()
        {
            var direction = MovementMath.CameraRelative(new Vector2(1f, 1f), 0f);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(0f));
        }

        [Test]
        public void JumpVelocity_ReachesRequestedHeight()
        {
            const float gravity = -20f;
            float v = MovementMath.JumpVelocity(1.2f, gravity);

            // h = v² / (2g)
            Assert.That(v * v / (2f * -gravity), Is.EqualTo(1.2f).Within(0.0001f));
        }
    }
}
