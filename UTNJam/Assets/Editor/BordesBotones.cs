using UnityEditor;
using UnityEngine;

// Aplica el 9-slice automáticamente a todo sprite cuyo nombre empiece con "boton_", "panel_", "titulo_" o "slider_..."
// Las demás imágenes no se tocan, así se pueden configurar a mano (por ejemplo, recortarlas en modo Multiple).
public class BordesBotones : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string nombre = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        Vector4 borde;
        if (nombre.StartsWith("boton_"))       borde = new Vector4(44, 52, 44, 48);
        else if (nombre.StartsWith("panel_"))  borde = new Vector4(60, 76, 60, 60);
        else if (nombre.StartsWith("titulo_")) borde = new Vector4(100, 0, 100, 0);
        else if (nombre.StartsWith("slider_fondo") || nombre.StartsWith("slider_relleno")) borde = new Vector4(50, 0, 50, 0);
        else return;   // no es una imagen de UI con bordes automáticos: se deja como esté

        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.spriteBorder = borde;
    }
}
