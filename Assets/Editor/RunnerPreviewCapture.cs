#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Optional visual check for the automated test; excluded from game builds.
public static class RunnerPreviewCapture
{
    public static void Save(string name)
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-runnerPreview") < 0) return;
        Camera camera = Camera.main;
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        RenderTexture output = new RenderTexture(480, 854, 24, RenderTextureFormat.ARGB32);
        output.Create();
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderMode previousMode = canvas.renderMode;
        Camera previousCamera = canvas.worldCamera;
        float previousDistance = canvas.planeDistance;
        float previousAspect = camera.aspect;
        int previousSortingOrder = canvas.sortingOrder;
        Texture2D pixels = null;
        try
        {
            camera.targetTexture = output;
            camera.aspect = 480f / 854f;
            UnityEngine.Object.FindFirstObjectByType<RunnerBackground>().RefreshLayout();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = output });
            RenderTexture.active = output;
            pixels = new Texture2D(480, 854, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 480, 854), 0, 0);
            pixels.Apply();
            string folder = Path.Combine(Application.dataPath, "..", "PreviewOutput");
            Directory.CreateDirectory(folder);
            string path = Path.GetFullPath(Path.Combine(folder, name + ".png"));
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Debug.Log("RUNNER_PREVIEW: " + path);
        }
        finally
        {
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousDistance;
            canvas.sortingOrder = previousSortingOrder;
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            UnityEngine.Object.FindFirstObjectByType<RunnerBackground>().RefreshLayout();
            RenderTexture.active = previousActive;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            output.Release();
            UnityEngine.Object.DestroyImmediate(output);
        }
    }
}
#endif
