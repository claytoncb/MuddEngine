using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class Sprite : MuddGroup
    {
        public string Tag = "";
        public int Facing = 0;
        public float Speed = 0;
        public float MinSpeed = 0;
        public float Acceleration;
        public float Height = 12f;
        public int Row;
        public bool Upright;
        public Raylib_cs.Rectangle src;
        public Raylib_cs.Rectangle dest;
        public int State = 0;
        public float StateChange;
        public int StateIndex = 0;

        public Sprite(string Id, Vector3 Position, float Speed, bool upright=true) : base(Id, Position)
        {
            Size = new Vector2(32,32);
            this.Position = Position;
            this.Speed = Speed;
            this.Upright = upright;
            MinSpeed = Speed;
            Acceleration = 1000f;
        }
        public virtual void Update(float dt, float t, Vector3 movement)
        {
            Position += new Vector3(movement.X,movement.Y,movement.Z);
            base.Update(dt,t);
        }
    }
}