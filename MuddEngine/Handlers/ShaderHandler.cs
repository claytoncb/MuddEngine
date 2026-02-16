using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using Rectangle = Raylib_cs.Rectangle;

namespace MuddEngine.MuddEngine
{
    public class ShaderHandler
    {
        CameraSprite Camera;
        private GBufferPass GBufferPass;
        public Compositer Compositer;
        private RenderTexture2D GBufferTexture;
        public const int ROWS_PER_SPRITE  = 8;
        public const int MAX_MUDD_OBJECTS = 512;
        public const int MAX_LIGHTS       = 16;
        public ShaderHandler(Vector2 screenSize)
        {
            GBufferPass = new();
            Compositer = new();
            GBufferTexture = Raylib.LoadRenderTexture((int)screenSize.X, (int)screenSize.Y * 4);
        }
        public void Load(CameraSprite Camera)
        {
            this.Camera = Camera;
            GBufferPass.Load(Camera);
            Compositer.Load(Camera);
        }
        public void UnLoad()
        {
            GBufferPass.UnLoad();
            Compositer.UnLoad();
        }
        public void Draw(
            Vector2 screenSize,
            List<LightSource> lights,
            List<MuddObject> muddObjects,
            Texture2D baseAtlas,
            Texture2D normalAtlas,
            Texture2D depthAtlas
        )
        {
            byte[] muddBytes = BufferHelper.LoadDataFromObjects(
                MAX_MUDD_OBJECTS,
                ROWS_PER_SPRITE,
                [.. muddObjects.Cast<object>()],
                o => ObjectHelpers.BuildSpriteColumn(o, Camera)
            );
           
            Raylib.BeginTextureMode(GBufferTexture);
            Raylib.ClearBackground(Color.Black);
            Vector2 renderTargetSize = new Vector2(GBufferTexture.Texture.Width, GBufferTexture.Texture.Height);

            GBufferPass.Draw(
                renderTargetSize,
                lights,
                muddObjects,
                baseAtlas,
                normalAtlas,
                depthAtlas,
                Keyboard.DebugMode
            );

            Raylib.EndTextureMode();
            if (Keyboard.DebugMode == 1)
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                Rectangle src = new Rectangle(0, 0, GBufferTexture.Texture.Width, -GBufferTexture.Texture.Height);
                Rectangle dst = new Rectangle(0, 0, screenSize.X, screenSize.Y);
                Vector2 origin = new Vector2(0, 0);
                Raylib.DrawTexturePro(GBufferTexture.Texture, src, dst, origin, 0.0f, Color.White);
                Raylib.DrawText($"Debug Mode: {Keyboard.DebugMode}", 10, 10, 20, Color.White);
                Raylib.EndDrawing();
            }
            else
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                Raylib.EndDrawing();
            }
        }
    }
}