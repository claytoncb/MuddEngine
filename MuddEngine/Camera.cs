using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class CameraSprite
    {
        public Camera2D Camera;
        public Vector3 Position;
        private Player Player;

        public CameraSprite(Player player, Vector2 screenSize)
        {
            Player = player;

            // Start at the player's position
            Position = player.Position;

            Camera = new Camera2D();
            Camera.Target   = new Vector2(Position.X, (Position.Y/2) - (Position.Z*8));
            Camera.Offset   = screenSize / 2f;   // center of screen
            Camera.Rotation = 0f;
            Camera.Zoom     = 1f;
        }

        public void Update(float dt)
        {
            // Follow the player
            Position = Player.Position;
            Camera.Target = new Vector2(Position.X, (Position.Y/2) - (Position.Z*8));
        }
    }
}