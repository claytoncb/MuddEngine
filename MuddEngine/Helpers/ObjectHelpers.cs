using System.Numerics;
using System.Drawing;
using Raylib_cs;

namespace MuddEngine.MuddEngine
{
    public static class ObjectHelpers
    {
        public static List<MuddObject> FlattenObjects(List<MuddObject> MuddObjects)
        {
            List<MuddObject> list = new();

            foreach (MuddObject muddObject in MuddObjects)
            {
                if (muddObject is MuddGroup group)
                {
                    list.Add(group);
                    list.AddRange(FlattenObjects(group.Children));
                }
                else
                {
                    list.Add(muddObject);
                }
            }
            return list
                .OrderBy(o => -o.GetPosition().Z)
                .ThenBy(o => o.GetPosition().Y)
                .ToList();
        }
        public static Vector2 WorldToScreen(Vector3 world, Camera2D cam)
        {
            // Your compositor uses this exact projection:
            // world2D = (X, Y/2 + Z)
            Vector2 world2D = new Vector2(world.X, (world.Y / 2f) + world.Z);

            return Raylib.GetWorldToScreen2D(world2D, cam);
        }
        public static RectangleF ComputeScreenBounds(
            MuddObject obj,
            Camera2D cam)
        {
            var pos = obj.GetPosition();

            // Same projection as compositor
            Vector2 world2D      = new Vector2(pos.X, (pos.Y / 2f) + pos.Z);
            Vector2 screenCenter = Raylib.GetWorldToScreen2D(world2D, cam);

            // Same frame math as compositor
            Vector2 scaledFrame   = obj.Size * cam.Zoom;
            Vector2 bottomLeft    = screenCenter - scaledFrame * 0.5f;

            // Same visible rect math as shader
            Vector2 scaledVisible = obj.VisibleSize * cam.Zoom;
            Vector2 scaledOffset  = obj.VisibleOffset * cam.Zoom;

            Vector2 minB = bottomLeft + scaledOffset;
            Vector2 maxB = minB + scaledVisible;

            float x = minB.X;
            float y = minB.Y;
            float w = maxB.X - minB.X;
            float h = maxB.Y - minB.Y;

            return new RectangleF(x, y, w, h);
        }
        public static bool IntersectsScreen(RectangleF r, Vector2 screenSize)
        {
            return !(r.Right < 0 ||
                     r.Left > screenSize.X ||
                     r.Bottom < 0 ||
                     r.Top > screenSize.Y);
        }
        public static List<MuddObject> FilterVisible(
            List<MuddObject> flat,
            Vector2 screenSize,
            Camera2D cam)
        {
            List<MuddObject> result = new();

            foreach (var obj in flat)
            {
                RectangleF bounds = ComputeScreenBounds(obj, cam);

                if (IntersectsScreen(bounds, screenSize))
                    result.Add(obj);
            }

            return result;
        }
        public static float[] BuildSpriteColumn(object o, CameraSprite Camera)
            {
                var obj = (MuddObject)o;   // or your actual type
                var pos = obj.GetPosition();

                Vector2 world2D      = new Vector2(pos.X, (pos.Y / 2f) + pos.Z);
                Vector2 screenCenter = Raylib.GetWorldToScreen2D(world2D, Camera.Camera);

                Vector2 scaledFrame = obj.Size * Camera.Camera.Zoom;
                Vector2 bottomLeft  = screenCenter - scaledFrame * 0.5f;

                float[] col =
                [
                    pos.X,
                    pos.Y,
                    pos.Z,
                    0f,
                    bottomLeft.X,
                    bottomLeft.Y,
                    0f,
                    0f,
                    obj.Size.X,
                    obj.Size.Y,
                    0f,
                    0f,
                    obj.SheetLocation.X,
                    obj.SheetLocation.Y,
                    0f,
                    0f,
                    obj.AtlasOrigin.X,
                    obj.AtlasOrigin.Y,
                    0f,
                    0f,
                    obj.VisibleOffset.X,
                    obj.VisibleOffset.Y,
                    0f,
                    0f,
                    obj.VisibleSize.X,
                    obj.VisibleSize.Y,
                    0f,
                    0f,
                    obj.isFlat ? 1f : 0f,
                    0f,
                    0f,
                    0f,
                ];
                            return col;
            }
    }
}