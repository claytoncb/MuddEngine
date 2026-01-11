using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Rectangle = Raylib_cs.Rectangle;
namespace MuddEngine.MuddEngine
{
    public class BufferHandler
    {
        public CameraSprite Camera;
        public RenderTexture2D BaseBuffer;
        public RenderTexture2D DepthBuffer;
        public RenderTexture2D NormalBuffer;
        public Shader CompositeShader;
        public Shader DepthShader;
        int locZNorm;
        int locYNorm;
        int locHalfHeight;
        Rectangle src;
        Vector2 dest;
        public BufferHandler(Vector2 ScreenSize)
        {
            src = new Rectangle(0, ScreenSize.Y, ScreenSize.X, -ScreenSize.Y);
            dest = new Vector2(0, 0);
            BaseBuffer   = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
            NormalBuffer = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
            DepthBuffer  = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
            DepthShader = Raylib.LoadShader(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/depth.fs"
            );
            locZNorm = Raylib.GetShaderLocation(DepthShader, "zNorm");
            locYNorm = Raylib.GetShaderLocation(DepthShader, "yNorm");
            locHalfHeight = Raylib.GetShaderLocation(DepthShader, "halfHeight");
        }
        public void OnLoad( CameraSprite Camera)
        {
            this.Camera = Camera;
        }
        public void WriteBase(List<Sprite2D> sprites)
        {
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
        public void WriteNormals(List<Sprite2D> sprites)
        {
            Raylib.BeginTextureMode(NormalBuffer);
            Raylib.ClearBackground(new Color(128, 128, 255, 0));

            if (Camera != null)
                Raylib.BeginMode2D(Camera.Camera);
                Raylib.BeginBlendMode(BlendMode.Alpha);

            foreach (var sprite in sprites)
            {
                sprite.DrawNormals();
            }

            Raylib.EndBlendMode();

            if (Camera != null)
                Raylib.EndMode2D();

            Raylib.EndTextureMode();
        }
        public void WriteDepth(List<Sprite2D> sprites, float MAX_Y, float MAX_Z, float MAX_SPRITE_HEIGHT)
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
                float spriteHalfHeight = sprite.Scale.Y * 0.5f;
                float spriteHalfHeightNorm = spriteHalfHeight / MAX_SPRITE_HEIGHT;
                // Shader MUST be active here
                Raylib.BeginShaderMode(DepthShader);
                Raylib.SetShaderValue(DepthShader, locYNorm, yNorm, ShaderUniformDataType.Float);
                Raylib.SetShaderValue(DepthShader, locZNorm, zNorm, ShaderUniformDataType.Float);
                Raylib.SetShaderValue(DepthShader, locHalfHeight, spriteHalfHeightNorm, ShaderUniformDataType.Float);
                sprite.DrawParallax();
                Raylib.EndShaderMode();
            }

            Raylib.EndBlendMode();

            if (Camera != null)
                Raylib.EndMode2D();

            Raylib.EndTextureMode();
        }
        public void WriteBuffers(List<Sprite2D> sprites, float MAX_Y, float MAX_Z, float MAX_SPRITE_HEIGHT)
        {
            WriteBase(sprites);
            WriteNormals(sprites);
            WriteDepth(sprites, MAX_Y, MAX_Z, MAX_SPRITE_HEIGHT);
        }
        public void DrawBase()
        {
            Raylib.DrawTextureRec(BaseBuffer.Texture, src, dest, Color.White);
            Raylib.DrawText("View: BaseBuffer (F1)", 10, 10, 20, Color.White);
        }
        public void DrawNormals()
        {
            Raylib.DrawTextureRec(NormalBuffer.Texture, src, dest, Color.White);
            Raylib.DrawText("View: NormalBuffer (F2)", 10, 10, 20, Color.White);
        }
        public void DrawDepth()
        {
            Raylib.DrawTextureRec(DepthBuffer.Texture, src, dest, Color.White);
            Raylib.DrawText("View: DepthBuffer (F3)", 10, 10, 20, Color.White);
        }
        public void Unload()
        {
            Raylib.UnloadShader(DepthShader);
            Raylib.UnloadRenderTexture(BaseBuffer);
            Raylib.UnloadRenderTexture(NormalBuffer);
            Raylib.UnloadRenderTexture(DepthBuffer);
        }
    }
}