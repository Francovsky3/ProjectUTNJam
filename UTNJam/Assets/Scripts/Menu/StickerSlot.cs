using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StickerSlot : MonoBehaviour
{
    [SerializeField] private Image imagenSticker;
    [SerializeField] private GameObject bloqueado;
    [SerializeField] private TMP_Text nombre;
    [Tooltip("Tamaño del sticker dentro de su recuadro (1 = lo llena entero). 0.6 queda parecido al candado")]
    [Range(0.1f, 1f)]
    [SerializeField] private float tamañoSticker = 0.6f;

    public void Configurar(Objeto objeto)
    {
        if (objeto == null)
        {
            Debug.LogError("❌ StickerSlot: el objeto recibido es NULL.", this);
            return;
        }

        if (imagenSticker == null)
        {
            Debug.LogError("❌ StickerSlot: 'Imagen Sticker' no está asignado en el prefab StickerSlot.", this);
            return;
        }

        if (nombre == null)
        {
            Debug.LogError("❌ StickerSlot: 'Nombre' no está asignado en el prefab StickerSlot.", this);
            return;
        }

        if (bloqueado == null)
        {
            Debug.LogError("❌ StickerSlot: 'Bloqueado' no está asignado en el prefab StickerSlot.", this);
            return;
        }

        imagenSticker.sprite = objeto.Sticker;
        imagenSticker.enabled = true;           // en el prefab el componente Image viene apagado
        imagenSticker.preserveAspect = true;    // que el dibujo no se deforme dentro del cuadrado
        imagenSticker.rectTransform.localScale = Vector3.one * tamañoSticker;
        nombre.text = objeto.NombreColeccion;

        // No hace falta un ColeccionManager en la escena: el progreso se lee de PlayerPrefs
        bool desbloqueado = ColeccionManager.Desbloqueado(objeto.IdColeccion);

        imagenSticker.gameObject.SetActive(desbloqueado);
        bloqueado.SetActive(!desbloqueado);

        if (!desbloqueado)
        {
            nombre.text = "???";
        }
    }
}