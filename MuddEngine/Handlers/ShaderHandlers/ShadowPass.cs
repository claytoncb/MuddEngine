using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;

namespace MuddEngine.MuddEngine
{
    public class ShadowPass
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
        public int locGBuffer;
        private int locSpriteData;
        private int locMaxSprites;
        private int locRowsPerSprite;
        private const int ROWS_PER_SPRITE = 8;
        private const int MAX_MUDD_OBJECTS = 512;
        public int locLightCount;
        public int locLightPositions;
        public int locLightRadii;
        public int locLightIntensities;
        public int locLightColors;
        private const int MAX_LIGHTS       = 16;
        public ShadowPass()
        {
            Shader = ShaderHelper.ShaderLoader.LoadShaderWithIncludes(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/shadowPass.fs",
                "Assets/Shaders/Compiled/shadowPassCompiled.fs"
            );
            locBaseAtlas    = Raylib.GetShaderLocation(Shader, "u_BaseAtlas");
            locGBuffer = Raylib.GetShaderLocation(Shader, "u_GBuffer");
            locSpriteData = Raylib.GetShaderLocation(Shader, "u_SpriteData");
            locMaxSprites = Raylib.GetShaderLocation(Shader, "u_MaxSprites");
            locRowsPerSprite = Raylib.GetShaderLocation(Shader, "u_RowsPerSprite");
            locScreenSize   = Raylib.GetShaderLocation(Shader, "screenSize");
            locObjectCount  = Raylib.GetShaderLocation(Shader, "muddObjectCount");
            locCameraPosition = Raylib.GetShaderLocation(Shader, "cameraPosition");
            locCameraZoom   = Raylib.GetShaderLocation(Shader, "cameraZoom");
            locLightCount       = Raylib.GetShaderLocation(Shader, "lightCount");
            locLightPositions   = Raylib.GetShaderLocation(Shader, "lightPositions");
            locLightRadii       = Raylib.GetShaderLocation(Shader, "lightRadii");
            locLightIntensities = Raylib.GetShaderLocation(Shader, "lightIntensities");
            locLightColors      = Raylib.GetShaderLocation(Shader, "lightColors");
            
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
            int ObjectCount,
            byte[] spriteBytes,
            RenderTexture2D GBufferTexture,
            Texture2D baseAtlas
        )
        {
            if (Camera == null)
                return;
            int lightCount  = Math.Min(lights?.Count ?? 0, MAX_LIGHTS);
            int objectCount = Math.Min(ObjectCount, MAX_MUDD_OBJECTS);
            float[] lightPosArray       = new float[MAX_LIGHTS * 3];
            float[] lightRadiusArray    = new float[MAX_LIGHTS];
            float[] lightIntensityArray = new float[MAX_LIGHTS];
            float[] lightColorArray     = new float[MAX_LIGHTS * 3];
            Raylib.BeginShaderMode(Shader);
            Raylib.SetShaderValueTexture(Shader, locBaseAtlas, baseAtlas);
            for (int i = 0; i < lightCount; i++)
            {
                var L = lights[i];
                lightPosArray[i * 3 + 0] = L.Position.X;
                lightPosArray[i * 3 + 1] = L.Position.Y;
                lightPosArray[i * 3 + 2] = L.Position.Z;

                lightRadiusArray[i]    = L.Radius;
                lightIntensityArray[i] = L.Intensity;

                lightColorArray[i * 3 + 0] = L.Color.R / 255.0f;
                lightColorArray[i * 3 + 1] = L.Color.G / 255.0f;
                lightColorArray[i * 3 + 2] = L.Color.B / 255.0f;
            }
            Raylib.SetShaderValue(Shader, locLightCount, lightCount, ShaderUniformDataType.Int);
            Raylib.SetShaderValueV(Shader, locLightPositions, lightPosArray, ShaderUniformDataType.Vec3, lightCount);
            Raylib.SetShaderValueV(Shader, locLightRadii, lightRadiusArray, ShaderUniformDataType.Float, lightCount);
            Raylib.SetShaderValueV(Shader, locLightIntensities, lightIntensityArray, ShaderUniformDataType.Float, lightCount);
            Raylib.SetShaderValueV(Shader, locLightColors, lightColorArray, ShaderUniformDataType.Vec3, lightCount);
            Raylib.UpdateTexture(spriteDataTex, spriteBytes);
            Raylib.SetShaderValueTexture(Shader, locGBuffer, GBufferTexture.Texture);
            Raylib.SetShaderValueTexture(Shader, locSpriteData, spriteDataTex);
            Raylib.SetShaderValue(Shader, locMaxSprites, MAX_MUDD_OBJECTS, ShaderUniformDataType.Int);
            Raylib.SetShaderValue(Shader, locRowsPerSprite, ROWS_PER_SPRITE, ShaderUniformDataType.Int);
            Vector2 atlasSize = new(baseAtlas.Width, baseAtlas.Height);
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