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

        [MenuItem("AetherWild/Configure M0 build")]
        public static void Configure()
        {
            PlayerSettings.companyName = "GhostFrame Studios";
            PlayerSettings.productName = "AetherWild";
            PlayerSettings.bundleVersion = "0.0.1";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            AssetDatabase.SaveAssets();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Configure();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene))
                throw new BuildFailedException("Missing M0 foundation scene.");
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
