#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RunnerTutorialCheck
{
    private static int stage;
    private static float deadline;
    static RunnerTutorialCheck()
    {
        if (SessionState.GetBool("RunnerTutorialCheck", false)) EditorApplication.update += Tick;
    }
    public static void Start()
    {
        SessionState.SetBool("RunnerTutorialCheck.HadPreference", PlayerPrefs.HasKey(GameManager.TutorialPreferenceKey));
        SessionState.SetInt("RunnerTutorialCheck.OldPreference", PlayerPrefs.GetInt(GameManager.TutorialPreferenceKey, 0));
        PlayerPrefs.DeleteKey(GameManager.TutorialPreferenceKey);
        PlayerPrefs.Save();
        SessionState.SetBool("RunnerTutorialCheck", true);
        stage = 0;
        deadline = 0;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    private static void RestorePreference()
    {
        if (SessionState.GetBool("RunnerTutorialCheck.HadPreference", false))
            PlayerPrefs.SetInt(GameManager.TutorialPreferenceKey, SessionState.GetInt("RunnerTutorialCheck.OldPreference", 0));
        else PlayerPrefs.DeleteKey(GameManager.TutorialPreferenceKey);
        PlayerPrefs.Save();
        SessionState.SetBool("RunnerTutorialCheck", false);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("RUNNER_TUTORIAL_FAIL: " + message);
        Debug.Log("RUNNER_TUTORIAL_CHECK: " + message);
    }
    private static PointerEventData Pointer(float x, int id = 1)
        => new PointerEventData(EventSystem.current) { pointerId = id, position = new Vector2(x, 350), button = PointerEventData.InputButton.Left };
    private static void Click(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
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
                    Require(!manager.HasLearnedControls && manager.State == GameManager.RunState.Ready, "Fresh player starts on Korean menu");
                    Require(GameObject.Find("StartLabel").GetComponent<Text>().text == "게임 시작", "Korean start button");
                    foreach (Text label in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        foreach (char c in label.text)
                            if (c >= 0xAC00 && c <= 0xD7A3) Require(label.font.HasCharacter(c), "Embedded Korean glyph " + c);
                    RunnerPreviewCapture.Save("KoreanStart");
                    Click("StartButton");
                    Require(manager.State == GameManager.RunState.Tutorial && manager.TutorialStep == 0, "First start opens tutorial");
                    player.Attack();
                    deadline = Time.time + 0.5f;
                    break;
                case 1:
                    Require(manager.TutorialStep == 0, "Wrong action does not skip swipe lesson");
                    Require(manager.ElapsedTime == 0 && manager.CurrentScore == 0 && player.CurrentHP == 3 && ObstacleMover.ActiveMonsters.Count == 0, "Practice freezes timer score HP and spawning");
                    player.TakeMissDamage();
                    Require(player.CurrentHP == 3, "Practice cannot damage player");
                    RunnerPreviewCapture.Save("TutorialSwipe");
                    Require(GameObject.Find("MovePad") == null, "Swipe pad replaced by arrow buttons");
                    int shotsBeforeMove = UnityEngine.Object.FindObjectsByType<MonsterShot>(FindObjectsSortMode.None).Length;
                    Click("MoveRightButton");
                    Require(!player.IsCharging && UnityEngine.Object.FindObjectsByType<MonsterShot>(FindObjectsSortMode.None).Length == shotsBeforeMove, "Movement arrow does not attack");
                    deadline = Time.time + 0.5f;
                    break;
                case 2:
                    Require(manager.TutorialStep == 1 && player.transform.position.x > 1.9f, "Right arrow advances to attack lesson");
                    RunnerPreviewCapture.Save("TutorialAttack");
                    Require(GameObject.Find("AttackButton") == null, "No duplicate attack button");
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerUpHandler);
                    deadline = Time.time + 0.5f;
                    break;
                case 3:
                    Require(manager.TutorialStep == 2, "Real attack advances to charge lesson");
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerDownHandler);
                    deadline = Time.time + 0.1f;
                    break;
                case 4:
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerUpHandler);
                    deadline = Time.time + 0.5f;
                    break;
                case 5:
                    Require(manager.TutorialStep == 2 && GameObject.Find("TutorialBody").GetComponent<Text>().text.Contains("길게"), "Short hold prompts retry without advancing");
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerDownHandler);
                    deadline = Time.time + 0.7f;
                    break;
                case 6:
                    Require(player.ChargeProgress >= 1f, "Charge meter fills during practice");
                    RunnerPreviewCapture.Save("TutorialCharge");
                    ExecuteEvents.Execute(GameObject.Find("ChargeButton"), Pointer(0), ExecuteEvents.pointerUpHandler);
                    deadline = Time.time + 0.5f;
                    break;
                case 7:
                    Require(manager.TutorialStep == 3 && manager.HasLearnedControls && GameObject.Find("TutorialFinish") != null, "Completed practice is remembered and shows rule summary");
                    Require(manager.ElapsedTime == 0 && manager.CurrentScore == 0 && player.CurrentHP == 3, "No practice cost");
                    RunnerPreviewCapture.Save("TutorialComplete");
                    Click("TutorialPlayButton");
                    manager.GetComponent<ObstacleSpawner>().enabled = false;
                    Require(manager.IsPlaying && player.transform.position.x == 0 && !player.IsCharging && manager.CurrentScore == 0, "Battle starts clean in center lane");
                    Require(UnityEngine.Object.FindObjectsByType<MonsterShot>(FindObjectsSortMode.None).Length == 0, "No practice projectile leaks into battle");
                    manager.EndGame(false);
                    Require(GameObject.Find("Result").GetComponent<Text>().text.Contains("최종 점수"), "Korean result");
                    RunnerPreviewCapture.Save("KoreanResult");
                    Click("RestartButton");
                    deadline = Time.time + 0.3f;
                    break;
                case 8:
                    Click("StartButton");
                    Require(manager.IsPlaying, "Returning player starts battle without forced tutorial");
                    manager.EndGame(false);
                    Click("RestartButton");
                    deadline = Time.time + 0.3f;
                    break;
                case 9:
                    Click("LearnButton");
                    Require(manager.State == GameManager.RunState.Tutorial && manager.TutorialStep == 0, "Tutorial can be replayed from menu");
                    player.MoveLeft();
                    Click("SkipTutorialButton");
                    Require(manager.IsPlaying && player.transform.position.x == 0, "Skip starts clean battle");
                    deadline = Time.time + 0.5f;
                    break;
                case 10:
                    Require(manager.IsPlaying && GameObject.Find("TutorialGuide") == null, "Pending tutorial advance cannot reopen lesson after skip");
                    RestorePreference();
                    Debug.Log("RUNNER_TUTORIAL_PASS: Korean glyphs, first play, swipe, attack, charge retry, completion, clean start, persistence, replay and skip");
                    EditorApplication.Exit(0);
                    return;
            }
            stage++;
        }
        catch (Exception error)
        {
            RestorePreference();
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
#endif
