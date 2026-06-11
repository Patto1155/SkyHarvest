// UnityEngine stub additions — world/island agent
// Only types that are NOT already defined in the other Stubs.*.cs files.
// Existing stubs cover: Vector2/3/2Int, Quaternion, Mathf, Color, Rect, Texture2D,
// Sprite, SpriteRenderer, Random, Time, Debug, Input, KeyCode, Physics2D, PlayerPrefs.

// SpriteLoader and SpriteAnimator are declared in global namespace (no `using`
// required) to match how the game code references them.

/// <summary>
/// Thin stub for SpriteLoader.
/// UI/bootstrap agent owns the real implementation.
/// This stub returns dummy 1×1 Sprites so the harness compiles and runs.
/// </summary>
public static class SpriteLoader
{
    public static UnityEngine.Sprite Load(string path) => MakeSprite();

    public static UnityEngine.Sprite[] LoadStrip(string path, int frameW)
        => new[] { MakeSprite() };

    public static UnityEngine.Sprite[] LoadStrip(string path, int frameW, UnityEngine.Vector2 pivot)
        => new[] { MakeSprite() };

    public static UnityEngine.Sprite LoadTile(string path) => MakeSprite();

    private static UnityEngine.Sprite MakeSprite()
    {
        var tex = new UnityEngine.Texture2D(1, 1);
        tex.SetPixel(0, 0, UnityEngine.Color.white);
        tex.Apply();
        return UnityEngine.Sprite.Create(
            tex,
            new UnityEngine.Rect(0, 0, 1, 1),
            new UnityEngine.Vector2(0.5f, 0f));
    }
}

// SpriteAnimator is already declared in HarnessShims.cs (SkyHarvest.Core.SpriteAnimator)
// — no duplicate needed here.
