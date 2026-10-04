using UnityEngine;
using UnityEngine.SceneManagement;

// Carga de escenas por nombre. En el editor funciona aunque la escena no esté en Build Settings.
public static class Escenas
{
    public static void Cargar(string nombre)
    {
        Time.timeScale = 1f;   // por si se sale desde la pausa

        if (Application.CanStreamedLevelBeLoaded(nombre))
        {
            SceneManager.LoadScene(nombre);
            return;
        }
#if UNITY_EDITOR
        string ruta = $"Assets/Scenes/{nombre}.unity";
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError($"Escenas: '{nombre}' no está en Build Settings.");
#endif
    }

    public static void Recargar()
    {
        Cargar(SceneManager.GetActiveScene().name);
    }
}
