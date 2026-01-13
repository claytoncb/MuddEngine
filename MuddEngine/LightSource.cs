using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class LightSource
    {
        public Vector3 Position;
        public float Radius;
        public float Intensity;
        public Raylib_cs.Color Color;

        // --- Rim Lighting ---
        public float RimIntensity = 0.1f;   // how bright the rim is
        public float RimPower = 1.0f;       // how sharp the rim edge is
        public Raylib_cs.Color RimColor = Raylib_cs.Color.White;

        public Texture2D IconTexture;
        public float IconScale = 4f;

        public LightSource(Vector3 pos, float radius, float intensity, Raylib_cs.Color color, string lightName)
        {
            Position = pos;
            Radius = radius;
            Intensity = intensity;
            Color = color;
            IconTexture = Raylib.LoadTexture($"Assets/LightSources/{lightName}.png");
        }

        public void DrawBase()
        {
            if (IconTexture.Id == 0) return;

            float size = IconTexture.Width * IconScale;

            float drawX = Position.X - size * 0.5f;
            float drawY = -(Position.Y / 2) - Position.Z - size * 0.5f;

            Raylib_cs.Rectangle src = new(0, 0, IconTexture.Width, IconTexture.Height);
            Raylib_cs.Rectangle dest = new(drawX, drawY, size, size);

            Raylib.DrawTexturePro(
                IconTexture,
                src,
                dest,
                Vector2.Zero,
                0f,
                Raylib_cs.Color.White
            );
        }


    }
}