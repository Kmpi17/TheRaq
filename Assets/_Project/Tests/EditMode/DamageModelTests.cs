using NUnit.Framework;
using UnityEngine;

namespace GTAChiraq.Tests
{
    public class DamageModelTests
    {
        static float Pistol => ItemCatalog.Get(ItemCatalog.Pistol).Damage;

        [Test]
        public void PistolShotsToKillByZone()
        {
            Assert.That(DamageModel.ShotsToKill(Pistol, BodyZone.Head), Is.EqualTo(1));
            Assert.That(DamageModel.ShotsToKill(Pistol, BodyZone.Torso), Is.EqualTo(2));
            Assert.That(DamageModel.ShotsToKill(Pistol, BodyZone.Arm), Is.EqualTo(6));
            Assert.That(DamageModel.ShotsToKill(Pistol, BodyZone.Leg), Is.EqualTo(6));
        }

        [Test]
        public void VestOnlyProtectsTheTorso()
        {
            Assert.That(DamageModel.ShotsToKill(Pistol, BodyZone.Torso, armor: 100f), Is.GreaterThanOrEqualTo(4));
            Assert.That(DamageModel.ShotsToKill(Pistol, BodyZone.Torso, armor: 50f), Is.EqualTo(3));
            Assert.That(DamageModel.ShotsToKill(Pistol, BodyZone.Head, armor: 100f), Is.EqualTo(1));
            Assert.That(DamageModel.ShotsToKill(Pistol, BodyZone.Leg, armor: 100f), Is.EqualTo(6));
        }

        [Test]
        public void BiggerBulletsNeedFewerShots()
        {
            Assert.That(DamageModel.ShotsToKill(70f, BodyZone.Leg), Is.LessThan(6));
            Assert.That(DamageModel.ShotsToKill(110f, BodyZone.Torso), Is.EqualTo(1));
            Assert.That(DamageModel.ShotsToKill(30f, BodyZone.Torso), Is.GreaterThan(2));
        }

        [Test]
        public void PunchesAndFallsAreNotLethalByZone()
        {
            var h = new HealthPool(100f);
            DamageModel.Apply(h, 24f, DamageKind.Melee, BodyZone.Head);
            Assert.That(h.Current, Is.EqualTo(64f).Within(1e-3f));
            var f = new HealthPool(100f, armor: 100f);
            DamageModel.Apply(f, 30f, DamageKind.Fall, BodyZone.Unknown);
            Assert.That(f.Current, Is.EqualTo(70f).Within(1e-3f));
            Assert.That(f.Armor, Is.EqualTo(100f));
        }

        [Test]
        public void HealthFindsTheZoneFromTheBones()
        {
            // Esqueleto mínimo de pie con los nombres de BuildPlayerBody.
            var root = new GameObject("Personaje");
            var health = root.AddComponent<Health>();
            Transform Bone(string name, Transform parent, Vector3 pos)
            {
                var t = new GameObject(name).transform;
                t.SetParent(parent, false);
                t.position = pos;
                return t;
            }
            var pelvis = Bone("Cuerpo", root.transform, new Vector3(0f, 0.92f, 0f));
            Bone("Craneo", pelvis, new Vector3(0f, 1.58f, 0f));
            foreach (var (side, x) in new[] { ("Izq", -1f), ("Der", 1f) })
            {
                var sh = Bone("Hombro" + side, pelvis, new Vector3(x * 0.235f, 1.42f, 0f));
                var el = Bone("Codo" + side, sh, new Vector3(x * 0.25f, 1.12f, 0f));
                Bone("Mano" + side, el, new Vector3(x * 0.25f, 0.82f, 0f));
                var hp = Bone("Cadera" + side, pelvis, new Vector3(x * 0.1f, 0.9f, 0f));
                var kn = Bone("Rodilla" + side, hp, new Vector3(x * 0.1f, 0.46f, 0f));
                Bone("Tobillo" + side, kn, new Vector3(x * 0.1f, 0.06f, 0f));
            }

            Assert.That(health.ZoneAt(new Vector3(0f, 1.7f, 0.12f)), Is.EqualTo(BodyZone.Head));
            Assert.That(health.ZoneAt(new Vector3(0f, 1.25f, 0.3f)), Is.EqualTo(BodyZone.Torso));   // de frente al pecho
            Assert.That(health.ZoneAt(new Vector3(0.33f, 1.2f, 0f)), Is.EqualTo(BodyZone.Arm));    // de lado, en el brazo
            Assert.That(health.ZoneAt(new Vector3(0.1f, 0.6f, 0.3f)), Is.EqualTo(BodyZone.Leg));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void WithoutBonesItCountsAsTorso()
        {
            var go = new GameObject("Diana");
            var health = go.AddComponent<Health>();
            Assert.That(health.ZoneAt(Vector3.up), Is.EqualTo(BodyZone.Unknown));
            health.TakeDamage(new DamageInfo(50f, DamageKind.Bullet, Vector3.up, Vector3.forward, null));
            Assert.That(health.Pool.Current, Is.EqualTo(50f).Within(1e-3f));
            Object.DestroyImmediate(go);
        }
    }
}
