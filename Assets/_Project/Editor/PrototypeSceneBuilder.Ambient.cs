using System.Collections.Generic;
using UnityEngine;

namespace GTAChiraq.Editor
{
    /// <summary>
    /// Vida del aire (objeto <c>Ambiente</c>, fuera de "Mundo"): vapor que sale de algunas alcantarillas (el invierno de
    /// Chicago) y humo de las chimeneas de la otra orilla del canal. Son sistemas de partículas sin colisiones con el
    /// material <c>Vapor</c> (textura <see cref="ProceduralTextures.Puff"/>). Se construye al final, cuando "Mundo" ya está.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const int SteamEvery = 5; // una de cada cinco alcantarillas echa vapor

        static void BuildAmbient(Transform world)
        {
            var root = new GameObject("Ambiente").transform;
            var puff = ParticleMat("Vapor", ProceduralTextures.Puff());

            // Vapor de alcantarillas: en orden de posición, para que salga igual cada vez que se genera la escena.
            var manholes = new List<Vector3>();
            foreach (var t in world.GetComponentsInChildren<Transform>(true))
                if (t.name == "Alcantarilla") manholes.Add(t.position);
            manholes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z));
            int count = 0;
            for (int i = 0; i < manholes.Count; i += SteamEvery, count++)
                Emitter(root, "VaporAlcantarilla", manholes[i] + Vector3.up * 0.05f, puff, steam: true, seed: i);

            // Humo de las chimeneas de la otra orilla (BuildSouthSkyline): penachos grandes y lentos que se van con el viento.
            foreach (var (x, z, h) in new[] { (-70f, -600f, 62f), (110f, -640f, 55f), (260f, -590f, 70f) })
                Emitter(root, "HumoChimenea", new Vector3(x, h + 0.5f, z), puff, steam: false, seed: (int)x);
            Debug.Log($"[GTAChiraq] Ambiente: vapor en {count} alcantarillas de {manholes.Count}.");
        }

        static void Emitter(Transform parent, string name, Vector3 position, Material material, bool steam, int seed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // la forma emite hacia +Z local: arriba
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = steam ? new ParticleSystem.MinMaxCurve(3f, 5f) : new ParticleSystem.MinMaxCurve(14f, 20f);
            main.startSpeed = steam ? new ParticleSystem.MinMaxCurve(0.5f, 1.1f) : new ParticleSystem.MinMaxCurve(2f, 3f);
            main.startSize = steam ? new ParticleSystem.MinMaxCurve(0.7f, 1.4f) : new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = steam ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(0.72f, 0.72f, 0.72f, 1f);
            main.maxParticles = steam ? 40 : 60;
            main.flipRotation = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = steam ? 7f : 2f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = steam ? 8f : 4f;
            shape.radius = steam ? 0.3f : 1.8f;

            // Crecen y se desvanecen; el viento los lleva un poco hacia el este.
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, steam ? 2.6f : 3.2f));
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(steam ? 0.32f : 0.5f, 0.15f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(steam ? 0.2f : 1.2f, steam ? 0.5f : 2f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.2f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = steam ? 0.25f : 0.6f;
            noise.frequency = 0.4f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = steam ? 0f : 10f;
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)(seed * 7919 + 13);
            ps.Play();
        }
    }
}
