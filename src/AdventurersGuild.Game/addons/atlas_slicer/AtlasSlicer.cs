#if TOOLS
using Godot;

public static class AtlasSlicer
{
    public static int SliceToPng(
        Image image,
        string outputDir,
        string filePrefix,
        int size,
        int cols,
        int rows,
        int from,
        int to)
    {
        string globalDir = ProjectSettings.GlobalizePath(outputDir);
        int saved = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                if (index < from || index > to) continue;

                var region = new Rect2I(col * size, row * size, size, size);
                if (IsRegionEmpty(image, region)) continue;

                var tile = image.GetRegion(region);
                tile.SavePng($"{globalDir}/{filePrefix}{saved}.png");
                saved++;
            }
        }

        return saved;
    }

    public static int SliceToAtlasTextures(
        Texture2D sourceTexture,
        Image image,
        string outputDir,
        string filePrefix,
        int size,
        int cols,
        int rows,
        int from,
        int to)
    {
        int saved = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                if (index < from || index > to) continue;

                var region = new Rect2I(col * size, row * size, size, size);
                if (IsRegionEmpty(image, region)) continue;

                var atlas = new AtlasTexture
                {
                    Atlas = sourceTexture,
                    Region = new Rect2(region.Position, region.Size)
                };

                string resPath = $"{outputDir}/{filePrefix}{saved}.tres";
                Error err = ResourceSaver.Save(atlas, resPath);

                if (err != Error.Ok)
                {
                    GD.PushError($"[AtlasSlicer] Failed to save {resPath}: {err}");
                    continue;
                }

                saved++;
            }
        }

        return saved;
    }

    public static bool IsRegionEmpty(Image image, Rect2I region)
    {
        for (int y = region.Position.Y; y < region.Position.Y + region.Size.Y; y++)
        {
            for (int x = region.Position.X; x < region.Position.X + region.Size.X; x++)
            {
                if (image.GetPixel(x, y).A > 0.01f)
                    return false;
            }
        }
        return true;
    }
}
#endif