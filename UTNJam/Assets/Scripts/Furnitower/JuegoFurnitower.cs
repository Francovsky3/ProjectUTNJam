using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Loop del prototipo: armar un grupo en la mesa -> la cámara va a la torre -> apuntar ->
// cae y se congela -> la cámara vuelve a la mesa -> repetir, hasta llegar al jarrón (gana)
// o hasta que la torre se cae o se vuelve inestable (pierde).
[RequireComponent(typeof(MesaDeArmado))]
public class JuegoFurnitower : MonoBehaviour
{
    enum Estado { Intro, Armado, Apuntando, Cayendo, Viendo, Fin }

    [Header("Escena (si se dejan vacíos se buscan solos)")]
    [SerializeField] Camera cam;
    [SerializeField] Collider suelo;
    [SerializeField] float centroTorreX = 0f;
    [Tooltip("Dónde se para la cámara para armar (por ejemplo 'Piece Camera pos'). Vacío = 40 unidades a la derecha de la torre.")]
    [SerializeField] Transform puntoArmado;
    [SerializeField] float restartTimerMax = 2f;
    [SerializeField] float restartTimer;

    [Header("Objetivo")]
    [Tooltip("Altura del jarrón medida desde el piso")]
    [SerializeField] float alturaJarron = 7f;
    [SerializeField] float estabilidadInicial = 100f;
    [Tooltip("Dibuja una línea amarilla a la altura del objetivo (el fondo ya muestra el jarrón)")]
    [SerializeField] bool mostrarLineaObjetivo = false;

    [Header("Costo de estabilidad por grupo")]
    [Tooltip("Cuánto pesa la inclinación con la que queda el grupo (45° = +1 x peso)")]
    [SerializeField] float pesoInclinacion = 1f;
    [Tooltip("Cuánto pesa lo lejos que queda del centro de la torre")]
    [SerializeField] float pesoDesvio = 1f;
    [SerializeField] float anchoReferencia = 2f;

    [Header("Apuntado y caída")]
    [SerializeField] float rangoApuntado = 4f;
    [SerializeField] float alturaLanzamiento = 2f;
    [SerializeField] float tiempoMaxAsentarse = 3f;
    [SerializeField] float tiempoQuieto = 0.4f;
    [SerializeField] float umbralQuieto = 0.05f;

    [Header("Cámara")]
    [SerializeField] float camaraSobrePiso = 4f;
    [SerializeField] float camaraSobreTope = 1f;
    [Tooltip("Segundos aproximados que tarda la cámara en ir de la mesa a la torre")]
    [SerializeField] float tiempoCamara = 0.4f;
    [Tooltip("Segundos que se queda mirando la torre después de que el grupo se asienta")]
    [SerializeField] float pausaTrasCaer = 0.8f;

    [Header("Intro (paneo al empezar)")]
    [Tooltip("Al empezar, la cámara arranca mirando el jarrón (el objetivo), baja hasta el piso y después va a la mesa. Un click la saltea.")]
    [SerializeField] bool mostrarIntro = true;
    [Tooltip("Segundos mirando el jarrón antes de bajar")]
    [SerializeField] float introEsperaArriba = 2f;
    [Tooltip("Segundos que tarda en bajar desde el jarrón hasta el piso")]
    [SerializeField] float introDuracionBajada = 6f;
    [Tooltip("Al empezar, cuánto más arriba de la línea del objetivo se centra la cámara (para ver el jarrón entero)")]
    [SerializeField] float introCentroSobreObjetivo = 0.75f;
    [Tooltip("Segundos mirando el piso antes de ir a la mesa")]
    [SerializeField] float introEsperaAbajo = 0.7f;
    Coroutine intro;

    [Header("Pantalla de fin (UI de la escena)")]
    [Tooltip("Panel que aparece al ganar o perder. Si queda vacío se usa un panel dibujado por código.")]
    [SerializeField] GameObject panelFin;
    [SerializeField] Button botonRehacer;
    [SerializeField] Button botonReiniciar;

    [Header("Pantalla de victoria (UI de la escena)")]
    [Tooltip("Si queda vacío se busca en el Canvas un panel llamado '" + NombrePanelVictoria + "'")]
    [SerializeField] GameObject panelVictoria;
    [Tooltip("Texto donde la X se reemplaza por la cantidad de grupos. Si queda vacío se busca '" + NombreTextoGrupos + "' dentro del panel")]
    [SerializeField] TMP_Text textoGrupos;

    const string NombrePanelVictoria = "victoria";
    const string NombreTextoGrupos = "texto victoria 2";
    string plantillaGrupos;

    [Header("Música de fin")]
    [SerializeField] AudioClip musicaVictoria;
    [SerializeField] AudioClip musicaDerrota;
    AudioClip musicaDeJuego;   // la que sonaba antes del fin, para volver a ella al rehacer

    [Header("Fondos de fin")]
    [Tooltip("Ilustración a pantalla completa al ganar. Después de unos segundos, o con un click, aparece encima el panel de victoria")]
    [SerializeField] Sprite fondoVictoria;
    [Tooltip("Ilustración a pantalla completa al perder. Después de unos segundos, o con un click, aparece encima el panel de derrota")]
    [SerializeField] Sprite fondoDerrota;
    [SerializeField] float segundosFondoFin = 3f;
    [Tooltip("Al ganar: imagen que aparece flotando sobre la ilustración, en la misma posición que tiene dentro de su lienzo")]
    [SerializeField] Sprite textoGanaste;
    [Tooltip("Al ganar: segundos hasta que un click vuelve al menú principal")]
    [SerializeField] float segundosAntesDeVolver = 2f;
    [SerializeField] string escenaMenu = "Menu";
    Image imagenFondoFin;
    Image imagenTextoGanaste;
    float momentoFin;
    AspectRatioFitter ajusteFondoFin;
    Coroutine esperaPanelFin;
    bool panelFinMostrado;

    [Header("Colores")]
    [SerializeField] Color colorJarron = new Color(1f, 0.8f, 0.2f, 0.8f);
    [SerializeField] Color colorGuia = new Color(1f, 1f, 1f, 0.35f);

    MesaDeArmado mesa;
    Estado estado;
    GrupoQueCae grupoActual;
    readonly List<GrupoQueCae> torre = new List<GrupoQueCae>();
    Transform raizTorre;
    SpriteRenderer guia;

    float pisoY, topeTorre, estabilidad;
    Vector2 posArmado;
    Vector3 velocidadCamara;
    float yMinCamaraTorre = float.NegativeInfinity;   // más abajo se vería el color de fondo de la cámara
    float finPausa;
    bool gano;
    bool puedeRehacer;
    string motivoFin = "";

    // Para poder deshacer el último grupo si la torre no aguantó
    GrupoQueCae ultimoGrupo;
    float estabilidadAntes, topeAntes;

    string mensaje = "";
    float mensajeHasta;

    float escalaGui = 1f;
    Rect rectPanel, rectBoton;
    bool botonVisible;
    GUIStyle estiloTexto, estiloCentrado, estiloTitulo, estiloMensaje, estiloBoton;

    float Altura => topeTorre - pisoY;

    void Start()
    {
        if (panelFin != null) panelFin.SetActive(false);
        BuscarPanelVictoria();
        if (panelVictoria != null) panelVictoria.SetActive(false);
        CrearFondoFin();
        if (textoGrupos != null) plantillaGrupos = textoGrupos.text;
        if (botonRehacer != null) botonRehacer.onClick.AddListener(RehacerUltimo);
        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(Reiniciar);

        mesa = GetComponent<MesaDeArmado>();
        if (cam == null) cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (suelo == null)
        {
            GameObject g = GameObject.FindWithTag("Ground");
            if (g != null) suelo = g.GetComponent<Collider>();
        }
        if (cam == null || suelo == null)
        {
            Debug.LogError("JuegoFurnitower: no encontré la cámara o el piso (tag Ground).");
            enabled = false;
            return;
        }

        Physics.SyncTransforms();
        pisoY = suelo.bounds.max.y;
        topeTorre = pisoY;
        estabilidad = estabilidadInicial;

        posArmado = puntoArmado != null
            ? (Vector2)puntoArmado.position
            : new Vector2(centroTorreX + 40f, pisoY + camaraSobrePiso);
        cam.transform.position = new Vector3(posArmado.x, posArmado.y, cam.transform.position.z);

        raizTorre = new GameObject("Torre").transform;
        if (mostrarLineaObjetivo)
            Dibujo.Rectangulo(null, "Altura del jarrón", new Vector2(centroTorreX, pisoY + alturaJarron),
                              new Vector2(rangoApuntado * 2f + 3f, 0.06f), colorJarron, -5);
        guia =Dibujo.Rectangulo(null, "Guía de caída", Vector2.zero, Vector2.one, colorGuia, -5);
        guia.enabled = false;

        mesa.Iniciar(posArmado, cam, this);
        CalcularLimiteCamaraTorre();

        if (mostrarIntro)
        {
            estado = Estado.Intro;
            intro = StartCoroutine(Intro());
        }
        else
        {
            estado = Estado.Armado;
        }
    }

    // Paneo inicial: muestra el objetivo (la altura del jarrón), baja hasta el piso y después va a la mesa
    IEnumerator Intro()
    {
        float z = cam.transform.position.z;
        float yArriba = pisoY + alturaJarron + introCentroSobreObjetivo;
        float yAbajo = Mathf.Max(pisoY + camaraSobrePiso, yMinCamaraTorre);

        cam.transform.position = new Vector3(centroTorreX, yArriba, z);
        yield return new WaitForSeconds(introEsperaArriba);

        for (float t = 0f; t < introDuracionBajada; t += Time.deltaTime)
        {
            float y = Mathf.Lerp(yArriba, yAbajo, Mathf.SmoothStep(0f, 1f, t / introDuracionBajada));
            cam.transform.position = new Vector3(centroTorreX, y, z);
            yield return null;
        }
        cam.transform.position = new Vector3(centroTorreX, yAbajo, z);
        yield return new WaitForSeconds(introEsperaAbajo);

        intro = null;
        TerminarIntro();
    }

    // Altura mínima de la cámara en la torre para no mostrar debajo de los fondos (sprites cuyo nombre empieza con "Fondo").
    // Para varios puntos a lo ancho de la pantalla busca el fondo más bajo que cubre ese punto,
    // y se queda con el borde más alto de esos, así no queda ningún hueco abajo.
    void CalcularLimiteCamaraTorre()
    {
        List<Bounds> fondos = new List<Bounds>();
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
            if (sr.enabled && sr.sprite != null && sr.name.StartsWith("Fondo")) fondos.Add(sr.bounds);
        if (fondos.Count == 0) return;

        float mitadAncho = cam.orthographicSize * cam.aspect;
        float bordeInferior = float.NegativeInfinity;
        const int muestras = 9;
        for (int i = 0; i < muestras; i++)
        {
            float x = centroTorreX - mitadAncho + 2f * mitadAncho * i / (muestras - 1);
            float masBajo = float.PositiveInfinity;
            foreach (Bounds b in fondos)
                if (x >= b.min.x && x <= b.max.x) masBajo = Mathf.Min(masBajo, b.min.y);
            if (!float.IsPositiveInfinity(masBajo)) bordeInferior = Mathf.Max(bordeInferior, masBajo);
        }
        if (!float.IsNegativeInfinity(bordeInferior))
            yMinCamaraTorre = bordeInferior + cam.orthographicSize;
    }

    // Termina la intro (o la saltea): la cámara va a la mesa y empieza el juego
    void TerminarIntro()
    {
        if (estado != Estado.Intro) return;
        if (intro != null)
        {
            StopCoroutine(intro);
            intro = null;
        }
        velocidadCamara = Vector3.zero;
        estado = Estado.Armado;
    }

    void Update()
    {
        if (MenuPausa.Pausado) return;

        Keyboard kb = Keyboard.current;

        if (Input.GetKey(KeyCode.R))
        {
            restartTimer += Time.deltaTime;
            if (restartTimer >= restartTimerMax)
            {
                Reiniciar();
                return;
            }
        }
        else
        {
            restartTimer = 0f;   // hay que mantener la R apretada sin soltarla
        }

        switch (estado)
        {
            case Estado.Intro:
                bool saltear = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                               (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame));
                if (saltear) TerminarIntro();
                break;

            case Estado.Armado:
                mesa.Actualizar();
                if (kb != null && kb.spaceKey.wasPressedThisFrame) Lanzar();
                break;

            case Estado.Apuntando:
                ActualizarApuntado();
                break;

            case Estado.Cayendo:
                if (grupoActual.Estado == GrupoQueCae.Fase.Asentado) Evaluar();
                break;

            case Estado.Viendo:
                if (Time.time >= finPausa) estado = Estado.Armado;
                break;
        }

        // Durante la intro la cámara la mueve el paneo
        if (estado != Estado.Intro) MoverCamara();
    }

    void Lanzar()
    {
        if (mesa.Arrastrando) return;
        if (!mesa.GrupoListo)
        {
            Avisar($"Pegá al menos {mesa.MinPorGrupo} objetos para armar un grupo");
            return;
        }

        grupoActual = mesa.ArmarGrupoParaLanzar();
        grupoActual.Apuntar(XApuntada(), topeTorre + alturaLanzamiento);
        guia.enabled = true;
        estado = Estado.Apuntando;
    }

    void ActualizarApuntado()
    {
        Mouse mouse = Mouse.current;

        float x = XApuntada();
        grupoActual.Apuntar(x, topeTorre + alturaLanzamiento);
        // La guía cae desde el centro de masa: muestra dónde va a cargar el peso sobre la torre
        guia.transform.position = new Vector3(grupoActual.CentroDeMasa.x, topeTorre + alturaLanzamiento / 2f, 0f);
        guia.transform.localScale = new Vector3(0.05f, alturaLanzamiento, 1f);

        // Esc queda para la pausa; el lanzamiento se cancela con click derecho
        bool cancelar = mouse != null && mouse.rightButton.wasPressedThisFrame;
        if (cancelar)
        {
            mesa.DevolverGrupo(grupoActual);
            grupoActual = null;
            guia.enabled = false;
            estado = Estado.Armado;
            return;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !MouseSobreHud())
        {
            guia.enabled = false;
            grupoActual.transform.SetParent(raizTorre, true);
            grupoActual.Soltar(suelo, torre.Count == 0, pisoY - 3f, tiempoMaxAsentarse, tiempoQuieto, umbralQuieto);
            estado = Estado.Cayendo;
        }
    }

    float XApuntada()
    {
        return Mathf.Clamp(MouseEnMundo().x, centroTorreX - rangoApuntado, centroTorreX + rangoApuntado);
    }

    void Evaluar()
    {
        GrupoQueCae g = grupoActual;
        grupoActual = null;
        torre.Add(g);

        ultimoGrupo = g;
        estabilidadAntes = estabilidad;
        topeAntes = topeTorre;

        if (g.SeCayo)
        {
            Terminar(false, "¡Un grupo se cayó de la torre!", true);
            return;
        }

        float costo = g.Inestabilidad * (1f
            + Mathf.Abs(g.Inclinacion) / 45f * pesoInclinacion
            + Mathf.Abs(g.CentroX - centroTorreX) / anchoReferencia * pesoDesvio);
        estabilidad -= costo;
        Avisar($"-{costo:0.#} de estabilidad");

        Physics.SyncTransforms();
        topeTorre = Mathf.Max(topeTorre, g.Limites().max.y);

        if (estabilidad <= 0f)
        {
            estabilidad = 0f;
            Terminar(false, "La torre quedó demasiado inestable", true);
            return;
        }
        if (Altura >= alturaJarron)
        {
            Terminar(true, "¡Llegaste al jarrón de galletitas!");
            return;
        }

        if (mesa.Reponer(torre.Count))
            Avisar("¡Aparecen objetos nuevos!");
        if (!mesa.QuedanObjetosSuficientes)
        {
            Terminar(false, "Te quedaste sin objetos");
            return;
        }

        // Un momento para ver cómo quedó la torre antes de volver a la mesa
        finPausa = Time.time + pausaTrasCaer;
        estado = Estado.Viendo;
    }

    void Terminar(bool gano, string motivo, bool puedeRehacer = false)
    {
        this.gano = gano;
        this.puedeRehacer = puedeRehacer;
        motivoFin = motivo;
        estado = Estado.Fin;

        AudioManager audio = AudioManager.Instance;
        if (audio != null)
        {
            musicaDeJuego = audio.MusicaActual;
            // Suena una sola vez y después vuelve la música del juego
            audio.PlayMusicUnaVez(gano ? musicaVictoria : musicaDerrota, musicaDeJuego);
        }

        // Primero la ilustración de victoria o derrota.
        // Al ganar: el texto "ganaste" flota arriba y, pasados unos segundos, un click vuelve al menú.
        // Al perder: el panel de derrota aparece después de unos segundos o con un click.
        panelFinMostrado = false;
        momentoFin = Time.time;
        Sprite fondo = gano ? fondoVictoria : fondoDerrota;
        if (imagenFondoFin != null && fondo != null)
        {
            imagenFondoFin.sprite = fondo;
            ajusteFondoFin.aspectRatio = fondo.rect.width / fondo.rect.height;
            imagenFondoFin.gameObject.SetActive(true);
            if (imagenTextoGanaste != null) imagenTextoGanaste.gameObject.SetActive(gano);
            if (!gano) esperaPanelFin = StartCoroutine(MostrarPanelFinTrasEspera());
        }
        else
        {
            MostrarPanelFin();
        }
    }

    // Click sobre la ilustración de fin
    void AlTocarFondoFin()
    {
        if (estado != Estado.Fin) return;
        if (!gano)
            MostrarPanelFin();
        else if (Time.time - momentoFin >= segundosAntesDeVolver)
            Escenas.Cargar(escenaMenu);
    }

    IEnumerator MostrarPanelFinTrasEspera()
    {
        yield return new WaitForSeconds(segundosFondoFin);
        esperaPanelFin = null;
        MostrarPanelFin();
    }

    // Muestra el panel de victoria o derrota (encima de la ilustración, si la hay). Se llama una sola vez por fin.
    void MostrarPanelFin()
    {
        if (estado != Estado.Fin || panelFinMostrado) return;
        panelFinMostrado = true;
        if (esperaPanelFin != null)
        {
            StopCoroutine(esperaPanelFin);
            esperaPanelFin = null;
        }

        if (gano && panelVictoria != null)
        {
            panelVictoria.SetActive(true);
            if (textoGrupos != null) textoGrupos.text = plantillaGrupos.Replace("X", torre.Count.ToString());
            return;
        }

        if (panelFin != null)
        {
            panelFin.SetActive(true);
            if (botonRehacer != null) botonRehacer.gameObject.SetActive(!gano && puedeRehacer);
        }
    }

    // Imagen a pantalla completa para las ilustraciones de victoria y derrota, justo detrás de los paneles de fin.
    // Cubre toda la pantalla sin deformarse (puede recortar un poco los bordes) y un click en ella adelanta el panel.
    void CrearFondoFin()
    {
        if (fondoVictoria == null && fondoDerrota == null) return;
        GameObject referencia = panelFin != null ? panelFin : panelVictoria;
        if (referencia == null || referencia.transform.parent == null) return;

        GameObject go = new GameObject("Fondo fin", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter), typeof(Button));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(referencia.transform.parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);

        int indice = referencia.transform.GetSiblingIndex();
        if (panelFin != null) indice = Mathf.Min(indice, panelFin.transform.GetSiblingIndex());
        if (panelVictoria != null) indice = Mathf.Min(indice, panelVictoria.transform.GetSiblingIndex());
        rt.SetSiblingIndex(indice);

        imagenFondoFin = go.GetComponent<Image>();
        ajusteFondoFin = go.GetComponent<AspectRatioFitter>();
        ajusteFondoFin.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        Button boton = go.GetComponent<Button>();
        boton.transition = Selectable.Transition.None;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };
        boton.onClick.AddListener(AlTocarFondoFin);

        go.SetActive(false);

        if (textoGanaste != null) CrearTextoGanaste(rt);
    }

    // El texto "ganaste" va justo encima de la ilustración. Ocupa en la pantalla la misma porción que ocupa
    // dentro de su lienzo, así queda donde lo dibujó el artista en cualquier resolución, y flota.
    void CrearTextoGanaste(RectTransform fondo)
    {
        GameObject go = new GameObject("Texto ganaste", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(fondo.parent, false);
        rt.SetSiblingIndex(fondo.GetSiblingIndex() + 1);

        Rect r = textoGanaste.rect;
        float ancho = textoGanaste.texture.width, alto = textoGanaste.texture.height;
        rt.anchorMin = new Vector2(r.xMin / ancho, r.yMin / alto);
        rt.anchorMax = new Vector2(r.xMax / ancho, r.yMax / alto);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        imagenTextoGanaste = go.GetComponent<Image>();
        imagenTextoGanaste.sprite = textoGanaste;
        imagenTextoGanaste.preserveAspect = true;
        imagenTextoGanaste.raycastTarget = false;   // los clicks pasan a la ilustración

        go.AddComponent<Flotar>();   // toma como centro la posición recién puesta
        go.SetActive(false);
    }

    // Si no se asignaron en el Inspector, se buscan por nombre en el Canvas (al lado del panel de derrota)
    void BuscarPanelVictoria()
    {
        if (panelVictoria == null && panelFin != null && panelFin.transform.parent != null)
        {
            Transform t = panelFin.transform.parent.Find(NombrePanelVictoria);
            if (t != null) panelVictoria = t.gameObject;
        }
        if (textoGrupos == null && panelVictoria != null)
        {
            Transform t = panelVictoria.transform.Find(NombreTextoGrupos);
            if (t != null) textoGrupos = t.GetComponent<TMP_Text>();
        }
    }

    // Saca de la torre el último grupo y lo devuelve a la mesa tal como estaba armado,
    // dejando la estabilidad y la altura como antes de soltarlo
    void RehacerUltimo()
    {
        if (ultimoGrupo == null) return;

        torre.Remove(ultimoGrupo);
        estabilidad = estabilidadAntes;
        topeTorre = topeAntes;
        mesa.DevolverGrupo(ultimoGrupo);
        ultimoGrupo = null;
        if (panelFin != null) panelFin.SetActive(false);
        if (esperaPanelFin != null)
        {
            StopCoroutine(esperaPanelFin);
            esperaPanelFin = null;
        }
        if (imagenFondoFin != null) imagenFondoFin.gameObject.SetActive(false);
        if (imagenTextoGanaste != null) imagenTextoGanaste.gameObject.SetActive(false);

        if (AudioManager.Instance != null && musicaDeJuego != null)
            AudioManager.Instance.PlayMusic(musicaDeJuego);

        Avisar("Corregí el grupo y volvé a soltarlo");
        estado = Estado.Armado;
    }

    // En Armado la cámara mira la mesa; en el resto de los estados mira la torre, siguiendo su altura
    void MoverCamara()
    {
        Vector2 objetivo = estado == Estado.Armado
            ? posArmado
            : new Vector2(centroTorreX, Mathf.Max(pisoY + camaraSobrePiso, topeTorre + camaraSobreTope, yMinCamaraTorre));

        Vector3 p = cam.transform.position;
        Vector3 destino = new Vector3(objetivo.x, objetivo.y, p.z);
        cam.transform.position = Vector3.SmoothDamp(p, destino, ref velocidadCamara, tiempoCamara);
    }

    void Reiniciar()
    {
        Escenas.Recargar();
    }

    public void Avisar(string texto)
    {
        mensaje = texto;
        mensajeHasta = Time.time + 2f;
    }

    public Vector3 MouseEnMundo()
    {
        Vector2 m = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector3 p = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, -cam.transform.position.z));
        p.z = 0f;
        return p;
    }

    public bool MouseSobreHud()
    {
        if (Mouse.current == null) return false;
        Vector2 m = Mouse.current.position.ReadValue();
        Vector2 gui = new Vector2(m.x, Screen.height - m.y) / escalaGui;
        return rectPanel.Contains(gui) || (botonVisible && rectBoton.Contains(gui));
    }

    // ---------- HUD (IMGUI, alcanza para el prototipo) ----------

    void OnGUI()
    {
        if (cam == null || mesa == null) return;

        // En la pantalla de fin se ven la ilustración y los paneles de la escena: el HUD no se dibuja encima
        bool hayPanelUI = gano ? panelVictoria != null || panelFin != null : panelFin != null;
        if (estado == Estado.Fin && hayPanelUI) return;

        escalaGui = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escalaGui, escalaGui, 1f));
        float ancho = Screen.width / escalaGui;
        CrearEstilos();

        // Durante la intro el HUD no se muestra: solo el aviso para saltearla
        if (estado == Estado.Intro)
        {
            GUI.Label(new Rect(0, 720 - 40, ancho, 30), "Click para saltear", estiloCentrado);
            return;
        }

        // En la pantalla de armado el panel va abajo a la izquierda (arriba está la cinta);
        // en la torre va arriba a la izquierda
        bool enMesa = estado == Estado.Armado;
        const float altoTira = 34f;
        rectPanel = enMesa ? new Rect(10, 720 - altoTira - 10 - 92, 340, 92) : new Rect(10, 10, 340, 92);
        float px = rectPanel.x, py = rectPanel.y;
        GUI.Box(rectPanel, GUIContent.none);
        GUI.Label(new Rect(px + 10, py + 4, 110, 24), "Estabilidad", estiloTexto);
        DibujarBarra(new Rect(px + 120, py + 8, 210, 16), estabilidad / estabilidadInicial);
        GUI.Label(new Rect(px + 10, py + 30, 320, 24), $"Altura: {Altura:0.0} / {alturaJarron:0.0}", estiloTexto);
        GUI.Label(new Rect(px + 10, py + 56, 320, 24),
                  $"Grupo: {mesa.CantidadEnGrupo}/{mesa.MaxPorGrupo} objetos  ·  inestabilidad {mesa.InestabilidadGrupo:0.#}", estiloTexto);

        botonVisible = enMesa && !MenuPausa.Pausado;
        rectBoton = new Rect(ancho - 210, 720 - altoTira - 10 - 44, 200, 44);
        if (botonVisible)
        {
            GUI.enabled = mesa.GrupoListo;
            if (GUI.Button(rectBoton, "Soltar grupo [Espacio]", estiloBoton))
            {
                AudioManager.ReproducirSFX(a => a.uiButton);
                Lanzar();
            }
            GUI.enabled = true;
        }

        Rect tira = new Rect(0, 720 - altoTira, ancho, altoTira);
        GUI.Box(tira, GUIContent.none);
        GUI.Label(tira, Instrucciones(), estiloCentrado);

        if (Time.time < mensajeHasta)
            GUI.Label(new Rect(0, 720 - altoTira - 38, ancho, 32), mensaje, estiloMensaje);

        if (estado == Estado.Fin)
            DibujarPanelFin(ancho);
    }

    void DibujarPanelFin(float ancho)
    {
        Rect r = new Rect(ancho / 2f - 240, 720 / 2f - 100, 480, 200);
        GUI.Box(r, GUIContent.none);
        GUI.Box(r, GUIContent.none);

        string titulo = gano ? "¡Ganaste!" : puedeRehacer ? "¡La torre no aguantó!" : "Perdiste";
        GUI.Label(new Rect(r.x, r.y + 18, r.width, 44), titulo, estiloTitulo);
        GUI.Label(new Rect(r.x, r.y + 70, r.width, 30), motivoFin, estiloCentrado);

        Rect botonIzq = new Rect(r.x + 30, r.y + 120, 200, 48);
        Rect botonDer = new Rect(r.xMax - 230, r.y + 120, 200, 48);
        if (!gano && puedeRehacer)
        {
            if (GUI.Button(botonIzq, "Rehacer último grupo", estiloBoton)) RehacerUltimo();
            if (GUI.Button(botonDer, "Empezar de nuevo", estiloBoton)) Reiniciar();
        }
        else
        {
            Rect centro = new Rect(r.center.x - 100, r.y + 120, 200, 48);
            if (GUI.Button(centro, gano ? "Jugar de nuevo" : "Empezar de nuevo", estiloBoton)) Reiniciar();
        }
    }

    string Instrucciones()
    {
        switch (estado)
        {
            case Estado.Armado:
                return $"Arrastrá objetos a la mesa y pegalos tocándose ({mesa.MinPorGrupo} a {mesa.MaxPorGrupo})  ·  " +
                       "Ruedita o Q/E: rotar mientras arrastrás  ·  Click derecho: devolver  ·  Espacio: soltar grupo";
            case Estado.Apuntando:
                return "Mové el mouse para elegir dónde cae  ·  Click: soltar  ·  Click derecho: volver a armar";
            case Estado.Cayendo:
                return "Cayendo...";
            default:
                return "";
        }
    }

    void DibujarBarra(Rect r, float t)
    {
        t = Mathf.Clamp01(t);
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.5f), 0f, 0f);
        Rect lleno = new Rect(r.x, r.y, r.width * t, r.height);
        GUI.DrawTexture(lleno, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Color.Lerp(Color.red, Color.green, t), 0f, 0f);
    }

    void CrearEstilos()
    {
        if (estiloTexto != null) return;

        estiloTexto = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        estiloTexto.normal.textColor = Color.white;
        estiloCentrado = new GUIStyle(estiloTexto) { alignment = TextAnchor.MiddleCenter };
        estiloTitulo = new GUIStyle(estiloCentrado) { fontSize = 34, fontStyle = FontStyle.Bold };
        estiloMensaje = new GUIStyle(estiloCentrado) { fontSize = 20, fontStyle = FontStyle.Bold };
        estiloMensaje.normal.textColor = new Color(1f, 0.85f, 0.3f);
        estiloBoton = new GUIStyle(GUI.skin.button) { fontSize = 15 };
    }

    // ---------- Gizmos para ajustar valores en el editor ----------

    void OnDrawGizmosSelected()
    {
        Collider s = suelo;
        if (s == null)
        {
            GameObject g = GameObject.FindWithTag("Ground");
            if (g != null) s = g.GetComponent<Collider>();
        }
        if (s == null) return;

        float piso = s.bounds.max.y;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(centroTorreX - rangoApuntado - 1f, piso + alturaJarron, 0f),
                        new Vector3(centroTorreX + rangoApuntado + 1f, piso + alturaJarron, 0f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(centroTorreX, piso + 0.5f, 0f), new Vector3(rangoApuntado * 2f, 1f, 0.1f));
    }
}
