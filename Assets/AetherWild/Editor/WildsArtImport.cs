using UnityEditor;

namespace AetherWild.Editor
{
    public sealed class WildsArtImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(assetPath!="Assets/AetherWild/Art/WildsDepth1.jpeg") return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=2048;
            importer.isReadable=false;
            importer.ClearPlatformTextureSettings("WebGL");
        }
    }
}
