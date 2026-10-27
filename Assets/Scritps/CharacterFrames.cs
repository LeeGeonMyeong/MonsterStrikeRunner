using UnityEngine;

// One texture per character; the frame rectangles share a texture and material.
public static class CharacterFrames
{
    private static Sprite[] hero, monster;
    public static Sprite[] Hero => hero != null && hero.Length == 8 ? hero : (hero = Load("RunnerHeroFrames", 4, 2));
    public static Sprite[] Monster => monster != null && monster.Length == 4 ? monster : (monster = Load("RunnerMonsterFrames", 4, 1));
    private static Sprite[] Load(string name, int columns, int rows)
    {
        Texture2D texture = Resources.Load<Texture2D>("Characters/" + name);
        if (texture == null) return new Sprite[0];
        float width = (float)texture.width / columns, height = (float)texture.height / rows;
        Sprite[] frames = new Sprite[columns * rows];
        for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                frames[index] = Sprite.Create(texture,
                    new Rect(column * width, texture.height - (row + 1) * height, width, height),
                    new Vector2(.5f, .5f), width / 1.2f, 0, SpriteMeshType.FullRect);
                frames[index].name = name + "_" + index;
            }
        return frames;
    }
    public static Sprite HeroAt(float walkTime, bool charging, float charge, float attackElapsed)
    {
        Sprite[] frames = Hero;
        if (frames.Length != 8) return CharacterArt.Hero;
        int index = attackElapsed >= 0f && attackElapsed < .28f
            ? 4 + Mathf.Min(3, Mathf.FloorToInt(attackElapsed / .07f))
            : charging ? 4 + (charge >= 1f ? 1 : 0)
            : Mathf.FloorToInt(walkTime * 9f) % 4;
        return frames[index];
    }
    public static Sprite MonsterAt(float walkTime)
    {
        Sprite[] frames = Monster;
        return frames.Length == 4 ? frames[Mathf.FloorToInt(walkTime * 8f) % 4] : CharacterArt.Monster;
    }
}
