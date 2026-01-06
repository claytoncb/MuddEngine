using System.Diagnostics;
using System.Numerics;
using Raylib_cs;


namespace MuddEngine.MuddEngine
{
    public abstract class MuddEngine
    {
        private Thread UpdateThread;
        private bool Running = true;
        public Raylib_cs.Color BackgroundColor = Raylib_cs.Color.Black;
        public Keyboard Keyboard = new Keyboard();
        private Stopwatch Stopwatch = new();
        public static CameraSprite Camera;
        public static LightingRenderer Lighting;
        protected List<LightSource> Lights = new();
        public static RenderTexture2D LightBuffer;
        public static RenderTexture2D BaseBuffer;
        public static Shader CompositeShader;
        private static List<Sprite2D> AllSprites = new();

        public MuddEngine(string Title, Vector2 ScreenSize)
        {
            Raylib.InitWindow((int)ScreenSize.X, (int)ScreenSize.Y, Title);
            Raylib.SetTargetFPS(60);

            // --- Create render buffers ---
            LightBuffer = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
            BaseBuffer  = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);

            // --- Load composite shader ---
            CompositeShader = Raylib.LoadShader(null, "Assets/Shaders/composite.fs");

            OnLoad();

            UpdateThread = new Thread(UpdateLoop);
            UpdateThread.Start();
            DrawLoop();

            Running = false;
            UpdateThread.Join();
            Raylib.CloseWindow();
        }
        public void AddLight(LightSource light) => Lights.Add(light);
        public void RemoveLight(LightSource light) => Lights.Remove(light);

        public static void RegisterSprite(Sprite2D sprite)
        {
            AllSprites.Add(sprite);
        }
        public static void UnregisterSprite(Sprite2D sprite)
        {
            AllSprites.Remove(sprite);
        }
        private void UpdateLoop()
        {
            Stopwatch.Start();

            while (Running)
            {
                float dt = (float)Stopwatch.Elapsed.TotalSeconds;
                Stopwatch.Restart();

                foreach (var sprite in AllSprites)
                    sprite.Update(dt, Keyboard);

                OnUpdate(dt);
                Camera.Update(dt);

                Thread.Sleep(1);
            }

        }

        private void DrawLoop()
        {
            while (!Raylib.WindowShouldClose())
            {
                // PASS 1 — LIGHT BUFFER
                Raylib.BeginTextureMode(LightBuffer);
                Raylib.ClearBackground(Raylib_cs.Color.Black);

                if (Camera != null)
                    Raylib.BeginMode2D(Camera.Camera);

                if (Lighting != null && Lights.Count > 0)
                {
                    foreach (var light in Lights)
                    {
                        Lighting.BeginLight(light);

                        foreach (var sprite in AllSprites)
                            sprite.DrawLight();

                        Lighting.EndLight();
                    }
                }

                if (Camera != null)
                    Raylib.EndMode2D();

                Raylib.EndTextureMode();

                // PASS 2 — BASE BUFFER
                Raylib.BeginTextureMode(BaseBuffer);
                Raylib.ClearBackground(Raylib_cs.Color.Black);

                if (Camera != null)
                    Raylib.BeginMode2D(Camera.Camera);

                OnDraw();

                foreach (var sprite in AllSprites)
                    sprite.DrawBase();

                if (Camera != null)
                    Raylib.EndMode2D();

                Raylib.EndTextureMode();

                // PASS 3 — COMPOSITE
                Raylib.BeginDrawing();
                Raylib.ClearBackground(BackgroundColor);

                Raylib.BeginShaderMode(CompositeShader);

                Raylib.SetShaderValueTexture(
                    CompositeShader,
                    Raylib.GetShaderLocation(CompositeShader, "baseTexture"),
                    BaseBuffer.Texture
                );

                Raylib.SetShaderValueTexture(
                    CompositeShader,
                    Raylib.GetShaderLocation(CompositeShader, "lightTexture"),
                    LightBuffer.Texture
                );

                Raylib.DrawTextureRec(
                    BaseBuffer.Texture,
                    new Raylib_cs.Rectangle(0, 0, BaseBuffer.Texture.Width, -BaseBuffer.Texture.Height),
                    new Vector2(0, 0),
                    Raylib_cs.Color.White
                );

                Raylib.EndShaderMode();

                // PASS 4 — ICONS
                if (Camera != null)
                    Raylib.BeginMode2D(Camera.Camera);

                foreach (var light in Lights)
                    light.DrawBase();

                if (Camera != null)
                    Raylib.EndMode2D();

                Raylib.EndDrawing();
            }
        }

        public abstract void OnLoad();
        public abstract void OnUpdate(float dt);
        public abstract void OnDraw();
    }
}