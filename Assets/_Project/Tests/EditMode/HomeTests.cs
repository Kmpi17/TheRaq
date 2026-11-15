using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GTAChiraq.Tests
{
    public class HomeTests
    {
        // ---------- Ropa ----------

        [Test]
        public void StarterOutfit_OwnsWardrobeAndWearsTheStarterClothes()
        {
            var o = Outfit.Starter();
            Assert.That(o.Equipped(ClothingSlot.Head), Is.EqualTo("gorra_roja"));
            Assert.That(o.Equipped(ClothingSlot.Torso), Is.EqualTo("sudadera_negra"));
            foreach (ClothingSlot slot in System.Enum.GetValues(typeof(ClothingSlot)))
                Assert.That(ClothingCatalog.InSlot(slot).Count(d => o.Owns(d.Id)), Is.GreaterThanOrEqualTo(3), slot.ToString());
        }

        [Test]
        public void Equip_ChangesOnlyItsSlotAndNeedsToOwnIt()
        {
            var o = Outfit.Starter();
            int changes = 0;
            o.Changed += () => changes++;

            Assert.IsTrue(o.Equip("plumifero_negro"));
            Assert.That(o.Equipped(ClothingSlot.Torso), Is.EqualTo("plumifero_negro"));
            Assert.That(o.Equipped(ClothingSlot.Head), Is.EqualTo("gorra_roja"));
            Assert.IsFalse(o.Equip("no_existe"));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void OnlyHeadFaceAndNeckCanGoBare()
        {
            var o = Outfit.Starter();
            Assert.IsNull(o.Equipped(ClothingSlot.Face), "Se empieza sin gafas.");
            Assert.IsNull(o.Equipped(ClothingSlot.Neck), "Se empieza sin cadena.");
            Assert.IsTrue(o.Equip("gafas_negras"));
            Assert.IsTrue(o.Equip("cadena_oro"));
            Assert.IsTrue(o.Unequip(ClothingSlot.Face));
            Assert.IsTrue(o.Unequip(ClothingSlot.Neck));
            Assert.IsNull(o.Equipped(ClothingSlot.Face));
            Assert.IsTrue(o.Unequip(ClothingSlot.Head));
            Assert.IsNull(o.Equipped(ClothingSlot.Head));
            Assert.IsFalse(o.Unequip(ClothingSlot.Torso));
            Assert.IsFalse(o.Unequip(ClothingSlot.Legs));
            Assert.IsNotNull(o.Equipped(ClothingSlot.Legs));
        }

        [Test]
        public void Outfit_SurvivesSerializationAndRepairsBrokenText()
        {
            var o = Outfit.Starter();
            o.Equip("gorro_gris");
            o.Equip("botas_trabajo");
            o.Equip("cadena_colgante");
            var back = Outfit.Deserialize(o.Serialize());
            Assert.That(back.Equipped(ClothingSlot.Head), Is.EqualTo("gorro_gris"));
            Assert.That(back.Equipped(ClothingSlot.Feet), Is.EqualTo("botas_trabajo"));
            Assert.That(back.Equipped(ClothingSlot.Neck), Is.EqualTo("cadena_colgante"));
            Assert.IsNull(back.Equipped(ClothingSlot.Face));
            Assert.That(back.Owned.Count(), Is.EqualTo(o.Owned.Count()));

            var broken = Outfit.Deserialize("tiene=gorra_negra;lleva=Head:gorra_negra,Torso:gorra_negra");
            Assert.That(broken.Equipped(ClothingSlot.Head), Is.EqualTo("gorra_negra"));
            Assert.That(broken.Equipped(ClothingSlot.Torso), Is.EqualTo("sudadera_negra"), "Una gorra no va en el torso: vuelve la de inicio.");
            Assert.That(Outfit.Deserialize("basura").Equipped(ClothingSlot.Torso), Is.EqualTo("sudadera_negra"));
            Assert.IsTrue(Outfit.Deserialize("tiene=gorra_roja;lleva=Head:gorra_roja").Owns("cadena_oro"),
                "Una partida vieja también tiene las prendas nuevas del armario básico.");
        }

        // ---------- Caja fuerte ----------

        [Test]
        public void Transfer_MovesWhatFitsAndLeavesTheRest()
        {
            var from = new Inventory(4);
            from.Add(ItemCatalog.Ammo9mm, 100);
            var to = new Inventory(1);
            to.Add(ItemCatalog.Ammo9mm, 80); // montón de 120: caben 40

            Assert.That(Stash.Transfer(from, 0, to), Is.EqualTo(40));
            Assert.That(from.Count(ItemCatalog.Ammo9mm), Is.EqualTo(60));
            Assert.That(to.Count(ItemCatalog.Ammo9mm), Is.EqualTo(120));
            Assert.That(Stash.Transfer(from, 3, to), Is.EqualTo(0), "Casilla vacía.");
        }

        [Test]
        public void Stash_MoneyAndItemsSurviveSerialization()
        {
            var s = new Stash(8);
            s.AddMoney(250);
            Assert.That(s.TakeMoney(400), Is.EqualTo(250), "No se saca más de lo que hay.");
            s.AddMoney(120);
            s.Items.Add(ItemCatalog.Pistol, 1);
            s.Items.Add(ItemCatalog.Medkit, 2);

            var back = Stash.Deserialize(s.Serialize(), 8);
            Assert.That(back.Money, Is.EqualTo(120));
            Assert.That(back.Items.Count(ItemCatalog.Pistol), Is.EqualTo(1));
            Assert.That(back.Items.Count(ItemCatalog.Medkit), Is.EqualTo(2));
            Assert.That(Stash.Deserialize("", 8).Money, Is.EqualTo(0));
        }

        // ---------- Interactuables ----------

        [Test]
        public void FindNearest_PicksClosestInsideItsRadius()
        {
            var a = new GameObject("A").AddComponent<Elevator>();
            var b = new GameObject("B").AddComponent<Elevator>();
            try
            {
                a.Configure(null, "a", 1.5f);
                b.Configure(null, "b", 3f);
                a.transform.position = new Vector3(1f, 0f, 0f);
                b.transform.position = new Vector3(2f, 0f, 0f);
                var both = new Interactable[] { a, b };

                Assert.AreSame(a, Interactable.FindNearest(Vector3.zero, both));
                Assert.AreSame(b, Interactable.FindNearest(new Vector3(-1f, 0f, 0f), both), "A queda fuera de su radio.");
                Assert.IsNull(Interactable.FindNearest(new Vector3(0f, 5f, 0f), both), "Demasiado alto.");
            }
            finally
            {
                Object.DestroyImmediate(a.gameObject);
                Object.DestroyImmediate(b.gameObject);
            }
        }
    }
}
