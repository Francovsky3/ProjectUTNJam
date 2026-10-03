using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

// Menú principal: el botón Jugar lleva a la escena del juego
public class MenuPrincipal : MonoBehaviour
{

    [SerializeField] string escenaJuego = "prototipo";
    

    [SerializeField] private Animator albumAnimator;

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
        Escenas.Cargar(escenaJuego);
    }
}
