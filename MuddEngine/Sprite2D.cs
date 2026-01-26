using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class Sprite2D
    {
        public Vector3 Position;
        public Vector2 Scale;
        public int Size = 32;
        public string Tag = "";
        public Texture2D SheetBase;
        public Texture2D SheetNormals;
        public Texture2D SheetParallax;
        public Texture2D SheetHeightMap;
        public Texture2D AlphaMask;
        public Bitmap Sprite = null;
        public int Facing = 0;
        public float Speed = 0;
        public Vector2 Movement = Vector2.Zero;
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

        public Sprite2D(Vector3 Position, string Tag, int Row, float Speed, string AtlasName, bool upright=true)
        {
            this.Position = Position;
            this.Scale = new Vector2(4,4);
            this.Scale.X = Scale.X * Size;
            this.Scale.Y = Scale.Y * Size;
            this.Tag = Tag;
            this.Speed = Speed;
            this.Row = Row;
            this.Upright = upright;
            MinSpeed = Speed;
            Acceleration = 1000f;
            SheetBase = File.Exists($"Assets/Sprites/{AtlasName}Atlas.png")?Raylib.LoadTexture($"Assets/Sprites/{AtlasName}Atlas.png"):new();
            SheetNormals = File.Exists($"Assets/Sprites/{AtlasName}Normals.png")?Raylib.LoadTexture($"Assets/Sprites/{AtlasName}Normals.png"):new();
            SheetParallax = File.Exists($"Assets/Sprites/{AtlasName}Parallax.png")?Raylib.LoadTexture($"Assets/Sprites/{AtlasName}Parallax.png"):new();
            Raylib.SetTextureFilter(SheetBase, TextureFilter.Point);
            Raylib.SetTextureFilter(SheetNormals, TextureFilter.Point);
            Raylib.SetTextureFilter(SheetParallax, TextureFilter.Point);
            src = new(StateIndex * Size, (Facing * 3 + State) * Size, Size, Size);
            dest = new(
                Position.X - (Scale.X / 2),
                -(Position.Y * .5f) - Position.Z - (Scale.Y * .5f),
                Scale.X,
                Scale.Y
            );
            MuddEngine.RegisterSprite(this);
        }

        public void DestroySelf()
        {
            MuddEngine.UnregisterSprite(this);
        }
        public bool HasTexture => SheetBase.Id != 0;
        public bool HasNormal => SheetNormals.Id != 0;
        public bool HasParallax => SheetParallax.Id != 0;
        public bool HasHeight => SheetHeightMap.Id != 0;

        public virtual void DrawBase()
        {
            if (!HasTexture) return;
            Raylib.DrawTexturePro(SheetBase, src, dest, Vector2.Zero, 0f, Raylib_cs.Color.White);
        }

        public virtual void DrawNormals()
        {
            if(!HasNormal) return;
            Raylib.DrawTexturePro(
                SheetNormals,
                src,
                dest,
                Vector2.Zero,
                0f,
                Raylib_cs.Color.White
            );
        }
        public virtual void DrawParallax()
        {
            if(!HasParallax) return;
            Raylib.DrawTexturePro(
                SheetParallax,
                src,
                dest,
                Vector2.Zero,
                0f,
                Raylib_cs.Color.White
            );
        }

        public virtual void Update(float dt, float t, Keyboard keyboard)
        {
            // Base sprite does NOT handle movement or dashing anymore.
            Vector2 movement = Movement * dt * Speed;
            Position += new Vector3(movement.X,movement.Y,0);
            src = new(StateIndex * Size, (Facing * 3 + State) * Size, Size, Size);
            dest = new(
                Position.X - (Scale.X / 2),
                -(Position.Y / 2) - Position.Z - (Scale.Y / 2),
                Scale.X,
                Scale.Y
            );
        }

        protected int GetFacing(Vector2 dir)
        {
            // Optional: deadzone to avoid jitter
            const float eps = 0.0001f;
            int direction = 0;
            // Normalize if needed
            if (dir.LengthSquared() > 1f)
                dir = Vector2.Normalize(dir);
            if (dir.Length() == 0f)
            {
                return Facing;
            }
                //Left/Right
            if ((dir.X < -eps) || (dir.X > eps))
            {
                direction=(dir.X > eps)?2:3;
                if (dir.Y > eps) direction += 2;
            }
            //no X entered, up or down
            else if (dir.Y < -eps)
                direction=0;
            else if (dir.Y > eps)
                direction=1;
            
            // Default fallback (no movement)
            return direction; // or whatever your idle facing is
        }

    }
}