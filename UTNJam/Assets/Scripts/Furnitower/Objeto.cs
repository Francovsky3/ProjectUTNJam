using System.Collections.Generic;
using UnityEngine;

// Objeto de la casa que se puede pegar con otros para armar un grupo.
// Va en cada prefab; si falta, la mesa lo agrega sola al crear el objeto.
public class Objeto : MonoBehaviour
{
    [SerializeField] float inestabilidad = 3f;
    [Tooltip("Peso del objeto: define hacia dónde se vuelca el grupo. 0 = usar la masa del Rigidbody del prefab")]
    [SerializeField] float masa = 0f;

    [Header("Colección")]
    [SerializeField] private string idColeccion;
    [SerializeField] private Sprite sticker;
    [SerializeField] private string nombreColeccion;

    [Header("Aparición")]
    [Tooltip("Cantidad de grupos ya colocados en la torre a partir de la cual empieza a aparecer (0 = desde el principio)")]
    [SerializeField] int apareceDesdeGrupo = 0;
    [Tooltip("Deja de aparecer cuando la torre llega a esta cantidad de grupos (0 = nunca deja de aparecer)")]
    [SerializeField] int dejaDeAparecerEnGrupo = 0;

    public string IdColeccion => idColeccion;
    public Sprite Sticker => sticker;
    public string NombreColeccion => nombreColeccion;
    public float Inestabilidad => inestabilidad;
    public float Masa => masa;
    public int ApareceDesdeGrupo => apareceDesdeGrupo;

    public int Lugar { get; set; } = -1;
    public Collider[] Colliders => colliders;
    public Quaternion RotacionInicial { get; private set; }   // la del prefab (ej. el colchón viene acostado)

    public bool PuedeAparecer(int gruposColocados)
    {
        return gruposColocados >= apareceDesdeGrupo &&
               (dejaDeAparecerEnGrupo <= 0 || gruposColocados < dejaDeAparecerEnGrupo);
    }

    static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int IdColor = Shader.PropertyToID("_Color");

    Collider[] colliders;
    Renderer[] renderers;   // sprites o mallas (los objetos placeholder son primitivas 3D)
    int[] ordenes;
    Color[] coloresOriginales;
    MaterialPropertyBlock bloque;

    void Awake()
    {
        RotacionInicial = transform.localRotation;

        // En la mesa los objetos no tienen física propia: el Rigidbody lo pone el grupo cuando cae
        float masaPrefab = 0f;
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            masaPrefab += rb.mass;
            rb.isKinematic = true;
            Destroy(rb);
        }
        if (masa <= 0) masa = masaPrefab > 0 ? masaPrefab : 0.5f;

        // Componentes del sistema anterior que en el prototipo no se usan
        foreach (Apilable a in GetComponents<Apilable>()) Destroy(a);
        foreach (Drag d in GetComponents<Drag>()) Destroy(d);

        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        ordenes = new int[renderers.Length];
        coloresOriginales = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            ordenes[i] = renderers[i].sortingOrder;
            coloresOriginales[i] = ColorDe(renderers[i]);
        }
    }

    static Color ColorDe(Renderer r)
    {
        if (r is SpriteRenderer sr) return sr.color;
        Material m = r.sharedMaterial;
        if (m == null) return Color.white;
        if (m.HasProperty(IdBaseColor)) return m.GetColor(IdBaseColor);
        if (m.HasProperty(IdColor)) return m.GetColor(IdColor);
        return Color.white;
    }

    // Multiplica el color original por "color" (blanco = color original)
    public void Teñir(Color color)
    {
        if (bloque == null) bloque = new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            Color final = coloresOriginales[i] * color;
            if (renderers[i] is SpriteRenderer sr)
            {
                sr.color = final;
                continue;
            }
            renderers[i].GetPropertyBlock(bloque);
            bloque.SetColor(IdBaseColor, final);
            bloque.SetColor(IdColor, final);
            renderers[i].SetPropertyBlock(bloque);
        }
    }

    public void AlFrente(bool alFrente)
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = ordenes[i] + (alFrente ? 10 : 0);
    }

    public Bounds Limites()
    {
        Bounds b = new Bounds(transform.position, Vector3.zero);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (i == 0) b = colliders[i].bounds;
            else b.Encapsulate(colliders[i].bounds);
        }
        return b;
    }

    // Límites del dibujo (no de los colliders); se actualizan al instante al mover el transform
    public Bounds LimitesVisuales()
    {
        Bounds b = new Bounds(transform.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (i == 0) b = renderers[i].bounds;
            else b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    // Centro de masa (en mundo) de varios objetos, ponderado por el peso de cada uno.
    // Antes de llamarlo hay que hacer Physics.SyncTransforms() si se movieron transforms.
    public static Vector3 CentroDeMasa(IReadOnlyList<Objeto> objetos)
    {
        Vector3 suma = Vector3.zero;
        float masaTotal = 0f;
        foreach (Objeto o in objetos)
        {
            suma += o.Limites().center * o.Masa;
            masaTotal += o.Masa;
        }
        return masaTotal > 0f ? suma / masaTotal : Vector3.zero;
    }

    // ¿Algún collider de este objeto toca (o queda a menos de "margen") de alguno del otro?
    // Antes de llamarlo hay que hacer Physics.SyncTransforms() si se movieron transforms.
    public bool TocaA(Objeto otro, float margen)
    {
        foreach (Collider c in colliders)
        {
            if (c == null || !c.enabled) continue;
            foreach (Collider hit in Superpuestos(c, margen))
            {
                if (hit.GetComponentInParent<Objeto>() == otro)
                    return true;
            }
        }
        return false;
    }

    static Collider[] Superpuestos(Collider c, float margen)
    {
        Transform t = c.transform;
        Vector3 escala = t.lossyScale;
        escala = new Vector3(Mathf.Abs(escala.x), Mathf.Abs(escala.y), Mathf.Abs(escala.z));

        switch (c)
        {
            case BoxCollider caja:
                Vector3 mitad = Vector3.Scale(caja.size, escala) * 0.5f + Vector3.one * margen;
                return Physics.OverlapBox(t.TransformPoint(caja.center), mitad, t.rotation);

            case SphereCollider esfera:
                float radio = esfera.radius * Mathf.Max(escala.x, escala.y, escala.z) + margen;
                return Physics.OverlapSphere(t.TransformPoint(esfera.center), radio);

            case CapsuleCollider capsula:
                Vector3 eje = capsula.direction == 0 ? Vector3.right : capsula.direction == 1 ? Vector3.up : Vector3.forward;
                float escalaEje = capsula.direction == 0 ? escala.x : capsula.direction == 1 ? escala.y : escala.z;
                float escalaRadio = capsula.direction == 0 ? Mathf.Max(escala.y, escala.z)
                                  : capsula.direction == 1 ? Mathf.Max(escala.x, escala.z)
                                  : Mathf.Max(escala.x, escala.y);
                float r = capsula.radius * escalaRadio;
                float medio = Mathf.Max(0f, capsula.height * escalaEje * 0.5f - r);
                Vector3 centro = t.TransformPoint(capsula.center);
                Vector3 dir = t.rotation * eje;
                return Physics.OverlapCapsule(centro - dir * medio, centro + dir * medio, r + margen);

            default:
                Bounds b = c.bounds;
                return Physics.OverlapBox(b.center, b.extents + Vector3.one * margen);
        }
    }
    public bool DesbloquearEnColeccion()
    {
        if (ColeccionManager.Instance == null)
            return false;

        return ColeccionManager.Instance.DesbloquearObjeto(IdColeccion);
    }

}
