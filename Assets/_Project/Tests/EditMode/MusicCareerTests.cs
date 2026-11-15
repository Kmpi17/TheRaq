using NUnit.Framework;

namespace GTAChiraq.Tests
{
    public class MusicCareerTests
    {
        [Test]
        public void TapOnTheBeatCountsAsHit()
        {
            var s = new StudioSession(60f, 4, 0.15f);
            Assert.That(s.Tap(s.BeatTime(0) + 0.1f), Is.True);
            Assert.That(s.Hits, Is.EqualTo(1));
        }

        [Test]
        public void TapOutsideTheWindowOrDuringCountInIsAMiss()
        {
            var s = new StudioSession(60f, 4, 0.15f);
            Assert.That(s.Tap(s.BeatTime(0) - 2f), Is.False);   // cuenta de entrada
            Assert.That(s.Tap(s.BeatTime(1) + 0.4f), Is.False); // tarde
            Assert.That(s.Misses, Is.EqualTo(2));
        }

        [Test]
        public void SameBeatCannotBeHitTwice()
        {
            var s = new StudioSession(60f, 4, 0.15f);
            s.Tap(s.BeatTime(2));
            Assert.That(s.Tap(s.BeatTime(2) + 0.05f), Is.False);
            Assert.That(s.Hits, Is.EqualTo(1));
        }

        [Test]
        public void PerfectSessionScoresOneAndMissesLowerIt()
        {
            var s = new StudioSession(72f, 8, 0.16f);
            for (int i = 0; i < 8; i++) s.Tap(s.BeatTime(i));
            Assert.That(s.Score, Is.EqualTo(1f));
            s.Tap(0.01f);
            s.Tap(0.02f);
            Assert.That(s.Score, Is.EqualTo((8f - 1f) / 8f).Within(1e-5f));
            Assert.That(s.Finished(s.Duration), Is.True);
        }

        [Test]
        public void FameGrowsWithScoreAndRanksGoUp()
        {
            Assert.That(MusicCareer.FameFor(0f), Is.EqualTo(5));
            Assert.That(MusicCareer.FameFor(1f), Is.EqualTo(125));
            var c = new MusicCareer();
            Assert.That(c.Rank, Is.EqualTo(MusicCareer.RankNames[0]));
            c.RecordTrack(1f);
            Assert.That(c.Tracks, Is.EqualTo(1));
            Assert.That(c.RankIndex, Is.EqualTo(1));
            Assert.That(c.ProgressToNext, Is.EqualTo((125f - 100f) / 200f).Within(1e-5f));
        }

        [Test]
        public void LastRankHasFullProgress()
        {
            var c = new MusicCareer(50, 5000);
            Assert.That(c.Rank, Is.EqualTo(MusicCareer.RankNames[^1]));
            Assert.That(c.ProgressToNext, Is.EqualTo(1f));
        }
    }
}
