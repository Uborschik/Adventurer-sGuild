#if TOOLS
using Godot;

[Tool]
public partial class AtlasPreview : Control
{
    private Texture2D texture;
    private int tileSize = 16;

    public void SetTexture(Texture2D texture)
    {
        this.texture = texture;
        QueueRedraw();
    }

    public void SetTileSize(int size)
    {
        tileSize = Mathf.Max(1, size);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.1f, 0.1f, 0.1f), true);

        if (texture == null) return;

        var texSize = texture.GetSize();
        if (texSize.X <= 0 || texSize.Y <= 0) return;

        float scale = Mathf.Min(Size.X / texSize.X, Size.Y / texSize.Y);
        var drawSize = texSize * scale;
        var drawPos = (Size - drawSize) / 2f;
        var rect = new Rect2(drawPos, drawSize);

        DrawTextureRect(texture, rect, false);

        float tilePx = tileSize * scale;
        var lineColor = new Color(1f, 1f, 0f, 0.5f);

        for (float x = drawPos.X; x <= drawPos.X + drawSize.X + 0.5f; x += tilePx)
            DrawLine(new Vector2(x, drawPos.Y), new Vector2(x, drawPos.Y + drawSize.Y), lineColor, 1f);

        for (float y = drawPos.Y; y <= drawPos.Y + drawSize.Y + 0.5f; y += tilePx)
            DrawLine(new Vector2(drawPos.X, y), new Vector2(drawPos.X + drawSize.X, y), lineColor, 1f);
    }
}
#endif