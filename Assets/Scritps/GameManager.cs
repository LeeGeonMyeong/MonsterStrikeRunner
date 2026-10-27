using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum RunState { Ready, Tutorial, Playing, Ended }
    public const string TutorialPreferenceKey = "Runner.Tutorial.v1";
    public static GameManager Instance { get; private set; }
    [SerializeField] private float duration = 60f;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Text hpText;
    [SerializeField] private float startingObstacleSpeed = 2.6f;
    [SerializeField] private float maximumObstacleSpeed = 7f;
    [SerializeField] private int collisionScorePenalty = 30;
    public RunState State { get; private set; } = RunState.Ready;
    public bool IsPlaying => State == RunState.Playing;
    public bool CanControl => IsPlaying || (State == RunState.Tutorial && TutorialStep < 3);
    public int TutorialStep { get; private set; }
    public bool HasLearnedControls => PlayerPrefs.GetInt(TutorialPreferenceKey, 0) == 1;
    public int CurrentScore => Mathf.Max(0, killScore + ItemScore - deductedScore);
    public int ItemScore { get; private set; }
    public int ItemsCollected { get; private set; }
    public float ElapsedTime => elapsed;
    public float DifficultyProgress => Mathf.Clamp01(elapsed / Mathf.Max(1f, duration));
    public int DifficultyLevel => Mathf.Min(4, Mathf.FloorToInt(DifficultyProgress * 4f) + 1);
    // Match speed changes to the four visible stages, rather than an imperceptible per-frame ramp.
    public float ObstacleSpeed => Mathf.Lerp(Mathf.Max(0.1f, startingObstacleSpeed),
        Mathf.Max(startingObstacleSpeed, maximumObstacleSpeed), (DifficultyLevel - 1) / 3f);
    private float elapsed;
    private int deductedScore, killScore;
    public int Kills { get; private set; }
    public int Misses { get; private set; }
    private Text status, result;
    private GameObject gameOverPanel;
    private GameObject startPanel;
    private Font font;
    private Image healthFill, healthTrail, damageFlash;
    private RectTransform healthBar;
    private float healthTarget = 1f;
    private float flashTime;
    private Text levelNotice;
    private int displayedLevel = 1;
    private float noticeTime;
    private PlayerController player;
    private GameObject swipeArea, holdControls;
    private Image chargeFill;
    private Text chargeLabel;
    private GameObject tutorialGuide, tutorialFinish;
    private Text tutorialTitle, tutorialBody;
    private Coroutine tutorialAdvance;
    private Text itemNotice, versionLabel;
    private float itemNoticeTime;
    private void Awake()
    {
        Instance = this;
        State = RunState.Ready;
        gameObject.AddComponent<RunnerBackground>();
        gameObject.AddComponent<RunnerItemSpawner>();
        font = Resources.Load<Font>("Fonts/RunnerKorean");
        if (font == null) throw new MissingReferenceException("한글 글꼴 RunnerKorean을 찾을 수 없습니다.");
        hpText.font = font;
        player = FindFirstObjectByType<PlayerController>();
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(480f, 854f);
        scaler.matchWidthOrHeight = 0.5f;
        Image hudBackdrop = MakeImage("HudBackdrop", canvas.transform, new Color(0.025f, 0.045f, 0.09f, 0.78f));
        RectTransform hudRect = hudBackdrop.rectTransform;
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(0.5f, 1f);
        hudRect.anchoredPosition = Vector2.zero;
        hudRect.sizeDelta = new Vector2(0f, 185f);
        hudBackdrop.transform.SetAsFirstSibling();
        Place(hpText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(460f, 45f));
        hpText.fontSize = 28;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.raycastTarget = false;
        BuildHealthBar();
        status = MakeText("RunStatus", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(500f, 80f), 25);
        levelNotice = MakeText("LevelNotice", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(500f, 50f), 28);
        levelNotice.fontStyle = FontStyle.Bold;
        levelNotice.color = Color.clear;
        itemNotice = MakeText("ItemNotice", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -255f), new Vector2(460f, 45f), 24);
        itemNotice.color = Color.clear;
        BuildPlayControls();
        gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        gameOverPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = (RectTransform)gameOverPanel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        gameOverPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        result = MakeText("Result", gameOverPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(460f, 245f), 30);
        GameObject buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(gameOverPanel.transform, false);
        Place((RectTransform)buttonObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -85f), new Vector2(280f, 80f));
        buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.8f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(Restart);
        Text label = MakeText("Label", buttonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 80f), 30);
        label.text = "다시 시작";
        gameOverPanel.SetActive(false);
        BuildStartScreen();
        BuildTutorial();
        versionLabel = MakeText("BuildVersion", canvas.transform, new Vector2(1f, 1f), new Vector2(-58f, -14f), new Vector2(106f, 24f), 15);
        versionLabel.text = "v" + Application.version;
        versionLabel.color = new Color(0.8f, 0.92f, 0.93f);
        SetControlsVisible(false);
        RefreshStatus();
    }
    private void Update()
    {
        UpdateFeedback();
        if (!IsPlaying) return;
        elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
        RefreshStatus();
        if (elapsed >= duration) EndGame(true);
    }
    private void RefreshStatus()
    {
        status.text = "남은 시간 " + Mathf.CeilToInt(Mathf.Max(0f, duration - elapsed)) + "초    점수 " + CurrentScore + "\n처치 " + Kills + "    단계 " + DifficultyLevel;
        if (DifficultyLevel > displayedLevel)
        {
            displayedLevel = DifficultyLevel;
            noticeTime = 1.6f;
            levelNotice.text = displayedLevel + "단계 · 속도 " + (ObstacleSpeed / Mathf.Max(0.1f, startingObstacleSpeed)).ToString("0.0") + "배!";
        }
    }
    public void EndGame(bool survived)
    {
        if (!IsPlaying) return;
        itemNotice.text = "";
        itemNoticeTime = 0f;
        foreach (RunnerItem item in FindObjectsByType<RunnerItem>(FindObjectsSortMode.None)) item.gameObject.SetActive(false);
        State = RunState.Ended;
        result.text = (survived ? "방어 성공!" : "게임 종료") + "\n최종 점수 " + CurrentScore + "\n처치 " + Kills + " / 놓침 " + Misses + "\n아이템 " + ItemsCollected + "개 / 보너스 " + ItemScore;
        SetControlsVisible(false);
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();
        versionLabel.transform.SetAsLastSibling();
    }
    private void Restart() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void StartGame()
    {
        if (State != RunState.Ready && State != RunState.Tutorial) return;
        CancelTutorialAdvance();
        ClearPracticeShots();
        player.ResetPractice();
        elapsed = 0f;
        deductedScore = 0;
        killScore = 0;
        Kills = Misses = 0;
        ItemScore = ItemsCollected = 0;
        itemNoticeTime = 0f;
        itemNotice.text = "";
        GetComponent<RunnerItemSpawner>().ResetRun();
        displayedLevel = 1;
        noticeTime = 0f;
        State = RunState.Playing;
        startPanel.SetActive(false);
        tutorialGuide.SetActive(false);
        tutorialFinish.SetActive(false);
        SetControlsVisible(true);
        RefreshStatus();
    }
    public int ApplyCollisionPenalty()
    {
        if (!IsPlaying) return 0;
        // Only remove points already earned; low scores can recover immediately.
        int loss = Mathf.Min(CurrentScore, Mathf.Max(0, collisionScorePenalty));
        deductedScore += loss;
        RefreshStatus();
        return loss;
    }
    private void BuildStartScreen()
    {
        Image panel = MakeImage("StartPanel", canvas.transform, new Color(0.015f, 0.025f, 0.055f, 0.9f));
        panel.raycastTarget = true;
        startPanel = panel.gameObject;
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text title = MakeText("GameTitle", startPanel.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 205f), new Vector2(460f, 120f), 38);
        title.text = "몬스터 스트라이크\n러너";
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.45f, 1f, 0.85f);
        Text rules = MakeText("StartRules", startPanel.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 80f), new Vector2(460f, 120f), 21);
        rules.text = "◀ ▶ 버튼으로 차선 이동\n일반 공격 · 꾹 눌러 충전\n화면 아래로 놓치면 체력 1 감소\n일반 50점 · 강화 90점\n60초 동안 몬스터를 막아 보세요\n체력 회복 +1 · 몸으로 획득";
        rules.rectTransform.sizeDelta = new Vector2(460f, 210f);
        rules.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        GameObject obj = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(startPanel.transform, false);
        Place((RectTransform)obj.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(300f, 85f));
        Image background = obj.GetComponent<Image>();
        background.color = new Color(0.08f, 0.5f, 0.45f);
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(StartRequested);
        Text label = MakeText("StartLabel", obj.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 85f), 32);
        label.text = "게임 시작";
        label.fontStyle = FontStyle.Bold;
        Button learn = MakeControlButton("LearnButton", startPanel.transform, new Vector2(0f, -260f),
            new Vector2(280f, 55f), "조작 연습 다시 하기", new Vector2(0.5f, 0.5f));
        learn.onClick.AddListener(BeginTutorial);
        Text firstTime = MakeText("FirstPlayHint", startPanel.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, -320f), new Vector2(440f, 40f), 16);
        firstTime.text = HasLearnedControls ? "조작이 헷갈리면 연습을 다시 해 보세요." : "첫 플레이는 3단계 조작 연습부터 시작해요.";
    }
    private void Start()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) SetHealth(player.CurrentHP, player.MaxHP);
    }
    private void StartRequested()
    {
        if (State != RunState.Ready) return;
        if (HasLearnedControls) StartGame(); else BeginTutorial();
    }
    public void BeginTutorial()
    {
        if (State != RunState.Ready && State != RunState.Tutorial) return;
        CancelTutorialAdvance();
        ClearPracticeShots();
        player.ResetPractice();
        State = RunState.Tutorial;
        TutorialStep = 0;
        startPanel.SetActive(false);
        tutorialFinish.SetActive(false);
        tutorialGuide.SetActive(true);
        SetControlsVisible(true);
        RefreshTutorial();
    }
    public void ReportTutorialAction(int step)
    {
        if (State != RunState.Tutorial || TutorialStep != step || tutorialAdvance != null) return;
        tutorialAdvance = StartCoroutine(AdvanceTutorial());
    }
    private IEnumerator AdvanceTutorial()
    {
        // Give the movement/projectile a moment to show before changing the instruction.
        yield return new WaitForSecondsRealtime(0.4f);
        tutorialAdvance = null;
        if (State != RunState.Tutorial) yield break;
        TutorialStep++;
        if (TutorialStep < 3) RefreshTutorial();
        else
        {
            RememberTutorial();
            player.CancelCharge();
            ClearPracticeShots();
            SetControlsVisible(false);
            tutorialGuide.SetActive(false);
            tutorialFinish.SetActive(true);
            tutorialFinish.transform.SetAsLastSibling();
            versionLabel.transform.SetAsLastSibling();
        }
    }
    public void HintIncompleteCharge()
    {
        if (State == RunState.Tutorial && TutorialStep == 2)
            tutorialBody.text = "조금 더 길게 눌러 보세요!\n막대가 가득 차면 손을 떼세요.\n강한 공격이 여러 몬스터를 관통해요.";
    }
    private void RefreshTutorial()
    {
        string[] titles = { "1 / 3  좌우 이동", "2 / 3  일반 공격", "3 / 3  충전 공격" };
        string[] bodies = {
            "아래의 [◀] 또는 [▶]를 눌러\n차선을 바꿔 보세요.\nPC에서는 좌우 방향키도 가능해요.",
            "오른쪽 [공격]을 짧게 눌렀다 떼세요.\n현재 차선의 앞으로 발사해요.\n보라색 몬스터는 한 발로 처치해요.",
            "같은 오른쪽 [공격]을 길게 누르세요.\n막대가 가득 차면 손을 떼세요.\n주황 몬스터도 한 발로 처치해요."
        };
        tutorialTitle.text = titles[TutorialStep];
        tutorialBody.text = bodies[TutorialStep];
    }
    private void SkipTutorial()
    {
        if (State != RunState.Tutorial) return;
        RememberTutorial();
        StartGame();
    }
    private void RememberTutorial()
    {
        PlayerPrefs.SetInt(TutorialPreferenceKey, 1);
        PlayerPrefs.Save();
    }
    private void CancelTutorialAdvance()
    {
        if (tutorialAdvance != null) StopCoroutine(tutorialAdvance);
        tutorialAdvance = null;
    }
    private void ClearPracticeShots()
    {
        foreach (MonsterShot shot in FindObjectsByType<MonsterShot>(FindObjectsSortMode.None))
        {
            shot.gameObject.SetActive(false);
            Destroy(shot.gameObject);
        }
    }
    private void SetControlsVisible(bool visible)
    {
        swipeArea.SetActive(visible);
        holdControls.SetActive(visible);
    }
    private void BuildTutorial()
    {
        Image guide = MakeImage("TutorialGuide", canvas.transform, new Color(0.02f, 0.07f, 0.12f, 0.96f));
        tutorialGuide = guide.gameObject;
        Place(guide.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(440f, 225f));
        guide.rectTransform.pivot = new Vector2(0.5f, 1f);
        tutorialTitle = MakeText("TutorialTitle", guide.transform, new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(420f, 40f), 25);
        tutorialTitle.color = new Color(0.45f, 1f, 0.85f);
        tutorialBody = MakeText("TutorialBody", guide.transform, new Vector2(0.5f, 1f),
            new Vector2(0f, -100f), new Vector2(420f, 105f), 20);
        Text safe = MakeText("TutorialSafeHint", guide.transform, new Vector2(0.5f, 1f),
            new Vector2(0f, -162f), new Vector2(420f, 30f), 15);
        safe.text = "연습 중에는 시간·체력·점수가 변하지 않아요.";
        Button skip = MakeControlButton("SkipTutorialButton", guide.transform, new Vector2(0f, -199f),
            new Vector2(180f, 38f), "연습 건너뛰기", new Vector2(0.5f, 1f));
        skip.GetComponentInChildren<Text>().fontSize = 17;
        skip.onClick.AddListener(SkipTutorial);
        tutorialGuide.SetActive(false);
        Image finish = MakeImage("TutorialFinish", canvas.transform, new Color(0.015f, 0.025f, 0.055f, 0.96f));
        finish.raycastTarget = true;
        tutorialFinish = finish.gameObject;
        finish.rectTransform.anchorMin = Vector2.zero;
        finish.rectTransform.anchorMax = Vector2.one;
        finish.rectTransform.offsetMin = finish.rectTransform.offsetMax = Vector2.zero;
        Text title = MakeText("TutorialFinishTitle", finish.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 200f), new Vector2(440f, 80f), 34);
        title.text = "조작 연습 완료!";
        title.color = new Color(0.45f, 1f, 0.85f);
        Text summary = MakeText("TutorialSummary", finish.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 35f), new Vector2(440f, 220f), 22);
        summary.text = "60초 동안 몬스터를 막아 보세요.\n일반 50점 · 강화 90점\n처치와 아이템으로 점수를 얻어요.\n화면 아래로 놓치면 체력 1 감소\n체력이 0이면 게임이 끝나요.\n체력 회복 +1 · 몸으로 획득";
        Button play = MakeControlButton("TutorialPlayButton", finish.transform, new Vector2(0f, -160f),
            new Vector2(300f, 85f), "전투 시작", new Vector2(0.5f, 0.5f));
        play.GetComponentInChildren<Text>().fontSize = 30;
        play.onClick.AddListener(StartGame);
        Button retry = MakeControlButton("TutorialRetryButton", finish.transform, new Vector2(0f, -260f),
            new Vector2(250f, 55f), "처음부터 다시 연습", new Vector2(0.5f, 0.5f));
        retry.onClick.AddListener(BeginTutorial);
        tutorialFinish.SetActive(false);
    }
    private void BuildPlayControls()
    {
        Image surface = MakeImage("SwipeArea", canvas.transform, Color.clear);
        swipeArea = surface.gameObject;
        surface.raycastTarget = true;
        surface.rectTransform.anchorMin = Vector2.zero;
        surface.rectTransform.anchorMax = Vector2.one;
        surface.rectTransform.offsetMin = surface.rectTransform.offsetMax = Vector2.zero;
        surface.transform.SetAsFirstSibling();
        holdControls = new GameObject("HoldControls", typeof(RectTransform));
        foreach (GameObject group in new[] { holdControls })
        {
            group.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)group.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        Button left = MakeControlButton("MoveLeftButton", holdControls.transform,
            new Vector2(-156f, 70f), new Vector2(80f, 85f), "◀", new Vector2(0.5f, 0f));
        left.onClick.AddListener(player.MoveLeft);
        left.GetComponentInChildren<Text>().fontSize = 34;
        Button right = MakeControlButton("MoveRightButton", holdControls.transform,
            new Vector2(-64f, 70f), new Vector2(80f, 85f), "▶", new Vector2(0.5f, 0f));
        right.onClick.AddListener(player.MoveRight);
        right.GetComponentInChildren<Text>().fontSize = 34;
        Button charge = MakeControlButton("ChargeButton", holdControls.transform,
            new Vector2(110f, 70f), new Vector2(170f, 85f), "공격\n길게 눌러 충전", new Vector2(0.5f, 0f));
        charge.enabled = false;
        charge.GetComponent<Image>().color = new Color(0.4f, 0.23f, 0.1f, 0.95f);
        charge.gameObject.AddComponent<RunnerHoldInput>().player = player;
        Image chargeTrack = MakeImage("ChargeTrack", holdControls.transform, new Color(0.12f, 0.13f, 0.2f));
        Place(chargeTrack.rectTransform, new Vector2(0.5f, 0f), new Vector2(110f, 124f), new Vector2(170f, 10f));
        chargeFill = MakeImage("ChargeFill", chargeTrack.transform, new Color(1f, 0.7f, 0.2f));
        Place(chargeFill.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(170f, 10f));
        chargeFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        chargeFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
        chargeLabel = charge.GetComponentInChildren<Text>();
    }
    private Button MakeControlButton(string name, Transform parent, Vector2 position, Vector2 size, string text, Vector2 anchor)
    {
        Image image = MakeImage(name, parent, new Color(0.08f, 0.35f, 0.4f, 0.9f));
        image.raycastTarget = true;
        Place(image.rectTransform, anchor, position, size);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text label = MakeText("Label", image.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22);
        label.text = text;
        return button;
    }
    public void RecordKill(int points)
    {
        if (!IsPlaying) return;
        Kills++;
        killScore += Mathf.Max(0, points);
        RefreshStatus();
    }
    public void RecordMiss()
    {
        if (!IsPlaying) return;
        Misses++;
        noticeTime = 0.8f;
        levelNotice.text = "몬스터를 놓쳤어요! 체력 -1";
        RefreshStatus();
    }
    public void CollectItem(RunnerItem.Kind kind)
    {
        if (!IsPlaying) return;
        ItemsCollected++;
        bool healed = player.RestoreHealth();
        int points = healed ? 0 : 50;
        ItemScore += points;
        itemNotice.text = healed ? "체력 회복 +1" : "체력 가득! 보너스 +50";
        itemNoticeTime = 1.8f;
        RefreshStatus();
    }
    private void BuildHealthBar()
    {
        Image background = MakeImage("HealthBar", canvas.transform, new Color(0.07f, 0.09f, 0.16f, 0.95f));
        healthBar = background.rectTransform;
        Place(healthBar, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(280f, 22f));
        healthTrail = MakeImage("HealthLossTrail", healthBar, new Color(1f, 0.28f, 0.25f));
        healthFill = MakeImage("HealthFill", healthBar, new Color(0.25f, 0.95f, 0.65f));
        foreach (Image image in new[] { healthTrail, healthFill })
        {
            Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(274f, 16f));
            image.rectTransform.pivot = new Vector2(0f, 0.5f);
        }
        damageFlash = MakeImage("DamageFlash", canvas.transform, Color.clear);
        RectTransform rect = damageFlash.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    public void SetHealth(int hp, int maximum)
    {
        healthTarget = maximum > 0 ? Mathf.Clamp01((float)hp / maximum) : 0f;
        hpText.text = "체력 " + hp + " / " + maximum;
    }
    public void ShowDamage(Vector3 worldPosition, int lostScore)
    {
        flashTime = 0.3f;
        damageFlash.transform.SetAsLastSibling();
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,
            camera.WorldToScreenPoint(worldPosition), null, out local);
        Text number = MakeText("DamageNumber", canvas.transform, new Vector2(0.5f, 0.5f),
            local + Vector2.up * 65f, new Vector2(120f, 70f), 42);
        number.text = "-1";
        number.fontStyle = FontStyle.Bold;
        number.color = new Color(1f, 0.4f, 0.35f);
        StartCoroutine(FloatDamageNumber(number));
        if (lostScore > 0)
        {
            Text scoreNumber = MakeText("ScorePenaltyNumber", canvas.transform, new Vector2(0.5f, 0.5f),
                local + Vector2.up * 115f, new Vector2(220f, 45f), 24);
            scoreNumber.text = "점수 -" + lostScore;
            scoreNumber.fontStyle = FontStyle.Bold;
            scoreNumber.color = new Color(1f, 0.82f, 0.3f);
            StartCoroutine(FloatDamageNumber(scoreNumber));
        }
    }
    private void UpdateFeedback()
    {
        if (healthFill == null) return;
        chargeFill.rectTransform.localScale = new Vector3(player.ChargeProgress, 1f, 1f);
        chargeLabel.text = player.ChargeProgress >= 1f ? "손을 떼세요!" : (player.IsCharging ? "충전 중..." : "공격\n길게 눌러 충전");
        float delta = Time.unscaledDeltaTime;
        noticeTime = Mathf.Max(0f, noticeTime - delta);
        itemNoticeTime = Mathf.Max(0f, itemNoticeTime - delta);
        itemNotice.color = new Color(0.45f, 1f, 0.8f, Mathf.Clamp01(itemNoticeTime / 0.3f));
        levelNotice.color = new Color(1f, 0.8f, 0.3f, Mathf.Clamp01(noticeTime / 0.4f));
        float fill = Mathf.MoveTowards(healthFill.rectTransform.localScale.x, healthTarget, delta * 4f);
        float trail = Mathf.MoveTowards(healthTrail.rectTransform.localScale.x, healthTarget, delta * 0.65f);
        healthFill.rectTransform.localScale = new Vector3(fill, 1f, 1f);
        healthTrail.rectTransform.localScale = new Vector3(trail, 1f, 1f);
        healthFill.color = healthTarget <= 0.34f ? new Color(1f, 0.35f, 0.3f) : new Color(0.25f, 0.95f, 0.65f);
        flashTime = Mathf.Max(0f, flashTime - delta);
        damageFlash.color = new Color(1f, 0.08f, 0.1f, flashTime / 0.3f * 0.22f);
        healthBar.localScale = Vector3.one * (1f + flashTime / 0.3f * 0.06f);
    }
    private IEnumerator FloatDamageNumber(Text number)
    {
        float age = 0f;
        Vector2 start = number.rectTransform.anchoredPosition;
        while (age < 0.65f)
        {
            age += Time.unscaledDeltaTime;
            number.rectTransform.anchoredPosition = start + Vector2.up * age * 90f;
            Color color = number.color;
            color.a = Mathf.Clamp01(1f - age / 0.65f);
            number.color = color;
            yield return null;
        }
        Destroy(number.gameObject);
    }
    private Image MakeImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
    private Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        Place(text.rectTransform, anchor, position, size);
        return text;
    }
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
