using System;
using UnityEditor;
using UnityEngine;

namespace AetherWild.Editor
{
    public sealed class ProductionArtImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/AetherWild/Art/",StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase)) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.isReadable=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.crunchedCompression=false;
            importer.maxTextureSize=4096;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.ClearPlatformTextureSettings("WebGL");
            importer.ClearPlatformTextureSettings("iPhone");
            importer.ClearPlatformTextureSettings("Android");
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape=false;
            settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=new Vector2(.5f,.5f);
            settings.spritePixelsPerUnit=100;
            settings.spriteBorder=Vector4.zero;
            // Presentation registration only. Full PNG rectangles and all source pixels remain intact.
            if(assetPath.Contains("/Summoners/Mae/"))
            {
                settings.spritePixelsPerUnit=820;
                if(assetPath.EndsWith("Mae_Idle.PNG")) settings.spritePivot=new Vector2(550f/1086,41f/1448);
                else if(assetPath.EndsWith("Mae_Walk_A.PNG")) settings.spritePivot=new Vector2(550f/1086,82f/1448);
                else if(assetPath.EndsWith("Mae_Walk_B.png"))
                {settings.spritePixelsPerUnit=940;settings.spritePivot=new Vector2(585f/1170,527f/2532);}
                else if(assetPath.EndsWith("Mae_Cast.PNG")) settings.spritePivot=new Vector2(480f/1086,59f/1448);
                else if(assetPath.EndsWith("Mae_Hit.PNG")) settings.spritePivot=new Vector2(550f/1086,34f/1448);
                else if(assetPath.EndsWith("Mae_Defeat.PNG")) settings.spritePivot=new Vector2(550f/1086,240f/1448);
            }
            else if(assetPath.Contains("/UI/"))
                settings.spriteBorder=assetPath.Contains("sigilslot")?new Vector4(260,260,260,260):new Vector4(520,210,520,210);
            importer.SetTextureSettings(settings);
        }
    }
}
