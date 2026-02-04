using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class CameraSprite
    {
        public Camera2D Camera;
        public Vector3 Position;
        private MuddObject Target;

        public CameraSprite(MuddObject Target, Vector2 screenSize)
        {
            this.Target = Target;
            // Start at the player's position
            Position = Target.GetPosition();
            Camera = new Camera2D();
            Camera.Target   = new Vector2(Position.X, (Position.Y / 2) + Position.Z);
            Camera.Offset   = screenSize / 2f;   // center of screen
            Camera.Rotation = 0f;
            Camera.Zoom     = 4f;
        }

        public void Update(float dt, float t)
        {
            // Follow the player
            Position = Target.GetPosition();
            Camera.Target = new Vector2(Position.X, (Position.Y / 2) + Position.Z);
        }
    }
}