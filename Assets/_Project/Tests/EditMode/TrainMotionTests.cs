using NUnit.Framework;

namespace GTAChiraq.Tests
{
    public class TrainMotionTests
    {
        static readonly TrainRoute Route = new()
        {
            Length = 600f, StationDistance = 300f, MaxSpeed = 16f, Acceleration = 1.1f, DwellSeconds = 10f, LayoverSeconds = 20f,
        };

        static TrainState RunUntil(TrainState s, System.Func<TrainState, bool> done, float dt = 0.02f, int maxSteps = 200000)
        {
            for (int i = 0; i < maxSteps && !done(s); i++) s = TrainMotion.Step(s, dt, Route);
            return s;
        }

        [Test]
        public void StopsExactlyAtTheStation()
        {
            var s = RunUntil(TrainMotion.Start(0f, Route), x => x.Phase == TrainPhase.Dwelling);

            Assert.That(s.Phase, Is.EqualTo(TrainPhase.Dwelling));
            Assert.That(s.Distance, Is.EqualTo(Route.StationDistance));
            Assert.That(s.Speed, Is.EqualTo(0f));
        }

        [Test]
        public void NeverExceedsMaxSpeed()
        {
            var s = TrainMotion.Start(0f, Route);
            for (int i = 0; i < 20000; i++)
            {
                s = TrainMotion.Step(s, 0.02f, Route);
                Assert.That(s.Speed, Is.LessThanOrEqualTo(Route.MaxSpeed + 0.0001f));
            }
        }

        [Test]
        public void AfterTheStationReachesTheEndAndStartsOver()
        {
            var s = RunUntil(TrainMotion.Start(0f, Route), x => x.Phase == TrainPhase.Waiting);
            Assert.That(s.Distance, Is.EqualTo(Route.Length));

            s = RunUntil(s, x => x.Phase == TrainPhase.Running);
            Assert.That(s.Distance, Is.EqualTo(0f));
            Assert.That(s.StoppedAtStation, Is.False);
        }

        [Test]
        public void StartingPastTheStationDoesNotGoBack()
        {
            var s = TrainMotion.Start(450f, Route);
            s = RunUntil(s, x => x.Phase != TrainPhase.Running);

            Assert.That(s.Phase, Is.EqualTo(TrainPhase.Waiting));
        }
    }
}
