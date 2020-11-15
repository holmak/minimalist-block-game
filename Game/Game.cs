using System;
using System.Linq;
using System.Collections.Generic;

class Game
{
    public static readonly string Title = "Minimalist RPG Demo";
    public static readonly int WindowScale = 4;
    public static readonly Vector2 Resolution = WindowScale * new Vector2(320, 240);
    public static readonly bool Debug = true;

    static readonly int AnimationPeriod = 15;

    TileTexture WallTiles = new TileTexture("wall_tiles.png", 16);
    TileTexture PropTiles = new TileTexture("prop_tiles.png", 16);
    TileTexture UITiles = new TileTexture("ui_tiles.png", 16);
    TileTexture FontTiles = new TileTexture("font_tiles.png", 8);

    int AnimationTimer = 0;
    int MapWidth, MapHeight;
    TileIndex[,] Walls;
    List<Creature> Creatures = new List<Creature>();
    Creature Player => Creatures[0];

    public Game()
    {
        if (Debug)
        {
            Engine.SetWindowDisplay(1);
        }

        LoadMap();
    }

    void LoadMap()
    {
        Random random = new Random(100);

        TileIndex[] floorTiles = Enumerable.Range(4, 12).Select(i => new TileIndex(i, 0)).ToArray();
        TileIndex[] topWallTiles = Enumerable.Range(1, 3).Select(i => new TileIndex(i, 1)).ToArray();
        TileIndex[] sideWallTiles = Enumerable.Range(2, 3).Select(i => new TileIndex(0, i)).ToArray();

        // Create the player:
        Creatures.Add(new Creature
        {
            Appearance = MakeTileSpan(new TileIndex(0, 8), new TileIndex(1, 0), 4),
        });

        // Parse map data:
        string[] raw = RawMapData.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        MapWidth = raw[0].Length;
        MapHeight = raw.Length;
        Walls = new TileIndex[MapWidth, MapHeight];
        for (int row = 0; row < MapHeight; row++)
        {
            for (int column = 0; column < MapWidth; column++)
            {
                char c = raw[row][column];
                char left = (column > 0) ? raw[row][column - 1] : '.';
                char right = (column < MapWidth - 1) ? raw[row][column + 1] : '.';
                char above = (row > 0) ? raw[row - 1][column] : '.';
                char below = (row < MapHeight - 1) ? raw[row + 1][column] : '.';

                TileIndex tile;
                if (c == '.') tile = new TileIndex(0, 0);
                else if (c == 'W')
                {
                    if (above == 'W')
                    {
                        tile = Choose(random, sideWallTiles);
                    }
                    else
                    {
                        tile = Choose(random, topWallTiles);
                    }
                }
                else
                {
                    tile = Choose(random, floorTiles);
                }
                Walls[column, row] = tile;

                Vector2 here = new Vector2(column, row) * WallTiles.TileSize;

                // Creatures always stand on floor tiles.
                if (c == '@')
                {
                    Player.Position = here;
                }
                else if (c == 'P')
                {
                    // Priest NPC:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(0, 9), new TileIndex(1, 0), 4)
                    });
                }
                else if (c == 'B')
                {
                    // Skeleton:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(0, 5), new TileIndex(1, 0), 4)
                    });
                }
                else if (c == 'L')
                {
                    // Ladder:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(3, 0)),
                    });
                }
                else if (c == 'T')
                {
                    // Treasure:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(4, 3)),
                    });
                }
            }
        }
    }

    static T Choose<T>(Random random, IList<T> list)
    {
        return list[random.Next(list.Count)];
    }

    public void Update()
    {
        AnimationTimer += 1;
        bool advanceFrame = false;
        if (AnimationTimer >= AnimationPeriod)
        {
            AnimationTimer -= AnimationPeriod;
            advanceFrame = true;
        }

        Vector2 input = Vector2.Zero;
        if (Engine.GetKeyHeld(Key.A)) input.X -= 1;
        if (Engine.GetKeyHeld(Key.D)) input.X += 1;
        if (Engine.GetKeyHeld(Key.W)) input.Y -= 1;
        if (Engine.GetKeyHeld(Key.S)) input.Y += 1;
        Player.Movement = input;

        for (int row = 0; row < MapHeight; row++)
        {
            for (int column = 0; column < MapWidth; column++)
            {
                TileEngine.DrawTile(WallTiles, Walls[column, row], new Vector2(column, row) * WallTiles.TileSize);
            }
        }

        // Apply input and physics:
        foreach (Creature creature in Creatures)
        {
            creature.Velocity += creature.Movement * Creature.MaxAcceleration * Engine.TimeDelta;
            creature.Velocity.X = Clamp(creature.Velocity.X, -Creature.MaxVelocity, Creature.MaxVelocity);
            creature.Velocity.Y = Clamp(creature.Velocity.Y, -Creature.MaxVelocity, Creature.MaxVelocity);
            creature.Position += creature.Velocity * Engine.TimeDelta;

            // Slow to a stop when there is no input -- separately on each axis:
            if (creature.Movement.X == 0)
            {
                float speed = Math.Abs(creature.Velocity.X);
                speed = Math.Max(0, speed - Creature.Deceleration);
                creature.Velocity.X = Math.Sign(creature.Velocity.X) * speed;
            }

            if (creature.Movement.Y == 0)
            {
                float speed = Math.Abs(creature.Velocity.Y);
                speed = Math.Max(0, speed - Creature.Deceleration);
                creature.Velocity.Y = Math.Sign(creature.Velocity.Y) * speed;
            }
        }

        // Draw back-to-front:
        foreach (Creature creature in Creatures.OrderBy(x => x.Position.Y))
        {
            TileEngine.DrawTile(PropTiles, creature.Appearance[creature.Frame], creature.Position);

            if (advanceFrame)
            {
                creature.Frame = (creature.Frame + 1) % creature.Appearance.Length;
            }
        }

        TileEngine.DrawTileString(FontTiles, "This is text.", new Vector2(0, 0));
    }

    static TileIndex GetHoveredCell(Vector2 point, TileTexture tiles)
    {
        return new TileIndex(
            (int)Math.Floor(point.X / tiles.TileSize.X / WindowScale),
            (int)Math.Floor(point.Y / tiles.TileSize.Y / WindowScale));
    }

    static TileIndex[] MakeTileSpan(TileIndex first, TileIndex step, int count)
    {
        TileIndex[] span = new TileIndex[count];
        for (int i = 0; i < count; i++)
        {
            span[i] = first;
            first += step;
        }
        return span;
    }

    static TileIndex[] MakeTileSpan(TileIndex only)
    {
        return new TileIndex[] { only };
    }

    static float Clamp(float x, float min, float max)
    {
        if (x < min) return min;
        if (x > max) return max;
        return x;
    }

    static readonly string RawMapData = @"
        ......................................
        ......................................
        ......................................
        ......................................
        ....WWWWWWWWWW...WWWWW................
        ....W---B----WWWWW---W................
        ....WL@---P--------T-W................
        ....W--------WWWWW---W................
        ....WWWWWWWWWW...WW-WW................
        ..................W-W..WWWWWWWW.......
        ..................W-WWWW------WWWW....
        ..................W---B----------D....
        ..................WWWWWW----B----D....
        .......................W------WWWW....
        .......................WWWWWWWW.......
        ......................................
        ......................................
        ......................................
        ......................................
        ";
}

class Creature
{
    public static readonly float MaxVelocity = 150;
    public static readonly float MaxAcceleration = MaxVelocity * 10;
    public static readonly float Deceleration = MaxVelocity * 15;

    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Movement;
    public TileIndex[] Appearance;
    public int Frame = 0;
}
