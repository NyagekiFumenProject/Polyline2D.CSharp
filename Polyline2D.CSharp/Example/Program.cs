using Polyline2DCSharp;
using System.Drawing;
using System.Drawing.Imaging;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var points = new List<Vec2>(){
                new Vec2( -0.25f, -0.5f  ),
                new Vec2( -0.25f,  0.5f  ),
                new Vec2( 0.25f,  0.25f ),
                new Vec2( 0.0f,   0.0f  ),
                new Vec2( 0.25f, -0.25f ),
                new Vec2(-0.4f,  -0.25f )
            };

            var output = Polyline2D.Create(new List<Vec2>(), points, 0.1f, Polyline2D.JointStyle.ROUND, Polyline2D.EndCapStyle.SQUARE);

            using var image = new Bitmap(800, 800);
            using var graphics = Graphics.FromImage(image);
            using var pen = new Pen(Brushes.Green);

            int i = 0;

            for (int idx = 0; idx < output.Count; idx++)
            {
                Console.WriteLine($"vert {idx} : ({output[idx].x:F2},{output[idx].y:F2})");
            }

            while (i <= output.Count - 3)
            {
                graphics.DrawPolygon(pen, new[] { output[i], output[i + 1], output[i + 2] }.Select(x => new PointF()
                {
                    X = x.x * 400 + 400,
                    Y = -x.y * 400 + 400,
                }).ToArray());

                i+=3;
            }

            graphics.Dispose();
            image.Save(@"F:\zz.png", ImageFormat.Png);

        }
    }
}