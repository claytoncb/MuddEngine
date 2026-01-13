using System.Media;
using System.Numerics;
using System.Security.Principal;
using MuddEngine.MuddEngine;
using Raylib_cs;

namespace MuddEngine
{
    class DemoGame : MuddEngine.MuddEngine
    {
        Player player;
        LightSource light1;
        Sprite2D ceilingLight1;
        LightSource light2;
        Sprite2D ceilingLight2;
        LightSource light3;
        Sprite2D ceilingLight3;
        LightSource light4;
        Sprite2D ceilingLight4;
        public DemoGame() : base("test",new Vector2(2048,1024)) {}
        public override void OnLoad()
        {
            player = new Player(new Vector3(0,0,50f),"PlayerHead",0, 150f,"Sprite");
            Camera = new CameraSprite(player, new Vector2(2048, 1024));
            light1 = new LightSource(new Vector3(0,0,80f), 2400f,1.0f, Raylib_cs.Color.Blue, "BlueLight");
            AddLight(light1);
            ceilingLight1 = new Sprite2D(new Vector3(0,0, 220f), "CeilingLight1",1, 0f,"Sprite");

            light2 = new LightSource(new Vector3(512,0,80f), 800f, 1.0f, Raylib_cs.Color.Red, "RedLight");
            AddLight(light2);
            ceilingLight2 = new Sprite2D(new Vector3(512,0, 220f), "CeilingLight2",1, 0f,"Sprite");

            light3 = new LightSource(new Vector3(-512,0,80f), 800f, 1.0f, Raylib_cs.Color.Green, "GreenLight");
            AddLight(light3);
            ceilingLight3 = new Sprite2D(new Vector3(-512,0, 220f), "CeilingLight3",1, 0f,"Sprite");

            light4 = new LightSource(new Vector3(-1024,0,80f), 800f, 1.0f, Raylib_cs.Color.White, "WhiteLight");
            AddLight(light4);
            ceilingLight4 = new Sprite2D(new Vector3(-1024,0, 220f), "CeilingLight4",1, 0f,"Sprite");
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 20; i++)
                {
                    Sprite2D Tile = new Sprite2D(new Vector3(-1536 + 128*i,64 - 128*j,0),$"Tile{i}-{j}",0,0,"Tile", false);
                }
            }
        }


        public override void OnUpdate(float dt)
        {
            //light1.Position.Z = player.Position.Y;
        }
        public override void OnDraw() {
        }
    }
    
}