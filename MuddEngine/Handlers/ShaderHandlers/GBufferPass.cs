using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;

namespace MuddEngine.MuddEngine
{
    public class GBufferPass
    {
        public Shader Shader;
        public CameraSprite Camera;

        public int locCameraPosition;
        public int locCameraOffset;
        public int locCameraTarget;
        public int locCameraZoom;

        public int locDebugMode;
        public int locScreenSize;
        public int locObjectCount;
        public int locAtlasSize;
        public int locScale;
        public int locBaseAtlas;
        public int locNormalsAtlas;
        public int locDepthAtlas;
        private Texture2D spriteDataTex;
        private const int ROWS_PER_SPRITE = 8;
        private int locSpriteData;
        private int locMaxSprites;
        private const int MAX_MUDD_OBJECTS = 512;
        public GBufferPass()
        {
            Shader = ShaderHelper.ShaderLoader.LoadShaderWithIncludes(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/gBufferPass.fs",
                "Assets/Shaders/Compiled/gBufferPassCompiled.fs"
            );
            locDebugMode    = Raylib.GetShaderLocation(Shader, "debugMode");
            locScreenSize   = Raylib.GetShaderLocation(Shader, "screenSize");
            locObjectCount  = Raylib.GetShaderLocation(Shader, "muddObjectCount");

            locCameraPosition = Raylib.GetShaderLocation(Shader, "cameraPosition");
            locCameraOffset = Raylib.GetShaderLocation(Shader, "cameraOffset");
            locCameraTarget = Raylib.GetShaderLocation(Shader, "cameraTarget");
            locCameraZoom   = Raylib.GetShaderLocation(Shader, "cameraZoom");

            locBaseAtlas    = Raylib.GetShaderLocation(Shader, "u_BaseAtlas");
            locNormalsAtlas = Raylib.GetShaderLocation(Shader, "u_NormalsAtlas");
            locDepthAtlas   = Raylib.GetShaderLocation(Shader, "u_DepthAtlas");

            locSpriteData = Raylib.GetShaderLocation(Shader, "u_SpriteData");
            locMaxSprites = Raylib.GetShaderLocation(Shader, "u_MaxSprites");
            //Create Sprite Data Texture
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
            List<LightSource> lights,
            List<MuddObject> muddObjects,
            Texture2D baseAtlas,
            Texture2D normalAtlas,
            Texture2D depthAtlas,
            int debugMode
        )
        {
            if (Camera == null)
                return;

            int objectCount = Math.Min(muddObjects?.Count ?? 0, MAX_MUDD_OBJECTS);


            Raylib.BeginShaderMode(Shader);

            if (locBaseAtlas != -1 && baseAtlas.Id != 0)
                Raylib.SetShaderValueTexture(Shader, locBaseAtlas, baseAtlas);
            if (locNormalsAtlas != -1 && normalAtlas.Id != 0)
                Raylib.SetShaderValueTexture(Shader, locNormalsAtlas, normalAtlas);
            if (locDepthAtlas != -1 && depthAtlas.Id != 0)
                Raylib.SetShaderValueTexture(Shader, locDepthAtlas, depthAtlas);

            //Update Sprite Data Texture Bytes
            byte[] spriteBytes = BufferHelper.LoadDataFromObjects(
                MAX_MUDD_OBJECTS,
                ROWS_PER_SPRITE,
                [.. muddObjects.Cast<object>()],
                o => ObjectHelpers.BuildSpriteColumn(o, Camera)
            );
            
            //Add Sprite Data Texture
            if (locSpriteData != -1)
            {
                Raylib.UpdateTexture(spriteDataTex, spriteBytes);
                Raylib.SetShaderValueTexture(Shader, locSpriteData, spriteDataTex);
            }
            if (locMaxSprites != -1)
                Raylib.SetShaderValue(Shader, locMaxSprites, MAX_MUDD_OBJECTS, ShaderUniformDataType.Int);

            Vector2 atlasSize = new Vector2(baseAtlas.Width, baseAtlas.Height);
            if (locAtlasSize != -1)
                Raylib.SetShaderValue(Shader, locAtlasSize, atlasSize, ShaderUniformDataType.Vec2);

            if (locScale != -1)
                Raylib.SetShaderValue(Shader, locScale, Camera.Camera.Zoom, ShaderUniformDataType.Float);

            if (locCameraPosition != -1)
                Raylib.SetShaderValue(Shader, locCameraPosition, Camera.Position, ShaderUniformDataType.Vec3);
            if (locCameraOffset != -1)
                Raylib.SetShaderValue(Shader, locCameraOffset, Camera.Camera.Offset, ShaderUniformDataType.Vec2);
            if (locCameraTarget != -1)
                Raylib.SetShaderValue(Shader, locCameraTarget, Camera.Camera.Target, ShaderUniformDataType.Vec2);
            if (locCameraZoom != -1)
                Raylib.SetShaderValue(Shader, locCameraZoom, Camera.Camera.Zoom, ShaderUniformDataType.Float);

            if (locDebugMode != -1)
                Raylib.SetShaderValue(Shader, locDebugMode, debugMode, ShaderUniformDataType.Int);
            if (locScreenSize != -1)
                Raylib.SetShaderValue(Shader, locScreenSize, screenSize, ShaderUniformDataType.Vec2);
            if (locObjectCount != -1)
                Raylib.SetShaderValue(Shader, locObjectCount, objectCount, ShaderUniformDataType.Int);

            Raylib.DrawRectangle(0, 0, (int)screenSize.X, (int)screenSize.Y, Color.White);

            Raylib.EndShaderMode();
        }
    }
}