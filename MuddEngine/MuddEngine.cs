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
    protected List<LightSource> Lights = new();
    private static List<Sprite2D> AllSprites = new();
    // Constructor
    public Compositer Compositer;
    public BufferHandler BufferHandler;

    public MuddEngine(string Title, Vector2 ScreenSize)
    {
        this.ScreenSize = ScreenSize;
        Raylib.InitWindow((int)ScreenSize.X, (int)ScreenSize.Y, Title);
        Raylib.SetTargetFPS(60);
        BufferHandler = new BufferHandler(ScreenSize);
        Compositer = new Compositer();
        OnLoad();
        BufferHandler.OnLoad(Camera);

        UpdateThread = new Thread(UpdateLoop);
        UpdateThread.Start();
        DrawLoop();

        Running = false;
        UpdateThread.Join();

        // Cleanup
        
        Raylib.UnloadShader(Compositer.Shader);
        BufferHandler.Unload();

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
            int view = 4; // 1=Base,2=Normals,3=Depth,4=Light,5=Composite (start on Light)
            while (!Raylib.WindowShouldClose())
            {
                // quick view keys
                if (Raylib.IsKeyPressed(KeyboardKey.One)) view = 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Two)) view = 2;
                if (Raylib.IsKeyPressed(KeyboardKey.Three)) view = 3;
                if (Raylib.IsKeyPressed(KeyboardKey.Four)) view = 4;
                if (Raylib.IsKeyPressed(KeyboardKey.Five)) view = 5;

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
                BufferHandler.WriteBuffers(spritesByDepth, MAX_Y, MAX_Z);

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
                        BufferHandler.DrawBase();
                        break;
                    case 2: // Normals
                        BufferHandler.DrawNormals();
                        break;
                    case 3: // Depth
                        BufferHandler.DrawDepth();
                        break;
                    default:
                        // CompositePass draws the final composite using CompositeShader.
                        // CompositePass expects to be called between BeginDrawing/EndDrawing.
                        Compositer.Draw(
                            BufferHandler.BaseBuffer,
                            BufferHandler.NormalBuffer,
                            BufferHandler.DepthBuffer,
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