using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;

namespace MuddEngine.MuddEngine
{
    public class Compositer
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

        public int locLightCount;
        public int locLightPositions;
        public int locLightRadii;
        public int locLightIntensities;
        public int locLightColors;

        private Texture2D spriteDataTex;
        private const int ROWS_PER_SPRITE = 8;

        private int locSpriteData;
        private int locMaxSprites;

        private const int MAX_MUDD_OBJECTS = 512;
        private const int MAX_LIGHTS       = 16;

        public Compositer()
        {
            Shader = ShaderHelper.ShaderLoader.LoadShaderWithIncludes(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/composite.fs",
                "Assets/Shaders/Compiled/compiled.fs"
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

            locLightCount       = Raylib.GetShaderLocation(Shader, "lightCount");
            locLightPositions   = Raylib.GetShaderLocation(Shader, "lightPositions");
            locLightRadii       = Raylib.GetShaderLocation(Shader, "lightRadii");
            locLightIntensities = Raylib.GetShaderLocation(Shader, "lightIntensities");
            locLightColors      = Raylib.GetShaderLocation(Shader, "lightColors");

            locSpriteData = Raylib.GetShaderLocation(Shader, "u_SpriteData");
            locMaxSprites = Raylib.GetShaderLocation(Shader, "u_MaxSprites");

            // Create float32 RGBA texture
            Image img = Raylib.GenImageColor(MAX_MUDD_OBJECTS, ROWS_PER_SPRITE, Color.Blank);
            Raylib.ImageFormat(ref img, PixelFormat.UncompressedR32G32B32A32);
            spriteDataTex = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
        }

        public void OnLoad(CameraSprite camera)
        {
            Camera = camera;
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

            int lightCount  = Math.Min(lights?.Count ?? 0, MAX_LIGHTS);
            int objectCount = Math.Min(muddObjects?.Count ?? 0, MAX_MUDD_OBJECTS);

            float[] lightPosArray       = new float[MAX_LIGHTS * 3];
            float[] lightRadiusArray    = new float[MAX_LIGHTS];
            float[] lightIntensityArray = new float[MAX_LIGHTS];
            float[] lightColorArray     = new float[MAX_LIGHTS * 3];

            Raylib.BeginShaderMode(Shader);

            if (locBaseAtlas != -1 && baseAtlas.Id != 0)
                Raylib.SetShaderValueTexture(Shader, locBaseAtlas, baseAtlas);
            if (locNormalsAtlas != -1 && normalAtlas.Id != 0)
                Raylib.SetShaderValueTexture(Shader, locNormalsAtlas, normalAtlas);
            if (locDepthAtlas != -1 && depthAtlas.Id != 0)
                Raylib.SetShaderValueTexture(Shader, locDepthAtlas, depthAtlas);

            // Lights
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

            // Sprite data rows

            // 8 rows * 4 floats per row per sprite
            float[] spriteBuffer = new float[MAX_MUDD_OBJECTS * ROWS_PER_SPRITE * 4];

            // Clear buffer to avoid stale texels
            Array.Clear(spriteBuffer, 0, spriteBuffer.Length);
            for (int objectIndex = 0; objectIndex < objectCount; objectIndex++)
            {
                var obj = muddObjects[objectIndex];
                var pos = obj.GetPosition();

                Vector2 world2D      = new Vector2(pos.X, (pos.Y / 2f) + pos.Z);
                Vector2 screenCenter = Raylib.GetWorldToScreen2D(world2D, Camera.Camera);

                Vector2 scaledFrame = obj.Size * Camera.Camera.Zoom;
                Vector2 bottomLeft  = screenCenter - scaledFrame * 0.5f;
                int rowIndex = objectIndex * 32;
                spriteBuffer[rowIndex + 0] = pos.X;
                spriteBuffer[rowIndex + 1] = pos.Y;
                spriteBuffer[rowIndex + 2] = pos.Z;
                spriteBuffer[rowIndex + 3] = 0f;
                spriteBuffer[rowIndex + 4] = bottomLeft.X;
                spriteBuffer[rowIndex + 5] = bottomLeft.Y;
                spriteBuffer[rowIndex + 6] = 0f;
                spriteBuffer[rowIndex + 7] = 0f;
                spriteBuffer[rowIndex + 8]  = obj.Size.X;
                spriteBuffer[rowIndex + 9]  = obj.Size.Y;
                spriteBuffer[rowIndex + 10] = 0f;
                spriteBuffer[rowIndex + 11] = 0f;
                spriteBuffer[rowIndex + 12] = obj.SheetLocation.X;
                spriteBuffer[rowIndex + 13] = obj.SheetLocation.Y;
                spriteBuffer[rowIndex + 14] = 0f;
                spriteBuffer[rowIndex + 15] = 0f;
                spriteBuffer[rowIndex + 16] = obj.AtlasOrigin.X;
                spriteBuffer[rowIndex + 17] = obj.AtlasOrigin.Y;
                spriteBuffer[rowIndex + 18] = 0f;
                spriteBuffer[rowIndex + 19] = 0f;
                spriteBuffer[rowIndex + 20] = obj.VisibleOffset.X;
                spriteBuffer[rowIndex + 21] = obj.VisibleOffset.Y;
                spriteBuffer[rowIndex + 22] = 0f;
                spriteBuffer[rowIndex + 23] = 0f;
                spriteBuffer[rowIndex + 24] = obj.VisibleSize.X;
                spriteBuffer[rowIndex + 25] = obj.VisibleSize.Y;
                spriteBuffer[rowIndex + 26] = 0f;
                spriteBuffer[rowIndex + 27] = 0f;
                spriteBuffer[rowIndex + 28] = obj.isFlat ? 1f : 0f;
                spriteBuffer[rowIndex + 29] = 0f;
                spriteBuffer[rowIndex + 30] = 0f;
                spriteBuffer[rowIndex + 31] = 0f;
            }

            // Upload light arrays
            if (locLightCount != -1)
                Raylib.SetShaderValue(Shader, locLightCount, lightCount, ShaderUniformDataType.Int);

            if (locLightPositions != -1 && lightCount > 0)
                Raylib.SetShaderValueV(Shader, locLightPositions, lightPosArray, ShaderUniformDataType.Vec3, lightCount);

            if (locLightRadii != -1 && lightCount > 0)
                Raylib.SetShaderValueV(Shader, locLightRadii, lightRadiusArray, ShaderUniformDataType.Float, lightCount);

            if (locLightIntensities != -1 && lightCount > 0)
                Raylib.SetShaderValueV(Shader, locLightIntensities, lightIntensityArray, ShaderUniformDataType.Float, lightCount);

            if (locLightColors != -1 && lightCount > 0)
                Raylib.SetShaderValueV(Shader, locLightColors, lightColorArray, ShaderUniformDataType.Vec3, lightCount);

            // --- FIXED FLOAT32 UPLOAD ---
            byte[] spriteBytes = new byte[spriteBuffer.Length * sizeof(float)];
            Buffer.BlockCopy(spriteBuffer, 0, spriteBytes, 0, spriteBytes.Length);
            Raylib.UpdateTexture(spriteDataTex, spriteBytes);

            Raylib.SetShaderValueTexture(Shader, locSpriteData, spriteDataTex);
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