#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RunnerSmokeCheck
{
    private static int stage;
    private static float deadline;
    private static ObstacleMover normal, armored, overlapping;
    private static int stoppedScore;
    static RunnerSmokeCheck()
    {
        if (SessionState.GetBool("RunnerCombatCheck", false)) EditorApplication.update += Tick;
    }
    public static void Start()
    {
        stage = 0;
        deadline = 0;
        SessionState.SetBool("RunnerCombatCheck", true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception("RUNNER_COMBAT_FAIL: " + message);
        Debug.Log("RUNNER_COMBAT_CHECK: " + message);
    }
    private static ObstacleMover Spawn(float x, float y, int health = 1)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacle.prefab");
        ObstacleMover monster = UnityEngine.Object.Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity).GetComponent<ObstacleMover>();
        monster.ConfigureHealth(health);
        return monster;
    }
    private static PointerEventData Pointer(float x, int id = 1)
        => new PointerEventData(EventSystem.current) { pointerId = id, position = new Vector2(x, 350), button = PointerEventData.InputButton.Left };
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
                    Require(manager.State == GameManager.RunState.Ready, "Ready screen");
                    float openingSpeed = manager.ObstacleSpeed;
                    ObstacleSpawner spawner = manager.GetComponent<ObstacleSpawner>();
                    Require(Mathf.Abs(openingSpeed - 2.6f) < 0.01f && spawner.CurrentSpawnInterval >= 2.3f, "Opening speed and spawn spacing");
                    float previousSpeed = openingSpeed;
                    for (int level = 1; level <= 4; level++)
                    {
                        typeof(GameManager).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, (level - 1) * 15f);
                        float stageSpeed = manager.ObstacleSpeed;
                        Require(manager.DifficultyLevel == level && (level == 1 || stageSpeed / previousSpeed >= 1.25f), "Noticeable speed increase at stage " + level);
                        typeof(GameManager).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, level * 15f - 0.01f);
                        Require(Mathf.Abs(manager.ObstacleSpeed - stageSpeed) < 0.001f, "Speed remains stable within stage " + level);
                        previousSpeed = stageSpeed;
                    }
                    typeof(GameManager).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, 60f);
                    Require(manager.ObstacleSpeed > openingSpeed && spawner.CurrentSpawnInterval < 1.1f, "Difficulty ramps to final pace");
                    typeof(GameManager).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, 0f);
                    Require(CharacterArt.Hero != null && CharacterArt.Monster != null, "Unified character art loads");
                    player.Attack(); player.BeginCharge(); player.MoveRight();
                    Require(!player.IsCharging && UnityEngine.Object.FindObjectsByType<MonsterShot>(FindObjectsSortMode.None).Length == 0, "No combat before START");
                    RunnerPreviewCapture.Save("CombatStart");
                    manager.StartGame();
                    manager.GetComponent<ObstacleSpawner>().enabled = false;
                    foreach (ObstacleMover monster in UnityEngine.Object.FindObjectsByType<ObstacleMover>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(monster.gameObject);
                    normal = Spawn(0, -1);
                    Require(GameObject.Find("AttackButton") == null, "Single attack button replaces duplicate normal button");
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerUpHandler);
                    player.Attack();
                    Require(UnityEngine.Object.FindObjectsByType<MonsterShot>(FindObjectsSortMode.None).Length == 1, "Attack cooldown blocks duplicate shot");
                    deadline = Time.time + 0.4f;
                    break;
                case 1:
                    Require(manager.Kills == 1 && player.CurrentHP == 3 && manager.CurrentScore == 50, "Normal kill gives exactly 50 points without survival gain");
                    Require(normal == null || !normal.TakeDamage(1), "Killed monster cannot reward twice");
                    armored = Spawn(0, 3, 2);
                    player.Attack();
                    deadline = Time.time + 0.45f;
                    break;
                case 2:
                    Require(armored != null && armored.Health == 1 && manager.Kills == 1, "Armored monster survives first normal shot");
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerDownHandler);
                    Require(player.IsCharging, "Pointer down begins charge");
                    player.Attack();
                    Require(UnityEngine.Object.FindObjectsByType<MonsterShot>(FindObjectsSortMode.None).Length == 0, "Normal attack blocked while charging");
                    deadline = Time.time + 0.7f;
                    break;
                case 3:
                    Require(player.ChargeProgress >= 1f, "Hold duration fills charge");
                    Spawn(0, 0, 2); Spawn(0, 1);
                    RunnerPreviewCapture.Save("CombatCharged");
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0, 2), ExecuteEvents.pointerUpHandler);
                    Require(player.IsCharging, "Other pointer cannot release charge");
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerUpHandler);
                    Require(!player.IsCharging && UnityEngine.Object.FindFirstObjectByType<MonsterShot>().Piercing, "Release fires charged projectile");
                    float recovery = (float)typeof(PlayerController).GetField("nextAttack", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player) - Time.time;
                    Require(recovery > 0f && recovery <= 0.13f, "Charged attack has fast recovery advantage");
                    deadline = Time.time + 0.4f;
                    break;
                case 4:
                    Require(manager.Kills == 4 && player.CurrentHP == 3 && manager.CurrentScore == 280, "Charged shot pierces multiple enemies with correct 50/90 kill rewards");
                    overlapping = Spawn(player.transform.position.x, player.transform.position.y);
                    deadline = Time.time + 0.15f;
                    break;
                case 5:
                    Require(player.CurrentHP == 3 && manager.Misses == 0 && !overlapping.GetComponent<BoxCollider2D>().enabled, "Direct overlap and crossing behind player cause no damage");
                    overlapping.transform.position = new Vector3(2f, ObstacleMover.MissBoundaryY - 0.1f, 0f);
                    deadline = Time.time + 0.15f;
                    break;
                case 6:
                    Require(player.CurrentHP == 2 && manager.Misses == 1, "Miss in another lane removes one HP");
                    Require((int)typeof(GameManager).GetField("deductedScore", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager) == 30, "Miss deducts score once");
                    Spawn(0, 1); Spawn(-2, 3, 2);
                    RunnerPreviewCapture.Save("CombatPlaying");
                    GameObject.Find("MoveLeftButton").GetComponent<Button>().onClick.Invoke();
                    GameObject.Find("MoveLeftButton").GetComponent<Button>().onClick.Invoke();
                    GameObject.Find("MoveLeftButton").GetComponent<Button>().onClick.Invoke();
                    Require((int)typeof(PlayerController).GetField("currentLane", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player) == 0, "Left arrow stops at left lane boundary");
                    for (int i = 0; i < 3; i++) GameObject.Find("MoveRightButton").GetComponent<Button>().onClick.Invoke();
                    Require((int)typeof(PlayerController).GetField("currentLane", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player) == 2, "Right arrow stops at right lane boundary");
                    deadline = Time.time + 0.3f;
                    break;
                case 7:
                    Require(Mathf.Abs(player.transform.position.x - 2f) < 0.05f, "Swipe changes aiming lane");
                    player.BeginCharge();
                    GameObject.Find("ChargeButton").SetActive(false);
                    Require(!player.IsCharging, "Disabled hold UI cancels charge without firing");
                    Spawn(-2, ObstacleMover.MissBoundaryY - 0.1f);
                    deadline = Time.time + 0.15f;
                    break;
                case 8:
                    Require(player.CurrentHP == 1 && manager.Misses == 2, "Second miss removes one HP");
                    Spawn(0, ObstacleMover.MissBoundaryY - 0.1f);
                    deadline = Time.time + 0.15f;
                    break;
                case 9:
                    Require(player.CurrentHP == 0 && manager.State == GameManager.RunState.Ended, "Third miss ends game");
                    Require(GameObject.Find("Result").GetComponent<Text>().text.Contains("처치 4 / 놓침 3"), "Result displays kill and miss totals");
                    stoppedScore = manager.CurrentScore;
                    player.Attack();
                    Require(manager.ApplyCollisionPenalty() == 0, "No score penalties after end");
                    RunnerPreviewCapture.Save("CombatResult");
                    deadline = Time.time + 0.2f;
                    break;
                case 10:
                    Require(manager.CurrentScore == stoppedScore, "Score freezes after end");
                    GameObject.Find("RestartButton").GetComponent<Button>().onClick.Invoke();
                    deadline = Time.time + 0.3f;
                    break;
                case 11:
                    Require(player.CurrentHP == 3 && manager.CurrentScore == 0 && manager.Kills == 0 && manager.Misses == 0 && manager.State == GameManager.RunState.Ready, "Restart resets combat and score");
                    Require(ObstacleMover.ActiveMonsters.Count == 0 && !player.IsCharging, "Restart clears enemies and charge");
                    manager.StartGame();
                    typeof(GameManager).GetField("duration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, 0.2f);
                    deadline = Time.time + 0.3f;
                    break;
                case 12:
                    Require(manager.State == GameManager.RunState.Ended && GameObject.Find("Result").GetComponent<Text>().text.Contains("방어 성공!"), "Survival timer still ends run");
                    Require(manager.CurrentScore == 0, "Surviving a run without kills earns zero points");
                    SessionState.SetBool("RunnerCombatCheck", false);
                    Debug.Log("RUNNER_COMBAT_PASS: normal shot, armor HP, charge, pierce, cooldown, off-lane miss damage, swipe, result, restart and timer");
                    EditorApplication.Exit(0);
                    return;
            }
            stage++;
        }
        catch (Exception exception)
        {
            SessionState.SetBool("RunnerCombatCheck", false);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
#endif
