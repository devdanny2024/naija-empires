#if UNITY_EDITOR
using UnityEditor;

namespace NaijaEmpires
{
    /// Forces HUD icon PNGs under Resources/NE/Icons/ to import as sprites — no manual
    /// Inspector clicking. Single sprite, no compression, point filter so the small
    /// monochrome glyphs stay crisp and can be tinted to the brand palette in code.
    public class IconImportPostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("Resources/NE/Icons/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = UnityEngine.FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
#endif
