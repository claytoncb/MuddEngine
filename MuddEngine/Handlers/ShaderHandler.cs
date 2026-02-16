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
        private ShadowPass ShadowPass;
        private LightingPass LightingPass;
        private SpriteCompositePass SpriteCompositePass;
        private ParticlePass ParticlePass;
        private FinalComposite FinalComposite;
        public Compositer Compositer;
        private RenderTexture2D GBufferTexture;
        private RenderTexture2D ShadowTexture;
        public const int ROWS_PER_SPRITE  = 8;
        public const int MAX_MUDD_OBJECTS = 512;
        public const int MAX_LIGHTS       = 16;
        public ShaderHandler(Vector2 screenSize)
        {
            GBufferPass = new();
            GBufferTexture = Raylib.LoadRenderTexture((int)screenSize.X, (int)screenSize.Y * 4);
            ShadowPass = new();
            ShadowTexture = Raylib.LoadRenderTexture((int)screenSize.X, (int)screenSize.Y);
            LightingPass = new();
            SpriteCompositePass = new();
            ParticlePass = new();
            FinalComposite = new();
            
        }
        public void Load(CameraSprite Camera)
        {
            this.Camera = Camera;
            GBufferPass.Load(Camera);
            ShadowPass.Load(Camera);
            LightingPass.Load(Camera);
            SpriteCompositePass.Load(Camera);
            ParticlePass.Load(Camera);
            FinalComposite.Load(Camera);
        }
        public void UnLoad()
        {
            GBufferPass.UnLoad();
            Raylib.UnloadRenderTexture(GBufferTexture);
            ShadowPass.UnLoad();
            Raylib.UnloadRenderTexture(ShadowTexture);
            LightingPass.UnLoad();
            SpriteCompositePass.UnLoad();
            ParticlePass.UnLoad();
            FinalComposite.UnLoad();
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
                muddObjects.Count,
                muddBytes,
                baseAtlas,
                normalAtlas,
                depthAtlas
            );
            Raylib.EndTextureMode();
            Raylib.BeginTextureMode(ShadowTexture);
            Raylib.ClearBackground(Color.Black);
            ShadowPass.Draw(
                screenSize,
                lights,
                muddObjects.Count,
                muddBytes,
                GBufferTexture,
                baseAtlas
            );
            Raylib.EndTextureMode();
            LightingPass.Draw();
            SpriteCompositePass.Draw();
            ParticlePass.Draw();
            FinalComposite.Draw();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Rectangle dst = new (0, 0, screenSize.X, screenSize.Y);
            Vector2 origin = new (0, 0);
            Rectangle src;
            switch(Keyboard.DebugMode)
            {
                case 0:
                break;
                case 1:
                    src = new (0, 0, GBufferTexture.Texture.Width, -GBufferTexture.Texture.Height);
                    Raylib.DrawTexturePro(GBufferTexture.Texture, src, dst, origin, 0.0f, Color.White);
                break;
                case 2:
                    src = new (0, 0, screenSize.X, -screenSize.Y);
                    Raylib.DrawTexturePro(ShadowTexture.Texture, src, dst, origin, 0.0f, Color.White);
                break;
                default:
                break;
            }
            Raylib.DrawText($"Debug Mode: {Keyboard.DebugMode}", 10, 10, 20, Color.White);
            Raylib.EndDrawing();
        }
    }
}