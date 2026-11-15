namespace GTAChiraq
{
    /// <summary>Capas de física que usa el juego. Los coches van en su propia capa: la cámara no choca con ellos, pero la escalada, la cobertura y las balas sí.</summary>
    public static class GameLayers
    {
        /// <summary>"Ignore Raycast": el jugador.</summary>
        public const int IgnoreRaycast = 2;
        /// <summary>"Vehiculo": todos los coches que se pueden conducir (el del jugador y los aparcados).</summary>
        public const int Vehicle = 6;
        /// <summary>"Personaje": los NPC (la cámara los atraviesa; no tapan la vista de otros NPC).</summary>
        public const int Npc = 7;
    }
}
