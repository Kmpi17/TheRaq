using NUnit.Framework;

namespace GTAChiraq.Tests
{
    public class InventoryTests
    {
        [Test]
        public void StacksFillBeforeUsingNewSlots()
        {
            var inv = new Inventory(4);
            Assert.That(inv.Add(ItemCatalog.Medkit, 2), Is.EqualTo(2));
            Assert.That(inv.Add(ItemCatalog.Medkit, 2), Is.EqualTo(2)); // 3 en la primera casilla, 1 en la segunda
            Assert.That(inv[0].Count, Is.EqualTo(3));
            Assert.That(inv[1].Count, Is.EqualTo(1));
            Assert.That(inv.Count(ItemCatalog.Medkit), Is.EqualTo(4));
        }

        [Test]
        public void FullInventoryAcceptsOnlyWhatFits()
        {
            var inv = new Inventory(2);
            inv.Add(ItemCatalog.Pistol, 1);
            Assert.That(inv.SpaceFor(ItemCatalog.Vest), Is.EqualTo(2));
            Assert.That(inv.Add(ItemCatalog.Vest, 5), Is.EqualTo(2));
            Assert.That(inv.Add(ItemCatalog.Wings, 1), Is.EqualTo(0));
            Assert.That(inv.Add("no_existe", 1), Is.EqualTo(0));
        }

        [Test]
        public void RemoveTakesAcrossSlotsAndClearsEmptyOnes()
        {
            var inv = new Inventory(3);
            inv.Add(ItemCatalog.Ammo9mm, 150); // 120 + 30
            Assert.That(inv.Remove(ItemCatalog.Ammo9mm, 40), Is.EqualTo(40));
            Assert.That(inv.Count(ItemCatalog.Ammo9mm), Is.EqualTo(110));
            Assert.That(inv[1].IsEmpty, Is.True);
            Assert.That(inv.Remove(ItemCatalog.Ammo9mm, 999), Is.EqualTo(110));
            Assert.That(inv.Has(ItemCatalog.Ammo9mm), Is.False);
        }

        [Test]
        public void WeaponsAreListedOnceInSlotOrder()
        {
            var inv = new Inventory(4);
            inv.Add(ItemCatalog.Medkit, 1);
            inv.Add(ItemCatalog.Pistol, 2); // no se apilan: dos casillas
            var weapons = inv.Weapons();
            Assert.That(weapons.Count, Is.EqualTo(1));
            Assert.That(weapons[0].Id, Is.EqualTo(ItemCatalog.Pistol));
        }

        [Test]
        public void ChangedFiresOnlyWhenSomethingChanges()
        {
            var inv = new Inventory(1);
            int changes = 0;
            inv.Changed += () => changes++;
            inv.Add(ItemCatalog.Wings, 1);
            inv.Add(ItemCatalog.Medkit, 1); // no cabe
            inv.Remove(ItemCatalog.Medkit, 1); // no hay
            Assert.That(changes, Is.EqualTo(1));
        }
    }
}
