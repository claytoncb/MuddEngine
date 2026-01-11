using System.Numerics;
using Raylib_cs;
using Color = Raylib_cs.Color;
namespace MuddEngine.MuddEngine
{
    public class Compositer
    {
        public Shader Shader;

        public int locBaseTex;
        public int locNormalTex;
        public int locDepthTex;
        public int locScreenSize;
        public int locMaxZ;
        public int locMaxY;
        public int locLightCount;
        public int locLightPos;
        public int locLightColor;

        public int locCameraTarget;
        public int locCameraOffset;
        public int locCameraZoom;
        public int locCameraPosition;
        public Compositer()
        {
            Shader = Raylib.LoadShader(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/composite.fs"
            );

            locBaseTex     = Raylib.GetShaderLocation(Shader, "baseTex");
            locNormalTex   = Raylib.GetShaderLocation(Shader, "normalTex");
            locDepthTex    = Raylib.GetShaderLocation(Shader, "depthTex");

            locScreenSize  = Raylib.GetShaderLocation(Shader, "screenSize");
            locMaxZ        = Raylib.GetShaderLocation(Shader, "maxZ");
            locMaxY        = Raylib.GetShaderLocation(Shader, "maxY");

            locLightCount  = Raylib.GetShaderLocation(Shader, "lightCount");
            locLightPos    = Raylib.GetShaderLocation(Shader, "lightPos");
            locLightColor  = Raylib.GetShaderLocation(Shader, "lightColor");

            locCameraTarget = Raylib.GetShaderLocation(Shader, "cameraTarget");
            locCameraOffset = Raylib.GetShaderLocation(Shader, "cameraOffset");
            locCameraZoom   = Raylib.GetShaderLocation(Shader, "cameraZoom");
            locCameraPosition = Raylib.GetShaderLocation(Shader, "cameraPos");

            // Sampler bindings
            if (locBaseTex != -1)
                Raylib.SetShaderValue(Shader, locBaseTex, new int[] { 0 }, ShaderUniformDataType.Int);

            if (locDepthTex != -1)
                Raylib.SetShaderValue(Shader, locDepthTex, new int[] { 1 }, ShaderUniformDataType.Int);

            if (locNormalTex != -1)
                Raylib.SetShaderValue(Shader, locNormalTex, new int[] { 2 }, ShaderUniformDataType.Int);
        }

        public void Draw(
            RenderTexture2D baseBuffer,
            RenderTexture2D normalBuffer,
            RenderTexture2D depthBuffer,
            Vector2 screenSize,
            float maxY,
            float maxZ,
            List<LightSource> lights,
            CameraSprite camera
        )
        {
            int count = Math.Min(lights.Count, 32); // MAX_LIGHTS

            // Build padded arrays (always 32 lights worth of memory)
            float[] posData   = new float[32 * 3];
            float[] colorData = new float[32 * 3];

            for (int i = 0; i < count; i++)
            {
                var p = lights[i].Position;
                posData[i * 3 + 0] = p.X;
                posData[i * 3 + 1] = p.Y;
                posData[i * 3 + 2] = p.Z;

                var c = lights[i].Color;
                colorData[i * 3 + 0] = c.R / 255f;
                colorData[i * 3 + 1] = c.G / 255f;
                colorData[i * 3 + 2] = c.B / 255f;
            }

            Raylib.BeginShaderMode(Shader);

            // Core uniforms
            Raylib.SetShaderValue(Shader, locScreenSize, screenSize, ShaderUniformDataType.Vec2);
            Raylib.SetShaderValue(Shader, locMaxZ, new float[] { maxZ }, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(Shader, locMaxY, new float[] { maxY }, ShaderUniformDataType.Float);

            // Camera uniforms
            Raylib.SetShaderValue(Shader, locCameraTarget, camera.Camera.Target, ShaderUniformDataType.Vec2);
            Raylib.SetShaderValue(Shader, locCameraOffset, camera.Camera.Offset, ShaderUniformDataType.Vec2);
            Raylib.SetShaderValue(Shader, locCameraZoom, camera.Camera.Zoom, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(Shader, locCameraPosition, camera.Position, ShaderUniformDataType.Vec3);

            // Light arrays (always upload full 32 slots)
            Raylib.SetShaderValueV(Shader, locLightPos,   posData,   ShaderUniformDataType.Vec3, 32);
            Raylib.SetShaderValueV(Shader, locLightColor, colorData, ShaderUniformDataType.Vec3, 32);
            Raylib.SetShaderValue(Shader,  locLightCount, count,     ShaderUniformDataType.Int);

            // Bind textures
            Raylib.SetShaderValueTexture(Shader, locBaseTex,   baseBuffer.Texture);
            Raylib.SetShaderValueTexture(Shader, locNormalTex, normalBuffer.Texture);
            Raylib.SetShaderValueTexture(Shader, locDepthTex,  depthBuffer.Texture);

            // Fullscreen composite
            Raylib.DrawRectangle(0, 0, (int)screenSize.X, (int)screenSize.Y, Color.White);

            Raylib.EndShaderMode();
        }
    }
}
