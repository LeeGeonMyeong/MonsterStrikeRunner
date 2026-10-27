using UnityEngine;

public class RunnerBackground : MonoBehaviour
{
    private Camera gameCamera;
    private Transform[] tiles;
    private Sprite tileSprite;
    private float offset, tileHeight;
    private float lastAspect, lastSize;
    public float ScrollOffset => offset;
    public void RefreshLayout()
    {
        if (tiles == null || gameCamera == null) return;
        Resize();
        PositionTiles();
    }

    private void Awake()
    {
        gameCamera = Camera.main;
        Texture2D texture = Resources.Load<Texture2D>("Backgrounds/ForestRoad");
        if (gameCamera == null || texture == null) return;
        tileSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), texture.width, 0, SpriteMeshType.FullRect);
        tiles = new Transform[2];
        for (int i = 0; i < tiles.Length; i++)
        {
            GameObject tile = new GameObject("ForestBackground_" + i);
            tile.transform.SetParent(transform, false);
            tiles[i] = tile.transform;
            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = tileSprite;
            renderer.sortingOrder = -20;
        }
        // Only replace the existing road when the new background has loaded successfully.
        GameObject road = GameObject.Find("Road");
        if (road != null && road.TryGetComponent(out SpriteRenderer roadRenderer)) roadRenderer.enabled = false;
        foreach (string name in new[] { "LaneLine_Left", "LaneLine_Right" })
        {
            GameObject line = GameObject.Find(name);
            if (line != null && line.TryGetComponent(out SpriteRenderer renderer))
                renderer.color = new Color(0.65f, 0.9f, 0.95f, 0.3f);
        }
        Resize();
        PositionTiles();
    }

    private void Resize()
    {
        lastAspect = gameCamera.aspect;
        lastSize = gameCamera.orthographicSize;
        float viewHeight = lastSize * 2f;
        float width = Mathf.Max(viewHeight * lastAspect, viewHeight / tileSprite.bounds.size.y) * 1.08f;
        tileHeight = tileSprite.bounds.size.y * width;
        foreach (Transform tile in tiles) tile.localScale = new Vector3(width, width, 1f);
        offset = Mathf.Repeat(offset, tileHeight);
    }

    private void Update()
    {
        if (tiles == null || gameCamera == null) return;
        if (!Mathf.Approximately(lastAspect, gameCamera.aspect) || !Mathf.Approximately(lastSize, gameCamera.orthographicSize)) Resize();
        GameManager manager = GameManager.Instance;
        if (manager != null && manager.IsPlaying)
            offset = Mathf.Repeat(offset + manager.ObstacleSpeed * 0.3f * Time.deltaTime, tileHeight);
        PositionTiles();
    }

    private void PositionTiles()
    {
        for (int i = 0; i < tiles.Length; i++)
            tiles[i].position = new Vector3(gameCamera.transform.position.x,
                gameCamera.transform.position.y - offset + i * tileHeight, 2f);
    }
    private void OnDestroy()
    {
        if (tileSprite != null) Destroy(tileSprite);
    }
}
