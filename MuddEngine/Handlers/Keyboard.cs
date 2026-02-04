using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public static class Keyboard
    {
        private static HashSet<Keys> keysDown = new();
        private static KeyboardKey lastHorizontal;
        private static KeyboardKey lastVertical;
        public static int DebugMode = 6;

        public static void KeyDown(object sender, KeyEventArgs e)
        {
            keysDown.Add(e.KeyCode);
        }

        public static void KeyUp(object sender, KeyEventArgs e)
        {
            keysDown.Remove(e.KeyCode);
        }

        public static bool IsKeyDown(Keys k) => keysDown.Contains(k);
        // Call once per frame to update all keyboard-derived state
        public static void Update()
        {
            UpdateDirectionMemory();
            UpdateDebugMode();
        }

        public static void UpdateDirectionMemory()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.A)) lastHorizontal = KeyboardKey.A;
            if (Raylib.IsKeyPressed(KeyboardKey.D)) lastHorizontal = KeyboardKey.D;
            if (Raylib.IsKeyPressed(KeyboardKey.W)) lastVertical = KeyboardKey.W;
            if (Raylib.IsKeyPressed(KeyboardKey.S)) lastVertical = KeyboardKey.S;
        }
        // Sets DebugMode to 1, 2, or 3 when those keys are pressed (top row or numpad)
        private static void UpdateDebugMode()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.One) || Raylib.IsKeyPressed(KeyboardKey.Kp1))
            {
                DebugMode = 1;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Two) || Raylib.IsKeyPressed(KeyboardKey.Kp2))
            {
                DebugMode = 2;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Three) || Raylib.IsKeyPressed(KeyboardKey.Kp3))
            {
                DebugMode = 3;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Four) || Raylib.IsKeyPressed(KeyboardKey.Kp4))
            {
                DebugMode = 4;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Five) || Raylib.IsKeyPressed(KeyboardKey.Kp5))
            {
                DebugMode = 5;
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Six) || Raylib.IsKeyPressed(KeyboardKey.Kp6))
            {
                DebugMode = 6;
            }
        }

        public static Vector2 Direction()
        {
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
        public static Vector3 Movement (Vector2 Direction, Vector3 Movement, float Speed, float Acceleration, float dt)
        {
            
            float length = Direction.Length();
            Vector2 Movement2D = new Vector2(Movement.X, Movement.Y);
            if (length > 0.1f)
            {
                Direction /= length;   // normalize
                Movement2D += Direction * Acceleration * dt;
                
            }
            else
            {
                 Movement2D += -Movement2D * Acceleration * dt * .1f;
            }
            
            if (Movement2D.Length() > Speed)
            {
                Movement2D = Vector2.Normalize(Movement2D) * Speed;
            }
            return new Vector3(Movement2D.X, Movement2D.Y, 0.0f);

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
    }
}