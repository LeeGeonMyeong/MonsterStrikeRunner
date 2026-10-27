using UnityEngine;

public class DamageFeedback : MonoBehaviour
{
    private SpriteRenderer target;
    private float remaining;
    private static Sprite spark;
    public Vector3 Offset => remaining > 0f ? Vector3.right * Mathf.Sin(remaining * 90f) * 0.07f : Vector3.zero;
    public void Initialize(SpriteRenderer sprite) { target = sprite; }
    public void Play(Vector3 position)
    {
        remaining = 0.45f;
        if (spark == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            spark = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }
        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.PI * 2f / 10f;
            GameObject obj = new GameObject("HitSpark");
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(0.16f, 0.06f, 1f);
            obj.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = spark;
            renderer.sortingOrder = 20;
            renderer.color = i % 2 == 0 ? new Color(1f, 0.8f, 0.25f) : new Color(1f, 0.3f, 0.2f);
            HitSpark particle = obj.AddComponent<HitSpark>();
            particle.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 2.3f;
        }
    }
    private void Update()
    {
        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        if (target == null) return;
        target.color = remaining <= 0f ? Color.white :
            ((int)(remaining * 22f) % 2 == 0 ? new Color(1f, 0.25f, 0.3f) : new Color(1f, 1f, 1f, 0.4f));
    }
}

public class HitSpark : MonoBehaviour
{
    public Vector3 velocity;
    private float life = 0.35f;
    private void Update()
    {
        life -= Time.unscaledDeltaTime;
        transform.position += velocity * Time.unscaledDeltaTime;
        transform.localScale *= Mathf.Exp(-5f * Time.unscaledDeltaTime);
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        Color color = sprite.color;
        color.a = Mathf.Clamp01(life / 0.35f);
        sprite.color = color;
        if (life <= 0f) Destroy(gameObject);
    }
}
