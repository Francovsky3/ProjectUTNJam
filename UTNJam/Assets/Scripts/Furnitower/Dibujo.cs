using UnityEngine;

// Rectángulos de color hechos en runtime, para los fondos y las guías del prototipo
// sin necesitar sprites nuevos.
public static class Dibujo
{
    static Sprite blanco;

    static Sprite Blanco
    {
        get
        {
            if (blanco == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                blanco = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            return blanco;
        }
    }

    public static SpriteRenderer Rectangulo(Transform padre, string nombre, Vector2 centro, Vector2 tamaño, Color color, int orden)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        go.transform.localScale = new Vector3(tamaño.x, tamaño.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Blanco;
        sr.color = color;
        sr.sortingOrder = orden;
        return sr;
    }
}
