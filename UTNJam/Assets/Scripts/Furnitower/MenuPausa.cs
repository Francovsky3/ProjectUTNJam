using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Menú de pausa: Esc lo abre y lo cierra, y tocar fuera de un panel lo cierra.
// Mientras está abierto el juego se congela (Time.timeScale = 0) y no recibe clicks.
public class MenuPausa : MonoBehaviour
{
    public static bool Pausado { get; private set; }

    [SerializeField] GameObject panelPausa;
    [SerializeField] GameObject panelOpciones;
    [SerializeField] Button botonMenuPrincipal;
    [SerializeField] Button botonEmpezarDeNuevo;
    [SerializeField] Button botonOpciones;
    [SerializeField] string escenaMenu = "Menu";
    [Tooltip("Fondo que aparece detrás de un panel abierto; tocarlo cierra el panel")]
    [SerializeField] Color colorFondo = new Color(0f, 0f, 0f, 0.4f);

    GameObject fondoPausa, fondoOpciones;

    void Start()
    {
        fondoPausa = CrearFondo(panelPausa, Reanudar);
        fondoOpciones = CrearFondo(panelOpciones, CerrarOpciones);
        Mostrar(panelPausa, fondoPausa, false);
        Mostrar(panelOpciones, fondoOpciones, false);

        if (botonMenuPrincipal != null) botonMenuPrincipal.onClick.AddListener(() => Escenas.Cargar(escenaMenu));
        if (botonEmpezarDeNuevo != null) botonEmpezarDeNuevo.onClick.AddListener(Escenas.Recargar);
        if (botonOpciones != null) botonOpciones.onClick.AddListener(AbrirOpciones);

        Pausado = false;
    }

    void OnDestroy()
    {
        // Si se cambia de escena desde la pausa, que el juego no quede congelado
        Pausado = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        if (panelOpciones != null && panelOpciones.activeSelf) CerrarOpciones();
        else if (Pausado) Reanudar();
        else Pausar();
    }

    public void Pausar()
    {
        Pausado = true;
        Time.timeScale = 0f;
        Mostrar(panelPausa, fondoPausa, true);
    }

    public void Reanudar()
    {
        Mostrar(panelOpciones, fondoOpciones, false);
        Mostrar(panelPausa, fondoPausa, false);
        Pausado = false;
        Time.timeScale = 1f;
    }

    void AbrirOpciones()
    {
        Mostrar(panelPausa, fondoPausa, false);
        Mostrar(panelOpciones, fondoOpciones, true);
    }

    // Las opciones se abren desde la pausa, así que al cerrarlas se vuelve a ella
    void CerrarOpciones()
    {
        Mostrar(panelOpciones, fondoOpciones, false);
        if (Pausado) Mostrar(panelPausa, fondoPausa, true);
    }

    // Fondo que cubre toda la pantalla justo detrás del panel: tapa el juego y al tocarlo cierra el panel
    GameObject CrearFondo(GameObject panel, UnityAction alTocar)
    {
        if (panel == null) return null;

        GameObject fondo = new GameObject($"Fondo {panel.name}", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = (RectTransform)fondo.transform;
        rt.SetParent(panel.transform.parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetSiblingIndex(panel.transform.GetSiblingIndex());

        fondo.GetComponent<Image>().color = colorFondo;
        Button boton = fondo.GetComponent<Button>();
        boton.transition = Selectable.Transition.None;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };
        boton.onClick.AddListener(alTocar);
        return fondo;
    }

    static void Mostrar(GameObject panel, GameObject fondo, bool visible)
    {
        if (fondo != null) fondo.SetActive(visible);
        if (panel != null) panel.SetActive(visible);
    }
}
