using UnityEngine;
using UnityEngine.EventSystems;

public class BotonHundible : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform texto;   // arrastrá acá el texto hijo del botón
    public float bajar = 8f;      // lo mismo que baja el sprite (en px del sprite)
    Vector2 posOriginal;

    void Awake()
    {
        // Si no se asignó, usa el primer texto hijo del botón
        if (texto == null)
        {
            TMPro.TMP_Text hijo = GetComponentInChildren<TMPro.TMP_Text>(true);
            if (hijo != null) texto = hijo.rectTransform;
        }
        if (texto != null) posOriginal = texto.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData e) { if (texto != null) texto.anchoredPosition = posOriginal + Vector2.down * bajar; }
    public void OnPointerUp(PointerEventData e) { if (texto != null) texto.anchoredPosition = posOriginal; }
}