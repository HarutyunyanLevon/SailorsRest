using UnityEditor;
using UnityEngine;

namespace SailorsRest.EditorTools
{
    /// <summary>Import settings shared by every SpriteCook image: a single point-filtered, uncompressed sprite.</summary>
    public static class PixelArtImport
    {
        public const string Menu = "Sailor's Rest/";

        public static void ApplyTo(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
