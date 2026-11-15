using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Zona de prácticas junto al inicio del jugador (acera sur de la manzana central): tres dianas para probar los
    /// puños y la pistola, y una fila de objetos para probar el inventario. Va fuera de "Mundo" porque se mueve.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        static readonly (string id, int count, float x)[] TrainingPickups =
        {
            (ItemCatalog.Ammo9mm, 24, -8.5f), (ItemCatalog.Medkit, 1, -7f), (ItemCatalog.Wings, 2, -5.5f),
            (ItemCatalog.EnergyDrink, 1, -4f), (ItemCatalog.Vest, 1, -2.5f), (ItemCatalog.Money, 50, -1f),
        };
        static readonly float[] TrainingDummiesX = { 5f, 7.5f, 10f };
        const float TrainingZ = -71.6f;

        static void BuildTrainingArea()
        {
            var root = new GameObject("Practica").transform;
            var burlap = Mat("Arpillera", new Color(0.62f, 0.52f, 0.36f));
            var wood = Mat("MaderaBanco", new Color(0.36f, 0.24f, 0.14f));
            var red = Mat("DianaRoja", new Color(0.75f, 0.1f, 0.1f));
            foreach (float x in TrainingDummiesX)
            {
                var dummy = new GameObject("Diana").transform;
                dummy.SetParent(root, false);
                dummy.localPosition = new Vector3(x, SidewalkTop, TrainingZ);
                dummy.localRotation = Quaternion.Euler(0f, -90f, 0f); // mira al oeste, hacia el inicio
                Primitive(PrimitiveType.Cylinder, "Base", dummy, new Vector3(0f, 0.04f, 0f), new Vector3(0.7f, 0.04f, 0.7f), wood, keepCollider: false);
                Primitive(PrimitiveType.Cylinder, "Poste", dummy, new Vector3(0f, 0.5f, 0f), new Vector3(0.1f, 0.5f, 0.1f), wood, keepCollider: true);
                Primitive(PrimitiveType.Capsule, "Torso", dummy, new Vector3(0f, 1.2f, 0f), new Vector3(0.55f, 0.45f, 0.4f), burlap, keepCollider: true);
                Primitive(PrimitiveType.Sphere, "Cabeza", dummy, new Vector3(0f, 1.82f, 0f), Vector3.one * 0.32f, burlap, keepCollider: true);
                Primitive(PrimitiveType.Cylinder, "Circulo", dummy, new Vector3(0f, 1.25f, 0.2f), new Vector3(0.3f, 0.01f, 0.3f), red, keepCollider: false)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                dummy.gameObject.AddComponent<Health>();
                dummy.gameObject.AddComponent<TrainingDummy>();
            }

            foreach (var (id, count, x) in TrainingPickups)
            {
                var def = ItemCatalog.Get(id);
                var color = def != null ? def.Color : new Color(0.2f, 0.6f, 0.25f);
                var pickup = new GameObject($"Objeto_{id}").transform;
                pickup.SetParent(root, false);
                pickup.localPosition = new Vector3(x, SidewalkTop + 0.45f, TrainingZ - 0.2f);
                var size = id == ItemCatalog.Money ? new Vector3(0.3f, 0.08f, 0.16f) : id == ItemCatalog.Vest ? new Vector3(0.45f, 0.5f, 0.12f) : Vector3.one * 0.3f;
                Box("Modelo", pickup, Vector3.zero, size, Mat($"Objeto_{id}", color), collider: false);
                pickup.gameObject.AddComponent<ItemPickup>().Configure(id, count);
            }
        }
    }
}
