using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AetherWild.Editor
{
    public static class ProductionArtChecks
    {
        [MenuItem("AetherWild/Tests/Production art references")]
        public static void Run()
        {
            var art=AssetDatabase.LoadAssetAtPath<ProductionArt>("Assets/AetherWild/Data/ProductionArt.asset");
            Require(art,"Production art data missing");
            foreach(var sprite in new[]{art.maeIdle,art.maeWalkA,art.maeWalkB,art.maeCast,art.maeHit,art.maeDefeat,
                art.healthFrame,art.shieldFrame,art.hudPanel,art.primaryButton,art.secondaryButton,
                art.sigilSlot,art.sigilSelected,art.title}) CheckSprite(sprite);
            var mae=AssetDatabase.LoadAssetAtPath<SummonerDefinition>("Assets/AetherWild/Data/Mae.asset");
            foreach(var sigil in mae.startingLoadout) CheckSprite(sigil.icon);
            foreach(var name in new[]{"origin","aerth","ash","aurora","tempest","lunar","xo","seeker"})
                CheckSprite(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/AetherWild/Art/Schools/"+name+".png"));
            Require(Mathf.Abs(art.maeIdle.pixelsPerUnit-820)<.1f,"Mae registration mismatch");
            Require(Mathf.Abs(art.maeWalkB.pixelsPerUnit-940)<.1f,"Walk B registration mismatch");
            Debug.Log("M2.1 production sprite references/import settings passed. Device rendering still requires acceptance.");
        }
        private static void CheckSprite(Sprite sprite)
        {
            Require(sprite,"Missing production sprite reference");
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            Require(importer.textureType==TextureImporterType.Sprite,"Expected sprite texture");
            Require(importer.alphaSource==TextureImporterAlphaSource.FromInput && importer.alphaIsTransparency,
                "Expected source alpha");
            Require(importer.textureCompression==TextureImporterCompression.Uncompressed,"Compression must remain disabled");
            Require(importer.maxTextureSize>=4096,"Do not downsize the authoritative sprites");
        }
        private static void Require(bool value,string message)
        { if(!value) throw new BuildFailedException(message); }
    }
}
