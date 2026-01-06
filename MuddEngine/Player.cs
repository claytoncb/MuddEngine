using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class Player : DashingEntity
    {
        public Player(Vector3 pos, string tag, int Row, float speed)
            : base(pos, tag, Row, speed)
        {
        }

        public override void Update(float dt, Keyboard keyboard)
        {
            Vector2 direction = keyboard.Direction();

            Movement = keyboard.Movement(direction, Movement, MinSpeed, Acceleration, dt);

            Facing = GetFacing(direction);

            base.Update(dt, keyboard);
        }
    }
}

