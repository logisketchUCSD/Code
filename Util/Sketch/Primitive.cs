using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.Ink;
using MathNet.Numerics.LinearAlgebra;

namespace Sketch
{
    /// <summary>
    /// Primitive subshape representations
    /// </summary>
    public abstract class Primitive
    {
        #region Member Variables
        private Guid id;
		/// <summary>
		/// A constant for allowing intersections slightly off the end of strokes
		/// </summary>
		protected static double INTERSECTION_TOLERANCE_PERCENTAGE = 0.05;
        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public Primitive()
        {
            this.id = Guid.NewGuid();
        }
        #endregion


        #region Interface Functions
        /// <summary>
        /// Draw the shape
        /// </summary>
        /// <param name="g">The graphics object</param>
        /// <param name="r">The renderer</param>
		/// <param name="inkMovedX">Distance ink moved in the X direction</param>
		/// <param name="inkMovedY">Distance ink moved in the Y direction</param>
		/// <param name="scale">Scale</param>
		public abstract void draw(Graphics g, Renderer r, float inkMovedX, float inkMovedY, float scale);

		/// <summary>
		/// Do two primitives intersect?
		/// </summary>
		/// <param name="other">The primitive to compare to</param>
		/// <returns>True iff they intersect</returns>
		public abstract bool intersects(Primitive other);

		/// <summary>
		/// At what point to two primitives intersect?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public abstract double[] intersection(Primitive other);

		/// <summary>
		/// Static function to see if two Primitives intersect
		/// </summary>
		/// <param name="lhs">Ths LHS primitive</param>
		/// <param name="rhs">The RHS primitive</param>
		/// <returns>True iff the primitives intersect</returns>
		public static bool intersects(Primitive lhs, Primitive rhs)
		{
			return lhs.intersects(rhs);
		}
        #endregion
    }



    /// <summary>
    /// A primitive representing a line segment
    /// </summary>
    public class LineSegment : Primitive
    {
        #region Member Variables
        private double slope;
        private double intercept;
        private double score;
        private double error;
        private Point startPoint;
        private Point endPoint;
        #endregion


        #region Constructor

        /// <summary>
        /// Create a new line segment
        /// </summary>
        /// <param name="points">The points that go in the line</param>
        public LineSegment(Point[] points)
			: base()
        {
            if (points.Length > 0)
            {
                findEndPoints(points);
                fitLeastSquares(points);
                this.score = 1.0 - this.error / this.LineLength;
            }
            else
            {
                slope = 0.0;
                intercept = 0.0;
				score = double.NegativeInfinity;
				error = double.PositiveInfinity;
                this.startPoint = new Point();
				this.endPoint = new Point();
            }
        }

		/// <summary>
		/// Create a new line segment between two points
		/// </summary>
		/// <param name="EndPoint1">One endpoint</param>
		/// <param name="EndPoint2">The other endpoint</param>
		public LineSegment(Point EndPoint1, Point EndPoint2)
			:base()
		{
			slope = (EndPoint2.Y - EndPoint1.Y) / (EndPoint2.X - EndPoint1.X);
			intercept = slope * (EndPoint1.Y - EndPoint1.X);
			startPoint = EndPoint1;
			endPoint = EndPoint2;
			score = 1.0 / LineLength;
			error = 0.0;
		}

		/// <summary>
		/// Construct a new line segment following the line y=mx + b with endpoints
		/// as close as possible to EndPoint1 and EndPoint2
		/// </summary>
		/// <param name="EndPoint1">The bound on one of the endpoints</param>
		/// <param name="EndPoint2">The bound on the other endpoint</param>
		/// <param name="m">The slope of the line</param>
		/// <param name="b">The intercept of the line</param>
		public LineSegment(Point EndPoint1, Point EndPoint2, double m, double b)
		{
			slope = m;
			intercept = b;
			if (EndPoint1.X < EndPoint2.X)
			{
				startPoint = getClosestPointOnLine(EndPoint1);
				endPoint = getClosestPointOnLine(EndPoint2);
			}
			else
			{
				startPoint = getClosestPointOnLine(EndPoint2);
				endPoint = getClosestPointOnLine(EndPoint1);
			}
			score = 1.0 / LineLength;
			error = 0.0;
		}

		/// <summary>
		/// Convert a list of points into a local-linear approximation of them
		/// </summary>
		/// <param name="lp">The list of points (in time-sorted order)</param>
		/// <returns>The list of line segments, with one line segment between every 2 points</returns>
		public static List<LineSegment> PointsToLineSegments(List<Point> lp)
		{
			List<LineSegment> ls = new List<LineSegment>(lp.Count - 1);
			for (int i = 0; i < lp.Count - 1; ++i)
			{
				ls.Add(new LineSegment(lp[i], lp[i + 1]));
			}
			return ls;
		}

		#endregion

		#region Misc

		/// <summary>
		/// Get the point on the line given by the slope and intercept
		/// that is as close as possible to point P
		/// </summary>
		/// <param name="p">The point to search around</param>
		/// <returns>A new Point</returns>
		public Point getClosestPointOnLine(Point p)
		{
			double x = (p.Y + (1 / slope) * p.X - intercept) / (slope + 1 / slope);
			double y = slope * x + intercept;
			return new Point((float)x, (float)y);
		}

		/// <summary>
		/// Find the distance to the closest endpoint from the given point
		/// </summary>
		/// <param name="inter">[x, y]</param>
		/// <returns>The distance</returns>
		public double distanceToEndpoint(double[] inter)
		{
			return Math.Min(Math.Sqrt(Math.Pow(inter[0] - startPoint.X, 2) + Math.Pow(inter[1] - startPoint.Y, 2)),
				Math.Sqrt(Math.Pow(inter[0] - endPoint.X, 2) + Math.Pow(inter[1] - endPoint.Y, 2)));
		}

		#endregion

		#region Private Functions

		private void findEndPoints(Point[] points)
        {
            this.startPoint = points[0];
            this.endPoint = points[points.Length - 1];
        }

        private void fitLeastSquares(Point[] points)
        {
            // This function adapts a solution from Wolfram MathWorld
            // http://mathworld.wolfram.com/LeastSquaresFittingPerpendicularOffsets.html

            // Initiallize variables
            int n = points.Length;   // Number of points
            double Sx = 0.0;    // Sum of all x values
            double Sy = 0.0;    // Sum of all y values
            double Sx2 = 0.0;   // Sum of all x^2 values
            double Sy2 = 0.0;   // Sum of all y^2 values
            double Sxy = 0.0;   // Sum of all x*y values
            double S = 0.0;     // Sum of distances from fit line to points, quantity squared
            double a, b;
            double[] x = new double[n];
            double[] y = new double[n];

            for (int i = 0; i < n; i++)
            {
                x[i] = (double)points[i].X;
                y[i] = (double)points[i].Y;
            }

            // Find the sums of various x and y quantities
            for (int i = 0; i < n; i++)
            {
                Sx += x[i];
                Sy += y[i];
                Sx2 += Math.Pow(x[i], 2.0);
                Sy2 += Math.Pow(y[i], 2.0);
                Sxy += x[i] * y[i];
            }

            // Find average x and y values
            double xBar = Sx / (double)n;
            double yBar = Sy / (double)n;

            // Equation 18 on the Wolfram site
            double B = 0.5 * ((Sy2 - n * Math.Pow(yBar, 2.0)) - (Sx2 - n * Math.Pow(xBar, 2.0)))
                / (n * xBar * yBar - Sxy);

            // Equation 20 on the Wolfram site
            double b1 = -B + Math.Sqrt(Math.Pow(B, 2.0) + 1);
            double b2 = -B - Math.Sqrt(Math.Pow(B, 2.0) + 1);

            // Equation 8 on the Wolfram site
            double a1 = yBar - b1 * xBar;
            double a2 = yBar - b2 * xBar;

            // Try fitting both lines (a1,b1) and (a2,b2) to determine which is better
            // Equation 4
            double R_1 = 0.0;
            double R_2 = 0.0;
            double num1 = 0.0;
            double num2 = 0.0;
            for (int i = 0; i < n; i++)
            {
                num1 = y[i] - (a1 + b1 * x[i]);
                num2 = y[i] - (a2 + b2 * x[i]);
                R_1 += Math.Abs(num1) / Math.Sqrt((1 + Math.Pow(b1, 2.0)));
                R_2 += Math.Abs(num2) / Math.Sqrt((1 + Math.Pow(b2, 2.0)));
            }

            // Assign a and b based on the better line fit
            if (R_1 < R_2)
            {
                a = a1;
                b = b1;
                S = R_1;
            }
            else
            {
                a = a2;
                b = b2;
                S = R_2;
            }

            this.error = S / n;

            this.slope = b;
            this.intercept = a;

        }

		private double[] lineIntersects(LineSegment other)
		{
			return lineIntersects(other, 0.0d);
		}

		/// <summary>
		/// Do two lines intersect?
		/// </summary>
		/// <param name="other">The other line</param>
		/// <param name="extra">The number of linelengths that rhs can be off the end of lhs</param>
		/// <returns>The coordinates at which the lines intersect, in the form [x, y]. Will be a 0-length array if they do not intersect</returns>
		private double[] lineIntersects(LineSegment other, double extra)
		{
			// Algorithm from
			// http://local.wasp.uwa/edu.au/~pbourke/geometry/lineline2d
			double x1, x2, x3, x4, y1, y2, y3, y4;
			if (startPoint.X < endPoint.X)
			{
				x1 = startPoint.X;
				y1 = startPoint.Y;
				x2 = endPoint.X;
				y2 = endPoint.Y;
			}
			else
			{
				x1 = endPoint.X;
				y1 = endPoint.Y;
				x2 = startPoint.X;
				y2 = startPoint.Y;
			}
			if (other.startPoint.X < other.endPoint.X)
			{
				x3 = other.startPoint.X;
				y3 = other.startPoint.Y;
				x4 = other.endPoint.X;
				y4 = other.endPoint.Y;
			}
			else
			{
				x3 = other.endPoint.X;
				y3 = other.endPoint.Y;
				x4 = other.startPoint.X;
				y4 = other.EndPoint.Y;
			}
			double denom = ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
			if (denom == 0)
				return new double[] { };
			double xtop = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3));
			double ytop = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3));
			double ua = xtop / denom;
			double ub = ytop / denom;
			// Make sure that the intersection point is in the segments
			if ((ua > (0 - extra) && ua < (1 + extra)) && (ub > (0 - extra) && ub < (1+extra)))
			{
				double x = x1 + ua * (x2 - x1);
				double y = y1 + ua * (y2 - y1);
				return new double[] { x, y };
			}
			return new double[] { };
		}

		/// <summary>
		/// Near-intersection
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public double[] nearIntersection(LineSegment other)
		{
			return lineIntersects(other, 25);
		}

        #endregion


        #region Getters & Setters

        /// <summary>
        /// 
        /// </summary>
        public double Slope
        {
            get { return this.slope; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Intercept
        {
            get { return this.intercept; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Score
        {
            get { return this.score; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Error
        {
            get { return this.error; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Point StartPoint
        {
            get { return this.startPoint; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Point EndPoint
        {
            get { return this.endPoint; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double LineLength
        {
			get { return startPoint.distance(endPoint); }
        }

        #endregion


        #region Interface Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
		/// <param name="inkMovedX"></param>
		/// <param name="inkMovedY"></param>
		/// <param name="scale"></param>
		public override void draw(Graphics g, Renderer r, float inkMovedX, float inkMovedY, float scale)
        {
			System.Drawing.Point pt1 = new System.Drawing.Point(
                (int)((this.startPoint.X + inkMovedX) * scale), (int)((this.startPoint.Y + inkMovedY) * scale));
            System.Drawing.Point pt2 = new System.Drawing.Point(
                (int)((this.endPoint.X + inkMovedX) * scale), (int)((this.endPoint.Y + inkMovedY) * scale));
            int rectLength = 4;

            r.InkSpaceToPixel(g, ref pt1);
            r.InkSpaceToPixel(g, ref pt2);

            Rectangle rect1 = new Rectangle(pt1.X - rectLength / 2, pt1.Y - rectLength / 2, rectLength, rectLength);
            Rectangle rect2 = new Rectangle(pt2.X - rectLength / 2, pt2.Y - rectLength / 2, rectLength, rectLength);

            Pen p1 = new Pen(Color.Tan, 1);
            Pen p2 = new Pen(Color.Blue, 2);
            Brush brush1 = Brushes.Tan;

            g.DrawRectangle(p1, rect1);
            g.FillRectangle(brush1, rect1);

            g.DrawRectangle(p1, rect2);
            g.FillRectangle(brush1, rect2);
            g.DrawLine(p2, pt1, pt2);
        }

		/// <summary>
		/// Does this LineSegment intersect another primitive
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool intersects(Primitive other)
		{
			if (other is LineSegment)
				return (lineIntersects(other as LineSegment).Length != 0);
			else
				return other.intersects(this);
		}

		/// <summary>
		/// Where might this LineSegment intersect another primitive?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override double[] intersection(Primitive other)
		{
			if (other is LineSegment)
				return lineIntersects(other as LineSegment);
			else
				return other.intersection(this);
		}
        #endregion
	}



    /// <summary>
    /// A primitive representing an arc segment
    /// </summary>
    public class ArcSegment : Primitive
    {
        #region Constants and Parameters
        const double MIN_SWEEP_ANGLE = 30.0;
        const double SMALL_SWEEP_SCORE = 0.80;
        #endregion


        #region Member Variables
        private double sweepAngle;
        private double startAngle;
        private double radius;
        private PointF centerPoint;
        private double error;
        private double score;
        private Point[] points;
        #endregion


        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        public ArcSegment(Point[] points)
			: base()
        {
            this.points = points;
            if (points.Length > 0)
            {
                fitLeastSquares(points);
                findArcAngles(points);
                this.score = 1 - this.error / this.ArcLength;
                if (Math.Abs(this.sweepAngle) < MIN_SWEEP_ANGLE && this.score > SMALL_SWEEP_SCORE)
                    this.score = SMALL_SWEEP_SCORE;
            }
            else
            {
                this.sweepAngle = 0.0;
                this.startAngle = 0.0;
                this.radius = 0.0;
				this.centerPoint = new PointF();
				score = double.NegativeInfinity;
				error = double.PositiveInfinity;
            }
        }
        #endregion


        #region Private Functions
        private void fitLeastSquares(Point[] points)
        {
            double a, b, c, r;

            double Sx = 0.0;
            double Sy = 0.0;
            double Sx2 = 0.0;
            double Sy2 = 0.0;
            double Sxy = 0.0;
            double term = 0.0;
            double Sa = 0.0;
            double Sb = 0.0;
            double Sc = 0.0;
            double det = 0.0;
            double term1 = 0.0;
            double term2 = 0.0;
            double term3 = 0.0;
            double num1 = 0.0;
            double num2 = 0.0;
            double num3 = 0.0;
            double Sm = 0.0;

            int n = points.Length;

            double[] x = new double[n];
            double[] y = new double[n];
            for (int i = 0; i < n; i++)
            {
                x[i] = (double)points[i].X;
                y[i] = (double)points[i].Y;
            }

            for (int i = 0; i < n; i++)
            {
                Sx += x[i];
                Sy += y[i];
                Sx2 += Math.Pow(x[i], 2.0);
                Sy2 += Math.Pow(y[i], 2.0);
                Sxy += (x[i] * y[i]);
                term = -(Math.Pow(x[i], 2.0) + Math.Pow(y[i], 2.0));
                Sa += term * x[i];
                Sb += term * y[i];
                Sc += term;
            }

            term1 = (2 * Sy2 * n) - (2 * Sy * Sy);
            term2 = (2 * Sxy * n) - (2 * Sx * Sy);
            term3 = (2 * Sxy * 2 * Sy) - (2 * Sx * 2 * Sy2);

            det = (2 * Sx2 * term1) - (2 * Sxy * term2) + (Sx * term3);

            term1 = (2 * Sy2 * n) - (2 * Sy * Sy);
            term2 = (Sb * n) - (Sc * Sy);
            term3 = (Sb * 2 * Sy) - (Sc * 2 * Sy2);

            num1 = (Sa * term1) - (2 * Sxy * term2) + (Sx * term3);

            term1 = (Sb * n) - (Sc * Sy);
            term2 = (2 * Sxy * n) - (2 * Sx * Sy);
            term3 = (2 * Sxy * Sc) - (2 * Sx * Sb);

            num2 = (2 * Sx2 * term1) - (Sa * term2) + (Sx * term3);

            term1 = (2 * Sy2 * Sc) - (2 * Sy * Sb);
            term2 = (2 * Sxy * Sc) - (2 * Sx * Sb);
            term3 = (2 * Sxy * 2 * Sy) - (2 * Sx * 2 * Sy2);

            num3 = (2 * Sx2 * term1) - (2 * Sxy * term2) + (Sa * term3);

            if (det != 0)
            {
                a = num1 / det;
                b = num2 / det;
                c = num3 / det;
            }
            else
            {
                a = 100000.0;
                b = 100000.0;
                c = 100000.0;
            }

            r = Math.Sqrt(a * a + b * b - c);
 
            for (int i = 0; i < n; i++)
            {
                Sm += Math.Abs(Math.Sqrt(Math.Pow(x[i] + a, 2.0) + Math.Pow(y[i] + b, 2.0)) - r);
            }

            radius = r;
			centerPoint = new PointF((float)(-a), (float)(-b));
            error = (1 / (double)n) * Sm;
        }

        private void findArcAngles(Point[] points)
        {
            float deltay = (points[0].Y - this.centerPoint.Y);
            float deltax = (points[0].X - this.centerPoint.X);

            float deltay2 = (points[points.Length - 1].Y - this.centerPoint.Y);
            float deltax2 = (points[points.Length - 1].X - this.centerPoint.X);

            double startAngle = Math.Atan2(deltay, deltax);
            startAngle = startAngle * 180 / Math.PI;
            if (startAngle < 0)
                startAngle = 360 + startAngle;

            double endAngle = Math.Atan2(deltay2, deltax2);
            endAngle = endAngle * 180 / Math.PI;
            if (endAngle < 0)
                endAngle = 360 + endAngle;

            bool axisCrossing = false;
            bool clockwise = false;
            double theta = startAngle;
            double theta_last = startAngle;
            double deltaTheta = 0.0;
            double SumdeltaTheta = 0.0;
            for (int i = 1; i < points.Length; i = i + 5)
            {
                theta = Math.Atan2((points[i].Y - this.centerPoint.Y), (points[i].X - this.centerPoint.X));
                theta = theta * 180 / Math.PI;
                if (theta < 0)
                    theta = 360 + theta;
                deltaTheta = theta - theta_last;
                if (Math.Abs(deltaTheta) > 180)
                {
                    axisCrossing = true;
                    if (theta < 180) deltaTheta += 360;
                    else deltaTheta -= 360;
                }

                SumdeltaTheta += deltaTheta;
                theta_last = theta;
            }

            theta = Math.Atan2((points[points.Length - 1].Y - this.centerPoint.Y),
                (points[points.Length - 1].X - this.centerPoint.X));
            theta = theta * 180 / Math.PI;
            if (theta < 0)
                theta = 360 + theta;
            deltaTheta = theta - theta_last;
            if (Math.Abs(deltaTheta) > 180)
            {
                axisCrossing = true;
                if (theta < 180) deltaTheta += 360;
                else deltaTheta -= 360;
            }

            SumdeltaTheta += deltaTheta;

            if (SumdeltaTheta < 0)
                clockwise = true;

            if (Math.Abs(SumdeltaTheta) > 360)
            {
                if (clockwise)
                    this.sweepAngle = 360;
                else
                    this.sweepAngle = -360;
            }
            else
            {
                if (axisCrossing && clockwise)
                    this.sweepAngle = -(startAngle + (360 - endAngle));
                else if (axisCrossing && !clockwise)
                    this.sweepAngle = endAngle + (360 - startAngle);
                else
                    this.sweepAngle = endAngle - startAngle;
            }
            this.startAngle = startAngle;
        }

		/// <summary>
		/// Find the point of intersection of an arc and a line
		/// </summary>
		/// <param name="other">The line to find intersection with</param>
		/// <returns>[x, y] if applicable; [] otherwise</returns>
		private double[] ArcLineIntersection(LineSegment other)
		{
			double x1, x2, x3, y1, y2, y3, a, b, c, det, u, t;
            int numof_int = 0;

            x1 = (double)other.StartPoint.X; y1 = (double)other.StartPoint.Y;
            x2 = (double)other.EndPoint.X; y2 = (double)other.EndPoint.Y;
            x3 = (double)centerPoint.X; y3 = (double)centerPoint.Y;

            a = Math.Pow(x2 - x1, 2.0) + Math.Pow(y2 - y1, 2.0);
            b = 2 * ((x2 - x1) * (x1 - x3) + (y2 - y1) * (y1 - y3));
            c = Math.Pow(x3, 2.0) + Math.Pow(y3, 2.0) + Math.Pow(x1, 2.0) + Math.Pow(y1, 2.0) - 2 * (x3 * x1 + y3 * y1) - Math.Pow(radius, 2.0);

            if (a == 0.0) return new double[0];

            det = Math.Pow(b, 2.0) - 4 * a * c;

            if (det < 0.0) return new double[0];  //no intersection

			else if (det == 0.0)  //one intersection
			{
				u = (-b) / 2.0 / a;
				if (u > (1 + INTERSECTION_TOLERANCE_PERCENTAGE) || u < (0 - INTERSECTION_TOLERANCE_PERCENTAGE)) return new double[0];

				float x = (float)(x1 + u * (x2 - x1));
				float y = (float)(y1 + u * (y2 - y1));
				double[] intPt = new double[] { x, y };

				double theta = Math.Atan2(intPt[1] - centerPoint.Y, intPt[0] - centerPoint.X) * 180 / Math.PI;

				double endAngle = startAngle + sweepAngle;

				if (sweepAngle > 0)
					t = (theta -startAngle) / (endAngle - startAngle);
				else
					t = -(theta - startAngle) / (endAngle - startAngle);

			}

			else //two intersections
			{
				u = (-b - Math.Sqrt(det)) / 2.0 / a;

				if (!(u > (1 + INTERSECTION_TOLERANCE_PERCENTAGE) || u < (0 - INTERSECTION_TOLERANCE_PERCENTAGE)))
				{
					//intP1.x = x1 + u * (x2 - x1);
					//intP1.y = y1 + u * (y2 - y1);
					numof_int++;
				}

				u = (-b + Math.Sqrt(det)) / 2.0 / a;

				if (!(u > (1 + INTERSECTION_TOLERANCE_PERCENTAGE) || u < (0 - INTERSECTION_TOLERANCE_PERCENTAGE)))
				{
					//intP2.x = x1 + u * (x2 - x1);
					//intP2.y = y1 + u * (y2 - y1);
					numof_int++;
					//if (numof_int == 1) intP1 = intP2;
				}
			}
			return new double[2];
		}
        #endregion


        #region Getters & Setters

        /// <summary>
        /// 
        /// </summary>
        public Point[] Points
        {
            get { return this.points; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double SweepAngle
        {
            get { return this.sweepAngle; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double StartAngle
        {
            get { return this.startAngle; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Radius
        {
            get { return this.radius; }
        }

        /// <summary>
        /// 
        /// </summary>
        public System.Drawing.PointF CenterPoint
        {
            get { return this.centerPoint; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Error
        {
            get { return this.error; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Score
        {
            get { return this.score; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double ArcLength
        {
            get { return this.radius * Math.Abs(this.sweepAngle) * Math.PI / 180.0; }
        }

        #endregion


        #region Interface Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
		/// <param name="inkMovedX"></param>
		/// <param name="inkMovedY"></param>
		/// <param name="scale"></param>
        public override void draw(Graphics g, Renderer r, float inkMovedX, float inkMovedY, float scale)
        {
            System.Drawing.Point centerPoint = new System.Drawing.Point(
                (int)((this.centerPoint.X + inkMovedX) * scale), (int)((this.centerPoint.Y + inkMovedY) * scale));

            int rectLength = 4;
            System.Drawing.Point pt1 = new System.Drawing.Point(
                centerPoint.X + (int)(this.radius * scale * Math.Cos(startAngle * Math.PI / 180.0)),
                centerPoint.Y + (int)(this.radius * scale * Math.Sin(startAngle * Math.PI / 180.0)));

            System.Drawing.Point pt2 = new System.Drawing.Point(
                centerPoint.X + (int)(this.radius * scale * Math.Cos((startAngle + sweepAngle) * Math.PI / 180.0)),
                centerPoint.Y + (int)(this.radius * scale * Math.Sin((startAngle + sweepAngle) * Math.PI / 180.0)));

            // Make a point using the radius value so that its value can be converted from ink space to pixel
            System.Drawing.Point temp = new System.Drawing.Point((int)(this.radius * scale), 0);

            r.InkSpaceToPixel(g, ref pt1);
            r.InkSpaceToPixel(g, ref pt2);
            r.InkSpaceToPixel(g, ref centerPoint);
            r.InkSpaceToPixel(g, ref temp);

            int radius = 1;

            if (temp.X != 0)
                radius = temp.X;

            Rectangle rect = new Rectangle(centerPoint.X - radius, centerPoint.Y - radius, 2 * radius, 2 * radius);
            Rectangle rect1 = new Rectangle(pt1.X - rectLength / 2, pt1.Y - rectLength / 2, rectLength, rectLength);
            Rectangle rect2 = new Rectangle(pt2.X - rectLength / 2, pt2.Y - rectLength / 2, rectLength, rectLength);

            Pen p1 = new Pen(Color.Tan, 1);
            Pen p2 = new Pen(Color.Blue, 2);
            Brush brush1 = Brushes.Tan;

            g.DrawArc(p2, rect, (float)startAngle, (float)sweepAngle);
            g.DrawRectangle(p1, rect1);
            g.FillRectangle(brush1, rect1);

            g.DrawRectangle(p1, rect2);
            g.FillRectangle(brush1, rect2);
        }

		/// <summary>
		/// Does this ArcSegment intersect another primitive?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool intersects(Primitive other)
		{
			if (other is ArcSegment)
				return true;
			else if (other is LineSegment)
				return (ArcLineIntersection(other as LineSegment).Length != 0);
			else
				return other.intersects(this);
		}

		/// <summary>
		/// Where does this ArcSegment intersect another primitive?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override double[] intersection(Primitive other)
		{
			throw new Exception("The method or operation is not implemented.");
		}
        #endregion
    }

    /// <summary>
    /// A primitive representing a circle
    /// </summary>
    public class Circle : Primitive
    {
        #region Member variables
        private Point centerPoint;
        private double radius;
        private double error;
        private double score;
        private Point[] points;
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points"></param>
        public Circle(Point[] points)
            : base()
        {
            this.points = points;

            if (points.Length > 0)
            {
                fitToPoints();
                this.score = 1 - this.error / this.Circumference;
            }
            else
            {
                this.centerPoint = new Point();
                this.radius = 0;
                this.error = 0;
                this.score = 0;
            }
        }

        #endregion

        #region Private Functions

        private void fitToPoints()
        {
            double totalX = 0;
            double totalY = 0;
            foreach (Point pt in points)
            {
                totalX += pt.X;
                totalY += pt.Y;
            }
            centerPoint = new Point((float) totalX / points.Length, (float) totalY / points.Length);

            double totalD = 0;
            foreach (Point pt in points)
            {
                totalD += centerPoint.distance(pt);
            }
            radius = totalD / points.Length;

            double totalE = 0;
            foreach (Point pt in points)
            {
                totalE += Math.Pow(centerPoint.distance(pt) - radius, 2);
            }
            error = totalE / points.Length;
        }

        #endregion

        #region Getters/Setters
        /// <summary>
        /// Gets the circumference of the circle.
        /// </summary>
        public double Circumference
        {
            get { return (2 * Math.PI * radius); }
        }

        /// <summary>
        /// Gets the score of the circle.
        /// </summary>
        public double Score
        {
            get { return score; }
        }

        /// <summary>
        /// Gets the center point of the circle.
        /// </summary>
        public Point Center
        {
            get { return centerPoint; }
        }

        /// <summary>
        /// Gets the radius of the circle.
        /// </summary>
        public double Radius
        {
            get { return radius; }
        }

        /// <summary>
        /// Gets the points in the circle
        /// </summary>
        public Point[] Points
        {
            get { return points; }
        }
        #endregion

        #region Interface Functions

        /// <summary>
        /// Draws a circle
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <param name="inkMovedX"></param>
        /// <param name="inkMovedY"></param>
        /// <param name="scale"></param>
        public override void draw(Graphics g, Renderer r, float inkMovedX, float inkMovedY, float scale)
        {
            System.Drawing.Point centerPoint = new System.Drawing.Point(
    (int)((this.centerPoint.X + inkMovedX) * scale), (int)((this.centerPoint.Y + inkMovedY) * scale));


            // Make a point using the radius value so that its value can be converted from ink space to pixel
            System.Drawing.Point temp = new System.Drawing.Point((int)(this.radius * scale), 0);

            r.InkSpaceToPixel(g, ref centerPoint);
            r.InkSpaceToPixel(g, ref temp);

            int radius = 0;
            if (temp.X != 0)
                radius = temp.X;

            Pen p1 = new Pen(Color.Red, 1);

            g.DrawEllipse(p1, centerPoint.X - radius, centerPoint.Y - radius, radius * 2, radius * 2);
        }

        /// <summary>
        /// DOES NOT DO ANYTHING RIGHT NOW
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override double[] intersection(Primitive other)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// DOES NOT DO ANYTHING RIGHT NOW
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool intersects(Primitive other)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }


    /// <summary>
    /// 
    /// </summary>
    public class UndefinedSegment : Primitive
    {
        #region Member Variables
        private Point[] points;
        private System.Drawing.PointF startPoint;
        private System.Drawing.PointF endPoint;
        #endregion


        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        public UndefinedSegment(Point[] points)
			: base()
        {
            this.points = points;
            this.startPoint = points[0].SysDrawPointF;
            this.endPoint = points[points.Length - 1].SysDrawPointF;
        }
        #endregion


        #region Getters & Setters

        /// <summary>
        /// 
        /// </summary>
        public Point[] Points
        {
            get { return this.points; }
        }

        /// <summary>
        /// 
        /// </summary>
        public System.Drawing.PointF StartPoint
        {
            get { return this.startPoint; }
        }

        /// <summary>
        /// 
        /// </summary>
        public System.Drawing.PointF EndPoint
        {
            get { return this.endPoint; }
        }
        #endregion

		#region Interface Functions

		/// <summary>
		/// Draw nothing
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="inkMovedX"></param>
		/// <param name="inkMovedY"></param>
		/// <param name="scale"></param>
		public override void draw(Graphics g, Renderer r, float inkMovedX, float inkMovedY, float scale)
		{
			// Do nothing
		}

		/// <summary>
		/// Test for intersection
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool intersects(Primitive other)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// Locate intersections
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override double[] intersection(Primitive other)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion
	}
}
