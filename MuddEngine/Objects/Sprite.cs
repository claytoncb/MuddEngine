using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class Sprite : MuddGroup
    {
        public string Tag = "";
        public int Facing = 0;
        public float Height = 12f;
        public int Row;
        public bool Upright;
        public Raylib_cs.Rectangle src;
        public Raylib_cs.Rectangle dest;
        public int State = 0;
        public float StateChange;
        public int StateIndex = 0;

        public Sprite(string Id, Vector3 Position, bool Upright=true) : base(Id, Position)
        {
            Size = new Vector2(32,32);
            this.Position = Position;
            this.Upright = Upright;
        }
    }
}