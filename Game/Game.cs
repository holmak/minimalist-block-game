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
    public static readonly int EndMusicFrame = 18;

    // Assets loaded from files:
    public static Texture Block;
}

class Game
{
    public static readonly string Title = "Minimalist Block Game";
    public static readonly Vector2 Resolution = new Vector2(1280, 768);

    public static readonly bool Debug = true;
    public static readonly float BlockSpeed = 200;
    public static readonly float Gravity = 100;

    Vector2 Position;

    public Game()
    {
        if (Debug)
        {
            Engine.SetWindowDisplay(1);
        }

        Assets.Block = Engine.LoadTexture("block.png");

        Position = new Vector2((Resolution.X - Assets.Block.Width) / 2, -Assets.Block.Height);
    }

    public void Update()
    {
        bool fall = false;

        if (Engine.GetKeyHeld(Key.Left))
        {
            Position.X -= BlockSpeed * Engine.TimeDelta;
        }
        
        if (Engine.GetKeyHeld(Key.Right))
        {
            Position.X += BlockSpeed * Engine.TimeDelta;
        }

        if (Engine.GetKeyHeld(Key.Down))
        {
            fall = true;
        }

        // Keep the block on screen:
        Position.X = Math.Max(Position.X, 0);
        Position.X = Math.Min(Position.X, Resolution.X - Assets.Block.Width);

        Position.Y += (fall ? 3 : 1) * Gravity * Engine.TimeDelta;

        if (Position.Y > Resolution.Y)
        {
            Position.Y = -Assets.Block.Height;
        }

        Engine.DrawTexture(Assets.Block, Position, Color.Red);
    }
}
