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
    private Thread UpdateThread;
    private bool Running = true;
    public Color BackgroundColor = Color.Black;
    public Keyboard Keyboard = new Keyboard();
    private Stopwatch Stopwatch = new();
    public Vector2 ScreenSize;
    public static CameraSprite Camera;
    public static LightingRenderer Lighting;
    protected List<LightSource> Lights = new();

    public static RenderTexture2D BaseBuffer;
    public RenderTexture2D DepthBuffer;
    public RenderTexture2D NormalBuffer;
    public static Shader CompositeShader;
    public static Shader DepthShader;

    private static List<Sprite2D> AllSprites = new();
    int locUprightSprite;
    int locZNorm;
    int locYNorm;

    // Constructor
    public Compositer Compositer;

    public MuddEngine(string title, Vector2 screenSize)
    {
        ScreenSize = screenSize;

        Raylib.InitWindow((int)ScreenSize.X, (int)ScreenSize.Y, title);
        Raylib.SetTargetFPS(60);

        BaseBuffer   = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
        NormalBuffer = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
        DepthBuffer  = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);

        DepthShader = Raylib.LoadShader(
            "Assets/Shaders/vertexShader.vs",
            "Assets/Shaders/depth.fs"
        );
        locUprightSprite = Raylib.GetShaderLocation(DepthShader, "uprightSprite");
        locZNorm = Raylib.GetShaderLocation(DepthShader, "zNorm");
        locYNorm = Raylib.GetShaderLocation(DepthShader, "yNorm");

        // NEW compositer
        Compositer = new Compositer();

        OnLoad();

        UpdateThread = new Thread(UpdateLoop);
        UpdateThread.Start();
        DrawLoop();

        Running = false;
        UpdateThread.Join();

        // Cleanup
        Raylib.UnloadShader(DepthShader);
        Raylib.UnloadShader(Compositer.Shader);

        Raylib.UnloadRenderTexture(BaseBuffer);
        Raylib.UnloadRenderTexture(NormalBuffer);
        Raylib.UnloadRenderTexture(DepthBuffer);

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
        private void GBufferPass(List<Sprite2D> sprites)
        {
            BasePass(sprites);
            NormalsPass(sprites);
            DepthPass(sprites);
        }
        private void BasePass(List<Sprite2D> sprites)
        {
            // --- BASE (ALBEDO) ---
            Raylib.BeginTextureMode(BaseBuffer);
            Raylib.ClearBackground(Color.Blank); // transparent clear so alpha = 0 where nothing is drawn

            if (Camera != null)
                Raylib.BeginMode2D(Camera.Camera);

            // Ensure alpha blending is enabled while drawing sprites into the base buffer
            Raylib.BeginBlendMode(BlendMode.Alpha);
            foreach (var sprite in sprites)
                sprite.DrawBase(); // your sprite draw should use Color.White internally to preserve alpha
            Raylib.EndBlendMode();

            if (Camera != null)
                Raylib.EndMode2D();

            Raylib.EndTextureMode();
        }

        private void NormalsPass(List<Sprite2D> sprites)
        {
            // --- NORMALS ---
            Raylib.BeginTextureMode(NormalBuffer);
            Raylib.ClearBackground(new Color(128, 128, 255, 0));

            if (Camera != null)
                Raylib.BeginMode2D(Camera.Camera);
            Raylib.BeginBlendMode(BlendMode.Alpha);

            foreach (var sprite in sprites)
            {
                sprite.DrawLight();
            }

            Raylib.EndBlendMode();

            if (Camera != null)
                Raylib.EndMode2D();

            Raylib.EndTextureMode();
        }

        private void DepthPass(List<Sprite2D> sprites)
        {
            Raylib.BeginTextureMode(DepthBuffer);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));

            if (Camera != null)
                Raylib.BeginMode2D(Camera.Camera);

            // Depth pass MUST use alpha blending so sprite silhouettes are preserved
            Raylib.BeginBlendMode(BlendMode.Alpha);

            foreach (var sprite in sprites)
            {
                float yNorm = Math.Clamp(((sprite.Position.Y - Camera.Position.Y) / (MAX_Y * 2)) + 0.5f, 0f, 1f);
                float zNorm = Math.Clamp(sprite.Position.Z / MAX_Z, 0f, 1f);

                // Shader MUST be active here
                Raylib.BeginShaderMode(DepthShader);
                Raylib.SetShaderValue(DepthShader, locUprightSprite, sprite.Upright?1f:0f, ShaderUniformDataType.Float);
                Raylib.SetShaderValue(DepthShader, locYNorm, yNorm, ShaderUniformDataType.Float);
                Raylib.SetShaderValue(DepthShader, locZNorm, zNorm, ShaderUniformDataType.Float);

                Raylib.DrawTexturePro(
                    sprite.Sheet,
                    sprite.src,
                    sprite.dest,
                    Vector2.Zero,
                    0f,
                    Color.White
                );

                Raylib.EndShaderMode();
            }

            Raylib.EndBlendMode();

            if (Camera != null)
                Raylib.EndMode2D();

            Raylib.EndTextureMode();
        }
        private void DrawLoop()
        {
            int view = 4; // 1=Base,2=Normals,3=Depth,4=Light,5=Composite (start on Light)
            while (!Raylib.WindowShouldClose())
            {
                // quick view keys
                if (Raylib.IsKeyPressed(KeyboardKey.One)) view = 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Two)) view = 2;
                if (Raylib.IsKeyPressed(KeyboardKey.Three)) view = 3;
                if (Raylib.IsKeyPressed(KeyboardKey.Four)) view = 4;

                // snapshot sprites safely
                List<Sprite2D> spritesByDepth;
                lock (AllSprites)
                {
                    spritesByDepth = AllSprites
                        .OrderBy(s => s.Position.Z)
                        .ThenBy(s => -s.Position.Y)
                        .ToList();
                }

                // produce G-buffers and lighting
                GBufferPass(spritesByDepth);

                // draw selected buffer or composite full-screen for inspection
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                // RenderTexture2D textures are typically vertically flipped relative to screen.
                // Use a source rectangle with negative height to flip them upright.
                Rectangle src = new Rectangle(0, ScreenSize.Y, ScreenSize.X, -ScreenSize.Y);
                Vector2 dest = new Vector2(0, 0);

                switch (view)
                {
                    case 1: // Base (albedo)
                        Raylib.DrawTextureRec(BaseBuffer.Texture, src, dest, Color.White);
                        Raylib.DrawText("View: BaseBuffer (F1)", 10, 10, 20, Color.White);
                        break;
                    case 2: // Normals
                        Raylib.DrawTextureRec(NormalBuffer.Texture, src, dest, Color.White);
                        Raylib.DrawText("View: NormalBuffer (F2)", 10, 10, 20, Color.White);
                        break;
                    case 3: // Depth
                        Raylib.DrawTextureRec(DepthBuffer.Texture, src, dest, Color.White);
                        Raylib.DrawText("View: DepthBuffer (F3)", 10, 10, 20, Color.White);
                        break;
                    default:
                        // CompositePass draws the final composite using CompositeShader.
                        // CompositePass expects to be called between BeginDrawing/EndDrawing.
                        Compositer.Draw(
                            BaseBuffer,
                            NormalBuffer,
                            DepthBuffer,
                            ScreenSize,
                            MAX_Y,
                            MAX_Z,
                            Lights,
                            Camera
                        );

                        Raylib.DrawText("View: Composite (F4)", 10, 10, 20, Color.White);
                        break;
                }

                Raylib.EndDrawing();
            }
        }
        public abstract void OnLoad();
        public abstract void OnUpdate(float dt);
        public abstract void OnDraw();
    }
}