using System;
using System.Drawing;
using System.Drawing.Imaging;



namespace MuddEngine
{
    class Program
    {
        static void InvertRedGreen(string inputPath, string outputPath)
{
    using (Bitmap bmp = new Bitmap(inputPath))
    {
        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                Color c = bmp.GetPixel(x, y);

                // Invert red and green channels
                byte r = (byte)255;
                byte g = (byte)0;
                byte b = (byte)0;   // leave blue unchanged
                byte a = c.A;

                bmp.SetPixel(x, y, Color.FromArgb(a, r, g, b));
            }
        }

        bmp.Save(outputPath, ImageFormat.Png);
    }
}
        public static void Main()
        {
            DemoGame game = new();
            //InvertRedGreen(@"C:\Users\clayt\Documents\CsharpApps\MuddEngine\bin\Debug\net10.0-windows\Assets\Sprites\ConductorNormals.png",@"C:\Users\clayt\Documents\CsharpApps\MuddEngine\bin\Debug\net10.0-windows\Assets\Sprites\ConductorParallax.png");
        }
    }
}

