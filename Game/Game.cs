using System;
using System.Collections.Generic;

class Game
{
    public static readonly string Title = "Minimalist RPG Demo";
    public static readonly int WindowScale = 4;
    public static readonly Vector2 Resolution = WindowScale * new Vector2(320, 240);
    public static readonly bool Debug = true;

    TileTexture WallTiles = new TileTexture("wall_tiles.png", 16);
    TileTexture PropTiles = new TileTexture("prop_tiles.png", 16);
    TileTexture UITiles = new TileTexture("ui_tiles.png", 16);
    TileTexture FontTiles = new TileTexture("font_tiles.png", 8);

    TileIndex[] Walls = new TileIndex[50];

    EditMode EditMode = EditMode.None;
    TileIndex SelectedWallTile;

    public Game()
    {
        if (Debug)
        {
            Engine.SetWindowDisplay(1);
        }
    }

    public void Update()
    {
        for (int i = 0; i < Walls.Length; i++)
        {
            TileEngine.DrawTile(WallTiles, Walls[i], new Vector2(i, 0) * WallTiles.TileSize);
        }
        TileEngine.DrawTileString(FontTiles, "Hello, world!", new Vector2(0, 80));

        if (Debug)
        {
            UpdateEditor();
        }
    }

    void UpdateEditor()
    {
        TileIndex hovered = GetHoveredCell(Engine.MousePosition, WallTiles);

        if (EditMode == EditMode.None)
        {
            if (Engine.GetKeyDown(Key.LeftControl))
            {
                EditMode = EditMode.SelectWall;
            }
        }
        else if (EditMode == EditMode.SelectWall)
        {
            Engine.DrawTexture(WallTiles.Texture, Vector2.Zero, size: WallTiles.Texture.Size * WindowScale, scaleMode: TextureScaleMode.Nearest);
            TileIndex paletteHovered = GetHoveredCell(Engine.MousePosition, WallTiles);
            TileEngine.DrawTile(UITiles, new TileIndex(1, 0), paletteHovered * WallTiles.TileSize);

            if (Engine.GetMouseButtonDown(MouseButton.Left))
            {
                EditMode = EditMode.DrawWall;
                SelectedWallTile = paletteHovered;
            }
        }
        else if (EditMode == EditMode.DrawWall)
        {
            if (Engine.GetMouseButtonDown(MouseButton.Left))
            {
                EditMode = EditMode.SetWall;
            }
            else if (Engine.GetMouseButtonDown(MouseButton.Right))
            {
                EditMode = EditMode.None;
            }
        }
        else if (EditMode == EditMode.SetWall)
        {
            if (Engine.GetMouseButtonHeld(MouseButton.Left))
            {
                int i = hovered.Column;
                if (i >= 0 && i < Walls.Length) Walls[i] = SelectedWallTile;
            }

            if (Engine.GetMouseButtonUp(MouseButton.Left))
            {
                EditMode = EditMode.DrawWall;
            }
        }
    }

    static TileIndex GetHoveredCell(Vector2 point, TileTexture tiles)
    {
        return new TileIndex(
            (int)Math.Floor(point.X / tiles.TileSize.X / WindowScale),
            (int)Math.Floor(point.Y / tiles.TileSize.Y / WindowScale));
    }
}

enum EditMode
{
    None,
    SelectWall,
    DrawWall,
    SetWall,
}

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

    public static Vector2 operator *(TileIndex t, Vector2 v)
    {
        return new Vector2(t.Column * v.X, t.Row * v.Y);
    }
}
