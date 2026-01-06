using System.Media;
using System.Numerics;
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
        LightSource light3;
        LightSource light4;
        public DemoGame() : base("test",new Vector2(2048,1024)) {}
        public override void OnLoad()
        {
            player = new Player(new Vector3(1024,512,0),"PlayerHead",0, 150f);
            Camera = new CameraSprite(player, new Vector2(2048, 1024));
            Lighting = new LightingRenderer();
            light1 = new LightSource(player.Position+ new Vector3(0,0,12f), 800f,1.0f, Raylib_cs.Color.Blue, "BlueLight");
            AddLight(light1);
            //ceilingLight1 = new Sprite2D(player.Position + new Vector3(512,0, 19f), "CeilingLight1",1, 0f);
            light2 = new LightSource(player.Position + new Vector3(512,0,12f), 800f, 1.0f, Raylib_cs.Color.Red, "RedLight");
            AddLight(light2);
            light3 = new LightSource(player.Position + new Vector3(-512,0,12f), 800f, 1.0f, Raylib_cs.Color.Green, "GreenLight");
            AddLight(light3);
            light4 = new LightSource(player.Position + new Vector3(-1024,0,12f), 800f, 1.0f, Raylib_cs.Color.White, "WhiteLight");
            AddLight(light4);

}


        public override void OnUpdate(float dt)
        {
        }
        public override void OnDraw() {
        }
    }
    
}