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
        public int locMetaTex;
        public int locScreenSize;

        public int locLightCount;
        public int locLightPos;
        public int locLightColor;

        public int locSpriteWorldPosArray;
        public int locSpriteCount;

        public Compositer()
        {
            Shader = Raylib.LoadShader(
                "Assets/Shaders/vertexShader.vs",
                "Assets/Shaders/composite.fs"
            );

            locBaseTex    = Raylib.GetShaderLocation(Shader, "baseTex");
            locNormalTex  = Raylib.GetShaderLocation(Shader, "normalTex");
            locDepthTex   = Raylib.GetShaderLocation(Shader, "depthTex");
            locMetaTex    = Raylib.GetShaderLocation(Shader, "metaTex");

            locScreenSize = Raylib.GetShaderLocation(Shader, "screenSize");

            locLightCount = Raylib.GetShaderLocation(Shader, "lightCount");
            locLightPos   = Raylib.GetShaderLocation(Shader, "lightPos");
            locLightColor = Raylib.GetShaderLocation(Shader, "lightColor");

            locSpriteWorldPosArray = Raylib.GetShaderLocation(Shader, "spriteWorldPosArray");
            locSpriteCount         = Raylib.GetShaderLocation(Shader, "spriteCount");

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
            RenderTexture2D metaBuffer,
            Vector2 screenSize,
            List<LightSource> lights,
            List<Sprite2D> sprites
        )
        {
            int count = Math.Min(lights.Count, 32);

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

            float[] spriteWorldPosArray = new float[512 * 3];
            for (int i = 0; i < sprites.Count && i < 512; i++)
            {
                var s = sprites[i];
                spriteWorldPosArray[i * 3 + 0] = s.Position.X;
                spriteWorldPosArray[i * 3 + 1] = s.Position.Y;
                spriteWorldPosArray[i * 3 + 2] = s.Position.Z;
            }

            Raylib.BeginShaderMode(Shader);

            Raylib.SetShaderValue(Shader, locScreenSize, screenSize, ShaderUniformDataType.Vec2);

            Raylib.SetShaderValueV(Shader, locLightPos,   posData,   ShaderUniformDataType.Vec3, 32);
            Raylib.SetShaderValueV(Shader, locLightColor, colorData, ShaderUniformDataType.Vec3, 32);
            Raylib.SetShaderValue(Shader,  locLightCount, count,     ShaderUniformDataType.Int);

            Raylib.SetShaderValueV(Shader, locSpriteWorldPosArray, spriteWorldPosArray, ShaderUniformDataType.Vec3, 512);
            Raylib.SetShaderValue(Shader,  locSpriteCount, sprites.Count, ShaderUniformDataType.Int);

            Raylib.SetShaderValueTexture(Shader, locBaseTex,   baseBuffer.Texture);
            Raylib.SetShaderValueTexture(Shader, locNormalTex, normalBuffer.Texture);
            Raylib.SetShaderValueTexture(Shader, locDepthTex,  depthBuffer.Texture);
            Raylib.SetShaderValueTexture(Shader, locMetaTex,   metaBuffer.Texture);

            Raylib.DrawRectangle(0, 0, (int)screenSize.X, (int)screenSize.Y, Color.White);

            Raylib.EndShaderMode();
        }
    }
}