using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;

namespace Featurefy
{
    /// <summary>
    /// Line Class
    /// </summary>
    [DebuggerDisplay("X1 = {endpoint1.X}, Y1 = {endpoint1.Y}, X2 = {endpoint2.X}, Y2 = {endpoint2.Y}, m = {slope}, b = {intercept}")]
    public class Line
    {
        #region Member Variables
        private float slope;
        private float intercept;
        private PointF endpoint1;
        private PointF endpoint2;
        private Guid Id;
        private bool isEndLine;
        #endregion

        #region Constructors
        /// <summary>
        /// Main Constructor, takes two System.Drawing.PointF points.
        /// </summary>
        /// <param name="p1">First point</param>
        /// <param name="p2">Second point</param>
        public Line(PointF p1, PointF p2)
        {
            this.Id = Guid.NewGuid();
            this.endpoint1 = p1;
            this.endpoint2 = p2;
            float[] temp = computeLine(p1, p2);
            this.slope = temp[0];
            this.intercept = temp[1];
            this.isEndLine = false;
        }

        /// <summary>
        /// Constructor which creates a ray from a point at a specified angle
        /// Angle must be in radians
        /// </summary>
        /// <param name="p1">Point to start ray from</param>
        /// <param name="angle">Angle to make ray in radians</param>
        public Line(PointF p1, double angle)
        {
            this.Id = Guid.NewGuid();
            this.endpoint1 = p1;
            double x = (double)(p1.X + 100000.0f * (float)Math.Cos(angle));
            double y = (double)(p1.Y + 100000.0f * (float)Math.Sin(angle));
            PointF p2 = new PointF((float)x, (float)y);
            this.endpoint2 = p2;
            float[] temp = computeLine(p1, p2);
            this.slope = temp[0];
            this.intercept = temp[1];
            this.isEndLine = false;
        }

        /// <summary>
        /// Main Constructor, takes two System.Drawing.PointF points.
        /// </summary>
        /// <param name="p1">First point</param>
        /// <param name="p2">Second point</param>
        /// <param name="isEndLine"></param>
        public Line(PointF p1, PointF p2, bool isEndLine)
        {
            this.Id = Guid.NewGuid();
            this.endpoint1 = p1;
            this.endpoint2 = p2;
            float[] temp = computeLine(p1, p2);
            this.slope = temp[0];
            this.intercept = temp[1];
            this.isEndLine = isEndLine;
        }

        /// <summary>
        /// Constructor which creates a ray from a point at a specified angle
        /// Angle must be in radians
        /// </summary>
        /// <param name="p1">Point to start ray from</param>
        /// <param name="angle">Angle to make ray in radians</param>
        /// <param name="isEndLine"></param>
        public Line(PointF p1, double angle, bool isEndLine)
        {
            this.Id = Guid.NewGuid();
            this.endpoint1 = p1;
            double x = (double)(p1.X + 100000.0f * (float)Math.Cos(angle));
            double y = (double)(p1.Y + 100000.0f * (float)Math.Sin(angle));
            PointF p2 = new PointF((float)x, (float)y);
            this.endpoint2 = p2;
            float[] temp = computeLine(p1, p2);
            this.slope = temp[0];
            this.intercept = temp[1];
            this.isEndLine = isEndLine;
        }

        #endregion

        #region Public Functions
        /// <summary>
        /// Extend the line by a given length (from endpoint1 to endpoint2)
        /// </summary>
        /// <param name="length">Amount to extend the line</param>
        public void extend(double length)
        {
            float dx = this.endpoint2.X - this.endpoint1.X;
            float dy = this.endpoint2.Y - this.endpoint1.Y;
            double theta = Math.Atan2((double)dy, (double)dx);
            //PointF temp = this.endpoint2;
            PointF p1 = new PointF();
            //double theta = Math.Atan((double)this.slope);

            p1.X = this.endpoint2.X + (float)(length * Math.Cos(theta));
            p1.Y = this.endpoint2.Y + (float)(length * Math.Sin(theta));

            this.endpoint2 = p1;
            //this.endpoint1 = temp;
        }

        /// <summary>
        /// Gets the length of the line
        /// </summary>
        public double LineLength
        {
            get
            {
                double dx = (double)(this.endpoint2.X - this.endpoint1.X);
                double dy = (double)(this.endpoint2.Y - this.endpoint1.Y);

                double dx2 = Math.Pow(dx, 2.0);
                double dy2 = Math.Pow(dy, 2.0);

                return Math.Sqrt(dx2 + dy2);
            }
        }

        /// <summary>
        /// Returns true iff this line and line a have the same endpoints
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public bool sameAs(Line a)
        {
            bool same = false;

            if (this.endpoint1 == a.endpoint1)
            {
                if (this.endpoint2 == a.endpoint2)
                {
                    same = true;
                }
            }

            return same;
        }
        #endregion

        #region Getters & Setters

        /// <summary>
        /// Get the slope of the line
        /// </summary>
        public float Slope
        {
            get { return this.slope; }
        }

        /// <summary>
        /// Get the intercept of the line
        /// </summary>
        public float Intercept
        {
            get { return this.intercept; }
        }

        /// <summary>
        /// Get the first endpoint of the line
        /// </summary>
        public PointF EndPoint1
        {
            get { return this.endpoint1; }
        }

        /// <summary>
        /// et the second endpoint of the line
        /// </summary>
        public PointF EndPoint2
        {
            get { return this.endpoint2; }
        }

        /// <summary>
        /// Get the ID of the line
        /// </summary>
        public Guid ID
        {
            get { return this.Id; }
        }

        /// <summary>
        /// Returns true if this line is an end line
        /// </summary>
        public bool IsEndLine
        {
            get { return this.isEndLine; }
        }
        #endregion

        #region Private Functions
        private float[] computeLine(PointF p1, PointF p2)
        {
            float[] att = new float[2];

            // Compute slope of the line
            if (p2.X != p1.X)
                att[0] = (p2.Y - p1.Y) / (p2.X - p1.X);
            else
                att[0] = (p2.Y - p1.Y) / 0.01f;
            // Compute y-intersection of the line
            att[1] = p1.Y - att[0] * p1.X;

            return att;
        }
        #endregion

        #region Static Functions
        /// <summary>
        /// Determine whether two line segments intersect
        /// </summary>
        /// <param name="a">First Line Segment</param>
        /// <param name="b">Second Line Segment</param>
        /// <returns>Bool variable indicating whether the two line segments intersect</returns>
        public static bool intersects(Line a, Line b)
        {
            PointF p1 = findIntersection(a, b);
            bool intersects = false;

            if ((p1.X >= a.endpoint1.X && p1.X <= a.endpoint2.X) 
                || (p1.X >= a.endpoint2.X && p1.X <= a.endpoint1.X))
            {
                if ((p1.Y >= a.endpoint1.Y && p1.Y <= a.endpoint2.Y)
                    || (p1.Y >= a.endpoint2.Y && p1.Y <= a.endpoint1.Y))
                {
                    if ((p1.X >= b.endpoint1.X && p1.X <= b.endpoint2.X)
                        || (p1.X >= b.endpoint2.X && p1.X <= b.endpoint1.X))
                    {
                        if ((p1.Y >= b.endpoint1.Y && p1.Y <= b.endpoint2.Y)
                            || (p1.Y >= b.endpoint2.Y && p1.Y <= b.endpoint1.Y))
                        {

                            intersects = true;
                        }
                    }
                }
            }

            return intersects;
        }

        /// <summary>
        /// Finds the point of intersection of the two lines
        /// </summary>
        /// <param name="a">First Line</param>
        /// <param name="b">Second Line</param>
        /// <returns>System.Drawing.PointF at the intersection point</returns>
        public static PointF findIntersection(Line a, Line b)
        {
            // Taken from http://mathworld.wolfram.com/Line-LineIntersection.html
            float determinant = (a.endpoint1.X - a.endpoint2.X) * (b.endpoint1.Y - b.endpoint2.Y)
                - (b.endpoint1.X - b.endpoint2.X) * (a.endpoint1.Y - a.endpoint2.Y);

            float detA = (a.endpoint1.X * a.endpoint2.Y) - (a.endpoint2.X * a.endpoint1.Y);
            float detB = (b.endpoint1.X * b.endpoint2.Y) - (b.endpoint2.X * b.endpoint1.Y);

            float detX = (detA * (b.endpoint1.X - b.endpoint2.X)) - (detB * (a.endpoint1.X - a.endpoint2.X));
            float detY = (detA * (b.endpoint1.Y - b.endpoint2.Y)) - (detB * (a.endpoint1.Y - a.endpoint2.Y));

            if (determinant != 0)
                return new PointF((detX / determinant), (detY / determinant));
            else
                return new PointF(0.0f, 0.0f);
        }

        /// <summary>
        /// Finds the intersection distance along line A.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="DistanceToEndPoint1"></param>
        /// <returns></returns>
        public static float findIntersectionDistanceAlongLineA(Line a, Line b, bool DistanceToEndPoint1)
        {
            // Taken from http://mathworld.wolfram.com/Line-LineIntersection.html
            float determinant = (a.endpoint1.X - a.endpoint2.X) * (b.endpoint1.Y - b.endpoint2.Y)
                - (b.endpoint1.X - b.endpoint2.X) * (a.endpoint1.Y - a.endpoint2.Y);

            float detA = (a.endpoint1.X * a.endpoint2.Y) - (a.endpoint2.X * a.endpoint1.Y);
            float detB = (b.endpoint1.X * b.endpoint2.Y) - (b.endpoint2.X * b.endpoint1.Y);

            float detX = (detA * (b.endpoint1.X - b.endpoint2.X)) - (detB * (a.endpoint1.X - a.endpoint2.X));
            float detY = (detA * (b.endpoint1.Y - b.endpoint2.Y)) - (detB * (a.endpoint1.Y - a.endpoint2.Y));

            if (determinant != 0)
            {
                PointF p = new PointF((detX / determinant), (detY / determinant));
                float length = Compute.EuclideanDistance(a.EndPoint1, a.EndPoint2);
                float f = Compute.EuclideanDistance(a.EndPoint1, p);
                if (DistanceToEndPoint1)
                    return f / length;
                else
                    return 1f - (f / length);
            }
            else
                return -1f;
        }
        #endregion
    }
}
