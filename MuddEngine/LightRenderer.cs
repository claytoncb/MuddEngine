using System;
using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class LightingRenderer
    {
        public Shader Shader;

        int locLightPos;
        int locLightColor;
        int locLightRadius;
        int locLightIntensity;
        int locSpecularStrength;
        int locShininess;
        int locAmbient;

        // --- Rim Lighting Uniforms ---
        int locRimIntensity;
        int locRimPower;
        int locRimColor;
        public int locSpritePos;
        public int locNormal;


        public LightingRenderer()
        {
            Shader = Raylib.LoadShader(null, "Assets/Shaders/light.fs");

            locLightPos         = Raylib.GetShaderLocation(Shader, "lightPos");
            locLightColor       = Raylib.GetShaderLocation(Shader, "lightColor");
            locLightRadius      = Raylib.GetShaderLocation(Shader, "lightRadius");
            locLightIntensity   = Raylib.GetShaderLocation(Shader, "lightIntensity");
            locSpecularStrength = Raylib.GetShaderLocation(Shader, "specularStrength");
            locShininess        = Raylib.GetShaderLocation(Shader, "shininess");
            locAmbient          = Raylib.GetShaderLocation(Shader, "ambient");
            locRimIntensity = Raylib.GetShaderLocation(Shader, "rimIntensity");
            locRimPower     = Raylib.GetShaderLocation(Shader, "rimPower");
            locRimColor     = Raylib.GetShaderLocation(Shader, "rimColor");
            locSpritePos = Raylib.GetShaderLocation(Shader, "spritePos");
            locNormal    = Raylib.GetShaderLocation(Shader, "normalMap");

        }

        public void BeginLight(LightSource light)
        {
            Vector3 color = new(
                light.Color.R / 255f,
                light.Color.G / 255f,
                light.Color.B / 255f
            );

            Vector3 lightPos3D = new(
                light.Position.X,
                light.Position.Y,
                light.Position.Z
            );

            Raylib.SetShaderValue(Shader, locLightPos,       lightPos3D, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(Shader, locLightColor,     color,      ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(Shader, locLightRadius,    light.Radius,    ShaderUniformDataType.Float);
            Raylib.SetShaderValue(Shader, locLightIntensity, light.Intensity, ShaderUniformDataType.Float);

            Raylib.SetShaderValue(Shader, locSpecularStrength, 0.1f,  ShaderUniformDataType.Float);
            Raylib.SetShaderValue(Shader, locShininess,        256f,  ShaderUniformDataType.Float);
            Raylib.SetShaderValue(Shader, locAmbient,          0.05f, ShaderUniformDataType.Float);

            Raylib.SetShaderValue(Shader, locRimIntensity, light.RimIntensity, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(Shader, locRimPower,     light.RimPower,     ShaderUniformDataType.Float);

            Vector3 rimColor = new(
                light.RimColor.R / 255f,
                light.RimColor.G / 255f,
                light.RimColor.B / 255f
            );
            Raylib.SetShaderValue(Shader, locRimColor, rimColor, ShaderUniformDataType.Vec3);

            // Only blending here
            Raylib.BeginBlendMode(BlendMode.Additive);
        }

        public void EndLight()
        {
            Raylib.EndBlendMode();
        }
    }
}