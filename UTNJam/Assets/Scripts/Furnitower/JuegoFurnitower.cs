using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Loop del prototipo: armar un grupo en la mesa -> apuntar -> cae y se congela -> repetir
// hasta llegar al jarrón (gana) o hasta que la torre se cae o se vuelve inestable (pierde).
[RequireComponent(typeof(MesaDeArmado))]
public class JuegoFurnitower : MonoBehaviour
{
    enum Estado { Armado, Apuntando, Cayendo, Fin }

    [Header("Escena (si se dejan vacíos se buscan solos)")]
    [SerializeField] Camera cam;
    [SerializeField] Collider suelo;
    [SerializeField] float centroTorreX = 0f;

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
    [SerializeField] float rangoApuntado = 3f;
    [SerializeField] float alturaLanzamiento = 2f;
    [SerializeField] float tiempoMaxAsentarse = 3f;
    [SerializeField] float tiempoQuieto = 0.4f;
    [SerializeField] float umbralQuieto = 0.05f;

    [Header("Cámara")]
    [SerializeField] float desplazamientoCamaraX = -3f;
    [SerializeField] float camaraSobrePiso = 4f;
    [SerializeField] float camaraSobreTope = 0.5f;
    [SerializeField] float suavizadoCamara = 3f;

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
    bool gano;
    string motivoFin = "";
    string mensaje = "";
    float mensajeHasta;

    float escalaGui = 1f;
    Rect rectPanel, rectBoton;
    bool botonVisible;
    GUIStyle estiloTexto, estiloCentrado, estiloTitulo, estiloMensaje, estiloJarron, estiloBoton;

    float Altura => topeTorre - pisoY;

    void Start()
    {
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

        cam.transform.position = new Vector3(centroTorreX + desplazamientoCamaraX, pisoY + camaraSobrePiso, cam.transform.position.z);

        raizTorre = new GameObject("Torre").transform;
        Dibujo.Rectangulo(null, "Altura del jarrón", new Vector2(centroTorreX + 0.5f, pisoY + alturaJarron),
                          new Vector2(rangoApuntado * 2f + 2f, 0.06f), colorJarron, -5);
        guia = Dibujo.Rectangulo(null, "Guía de caída", Vector2.zero, Vector2.one, colorGuia, -5);
        guia.enabled = false;

        mesa.Iniciar(cam, this);
        estado = Estado.Armado;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
        {
            Reiniciar();
            return;
        }

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
        Keyboard kb = Keyboard.current;

        float x = XApuntada();
        grupoActual.Apuntar(x, topeTorre + alturaLanzamiento);
        // La guía cae desde el centro de masa: muestra dónde va a cargar el peso sobre la torre
        guia.transform.position = new Vector3(grupoActual.CentroDeMasa.x, topeTorre + alturaLanzamiento / 2f, 0f);
        guia.transform.localScale = new Vector3(0.05f, alturaLanzamiento, 1f);

        bool cancelar = (mouse != null && mouse.rightButton.wasPressedThisFrame) ||
                        (kb != null && kb.escapeKey.wasPressedThisFrame);
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

        if (g.SeCayo)
        {
            Terminar(false, "¡Un grupo se cayó de la torre!");
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
            Terminar(false, "La torre quedó demasiado inestable");
            return;
        }
        if (Altura >= alturaJarron)
        {
            Terminar(true, "¡Llegaste al jarrón de galletitas!");
            return;
        }

        mesa.Reponer();
        if (!mesa.QuedanObjetosSuficientes)
        {
            Terminar(false, "Te quedaste sin objetos");
            return;
        }
        estado = Estado.Armado;
    }

    void Terminar(bool gano, string motivo)
    {
        this.gano = gano;
        motivoFin = motivo;
        estado = Estado.Fin;
    }

    void MoverCamara()
    {
        float objetivo = Mathf.Max(pisoY + camaraSobrePiso, topeTorre + camaraSobreTope);
        Vector3 p = cam.transform.position;
        p.y = Mathf.Lerp(p.y, objetivo, 1f - Mathf.Exp(-suavizadoCamara * Time.deltaTime));
        cam.transform.position = p;
    }

    void Reiniciar()
    {
        Scene escena = SceneManager.GetActiveScene();
        if (escena.buildIndex >= 0)
            SceneManager.LoadScene(escena.buildIndex);
#if UNITY_EDITOR
        else
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(escena.path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
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

        rectPanel = new Rect(10, 10, 360, 92);
        GUI.Box(rectPanel, GUIContent.none);
        GUI.Label(new Rect(20, 14, 110, 24), "Estabilidad", estiloTexto);
        DibujarBarra(new Rect(130, 18, 230, 16), estabilidad / estabilidadInicial);
        GUI.Label(new Rect(20, 40, 340, 24), $"Altura: {Altura:0.0} / {alturaJarron:0.0}", estiloTexto);
        GUI.Label(new Rect(20, 66, 340, 24),
                  $"Grupo: {mesa.CantidadEnGrupo}/{mesa.MaxPorGrupo} objetos  ·  inestabilidad {mesa.InestabilidadGrupo:0.#}", estiloTexto);

        botonVisible = estado == Estado.Armado;
        rectBoton = new Rect(380, 10, 190, 40);
        if (botonVisible)
        {
            GUI.enabled = mesa.GrupoListo;
            if (GUI.Button(rectBoton, "Soltar grupo [Espacio]", estiloBoton)) Lanzar();
            GUI.enabled = true;
        }

        Vector3 jarron = cam.WorldToScreenPoint(new Vector3(centroTorreX + rangoApuntado + 1.5f, pisoY + alturaJarron, 0f));
        GUI.Label(new Rect(jarron.x / escalaGui - 220, (Screen.height - jarron.y) / escalaGui - 26, 220, 24),
                  "Jarrón de galletitas", estiloJarron);

        Rect tira = new Rect(0, 720 - 34, ancho, 34);
        GUI.Box(tira, GUIContent.none);
        GUI.Label(tira, Instrucciones(), estiloCentrado);

        if (Time.time < mensajeHasta)
            GUI.Label(new Rect(0, 720 - 72, ancho, 32), mensaje, estiloMensaje);

        if (estado == Estado.Fin)
        {
            Rect r = new Rect(ancho / 2f - 230, 720 / 2f - 85, 460, 170);
            GUI.Box(r, GUIContent.none);
            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x, r.y + 20, r.width, 44), gano ? "¡Ganaste!" : "Perdiste", estiloTitulo);
            GUI.Label(new Rect(r.x, r.y + 75, r.width, 30), motivoFin, estiloCentrado);
            GUI.Label(new Rect(r.x, r.y + 115, r.width, 30), "Apretá R para reiniciar", estiloCentrado);
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
                return "Mové el mouse para elegir dónde cae  ·  Click: soltar  ·  Click derecho o Esc: volver a armar";
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
