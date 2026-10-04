using UnityEngine;

// Menú principal: el botón Jugar lleva a la escena del juego
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private Animator albumAnimator;
    [SerializeField] Animator logoAnimator;

    public void OpenAlbum()
    {
        albumAnimator.SetBool("IsOpen", true);
    }

    public void CloseAlbum()
    {
        albumAnimator.SetBool("IsOpen", false);
    }

    public void Jugar()
    {
        logoAnimator.SetBool("playAnim", true);
    }
}
