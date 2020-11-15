using System;
using System.Collections.Generic;
using System.Linq;

static class TileEngine
{
    public static void DrawTile(TileTexture tiles, TileIndex index, Vector2 position)
    {
        Engine.DrawTexture(
            tiles.Texture,
            position: Game.WindowScale * position,
            scaleMode: TextureScaleMode.Nearest,
            source: new Bounds2(index * tiles.TileSize, tiles.TileSize),
            size: Game.WindowScale * tiles.TileSize);
    }

    public static void DrawTileString(TileTexture tileFont, string text, Vector2 position)
    {
        foreach (char c in text)
        {
            if (c > ' ' && c <= 127)
            {
                DrawTile(tileFont, new TileIndex(c % 16, c / 16), position);
            }
            position.X += tileFont.TileSize.X;
        }
    }
}

class TileTexture
{
    public readonly Texture Texture;
    public readonly Vector2 TileSize;

    public TileTexture(string filename, int tileWidth)
    {
        Texture = Engine.LoadTexture(filename);
        TileSize = new Vector2(tileWidth, tileWidth);
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
