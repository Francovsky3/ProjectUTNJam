using UnityEngine;
using UnityEngine.UI;

// Menú principal: el botón Jugar lleva a la escena del juego
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] Button botonJugar;
    [SerializeField] string escenaJuego = "prototipo";

    void Start()
    {
        if (botonJugar != null) botonJugar.onClick.AddListener(Jugar);
    }

    public void Jugar()
    {
        Escenas.Cargar(escenaJuego);
    }
}
