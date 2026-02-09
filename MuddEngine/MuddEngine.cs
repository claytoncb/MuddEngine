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
        private Stopwatch Stopwatch = new();
        private Stopwatch LoopStopwatch = new();
        public Vector2 ScreenSize;
        public static CameraSprite Camera;
        protected static List<LightSource> AllLights = new();
        private static List<MuddObject> AllObjects = new();
        public Texture2D BaseAtlas;
        public Texture2D NormalAtlas;
        public Texture2D DepthAtlas;
        public Compositer Compositer;

        public MuddEngine(string title, Vector2 screenSize)
        {
            ScreenSize = screenSize;
            
            Raylib.InitWindow((int)screenSize.X, (int)screenSize.Y, title);
            Raylib.SetTargetFPS(60);
            BaseAtlas = Raylib.LoadTexture($"Assets/Sprites/Base.png");
            NormalAtlas = Raylib.LoadTexture($"Assets/Sprites/Normal.png");
            DepthAtlas = Raylib.LoadTexture($"Assets/Sprites/Depth.png");
            Raylib.SetTextureFilter(BaseAtlas, TextureFilter.Point);
            Raylib.SetTextureFilter(NormalAtlas, TextureFilter.Point);
            Raylib.SetTextureFilter(DepthAtlas, TextureFilter.Point);
            Compositer = new Compositer();

            OnLoad();
            Compositer.OnLoad(Camera);
            Stopwatch.Start();
            GameLoop();

            Raylib.UnloadShader(Compositer.Shader);
            Raylib.CloseWindow();
        }
        public static void RegisterObject(MuddObject muddObject) => AllObjects.Add(muddObject);
        public static void UnregisterObject(MuddObject muddObject) => AllObjects.Remove(muddObject);
        public static void RegisterLight(LightSource light) => AllLights.Add(light);
        public static void UnregisterLight(LightSource light) => AllLights.Remove(light);

        private void GameLoop()
        {
            LoopStopwatch.Start();
            while (!Raylib.WindowShouldClose())
            {
                float dt = (float)LoopStopwatch.Elapsed.TotalSeconds;
                float t = (float)Stopwatch.Elapsed.TotalSeconds;
                LoopStopwatch.Restart();

                foreach (var MuddObject in AllObjects)
                    MuddObject.Update(dt, t);

                OnUpdate(dt, t);
                Camera.Update(dt, t);     
                Keyboard.Update();              
                    
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                List<MuddObject> FlatObjects = ObjectHelpers.FlattenObjects(AllObjects);

                List<MuddObject> VisibleObjects = ObjectHelpers.FilterVisible(
                    FlatObjects,
                    ScreenSize,
                    Camera.Camera
                );

                Compositer.Draw(
                            ScreenSize,
                            AllLights,
                            VisibleObjects,
                            BaseAtlas,
                            NormalAtlas,
                            DepthAtlas,
                            Keyboard.DebugMode
                        );

                Raylib.EndDrawing();
            }
        }
        public abstract void OnLoad();
        public abstract void OnUpdate(float dt, float t);
        public abstract void OnDraw();
    }
}