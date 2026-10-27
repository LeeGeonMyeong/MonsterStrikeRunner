#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RunnerItemCheck
{
    private static int stage;
    private static float deadline;
    static RunnerItemCheck() { if (SessionState.GetBool("RunnerItemCheck", false)) EditorApplication.update += Tick; }
    public static void Start()
    {
        stage = 0; deadline = 0;
        SessionState.SetBool("RunnerItemCheck", true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    private static void Require(bool value, string label)
    { if (!value) throw new Exception("RUNNER_ITEM_FAIL: " + label); Debug.Log("RUNNER_ITEM_CHECK: " + label); }
    private static void SetElapsed(GameManager manager, float value)
        => typeof(GameManager).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, value);
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || GameManager.Instance == null) return;
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (Time.time < deadline) return;
        try
        {
            GameManager manager = GameManager.Instance;
            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            switch (stage)
            {
                case 0:
                    Require(GameObject.Find("BuildVersion").GetComponent<Text>().text == "v1.1.1", "Visible release version on menu");
                    manager.BeginTutorial();
                    RunnerPreviewCapture.Save("TopViewTutorial");
                    RunnerItem practice = RunnerItem.Create(RunnerItem.Kind.Heal, player.transform.position);
                    Require(!practice.TryCollect(player) && manager.ItemScore == 0, "No item reward during tutorial");
                    manager.StartGame();
                    manager.GetComponent<ObstacleSpawner>().enabled = false;
                    manager.GetComponent<RunnerItemSpawner>().enabled = false;
                    RunnerItem heal = RunnerItem.Create(RunnerItem.Kind.Heal, player.transform.position);
                    Require(heal.TryCollect(player) && !heal.TryCollect(player), "Pickup grants reward exactly once");
                    Require(player.CurrentHP == 3 && manager.ItemScore == 50, "Full health converts healing item to 50 points");
                    manager.RecordMiss();
                    player.TakeMissDamage();
                    Require(player.CurrentHP == 2 && manager.CurrentScore == 20, "Miss penalty still applies to earned item points");
                    Require(RunnerItem.Create(RunnerItem.Kind.Heal, player.transform.position).TryCollect(player) && player.CurrentHP == 3, "Healing restores one HP");
                    RunnerItem.Create(RunnerItem.Kind.Heal, player.transform.position).TryCollect(player);
                    Require(manager.ItemScore == 100 && manager.CurrentScore == 70 && manager.Kills == 0, "Full-health heal converts to 50 without changing kills");
                    RunnerItem wrongLane = RunnerItem.Create(RunnerItem.Kind.Heal, player.transform.position + Vector3.right * 2f);
                    Require(!wrongLane.TryCollect(player), "Items in another lane cannot be collected");
                    wrongLane.transform.position = new Vector3(2f, ObstacleMover.MissBoundaryY - 0.2f, 0f);
                    RunnerItem.Create(RunnerItem.Kind.Heal, player.transform.position + Vector3.up * 0.6f);
                    RunnerPreviewCapture.Save("TopViewItems");
                    deadline = Time.time + 0.5f;
                    break;
                case 1:
                    Require(manager.CurrentScore == 120 && manager.ItemsCollected == 4, "Falling item collects when crossing player");
                    Require(player.CurrentHP == 3 && manager.Misses == 1, "Missed item causes no damage or monster miss");
                    manager.GetComponent<RunnerItemSpawner>().enabled = true;
                    SetElapsed(manager, 6f);
                    deadline = Time.time + 0.05f;
                    break;
                case 2:
                    Require(UnityEngine.Object.FindObjectsByType<RunnerItem>(FindObjectsSortMode.None).Length == 1, "First item spawns at six seconds");
                    Require(UnityEngine.Object.FindFirstObjectByType<RunnerItem>().ItemKind == RunnerItem.Kind.Heal, "First scheduled item heals");
                    SetElapsed(manager, 14f);
                    deadline = Time.time + 0.05f;
                    break;
                case 3:
                    RunnerItem[] items = UnityEngine.Object.FindObjectsByType<RunnerItem>(FindObjectsSortMode.None);
                    Require(items.Length == 2 && Array.TrueForAll(items, x => x.ItemKind == RunnerItem.Kind.Heal), "Every scheduled item heals; no gold crystals");
                    RunnerPreviewCapture.Save("TopViewScheduledItems");
                    manager.EndGame(true);
                    Require(GameObject.Find("Result").GetComponent<Text>().text.Contains("아이템 4개 / 보너스 150"), "Result reports item totals");
                    Require(GameObject.Find("BuildVersion").transform.GetSiblingIndex() > GameObject.Find("GameOverPanel").transform.GetSiblingIndex(), "Version remains visible above result overlay");
                    Require(!RunnerItem.Create(RunnerItem.Kind.Heal, player.transform.position).TryCollect(player), "No pickup after game ends");
                    RunnerPreviewCapture.Save("TopViewResult");
                    GameObject.Find("RestartButton").GetComponent<Button>().onClick.Invoke();
                    deadline = Time.time + 0.3f;
                    break;
                case 4:
                    Require(manager.State == GameManager.RunState.Ready && manager.ItemScore == 0 && manager.ItemsCollected == 0 && player.CurrentHP == 3, "Restart resets items health and bonus");
                    Require(UnityEngine.Object.FindObjectsByType<RunnerItem>(FindObjectsSortMode.None).Length == 0, "Restart removes every item");
                    SessionState.SetBool("RunnerItemCheck", false);
                    Debug.Log("RUNNER_ITEM_PASS: release version, tutorial isolation, heal cap, conversion, bonus, pickup lane and sweep, spawning, result and restart");
                    EditorApplication.Exit(0); return;
            }
            stage++;
        }
        catch (Exception ex) { SessionState.SetBool("RunnerItemCheck", false); Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
#endif
