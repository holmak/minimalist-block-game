using System;
using System.Collections.Generic;
using System.Linq;

static class TileEngine
{
    public static void DrawTile(TileTexture tiles, TileIndex index, Vector2 position)
    {
        Engine.DrawTexture(
            tiles.Texture,
            position: position,
            scaleMode: TextureScaleMode.Nearest,
            source: new Bounds2(index * tiles.SourceSize, tiles.SourceSize),
            size: tiles.DestinationSize);
    }

    public static void DrawTileString(TileTexture tileFont, string text, Vector2 position)
    {
        float step = tileFont.DestinationSize.X;
        float left = position.X;
        foreach (char c in text)
        {
            if (c == '\n')
            {
                position.X = left;
                position.Y += step;
            }
            else
            {
                DrawTile(tileFont, new TileIndex(c % 16, c / 16), position);
                position.X += step;
            }
        }
    }
}

class TileTexture
{
    public readonly Texture Texture;
    public readonly Vector2 SourceSize;
    public readonly Vector2 DestinationSize;

    public TileTexture(string filename, int sourceWidth, int scale)
    {
        Texture = Engine.LoadTexture(filename);
        SourceSize = new Vector2(sourceWidth, sourceWidth);
        DestinationSize = SourceSize * scale;
    }
}

struct TileIndex
{
    public readonly int Column, Row;

    public TileIndex(int column, int row)
    {
        Column = column;
        Row = row;
    }

    public static TileIndex operator +(TileIndex t, TileIndex u)
    {
        return new TileIndex(t.Column + u.Column, t.Row + u.Row);
    }

    public static Vector2 operator *(TileIndex t, Vector2 v)
    {
        return new Vector2(t.Column * v.X, t.Row * v.Y);
    }
}
