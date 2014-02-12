using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra;

namespace SubRecognizer
{
    [Serializable]
    public class BitmapPoints : ICloneable
    {
        [Serializable]
        private class GenericPoint : ICloneable
        {
            private double _x;
            private double _y;

            private bool _isEndOfStroke;
            private Guid _id;

            public GenericPoint()
            {
                _x = 0.0;
                _y = 0.0;

                _isEndOfStroke = false;
                _id = Guid.NewGuid();
            }

            public GenericPoint(double x, double y, bool isEndOfStroke)
            {
                _x = x;
                _y = y;

                _isEndOfStroke = isEndOfStroke;
                _id = Guid.NewGuid();
            }

            public GenericPoint(GenericPoint gp)
            {
                this._id = gp._id;
                this._x = gp._x;
                this._y = gp._y;
                this._isEndOfStroke = gp._isEndOfStroke;
            }

            public object Clone()
            {
                return new GenericPoint(this);
            }

            public double X
            {
                get { return _x; }
                set { _y = value; }
            }

            public double Y
            {
                get { return _y; }
                set { _y = value; }
            }

            public double R(Coord center, double averageDist)
            {
                return Math.Sqrt(Math.Pow(_x - center.X, 2) + Math.Pow(_y - center.Y, 2)) / averageDist;
            }

            public double Theta(Coord center)
            {
                return Math.Atan2(_y - center.Y, _x - center.X);
            }

            public Guid Id
            {
                get { return _id; }
            }

            public bool IsEndOfStroke
            {
                get { return _isEndOfStroke; }
            }

            public void Rotate(double theta)
            {
                double oldX = _x;
                double oldY = _y;
                _x = oldX * Math.Cos(theta) - oldY * Math.Sin(theta);
                _y = oldX * Math.Sin(theta) + oldY * Math.Cos(theta);
            }

            public void Rotate(double theta, Coord around)
            {
                _x -= around.X;
                _y -= around.Y;

                Rotate(theta);

                _x += around.X;
                _y += around.Y;
            }
        }

        private List<GenericPoint> _points;
        private Coord _polarCenter;
        private double _averageDist;

        public BitmapPoints()
        {
            _points = new List<GenericPoint>();
            _polarCenter = new Coord();
            _averageDist = 0.0;
        }

        public object Clone()
        {
            BitmapPoints points = (BitmapPoints)this.MemberwiseClone();

            points._polarCenter = (Coord)this._polarCenter.Clone();

            points._points = new List<GenericPoint>();
            foreach (GenericPoint pt in this._points)
                points._points.Add((GenericPoint)pt.Clone());

            return points;
        }

        public void AddStroke(Sketch.Substroke sub)
        {
            for (int i = 0; i < sub.PointsL.Count; i++)
                _points.Add(new GenericPoint(sub.PointsL[i].X, sub.PointsL[i].Y, false));
            _points.Add(new GenericPoint(sub.PointsL[sub.PointsL.Count - 1].X, sub.PointsL[sub.PointsL.Count - 1].Y, true));

            update();
        }

        public System.Drawing.RectangleF BoundingBox()
        {
            float xmax, ymax, xmin, ymin;
            xmax = ymax = float.NegativeInfinity;
            xmin = ymin = float.PositiveInfinity;

            foreach (GenericPoint pt in _points)
            {
                xmax = (float)Math.Max(xmax, pt.X);
                xmin = (float)Math.Min(xmin, pt.X);
                ymax = (float)Math.Max(ymax, pt.Y);
                ymin = (float)Math.Min(ymin, pt.Y);
            }

            return new System.Drawing.RectangleF(xmin, ymin, (xmax - xmin), (ymax - ymin));
        }

        /// <summary>
        /// Clamp the given number to the range bottom (inclusive) to top (exclusive)
        /// </summary>
        /// <param name="n"></param>
        /// <param name="bottom"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        private int confineToRange(int n, int bottom, int top)
        {
            if (n < bottom)
                return bottom;
            if (n >= top)
                return top - 1;
            return n;
        }

        public Matrix QuantizeScreen(int grid_x, int grid_y)
        {
            // create a general matrix full of 0s
            Matrix mesh = new Matrix(grid_x, grid_y, 0.0);

            // Find the bounding box of the points.
            System.Drawing.RectangleF bBox = BoundingBox();

            if (_points.Count == 0) return mesh;

            // Treat the bounding box as having a minimum size of 1x1 to avoid divide-by-zero
            float bBoxWidth = Math.Max(bBox.Width, 1);
            float bBoxHeight = Math.Max(bBox.Height, 1);
            
            // find the step we want
            // Note: this is an intentional deviation from the original paper.
            //      We do not maintain the original aspect ratio of the shape.
            double stepX = bBoxWidth / (double)(grid_x - 1);
            double stepY = bBoxHeight / (double)(grid_y - 1);

            // find the center of the image
            double cx = (double)(bBox.Left + bBox.Right) / 2.0;
            double cy = (double)(bBox.Top + bBox.Bottom) / 2.0;

            foreach (GenericPoint pt in _points)
            {
                // normalize the point's coordinates
                int x_index = (int)Math.Floor(((pt.X - cx) / stepX) + grid_x / 2);
                int y_index = (int)Math.Floor(((pt.Y - cy) / stepY) + grid_y / 2);

                // In rare cases, numeric precision problems can bite us.
                // These lines make sure that doesn't happen.
                x_index = confineToRange(x_index, 0, grid_x);
                y_index = confineToRange(y_index, 0, grid_y);

                mesh[y_index, x_index] = 1.0;
            }

#if JESSI
            Console.WriteLine("Quantized screen");
            printPrettyMatrix(mesh, grid_x, grid_y);
            Console.WriteLine();
            //Console.Write(mesh.ToString());
#endif

            return mesh;
        }

        public Matrix QuantizePolar(int grid_x, int grid_y)
        {
            // create a general matrix full of 0s
            Matrix mesh = new Matrix(grid_y, grid_x, 0.0);

            if (_points.Count == 0) return mesh;

            double stepX = 2.0 * Math.PI / grid_x; // X is the theta coordinate
            double stepY = 1.0 / grid_y; // Y is the R coordinate

            foreach (GenericPoint pt in _points)
            {
                int x_index = (int)Math.Floor((pt.Theta(_polarCenter) + Math.PI) / stepX);
                double r = pt.R(_polarCenter, _averageDist);
                double t = pt.Theta(_polarCenter);
                int y_index = (int)Math.Floor(pt.R(_polarCenter, _averageDist) / stepY);

                if (y_index < grid_y && x_index < grid_x)
                    mesh[y_index, x_index] = 1.0;
            }

#if JESSI
            Console.WriteLine("Quantized polar");
            printPrettyMatrix(mesh, grid_x, grid_y);
            Console.WriteLine();
            //Console.Write(mesh.ToString());

#endif

            return mesh;
        }

        /// <summary>
        ///  Prints out the matrix, replacing 0's and 1's with spaces and dashes
        ///  Used to print out templates and gates.
        /// </summary>
        public void printPrettyMatrix(Matrix mesh, int grid_x, int grid_y)
        {
            for (int row = 0; row < grid_x; ++row)
            {
                for (int col = 0; col < grid_y; ++col)
                {

                    if (mesh[row, col] == 0)
                        Console.Write(' ');
                    else
                        Console.Write('X');
                }
                Console.WriteLine();
            }
        }



        /// <summary>
        /// Rotates the list of points around it's weighted polar center.
        /// The polar center should not change.
        /// </summary>
        /// <param name="theta"></param>
        public void Rotate(double theta)
        {
            foreach (GenericPoint pt in _points)
                pt.Rotate(theta, _polarCenter);
        }

        /// <summary>
        /// Calculates the weighted polar center of the points.
        /// </summary>
        private void update()
        {
            double x_center = 0.0;
            double y_center = 0.0;

            double totalLength = 0.0;

            if (_points.Count == 0) _polarCenter = new Coord();

            for (int p = 0; p < _points.Count; p++)
            {
                if (_points[p].IsEndOfStroke) continue;

                // the Euclidean length of the segment
                double segmentLength = Math.Sqrt(Math.Pow((_points[p].X - _points[p + 1].X), 2) 
                                               + Math.Pow((_points[p].Y - _points[p + 1].Y), 2));

                // the segment's center
                double x = (_points[p].X + _points[p + 1].X) / 2.0;
                double y = (_points[p].Y + _points[p + 1].Y) / 2.0;

                // Add the information from this line segment
                // segments are weighted by the length of the stroke
                totalLength += segmentLength;
                x_center += x * segmentLength;
                y_center += y * segmentLength;
            }

            // normalize the coordinates of the center
            x_center /= totalLength;
            y_center /= totalLength;

            _polarCenter = new Coord(x_center, y_center);

            // calculate the average distance of points from the center
            _averageDist = 0.0;
            foreach (GenericPoint pt in _points)
                _averageDist += Math.Sqrt(Math.Pow(pt.X - _polarCenter.X, 2) 
                                        + Math.Pow(pt.Y - _polarCenter.Y, 2));
            _averageDist /= _points.Count;
        }
    }

    [Serializable]
    [DebuggerDisplay("x={_x} y={_y}")]
    public class Coord: ICloneable
    {
        private double _x;
        private double _y;

        public Coord()
        {
            _x = 0.0;
            _y = 0.0;
        }

        public Coord(Coord c)
        {
            Coord temp = (Coord)c.Clone();
            this._x = temp._x;
            this._y = temp._y;
        }

        public Coord(double x, double y)
        {
            _x = x;
            _y = y;
        }

        public Coord(int x, int y)
        {
            _x = (double)x;
            _y = (double)y;
        }

        public object Clone()
        {
            Coord c = (Coord)this.MemberwiseClone();
            return c;
        }

        public bool Equals(Coord c)
        {
            return (Math.Abs(_x - c._x) < double.Epsilon && Math.Abs(_x - c._x) < double.Epsilon);
        }

        public Coord Plus(Coord c)
        {
            return new Coord(_x + c._x, _y + c._y);
        }

        public Coord Minus(Coord c)
        {
            return new Coord(_x - c._x, _y - c._y);
        }

        /// <summary>
        /// calculates the distance between the calling point and point c
        /// </summary>
        public double distanceTo(Coord c)
        {
            double dx = _x - c._x;
            double dy = _y - c._y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public void Translate(double dx, double dy)
        {
            _x += dx;
            _y += dy;
        }

        public void Rotate(double theta)
        {
            double oldx = _x;
            double oldy = _y;
            _x = oldx * Math.Cos(theta) - oldy * Math.Sin(theta);
            _y = oldx * Math.Sin(theta) + oldy * Math.Cos(theta);
        }

        public void Scale(double sx, double sy)
        {
            _x *= sx;
            _y *= sy;
        }

        private void copy(Coord c)
        {
            _x = c._x;
            _y = c._y;
        }

        public double X
        {
            get { return _x; }
        }

        public double Y
        {
            get { return _y; }
        }
    }
}
