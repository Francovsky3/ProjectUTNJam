using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Lo llama la animación del logo al terminar (evento "ChangeToScene").
// Si hay una imagen de "cómo jugar", primero la muestra a pantalla completa
// y con un click (o Espacio / Enter) pasa al juego.
public class SceneChange : MonoBehaviour
{
    [SerializeField] string escenaJuego = "prototipo";
    [Tooltip("Imagen de cómo jugar que se muestra antes de empezar. Vacío = se va directo al juego")]
    [SerializeField] Sprite comoJugar;
    [Tooltip("Segundos en los que se ignoran los clicks, para no saltearla sin querer con el mismo click de Jugar")]
    [SerializeField] float segundosMinimos = 0.5f;

    GameObject pantalla;
    float mostradaDesde;
    bool cargando;

    public void ChangeToScene()
    {
        if (comoJugar == null)
        {
            Cargar();
            return;
        }
        if (pantalla == null) MostrarComoJugar();
    }

    void Update()
    {
        if (pantalla == null || cargando || !PasoElTiempoMinimo()) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            Cargar();
    }

    // Imagen a pantalla completa (sin deformarse) por encima de todo el menú
    void MostrarComoJugar()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Cargar();
            return;
        }

        pantalla = new GameObject("Como jugar", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter), typeof(Button));
        RectTransform rt = (RectTransform)pantalla.transform;
        rt.SetParent(canvas.rootCanvas.transform, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.SetAsLastSibling();

        pantalla.GetComponent<Image>().sprite = comoJugar;
        AspectRatioFitter ajuste = pantalla.GetComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ajuste.aspectRatio = comoJugar.rect.width / comoJugar.rect.height;

        Button boton = pantalla.GetComponent<Button>();
        boton.transition = Selectable.Transition.None;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };
        boton.onClick.AddListener(() => { if (PasoElTiempoMinimo()) Cargar(); });

        mostradaDesde = Time.unscaledTime;
    }

    bool PasoElTiempoMinimo()
    {
        return Time.unscaledTime - mostradaDesde >= segundosMinimos;
    }

    void Cargar()
    {
        if (cargando) return;
        cargando = true;
        Escenas.Cargar(escenaJuego);
    }
}
