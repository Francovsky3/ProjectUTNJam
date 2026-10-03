using UnityEditor;
using UnityEngine;

// Aplica el 9-slice automáticamente a todo sprite cuyo nombre empiece con "boton_"
public class BordesBotones : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string nombre = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;

if (nombre.StartsWith("boton_"))       imp.spriteBorder = new Vector4(44, 52, 44, 48);
else if (nombre.StartsWith("panel_"))  imp.spriteBorder = new Vector4(60, 76, 60, 60);
else if (nombre.StartsWith("titulo_")) imp.spriteBorder = new Vector4(100, 0, 100, 0);
else return;
    }
}