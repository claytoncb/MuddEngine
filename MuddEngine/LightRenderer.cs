using System;
using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class LightingRenderer
    {
        public Shader Shader;

        public int locLightPos;
        public int locLightColor;
        public int locLightRadius;
        public int locLightIntensity;
        public int locSpecularStrength;
        public int locShininess;
        public int locAmbient;

        // --- Rim Lighting Uniforms ---
        public int locRimIntensity;
        public int locRimPower;
        public int locRimColor;
        public int locSpritePos;
        public int locSpriteZ;
        public int locNormal;
        public int locDepthMask;
        public int locScreenSize;
        public int locCameraTarget;
        public int locCameraZoom;
        public int locMaxWorldZ;
        public LightingRenderer()
        {
            Shader = Raylib.LoadShader("light.vs", "Assets/Shaders/light.fs");

            locLightPos       = Raylib.GetShaderLocation(Shader, "lightPos");
            locLightColor     = Raylib.GetShaderLocation(Shader, "lightColor");
            locLightRadius    = Raylib.GetShaderLocation(Shader, "lightRadius");
            locLightIntensity = Raylib.GetShaderLocation(Shader, "lightIntensity");
            locSpecularStrength = Raylib.GetShaderLocation(Shader, "specularStrength");
            locShininess        = Raylib.GetShaderLocation(Shader, "shininess");
            locAmbient          = Raylib.GetShaderLocation(Shader, "ambient");

            locRimIntensity = Raylib.GetShaderLocation(Shader, "rimIntensity");
            locRimPower     = Raylib.GetShaderLocation(Shader, "rimPower");
            locRimColor     = Raylib.GetShaderLocation(Shader, "rimColor");

            locNormal    = Raylib.GetShaderLocation(Shader, "normalMap");
            locDepthMask = Raylib.GetShaderLocation(Shader, "depthMask");

            // Explicitly bind sampler indices: normalMap -> unit 0, depthMask -> unit 1
            if (locNormal != -1 && locDepthMask != -1)
            {
                int[] unit = new int[] { 0 };
                Raylib.SetShaderValue(Shader, locNormal, unit, ShaderUniformDataType.Int);

                unit[0] = 1;
                Raylib.SetShaderValue(Shader, locDepthMask, unit, ShaderUniformDataType.Int);
            }

            locScreenSize = Raylib.GetShaderLocation(Shader, "screenSize");
            locCameraTarget = Raylib.GetShaderLocation(Shader, "cameraTarget");
            locCameraZoom   = Raylib.GetShaderLocation(Shader, "cameraZoom");
            locMaxWorldZ   = Raylib.GetShaderLocation(Shader, "maxWorldZ");

        }



        public void BeginLight(
    LightSource light,
    RenderTexture2D depthBuffer,
    RenderTexture2D normalBuffer,
    Camera2D camera,
    Vector2 screenSize
)
{
    // Light color
    Vector3 color = new(
        light.Color.R / 255f,
        light.Color.G / 255f,
        light.Color.B / 255f
    );

    // Light world position
    Vector3 lightPos3D = new(
        light.Position.X,
        light.Position.Y,
        light.Position.Z
    );

    // Set light uniforms
    Raylib.SetShaderValue(Shader, locLightPos,       lightPos3D, ShaderUniformDataType.Vec3);
    Raylib.SetShaderValue(Shader, locLightColor,     color,      ShaderUniformDataType.Vec3);
    Raylib.SetShaderValue(Shader, locLightRadius,    light.Radius,    ShaderUniformDataType.Float);
    Raylib.SetShaderValue(Shader, locLightIntensity, light.Intensity, ShaderUniformDataType.Float);

    Raylib.SetShaderValue(Shader, locSpecularStrength, 0.1f,  ShaderUniformDataType.Float);
    Raylib.SetShaderValue(Shader, locShininess,        256f,  ShaderUniformDataType.Float);
    Raylib.SetShaderValue(Shader, locAmbient,          0.05f, ShaderUniformDataType.Float);

    // Rim lighting
    Raylib.SetShaderValue(Shader, locRimIntensity, light.RimIntensity, ShaderUniformDataType.Float);
    Raylib.SetShaderValue(Shader, locRimPower,     light.RimPower,     ShaderUniformDataType.Float);

    Vector3 rimColor = new(
        light.RimColor.R / 255f,
        light.RimColor.G / 255f,
        light.RimColor.B / 255f
    );
    Raylib.SetShaderValue(Shader, locRimColor, rimColor, ShaderUniformDataType.Vec3);

    // Camera uniforms
    Raylib.SetShaderValue(Shader, locCameraTarget, camera.Target, ShaderUniformDataType.Vec2);
    Raylib.SetShaderValue(Shader, locCameraZoom,   camera.Zoom,   ShaderUniformDataType.Float);

    // Screen size
    Raylib.SetShaderValue(Shader, locScreenSize, screenSize, ShaderUniformDataType.Vec2);

    // Max world Z (for depth normalization)
    float maxWorldZ = 100f;
    Raylib.SetShaderValue(Shader, locMaxWorldZ, maxWorldZ, ShaderUniformDataType.Float);

    // Bind textures
    Raylib.SetShaderValueTexture(Shader, locNormal,    normalBuffer.Texture);
    Raylib.SetShaderValueTexture(Shader, locDepthMask, depthBuffer.Texture);

    // Additive blending for lights
    Raylib.BeginBlendMode(BlendMode.Additive);
}

        public void EndLight()
        {
            Raylib.EndBlendMode();
        }
    }
}