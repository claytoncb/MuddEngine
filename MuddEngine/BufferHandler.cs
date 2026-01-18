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
        public RenderTexture2D MetaBuffer;

        public Shader DepthShader;
        int locSpriteAtlasPos;
        int locFrameSize;
        int locAtlasSize;
        public Shader MetaShader;
        int locSpriteID;
        Rectangle src;
        Vector2 dest;

        public BufferHandler(Vector2 ScreenSize)
        {
            src = new Rectangle(0, ScreenSize.Y, ScreenSize.X, -ScreenSize.Y);
            dest = new Vector2(0, 0);

            BaseBuffer   = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
            NormalBuffer = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
            DepthBuffer  = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);
            MetaBuffer   = Raylib.LoadRenderTexture((int)ScreenSize.X, (int)ScreenSize.Y);

            DepthShader = Raylib.LoadShader(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/depth.fs"
            );
            locSpriteAtlasPos = Raylib.GetShaderLocation(DepthShader, "spriteAtlasPos");
            locFrameSize = Raylib.GetShaderLocation(DepthShader, "frameSize");
            locAtlasSize = Raylib.GetShaderLocation(DepthShader, "atlasSize");
            MetaShader = Raylib.LoadShader(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/meta.fs"
            );
            locSpriteID = Raylib.GetShaderLocation(MetaShader, "spriteID");
        }

        public void OnLoad(CameraSprite Camera)
        {
            this.Camera = Camera;
        }

        public void WriteBase(List<Sprite2D> sprites)
        {
            Raylib.BeginTextureMode(BaseBuffer);
            Raylib.ClearBackground(Color.Blank);

            if (Camera != null)
                Raylib.BeginMode2D(Camera.Camera);

            Raylib.BeginBlendMode(BlendMode.Alpha);
            foreach (var sprite in sprites)
                sprite.DrawBase();
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
                sprite.DrawNormals();
            Raylib.EndBlendMode();

            if (Camera != null)
                Raylib.EndMode2D();

            Raylib.EndTextureMode();
        }

        public void WriteDepth(List<Sprite2D> sprites)
        {
            Raylib.BeginTextureMode(DepthBuffer);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));

            if (Camera != null)
                Raylib.BeginMode2D(Camera.Camera);

            Raylib.BeginBlendMode(BlendMode.Alpha);
            foreach (var sprite in sprites)
            {
                Raylib.BeginShaderMode(DepthShader);
                Raylib.SetShaderValue(DepthShader, locSpriteAtlasPos, new Vector2(sprite.Facing, sprite.Row), ShaderUniformDataType.Vec2);
                Raylib.SetShaderValue(DepthShader, locFrameSize, (float)sprite.Size, ShaderUniformDataType.Float);
                Raylib.SetShaderValue(DepthShader, locAtlasSize, sprite.SheetBase.Dimensions, ShaderUniformDataType.Vec2);
                sprite.DrawParallax();
                Raylib.EndShaderMode();
            }
            Raylib.EndBlendMode();

            if (Camera != null)
                Raylib.EndMode2D();

            Raylib.EndTextureMode();
        }

        public void WriteMeta(List<Sprite2D> sprites)
        {
            Raylib.BeginTextureMode(MetaBuffer);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));

            if (Camera != null)
                Raylib.BeginMode2D(Camera.Camera);

            Raylib.BeginBlendMode(BlendMode.Alpha);

            for (int i = 0; i < sprites.Count; i++)
            {
                float spriteIDNorm = (sprites.Count > 1)
                    ? (float)i / (sprites.Count - 1)
                    : 0.0f;

                Raylib.BeginShaderMode(MetaShader);
                Raylib.SetShaderValue(MetaShader, locSpriteID, spriteIDNorm, ShaderUniformDataType.Float);
                sprites[i].DrawParallax();
                Raylib.EndShaderMode();
            }

            Raylib.EndBlendMode();

            if (Camera != null)
                Raylib.EndMode2D();

            Raylib.EndTextureMode();
        }

        public void WriteBuffers(List<Sprite2D> sprites)
        {
            WriteBase(sprites);
            WriteNormals(sprites);
            WriteDepth(sprites);
            WriteMeta(sprites);
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

        public void DrawMeta()
        {
            Raylib.DrawTextureRec(MetaBuffer.Texture, src, dest, Color.White);
            Raylib.DrawText("View: MetaBuffer (F4)", 10, 10, 20, Color.White);
        }

        public void Unload()
        {
            Raylib.UnloadShader(DepthShader);
            Raylib.UnloadShader(MetaShader);
            Raylib.UnloadRenderTexture(BaseBuffer);
            Raylib.UnloadRenderTexture(NormalBuffer);
            Raylib.UnloadRenderTexture(DepthBuffer);
            Raylib.UnloadRenderTexture(MetaBuffer);
        }
    }
}