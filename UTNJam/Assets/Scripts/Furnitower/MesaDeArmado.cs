using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Mesa donde el jugador arrastra, rota y pega objetos para armar un grupo.
// Está en su propia zona del mundo; la cámara viene acá para armar y va a la torre para soltar.
public class MesaDeArmado : MonoBehaviour
{
    [Header("Objetos")]
    [Tooltip("Todos los objetos posibles. Cada uno define en su componente Objeto desde qué grupo aparece.")]
    [SerializeField] List<GameObject> prefabs;
    [SerializeField] float escalaObjetos = 1.5f;
    [SerializeField] int lugaresEnBandeja = 5;
    [Tooltip("Rellenar la bandeja después de cada grupo. Si está apagado, se usa un pool fijo de 'Objetos Totales'.")]
    [SerializeField] bool reponerBandeja = true;
    [SerializeField] int objetosTotales = 8;

    [Header("Grupo")]
    [SerializeField] int minPorGrupo = 2;
    [SerializeField] int maxPorGrupo = 3;
    [SerializeField] float margenContacto = 0.08f;
    [SerializeField] float pasoRotacion = 15f;
    [Tooltip("Distancia máxima por paso al arrastrar: más chico = choques más precisos entre objetos")]
    [SerializeField] float pasoArrastre = 0.05f;

    [Header("Fondo")]
    [Tooltip("Imagen de fondo de la pantalla de armado; se escala para ocupar todo el alto de la cámara")]
    [SerializeField] Sprite fondo;

    [Header("Ubicación (relativa al punto de armado, con la cámara de tamaño 5)")]
    [SerializeField] Vector2 centroZona = new Vector2(0f, -1.75f);
    [SerializeField] Vector2 tamañoZona = new Vector2(8f, 5.2f);
    [Tooltip("Altura de la cinta: los objetos de la bandeja se apoyan con su base en esta Y")]
    [SerializeField] float alturaBandeja = 3.1f;
    [SerializeField] float centroBandejaX = 0.7f;
    [SerializeField] float separacionLugares = 2.6f;

    [Header("Colores")]
    [SerializeField] Color colorZona = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] Color colorGrupo = new Color(0.8f, 1f, 0.8f);
    [SerializeField] Color colorCentroDeMasa = new Color(0.9f, 0.15f, 0.15f, 0.9f);

    JuegoFurnitower juego;
    Camera cam;
    Transform raiz;
    SpriteRenderer marcaCentro, plomada;
    readonly List<Objeto> paraCentro = new List<Objeto>();
    Objeto[] ocupantes;
    int creados;

    readonly List<Objeto> enMesa = new List<Objeto>();
    readonly List<Objeto> grupo = new List<Objeto>();
    readonly Dictionary<Objeto, Pose> posesArmado = new Dictionary<Objeto, Pose>();

    Objeto agarrado;
    Vector3 offsetAgarre;
    Vector3 ultimaPosValida;
    bool arrastreValido;   // false mientras el objeto agarrado arranca encimado con otro

    public int MinPorGrupo => minPorGrupo;
    public int MaxPorGrupo => maxPorGrupo;
    public int CantidadEnGrupo => grupo.Count;
    public bool GrupoListo => grupo.Count >= minPorGrupo && agarrado == null;
    public bool Arrastrando => agarrado != null;
    public bool QuedanObjetosSuficientes => enMesa.Count >= minPorGrupo;

    public float InestabilidadGrupo
    {
        get
        {
            float total = 0;
            foreach (Objeto o in grupo) total += o.Inestabilidad;
            return total;
        }
    }

    public void Iniciar(Vector2 centro, Camera camara, JuegoFurnitower juego)
    {
        cam = camara;
        this.juego = juego;

        raiz = new GameObject("Mesa").transform;
        raiz.position = new Vector3(centro.x, centro.y, 0f);

        if (fondo != null)
        {
            SpriteRenderer sr = new GameObject("Fondo armado").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(raiz, false);
            sr.sprite = fondo;
            sr.sortingOrder = -50;
            float escala = cam.orthographicSize * 2f / fondo.bounds.size.y;
            sr.transform.localScale = new Vector3(escala, escala, 1f);
            sr.transform.localPosition = -fondo.bounds.center * escala;
        }
        Dibujo.Rectangulo(raiz, "Zona de armado", centroZona, tamañoZona, colorZona, -19);

        // Rombo en el centro de masa del grupo y una plomada hacia abajo,
        // para ver hacia qué lado va a cargar el peso
        marcaCentro = Dibujo.Rectangulo(raiz, "Centro de masa", Vector2.zero, new Vector2(0.18f, 0.18f), colorCentroDeMasa, 15);
        marcaCentro.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Color colorPlomada = colorCentroDeMasa;
        colorPlomada.a *= 0.6f;
        plomada = Dibujo.Rectangulo(raiz, "Plomada", Vector2.zero, Vector2.one, colorPlomada, 14);
        MostrarCentroDeMasa();

        ocupantes = new Objeto[lugaresEnBandeja];
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogError("MesaDeArmado: falta cargar los prefabs de objetos en la lista.");
            return;
        }
        Reponer(0);
    }

    // Llena los lugares vacíos de la bandeja con objetos disponibles según el avance de la torre.
    // Devuelve true si empezó a aparecer algún objeto nuevo.
    public bool Reponer(int gruposColocados)
    {
        if (prefabs == null || prefabs.Count == 0) return false;

        List<GameObject> disponibles = new List<GameObject>();
        bool hayNuevos = false;
        foreach (GameObject p in prefabs)
        {
            if (p == null) continue;   // entrada vacía o prefab borrado
            Objeto datos = p.GetComponent<Objeto>();
            if (datos != null && !datos.PuedeAparecer(gruposColocados)) continue;
            disponibles.Add(p);
            if (datos != null && gruposColocados > 0 && datos.ApareceDesdeGrupo == gruposColocados)
                hayNuevos = true;
        }
        if (disponibles.Count == 0)
        {
            Debug.LogWarning($"MesaDeArmado: ningún objeto puede aparecer con {gruposColocados} grupos; uso todos.");
            disponibles.AddRange(prefabs.FindAll(p => p != null));
            if (disponibles.Count == 0) return false;
        }

        for (int i = 0; i < ocupantes.Length; i++)
        {
            if (ocupantes[i] != null) continue;
            if (!reponerBandeja && creados >= objetosTotales) break;
            ocupantes[i] = Crear(i, disponibles[Random.Range(0, disponibles.Count)]);
        }
        return hayNuevos;
    }

    Objeto Crear(int lugar, GameObject prefab)
    {
        GameObject go = Instantiate(prefab, raiz);
        go.transform.localScale = prefab.transform.localScale * escalaObjetos;

        Objeto o = go.GetComponent<Objeto>();
        if (o == null) o = go.AddComponent<Objeto>();
        o.Lugar = lugar;

        creados++;
        enMesa.Add(o);
        VolverALugar(o);
        return o;
    }

    // Lo llama el juego cada frame mientras se está armando
    public void Actualizar()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        Vector3 puntero = juego.MouseEnMundo();

        if (agarrado == null)
        {
            if (!juego.MouseSobreHud())
            {
                if (mouse.leftButton.wasPressedThisFrame) Agarrar(puntero);
                else if (mouse.rightButton.wasPressedThisFrame) DevolverClickeado();
            }
            MostrarCentroDeMasa();
            return;
        }

        // Rotación mientras se arrastra: ruedita o Q/E
        float giro = 0f;
        float rueda = mouse.scroll.ReadValue().y;
        if (rueda > 0.01f) giro += pasoRotacion;
        else if (rueda < -0.01f) giro -= pasoRotacion;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.qKey.wasPressedThisFrame) giro += pasoRotacion;
            if (kb.eKey.wasPressedThisFrame) giro -= pasoRotacion;
        }
        if (giro != 0f) Rotar(giro);
        MoverAgarrado(puntero + offsetAgarre);

        if (mouse.leftButton.wasReleasedThisFrame) Soltar();
        MostrarCentroDeMasa();
    }

    // Muestra el centro de masa del grupo; mientras se arrastra un objeto dentro de la zona,
    // lo incluye como vista previa de dónde quedaría el peso
    void MostrarCentroDeMasa()
    {
        paraCentro.Clear();
        paraCentro.AddRange(grupo);
        if (agarrado != null && grupo.Count > 0 && DentroDeZona(agarrado.transform.position))
            paraCentro.Add(agarrado);

        bool visible = paraCentro.Count > 0;
        marcaCentro.enabled = visible;
        plomada.enabled = visible;
        if (!visible) return;

        Physics.SyncTransforms();
        Vector3 centro = raiz.InverseTransformPoint(Objeto.CentroDeMasa(paraCentro));
        float piso = centroZona.y - tamañoZona.y / 2f;
        marcaCentro.transform.localPosition = new Vector3(centro.x, centro.y, 0f);
        plomada.transform.localPosition = new Vector3(centro.x, (centro.y + piso) / 2f, 0f);
        plomada.transform.localScale = new Vector3(0.03f, Mathf.Max(0.01f, centro.y - piso), 1f);
    }

    Objeto ObjetoBajoElMouse()
    {
        Physics.SyncTransforms();
        Ray rayo = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        foreach (RaycastHit hit in Physics.RaycastAll(rayo))
        {
            Objeto o = hit.collider.GetComponentInParent<Objeto>();
            if (o != null && enMesa.Contains(o)) return o;
        }
        return null;
    }

    void Agarrar(Vector3 puntero)
    {
        Objeto o = ObjetoBajoElMouse();
        if (o == null) return;

        agarrado = o;
        offsetAgarre = o.transform.position - puntero;
        offsetAgarre.z = 0f;
        grupo.Remove(o);
        o.Teñir(Color.white);
        o.AlFrente(true);

        arrastreValido = !SuperponeAlGrupo();
        ultimaPosValida = o.transform.position;
    }

    // ---------- Choques entre el objeto arrastrado y los del grupo ----------
    // Los objetos de la mesa no tienen Rigidbody, así que el choque se resuelve a mano:
    // se avanza en pasos chicos y, si el objeto se mete en otro, se lo empuja hacia afuera.

    void MoverAgarrado(Vector3 objetivo)
    {
        objetivo.z = 0f;

        // Si arrancó encimado (no debería pasar), se mueve libre hasta quedar en un lugar válido
        if (!arrastreValido)
        {
            agarrado.transform.position = objetivo;
            arrastreValido = !SuperponeAlGrupo();
            ultimaPosValida = agarrado.transform.position;
            return;
        }

        Vector3 delta = objetivo - agarrado.transform.position;
        int pasos = Mathf.Clamp(Mathf.CeilToInt(delta.magnitude / pasoArrastre), 1, 80);
        Vector3 paso = delta / pasos;
        for (int i = 0; i < pasos; i++)
        {
            agarrado.transform.position += paso;
            if (!SepararDelGrupo())
            {
                agarrado.transform.position = ultimaPosValida;
                break;
            }
            ultimaPosValida = agarrado.transform.position;
        }
    }

    void Rotar(float grados)
    {
        Quaternion q = Quaternion.Euler(0f, 0f, grados);
        Quaternion rotacionAntes = agarrado.transform.rotation;
        agarrado.transform.rotation = q * rotacionAntes;

        // Si al girar se mete en otro objeto y no hay forma de sacarlo, no gira
        if (arrastreValido && !SepararDelGrupo())
        {
            agarrado.transform.rotation = rotacionAntes;
            agarrado.transform.position = ultimaPosValida;
            return;
        }
        offsetAgarre = q * offsetAgarre;
        ultimaPosValida = agarrado.transform.position;
    }

    // Empuja al objeto agarrado fuera de los objetos del grupo. Devuelve false si no pudo.
    bool SepararDelGrupo()
    {
        for (int intento = 0; intento < 8; intento++)
        {
            bool choco = false;
            foreach (Collider a in agarrado.Colliders)
            {
                foreach (Objeto otro in grupo)
                {
                    foreach (Collider b in otro.Colliders)
                    {
                        if (!Penetracion(a, b, out Vector3 direccion, out float distancia)) continue;

                        // Si la salida más corta es en profundidad (Z), en 2D no tiene solución
                        if (Mathf.Abs(direccion.z) > 0.7f) return false;
                        direccion.z = 0f;
                        agarrado.transform.position += direccion.normalized * (distancia + 0.002f);
                        choco = true;
                    }
                }
            }
            if (!choco) return true;
        }
        return false;
    }

    bool SuperponeAlGrupo()
    {
        foreach (Collider a in agarrado.Colliders)
            foreach (Objeto otro in grupo)
                foreach (Collider b in otro.Colliders)
                    if (Penetracion(a, b, out _, out float distancia) && distancia > 0.01f)
                        return true;
        return false;
    }

    static bool Penetracion(Collider a, Collider b, out Vector3 direccion, out float distancia)
    {
        Transform ta = a.transform, tb = b.transform;
        return Physics.ComputePenetration(a, ta.position, ta.rotation, b, tb.position, tb.rotation,
                                          out direccion, out distancia);
    }

    void Soltar()
    {
        Objeto o = agarrado;
        agarrado = null;
        o.AlFrente(false);

        if (!arrastreValido && DentroDeZona(o.transform.position))
        {
            VolverALugar(o);
            juego.Avisar("No puede quedar adentro de otro objeto");
            Reagrupar(null);
            return;
        }

        if (!DentroDeZona(o.transform.position))
        {
            VolverALugar(o);
            Reagrupar(null);
            return;
        }
        if (grupo.Count >= maxPorGrupo)
        {
            VolverALugar(o);
            juego.Avisar($"Máximo {maxPorGrupo} objetos por grupo");
            Reagrupar(null);
            return;
        }

        grupo.Add(o);
        Reagrupar(o);
        if (!grupo.Contains(o))
            juego.Avisar("Tiene que tocar al grupo");
    }

    // Click derecho: el objeto vuelve a la bandeja
    void DevolverClickeado()
    {
        Objeto o = ObjetoBajoElMouse();
        if (o == null || !grupo.Contains(o)) return;
        grupo.Remove(o);
        VolverALugar(o);
        Reagrupar(null);
    }

    // Deja en el grupo solo la parte conectada más grande; lo que quedó suelto vuelve a la bandeja.
    // Ante un empate gana la parte que no tiene al objeto recién soltado.
    void Reagrupar(Objeto soltado)
    {
        Physics.SyncTransforms();

        List<Objeto> mejor = null;
        List<Objeto> pendientes = new List<Objeto>(grupo);
        while (pendientes.Count > 0)
        {
            List<Objeto> parte = new List<Objeto> { pendientes[0] };
            pendientes.RemoveAt(0);
            for (int i = 0; i < parte.Count; i++)
            {
                for (int j = pendientes.Count - 1; j >= 0; j--)
                {
                    if (parte[i].TocaA(pendientes[j], margenContacto))
                    {
                        parte.Add(pendientes[j]);
                        pendientes.RemoveAt(j);
                    }
                }
            }

            if (mejor == null || parte.Count > mejor.Count ||
                (parte.Count == mejor.Count && mejor.Contains(soltado)))
                mejor = parte;
        }

        for (int i = grupo.Count - 1; i >= 0; i--)
        {
            Objeto o = grupo[i];
            if (mejor != null && mejor.Contains(o)) continue;
            grupo.RemoveAt(i);
            VolverALugar(o);
        }
        foreach (Objeto o in grupo)
            o.Teñir(colorGrupo);
    }

    bool DentroDeZona(Vector3 posicionMundo)
    {
        Vector3 local = raiz.InverseTransformPoint(posicionMundo);
        return Mathf.Abs(local.x - centroZona.x) <= tamañoZona.x / 2f &&
               Mathf.Abs(local.y - centroZona.y) <= tamañoZona.y / 2f;
    }

    Vector3 PosicionLugar(int lugar)
    {
        float x = centroBandejaX + (lugar - (lugaresEnBandeja - 1) / 2f) * separacionLugares;
        return new Vector3(x, alturaBandeja, 0f);
    }

    // Pone el objeto en su lugar de la cinta: centrado en X y apoyado con su base en la cinta
    void VolverALugar(Objeto o)
    {
        o.transform.SetParent(raiz, false);
        o.transform.localPosition = PosicionLugar(o.Lugar);
        o.transform.localRotation = o.RotacionInicial;

        Vector3 destino = raiz.TransformPoint(PosicionLugar(o.Lugar));
        Bounds visual = o.LimitesVisuales();
        o.transform.position += new Vector3(destino.x - visual.center.x, destino.y - visual.min.y, 0f);
        o.Teñir(Color.white);
    }

    // Saca el grupo de la mesa y lo junta bajo un GrupoQueCae
    public GrupoQueCae ArmarGrupoParaLanzar()
    {
        if (!GrupoListo) return null;

        Vector3 centro = Vector3.zero;
        posesArmado.Clear();
        foreach (Objeto o in grupo)
        {
            centro += o.transform.position;
            posesArmado[o] = new Pose(o.transform.localPosition, o.transform.localRotation);
        }
        centro /= grupo.Count;
        centro.z = 0f;

        GameObject raizGrupo = new GameObject("Grupo");
        raizGrupo.transform.position = centro;
        GrupoQueCae g = raizGrupo.AddComponent<GrupoQueCae>();

        List<Objeto> objetos = new List<Objeto>(grupo);
        foreach (Objeto o in objetos)
        {
            o.DesbloquearEnColeccion();
            o.Teñir(Color.white);
            enMesa.Remove(o);
            ocupantes[o.Lugar] = null;

        }
        grupo.Clear();
        MostrarCentroDeMasa();

        g.Preparar(objetos);
        return g;
    }

    // Se canceló el lanzamiento: el grupo vuelve a la mesa tal como estaba armado
    public void DevolverGrupo(GrupoQueCae g)
    {
        foreach (Objeto o in g.Objetos)
        {
            o.transform.SetParent(raiz, false);
            if (posesArmado.TryGetValue(o, out Pose pose))
            {
                o.transform.localPosition = pose.position;
                o.transform.localRotation = pose.rotation;
            }
            enMesa.Add(o);
            grupo.Add(o);
            ocupantes[o.Lugar] = o;
            o.Teñir(colorGrupo);
        }
        Destroy(g.gameObject);
        MostrarCentroDeMasa();
    }
}
