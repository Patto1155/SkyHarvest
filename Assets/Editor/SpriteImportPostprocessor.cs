// Enforces pixel-art import settings for every texture under Assets/Resources/Sprites.
// The game builds Sprites at runtime via SpriteLoader (Sprite.Create over Texture2D),
// so frame-rect math depends on textures keeping their exact source dimensions:
// NPOT scaling, mipmaps, or compression silently corrupt tile strips.
using UnityEditor;
using UnityEngine;

public class SpriteImportPostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("Assets/Resources/Sprites/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}
