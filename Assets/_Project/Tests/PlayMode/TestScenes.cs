using System.Collections;
using UnityEngine.SceneManagement;

namespace GTAChiraq.Tests
{
    /// <summary>
    /// Para los tests que montan su propio mundo: crea una escena vacía y descarga las demás. Hace falta porque los
    /// tests que cargan la escena prototipo (BackPorch, StationStairs) la dejan cargada, con su jugador, su población
    /// de NPC y su tráfico, y eso estorbaría a los tests que vinieran detrás.
    /// </summary>
    public static class TestScenes
    {
        static int count;

        public static IEnumerator Fresh()
        {
            var fresh = SceneManager.CreateScene("TestVacia" + ++count);
            SceneManager.SetActiveScene(fresh);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s != fresh && s.isLoaded)
                {
                    var op = SceneManager.UnloadSceneAsync(s);
                    while (op != null && !op.isDone) yield return null;
                }
            }
            yield return null;
        }
    }
}
