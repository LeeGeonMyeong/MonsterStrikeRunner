using UnityEngine;

// Cache generated textures as sprites so no manual Inspector assignment is required.
public static class CharacterArt
{
    private static Sprite hero, monster;
    public static Sprite Hero => hero != null ? hero : (hero = Load("RunnerHero"));
    public static Sprite Monster => monster != null ? monster : (monster = Load("RunnerMonster"));
    private static Sprite Load(string name)
    {
        Texture2D texture = Resources.Load<Texture2D>("Characters/" + name);
        if (texture == null) { Debug.LogWarning("Missing character art: " + name); return null; }
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), texture.width / 1.2f, 0, SpriteMeshType.FullRect);
    }
}
