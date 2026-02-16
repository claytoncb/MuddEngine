using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;

namespace MuddEngine.MuddEngine
{
    public class ParticlePass
    {
        public Shader Shader;
        public CameraSprite Camera;
        public void Load(CameraSprite camera)
        {
            Camera = camera;
        }
        public void UnLoad()
        {
            Raylib.UnloadShader(Shader);
        }
        public void Draw()
        {
            
        }
    }
}