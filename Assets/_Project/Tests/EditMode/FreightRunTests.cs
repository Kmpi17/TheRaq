using NUnit.Framework;

namespace GTAChiraq.Tests
{
    public class FreightRunTests
    {
        const float From = -310f, To = 310f, Length = 240f, Speed = 9f;

        [Test]
        public void WaitsThenCrossesEastAndWaitsAgainOutOfTheWorld()
        {
            var s = FreightRun.Waiting(10f, From, To, Length);
            s = FreightRun.Step(s, 5f, Speed, From, To, Length, 60f);
            Assert.IsFalse(s.Running);
            s = FreightRun.Step(s, 6f, Speed, From, To, Length, 60f);
            Assert.IsTrue(s.Running);
            Assert.AreEqual(1, s.Direction);
            Assert.AreEqual(From - Length / 2f, s.Center, 1e-3f); // sale con la cabeza en el borde
            float t = 0f;
            while (s.Running && t < 500f) { s = FreightRun.Step(s, 0.1f, Speed, From, To, Length, 60f); t += 0.1f; }
            Assert.IsFalse(s.Running);
            Assert.Greater(s.Center - Length / 2f, To);          // la cola ya ha salido
            Assert.AreEqual((To - From + Length) / Speed, t, 0.2f);
        }

        [Test]
        public void AlternatesDirection()
        {
            var s = FreightRun.Waiting(0f, From, To, Length);
            s = FreightRun.Step(s, 0.01f, Speed, From, To, Length, 0f);
            Assert.AreEqual(1, s.Direction);
            while (s.Running) s = FreightRun.Step(s, 0.5f, Speed, From, To, Length, 0f);
            s = FreightRun.Step(s, 0.01f, Speed, From, To, Length, 0f);
            Assert.IsTrue(s.Running);
            Assert.AreEqual(-1, s.Direction);
            Assert.AreEqual(To + Length / 2f, s.Center, 1e-3f);
        }

        [Test]
        public void CrossingClosesBeforeTheHeadAndOpensAfterTheTail()
        {
            var s = new FreightRun { Running = true, Direction = 1 };
            const float x = 0f, warning = 85f, clearance = 6f;
            s.Center = -Length / 2f - 100f;          // cabeza a 100 m
            Assert.IsFalse(FreightRun.Blocks(s, Length, x, warning, clearance));
            s.Center = -Length / 2f - 50f;           // cabeza a 50 m
            Assert.IsTrue(FreightRun.Blocks(s, Length, x, warning, clearance));
            s.Center = 0f;                           // cruzando
            Assert.IsTrue(FreightRun.Blocks(s, Length, x, warning, clearance));
            s.Center = Length / 2f + 3f;             // cola a 3 m
            Assert.IsTrue(FreightRun.Blocks(s, Length, x, warning, clearance));
            s.Center = Length / 2f + 10f;            // cola a 10 m: libre
            Assert.IsFalse(FreightRun.Blocks(s, Length, x, warning, clearance));
            s.Direction = -1; s.Center = Length / 2f + 50f; // hacia el oeste, cabeza a 50 m
            Assert.IsTrue(FreightRun.Blocks(s, Length, x, warning, clearance));
        }
    }
}
