using System.Numerics;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public class Sprite2D : IDrawable
    {
        public Vector3 Position {get; set;}
        public Vector2 Scale;
        public int Size = 32;
        public string Tag = "";
        public Texture2D Sheet;
        public Texture2D SheetNormals;
        public Texture2D SheetHeightMap;
        public Bitmap Sprite = null;
        public int Facing = 0;
        public float Speed = 0;
        public Vector2 Movement = Vector2.Zero;
        public float MinSpeed = 0;
        public float Acceleration;
        public float Height = 12f;
        public int Row;

        public Sprite2D(Vector3 Position, string Tag, int Row, float Speed)
        {
            this.Position = Position;
            this.Scale = new Vector2(4,4);
            this.Scale.X = Scale.X * Size;
            this.Scale.Y = Scale.Y * Size;
            this.Tag = Tag;
            this.Speed = Speed;
            this.Row = Row;
            MinSpeed = Speed;
            Acceleration = 1000f;
            //set texture
            string path = $"Assets/Sprites/Atlas.png";
            Sheet = File.Exists(path)?Raylib.LoadTexture(path):new();
            //set normals
            string pathNormals = $"Assets/Sprites/Normals.png";
            SheetNormals = File.Exists(pathNormals)?Raylib.LoadTexture(pathNormals):new();
            MuddEngine.RegisterSprite(this);
        }

        public void DestroySelf()
        {
            MuddEngine.UnregisterSprite(this);
        }
        public bool HasTexture => Sheet.Id != 0;
        public bool HasNormal => SheetNormals.Id != 0;
        public bool HasHeight => SheetHeightMap.Id != 0;

        public virtual void DrawBase()
{
    //return if no sheet
    if (Sheet.Id == 0) return;
    //location on texture atlas
    Raylib_cs.Rectangle src = new(Facing * Size, Row * Size, Size, Size);
    //location on screen
    Raylib_cs.Rectangle dest = new(
        Position.X - (Scale.X / 2),
        (Position.Y/2) - (Position.Z*8) - (Scale.Y / 2),
        Scale.X,
        Scale.Y
    );
    Raylib.DrawTexturePro(Sheet, src, dest, Vector2.Zero, 0f, Raylib_cs.Color.White);
}
public virtual void DrawNormal()
{
    if (!HasNormal) return;

    // location in normal atlas
    Raylib_cs.Rectangle src = new(
        Facing * Size,
        Row * Size,
        Size,
        Size
    );

    // location on screen (same as DrawBase / DrawLight)
    Raylib_cs.Rectangle dest = new(
        Position.X - (Scale.X / 2),
        (Position.Y / 2) - (Position.Z * 8) - (Scale.Y / 2),
        Scale.X,
        Scale.Y
    );

    Raylib.DrawTexturePro(
        SheetNormals,
        src,
        dest,
        Vector2.Zero,
        0f,
        Raylib_cs.Color.White
    );
}

public virtual void DrawLight()
{
    //return if no texture, normal, or lighting
    if (!HasTexture || !HasNormal || MuddEngine.Lighting == null) return;
    //set normalMap
    int locNormal = Raylib.GetShaderLocation(MuddEngine.Lighting.Shader, "normalMap");
    Raylib.SetShaderValueTexture(MuddEngine.Lighting.Shader, locNormal, SheetNormals);
    //set spritePos
    int locSpritePos = Raylib.GetShaderLocation(MuddEngine.Lighting.Shader, "spritePos");
    Vector3 spritePos = new(Position.X, Position.Y, Position.Z);
    Raylib.SetShaderValue(MuddEngine.Lighting.Shader, locSpritePos, spritePos, ShaderUniformDataType.Vec3);
    //location in normal atlas
    Raylib_cs.Rectangle src = new(Facing * Size, Row * Size, Size, Size);
    //location on screen
    Raylib_cs.Rectangle dest = new(
        Position.X - (Scale.X / 2),
        (Position.Y/2) - (Position.Z*8) - (Scale.Y / 2),
        Scale.X,
        Scale.Y
    );
    //draw normal
    Raylib.DrawTexturePro(Sheet, src, dest, Vector2.Zero, 0f, Raylib_cs.Color.White);
}

        public virtual void Update(float dt, Keyboard keyboard)
        {
            // Base sprite does NOT handle movement or dashing anymore.
            Vector2 movement = Movement * dt * Speed;
            Position += new Vector3(movement.X,movement.Y,0);
        }

        protected int GetFacing(Vector2 dir)
        {
            // Optional: deadzone to avoid jitter
            const float eps = 0.0001f;
            int direction;
            // Normalize if needed
            if (dir.LengthSquared() > 1f)
                dir = Vector2.Normalize(dir);
            if (dir.Length() == 0f)
            {
                return Facing;
            }
                //Left/Right
            if (dir.X < -eps)
                direction=1;
            else if (dir.X > eps)
                direction=0;
            else 
                direction = Facing%2;
            // Up/Down
            if (dir.Y < -eps)
                direction+=2;
            
            // Default fallback (no movement)
            return direction; // or whatever your idle facing is
        }

    }
}