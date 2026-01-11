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
            player = new Player(new Vector3(1024,512,10),"PlayerHead",0, 150f);
            Camera = new CameraSprite(player, new Vector2(2048, 1024));
            Lighting = new LightingRenderer();

            light1 = new LightSource(player.Position+ new Vector3(0,0,17f), 2400f,1.0f, Raylib_cs.Color.Blue, "BlueLight");
            AddLight(light1);
            ceilingLight1 = new Sprite2D(player.Position + new Vector3(0,0, 18f), "CeilingLight1",1, 0f);

            light2 = new LightSource(player.Position + new Vector3(512,0,17f), 800f, 1.0f, Raylib_cs.Color.Red, "RedLight");
            AddLight(light2);
            ceilingLight2 = new Sprite2D(player.Position + new Vector3(512,0, 18f), "CeilingLight2",1, 0f);

            light3 = new LightSource(player.Position + new Vector3(-512,0,17f), 800f, 1.0f, Raylib_cs.Color.Green, "GreenLight");
            AddLight(light3);
            ceilingLight3 = new Sprite2D(player.Position + new Vector3(-512,0, 18f), "CeilingLight3",1, 0f);

            light4 = new LightSource(player.Position + new Vector3(-1024,0,17f), 800f, 1.0f, Raylib_cs.Color.White, "WhiteLight");
            AddLight(light4);
            ceilingLight4 = new Sprite2D(player.Position + new Vector3(-1024,0, 18f), "CeilingLight4",1, 0f);
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 20; i++)
                {
                    Sprite2D Tile = new Sprite2D(new Vector3(128*i,640 - 128*j,0),$"Tile{i}-{j}",0,0,"Assets/Sprites/TileAtlas.png","Assets/Sprites/TileNormals.png", false);
                }
            }
        }


        public override void OnUpdate(float dt)
        {
        }
        public override void OnDraw() {
        }
    }
    
}