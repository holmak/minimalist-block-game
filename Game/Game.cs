using System;
using System.Collections.Generic;

class Game
{
    public static readonly string Title = "Minimalist Game Framework";
    public static readonly Vector2 Resolution = new Vector2(1024, 768);
    public static readonly bool Debug = true;

    Texture DungeonTiles = Engine.LoadTexture("dungeon_tiles.png");
    Texture FontTiles = Engine.LoadTexture("dungeon_tiles.png");

    public Game()
    {
        if (Debug)
        {
            Engine.SetWindowDisplay(1);
        }
    }

    public void Update()
    {
        Engine.DrawTexture(DungeonTiles, Vector2.Zero, scaleMode: TextureScaleMode.Nearest, size: 4 * DungeonTiles.Size);
    }
}
