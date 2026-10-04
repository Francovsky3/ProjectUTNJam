using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Lo llama la animación del logo al terminar (evento "ChangeToScene").
// Si hay una imagen de "cómo jugar", primero la muestra a pantalla completa
// y con un click (o Espacio / Enter) pasa al juego.
public class SceneChange : MonoBehaviour
{
    [SerializeField] string escenaJuego = "prototipo";

    public void ChangeToScene()
    {
        SceneManager.LoadScene(escenaJuego);
    }
}
