using UnityEngine;
using UnityEngine.InputSystem;

// Cambia de escena. Lo llama la animación del logo al terminar (evento "ChangeToScene"),
// o, con "Avanzar Con Click" activado, un click (o Espacio / Enter) en cualquier lado.
public class SceneChange : MonoBehaviour
{
    [SerializeField] string escenaJuego = "prototipo";
    [Tooltip("Si está activo, un click (o Espacio / Enter) en cualquier lado carga la escena")]
    [SerializeField] bool avanzarConClick = false;
    [Tooltip("Segundos al empezar en los que se ignoran los clicks, para no saltear la escena sin querer")]
    [SerializeField] float segundosMinimos = 0.5f;

    float desde;
    bool cargando;

    void Start()
    {
        desde = Time.unscaledTime;
    }

    void Update()
    {
        if (!avanzarConClick || cargando || Time.unscaledTime - desde < segundosMinimos) return;

        Mouse mouse = Mouse.current;
        Keyboard kb = Keyboard.current;
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool tecla = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        if (click || tecla) ChangeToScene();
    }

    public void ChangeToScene()
    {
        if (cargando) return;
        cargando = true;
        Escenas.Cargar(escenaJuego);
    }
}
