using System.Numerics;
using Raylib_cs;

namespace MuddEngine.DemoGame
{
    public class Player(string Id, Vector3 pos) : MuddEngine.Sprite(Id, pos)
    {
        public float maxWalkingSpeed = 42;
        public float maxDashingSpeed = 96;
        public float baseAcceleration = 1000;
        public float friction = 8f;
        public float Stamina = 100f;
        public float MaxStamina = 100f;
        public float StaminaDepletion = -20f;
        public float StaminaRegeneration = 20f;
        public bool Dashing = false;
        private const int frameCount = 7;
        public override void Update(float dt, float t)
        {
            Vector2 direction = MuddEngine.Keyboard.Direction();
            Facing = GetFacing(direction);

            // Movement
            Acceleration = new Vector3(direction.X, direction.Y, 0) * baseAcceleration;
            HandleDash(dt);

            // 1. Apply acceleration
            Velocity += Acceleration * dt;

            // 2. Apply friction only when not moving
            Velocity *= MathF.Exp(-friction * dt);
            if (Velocity.Length() < 0.5f)
                    Velocity = Vector3.Zero;
            
            // 3. Clamp velocity
            float maxSpeed = Dashing ? maxDashingSpeed : maxWalkingSpeed;
            float speed = Velocity.Length();
            if (speed > maxSpeed)
                Velocity = Vector3.Normalize(Velocity) * maxSpeed;

            // 4. Update position
            Position += Velocity * dt;
            // Animation
            FrameHandler(t, direction);

            // Stamina
            Stamina += (Dashing ? StaminaDepletion : StaminaRegeneration) * dt;
            Stamina = Math.Clamp(Stamina, 0f, MaxStamina);
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
        private void HandleDash(float dt)
        {
            bool holdingSpace = MuddEngine.Keyboard.IsHoldingSpace();
            bool moving = Velocity.LengthSquared() > 0.01f;
            bool hasStamina = Stamina > 0f;

            // --- Start dash ---
            if (holdingSpace && moving && hasStamina && (Dashing || (Stamina == MaxStamina)))
                Dashing = true;

            // --- Stop dash ---
            if (!holdingSpace || Stamina <= 0f || !moving)
                Dashing = false;

            // --- Stamina update ---
            if (Dashing)
                Stamina += StaminaDepletion * dt;
            else
                Stamina += StaminaRegeneration * dt;

            Stamina = Math.Clamp(Stamina, 0f, MaxStamina);
        }
        private void FrameHandler(float t, Vector2 direction)
{
    // 1. Determine animation row
    int newState = Dashing ? 2: (direction.Length()<0.5f? 0:1);
    if (State != newState)
    {
        StateChange = t;
        State = newState;
    }
    StateIndex = (int)Math.Floor((t - StateChange)*(State==0?6:12))%frameCount;
    SheetLocation = new Vector2(
        StateIndex * 32,
        Facing * 32 * 3 + 32 * State
    );
}


    }
}

