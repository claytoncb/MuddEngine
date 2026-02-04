using System.Media;
using System.Numerics;
using System.Security.Principal;
using MuddEngine.MuddEngine;
using Raylib_cs;

namespace MuddEngine.DemoGame
{
    class DemoGame : MuddEngine.MuddEngine
    {
        Player player;
        LightSource light1;
        Sprite ceilingLight1;
        LightSource light2;
        Sprite ceilingLight2;
        LightSource light3;
        Sprite ceilingLight3;
        LightSource light4;
        Sprite ceilingLight4;
        public DemoGame() : base("test",new Vector2(2048,1024)) {}
        public override void OnLoad()
        {
            player = new Player("Conductor",new Vector3(0,0,0.01f),0.5f);

            Camera = new CameraSprite(player, new Vector2(2048, 1024));

            light1 = new LightSource(new Vector3(128,0,100f), 400f,2.0f, new Raylib_cs.Color(80,122,255));
            light2 = new LightSource(new Vector3(512,0,100f), 400f, 2.0f, new Raylib_cs.Color(250,100,100));
            light3 = new LightSource(new Vector3(-128,0,100f), 400f, 2.0f, new Raylib_cs.Color(40,250,150));
            light4 = new LightSource(new Vector3(-512,0,100f), 400f, 2.0f, new Raylib_cs.Color(255,255,255));
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 64; i++)
                {
                    Sprite Tile = new Sprite($"Tile{i}-{j}",new Vector3(-768 + 32*i,32 - 32*j,0),0);
                    Tile.SheetLocation = new((j==0?32:0) + (i==0?128:0) + (i==63?64:0),576);
                    Tile.VisibleSize = new(32,16);
                    Tile.VisibleOffset = new(0,7);
                    Tile.isFlat = true;
                }
            }
        }


        public override void OnUpdate(float dt, float t)
        {
            //light1.Position.Z = player.Position.Y;
        }
        public override void OnDraw() {
        }
    }
    
}