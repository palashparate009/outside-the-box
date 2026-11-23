using UnityEditor;

namespace OutsideTheBox.Editor
{
    /// <summary>Keeps generated presentation art small and mobile-friendly on every import.</summary>
    internal sealed class GeneratedArtImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/OutsideTheBox/Resources/OutsideTheBox/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            var isFrame = assetPath.EndsWith("GlassFrame.png", System.StringComparison.Ordinal);
            var isCosmeticAtlas = assetPath.EndsWith("Cosmetics/CosmeticAtlas.png", System.StringComparison.Ordinal);
            var isUiSprite = assetPath.StartsWith(ArtRoot + "UI/", System.StringComparison.Ordinal);
            var maxSize = isCosmeticAtlas ? 2048 : isFrame ? 1024 : 512;
            // UI Images load these as Sprite assets. Forcing Default on every import
            // removed the star subasset and rendered ratings as plain rectangles.
            importer.textureType = isUiSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (isUiSprite) importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = maxSize;

            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = maxSize,
                format = isCosmeticAtlas ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6,
                compressionQuality = 100
            });
        }
    }
}
