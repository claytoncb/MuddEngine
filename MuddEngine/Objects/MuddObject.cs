using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class MuddObject
    {
        public string Id;
        public Vector3 Position;
        public Vector3 Velocity = Vector3.Zero;
        public Vector3 Acceleration = Vector3.Zero;
        public Vector2 Size = new(32,32);
        public Vector2 VisibleSize = new(32,32);
        public Vector2 VisibleOffset = new(0,0);
        public bool isFlat = false;
        public Vector2 SheetLocation = Vector2.Zero;
        public Vector2 AtlasOrigin = Vector2.Zero;
        public MuddGroup Parent;
        public MuddObject(string Id, Vector3 Position)
        {
            this.Id = Id;
            this.Position = Position;
            MuddEngine.RegisterObject(this);
        }
        public Vector3 GetPosition()
        {
            return Position + (Parent!=null?Parent.GetPosition():Vector3.Zero);
        }
        public Vector3 GetVelocity()
        {
            return Velocity + (Parent!=null?Parent.GetVelocity():Vector3.Zero);
        }
        public Vector3 GetAcceleration()
        {
            return Acceleration + (Parent!=null?Parent.GetAcceleration():Vector3.Zero);
        }
        public void DestroySelf()
        {
            MuddEngine.UnregisterObject(this);
        }
        public virtual void Update(float dt, float t)
        {
            Velocity += Acceleration * dt;
            Position += Velocity*dt;
        }
    }
}