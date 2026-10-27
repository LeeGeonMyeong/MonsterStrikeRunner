#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class RunnerWebBuild
{
    [MenuItem("Runner/Build Web for GitHub Pages")]
    public static void Build()
    {
        PlayerSettings.bundleVersion = "1.1.3";
        PlayerSettings.defaultWebScreenWidth = 480;
        PlayerSettings.defaultWebScreenHeight = 854;
        PlayerSettings.WebGL.template = "PROJECT:RunnerPortrait";
        // GitHub Pages cannot set Content-Encoding; the bundled decoder handles compressed files.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
        OptimizeTexture("Assets/Resources/Characters/RunnerHero.png", 256);
        OptimizeTexture("Assets/Resources/Characters/RunnerMonster.png", 256);
        OptimizeTexture("Assets/Resources/Backgrounds/ForestRoad.png", 1024);
        OptimizeTexture("Assets/Resources/Characters/RunnerHeroFrames.png", 1024);
        OptimizeTexture("Assets/Resources/Characters/RunnerMonsterFrames.png", 1024);
        MakeLoadingThumbnail();
        string output = Path.GetFullPath("docs");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Runner Web build failed: " + report.summary.result);
        File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
        UnityEngine.Debug.Log("RUNNER_WEB_BUILD_PASS: " + output);
    }
    private static void OptimizeTexture(string path, int size)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("WebGL");
        settings.overridden = true;
        settings.maxTextureSize = size;
        // RGBA32 works on both desktop and phone GPUs; Brotli reduces its download size.
        settings.format = TextureImporterFormat.RGBA32;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(settings);
        importer.SaveAndReimport();
    }
    private static void MakeLoadingThumbnail()
    {
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Characters/RunnerHero.png");
        int height = 160;
        int width = Mathf.Max(1, Mathf.RoundToInt(height * (float)source.width / source.height));
        RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        Texture2D thumbnail = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            thumbnail.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            thumbnail.Apply();
            File.WriteAllBytes("Assets/WebGLTemplates/RunnerPortrait/hero.png", thumbnail.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(thumbnail);
        }
        AssetDatabase.Refresh();
    }
}
#endif
