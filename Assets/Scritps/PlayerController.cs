using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float laneDistance = 2f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private int maxHP = 3;
    [SerializeField] private Text hpText;
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private float chargeDuration = 0.45f;
    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public float LaneDistance => laneDistance;
    public bool IsCharging { get; private set; }
    public float ChargeProgress => IsCharging ? Mathf.Clamp01((Time.time - chargeStarted) / chargeDuration) : 0f;
    public bool CanAttack => GameManager.Instance != null && GameManager.Instance.CanControl && CurrentHP > 0;
    private Rigidbody2D body;
    private Transform visual;
    private int currentLane = 1;
    private float groundY, nextAttack, chargeStarted, attackFlash;
    private DamageFeedback feedback;
    private SpriteRenderer artwork;
    private bool chargeSoundPlayed;
    private float animationAttackAt = -10f;
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        groundY = transform.position.y;
        CurrentHP = maxHP;
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        GameObject child = new GameObject("PlayerVisual");
        child.transform.SetParent(transform, false);
        visual = child.transform;
        artwork = child.AddComponent<SpriteRenderer>();
        artwork.sprite = CharacterArt.Hero != null ? CharacterArt.Hero : source.sprite;
        artwork.color = Color.white;
        artwork.sortingLayerID = source.sortingLayerID;
        artwork.sortingOrder = source.sortingOrder;
        source.enabled = false;
        feedback = gameObject.AddComponent<DamageFeedback>();
        feedback.Initialize(artwork);
        UpdateHP();
    }
    private void Update()
    {
        if (!CanAttack) { CancelCharge(); return; }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame) MoveLeft();
            if (keyboard.rightArrowKey.wasPressedThisFrame) MoveRight();
            if (keyboard.spaceKey.wasPressedThisFrame) Attack();
            if (keyboard.downArrowKey.wasPressedThisFrame) BeginCharge();
            if (keyboard.downArrowKey.wasReleasedThisFrame) ReleaseCharge();
        }
        if (IsCharging && ChargeProgress >= 1f && !chargeSoundPlayed)
        { chargeSoundPlayed = true; RunnerAudio.Instance?.ChargeReady(); }
        artwork.sprite = CharacterFrames.HeroAt(Time.time, IsCharging, ChargeProgress, Time.time - animationAttackAt);
        attackFlash = Mathf.Max(0f, attackFlash - Time.deltaTime);
        float stride = Mathf.Sin(Time.time * 14f);
        float lean = Mathf.Clamp(((currentLane - 1) * laneDistance - body.position.x) * -12f, -15f, 15f);
        float recoil = attackFlash / 0.2f;
        visual.localPosition = Vector3.up * (Mathf.Abs(stride) * 0.07f - recoil * 0.12f) + feedback.Offset;
        visual.localRotation = Quaternion.Euler(0, 0, lean + (IsCharging ? Mathf.Sin(Time.time * 35f) * ChargeProgress * 5f : stride * 3f));
        visual.localScale = new Vector3(1f + ChargeProgress * 0.12f + recoil * 0.15f - stride * 0.025f,
            1f + ChargeProgress * 0.06f - recoil * 0.12f + stride * 0.035f, 1f);
    }
    private void FixedUpdate()
    {
        if (!CanAttack) return;
        float x = Mathf.MoveTowards(body.position.x, (currentLane - 1) * laneDistance, moveSpeed * Time.fixedDeltaTime);
        body.MovePosition(new Vector2(x, groundY));
    }
    public void MoveLeft() { ChangeLane(-1); }
    public void MoveRight() { ChangeLane(1); }
    private void ChangeLane(int direction)
    {
        if (!CanAttack) return;
        int next = Mathf.Clamp(currentLane + direction, 0, 2);
        if (next == currentLane) return;
        currentLane = next;
        GameManager.Instance.ReportTutorialAction(0);
    }
    public void Attack()
    {
        if (!IsCharging && Fire(1, false)) GameManager.Instance.ReportTutorialAction(1);
    }
    public void BeginCharge()
    {
        if (!CanAttack || IsCharging || Time.time < nextAttack) return;
        IsCharging = true;
        chargeStarted = Time.time;
        chargeSoundPlayed = false;
    }
    public void ReleaseCharge()
    {
        if (!IsCharging) return;
        bool full = ChargeProgress >= 1f;
        IsCharging = false;
        bool fired = Fire(full ? 2 : 1, full);
        if (full && fired) GameManager.Instance.ReportTutorialAction(2);
        else if (fired && GameManager.Instance.TutorialStep == 1) GameManager.Instance.ReportTutorialAction(1);
        else if (!full) GameManager.Instance.HintIncompleteCharge();
    }
    public void CancelCharge() { IsCharging = false; }
    private bool Fire(int damage, bool piercing)
    {
        if (!CanAttack || Time.time < nextAttack) return false;
        // A full charge recovers quickly; normal fire still reacts faster to close threats.
        nextAttack = Time.time + (piercing ? 0.12f : attackCooldown);
        animationAttackAt = Time.time;
        attackFlash = 0.2f;
        RunnerAudio.Instance?.Fire(piercing);
        MonsterShot.Create(new Vector3(body.position.x, groundY + 0.65f, 0f), damage, piercing);
        return true;
    }
    public void ResetPractice()
    {
        CancelCharge();
        currentLane = 1;
        body.position = new Vector2(0f, groundY);
        transform.position = new Vector3(0f, groundY, transform.position.z);
        CurrentHP = maxHP;
        nextAttack = attackFlash = 0f;
        animationAttackAt = -10f;
        if (artwork != null) artwork.sprite = CharacterFrames.HeroAt(0f, false, 0f, -1f);
        if (visual != null) { visual.localPosition = Vector3.zero; visual.localScale = Vector3.one; }
        UpdateHP();
    }
    public void TakeMissDamage()
    {
        if (!CanAttack || !GameManager.Instance.IsPlaying) return;
        CurrentHP = Mathf.Max(0, CurrentHP - 1);
        RunnerAudio.Instance?.Hurt();
        UpdateHP();
        feedback.Play(visual.position);
        int lostScore = GameManager.Instance.ApplyCollisionPenalty();
        GameManager.Instance.ShowDamage(visual.position, lostScore);
        if (CurrentHP == 0) GameManager.Instance.EndGame(false);
    }
    public bool RestoreHealth()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || CurrentHP <= 0 || CurrentHP >= maxHP) return false;
        CurrentHP++;
        UpdateHP();
        return true;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelCharge(); }
    private void OnDisable() { CancelCharge(); }
    private void UpdateHP()
    {
        if (hpText != null) hpText.text = "HP : " + CurrentHP;
        if (GameManager.Instance != null) GameManager.Instance.SetHealth(CurrentHP, maxHP);
    }
}
