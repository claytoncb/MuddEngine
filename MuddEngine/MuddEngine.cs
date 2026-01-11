using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Rectangle = Raylib_cs.Rectangle;

namespace MuddEngine.MuddEngine
{
    public abstract class MuddEngine
    {
        public float MAX_Z = 200000f;
        public float MAX_Y = 1000f;
        public float MAX_SPRITE_HEIGHT = 1000f;

        public Color BackgroundColor = Color.Black;
        public Keyboard Keyboard = new Keyboard();
        private Stopwatch Stopwatch = new();
        public Vector2 ScreenSize;

        public static CameraSprite Camera;
        protected List<LightSource> Lights = new();
        private static List<Sprite2D> AllSprites = new();

        public Compositer Compositer;
        public BufferHandler BufferHandler;

        public MuddEngine(string title, Vector2 screenSize)
        {
            ScreenSize = screenSize;

            Raylib.InitWindow((int)screenSize.X, (int)screenSize.Y, title);
            Raylib.SetTargetFPS(60);

            BufferHandler = new BufferHandler(screenSize);
            Compositer = new Compositer();

            OnLoad();
            BufferHandler.OnLoad(Camera);

            GameLoop();

            // Cleanup
            Raylib.UnloadShader(Compositer.Shader);
            BufferHandler.Unload();
            Raylib.CloseWindow();
        }

        // ---------------------------------------------------------
        // Sprite registration
        // ---------------------------------------------------------
        public void AddLight(LightSource light) => Lights.Add(light);
        public void RemoveLight(LightSource light) => Lights.Remove(light);

        public static void RegisterSprite(Sprite2D sprite) => AllSprites.Add(sprite);
        public static void UnregisterSprite(Sprite2D sprite) => AllSprites.Remove(sprite);

        // ---------------------------------------------------------
        // Unified single-threaded game loop
        // ---------------------------------------------------------
        private void GameLoop()
        {
            Stopwatch.Start();
            int view = 4;

            while (!Raylib.WindowShouldClose())
            {
                // Compute delta time
                float dt = (float)Stopwatch.Elapsed.TotalSeconds;
                Stopwatch.Restart();

                // -------------------------------------------------
                // UPDATE
                // -------------------------------------------------
                foreach (var sprite in AllSprites)
                    sprite.Update(dt, Keyboard);

                OnUpdate(dt);
                Camera.Update(dt);

                // -------------------------------------------------
                // SORT SPRITES
                // -------------------------------------------------
                var spritesByDepth = AllSprites
                    .OrderBy(s => s.Position.Z)
                    .ThenBy(s => -s.Position.Y)
                    .ToList();

                // -------------------------------------------------
                // WRITE G-BUFFERS
                // -------------------------------------------------
                BufferHandler.WriteBuffers(spritesByDepth, MAX_Y, MAX_Z, MAX_SPRITE_HEIGHT);

                // -------------------------------------------------
                // DRAW
                // -------------------------------------------------
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                Rectangle src = new Rectangle(0, ScreenSize.Y, ScreenSize.X, -ScreenSize.Y);
                Vector2 dest = new Vector2(0, 0);

                // Debug view switching
                if (Raylib.IsKeyPressed(KeyboardKey.One)) view = 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Two)) view = 2;
                if (Raylib.IsKeyPressed(KeyboardKey.Three)) view = 3;
                if (Raylib.IsKeyPressed(KeyboardKey.Four)) view = 4;
                if (Raylib.IsKeyPressed(KeyboardKey.Five)) view = 5;

                switch (view)
                {
                    case 1:
                        BufferHandler.DrawBase();
                        break;
                    case 2:
                        BufferHandler.DrawNormals();
                        break;
                    case 3:
                        BufferHandler.DrawDepth();
                        break;
                    default:
                        Compositer.Draw(
                            BufferHandler.BaseBuffer,
                            BufferHandler.NormalBuffer,
                            BufferHandler.DepthBuffer,
                            ScreenSize,
                            MAX_Y,
                            MAX_Z,
                            MAX_SPRITE_HEIGHT,
                            Lights,
                            Camera
                        );
                        Raylib.DrawText("View: Composite (F4)", 10, 10, 20, Color.White);
                        break;
                }

                Raylib.EndDrawing();
            }
        }

        // ---------------------------------------------------------
        // User overrides
        // ---------------------------------------------------------
        public abstract void OnLoad();
        public abstract void OnUpdate(float dt);
        public abstract void OnDraw();
    }
}