using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú principal: el botón Jugar lleva a la escena del juego
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] Button botonJugar;
    [SerializeField] string escenaJuego = "prototipo";

    [SerializeField] private Animator albumAnimator;
    [SerializeField] private Animator creditosAnimator;

    public void OpenAlbum()
    {
        albumAnimator.SetBool("IsOpen", true);
    }

    public void CloseAlbum()
    {
        albumAnimator.SetBool("IsOpen", false);
    }
    public void OpenCreditos()
    {
        creditosAnimator.SetBool("IsOpen", true);
    }

    public void CloseCreditos()
    {
        creditosAnimator.SetBool("IsOpen", false);
    }

    void Start()
    {
        if (botonJugar != null) botonJugar.onClick.AddListener(Jugar);
    }

    public void Jugar()
    {
        if (Application.CanStreamedLevelBeLoaded(escenaJuego))
        {
            SceneManager.LoadScene(escenaJuego);
            return;
        }
#if UNITY_EDITOR
        // En el editor funciona aunque la escena no esté en Build Settings
        string ruta = $"Assets/Scenes/{escenaJuego}.unity";
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError($"MenuPrincipal: la escena '{escenaJuego}' no está en Build Settings.");
#endif
    }
}
