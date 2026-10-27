using System.Collections.Generic;
using UnityEngine;

public class MonsterShot : MonoBehaviour
{
    private static Sprite pixel;
    public static Sprite Pixel => pixel != null ? pixel : (pixel = Sprite.Create(Texture2D.whiteTexture,
        new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f));
    public int Damage { get; private set; }
    public bool Piercing { get; private set; }
    private readonly HashSet<ObstacleMover> hit = new HashSet<ObstacleMover>();
    public static MonsterShot Create(Vector3 position, int damage, bool piercing)
    {
        GameObject obj = new GameObject(piercing ? "ChargedShot" : "NormalShot");
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(piercing ? 0.35f : 0.14f, piercing ? 0.9f : 0.5f, 1);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = Pixel;
        renderer.color = piercing ? new Color(1f, 0.7f, 0.15f) : new Color(0.3f, 1f, 0.85f);
        renderer.sortingOrder = 10;
        // A bright core and tapered trail distinguish the charged bolt at phone size.
        if (piercing)
        {
            GameObject core = new GameObject("ChargedCore");
            core.transform.SetParent(obj.transform, false);
            core.transform.localScale = new Vector3(0.4f, 0.8f, 1f);
            SpriteRenderer glow = core.AddComponent<SpriteRenderer>();
            glow.sprite = Pixel;
            glow.color = new Color(1f, 1f, 0.8f);
            glow.sortingOrder = 11;
        }
        MonsterShot shot = obj.AddComponent<MonsterShot>();
        shot.Damage = damage;
        shot.Piercing = piercing;
        return shot;
    }
    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.CanControl) { Destroy(gameObject); return; }
        float previousY = transform.position.y;
        transform.position += Vector3.up * 12f * Time.deltaTime;
        // Sweep the travelled segment to catch hits even on a slow browser frame.
        ObstacleMover target;
        do
        {
            target = null;
            foreach (ObstacleMover monster in ObstacleMover.ActiveMonsters)
            {
                if (monster == null || monster.IsResolved || hit.Contains(monster)) continue;
                Vector3 position = monster.transform.position;
                if (Mathf.Abs(position.x - transform.position.x) > 0.55f ||
                    position.y + 0.5f < previousY || position.y - 0.5f > transform.position.y) continue;
                if (target == null || position.y < target.transform.position.y) target = monster;
            }
            if (target != null)
            {
                hit.Add(target);
                target.TakeDamage(Damage);
                if (!Piercing) { Destroy(gameObject); return; }
            }
        } while (target != null);
        if (transform.position.y > 7f) Destroy(gameObject);
    }
    public static void Burst(Vector3 position, Color color)
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            GameObject obj = new GameObject("MonsterHitSpark");
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(0.16f, 0.08f, 1);
            obj.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = Pixel;
            renderer.color = color;
            renderer.sortingOrder = 15;
            obj.AddComponent<HitSpark>().velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 2f;
        }
    }
}
