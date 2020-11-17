using System;
using System.Linq;
using System.Collections.Generic;

static class Assets
{
    // Constants:
    public static readonly int AnimationPeriod = 15;
    public static readonly Color WinningTextColor = Color.Black;
    public static readonly Color LosingTextColor = new Color(255, 0, 0);
    public static Vector2 CellSize;

    // Assets loaded from files:
    public static readonly int TileAssetScale = 4;
    public static TileTexture WallTiles;
    public static TileTexture PropTiles;
    public static TileTexture UITiles;
    public static TileTexture FontTiles;
}

class Game
{
    public static readonly string Title = "Minimalist RPG Demo";
    public static readonly Vector2 Resolution = new Vector2(1280, 768);

    public static readonly bool Debug = true;
    public static readonly bool DebugCollision = false;

    int AnimationTimer = 0;
    int MapWidth, MapHeight;
    TileIndex[,] Walls;
    bool[,] Obstacles;
    Vector2 Origin = Vector2.Zero;
    List<Creature> Creatures = new List<Creature>();
    Creature Player => Creatures[0];
    GameState State = GameState.Playing;
    int EndGameFrame = 0;
    string[] Conversation = new string[0];
    int ConversationPage = 0;
    bool InConversation => ConversationPage < Conversation.Length;

    public Game()
    {
        if (Debug)
        {
            Engine.SetWindowDisplay(1);
        }

        Assets.WallTiles = new TileTexture("wall_tiles.png", 16, Assets.TileAssetScale);
        Assets.PropTiles = new TileTexture("prop_tiles.png", 16, Assets.TileAssetScale);
        Assets.UITiles = new TileTexture("ui_tiles.png", 16, Assets.TileAssetScale);
        Assets.FontTiles = new TileTexture("font_tiles.png", 8, Assets.TileAssetScale);
        Assets.CellSize = Assets.WallTiles.DestinationSize;

        LoadMap();
    }

    void LoadMap()
    {
        Random random = new Random(100);

        // Create the player:
        Creatures.Add(new Creature
        {
            Speed = 240,
            Appearance = MakeTileSpan(new TileIndex(0, 8), new TileIndex(1, 0), 4),
        });

        // Parse map data:
        string[] raw = RawMapData.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        MapWidth = raw[0].Length;
        MapHeight = raw.Length;
        Walls = new TileIndex[MapWidth, MapHeight];
        Obstacles = new bool[MapWidth, MapHeight];
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
                bool obstacle = false;
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

                    obstacle = true;
                }
                else
                {
                    tiles = MakeTileSpan(new TileIndex(4, 0), new TileIndex(1, 0), 12);
                }
                Walls[column, row] = Choose(random, tiles);

                Vector2 here = new Vector2(column, row) * Assets.CellSize;

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
                        Appearance = MakeTileSpan(new TileIndex(0, 9), new TileIndex(1, 0), 4),
                        Conversation = new[]
                        {
                            "Expedition Leader\n\n\"Phew!\"",
                            "Expedition Leader\n\n\"We nearly didn't make it\n out of that level!\"",
                            "Expedition Leader\n\n\"Now, quickly, the exit is\n just ahead.\"",
                            "Expedition Leader\n\n\"Go!\"",
                        },
                    });
                }
                else if (c == 'S')
                {
                    // Skeleton:
                    Creatures.Add(new Creature
                    {
                        Speed = 40,
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(0, 5), new TileIndex(1, 0), 4),
                        CanKill = true,
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
                    obstacle = true;
                }
                else if (c == 'B')
                {
                    // Big crate:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(3, 3)),
                    });
                    obstacle = true;
                }
                else if (c == 'b')
                {
                    // Small crate:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(2, 3)),
                    });
                    obstacle = true;
                }
                else if (c == 'f')
                {
                    // Wall torch:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(3, 4)),
                    });
                }
                else if (c == 'C')
                {
                    // Large cobweb:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(4, 1)),
                    });
                }
                else if (c == 'c')
                {
                    // Small cobweb:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(5, 1)),
                    });
                }
                else if (c == 'r')
                {
                    // Bones:
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(7, 2)),
                        IsFlat = true,
                    });
                }
                else if (c == 'D')
                {
                    // Door (left):
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(0, 0)),
                        CanWin = true,
                    });
                    obstacle = true;
                }
                else if (c == 'O')
                {
                    // Door (right):
                    Creatures.Add(new Creature
                    {
                        Position = here,
                        Appearance = MakeTileSpan(new TileIndex(1, 0)),
                        CanWin = true,
                    });
                    obstacle = true;
                }

                Obstacles[column, row] = obstacle;
            }
        }
    }

    static T Choose<T>(Random random, IList<T> list)
    {
        return list[random.Next(list.Count)];
    }

    public void Update()
    {
        List<Action> diagnostics = new List<Action>();

        AnimationTimer += 1;
        bool advanceFrame = false;
        if (AnimationTimer >= Assets.AnimationPeriod)
        {
            AnimationTimer -= Assets.AnimationPeriod;
            advanceFrame = true;
        }

        string[] availableConversation = new string[0];
        foreach (Creature creature in Creatures)
        {
            if (creature != Player)
            {
                float distance = (creature.Position - Player.Position).Length();

                if (creature.Conversation.Length > 0 && distance < 96)
                {
                    availableConversation = creature.Conversation;
                }

                if (creature.CanKill && State == GameState.Playing && distance < 64)
                {
                    State = GameState.Losing;
                }

                if (creature.CanWin && State == GameState.Playing && distance < 48)
                {
                    State = GameState.Winning;
                }
            }
        }

        if (availableConversation.Length == 0)
        {
            // Cancel the conversation:
            Conversation = availableConversation;
        }
        else if (!InConversation && Engine.GetKeyDown(Key.J))
        {
            // Begin a conversation:
            Conversation = availableConversation;
            ConversationPage = 0;
        }
        else if (InConversation && Engine.GetKeyDown(Key.J))
        {
            // Continue a conversation:
            ConversationPage += 1;
        }

        Vector2 input = Vector2.Zero;
        if (Engine.GetKeyHeld(Key.A)) input.X -= 1;
        if (Engine.GetKeyHeld(Key.D)) input.X += 1;
        if (Engine.GetKeyHeld(Key.W)) input.Y -= 1;
        if (Engine.GetKeyHeld(Key.S)) input.Y += 1;
        Player.Movement = input;

        // AI:
        foreach (Creature creature in Creatures)
        {
            if (creature.CanKill)
            {
                Vector2 vectorToPlayer = Player.Position - creature.Position;
                float distanceToPlayer = vectorToPlayer.Length();
                if (distanceToPlayer < 300)
                {
                    creature.Movement = vectorToPlayer.Normalized();
                }
                else
                {
                    creature.Movement = Vector2.Zero;
                }
            }
        }

        // Apply input and physics:
        foreach (Creature creature in Creatures)
        {
            if (State != GameState.Playing)
            {
                creature.Movement = Vector2.Zero;
                creature.Velocity = Vector2.Zero;
            }

            creature.Velocity += creature.Movement * creature.MaxAcceleration * Engine.TimeDelta;
            creature.Velocity.X = Clamp(creature.Velocity.X, -creature.Speed, creature.Speed);
            creature.Velocity.Y = Clamp(creature.Velocity.Y, -creature.Speed, creature.Speed);

            // Update position and collide:
            {
                // These bounds describe the "solid" part of the entity; it is independent of position.
                Bounds2 creatureShape = new Bounds2(
                    new Vector2(1 / 8f, 6 / 8f) * Assets.CellSize,
                    new Vector2(6 / 8f, 2 / 8f) * Assets.CellSize);

                Vector2 motion = creature.Velocity * Engine.TimeDelta;
                TileIndex nearest = GetCellAt(creature.Position, Assets.WallTiles);

                // Find everything that could be collided with:
                List<Bounds2> obstacles = new List<Bounds2>();
                for (int row = nearest.Row - 2; row <= nearest.Row + 2; row++)
                {
                    for (int column = nearest.Column - 2; column <= nearest.Column + 2; column++)
                    {
                        if (Obstacles[column, row])
                        {
                            Bounds2 obstacleBounds = new Bounds2(
                                new Vector2(column, row) * Assets.CellSize,
                                Assets.CellSize);

                            // The effective bounds are the sum of the obstacle's and the mover's bounds:
                            Vector2 min = obstacleBounds.Min - creatureShape.Max;
                            Vector2 max = obstacleBounds.Max - creatureShape.Min;
                            Bounds2 totalBounds = new Bounds2(min, max - min);
                            obstacles.Add(totalBounds);

                            if (DebugCollision && creature == Player)
                            {
                                diagnostics.Add(() => Engine.DrawRectEmpty(obstacleBounds.Translated(Origin), Color.Green));
                            }
                        }
                    }
                }

                // Calculate collision along the X and Y axes independently.
                // Each calculation considers only motion along that axis.
                // Performing these steps sequentially ensures that objects can't move diagonally through other objects.

                // X step:
                Vector2 position = creature.Position;
                Vector2 newPosition = position + new Vector2(motion.X, 0);
                foreach (Bounds2 bounds in obstacles)
                {
                    // Leftward:
                    if (motion.X < 0 &&
                        position.Y > bounds.Position.Y &&
                        position.Y < bounds.Position.Y + bounds.Size.Y)
                    {
                        float limit = bounds.Position.X + bounds.Size.X;
                        if (position.X >= limit && newPosition.X < limit) newPosition.X = limit;
                    }

                    // Rightward:
                    if (motion.X > 0 &&
                        position.Y > bounds.Position.Y &&
                        position.Y < bounds.Position.Y + bounds.Size.Y)
                    {
                        float limit = bounds.Position.X;
                        if (position.X <= limit && newPosition.X > limit) newPosition.X = limit;
                    }
                }
                creature.Position.X = newPosition.X;

                // Y step:
                position = creature.Position;
                newPosition = position + new Vector2(0, motion.Y);
                foreach (Bounds2 bounds in obstacles)
                {
                    // Upward:
                    if (motion.Y < 0 &&
                        position.X > bounds.Position.X &&
                        position.X < bounds.Position.X + bounds.Size.X)
                    {
                        float limit = bounds.Position.Y + bounds.Size.Y;
                        if (position.Y >= limit && newPosition.Y < limit) newPosition.Y = limit;
                    }

                    // Downward:
                    if (motion.Y > 0 &&
                        position.X > bounds.Position.X &&
                        position.X < bounds.Position.X + bounds.Size.X)
                    {
                        float limit = bounds.Position.Y;
                        if (position.Y <= limit && newPosition.Y > limit) newPosition.Y = limit;
                    }
                }
                creature.Position.Y = newPosition.Y;

                if (DebugCollision && creature == Player)
                {
                    diagnostics.Add(() =>
                    {
                        Engine.DrawRectEmpty(creatureShape.Translated(Origin + creature.Position), Color.Green);
                    });
                }
            }

            // Slow to a stop when there is no input -- separately on each axis:
            if (creature.Movement.X == 0)
            {
                float speed = Math.Abs(creature.Velocity.X);
                speed = Math.Max(0, speed - creature.Deceleration);
                creature.Velocity.X = Math.Sign(creature.Velocity.X) * speed;
            }

            if (creature.Movement.Y == 0)
            {
                float speed = Math.Abs(creature.Velocity.Y);
                speed = Math.Max(0, speed - creature.Deceleration);
                creature.Velocity.Y = Math.Sign(creature.Velocity.Y) * speed;
            }
        }

        // Scroll to keep the player onscreen:
        {
            Vector2 margin = new Vector2(300, 250);
            Origin.X = Clamp(Origin.X,
                -Player.Position.X + margin.X,
                -(Player.Position.X + Assets.PropTiles.DestinationSize.X) + Resolution.X - margin.X);
            Origin.Y = Clamp(Origin.Y,
                -Player.Position.Y + margin.Y,
                -(Player.Position.Y + Assets.PropTiles.DestinationSize.Y) + Resolution.Y - margin.Y);
        }

        // Draw the static part of the map:
        for (int row = 0; row < MapHeight; row++)
        {
            for (int column = 0; column < MapWidth; column++)
            {
                TileEngine.DrawTile(Assets.WallTiles, Walls[column, row], Origin + new Vector2(column, row) * Assets.CellSize);
            }
        }

        // Draw back-to-front:
        foreach (Creature creature in Creatures.OrderBy(x => x.IsFlat ? 0 : 1).ThenBy(x => x.Position.Y))
        {
            TileEngine.DrawTile(Assets.PropTiles, creature.Appearance[creature.Frame], Origin + creature.Position);

            if (advanceFrame)
            {
                creature.Frame = (creature.Frame + 1) % creature.Appearance.Length;
            }
        }

        // Draw UI:
        if (InConversation)
        {
            string speech = Conversation[ConversationPage];
            int width = 16;
            int height = 3;
            Vector2 pos = new TileIndex((20 - width) / 2, 12 - height) * Assets.CellSize;
            DrawBorder(pos, width, height);
            pos += 0.5f * Assets.CellSize;
            TileEngine.DrawTileString(Assets.FontTiles, speech, pos);
        }
        else if (availableConversation.Length > 0)
        {
            string text = "\x18 Converse";
            Vector2 pos = new Vector2((20 - (text.Length + 1) / 2) / 2, 11) * Assets.CellSize;
            TileEngine.DrawTileString(Assets.FontTiles, text, pos);
        }

        if (State == GameState.Losing)
        {
            if (advanceFrame) EndGameFrame += 1;
            Color background = Color.Black.WithAlpha(EndGameFrame / 5f);
            Engine.DrawRectSolid(new Bounds2(Vector2.Zero, Resolution), background);

            string text = "You died";
            float textAlpha = Clamp((EndGameFrame - 8) / 5f, 0, 1);
            Color textColor = Assets.LosingTextColor.WithAlpha(textAlpha);
            Vector2 pos = new Vector2((20 - (text.Length + 1) / 2) / 2, 5) * Assets.CellSize;
            TileEngine.DrawTileString(Assets.FontTiles, text, pos, color: textColor);
        }
        else if (State == GameState.Winning)
        {
            if (advanceFrame) EndGameFrame += 1;
            Color background = Color.White.WithAlpha(EndGameFrame / 5f);
            Engine.DrawRectSolid(new Bounds2(Vector2.Zero, Resolution), background);

            string text = "You escaped!";
            float textAlpha = Clamp((EndGameFrame - 8) / 5f, 0, 1);
            Color textColor = Assets.WinningTextColor.WithAlpha(textAlpha);
            Vector2 pos = new Vector2((20 - (text.Length + 1) / 2) / 2, 5) * Assets.CellSize;
            TileEngine.DrawTileString(Assets.FontTiles, text, pos, color: textColor);
        }

        if (EndGameFrame > 20)
        {
            string text = "\x1A New game";
            Vector2 pos = new Vector2((20 - (text.Length + 1) / 2) / 2, 11) * Assets.CellSize;
            Color color = (State == GameState.Winning) ? Assets.WinningTextColor : Assets.LosingTextColor;
            TileEngine.DrawTileString(Assets.FontTiles, text, pos, color: color);
        }

        // Draw debug information:
        if (Debug)
        {
            foreach (Action action in diagnostics)
            {
                action();
            }
        }
    }

    static TileIndex GetCellAt(Vector2 point, TileTexture tiles)
    {
        return new TileIndex(
            (int)Math.Floor(point.X / tiles.DestinationSize.X),
            (int)Math.Floor(point.Y / tiles.DestinationSize.Y));
    }

    void DrawBorder(Vector2 position, int width, int height)
    {
        TileIndex borderTiles = new TileIndex(0, 1);
        for (int j = 0; j < height; j++)
        {
            int row;
            if (j == 0) row = 0;
            else if (j == height - 1) row = 2;
            else row = 1;

            for (int i = 0; i < width; i++)
            {
                int column;
                if (i == 0) column = 0;
                else if (i == width - 1) column = 2;
                else column = 1;

                TileIndex tile = borderTiles + new TileIndex(column, row);
                TileEngine.DrawTile(Assets.UITiles, tile, position + new Vector2(i, j) * Assets.CellSize);
            }
        }
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
        ........................WWWWDOWWWW....
        ........................Wf------cW....
        .................WWWWWWWW-----S--W....
        .................W-------r-------W....
        ....WWWWWWWWWW...W--WWWWW--S----BW....
        ....W---B---fWWWWW-BW...W-----Bb-W....
        ....WL@---P---------W...WWWWWWWWWW....
        ....WC------fWWWWW-rW.................
        ....WWWWWWWWWW...WB-W.................
        .................W--W.................
        .................W--W.................
        .................W--W.................
        .................WS-W.................
        .................W-SW.................
        .................W--W.................
        .................W--W.................
        .................W--W.................
        .................W--W.................
        .................W--W.................
        ......................................
        ......................................
        ";
}

class Creature
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Movement;
    public float Speed = 0;
    public float MaxAcceleration => Speed * 5;
    public float Deceleration => Speed * 6;
    public TileIndex[] Appearance;
    public bool IsFlat = false;
    public int Frame = 0;
    public string[] Conversation = new string[0];
    public bool CanWin = false;
    public bool CanKill = false;
}

enum GameState
{
    Playing,
    Winning,
    Losing,
}
