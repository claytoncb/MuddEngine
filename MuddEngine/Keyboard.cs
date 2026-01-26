using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class Keyboard
    {
        private HashSet<Keys> keysDown = new();
        private KeyboardKey lastHorizontal;
        private KeyboardKey lastVertical;

        public void KeyDown(object sender, KeyEventArgs e)
        {
            keysDown.Add(e.KeyCode);
        }

        public void KeyUp(object sender, KeyEventArgs e)
        {
            keysDown.Remove(e.KeyCode);
        }

        public bool IsKeyDown(Keys k) => keysDown.Contains(k);
        public void UpdateDirectionMemory()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.A)) lastHorizontal = KeyboardKey.A;
            if (Raylib.IsKeyPressed(KeyboardKey.D)) lastHorizontal = KeyboardKey.D;
            if (Raylib.IsKeyPressed(KeyboardKey.W)) lastVertical = KeyboardKey.W;
            if (Raylib.IsKeyPressed(KeyboardKey.S)) lastVertical = KeyboardKey.S;
        }
        public Vector2 Direction()
        {
            UpdateDirectionMemory();
            Vector2 input = Vector2.Zero;

            bool left  = Raylib.IsKeyDown(KeyboardKey.A);
            bool right = Raylib.IsKeyDown(KeyboardKey.D);
            bool up    = Raylib.IsKeyDown(KeyboardKey.W);
            bool down  = Raylib.IsKeyDown(KeyboardKey.S);

            // Horizontal
            if (left && !right) input.X = -1;
            else if (right && !left) input.X = 1;
            else if (left && right)
            {
                // both pressed → use last pressed
                input.X = (lastHorizontal == KeyboardKey.A) ? -1 : 1;
            }

            // Vertical
            if (up && !down) input.Y = 1;
            else if (down && !up) input.Y = -1;
            else if (up && down)
            {
                input.Y = (lastVertical == KeyboardKey.W) ? -1 : 1;
            }

            // Normalize diagonal
            if (input.Length() > 1f)
                input = Vector2.Normalize(input);

            return input;

        }
        public Vector2 Movement (Vector2 Direction, Vector2 Movement, float Speed, float Acceleration, float dt)
        {
            float length = Direction.Length();

            if (length > 0.1f)
            {
                Direction /= length;   // normalize
                Movement += Direction * Acceleration * dt;
            } else Movement += -Movement * Acceleration * dt * .1f;
                
            if (Movement.Length() > Speed)
                Movement = Vector2.Normalize(Movement) * Speed;

            return Movement;

        }

        public static Tuple<float, bool> Speed(bool Moving, bool Dashing, float Speed, float SprintSpeed, float Stamina, float DashAcceleration, float dt)
        {
            bool holdingSpace = Raylib.IsKeyDown(KeyboardKey.Space);
            bool hasStamina = Stamina == 100f;
            if (holdingSpace && hasStamina && Moving) Dashing = true;
            if (Stamina == 0 || !holdingSpace) Dashing = false;
            Speed += (Dashing?1f:-1f) * DashAcceleration * dt;
            return new(Math.Max(Math.Min(Speed,SprintSpeed), holdingSpace?1.001f:1f), Dashing);
        }

        internal void Update()
        {
            return;
        }
    }
}