using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    public class TrafficMathTests
    {
        [Test]
        public void StoppingSpeedIsZeroAtTheLineAndGrowsWithDistance()
        {
            Assert.AreEqual(0f, TrafficMath.StoppingSpeed(0f, 4.5f));
            Assert.AreEqual(0f, TrafficMath.StoppingSpeed(-3f, 4.5f));
            Assert.AreEqual(Mathf.Sqrt(2f * 4.5f * 20f), TrafficMath.StoppingSpeed(20f, 4.5f), 1e-4f);
        }

        [Test]
        public void FollowSpeedNeverExceedsCruiseAndStopsAtTheGap()
        {
            Assert.AreEqual(13f, TrafficMath.FollowSpeed(200f, 2.2f, 4.5f, 13f), 1e-4f);
            Assert.AreEqual(0f, TrafficMath.FollowSpeed(2f, 2.2f, 4.5f, 13f));
            Assert.Less(TrafficMath.FollowSpeed(6f, 2.2f, 4.5f, 13f), 13f);
        }

        [Test]
        public void AmberStopsOnlyIfThereIsRoomToBrake()
        {
            Assert.IsTrue(TrafficMath.StopsForAmber(40f, 13f, 4.5f));   // necesita ~19 m
            Assert.IsFalse(TrafficMath.StopsForAmber(8f, 13f, 4.5f));   // ya no le da: pasa
        }

        [Test]
        public void ApproachRespectsAccelerationAndBraking()
        {
            Assert.AreEqual(2.2f, TrafficMath.Approach(0f, 10f, 2.2f, 4.5f, 1f), 1e-4f);
            Assert.AreEqual(5.5f, TrafficMath.Approach(10f, 0f, 2.2f, 4.5f, 1f), 1e-4f);
            Assert.AreEqual(9f, TrafficMath.Approach(8f, 9f, 2.2f, 4.5f, 1f), 1e-4f);
        }

        [Test]
        public void TurnControlIsTheCornerForARightTurnAndTheMiddleGoingStraight()
        {
            // Llega hacia el norte por x = 2 y sale hacia el este por z = 10: la esquina es (2, 10).
            var c = TrafficMath.TurnControl(new Vector3(2f, 0f, 5f), Vector3.forward, new Vector3(7f, 0f, 10f), Vector3.right);
            Assert.AreEqual(2f, c.x, 1e-4f);
            Assert.AreEqual(10f, c.z, 1e-4f);
            var m = TrafficMath.TurnControl(new Vector3(0f, 0f, 0f), Vector3.forward, new Vector3(0f, 0f, 10f), Vector3.forward);
            Assert.AreEqual(5f, m.z, 1e-4f);
        }

        [Test]
        public void BezierGoesFromStartToEnd()
        {
            var a = new Vector3(0f, 0f, 0f);
            var b = new Vector3(10f, 0f, 0f);
            Assert.AreEqual(a, TrafficMath.Bezier(a, new Vector3(5f, 0f, 5f), b, 0f));
            Assert.AreEqual(b, TrafficMath.Bezier(a, new Vector3(5f, 0f, 5f), b, 1f));
        }

        [Test]
        public void OffsetRightKeepsToTheRightOfTheDirectionOfTravel()
        {
            // Hacia +Z, la derecha es +X; hacia −X, la derecha es +Z.
            var north = TrafficMath.OffsetRight(new[] { Vector3.zero, new Vector3(0f, 0f, 10f) }, 2f);
            Assert.AreEqual(2f, north[0].x, 1e-4f);
            Assert.AreEqual(2f, north[1].x, 1e-4f);
            var west = TrafficMath.OffsetRight(new[] { Vector3.zero, new Vector3(-10f, 0f, 0f) }, 2f);
            Assert.AreEqual(2f, west[0].z, 1e-4f);
        }

        [Test]
        public void ChooseNextNeverMakesAUTurnNorEntersADeadEnd()
        {
            var incoming = Vector3.forward;
            var options = new List<Vector3> { Vector3.back, Vector3.right, Vector3.forward, Vector3.left };
            var usable = new List<bool> { true, true, true, false };
            for (float roll = 0f; roll < 1f; roll += 0.01f)
            {
                int pick = TrafficMath.ChooseNext(incoming, options, usable, roll);
                Assert.That(pick == 1 || pick == 2, $"roll {roll}: {pick}");
            }
            // Recto (peso 2) sale el doble que el giro (peso 1).
            int straight = 0;
            for (int i = 0; i < 300; i++) if (TrafficMath.ChooseNext(incoming, options, usable, i / 300f) == 2) straight++;
            Assert.AreEqual(200, straight, 2);
            Assert.AreEqual(-1, TrafficMath.ChooseNext(incoming, new List<Vector3> { Vector3.back }, new List<bool> { true }, 0.5f));
        }

        [Test]
        public void DensityIsLowAtNightAndHighestAtRushHour()
        {
            Assert.Less(TrafficMath.Density(3f), 0.3f);
            Assert.Greater(TrafficMath.Density(17.5f), TrafficMath.Density(13f));
            Assert.Greater(TrafficMath.Density(8f), TrafficMath.Density(13f));
            for (float h = 0f; h < 24f; h += 0.5f)
            {
                float d = TrafficMath.Density(h);
                Assert.That(d >= 0f && d <= 1f, $"{h}: {d}");
            }
        }
    }
}
