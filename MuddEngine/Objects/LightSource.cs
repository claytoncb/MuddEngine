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

        public LightSource(Vector3 pos, float radius, float intensity, Raylib_cs.Color color)
        {
            Position = pos;
            Radius = radius;
            Intensity = intensity;
            Color = color;
            MuddEngine.RegisterLight(this);
        }
        public void DestroySelf()
        {
            MuddEngine.UnregisterLight(this);
        }
    }
}