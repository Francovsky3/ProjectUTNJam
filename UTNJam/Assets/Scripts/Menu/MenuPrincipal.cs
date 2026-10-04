using Unity.VisualScripting;
using UnityEngine;

// Menú principal: el botón Jugar lleva a la escena del juego
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] GameObject albumAnimator;
    [SerializeField] GameObject optionsPanel;

    [SerializeField] Animator creditsAnimator;
    [SerializeField] Animator logoAnimator;

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
