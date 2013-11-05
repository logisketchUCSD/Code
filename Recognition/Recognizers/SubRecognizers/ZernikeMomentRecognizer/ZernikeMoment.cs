using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra;

namespace SubRecognizer
{
    [Serializable]
    public class ZernikeMoment
    {
        string m_Name;

        Dictionary<string, double> _featureValues;

        List<string> features = new List<string>(new string[] { 
                "A00", "A11", "A20", "A22", "A31", "A33", "A40", 
                "A42", "A44", "A51", "A53", "A55", "A60", "A62", 
                "A64", "A66", "A71", "A73", "A75", "A77", "A80", 
                "A82", "A84", "A86", "A88", "A91", "A93", "A95", 
                "A97", "A99", "A100", "A102", "A104", "A106", 
                "A108", "A1010" });

        /*public ZernikeMoment(string name, Point[] strokePoints)
        {
            _featureValues = new Dictionary<string, double>();
            m_Name = name;

            List<PointF> polarPoints = GetNormalizedPolarPoints(strokePoints);

            CalculateAndAssignZernikeValues(polarPoints);
        }*/

        public ZernikeMoment(List<Sketch.Substroke> strokes)
        {
            _featureValues = new Dictionary<string, double>();
            m_Name = "Unknown";
            List<Point> points = new List<Point>();
            foreach (Sketch.Substroke s in strokes)
                foreach (Sketch.Point pt in s.Points)
                    points.Add(pt.SysDrawPoint);

            List<PointF> polarPoints = GetNormalizedPolarPoints(points.ToArray());

            CalculateAndAssignZernikeValues(polarPoints);
        }

        public ZernikeMoment(string label, List<Sketch.Substroke> strokes)
        {
            _featureValues = new Dictionary<string, double>();
            m_Name = label;
            List<Point> points = new List<Point>();
            foreach (Sketch.Substroke s in strokes)
                foreach (Sketch.Point pt in s.Points)
                    points.Add(pt.SysDrawPoint);

            List<PointF> polarPoints = GetNormalizedPolarPoints(points.ToArray());

            CalculateAndAssignZernikeValues(polarPoints);
        }

        public Dictionary<string, object> FeatureValuesObject
        {
            get
            {
                Dictionary<string, object> output = new Dictionary<string, object>();
                foreach (KeyValuePair<string, double> pair in _featureValues)
                    output.Add(pair.Key, (object)pair.Value);

                return output;
            }
        }

        public Dictionary<string, double> FeatureValues
        {
            get { return _featureValues; }
        }

        public List<double> Values
        {
            get
            {
                List<double> values = new List<double>(_featureValues.Values);

                return values;
            }
        }

        private void CalculateAndAssignZernikeValues(List<PointF> polarPoints)
        {
            foreach (string feature in features)
            {
                try
                {
                    if (feature.Length == 3)
                    {
                        int n, m;
                        bool bN = int.TryParse(feature.Substring(1, 1), out n);
                        bool bM = int.TryParse(feature.Substring(2, 1), out m);
                        if (bN && bM && !_featureValues.ContainsKey(feature))
                            _featureValues.Add(feature, Zernicke(n, m, polarPoints));
                    }
                    else if (feature.Length == 4)
                    {
                        int n, m;
                        bool bN = int.TryParse(feature.Substring(1, 2), out n);
                        bool bM = int.TryParse(feature.Substring(3, 1), out m);
                        if (bN && bM && !_featureValues.ContainsKey(feature))
                            _featureValues.Add(feature, Zernicke(n, m, polarPoints));
                    }
                    else if (feature.Length == 5)
                    {
                        int n, m;
                        bool bN = int.TryParse(feature.Substring(1, 2), out n);
                        bool bM = int.TryParse(feature.Substring(3, 2), out m);
                        if (bN && bM && !_featureValues.ContainsKey(feature))
                            _featureValues.Add(feature, Zernicke(n, m, polarPoints));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Zernike Calculate: " + e.Message);
                }
            }
        }

        private List<PointF> GetNormalizedPolarPoints(Point[] strokePoints)
        {
            List<Point> RastPoints = RasterizePoints(strokePoints, 48);

            int sumX = 0;
            int sumY = 0;
            foreach (Point pt in RastPoints)
            {
                sumX += pt.X;
                sumY += pt.Y;
            }

            float xBar = (float)sumX / (float)strokePoints.Length;
            float yBar = (float)sumY / (float)strokePoints.Length;

            List<PointF> movedPoints = new List<PointF>(strokePoints.Length);
            foreach (Point pt in strokePoints)
                movedPoints.Add(new PointF((float)pt.X - xBar, (float)pt.Y - yBar));

            double rMax = 0.0;
            foreach (PointF pt in movedPoints)
                rMax = Math.Max(rMax, Radius(pt));

            List<PointF> polarPoints = new List<PointF>(movedPoints.Count);
            foreach (PointF pt in movedPoints)
            {
                double r = Radius(pt) / rMax;
                double theta = Math.Atan2(pt.Y, pt.X);
                if (theta < 0.0)
                    theta += 2 * Math.PI;
                polarPoints.Add(new PointF((float)r, (float)theta));
            }

            return polarPoints;
        }

        private List<Point> RasterizePoints(Point[] strokePoints, int length)
        {
            double diagLength = Math.Sqrt(Math.Pow((double)(length - 1), 2.0));
            List<Point> points = new List<Point>(strokePoints.Length);
            Rectangle box = BoundingBox(strokePoints);
            int maxSide = box.Width;
            if (box.Height > maxSide)
                maxSide = box.Height;
            double boxDiag = Math.Sqrt(Math.Pow((double)maxSide, 2.0));
            double ratio = diagLength / boxDiag;
            Matrix matrix = new Matrix(length, length, 0.0);

            foreach (Point pt in strokePoints)
            {
                int x = (int)Math.Floor((pt.X - box.Left) * ratio);
                int y = (int)Math.Floor((pt.Y - box.Top) * ratio);
                matrix[x, y] = 1.0;
            }

            for (int i = 0; i < matrix.RowCount; i++)
                for (int j = 0; j < matrix.ColumnCount; j++)
                    if (matrix[i, j] > 0.0)
                        points.Add(new Point(i, j));

            return points;
        }

        /// <summary>
        /// Finds the bounding box for a list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        Rectangle BoundingBox(Point[] points)
        {
            int[] MinMax = ComputeMinMax(points);

            int width = MinMax[1] - MinMax[0];
            int height = MinMax[3] - MinMax[2];

            return new Rectangle(MinMax[0], MinMax[2], width, height);
        }

        /// <summary>
        /// { minX, maxX, minY, maxY }
        /// Determines the minimum and maximum values of X and Y for a set of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        int[] ComputeMinMax(Point[] points)
        {
            if (points.Length == 0)
                return new int[] { 0, 0, 0, 0 };

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                minX = Math.Min(minX, points[i].X);
                maxX = Math.Max(maxX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxY = Math.Max(maxY, points[i].Y);
            }

            return new int[] { minX, maxX, minY, maxY };
        }

        private double Radius(PointF pt)
        {
            return Math.Sqrt(Math.Pow(pt.X, 2.0) + Math.Pow(pt.Y, 2.0));
        }

        private double Zernicke(int n, int m, List<PointF> pointsPolar)
        {
            double sum = 0.0;
            foreach (PointF pt in pointsPolar)
                sum += BasisFunction(n, m, pt.X, pt.Y);

            sum *= (n + 1) / Math.PI;
            return Math.Abs(sum);
        }

        private double BasisFunction(int n, int m, double rho, double theta)
        {
            double R = RadialPolynomial(n, m, rho);
            double e = 1.0;// Math.Exp(m * theta);

            return R * e;
        }

        private double RadialPolynomial(int n, int m, double rho)
        {
            double sum = 0.0;
            int k = Math.Abs(m);

            for (int i = k; i <= n; i++)
            {
                if ((n - k) % 2 == 0)
                {
                    double a = Math.Pow(-1.0, (double)(n - k) / 2.0);
                    double b = factorial((n + k) / 2);
                    double c = factorial((n - k) / 2);
                    double d = factorial((k + m) / 2);
                    double e = factorial((k - m) / 2);
                    double f = Math.Pow(rho, (double)k);
                    double value = ((a * b) / (c * d * e)) * f;
                    sum += value;
                }
            }

            return sum;
        }

        private double factorial(int n)
        {
            if (n < 0)
                return 0.0;
            else if (n == 0)
                return 1.0;
            else
                return (double)n * factorial(n - 1);

        }

        public void Print(System.IO.StreamWriter writer)
        {
            foreach (KeyValuePair<string, double> pair in _featureValues)
                writer.Write(pair.Value.ToString() + ",");
            writer.WriteLine(m_Name);
        }
    }

}
