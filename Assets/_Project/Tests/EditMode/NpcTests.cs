using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    public class NpcTests
    {
        [Test]
        public void BlockSideAreAlliesAndRivalsAreAlliedToEachOther()
        {
            Assert.AreEqual(FactionRelation.Ally, FactionCatalog.Relation(Faction.AshBlock, Faction.Kessler));
            Assert.AreEqual(FactionRelation.Ally, FactionCatalog.Relation(Faction.AshBlock, Faction.Crestline));
            Assert.AreEqual(FactionRelation.Ally, FactionCatalog.Relation(Faction.Dunmore, Faction.Larkin));
            Assert.IsTrue(FactionCatalog.Hostile(Faction.AshBlock, Faction.Dunmore));
            Assert.IsTrue(FactionCatalog.Hostile(Faction.Kessler, Faction.Larkin));
            Assert.IsTrue(FactionCatalog.Hostile(Faction.Larkin, Faction.Crestline));
            Assert.IsFalse(FactionCatalog.Hostile(Faction.None, Faction.Dunmore));
        }

        [Test]
        public void TerritoriesMatchTheMap()
        {
            Assert.AreEqual(Faction.AshBlock, FactionCatalog.TerritoryAt(new Vector3(0f, 0f, 0f)));
            Assert.AreEqual(Faction.Kessler, FactionCatalog.TerritoryAt(new Vector3(232f, 0f, 58f)));
            Assert.AreEqual(Faction.Larkin, FactionCatalog.TerritoryAt(new Vector3(182f, 0f, 190f)));
            Assert.AreEqual(Faction.Dunmore, FactionCatalog.TerritoryAt(new Vector3(30f, 0f, 200f)));
            Assert.AreEqual(Faction.Crestline, FactionCatalog.TerritoryAt(new Vector3(-238f, 0f, 158f)));
            Assert.AreEqual(Faction.None, FactionCatalog.TerritoryAt(new Vector3(0f, 0f, -150f))); // cocheras
            Assert.AreEqual(Faction.None, FactionCatalog.TerritoryAt(new Vector3(-200f, 0f, 0f)));  // instituto
        }

        [Test]
        public void NightHasFewCiviliansAndMoreGangs()
        {
            Assert.Greater(NpcMath.Civilians(1f, 0f, 0f), 15);
            Assert.LessOrEqual(NpcMath.Civilians(0f, 0f, 0f), 3);
            Assert.Greater(NpcMath.GangMembers(0f, 0f, 0f), NpcMath.GangMembers(1f, 0f, 0f));
            Assert.Greater(NpcMath.ArmedChance(0f), NpcMath.ArmedChance(1f));
        }

        [Test]
        public void RainAndSnowEmptyTheStreetsMoreForCivilians()
        {
            float civDrop = 1f - NpcMath.Civilians(1f, 1f, 0f) / (float)NpcMath.Civilians(1f, 0f, 0f);
            float gangDrop = 1f - NpcMath.GangMembers(1f, 1f, 0f) / (float)NpcMath.GangMembers(1f, 0f, 0f);
            Assert.Greater(civDrop, gangDrop);
            Assert.Less(NpcMath.Civilians(1f, 0f, 1f), NpcMath.Civilians(1f, 0f, 0f));
        }

        [Test]
        public void RaidsAreLikelierAtNight()
        {
            Assert.Greater(NpcMath.RaidChance(0f, 0f, 0f), 3f * NpcMath.RaidChance(1f, 0f, 0f));
            Assert.Less(NpcMath.RaidChance(0f, 1f, 0f), NpcMath.RaidChance(0f, 0f, 0f));
        }

        [Test]
        public void ShopsCloseAtNightExceptLiquor()
        {
            Assert.IsTrue(NpcMath.ShopOpen(12f, false));
            Assert.IsFalse(NpcMath.ShopOpen(23f, false));
            Assert.IsFalse(NpcMath.ShopOpen(3f, false));
            Assert.IsTrue(NpcMath.ShopOpen(3f, true));
            Assert.IsTrue(NpcMath.ShopOpen(8f, false));
            Assert.IsFalse(NpcMath.ShopOpen(21f, false));
        }

        [Test]
        public void GangsPreferTheirHabitualCornersAndAvoidEnemySpots()
        {
            float own = NpcMath.SpotScore(NpcRole.Gang, Faction.Dunmore, Faction.Dunmore, true, false, true, 0f, 30f, 0, 3);
            float neutral = NpcMath.SpotScore(NpcRole.Gang, Faction.Dunmore, Faction.None, false, false, true, 0f, 30f, 0, 3);
            float enemy = NpcMath.SpotScore(NpcRole.Gang, Faction.Dunmore, Faction.AshBlock, true, false, true, 0f, 5f, 0, 3);
            Assert.Greater(own, 3f * neutral);
            Assert.AreEqual(0f, enemy);
        }

        [Test]
        public void RainSendsPeopleUnderCoverAndFullOrClosedSpotsAreSkipped()
        {
            float covered = NpcMath.SpotScore(NpcRole.Civilian, Faction.None, Faction.None, false, true, true, 1f, 30f, 0, 2);
            float open = NpcMath.SpotScore(NpcRole.Civilian, Faction.None, Faction.None, false, false, true, 1f, 30f, 0, 2);
            Assert.Greater(covered, 4f * open);
            Assert.AreEqual(0f, NpcMath.SpotScore(NpcRole.Civilian, Faction.None, Faction.None, false, false, true, 0f, 10f, 2, 2));
            Assert.AreEqual(0f, NpcMath.SpotScore(NpcRole.Civilian, Faction.None, Faction.None, false, false, false, 0f, 10f, 0, 2));
        }

        [Test]
        public void SuspicionGrowsFasterCloseAndAtNight()
        {
            Assert.Greater(NpcMath.SuspicionRate(5f, 1f), NpcMath.SuspicionRate(25f, 1f));
            Assert.Greater(NpcMath.SuspicionRate(10f, 0f), NpcMath.SuspicionRate(10f, 1f));
            Assert.Greater(NpcMath.SightRange(0f), NpcMath.SightRange(1f));
            Assert.Less(NpcMath.StareAt, NpcMath.ConfrontAt);
            Assert.Less(NpcMath.ConfrontAt, NpcMath.AttackAt);
        }

        [Test]
        public void FactionOutfitsUseRealClothingAndTheirColour()
        {
            var rng = new System.Random(3);
            foreach (Faction f in System.Enum.GetValues(typeof(Faction)))
                for (int i = 0; i < 20; i++)
                    foreach (var id in FactionCatalog.RandomOutfit(f, rng))
                        Assert.IsNotNull(ClothingCatalog.Get(id), $"{f}: {id}");
            var outfit = Outfit.Of(FactionCatalog.RandomOutfit(Faction.Kessler, rng));
            Assert.IsNotNull(outfit.Equipped(ClothingSlot.Torso));
            Assert.IsNotNull(outfit.Equipped(ClothingSlot.Legs));
        }

        [Test]
        public void SpotHandsOutSeparateSlots()
        {
            var go = new GameObject("Sitio");
            try
            {
                var s = go.AddComponent<NpcSpot>();
                s.Configure(NpcActivity.Hangout, 3, 1.5f, false, true, true);
                var a = new GameObject("A");
                var b = new GameObject("B");
                var pa = s.Claim(a);
                var pb = s.Claim(b);
                Assert.Greater(Vector3.Distance(pa, pb), 1f);
                Assert.AreEqual(2, s.Occupants);
                s.Release(a);
                Assert.AreEqual(1, s.Occupants);
            }
            finally
            {
                Object.DestroyImmediate(go);
                foreach (var extra in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    if (extra.name is "A" or "B") Object.DestroyImmediate(extra);
            }
        }
    }
}
