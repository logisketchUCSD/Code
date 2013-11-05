/*
 * File: Point.cs
 *
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using MathNet.Numerics.LinearAlgebra;

namespace Sketch
{
	/// <summary>
	/// Represents an immutable point.
	/// </summary>
	[Serializable]
	public class Point : IComparable<Point>
	{
        /// <summary>
        /// Compares points for equality based on X,Y position.
        /// If not equal, returns difference in time, just like the default CompareTo
        /// </summary>
		public class PointXYComparer : IComparer<Point>
		{
			int IComparer<Point>.Compare(Point a, Point b)
			{
				if (a.X == b.X && a.Y == b.Y) return 0;
				return (int)(a.Time - b.Time);
			}
		}

		#region INTERNALS

		/// <summary>
		/// The XML attributes of the Point
		/// </summary>
		private XmlStructs.XmlPointAttrs _xmlAttributes;
		
		#endregion

		#region CONSTRUCTORS
		
		/// <summary>
		/// Constructor
		/// </summary>
		public Point()
			: this(new XmlStructs.XmlPointAttrs())
		{
			//Calls the main constructor
		}

		/// <summary>
		/// Construct a new Point class from an existing Point.
		/// </summary>
		/// <param name="point">Existing Point to copy</param>
		public Point(Point point)
			: this(point._xmlAttributes.Clone())
		{
			// Calls the main constructor
		}
		
		
		/// <summary>
		/// Construct a new Point class from XML attributes.
		/// </summary>
		/// <param name="XmlAttrs">The XML attributes of the Point</param>
		public Point(XmlStructs.XmlPointAttrs XmlAttrs)
		{
			this._xmlAttributes = XmlAttrs;
		}

        /// <summary>
        /// Construct a new point with the given attributes
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="pressure"></param>
        /// <param name="time"></param>
        /// <param name="name"></param>
        public Point(float x, float y, float? pressure = null, ulong? time = null, string name = null)
        {
            this._xmlAttributes = new XmlStructs.XmlPointAttrs(x, y, pressure, time, name);
        }

        /// <summary>
        /// Construct a new Point from (x,y) with given pressure
        /// </summary>
        /// <param name="x">X value of point</param>
        /// <param name="y">Y value of point</param>
        /// <param name="pressure">pressure of point in (0,1)</param>
        public Point(float x, float y, float pressure)
            : this(x, y, (ushort)(pressure*ushort.MaxValue), null, null)
        {
        }
		
		#endregion

		#region GETTERS & SETTERS

        /// <summary>
        /// Get the XML attributes associated with this point.
        /// </summary>
        public XmlStructs.XmlPointAttrs XmlAttrs
        {
            get { return _xmlAttributes; }
        }

		/// <summary>
		/// A getter for the x-coordinate of the Point.
		/// This is created so we can bypass having to go into the XML Attributes,
		/// because (x,y) coordinates are more closely tied to actual sketch and stroke
		/// attributes than the XML format.
		/// </summary>
		public float X
		{
			get
			{
                return this._xmlAttributes.X; //(float)this.XmlAttrs.X;
			}
		}

		
		/// <summary>
		/// A getter for the y-coordinate of the Point.
		/// This is created so we can bypass having to go into the XML Attributes,
		/// because (x,y) coordinates are more closely tied to actual sketch and stroke
		/// attributes than the XML format.
		/// </summary>
		public float Y
		{
			get
			{
                return this._xmlAttributes.Y;// (float)this.XmlAttrs.Y;
			}
		}


		/// <summary>
		/// Get the ulong Time of this Point.
		/// </summary>
		public ulong Time
		{
			get
			{
                return this._xmlAttributes.Time.Value;// (ulong)this.XmlAttrs.Time;
			}
		}


		/// <summary>
		/// Get the ushort Pressure of this Point
		/// </summary>
		public float Pressure
		{
			get
			{
                return this._xmlAttributes.Pressure.Value;// (ushort)this.XmlAttrs.Pressure;
			}
		}

        /// <summary>
        /// Get the ID of this Point
        /// </summary>
        public Guid Id
        {
            get { return _xmlAttributes.Id; }
        }

        /// <summary>
        /// Get or set the name of this Point
        /// </summary>
        public string Name
        {
            get { return _xmlAttributes.Name; }
        }

		/// <summary>
		/// Converts to a System.Drawing.Point using X and Y values
		/// </summary>
		public System.Drawing.Point SysDrawPoint
        {
            get { return new System.Drawing.Point((int)this.X, (int)this.Y); }
        }

        /// <summary>
        /// Converts to a System.Drawing.PointF using X and Y values
        /// </summary>
        public System.Drawing.PointF SysDrawPointF
        {
            get { return new System.Drawing.PointF(this.X, this.Y); }
        }

		#endregion

		#region OTHER

		/// <summary>
		/// Clone this Point.
		/// </summary>
		/// <returns>A Cloned point.</returns>
		public Point Clone()
		{
			return new Point(this);
		}


		/// <summary>
		/// Compare this Point to another based on time.
		/// Returns less than 0 if this time is less than the other's.
		/// Returns 0 if this time is equal to the other's.
		/// Returns greater than 0 if this time is greater than the other's.
		/// </summary>
		/// <param name="point">The other Point to compare this one to</param>
		/// <returns>An integer indicating how the Point times compare</returns>
        int System.IComparable<Point>.CompareTo(Point point)
        {
            return (int)(this.Time - point.Time);
        }

		#endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string  ToString()
        {
            return "(" + this.X + ", " + this.Y + ")";
        }

		/// <summary>
		/// Calculates the Euclidean distance between two points
		/// </summary>
		/// <param name="other">The point to find the distance to</param>
		/// <returns></returns>
		public double distance(Point other)
		{
			float x1 = X;
			float y1 = Y;
			float x2 = other.X;
			float y2 = other.Y;

			return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
		}

        /// <summary>
        /// This method overrides the System.Equals method, and compares the two points based on the time
        /// rather than whether or not they point to the same object.
        /// </summary>
        /// <param name="obj">Another Point object</param>
        /// <returns>A boolean value whether or not the two Points are the same</returns>
        public override bool Equals(object obj)
        {
            if (obj == null) 
                return false;

            return Equals((Point)obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Equals(Point point)
        {
            return
                _xmlAttributes.X == point._xmlAttributes.X &&
                _xmlAttributes.Y == point._xmlAttributes.Y &&
                _xmlAttributes.Time == point._xmlAttributes.Time && //comment out this line if trying to get rid of duplicate points
                _xmlAttributes.Pressure == point._xmlAttributes.Pressure;
        }
        
        /// <summary>
        /// This method was required to override the .Equals method
        /// </summary>
        /// <returns>The hash code of the base (?)</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

		/// <summary>
		/// Get a point which is the result of rotating this point around a center
		/// </summary>
		/// <param name="ntheta">The angle to rotate by, in radians</param>
		/// <param name="xCenter">The x-coordinate of the center point</param>
		/// <param name="yCenter">The y-coordinate of the center point</param>
        public Point rotate(double ntheta, double xCenter, double yCenter)
        {
            /*Matrix multiplication for rotation about a center (xc, yc)
             *    
             * | X'| = |cos(theta) -sin(theta)| |x - xc| + |xc|
             * | Y'|   |sin(theta)  cos(theta)| |y - yc|   |yc|
             *   
             */
			double r = Math.Sqrt(Math.Pow(X - xCenter, 2) + Math.Pow(Y - yCenter, 2));
			double theta = Math.Atan2(Y - yCenter, X - xCenter);

            float x = (float)(r * Math.Cos(theta + ntheta) + xCenter);
            float y = (float)(r * Math.Sin(theta + ntheta) + yCenter);

            return new Point(x, y, Pressure, Time, Name);
        }

		/// <summary>
		/// Scale this point away from (or torwards) a center
		/// </summary>
		/// <param name="scaleFactor">The multiplicative scaling factor</param>
		/// <param name="xCenter">The X coordinate</param>
		/// <param name="yCenter">The Y coordinate</param>
		public Point scale(double scaleFactor, double xCenter, double yCenter)
		{
			// Woo polar coordinates
			double r = scaleFactor * Math.Sqrt(Math.Pow(X - xCenter, 2) + Math.Pow(Y - yCenter, 2));
			double theta = Math.Atan2(Y - yCenter, X - xCenter);
			float x = (float)(r * Math.Cos(theta) + xCenter);
			float y = (float)(r * Math.Sin(theta) + yCenter);

            return new Point(x, y, Pressure, Time, null);
		}

		/// <summary>
		/// Scale this point away from (or torwards) a center along the x-axis
		/// </summary>
		/// <param name="scaleFactor">The multiplicative scaling factor</param>
		/// <param name="xCenter">The X coordinate</param>
		/// <param name="yCenter">The Y coordinate</param>
		public Point stretchX(double scaleFactor, float xCenter, float yCenter)
		{
			float x = xCenter + (float)(scaleFactor * (_xmlAttributes.X - xCenter));
            return new Point(x, Y, Pressure, Time, null);
		}

        /// <summary>
        /// Apply an affine transform to this point.
        /// </summary>
        /// <param name="affine">the transform</param>
        public Point transform(Matrix affine)
        {
            double x = X;
            double y = Y;

            double newx = affine[0,0] * x + affine[0,1] * y + affine[0,2];
            double newy = affine[1,0] * x + affine[1,1] * y + affine[1,2];

            return new Point((float)newx, (float)newy, Pressure, Time, null);
        }
	}
}
