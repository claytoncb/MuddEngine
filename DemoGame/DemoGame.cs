using System.Numerics;
using MuddEngine.MuddEngine;

namespace MuddEngine.DemoGame
{
    class DemoGame : MuddEngine.MuddEngine
    {
        Player player;
        public DemoGame() : base("test",new Vector2(2048,1024)) {}
        public override void OnLoad()
        {
            player = new Player("Conductor",new Vector3(0,0,0.01f));
            _ = new Sprite($"FloorTop", new Vector3(0, 32, 32))
                    {
                        SheetLocation = new (0, 576),
                        VisibleSize = new(32, 16),
                        VisibleOffset = new(0,8),
                    };

            Camera = new CameraSprite(player, new Vector2(2048, 1024));            
            for (int i = 0; i < 17; i++)
                {
                    _ = new Sprite($"FloorTop{i}", new Vector3(-256 + 32 * i, 32, 0))
                    {
                        SheetLocation = new (0, 576),
                        VisibleSize = new(32, 16),
                        VisibleOffset = new(0,8),
                        isFlat = true
                    };
                    _ = new Sprite($"FloorLower{i}", new Vector3(-256 + 32 * i, -64, 0))
                    {
                        SheetLocation = new (0, 576),
                        VisibleSize = new(32, 16),
                        VisibleOffset = new(0,8),
                        isFlat = true
                    };
                    _ = new Sprite($"Carpet{i}", new Vector3(-256 + 32 * i, -16, 0))
                    {
                        SheetLocation = new (i==0?64:(i==16?32:0), 608),
                        VisibleSize = new(32, 32),
                        isFlat = true
                    };
                    _ = new Sprite($"WallBottom{i}", new Vector3(-256 + 32 * i, 80, 0))
                    {
                        SheetLocation = new (32*(i%4==0?1:(i%4==1 || i%4==3)?2:0)*2, 640),
                        VisibleSize = new(32, 32),
                    };
                    _ = new Sprite($"WallTop{i}", new Vector3(-256 + 32 * i, 80, 32))
                    {
                        SheetLocation = new (32*(i%4==0?1:(i%4==1 || i%4==3)?2:0)*2+32, 640),
                        VisibleSize = new(32, 32),
                    };
                    if (i%4==1 || i%4==3)
                    {
                        _ = new LightSource(new Vector3(-256 + 15.5f + 32 * i, 58, 38.5f), 108f, 1.0f, new Raylib_cs.Color(255, 200, 100));
                        _ = new LightSource(new Vector3(-256 + 15.5f + 32 * i, -58, 38.5f), 108f, 1.0f, new Raylib_cs.Color(255, 200, 100));
                    }
                }
        }


        public override void OnUpdate(float dt, float t)
        {
            Camera.Camera.Zoom = Keyboard.scrollLocation;
        }
        public override void OnDraw() {
        }
    }
    
}