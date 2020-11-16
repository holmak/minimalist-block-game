using System;
using System.Linq;
using System.Collections.Generic;

class Game
{
    public static readonly string Title = "Minimalist RPG Demo";
    public static readonly Vector2 Resolution = new Vector2(1280, 768);
    public static readonly int AssetScale = 4;
    public static readonly bool Debug = true;

    static readonly int AnimationPeriod = 15;

    TileTexture WallTiles = new TileTexture("wall_tiles.png", 16, AssetScale);
    TileTexture PropTiles = new TileTexture("prop_tiles.png", 16, AssetScale);
    TileTexture UITiles = new TileTexture("ui_tiles.png", 16, AssetScale);
    TileTexture FontTiles = new TileTexture("font_tiles.png", 8, AssetScale);

    int AnimationTimer = 0;
    int MapWidth, MapHeight;
    TileIndex[,] Walls;
    Vector2 Origin = Vector2.Zero;
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

                TileIndex[] tiles;
                if (c == '.')
                {
                    tiles = MakeTileSpan(new TileIndex(0, 0));
                }
                else if (c == 'W')
                {
                    // Outer corners:
                    if (below == 'W' && right == 'W' && above == '.') tiles = MakeTileSpan(new TileIndex(0, 1));
                    else if (below == 'W' && left == 'W' && above == '.') tiles = MakeTileSpan(new TileIndex(5, 1));
                    else if (above == 'W' && right == 'W' && below == '.') tiles = MakeTileSpan(new TileIndex(0, 5));
                    else if (above == 'W' && left == 'W' && below == '.') tiles = MakeTileSpan(new TileIndex(5, 5));
                    // Inside corners:
                    else if (below == 'W' && right == 'W' && above != '.') tiles = MakeTileSpan(new TileIndex(8, 1));
                    else if (below == 'W' && left == 'W' && above != '.') tiles = MakeTileSpan(new TileIndex(11, 1));
                    // Horizontal walls:
                    else if (above == '.' || (above == 'W' && (left == 'W' || right == 'W'))) tiles = MakeTileSpan(new TileIndex(1, 1), new TileIndex(1, 0), 4);
                    else if (below == '.') tiles = MakeTileSpan(new TileIndex(1, 5), new TileIndex(1, 0), 4);
                    // Vertical walls:
                    else if (left == '.') tiles = MakeTileSpan(new TileIndex(0, 2), new TileIndex(0, 1), 3);
                    else if (right == '.') tiles = MakeTileSpan(new TileIndex(5, 2), new TileIndex(0, 1), 3);
                    else tiles = MakeTileSpan(new TileIndex(2, 0));
                }
                else
                {
                    tiles = MakeTileSpan(new TileIndex(4, 0), new TileIndex(1, 0), 12);
                }
                Walls[column, row] = Choose(random, tiles);

                Vector2 here = new Vector2(column, row) * WallTiles.DestinationSize;

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
                        IsFlat = true,
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

        // Scroll to keep the player onscreen:
        {
            Vector2 margin = new Vector2(300, 250);
            Origin.X = Clamp(Origin.X,
                -Player.Position.X + margin.X,
                -(Player.Position.X + PropTiles.DestinationSize.X) + Resolution.X - margin.X);
            Origin.Y = Clamp(Origin.Y,
                -Player.Position.Y + margin.Y,
                -(Player.Position.Y + PropTiles.DestinationSize.Y) + Resolution.Y - margin.Y);
        }

        // Draw the static part of the map:
        for (int row = 0; row < MapHeight; row++)
        {
            for (int column = 0; column < MapWidth; column++)
            {
                TileEngine.DrawTile(WallTiles, Walls[column, row], Origin + new Vector2(column, row) * WallTiles.DestinationSize);
            }
        }

        // Draw back-to-front:
        foreach (Creature creature in Creatures.OrderBy(x => x.IsFlat ? 0 : 1).ThenBy(x => x.Position.Y))
        {
            TileEngine.DrawTile(PropTiles, creature.Appearance[creature.Frame], Origin + creature.Position);

            if (advanceFrame)
            {
                creature.Frame = (creature.Frame + 1) % creature.Appearance.Length;
            }
        }
    }

    static TileIndex GetHoveredCell(Vector2 point, TileTexture tiles)
    {
        return new TileIndex(
            (int)Math.Floor(point.X / tiles.DestinationSize.X),
            (int)Math.Floor(point.Y / tiles.DestinationSize.Y));
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
        ....W--------WWWWW---W................
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
    public static readonly float MaxVelocity = 280;
    public static readonly float MaxAcceleration = MaxVelocity * 5;
    public static readonly float Deceleration = MaxVelocity * 6;

    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Movement;
    public TileIndex[] Appearance;
    public bool IsFlat = false;
    public int Frame = 0;
}
