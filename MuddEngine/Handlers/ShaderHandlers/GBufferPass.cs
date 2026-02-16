using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using Rectangle = Raylib_cs.Rectangle;

namespace MuddEngine.MuddEngine
{
    public class GBufferPass
    {
        public Shader Shader;
        public CameraSprite Camera;
        public int locCameraPosition;
        public int locCameraZoom;
        public int locScreenSize;
        public int locObjectCount;
        public int locAtlasSize;
        public int locBaseAtlas;
        public int locNormalsAtlas;
        public int locDepthAtlas;
        private Texture2D spriteDataTex;
        private int locSpriteData;
        private int locMaxSprites;
        private int locRowsPerSprite;
        private const int ROWS_PER_SPRITE = 8;
        private const int MAX_MUDD_OBJECTS = 512;
        public GBufferPass()
        {
            Shader = ShaderHelper.ShaderLoader.LoadShaderWithIncludes(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/gBufferPass.fs",
                "Assets/Shaders/Compiled/gBufferPassCompiled.fs"
            );
            locBaseAtlas    = Raylib.GetShaderLocation(Shader, "u_BaseAtlas");
            locNormalsAtlas = Raylib.GetShaderLocation(Shader, "u_NormalsAtlas");
            locDepthAtlas   = Raylib.GetShaderLocation(Shader, "u_DepthAtlas");
            locSpriteData = Raylib.GetShaderLocation(Shader, "u_SpriteData");
            locMaxSprites = Raylib.GetShaderLocation(Shader, "u_MaxSprites");
            locRowsPerSprite = Raylib.GetShaderLocation(Shader, "u_RowsPerSprite");
            locScreenSize   = Raylib.GetShaderLocation(Shader, "screenSize");
            locAtlasSize   = Raylib.GetShaderLocation(Shader, "atlasSize");
            locObjectCount  = Raylib.GetShaderLocation(Shader, "muddObjectCount");
            locCameraPosition = Raylib.GetShaderLocation(Shader, "cameraPosition");
            locCameraZoom   = Raylib.GetShaderLocation(Shader, "cameraZoom");
            spriteDataTex = BufferHelper.CreateImage(MAX_MUDD_OBJECTS, ROWS_PER_SPRITE);
        }
        public void Load(CameraSprite camera)
        {
            Camera = camera;
        }
        public void UnLoad()
        {
            Raylib.UnloadShader(Shader);
        }
        public void Draw(
            Vector2 screenSize,
            int ObjectCount,
            byte[] spriteBytes,
            Texture2D baseAtlas,
            Texture2D normalAtlas,
            Texture2D depthAtlas
        )
        {
            if (Camera == null)
                return;
            int objectCount = Math.Min(ObjectCount, MAX_MUDD_OBJECTS);
            Raylib.BeginShaderMode(Shader);
            Raylib.SetShaderValueTexture(Shader, locBaseAtlas, baseAtlas);
            Raylib.SetShaderValueTexture(Shader, locNormalsAtlas, normalAtlas);
            Raylib.SetShaderValueTexture(Shader, locDepthAtlas, depthAtlas);
            Raylib.UpdateTexture(spriteDataTex, spriteBytes);
            Raylib.SetShaderValueTexture(Shader, locSpriteData, spriteDataTex);
            Raylib.SetShaderValue(Shader, locMaxSprites, MAX_MUDD_OBJECTS, ShaderUniformDataType.Int);
            Raylib.SetShaderValue(Shader, locRowsPerSprite, ROWS_PER_SPRITE, ShaderUniformDataType.Int);
            Vector2 atlasSize = new (baseAtlas.Width, baseAtlas.Height);
            Raylib.SetShaderValue(Shader, locAtlasSize, atlasSize, ShaderUniformDataType.Vec2);
            Raylib.SetShaderValue(Shader, locCameraPosition, Camera.Position, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(Shader, locCameraZoom, Camera.Camera.Zoom, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(Shader, locScreenSize, screenSize, ShaderUniformDataType.Vec2);
            Raylib.SetShaderValue(Shader, locObjectCount, objectCount, ShaderUniformDataType.Int);
            Raylib.DrawRectangle(0, 0, (int)screenSize.X, (int)screenSize.Y, Color.White);
            Raylib.EndShaderMode();
        }
    }
}