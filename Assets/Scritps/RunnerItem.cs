using UnityEngine;

public class RunnerItem : MonoBehaviour
{
    public enum Kind { Heal }
    public Kind ItemKind { get; private set; }
    public bool IsCollected { get; private set; }
    private PlayerController player;
    private Transform visual;
    public static RunnerItem Create(Kind kind, Vector3 position)
    {
        GameObject obj = new GameObject("HealingItem");
        obj.transform.position = position;
        RunnerItem item = obj.AddComponent<RunnerItem>();
        item.ItemKind = kind;
        item.player = FindFirstObjectByType<PlayerController>();
        item.visual = new GameObject("ItemVisual").transform;
        item.visual.SetParent(obj.transform, false);
        item.Part("Outline", new Vector2(0.64f, 0.64f), new Color(0.04f, 0.12f, 0.18f), 45f, 5);
        item.Part("HealVertical", new Vector2(0.18f, 0.5f), new Color(0.25f, 1f, 0.65f), 0f, 6);
        item.Part("HealHorizontal", new Vector2(0.5f, 0.18f), new Color(0.25f, 1f, 0.65f), 0f, 6);
        return item;
    }
    private void Part(string name, Vector2 size, Color color, float rotation, int order)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(visual, false);
        part.transform.localScale = new Vector3(size.x, size.y, 1);
        part.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = MonsterShot.Pixel;
        renderer.color = color;
        renderer.sortingOrder = order;
    }
    private void Update()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || !manager.IsPlaying) { Destroy(gameObject); return; }
        float previousY = transform.position.y;
        transform.position += Vector3.down * Mathf.Max(2.5f, manager.ObstacleSpeed * 0.7f) * Time.deltaTime;
        visual.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f) * 0.08f);
        if (player != null && Mathf.Abs(player.transform.position.x - transform.position.x) < 0.65f &&
            player.transform.position.y >= transform.position.y - 0.45f && player.transform.position.y <= previousY + 0.45f)
            Resolve();
        if (!IsCollected && transform.position.y < ObstacleMover.MissBoundaryY) Destroy(gameObject);
    }
    public bool TryCollect(PlayerController target)
    {
        if (target == null || Mathf.Abs(target.transform.position.x - transform.position.x) >= 0.65f ||
            Mathf.Abs(target.transform.position.y - transform.position.y) > 0.65f) return false;
        return Resolve();
    }
    private bool Resolve()
    {
        if (IsCollected || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return false;
        IsCollected = true;
        gameObject.SetActive(false);
        GameManager.Instance.CollectItem(ItemKind);
        MonsterShot.Burst(transform.position, new Color(0.2f, 1f, 0.6f));
        Destroy(gameObject);
        return true;
    }
    public static void Clear()
    {
        foreach (RunnerItem item in FindObjectsByType<RunnerItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { item.gameObject.SetActive(false); Destroy(item.gameObject); }
    }
}
