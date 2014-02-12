using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using Utilities;
using System.Diagnostics;
using Featurefy; // we need Featurefy.Compute

namespace SubRecognizer
{
    [Serializable]
    [DebuggerDisplay("Name={m_Name}")]
    public class DollarTemplateAverage
    {
        #region Constants and Parameters

        private const double SCORE_THRESHOLD = 0.60;
        private const int NumResampledPoints = 48;
        private const double THETA_MAX = Math.PI / 4.0; // 45 degrees
        private const double THETA_DELTA = 2.0 * Math.PI / 180.0; // 2.0 degrees
        private const double SIZE = 100.0;
        private const double PHI = 0.6180339887499; //0.5 * (-1 + Math.Sqrt(5.0)); // Golden ratio

        #endregion

        #region Member variables

        /// <summary>
        /// Symbol name
        /// </summary>
        string m_Name;

        /// <summary>
        /// List of point averages and std devs
        /// </summary>
        List<AveragePoint> m_AveragePoints;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DollarTemplateAverage(List<Point> originalPoints, string SymbolName)
        {
            m_Name = SymbolName;
            List<Point> points = new List<Point>(originalPoints);
            List<Point> rsPoints = ReSamplePoints(points);
            rsPoints = rotateToZero(rsPoints);
            rsPoints = scaleToSquare(rsPoints, SIZE);
            rsPoints = translateToOrigin(rsPoints);

            m_AveragePoints = new List<AveragePoint>();
            foreach (Point pt in rsPoints)
                m_AveragePoints.Add(new AveragePoint(pt));
        }

        public DollarTemplateAverage(List<Point> originalPoints)
        {
            m_Name = "Unknown";
            List<Point> points = new List<Point>(originalPoints);
            List<Point> rsPoints = ReSamplePoints(points);
            rsPoints = rotateToZero(rsPoints);
            rsPoints = scaleToSquare(rsPoints, SIZE);
            rsPoints = translateToOrigin(rsPoints);

            m_AveragePoints = new List<AveragePoint>();
            foreach (Point pt in rsPoints)
                m_AveragePoints.Add(new AveragePoint(pt));
        }

        #endregion

        #region Interface Functions (i.e. Add)

        /// <summary>
        /// Add an example so that the average points for the template
        /// can be updated.
        /// </summary>
        /// <param name="originalPoints">Non-resampled points for the new example</param>
        public void AddExample(List<Point> originalPoints)
        {
            List<Point> rsPoints = ReSamplePoints(originalPoints);
            if (m_AveragePoints.Count != rsPoints.Count)
            {
                return;
                //throw new Exception("Number of points is inconsistent");
            }

            rsPoints = rotateToZero(rsPoints);
            rsPoints = scaleToSquare(rsPoints, SIZE);
            rsPoints = translateToOrigin(rsPoints);

            for (int i = 0; i < m_AveragePoints.Count; i++)
                m_AveragePoints[i].AddPoint(rsPoints[i]);
        }

        #endregion

        #region Computations for Template (reSample, rotate, translate, etc.)

        /// <summary>
        /// Resamples the points into a fixed number for 1-to-1 comparison
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private List<Point> ReSamplePoints(List<Point> points)
        {
            List<Point> newPoints = new List<Point>(NumResampledPoints);
            double aLength = ArcLength(points);
            double I = aLength / (NumResampledPoints - 1); // Interval after which to add a new point
            double D = 0.0;
            double d = 0.0;

            float x0 = points[0].X;
            float y0 = points[0].Y;
            newPoints.Add(new Point(x0, y0));

            for (int i = 1; i < points.Count; i++)
            {
                d = Compute.EuclideanDistance(points[i - 1], points[i]);

                if (D + d >= I)
                {
                    float x = points[i - 1].X + (float)((I - D) / d * (points[i].X - points[i - 1].X));
                    float y = points[i - 1].Y + (float)((I - D) / d * (points[i].Y - points[i - 1].Y));
                    Point q = new Point(x, y);
                    newPoints.Add(q);
                    points.Insert(i, q);
                    D = 0.0;
                }
                else
                    D = D + d;
            }

            if (newPoints.Count < NumResampledPoints)
                newPoints.Add(points[points.Count - 1]);

            return newPoints;
        }

        /// <summary>
        /// Computes the arc length of the list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private double ArcLength(List<Point> points)
        {
            double d = 0.0;
            for (int i = 1; i < points.Count; i++)
                d += Compute.EuclideanDistance(points[i - 1], points[i]);

            return d;
        }

        /// <summary>
        /// Rotate the shape so that the first point of the stroke is at zero degrees.
        /// </summary>
        /// <param name="points">List of points to be rotated</param>
        /// <returns>List of new, rotated points</returns>
        private List<Point> rotateToZero(List<Point> points)
        {
            Point c = centroid(points);
            double theta;

            theta = Math.Atan2((double)(points[0].Y - c.Y), (double)(points[0].X - c.X));
            while (theta > Math.PI || theta <= -Math.PI)
            {
                if (theta > Math.PI)
                    theta -= 2 * Math.PI;
                else if (theta <= -Math.PI)
                    theta += 2 * Math.PI;
            }

            List<Point> newPoints = rotateBy(points, -theta);

            return newPoints;
        }

        /// <summary>
        /// Rotates a list of points by a specified angle theta.
        /// </summary>
        /// <param name="points">List of points to be rotated</param>
        /// <param name="theta">Angle to be rotated by</param>
        /// <returns>List of new, rotated points</returns>
        private List<Point> rotateBy(List<Point> points, double theta)
        {
            List<Point> newPoints = new List<Point>(points.Count);
            Point c = centroid(points);

            for (int i = 0; i < points.Count; i++)
            {
                float x1 = points[i].X - c.X;
                float y1 = points[i].Y - c.Y;
                double cosTheta = Math.Cos(theta);
                double sinTheta = Math.Sin(theta);
                double dx = x1 * cosTheta - y1 * sinTheta;// (points[i].X - c.X) * Math.Cos(theta) - (points[i].Y - c.Y) * Math.Sin(theta);
                double dy = x1 * sinTheta + y1 * cosTheta;// (points[i].X - c.X) * Math.Sin(theta) + (points[i].Y - c.Y) * Math.Cos(theta);
                float x = (float)dx + c.X;
                float y = (float)dy + c.Y;
                newPoints.Add(new Point(x, y));
            }

            return newPoints;
        }

        /// <summary>
        /// Finds the centroid of a list of points (average x, average y)
        /// </summary>
        /// <param name="points">List of points to find centroid of</param>
        /// <returns>Centroid Point</returns>
        private Point centroid(List<Point> points)
        {
            float x = 0f;
            float y = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                x += points[i].X;
                y += points[i].Y;
            }

            x /= (float)points.Count;
            y /= (float)points.Count;

            return new Point(x, y);
        }

        /// <summary>
        /// Non-uniform scaling of the list of points to a square.
        /// </summary>
        /// <param name="points">List of points to be scaled</param>
        /// <param name="size">Size of one edge of the square</param>
        /// <returns>List of new points that have been scaled</returns>
        private List<Point> scaleToSquare(List<Point> points, double size)
        {
            List<Point> newPoints = new List<Point>(points.Count);
            System.Drawing.RectangleF bBox = Compute.BoundingBox(points.ToArray());

            for (int i = 0; i < points.Count; i++)
            {
                float x = points[i].X * (float)(size) / bBox.Width;
                float y = points[i].Y * (float)(size) / bBox.Height;
                newPoints.Add(new Point(x, y));
            }

            return newPoints;
        }

        /// <summary>
        /// Move the points so that their centroid is at the origin. Does not mean that
        /// the bounding box is centered at the origin.
        /// </summary>
        /// <param name="points">List of points to be translated</param>
        /// <returns>New list of translated points</returns>
        private List<Point> translateToOrigin(List<Point> points)
        {
            List<Point> newPoints = new List<Point>(points.Count);
            Point c = centroid(points);

            for (int i = 0; i < points.Count; i++)
            {
                float x = points[i].X - c.X;
                float y = points[i].Y - c.Y;
                newPoints.Add(new Point(x, y));
            }

            return newPoints;
        }

        #endregion

        #region Recognition / Comparison

        /// <summary>
        /// Match this symbol to the best Template in templates.
        /// </summary>
        /// <param name="templates">Existing templates with which to match</param>
        /// <param name="score">Score of best matching template</param>
        public DollarTemplateAverage RecognizeSymbol(List<DollarTemplateAverage> templates, ref double score)
        {
            if (templates.Count == 0)
                return null;
    
            DollarTemplateAverage Tbest = templates[0];
            double bestD = double.MaxValue;
            double bestTheta = 0.0;
            double d = 0.0;

            for (int i = 0; i < templates.Count; i++)
            {
                double theta = 0.0;
                d = EuclideanDistanceAtBestAngle(m_AveragePoints, templates[i], -THETA_MAX, THETA_MAX, THETA_DELTA, ref theta);
                //d = distanceAtBestAngle(m_AveragePoints, templates[i], -THETA_MAX, THETA_MAX, THETA_DELTA, ref theta);

                if (d < bestD)
                {
                    bestD = d;
                    bestTheta = theta;
                    Tbest = templates[i];
                }
            }

            //double bestDistance = EuclideanDistanceAtAngle(m_AveragePoints, Tbest, bestTheta);
            double bestDistance = distanceAtAngle(m_AveragePoints, Tbest, bestTheta);
            bestDistance /= 10.0;
            double score1 = 1 - bestD / (0.5 * Math.Sqrt(Math.Pow(SIZE, 2.0) * 2));
            double score2 = Math.Exp(-bestDistance);
            //Console.WriteLine("BestDistance = {0}, SDscore = {1} ||| EuclDistance = {2}, Escore = {3}", bestDistance, score2, bestD, score1);

            score = Math.Max(score1, score2);

            return Tbest;
        }

        public string Recognize(List<DollarTemplateAverage> templates)
        {
            double score = 0.0;
            DollarTemplateAverage best = RecognizeSymbol(templates, ref score);
            if (score < SCORE_THRESHOLD)
                return "Unknown";

            return best.Name;
        }

        public Dictionary<DollarTemplateAverage, double> RecognizeSymbol(List<DollarTemplateAverage> templates)
        {
            if (templates.Count == 0)
                return null;

            Dictionary<double, DollarTemplateAverage> recoResults = new Dictionary<double, DollarTemplateAverage>();

            foreach (DollarTemplateAverage T in templates)
            {
                double theta = 0.0;
                double dEucl = EuclideanDistanceAtBestAngle(m_AveragePoints, T, -THETA_MAX, THETA_MAX, THETA_DELTA, ref theta);
                double dSD = distanceAtAngle(m_AveragePoints, T, theta);
                dSD /= 10.0;

                double scoreE = 1 - dEucl / (0.5 * Math.Sqrt(Math.Pow(SIZE, 2.0) * 2));
                double scoreSD = Math.Exp(-dSD);
                double score = Math.Max(scoreE, scoreSD);

                while (recoResults.ContainsKey(score))
                    score += double.Epsilon;

                recoResults.Add(score, T);
            }

            List<double> values = new List<double>(recoResults.Keys);
            values.Sort();

            Dictionary<DollarTemplateAverage, double> outputResults = new Dictionary<DollarTemplateAverage, double>(recoResults.Count);
            for (int i = values.Count - 1; i >= 0; i--)
                outputResults.Add(recoResults[values[i]], values[i]);
            

            return outputResults;
        }

        private double EuclideanDistanceAtBestAngle(List<AveragePoint> points, DollarTemplateAverage T, double thetaA, double thetaB, double thetaDelta, ref double theta)
        {
            double f1 = EuclideanDistanceAtAngle(points, T, thetaA);
            double f2 = EuclideanDistanceAtAngle(points, T, thetaB);

            while (Math.Abs(thetaB - thetaA) > thetaDelta)
            {
                if (f1 < f2)
                {
                    thetaB = (1 - PHI) * thetaA + PHI * thetaB;
                    f2 = EuclideanDistanceAtAngle(points, T, thetaB);
                }
                else
                {
                    thetaA = PHI * thetaA + (1 - PHI) * thetaB;
                    f1 = EuclideanDistanceAtAngle(points, T, thetaA);
                }
            }

            if (f1 < f2)
            {
                theta = thetaB;
                return f1;
            }
            else
            {
                theta = thetaA;
                return f2;
            }
        }

        /// <summary>
        /// Find the optimal angle and compute the distance at this angle. Uses the Golden Section Search.
        /// </summary>
        /// <param name="points">Points trying to be matched to template</param>
        /// <param name="T">Template being matched to</param>
        /// <param name="thetaA">Initial lowest angle</param>
        /// <param name="thetaB">Initial highest angle</param>
        /// <param name="thetaDelta">Threshold below which to stop searching</param>
        /// <returns>Distance at best angle</returns>
        private double distanceAtBestAngle(List<AveragePoint> points, DollarTemplateAverage T, double thetaA, double thetaB, double thetaDelta, ref double theta)
        {
            double f1 = distanceAtAngle(points, T, thetaA);
            double f2 = distanceAtAngle(points, T, thetaB);

            while (Math.Abs(thetaB - thetaA) > thetaDelta)
            {
                if (f1 < f2)
                {
                    thetaB = (1 - PHI) * thetaA + PHI * thetaB;
                    f2 = distanceAtAngle(points, T, thetaB);
                }
                else
                {
                    thetaA = PHI * thetaA + (1 - PHI) * thetaB;
                    f1 = distanceAtAngle(points, T, thetaA);
                }
            }

            if (f1 < f2)
            {
                theta = thetaB;
                return f1;
            }
            else
            {
                theta = thetaA;
                return f2;
            }
        }

        /// <summary>
        /// Calculates the distance at a specified angle theta.
        /// </summary>
        /// <param name="points">Points being rotated by an angle</param>
        /// <param name="T">Template being matched to</param>
        /// <param name="theta">Angle to rotate points</param>
        /// <returns>Distance at specified angle</returns>
        private double distanceAtAngle(List<AveragePoint> avgPoints, DollarTemplateAverage T, double theta)
        {
            List<Point> points = GetPoints(avgPoints);
            List<Point> newPoints = rotateBy(points, theta);

            double dist = pathDistance(newPoints, T.m_AveragePoints) / (double)points.Count;

            return dist;
        }

        /// <summary>
        /// Computes the total Euclidean Distance between two templates at a specified angle
        /// </summary>
        /// <param name="avgPoints"></param>
        /// <param name="T"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        private double EuclideanDistanceAtAngle(List<AveragePoint> avgPoints, DollarTemplateAverage T, double theta)
        {
            List<Point> points = GetPoints(avgPoints);
            List<Point> newPoints = rotateBy(points, theta);

            return EuclideanDistance(newPoints, T.m_AveragePoints) / (double)points.Count;
        }

        /// <summary>
        /// Total Euclidean Distance between two lists of points
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private double EuclideanDistance(List<Point> A, List<AveragePoint> B)
        {
            double d = 0.0;
            if (A.Count != B.Count)
                throw new Exception("Mismatch in number of points - EuclideanDistance for DollarTemplate");

            for (int i = 0; i < A.Count; i++)
                d += Compute.EuclideanDistance(A[i], B[i].SketchPoint);

            return d;
        }

        /// <summary>
        /// Get the current list of sketch points based on average points
        /// </summary>
        /// <param name="avgPoints"></param>
        /// <returns></returns>
        private List<Point> GetPoints(List<AveragePoint> avgPoints)
        {
            List<Point> points = new List<Point>(avgPoints.Count);
            foreach (AveragePoint pt in avgPoints)
                points.Add(pt.SketchPoint);

            return points;
        }

        /// <summary>
        /// Find the average of the distances between each corresponding point in lists A and B.
        /// </summary>
        /// <param name="A">List 1 of points to compare</param>
        /// <param name="B">List 2 of points to compare</param>
        /// <returns>Average distance between corresponding points</returns>
        private double pathDistance(List<Point> A, List<AveragePoint> B)
        {
            double d = 0.0;
            if (A.Count != B.Count)
                throw new Exception("Mismatch in number of points - pathDistance for DollarTemplate");

            for (int i = 0; i < A.Count; i++)
                d += StdDevDistance(A[i], B[i]);

            return d;
        }

        /// <summary>
        /// Distance metric using how far away the point is from the average
        /// points by number of standard deviations
        /// </summary>
        /// <param name="point"></param>
        /// <param name="averagePoint"></param>
        /// <returns></returns>
        private double StdDevDistance(Point point, AveragePoint averagePoint)
        {
            double dX = averagePoint.StdDevX(point);
            double dY = averagePoint.StdDevY(point);

            double squared = Math.Pow(dX, 2.0) + Math.Pow(dY, 2.0);

            return Math.Sqrt(squared);
        }

        #endregion

        #region Getters

        public string Name
        {
            get { return m_Name; }
        }

        public List<float[]> Points
        {
            get 
            {
                List<float[]> points = new List<float[]>();

                foreach (AveragePoint pt in m_AveragePoints)
                {
                    Point s = pt.SketchPoint;
                    points.Add(new float[4] { s.X, s.Y, pt.StdX, pt.StdY });
                }

                return points; 
            }
        }

        #endregion

        public static List<DollarTemplateAverage> LoadTemplates(string dir)
        {
            /*
            string dir = Directory.GetCurrentDirectory();
            DirectoryInfo info = Directory.GetParent(dir);
            info = Directory.GetParent(info.FullName);
            dir = info.FullName + "\\Training Data";
            */

            List<string> files = new List<string>(System.IO.Directory.GetFiles(dir));
            List<DollarTemplateAverage> templates = new List<DollarTemplateAverage>();

            foreach (string file in files)
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(file);
                string name = reader.ReadLine();
                string numPoints = reader.ReadLine();

                string line;
                List<Sketch.Point> points = new List<Sketch.Point>();
                while ((line = reader.ReadLine()) != null && line != "")
                {
                    string[] data = line.Split(",".ToCharArray());
                    if (data.Length > 2)
                    {
                        string strX = data[0];
                        string strY = data[1];
                        float x, y;
                        bool goodX = float.TryParse(strX, out x);
                        bool goodY = float.TryParse(strY, out y);
                        if (goodX && goodY)
                            points.Add(new Sketch.Point(x, y));
                    }
                }

                reader.Close();

                bool foundTemplate = false;
                foreach (DollarTemplateAverage temp in templates)
                {
                    if (temp.Name == name)
                    {
                        foundTemplate = true;
                        temp.AddExample(points);
                    }
                }

                if (!foundTemplate)
                    templates.Add(new DollarTemplateAverage(points, name));
            }

            return templates;
        }
    }

    [Serializable]
    [DebuggerDisplay("Name={m_Name}")]
    public class DollarTemplate
    {
        #region Constants and Parameters

        private const double SCORE_THRESHOLD = 0.60;
        private const int NumResampledPoints = 48;
        private const double THETA_MAX = Math.PI / 4.0; // 45 degrees
        private const double THETA_DELTA = 2.0 * Math.PI / 180.0; // 2.0 degrees
        private const double SIZE = 100.0;
        private const double PHI = 0.6180339887499; //0.5 * (-1 + Math.Sqrt(5.0)); // Golden ratio

        #endregion

        #region Member variables

        /// <summary>
        /// Symbol name
        /// </summary>
        string m_Name;

        /// <summary>
        /// List of point averages and std devs
        /// </summary>
        List<Point> m_Points;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DollarTemplate(List<Point> originalPoints, string SymbolName)
        {
            m_Name = SymbolName;
            List<Point> points = new List<Point>(originalPoints);
            List<Point> rsPoints = ReSamplePoints(points);
            rsPoints = rotateToZero(rsPoints);
            rsPoints = scaleToSquare(rsPoints, SIZE);
            rsPoints = translateToOrigin(rsPoints);

            m_Points = rsPoints;
        }

        public DollarTemplate(List<Point> originalPoints)
        {
            m_Name = "Unknown";
            List<Point> points = new List<Point>(originalPoints);
            List<Point> rsPoints = ReSamplePoints(points);
            rsPoints = rotateToZero(rsPoints);
            rsPoints = scaleToSquare(rsPoints, SIZE);
            rsPoints = translateToOrigin(rsPoints);

            m_Points = rsPoints;
        }

        #endregion

        #region Computations for Template (reSample, rotate, translate, etc.)

        /// <summary>
        /// Resamples the points into a fixed number for 1-to-1 comparison
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private List<Point> ReSamplePoints(List<Point> points)
        {
            List<Point> newPoints = new List<Point>(NumResampledPoints);
            double aLength = ArcLength(points);
            double I = aLength / (NumResampledPoints - 1); // Interval after which to add a new point
            double D = 0.0;
            double d = 0.0;

            float x0 = points[0].X;
            float y0 = points[0].Y;
            newPoints.Add(new Point(x0, y0));

            for (int i = 1; i < points.Count; i++)
            {
                d = Compute.EuclideanDistance(points[i - 1], points[i]);

                if (D + d >= I)
                {
                    float x = points[i - 1].X + (float)((I - D) / d * (points[i].X - points[i - 1].X));
                    float y = points[i - 1].Y + (float)((I - D) / d * (points[i].Y - points[i - 1].Y));
                    Point q = new Point(x, y);
                    newPoints.Add(q);
                    points.Insert(i, q);
                    D = 0.0;
                }
                else
                    D = D + d;
            }

            if (newPoints.Count < NumResampledPoints)
                newPoints.Add(points[points.Count - 1]);

            return newPoints;
        }

        /// <summary>
        /// Computes the arc length of the list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private double ArcLength(List<Point> points)
        {
            double d = 0.0;
            for (int i = 1; i < points.Count; i++)
                d += Compute.EuclideanDistance(points[i - 1], points[i]);

            return d;
        }

        /// <summary>
        /// Rotate the shape so that the first point of the stroke is at zero degrees.
        /// </summary>
        /// <param name="points">List of points to be rotated</param>
        /// <returns>List of new, rotated points</returns>
        private List<Point> rotateToZero(List<Point> points)
        {
            Point c = centroid(points);
            double theta;

            theta = Math.Atan2((double)(points[0].Y - c.Y), (double)(points[0].X - c.X));
            while (theta > Math.PI || theta <= -Math.PI)
            {
                if (theta > Math.PI)
                    theta -= 2 * Math.PI;
                else if (theta <= -Math.PI)
                    theta += 2 * Math.PI;
            }

            List<Point> newPoints = rotateBy(points, -theta);

            return newPoints;
        }

        /// <summary>
        /// Rotates a list of points by a specified angle theta.
        /// </summary>
        /// <param name="points">List of points to be rotated</param>
        /// <param name="theta">Angle to be rotated by</param>
        /// <returns>List of new, rotated points</returns>
        private List<Point> rotateBy(List<Point> points, double theta)
        {
            List<Point> newPoints = new List<Point>(points.Count);
            Point c = centroid(points);

            for (int i = 0; i < points.Count; i++)
            {
                float x1 = points[i].X - c.X;
                float y1 = points[i].Y - c.Y;
                double cosTheta = Math.Cos(theta);
                double sinTheta = Math.Sin(theta);
                double dx = x1 * cosTheta - y1 * sinTheta;// (points[i].X - c.X) * Math.Cos(theta) - (points[i].Y - c.Y) * Math.Sin(theta);
                double dy = x1 * sinTheta + y1 * cosTheta;// (points[i].X - c.X) * Math.Sin(theta) + (points[i].Y - c.Y) * Math.Cos(theta);
                float x = (float)dx + c.X;
                float y = (float)dy + c.Y;
                newPoints.Add(new Point(x, y));
            }

            return newPoints;
        }

        /// <summary>
        /// Finds the centroid of a list of points (average x, average y)
        /// </summary>
        /// <param name="points">List of points to find centroid of</param>
        /// <returns>Centroid Point</returns>
        private Point centroid(List<Point> points)
        {
            float x = 0f;
            float y = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                x += points[i].X;
                y += points[i].Y;
            }

            x /= (float)points.Count;
            y /= (float)points.Count;

            return new Point(x, y);
        }

        /// <summary>
        /// Non-uniform scaling of the list of points to a square.
        /// </summary>
        /// <param name="points">List of points to be scaled</param>
        /// <param name="size">Size of one edge of the square</param>
        /// <returns>List of new points that have been scaled</returns>
        private List<Point> scaleToSquare(List<Point> points, double size)
        {
            List<Point> newPoints = new List<Point>(points.Count);
            System.Drawing.RectangleF bBox = Compute.BoundingBox(points.ToArray());

            for (int i = 0; i < points.Count; i++)
            {
                float x = points[i].X * (float)(size) / bBox.Width;
                float y = points[i].Y * (float)(size) / bBox.Height;
                newPoints.Add(new Point(x, y));
            }

            return newPoints;
        }

        /// <summary>
        /// Move the points so that their centroid is at the origin. Does not mean that
        /// the bounding box is centered at the origin.
        /// </summary>
        /// <param name="points">List of points to be translated</param>
        /// <returns>New list of translated points</returns>
        private List<Point> translateToOrigin(List<Point> points)
        {
            List<Point> newPoints = new List<Point>(points.Count);
            Point c = centroid(points);

            for (int i = 0; i < points.Count; i++)
            {
                float x = points[i].X - c.X;
                float y = points[i].Y - c.Y;
                newPoints.Add(new Point(x, y));
            }

            return newPoints;
        }

        #endregion

        #region Recognition / Comparison

        public string Recognize(List<DollarTemplate> templates)
        {
            if (m_Points.Count != NumResampledPoints)
                return "Unknown";

            double score = 0.0;
            DollarTemplate best = RecognizeSymbol(templates, out score);
            if (score < SCORE_THRESHOLD)
                return "Unknown";

            return best.Name;
        }

        /// <summary>
        /// Match this symbol to the best Template in templates.
        /// </summary>
        /// <param name="templates">Existing templates with which to match</param>
        /// <param name="score">Score of best matching template</param>
        public DollarTemplate RecognizeSymbol(List<DollarTemplate> templates, out double score)
        {
            score = 0.0;
            if (templates.Count == 0)
                return null;

            DollarTemplate Tbest = templates[0];
            double bestD = double.MaxValue;
            double bestTheta = 0.0;
            double d = 0.0;

            for (int i = 0; i < templates.Count; i++)
            {
                double theta;
                d = EuclideanDistanceAtBestAngle(m_Points, templates[i], -THETA_MAX, THETA_MAX, THETA_DELTA, out theta);

                if (d < bestD)
                {
                    bestD = d;
                    bestTheta = theta;
                    Tbest = templates[i];
                }
            }

            score = 1 - bestD / (0.5 * Math.Sqrt(Math.Pow(SIZE, 2.0) * 2));

            return Tbest;
        }

        private double EuclideanDistanceAtBestAngle(List<Point> points, DollarTemplate T, double thetaA, double thetaB, double thetaDelta, out double theta)
        {
            double f1 = EuclideanDistanceAtAngle(points, T, thetaA);
            double f2 = EuclideanDistanceAtAngle(points, T, thetaB);

            while (Math.Abs(thetaB - thetaA) > thetaDelta)
            {
                if (f1 < f2)
                {
                    thetaB = (1 - PHI) * thetaA + PHI * thetaB;
                    f2 = EuclideanDistanceAtAngle(points, T, thetaB);
                }
                else
                {
                    thetaA = PHI * thetaA + (1 - PHI) * thetaB;
                    f1 = EuclideanDistanceAtAngle(points, T, thetaA);
                }
            }

            if (f1 < f2)
            {
                theta = thetaB;
                return f1;
            }
            else
            {
                theta = thetaA;
                return f2;
            }
        }

        /// <summary>
        /// Computes the total Euclidean Distance between two templates at a specified angle
        /// </summary>
        /// <param name="avgPoints"></param>
        /// <param name="T"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        private double EuclideanDistanceAtAngle(List<Point> points, DollarTemplate T, double theta)
        {
            List<Point> newPoints = rotateBy(points, theta);

            return EuclideanDistance(newPoints, T.m_Points) / (double)points.Count;
        }

        /// <summary>
        /// Total Euclidean Distance between two lists of points
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private double EuclideanDistance(List<Point> A, List<Point> B)
        {
            double d = 0.0;
            if (A.Count != B.Count)
                throw new Exception("Mismatch in number of points - EuclideanDistance for DollarTemplate");

            for (int i = 0; i < A.Count; i++)
                d += Compute.EuclideanDistance(A[i], B[i]);

            return d;
        }

        #endregion

        #region Getters

        public string Name
        {
            get { return m_Name; }
        }

        #endregion
    }

    [Serializable]
    class AveragePoint
    {
        float m_X;
        float m_Y;
        List<float> m_AllX;
        List<float> m_AllY;
        float m_StdDevX;
        float m_StdDevY;

        public AveragePoint(Point pt)
        {
            m_AllX = new List<float>();
            m_AllX.Add(pt.X);
            m_AllY = new List<float>();
            m_AllY.Add(pt.Y);

            FindAvgsAndStdDevs();
        }

        private void FindAvgsAndStdDevs()
        {
            m_X = Compute.Mean(m_AllX);
            m_Y = Compute.Mean(m_AllY);
            if (m_AllX.Count > 1 && m_AllY.Count > 1)
            {
                m_StdDevX = (float)Compute.StandardDeviation(m_AllX);
                m_StdDevY = (float)Compute.StandardDeviation(m_AllY);
                if (m_StdDevX < 1f)
                    m_StdDevX = 1f;
                if (m_StdDevY < 1f)
                    m_StdDevY = 1f;
            }
            else
            {
                m_StdDevX = 10f;
                m_StdDevY = 10f;
            }
        }

        public void AddPoint(Point pt)
        {
            m_AllX.Add(pt.X);
            m_AllY.Add(pt.Y);

            FindAvgsAndStdDevs();
        }

        public Point SketchPoint
        {
            get { return new Point(m_X, m_Y); }
        }

        public double StdDevX(Point pt)
        {
            double delta = pt.X - m_X;

            return delta / m_StdDevX;
        }

        public double StdDevY(Point pt)
        {
            double delta = pt.Y - m_Y;

            return delta / m_StdDevY;
        }

        public float StdX
        {
            get { return m_StdDevX; }
        }

        public float StdY
        {
            get { return m_StdDevY; }
        }
    }
}
