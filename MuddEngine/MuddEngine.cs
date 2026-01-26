using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Rectangle = Raylib_cs.Rectangle;

namespace MuddEngine.MuddEngine
{
    public abstract class MuddEngine
    {
        public Color BackgroundColor = Color.Black;
        public Keyboard Keyboard = new Keyboard();
        private Stopwatch Stopwatch = new();
        private Stopwatch LoopStopwatch = new();
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
            Compositer    = new Compositer();

            OnLoad();
            BufferHandler.OnLoad(Camera);
            Compositer.OnLoad(Camera);
            Stopwatch.Start();
            GameLoop();

            Raylib.UnloadShader(Compositer.Shader);
            BufferHandler.Unload();
            Raylib.CloseWindow();
        }

        public void AddLight(LightSource light) => Lights.Add(light);
        public void RemoveLight(LightSource light) => Lights.Remove(light);

        public static void RegisterSprite(Sprite2D sprite) => AllSprites.Add(sprite);
        public static void UnregisterSprite(Sprite2D sprite) => AllSprites.Remove(sprite);

        private void GameLoop()
        {
            LoopStopwatch.Start();
            int view = 5;

            while (!Raylib.WindowShouldClose())
            {
                float dt = (float)LoopStopwatch.Elapsed.TotalSeconds;
                float t = (float)Stopwatch.Elapsed.TotalSeconds;
                LoopStopwatch.Restart();

                foreach (var sprite in AllSprites)
                    sprite.Update(dt, t, Keyboard);

                OnUpdate(dt, t);
                Camera.Update(dt, t);

                var spritesByDepth = AllSprites
                    .OrderBy(s => s.Position.Z)
                    .ThenBy(s => -s.Position.Y)
                    .ToList();

                BufferHandler.WriteBuffers(spritesByDepth);

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                if (Raylib.IsKeyPressed(KeyboardKey.One))  view = 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Two))  view = 2;
                if (Raylib.IsKeyPressed(KeyboardKey.Three))view = 3;
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
                    case 4:
                        BufferHandler.DrawMeta();
                        break;
                    default:
                        Compositer.Draw(
                            BufferHandler.BaseBuffer,
                            BufferHandler.NormalBuffer,
                            BufferHandler.DepthBuffer,
                            BufferHandler.MetaBuffer,
                            ScreenSize,
                            Lights,
                            spritesByDepth
                        );
                        Raylib.DrawText("View: Composite (F5)", 10, 10, 20, Color.White);
                        break;
                }

                Raylib.EndDrawing();
            }
        }

        public abstract void OnLoad();
        public abstract void OnUpdate(float dt, float t);
        public abstract void OnDraw();
    }
}