using System.Collections;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.TestTools;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Comportamiento de los NPC en un suelo plano con malla de navegación hecha en el momento (sin la escena): el
    /// enemigo te ataca en su barrio, el aliado no, y el civil huye de un disparo.
    /// </summary>
    public class NpcPlayTests
    {
        GameObject ground, player;
        Health playerHealth;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return TestScenes.Fresh();
            NpcAlerts.Clear();
            NpcBrain.ResetPlayerCache();
            Time.timeScale = 4f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var b in Object.FindObjectsByType<NpcBrain>(FindObjectsSortMode.None)) Object.Destroy(b.gameObject);
            if (ground) Object.Destroy(ground);
            if (player) Object.Destroy(player);
            NpcBrain.ResetPlayerCache();
        }

        void World(Vector3 center)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Suelo";
            ground.transform.position = center + Vector3.down * 0.5f;
            ground.transform.localScale = new Vector3(80f, 1f, 80f);
            var surface = ground.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~((1 << GameLayers.IgnoreRaycast) | (1 << GameLayers.Npc));
            surface.BuildNavMesh();

            player = new GameObject("Jugador") { tag = "Player", layer = GameLayers.IgnoreRaycast };
            player.transform.position = center;
            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.height = 1.8f;
            playerHealth = player.AddComponent<Health>();
        }

        static NpcBrain Npc(Vector3 position, Faction faction, NpcRole role, bool armed)
        {
            var go = new GameObject("NPC") { layer = GameLayers.Npc };
            go.transform.position = position;
            go.AddComponent<Health>();
            var brain = go.AddComponent<NpcBrain>();
            brain.Init(faction, role, armed);
            return brain;
        }

        [UnityTest]
        public IEnumerator EnemyInHisTerritoryStaresThenAttacks()
        {
            var center = new Vector3(30f, 0f, 200f); // barrio de Dunmore
            World(center);
            var npc = Npc(center + new Vector3(0f, 0f, 9f), Faction.Dunmore, NpcRole.Gang, armed: true);
            bool stared = false;
            for (float t = 0f; t < 25f && playerHealth.Pool.Current >= playerHealth.Pool.Max; t += Time.deltaTime)
            {
                if (npc.Current is NpcBrain.State.Stare or NpcBrain.State.Confront) stared = true;
                yield return null;
            }
            Assert.IsTrue(stared, "primero se queda mirando o le encara");
            Assert.Less(playerHealth.Pool.Current, playerHealth.Pool.Max, "acaba disparando al jugador");
            Assert.AreEqual(NpcBrain.State.Combat, npc.Current);
        }

        [UnityTest]
        public IEnumerator AllyDoesNotAttackThePlayer()
        {
            var center = new Vector3(232f, 0f, 40f); // Kessler
            World(center);
            var npc = Npc(center + new Vector3(0f, 0f, 6f), Faction.Kessler, NpcRole.Gang, armed: true);
            yield return new WaitForSeconds(10f);
            Assert.AreEqual(playerHealth.Pool.Max, playerHealth.Pool.Current);
            Assert.AreNotEqual(NpcBrain.State.Combat, npc.Current);
            Assert.AreEqual(0f, npc.Suspicion);
        }

        [UnityTest]
        public IEnumerator EnemyOutsideHisTerritoryLeavesThePlayerAlone()
        {
            var center = new Vector3(0f, 0f, -150f); // cocheras: de nadie
            World(center);
            var npc = Npc(center + new Vector3(0f, 0f, 8f), Faction.Larkin, NpcRole.Gang, armed: true);
            yield return new WaitForSeconds(10f);
            Assert.AreEqual(playerHealth.Pool.Max, playerHealth.Pool.Current);
            Assert.AreNotEqual(NpcBrain.State.Combat, npc.Current);
        }

        /// <summary>
        /// Petición del usuario (2026-11-17): al morir, el cuerpo se queda y lo que llevaba no se tira al suelo, se
        /// registra como un inventario. Muerto un armado, se le saca la pistola, las balas y el dinero con F.
        /// </summary>
        [UnityTest]
        public IEnumerator DeadNpcLeavesABodyToSearch()
        {
            var center = new Vector3(0f, 0f, -150f);
            World(center);
            var inventory = player.AddComponent<PlayerInventory>();
            var npc = Npc(center + new Vector3(0f, 0f, 1.2f), Faction.Larkin, NpcRole.Gang, armed: true);
            yield return null;
            int pickupsBefore = Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None).Length;
            npc.Health.TakeDamage(new DamageInfo(500f, DamageKind.Bullet, npc.transform.position + Vector3.up, Vector3.forward, player));
            yield return null;

            Assert.IsTrue(npc.Dead);
            var body = npc.GetComponent<CorpseLoot>();
            Assert.IsNotNull(body, "el cuerpo se puede registrar");
            Assert.AreEqual(pickupsBefore, Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None).Length, "no tira nada al suelo");
            Assert.AreEqual(1, body.Contents.Items.Count(ItemCatalog.Pistol), "lleva su pistola");

            int money = inventory.Money, pistols = inventory.Items.Count(ItemCatalog.Pistol), bodyMoney = body.Contents.Money;
            int ammo = inventory.Items.Count(ItemCatalog.Ammo9mm), bodyAmmo = body.Contents.Items.Count(ItemCatalog.Ammo9mm);
            Assert.AreSame(body, Interactable.FindNearest(player.transform.position, Interactable.All), "F lo registra");
            body.TakeAll(inventory);
            Assert.AreEqual(money + bodyMoney, inventory.Money, "el dinero pasa al jugador");
            Assert.AreEqual(pistols + 1, inventory.Items.Count(ItemCatalog.Pistol), "y la pistola");
            Assert.AreEqual(ammo + bodyAmmo, inventory.Items.Count(ItemCatalog.Ammo9mm), "y las balas");
            Assert.IsTrue(body.Empty, "el cuerpo se queda vacío");

            yield return new WaitForSeconds(15f); // a ×4: un minuto
            Assert.IsTrue(npc, "el cuerpo no desaparece");
        }

        [UnityTest]
        public IEnumerator CivilianRunsFromAGunshot()
        {
            var center = new Vector3(0f, 0f, -150f);
            World(center);
            var npc = Npc(center + new Vector3(0f, 0f, 6f), Faction.None, NpcRole.Civilian, armed: false);
            yield return new WaitForSeconds(0.5f);
            var start = npc.transform.position;
            NpcAlerts.Gunshot(center, player, null);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(NpcBrain.State.Flee, npc.Current);
            yield return new WaitForSeconds(2f);
            Assert.Greater(Vector3.Distance(npc.transform.position, center), Vector3.Distance(start, center) + 2f, "se aleja del disparo");
        }

        [UnityTest]
        public IEnumerator ShootingAnAllyMakesHimHoldAGrudge()
        {
            var center = new Vector3(0f, 0f, 0f); // Ash Block
            World(center);
            var npc = Npc(center + new Vector3(0f, 0f, 6f), Faction.AshBlock, NpcRole.Gang, armed: true);
            yield return null;
            Assert.IsFalse(npc.HostileToPlayer);
            npc.Health.TakeDamage(new DamageInfo(10f, DamageKind.Bullet, npc.transform.position, Vector3.forward, player));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(npc.Grudge);
            Assert.AreEqual(NpcBrain.State.Combat, npc.Current);
        }
    }
}
