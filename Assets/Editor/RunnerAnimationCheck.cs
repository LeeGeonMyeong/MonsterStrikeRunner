#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RunnerAnimationCheck
{
    private static int stage;
    private static float deadline;
    private static Sprite previous;
    private static PlayerController player;
    private static SpriteRenderer renderer;
    static RunnerAnimationCheck() { if (SessionState.GetBool("RunnerAnimationCheck", false)) EditorApplication.update += Tick; }
    public static void Start()
    {
        foreach (string name in new[] { "RunnerHeroFrames", "RunnerMonsterFrames" })
        {
            string path = "Assets/Resources/Characters/" + name + ".png";
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new Exception("Missing animation sheet " + name);
            importer.textureType = TextureImporterType.Default; importer.textureShape = TextureImporterShape.Texture2D;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("WebGL");
            settings.overridden = true; settings.maxTextureSize = 1024;
            settings.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(settings); importer.SaveAndReimport();
        }
        stage = 0; deadline = 0;
        SessionState.SetBool("RunnerAnimationCheck", true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    private static void Require(bool ok, string label)
    { if (!ok) throw new Exception("RUNNER_ANIMATION_FAIL: " + label); Debug.Log("RUNNER_ANIMATION_CHECK: " + label); }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || GameManager.Instance == null) return;
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (Time.time < deadline) return;
        try
        {
            GameManager manager = GameManager.Instance;
            switch (stage)
            {
                case 0:
                    Debug.Log("FRAME_COUNTS " + CharacterFrames.Hero.Length + " / " + CharacterFrames.Monster.Length + " direct=" + Resources.Load<Texture2D>("Characters/RunnerHeroFrames"));
                    Require(CharacterFrames.Hero.Length == 8 && CharacterFrames.Monster.Length == 4, "Eight hero and four monster frames loaded");
                    Require(CharacterFrames.Hero[0].texture == CharacterFrames.Hero[7].texture && CharacterFrames.Hero[0].rect != CharacterFrames.Hero[1].rect, "Frames use distinct regions on one texture");
                    Require(CharacterFrames.HeroAt(0, false, 0, -1) != CharacterFrames.HeroAt(.12f, false, 0, -1), "Walk switches drawings");
                    Require(CharacterFrames.HeroAt(0, true, 1, -1) == CharacterFrames.Hero[5], "Full charge holds prepared pose");
                    for (int i = 0; i < 4; i++) Require(CharacterFrames.HeroAt(0, false, 0, i * .07f + .001f) == CharacterFrames.Hero[4 + i], "Attack frame " + i);
                    Require(CharacterFrames.MonsterAt(0) != CharacterFrames.MonsterAt(.13f), "Monster walk switches drawings");
                    manager.StartGame(); manager.GetComponent<ObstacleSpawner>().enabled = false;
                    player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                    renderer = player.transform.Find("PlayerVisual").GetComponent<SpriteRenderer>();
                    deadline = Time.time + .03f; break;
                case 1:
                    Require(Array.IndexOf(CharacterFrames.Hero, renderer.sprite) >= 0, "Player uses actual animation sprites");
                    player.Attack(); deadline = Time.time + .09f; break;
                case 2:
                    Require(Array.IndexOf(CharacterFrames.Hero, renderer.sprite) >= 4, "Firing activates attack row");
                    RunnerPreviewCapture.Save("AnimationAttack");
                    deadline = Time.time + .30f; break;
                case 3:
                    Require(Array.IndexOf(CharacterFrames.Hero, renderer.sprite) < 4, "Attack returns to walking");
                    player.BeginCharge(); deadline = Time.time + .50f; break;
                case 4:
                    Require(renderer.sprite == CharacterFrames.Hero[5], "Charge state uses held drawing");
                    player.ReleaseCharge();
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacle.prefab");
                    GameObject enemy = UnityEngine.Object.Instantiate(prefab, new Vector3(2f, 3f, 0), Quaternion.identity);
                    enemy.GetComponent<ObstacleMover>().ConfigureHealth(2);
                    deadline = Time.time + .04f; break;
                case 5:
                    SpriteRenderer enemyRenderer = UnityEngine.Object.FindFirstObjectByType<ObstacleMover>().transform.Find("MonsterVisual").GetComponent<SpriteRenderer>();
                    Require(Array.IndexOf(CharacterFrames.Monster, enemyRenderer.sprite) >= 0, "Monster uses animation sheet");
                    RunnerPreviewCapture.Save("AnimationPlay");
                    player.ResetPractice();
                    Require(renderer.sprite == CharacterFrames.Hero[0], "Reset clears attack pose");
                    manager.EndGame(true);
                    previous = renderer.sprite; deadline = Time.time + .2f; break;
                case 6:
                    Require(renderer.sprite == previous, "Result stops character animation");
                    SessionState.SetBool("RunnerAnimationCheck", false);
                    Debug.Log("RUNNER_ANIMATION_PASS: sprite sheets, walk, attack, charge, monsters, reset and end");
                    EditorApplication.Exit(0); return;
            }
            stage++;
        }
        catch (Exception ex) { SessionState.SetBool("RunnerAnimationCheck", false); Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
#endif

