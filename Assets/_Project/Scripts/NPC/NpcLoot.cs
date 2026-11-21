namespace GTAChiraq
{
    /// <summary>
    /// Lo que lleva encima un NPC y se le puede sacar al registrar su cuerpo (lógica pura, se testea en EditMode).
    /// Petición del usuario (2026-11-17): al morir, el cuerpo se queda y lo que tenía no se tira al suelo: se abre como un
    /// inventario. Todo genérico (como <see cref="ItemCatalog"/>).
    /// </summary>
    public static class NpcLoot
    {
        public const int Slots = 8;

        /// <summary>
        /// Contenido de los bolsillos: dinero (los pandilleros más, los de una incursión aún más), su pistola con las balas
        /// que le quedan si iba armado, a veces comida, una bebida o un botiquín, y el chaleco si le queda la mitad.
        /// </summary>
        public static Stash Roll(NpcRole role, bool armed, bool raider, int bulletsLeft, float armorLeft, System.Random rng)
        {
            var s = new Stash(Slots);
            bool gang = role == NpcRole.Gang;

            if (rng.NextDouble() < (gang ? 0.85 : 0.7))
            {
                int money = gang ? rng.Next(15, 121) : rng.Next(5, 41);
                if (raider) money = money * 3 / 2;
                s.AddMoney(money);
            }

            if (armed)
            {
                s.Items.Add(ItemCatalog.Pistol, 1);
                int ammo = bulletsLeft + rng.Next(0, 13);
                if (ammo > 0) s.Items.Add(ItemCatalog.Ammo9mm, ammo);
            }
            else if (gang && rng.NextDouble() < 0.3) s.Items.Add(ItemCatalog.Ammo9mm, rng.Next(6, 13));

            if (gang)
            {
                if (rng.NextDouble() < 0.12) s.Items.Add(ItemCatalog.Medkit, 1);
                if (rng.NextDouble() < 0.15) s.Items.Add(ItemCatalog.EnergyDrink, 1);
            }
            else
            {
                if (rng.NextDouble() < 0.2) s.Items.Add(ItemCatalog.Wings, rng.Next(1, 3));
                if (rng.NextDouble() < 0.12) s.Items.Add(ItemCatalog.EnergyDrink, 1);
            }

            if (armorLeft >= 50f) s.Items.Add(ItemCatalog.Vest, 1);
            return s;
        }
    }
}
