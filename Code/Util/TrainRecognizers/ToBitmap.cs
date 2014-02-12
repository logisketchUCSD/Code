using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows;
using Sketch;

namespace TrainRecognizers
{
    class ToBitmap
    {

        public static Bitmap createFromShape(Shape shape, int width, int height, bool preserveAspect)
        {
            Rect bounds = shape.Bounds;
            int x = (int)bounds.X;
            int y = (int)bounds.Y;
            int w = (int)bounds.Width;
            int h = (int)bounds.Height;

            if (preserveAspect)
            {
                float nPercent = 0;
                float nPercentW = 0;
                float nPercentH = 0;

                nPercentW = ((float)width / (float)w);
                nPercentH = ((float)height / (float)h);

                if (nPercentH < nPercentW)
                    nPercent = nPercentH;
                else
                    nPercent = nPercentW;

                width = (int)(w * nPercent);
                height = (int)(h * nPercent);
            }

            float scaleX = (float)width / w;
            float scaleY = (float)height / h;

            Bitmap result = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(result);

            // transparent background
            g.CompositingMode = CompositingMode.SourceCopy;
            Brush clear = new SolidBrush(Color.Transparent);
            g.FillRectangle(clear, 0, 0, w, h);

            // draw over transparent background
            g.CompositingMode = CompositingMode.SourceOver;
            Pen pen = new Pen(Color.Black, 3);

            foreach (Substroke stroke in shape.Substrokes)
            {

                int count = stroke.Points.Length;

                if (count <= 1)
                    continue;

                Sketch.Point previous = stroke.Points[0];

                for (int i = 1; i < count;  i++)
                {
                    Sketch.Point current = stroke.Points[i];
                    g.DrawLine(
                        pen, 
                        (previous.X - x) * scaleX, 
                        (previous.Y - y) * scaleY, 
                        (current.X - x) * scaleX, 
                        (current.Y - y) * scaleY);
                    previous = current;
                }

            }

            return result;

        }

    }
}
