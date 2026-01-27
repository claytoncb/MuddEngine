using System.Numerics;

namespace MuddEngine.MuddEngine
{
    public class DashingEntity : Sprite2D
    {
        public float DashSpeed = 2.5f;
        public float Stamina;
        public float MaxStamina = 100f;
        public float StaminaDepletion = -20f;
        public float StaminaRegeneration = 50f;
        public float DashAcceleration = 1000f;
        public bool Dashing = false;

        public DashingEntity(Vector3 pos, string tag, int Row, float speed, string AltasName)
            : base(pos, tag, Row, speed, AltasName)
        {
            Stamina = MaxStamina;
        }

        public override void Update(float dt, float t, Keyboard keyboard)
        {
            // Entity handles stamina + dash speed
            (Speed, Dashing) = Keyboard.Speed(
                Movement.Length() >= MinSpeed,
                Dashing,
                Speed,
                DashSpeed,
                Stamina,
                DashAcceleration,
                dt
            );
            int newState = Dashing ? 2: (Movement.Length()<0.5f? 0:1);
            if (State != newState)
            {
                StateChange = t;
                State = newState;
            }
            StateIndex = (int)Math.Floor((t - StateChange)*(State==0?6:12))%7;
            Stamina += (Dashing ? StaminaDepletion : StaminaRegeneration) * dt;
            Stamina = Math.Clamp(Stamina, 0f, MaxStamina);

            base.Update(dt, t, keyboard);
        }
    }
}