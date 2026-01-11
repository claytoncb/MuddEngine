using System.Numerics;

namespace MuddEngine.MuddEngine
{
    public class DashingEntity : Sprite2D
    {
        public float DashSpeed = 5f;
        public float Stamina;
        public float MaxStamina = 100f;
        public float StaminaDepletion = -500f;
        public float StaminaRegeneration = 50f;
        public float DashAcceleration = 1000f;
        public bool Dashing = false;

        public DashingEntity(Vector3 pos, string tag, int Row, float speed, string AltasName)
            : base(pos, tag, Row, speed, AltasName)
        {
            Stamina = MaxStamina;
        }

        public override void Update(float dt, Keyboard keyboard)
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

            Stamina += (Dashing ? StaminaDepletion : StaminaRegeneration) * dt;
            Stamina = Math.Clamp(Stamina, 0f, MaxStamina);

            base.Update(dt, keyboard);
        }
    }
}