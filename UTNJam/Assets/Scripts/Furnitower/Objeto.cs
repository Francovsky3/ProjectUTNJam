using System.Collections.Generic;
using UnityEngine;

// Objeto de la casa que se puede pegar con otros para armar un grupo.
// Va en cada prefab; si falta, la mesa lo agrega sola al crear el objeto.
public class Objeto : MonoBehaviour
{
    [SerializeField] float inestabilidad = 3f;
    [Tooltip("Peso del objeto: define hacia dónde se vuelca el grupo. 0 = usar la masa del Rigidbody del prefab")]
    [SerializeField] float masa = 0f;

    public float Inestabilidad => inestabilidad;
    public float Masa => masa;
    public int Lugar { get; set; } = -1;

    Collider[] colliders;
    SpriteRenderer[] sprites;
    int[] ordenes;

    void Awake()
    {
        // En la mesa los objetos no tienen física propia: el Rigidbody lo pone el grupo cuando cae
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (masa <= 0) masa = rb.mass;
            rb.isKinematic = true;
            Destroy(rb);
        }
        if (masa <= 0) masa = 0.5f;

        // Componentes del sistema anterior que en el prototipo no se usan
        foreach (Apilable a in GetComponents<Apilable>()) Destroy(a);
        foreach (Drag d in GetComponents<Drag>()) Destroy(d);

        colliders = GetComponentsInChildren<Collider>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        ordenes = new int[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            ordenes[i] = sprites[i].sortingOrder;
    }

    public void Teñir(Color color)
    {
        foreach (SpriteRenderer s in sprites)
            s.color = color;
    }

    public void AlFrente(bool alFrente)
    {
        for (int i = 0; i < sprites.Length; i++)
            sprites[i].sortingOrder = ordenes[i] + (alFrente ? 10 : 0);
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
}
