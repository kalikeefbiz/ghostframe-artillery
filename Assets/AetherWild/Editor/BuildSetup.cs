using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AetherWild.Editor
{
    public sealed class BuildSetup : IPreprocessBuildWithReport
    {
        private const string Scene = "Assets/AetherWild/Scenes/Foundation.unity";
        public int callbackOrder => -1000;

        [MenuItem("AetherWild/Configure build")]
        public static void Configure()
        {
            PlayerSettings.companyName = "GhostFrame Studios";
            PlayerSettings.productName = "AetherWild";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            // Diagnostic compatibility build: avoid compressed-response/header dependencies.
            // Production sprites import as uncompressed RGBA; no DXT/ASTC dependency.
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.Generic;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            AssetDatabase.SaveAssets();
            CombatChecks.Run();
            SliceChecks.Run();
            ProductionArtChecks.Run();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Configure();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene))
                throw new BuildFailedException("Missing foundation scene.");
        }

        // Optional Build Automation pre-export hook; no Cloud-only types in gameplay.
        public static void PreExport() => Configure();

        public static void BuildWebGL()
        {
            Configure();
            string output = Environment.GetEnvironmentVariable("AETHERWILD_BUILD_PATH") ?? "Builds/WebGL";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene }, locationPathName = output,
                target = BuildTarget.WebGL, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("AetherWild WebGL build failed.");
        }
    }
}
