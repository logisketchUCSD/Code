/*
 * File: EndPoint.cs
 *
 * Authors: Matthew Weiner and Sam Gordon
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Sketch
{
    /// <summary>
    /// The EndPoint Class is used to differentiate endpoints from regular Points
    /// </summary>
    [Serializable]
    public class EndPoint : Point
    {
        #region Internals

        /// <summary>
        /// Slope threshold for saying a wire is horizontal.  If it is between -0.3 and 0.3, it will be horizontal.
        /// </summary>
        private const double MINIMUM_SLOPE_THRESHOLD = 0.3;

        /// <summary>
        /// Slope threshold for saying a wire is vertical.  If it is below -2 or above 2, it will be vertical.
        /// </summary>
        private const double MAXIMUM_SLOPE_THRESHOLD = 2.0;

        /// <summary>
        /// The fraction of total points to take when determining the slope of the endpoint.
        /// </summary>
        private const int FRACTION_OF_POINTS = 3;
        
        /// <summary>
        /// The offset of the line fit to the region around the EndPoint.
        /// </summary>
        private double b;

        /// <summary>
        /// The slope of the line fit to the region around the EndPoint.
        /// </summary>
        private double m;

        /// <summary>
        /// The substroke that the EndPoint belongs to.
        /// </summary>
        private Substroke parentSub;

        /// <summary>
        /// The shape that the EndPoint connects to
        /// </summary>
        private Shape connectedShape;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Creates a new EndPoint from XML Attributes.
        /// </summary>
        /// <param name="XmlAttrs">The attributes of the EndPoint</param>
        public EndPoint(XmlStructs.XmlPointAttrs XmlAttrs)
            :base(XmlAttrs)
        {
        }

        /// <summary>
        /// Creates a new EndPoint from an existing Point.
        /// </summary>
        /// <param name="point">The original Point</param>
        public EndPoint(Point point)
            :this(point, null)
        {
            // calls the main constructor
        }

        /// <summary>
        /// Creates an endpoint from a point and a parent substroke.
        /// </summary>
        /// <param name="point">The original Point</param>
        /// <param name="sub">The parent Substroke</param>
        public EndPoint(Point point, Substroke sub)
            :base(point)
        {
            this.parentSub = sub;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Copies the type of the SubSlope from one EndPoint to another
        /// </summary>
        /// <param name="copyfrom">The EndPoint to be copied</param>
        /// <param name="copyto">The EndPoint to copy to</param>
        internal static void CopySubSlopeType(EndPoint copyfrom, EndPoint copyto)
        {
            copyto.m = copyfrom.Slope;
            copyto.parentSub = copyfrom.parentSub;
        }

        /// <summary>
        /// Determines the slope of a line fit to the points around the EndPoint
        /// </summary>
        public void DetermineSlope()
        {
            List<Point> substrokePoints = ParentSub.PointsL;
            substrokePoints.Sort();

            int index = substrokePoints.IndexOf(this);

            List<Point> line;
            if (index < substrokePoints.Count / 2)
            {
                line = substrokePoints.GetRange(0, substrokePoints.Count / FRACTION_OF_POINTS);
            }
            else
            {
                line = substrokePoints.GetRange(substrokePoints.Count - substrokePoints.Count / FRACTION_OF_POINTS, substrokePoints.Count / FRACTION_OF_POINTS);
            }

            // Find the least squares fit to the points near the endpoint
            leastSquares(line);
        }

        /// <summary>
        /// Returns true of this endpoint is within distance x of the supplied shape.  If the endpoint is in the 
        /// supplied shape, it will only return true if there is a point not in the endpoint's own substroke that is within X of the endpoint.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="shape"></param>
        /// <returns></returns>
        public bool WithinXOf(double x, Shape shape)
        {
            foreach (Substroke stroke in shape.Substrokes)
                if (stroke != parentSub)
                    foreach (Point point in stroke.Points) 
                    {
                        double ptDist = Math.Sqrt(Math.Pow(point.X - X, 2) +
                                                  Math.Pow(point.Y - Y, 2));
                        if (ptDist <= x)
                        {
                            return true;
                        }
                    }
            return false;
        }

        /// <summary>
        /// Initiates finding the total least squares regression line for the 
        /// points surrounding the EndPoint
        /// </summary>
        private void leastSquares(List<Point> line)
        {
            System.Drawing.PointF[] pointf = new System.Drawing.PointF[line.Count];
            for (int i = 0; i < line.Count; i++ )
            {
                
                Point l = (Point)line[i];
                pointf[i] = l.SysDrawPointF;
            }

            double error = leastSquaresLineFit(pointf, out this.m, out this.b);
            this.m = -this.m;
        }

        /// <summary>
        /// Fits a line to a set of points.
        /// </summary>
        /// <param name="points">The list of points to fit</param>
        /// <param name="m">Out: The slope of the line</param>
        /// <param name="b">Out: The vertical intersection of the line</param>
        /// <returns></returns>
        protected double leastSquaresLineFit(System.Drawing.PointF[] points, out double m, out double b)
		{
			return leastSquaresLineFit(points, 0, points.Length - 1, out m, out b);
		}
		
		
		/// <summary>
		/// Finds the least squares fit parameters for a line of type y = mx + b.
		/// </summary>
		/// <param name="points">Points to fit a least squares line to</param>
		/// <param name="startIndex">Start index of the points to use</param>
		/// <param name="endIndex">End index of the points to use</param>
		/// <param name="m">Slope of the line</param>
		/// <param name="b">Vertical shift of the line</param>
		/// <returns>Error of the line fit (actual / theoretical)</returns>
		protected double leastSquaresLineFit(System.Drawing.PointF[] points, int startIndex, int endIndex, out double m, out double b)
		{
			int n = endIndex - startIndex + 1;
			
			if (startIndex == endIndex)
			{
				m = Double.PositiveInfinity;
				b = 0.0;
				return 0.0;
			}

			double sumX = 0.0;
			double sumY = 0.0;
			double sumXX = 0.0;
			double sumYY = 0.0;
			double sumXY = 0.0;
			
			double sumDist  = 0.0;
			double errOfFit = 0.0;
			
			// Calculate the sums
			for (int i = startIndex; i <= endIndex; i++)
			{
				double currX = points[i].X;
				double currY = points[i].Y;

				sumX += currX;
				sumXX += (currX * currX);

				sumY += currY;
				sumYY += (currY * currY);
			
				sumXY += (currX * currY);
			}

			// Denominator
			double denom = ((double)n * sumXX) - (sumX * sumX);

			// Make sure we don't have a divide by 0 error
			if (denom != 0.0)
			{
				// Slope
				m = (double)((n * sumXY) - (sumX * sumY)) / denom;
				// Shift
				b = (double)((sumY * sumXX) - (sumX * sumXY)) / denom;

				for (int i = startIndex; i <= endIndex; i++)
				{
					double y = (m * points[i].X) + b;
					
					// a = -m * b, b = -a / m, c = -shift / b
					// Distance to line = |ax0 + by0 + c| / Sqrt(a^2 + b^2)
					// http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
					
					// Let this (temp variable) b = 1.0
					double B = 1.0;
					double A = (-1.0 * m) * B;
					double C = (-1.0 * b) / B;

					double d = Math.Abs((A * points[i].X) + (B * points[i].Y) + C) / Math.Sqrt((A * A) + (B * B));

					sumDist += d;
				}
			}
			else
			{
				m = Double.PositiveInfinity;
				b = 0.0;
				
				double avgX = 0.0;
				for (int i = startIndex; i <= endIndex; i++)
				{
					avgX += points[i].X;
				}

				avgX /= (double)(endIndex - startIndex);
				
				for (int i = startIndex; i <= endIndex; i++)
				{
					sumDist += Math.Abs(points[i].X - avgX) / Math.Sqrt(avgX);
				}
			}
				
			errOfFit = sumDist / (double)(endIndex - startIndex);
			
			// Returns error of fit
			return errOfFit;
        }

        #endregion

        #region Getters and Setters

        /// <summary>
        /// Get the status of this endpoint as an internal endpoint
        /// </summary>
        public bool InternalEndPoint
        {
            get
            {
                if (connectedShape == null)
                    return false;
                bool returnVal = connectedShape == ParentSub.ParentShape;
                return returnVal;
            }

        }

        /// <summary>
        /// The slope of the wire around the EndPoint.
        /// </summary>
        /// <returns>The slope of the wire around the EndPoint.</returns>
        public double Slope
        {
            get { return this.m; }
        }

        /// <summary>
        /// The substroke that the EndPoint belongs to.
        /// </summary>
        public Substroke ParentSub
        {
            get { return this.parentSub; }
            set { this.parentSub = value; }
        }

        /// <summary>
        /// Get the list of parent shapes for this endpoint.
        /// </summary>
        public Shape ParentShape
        {
            get { return parentSub.ParentShape; }
        }

        /// <summary>
        /// The shape that this endpoint is connected to. When a connected shape is assigned, it is
        /// added to the list of connected shapes for every parent shape of this endpoint.
        /// </summary>
        public Shape ConnectedShape
        {
            get { return this.connectedShape; }

            set 
            {
                if (value != null)
                {
                    ParentShape.ConnectedShapes.Add(value);
                    value.ConnectedShapes.Add(ParentShape);
                }
                else if (connectedShape != null)
                {
                    bool shouldDisconnect = true;
                    foreach (EndPoint endpoint in connectedShape.Endpoints)
                    {
                        if (ParentShape == endpoint.ConnectedShape)
                        {
                            shouldDisconnect = false;
                            break;
                        }
                    }
                    if (shouldDisconnect)
                    {
                        foreach (EndPoint endpoint in ParentShape.Endpoints)
                        {
                            if (endpoint.ConnectedShape == connectedShape)
                            {
                                shouldDisconnect = false;
                                break;
                            }
                        }

                        if (shouldDisconnect)
                        {
                            ParentShape.ConnectedShapes.Remove(connectedShape);
                            connectedShape.ConnectedShapes.Remove(ParentShape);
                        }
                    }
                }
                connectedShape = value;
            }
        }

        /// <summary>
        /// True if the endpoint is connected to another shape,
        /// false if it is not.
        /// </summary>
        public bool IsConnected
        {
            get { return this.connectedShape != null; }
        }

        #endregion
    }
}
