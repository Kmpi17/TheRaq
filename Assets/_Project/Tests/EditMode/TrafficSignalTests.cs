using NUnit.Framework;

namespace GTAChiraq.Tests
{
    public class TrafficSignalTests
    {
        static readonly SignalTiming T = SignalTiming.Default;

        [Test]
        public void NeverGreenOrAmberInBothDirections()
        {
            for (float t = 0f; t < TrafficSignalMath.Cycle(T) * 2f; t += 0.1f)
            {
                var x = TrafficSignalMath.Vehicle(t, true, T);
                var z = TrafficSignalMath.Vehicle(t, false, T);
                Assert.That(x == SignalColor.Red || z == SignalColor.Red, $"t = {t}: X {x}, Z {z}");
            }
        }

        [Test]
        public void GreenThenAmberThenRedWithAllRedGap()
        {
            Assert.That(TrafficSignalMath.Vehicle(0f, true, T), Is.EqualTo(SignalColor.Green));
            Assert.That(TrafficSignalMath.Vehicle(T.Green + 0.5f, true, T), Is.EqualTo(SignalColor.Amber));
            float allRed = T.Green + T.Amber + 0.5f;
            Assert.That(TrafficSignalMath.Vehicle(allRed, true, T), Is.EqualTo(SignalColor.Red));
            Assert.That(TrafficSignalMath.Vehicle(allRed, false, T), Is.EqualTo(SignalColor.Red));
            Assert.That(TrafficSignalMath.Vehicle(TrafficSignalMath.Cycle(T) / 2f + 0.1f, false, T), Is.EqualTo(SignalColor.Green));
        }

        [Test]
        public void CycleRepeats()
        {
            float c = TrafficSignalMath.Cycle(T);
            for (float t = 0f; t < c; t += 0.37f)
                Assert.That(TrafficSignalMath.Vehicle(t + 3f * c, false, T), Is.EqualTo(TrafficSignalMath.Vehicle(t, false, T)));
        }

        [Test]
        public void PedestriansWalkOnlyWithParallelGreen()
        {
            for (float t = 0f; t < TrafficSignalMath.Cycle(T); t += 0.1f)
                foreach (bool alongX in new[] { true, false })
                {
                    var w = TrafficSignalMath.Pedestrian(t, alongX, T);
                    if (w != WalkSignal.DontWalk)
                        Assert.That(TrafficSignalMath.Vehicle(t, alongX, T), Is.EqualTo(SignalColor.Green), $"t = {t}");
                }
        }

        [Test]
        public void HandFlashesBeforeGreenEnds()
        {
            Assert.That(TrafficSignalMath.Pedestrian(1f, true, T), Is.EqualTo(WalkSignal.Walk));
            Assert.That(TrafficSignalMath.Pedestrian(T.Green - T.Clearance + 0.5f, true, T), Is.EqualTo(WalkSignal.Flashing));
            Assert.That(TrafficSignalMath.Pedestrian(T.Green + 0.5f, true, T), Is.EqualTo(WalkSignal.DontWalk));
            Assert.That(TrafficSignalMath.FlashOn(0.2f), Is.True);
            Assert.That(TrafficSignalMath.FlashOn(0.7f), Is.False);
        }
    }
}
