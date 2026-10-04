using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StickerSlot : MonoBehaviour
{
    [SerializeField] private Image imagenSticker;
    [SerializeField] private GameObject bloqueado;
    [SerializeField] private TMP_Text nombre;

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

        if (ColeccionManager.Instance == null)
        {
            Debug.LogError("❌ StickerSlot: no existe un ColeccionManager en la escena.", this);
            return;
        }

        imagenSticker.sprite = objeto.Sticker;
        nombre.text = objeto.NombreColeccion;

        bool desbloqueado =
            ColeccionManager.Instance.EstaDesbloqueado(objeto.IdColeccion);

        imagenSticker.gameObject.SetActive(desbloqueado);
        bloqueado.SetActive(!desbloqueado);

        if (!desbloqueado)
        {
            nombre.text = "???";
        }
    }
}