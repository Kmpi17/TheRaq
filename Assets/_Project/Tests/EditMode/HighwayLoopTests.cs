using NUnit.Framework;

namespace GTAChiraq.Tests
{
    public class HighwayLoopTests
    {
        [Test]
        public void AdvancesAlongTheRoute()
        {
            Assert.That(HighwayLoop.Advance(10f, 5f, 400f), Is.EqualTo(15f).Within(1e-4f));
        }

        [Test]
        public void WrapsToTheStartAfterTheEnd()
        {
            Assert.That(HighwayLoop.Advance(395f, 10f, 400f), Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void InitialDistanceLongerThanTheRouteWraps()
        {
            Assert.That(HighwayLoop.Advance(950f, 0f, 400f), Is.EqualTo(150f).Within(1e-4f));
        }

        [Test]
        public void StaysInsideTheRouteOverManySteps()
        {
            float d = 0f;
            for (int i = 0; i < 100000; i++)
            {
                d = HighwayLoop.Advance(d, 22f * 0.016f, 425f);
                Assert.That(d, Is.GreaterThanOrEqualTo(0f).And.LessThan(425f));
            }
        }

        [Test]
        public void ZeroLengthRouteStaysAtZero()
        {
            Assert.That(HighwayLoop.Advance(12f, 3f, 0f), Is.EqualTo(0f));
        }
    }
}
