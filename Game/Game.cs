using System;
using System.Collections.Generic;

class Game
{
    public static readonly string Title = "Minimalist RPG Demo";
    public static readonly int WindowScale = 4;
    public static readonly Vector2 Resolution = WindowScale * new Vector2(320, 240);
    public static readonly bool Debug = true;

    TileTexture DungeonTiles = new TileTexture("dungeon_tiles.png", 16);
    TileTexture FontTiles = new TileTexture("font_tiles.png", 8);

    public Game()
    {
        if (Debug)
        {
            Engine.SetWindowDisplay(1);
        }
    }

    public void Update()
    {
        for (int i = 0; i <= 5; i++)
        {
            TileEngine.DrawTile(DungeonTiles, i, 0, new Vector2(i, 0) * DungeonTiles.TileSize);
        }
        TileEngine.DrawTileString(FontTiles, "Hello, world!", new Vector2(0, 80));
    }
}

static class TileEngine
{
    public static void DrawTile(TileTexture tiles, int column, int row, Vector2 position)
    {
        Engine.DrawTexture(
            tiles.Texture,
            position: Game.WindowScale * position,
            scaleMode: TextureScaleMode.Nearest,
            source: new Bounds2(new Vector2(column, row) * tiles.TileSize, tiles.TileSize),
            size: Game.WindowScale * tiles.TileSize);
    }

    public static void DrawTileString(TileTexture tileFont, string text, Vector2 position)
    {
        foreach (char c in text)
        {
            if (c > ' ' && c <= 127)
            {
                DrawTile(tileFont, c % 16, c / 16, position);
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
