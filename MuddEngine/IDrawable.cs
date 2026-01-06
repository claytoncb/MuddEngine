using System.Numerics;

public interface IDrawable
{
    Vector3 Position { get; set; }
    void DrawBase();
}
