using System.Collections.Generic;
using UnityEngine;

// Grupo de objetos pegados. Mientras se apunta sigue al mouse arriba de la torre;
// al soltarlo cae con un único Rigidbody, se asienta unos segundos y se congela.
public class GrupoQueCae : MonoBehaviour
{
    public enum Fase { Apuntando, Cayendo, Asentado }

    const float TiempoMaximoCayendo = 8f;

    public Fase Estado { get; private set; } = Fase.Apuntando;
    public List<Objeto> Objetos { get; private set; }
    public float Inestabilidad { get; private set; }

    // Resultado, válido cuando Estado == Asentado
    public bool SeCayo { get; private set; }
    public float Inclinacion { get; private set; }   // grados respecto de cómo se armó
    public float CentroX { get; private set; }       // X del centro de masa

    public Vector3 CentroDeMasa => transform.TransformPoint(centroDeMasaLocal);

    Rigidbody rb;
    float alturaSobreBorde;   // distancia del pivote al borde inferior del grupo
    Vector3 centroDeMasaLocal;

    Collider suelo;
    bool esElPrimero;
    float limiteInferior, tiempoMaxAsentarse, tiempoQuieto, umbralQuieto;

    bool tocoAlgo;
    float tiempoCayendo, tiempoDesdeContacto, tiempoSinMoverse;

    public void Preparar(List<Objeto> objetos)
    {
        Objetos = objetos;
        foreach (Objeto o in objetos)
        {
            o.transform.SetParent(transform, true);
            Inestabilidad += o.Inestabilidad;
        }
        Physics.SyncTransforms();
        alturaSobreBorde = transform.position.y - Limites().min.y;
        centroDeMasaLocal = transform.InverseTransformPoint(Objeto.CentroDeMasa(objetos));
    }

    public Bounds Limites()
    {
        Bounds b = new Bounds(transform.position, Vector3.zero);
        for (int i = 0; i < Objetos.Count; i++)
        {
            if (i == 0) b = Objetos[i].Limites();
            else b.Encapsulate(Objetos[i].Limites());
        }
        return b;
    }

    // Ubica el grupo con su borde inferior a la altura indicada
    public void Apuntar(float x, float bordeInferior)
    {
        transform.position = new Vector3(x, bordeInferior + alturaSobreBorde, 0f);
    }

    public void Soltar(Collider suelo, bool esElPrimero, float limiteInferior,
                       float tiempoMaxAsentarse, float tiempoQuieto, float umbralQuieto)
    {
        this.suelo = suelo;
        this.esElPrimero = esElPrimero;
        this.limiteInferior = limiteInferior;
        this.tiempoMaxAsentarse = tiempoMaxAsentarse;
        this.tiempoQuieto = tiempoQuieto;
        this.umbralQuieto = umbralQuieto;

        float masa = 0f;
        foreach (Objeto o in Objetos) masa += o.Masa;

        Physics.SyncTransforms();
        // Si ya tiene un Rigidbody se reutiliza: AddComponent devuelve null cuando ya hay uno
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"GrupoQueCae: no se pudo agregar física a '{name}'; el grupo se da por asentado.");
            Estado = Fase.Asentado;
            return;
        }
        rb.isKinematic = false;
        rb.mass = Mathf.Max(0.1f, masa);
        rb.constraints = RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Por defecto Unity calcula el centro de masa según el tamaño de los colliders;
        // acá se usa el peso de cada objeto, así un objeto pesado en una punta vuelca el grupo para ese lado
        rb.centerOfMass = centroDeMasaLocal;

        Estado = Fase.Cayendo;

        AudioManager.ReproducirSFX(a => a.release);
    }

    void FixedUpdate()
    {
        if (Estado != Fase.Cayendo) return;

        // Sin física no puede asentarse: se da por terminado en vez de quedar cayendo para siempre
        if (rb == null)
        {
            Estado = Fase.Asentado;
            return;
        }

        float dt = Time.fixedDeltaTime;
        tiempoCayendo += dt;

        if (rb.worldCenterOfMass.y < limiteInferior)
        {
            SeCayo = true;
            Congelar();
            return;
        }

        if (tocoAlgo)
        {
            tiempoDesdeContacto += dt;
            bool quieto = rb.linearVelocity.magnitude < umbralQuieto &&
                          rb.angularVelocity.magnitude < umbralQuieto * 2f;
            tiempoSinMoverse = quieto ? tiempoSinMoverse + dt : 0f;
        }

        if (tiempoSinMoverse >= tiempoQuieto ||
            tiempoDesdeContacto >= tiempoMaxAsentarse ||
            tiempoCayendo >= TiempoMaximoCayendo)
            Congelar();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Estado != Fase.Cayendo) return;
        tocoAlgo = true;

        AudioManager.ReproducirSFX(a => a.fall);

        // Tocar el piso solo está permitido para el primer grupo
        if (!esElPrimero && (collision.collider == suelo || collision.collider.CompareTag("Ground")))
            SeCayo = true;
    }

    void Congelar()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Inclinacion = Mathf.DeltaAngle(0f, rb.rotation.eulerAngles.z);
        CentroX = rb.worldCenterOfMass.x;
        Estado = Fase.Asentado;
    }
}
