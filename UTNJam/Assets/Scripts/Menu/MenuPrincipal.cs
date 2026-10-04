using Unity.VisualScripting;
using UnityEngine;

// Menú principal: el botón Jugar lleva a la escena del juego
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] GameObject albumAnimator;
    [SerializeField] GameObject optionsPanel;

    [SerializeField] Animator creditsAnimator;
    [SerializeField] Animator logoAnimator;

    void Start()
    {
        // En el panel de créditos los textos están por encima del botón Cerrar y se quedaban con el click:
        // los textos e imágenes que no son parte de un botón dejan de recibir clicks (el fondo del panel queda igual)
        if (creditsAnimator != null)
        {
            foreach (UnityEngine.UI.Graphic grafico in creditsAnimator.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                if (grafico.gameObject != creditsAnimator.gameObject &&
                    grafico.GetComponentInParent<UnityEngine.UI.Button>(true) == null)
                    grafico.raycastTarget = false;
            }
        }
    }

    public void OpenAlbum()
    {
        albumAnimator.GetComponent<Animator>().SetBool("IsOpen", true);
        albumAnimator.GetComponent<AlbumUI>().ActualizarAlbum();
    }

    public void CloseAlbum()
    {
        albumAnimator.GetComponent<Animator>().SetBool("IsOpen", false);
    }

    public void OpenCredits()
    {
        creditsAnimator.SetBool("IsOpen", true);
    }

    public void CloseCredits()
    {
        creditsAnimator.SetBool("IsOpen", false);
    }

    public void ShowOptions()
    {
        optionsPanel.SetActive(true);
    }

    public void HideOptions()
    {
        optionsPanel.SetActive(false);
    }

    public void Jugar()
    {
        logoAnimator.SetBool("playAnim", true);
    }
}
