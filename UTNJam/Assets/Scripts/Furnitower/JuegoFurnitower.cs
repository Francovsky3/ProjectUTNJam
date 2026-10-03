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
    enum Estado { Armado, Apuntando, Cayendo, Viendo, Fin }

    [Header("Escena (si se dejan vacíos se buscan solos)")]
    [SerializeField] Camera cam;
    [SerializeField] Collider suelo;
    [SerializeField] float centroTorreX = 0f;
    [Tooltip("Dónde se para la cámara para armar (por ejemplo 'Piece Camera pos'). Vacío = 40 unidades a la derecha de la torre.")]
    [SerializeField] Transform puntoArmado;

    [Header("Objetivo")]
    [Tooltip("Altura del jarrón medida desde el piso")]
    [SerializeField] float alturaJarron = 7f;
    [SerializeField] float estabilidadInicial = 100f;

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
    GUIStyle estiloTexto, estiloCentrado, estiloTitulo, estiloMensaje, estiloJarron, estiloBoton;

    float Altura => topeTorre - pisoY;

    void Start()
    {
        if (panelFin != null) panelFin.SetActive(false);
        BuscarPanelVictoria();
        if (panelVictoria != null) panelVictoria.SetActive(false);
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
        Dibujo.Rectangulo(null, "Altura del jarrón", new Vector2(centroTorreX, pisoY + alturaJarron),
                          new Vector2(rangoApuntado * 2f + 3f, 0.06f), colorJarron, -5);
        guia = Dibujo.Rectangulo(null, "Guía de caída", Vector2.zero, Vector2.one, colorGuia, -5);
        guia.enabled = false;

        mesa.Iniciar(posArmado, cam, this);
        estado = Estado.Armado;
    }

    void Update()
    {
        if (MenuPausa.Pausado) return;

        Keyboard kb = Keyboard.current;

        switch (estado)
        {
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

        MoverCamara();
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

        Avisar("Corregí el grupo y volvé a soltarlo");
        estado = Estado.Armado;
    }

    // En Armado la cámara mira la mesa; en el resto de los estados mira la torre, siguiendo su altura
    void MoverCamara()
    {
        Vector2 objetivo = estado == Estado.Armado
            ? posArmado
            : new Vector2(centroTorreX, Mathf.Max(pisoY + camaraSobrePiso, topeTorre + camaraSobreTope));

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

        escalaGui = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escalaGui, escalaGui, 1f));
        float ancho = Screen.width / escalaGui;
        CrearEstilos();

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
            if (GUI.Button(rectBoton, "Soltar grupo [Espacio]", estiloBoton)) Lanzar();
            GUI.enabled = true;
        }

        Vector3 jarron = cam.WorldToScreenPoint(new Vector3(centroTorreX + rangoApuntado + 1.5f, pisoY + alturaJarron, 0f));
        GUI.Label(new Rect(jarron.x / escalaGui - 220, (Screen.height - jarron.y) / escalaGui - 26, 220, 24),
                  "Jarrón de galletitas", estiloJarron);

        Rect tira = new Rect(0, 720 - altoTira, ancho, altoTira);
        GUI.Box(tira, GUIContent.none);
        GUI.Label(tira, Instrucciones(), estiloCentrado);

        if (Time.time < mensajeHasta)
            GUI.Label(new Rect(0, 720 - altoTira - 38, ancho, 32), mensaje, estiloMensaje);

        bool hayPanelUI = gano ? panelVictoria != null || panelFin != null : panelFin != null;
        if (estado == Estado.Fin && !hayPanelUI)
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
        estiloJarron = new GUIStyle(estiloTexto) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
        estiloJarron.normal.textColor = new Color(colorJarron.r, colorJarron.g, colorJarron.b, 1f);
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
