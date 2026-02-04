using System.Numerics;
using Raylib_cs;

namespace MuddEngine.DemoGame
{
    public class Player(string Id, Vector3 pos, float speed) : MuddEngine.Sprite(Id, pos, speed)
    {
        public float DashSpeed = 2.5f;
        public float Stamina = 100f;
        public float MaxStamina = 100f;
        public float StaminaDepletion = -20f;
        public float StaminaRegeneration = 50f;
        public float DashAcceleration = 1000f;
        public bool Dashing = false;
        public override void Update(float dt, float t)
        {
            Vector2 direction = MuddEngine.Keyboard.Direction();
            Movement = MuddEngine.Keyboard.Movement(direction, Movement, MinSpeed, Acceleration, dt);
            Facing = GetFacing(direction);
            
            // Entity handles stamina + dash speed
            (Speed, Dashing) = MuddEngine.Keyboard.Speed(
                Movement.Length() >= MinSpeed,
                Dashing,
                Speed,
                DashSpeed,
                Stamina,
                DashAcceleration,
                dt
            );
            int newState = Dashing ? 2: (direction.Length()<0.5f? 0:1);
            if (State != newState)
            {
                StateChange = t;
                State = newState;
            }
            StateIndex = (int)Math.Floor((t - StateChange)*(State==0?6:12))%7;
            Stamina += (Dashing ? StaminaDepletion : StaminaRegeneration) * dt;
            Stamina = Math.Clamp(Stamina, 0f, MaxStamina);
            SheetLocation = new Vector2(StateIndex*32,Facing*32*3 + 32*State);
            base.Update(dt, t, Movement*Speed);
        }
        protected int GetFacing(Vector2 dir)
        {
            // Optional: deadzone to avoid jitter
            const float eps = 0.0001f;
            int spriteFacing = 0;
            // Normalize if needed
            if (dir.LengthSquared() > 1f)
                dir = Vector2.Normalize(dir);
            if (dir.Length() == 0f)
            {
                return Facing;
            }
                //Left/Right
            if (dir.X > eps)
            {
                spriteFacing=2;
                if (dir.Y > eps) spriteFacing -=1;
            }
            else if (dir.X < -eps)
            {
                spriteFacing=4;
                if (dir.Y > eps) spriteFacing += 1;
            }
            //no X entered, up or down
            else if (dir.Y < -eps)
                spriteFacing=3;
            else if (dir.Y > eps)
                spriteFacing=0;
            
            // Default fallback (no movement)
            return spriteFacing; // or whatever your idle facing is
        }
    }
}

