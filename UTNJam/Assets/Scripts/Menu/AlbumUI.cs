using UnityEngine;

public class AlbumUI : MonoBehaviour
{
    [Header("Colección")]
    [SerializeField] private CatalogoColeccion catalogo;

    [Header("UI")]
    [SerializeField] private StickerSlot prefabSticker;
    [SerializeField] private Transform contenedor;

    private void Start()
    {
        ActualizarAlbum();
    }

    public void ActualizarAlbum()
    {
        foreach (Transform hijo in contenedor)
        {
            Destroy(hijo.gameObject);
        }

        foreach (Objeto objeto in catalogo.Objetos)
        {
            StickerSlot slot = Instantiate(prefabSticker, contenedor);
            slot.Configurar(objeto);
        }
    }
}