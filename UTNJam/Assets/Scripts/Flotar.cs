using UnityEngine;

// Hace que el objeto suba y baje suavemente alrededor de su posición inicial.
// Sirve para UI (mueve el RectTransform) y para objetos del mundo (mueve el Transform).
public class Flotar : MonoBehaviour
{
    [Tooltip("Cuánto sube y baja desde su posición inicial (píxeles del Canvas en UI, unidades en el mundo)")]
    [SerializeField] float amplitud = 15f;
    [Tooltip("Segundos que tarda en hacer un ciclo completo: subir y volver a bajar")]
    [SerializeField] float periodo = 3f;

    RectTransform rect;
    Vector2 posicionUI;
    Vector3 posicionMundo;

    void Awake()
    {
        rect = transform as RectTransform;
        if (rect != null) posicionUI = rect.anchoredPosition;
        else posicionMundo = transform.localPosition;
    }

    void Update()
    {
        // Tiempo sin escala: sigue flotando aunque el juego esté en pausa
        float fase = Time.unscaledTime * 2f * Mathf.PI / Mathf.Max(0.01f, periodo);
        float desplazamiento = Mathf.Sin(fase) * amplitud;

        if (rect != null) rect.anchoredPosition = posicionUI + Vector2.up * desplazamiento;
        else transform.localPosition = posicionMundo + Vector3.up * desplazamiento;
    }
}
