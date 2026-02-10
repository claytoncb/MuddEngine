using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;

namespace MuddEngine.MuddEngine
{
    public static class BufferHelper
    {
        public static Texture2D CreateImage(int NumObjects, int RowsPerObject)
        {
            Texture2D Texture;
            Image img = Raylib.GenImageColor(NumObjects, RowsPerObject, Color.Blank);
            Raylib.ImageFormat(ref img, PixelFormat.UncompressedR32G32B32A32);
            Texture = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
            return Texture;
        }
        public static byte[] LoadDataFromObjects(int MaxObjects, int RowsPerObject, List<object> Objects, Func<object, float[]> getColumn)
        {
            float[] dataBuffer = new float[MaxObjects * RowsPerObject * 4];
            for (int row = 0; row < Objects.Count; row++)
            {
                // Get the 4‑float column for this object
                float[] column = getColumn(Objects[row]);

                // Compute the starting index in the buffer
                int ColumnStart = row * RowsPerObject * 4;
                // Insert the column into the buffer
                // (Assumes getColumn returns exactly rowsPerObject * 4 floats)
                Array.Copy(column, 0, dataBuffer, ColumnStart, RowsPerObject * 4);
            }
            byte[] spriteBytes = new byte[dataBuffer.Length * sizeof(float)];
            Buffer.BlockCopy(dataBuffer, 0, spriteBytes, 0, spriteBytes.Length);
            return spriteBytes;
        }
    }
}