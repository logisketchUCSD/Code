using System;
using System.Collections;

namespace Converter
{
	/// <summary>
	/// Summary description for Stroke.
	/// </summary>
	public class Stroke : Shape
	{
		private ArrayList points; 

		// Feature profiles used when splitting a stroke at corners (Normalized)
		private double[] arcLengthProfile = null;
		private double[] tanAngleProfile  = null;
		private double[] speedProfile     = null;
		private double[] curvatureProfile = null;

		// Corners found
		private int[] corners = null;

		private double average(double[] array)
		{
			double avg = 0.0;

			//Sum up all the points
			foreach(double point in array)
				avg += point;

			//Divide by n
			avg /= array.Length;
			
			return avg;
		}
		
		#region CONSTRUCTORS

		/// <summary>
		/// Stroke constructor
		/// </summary>
		/// <param name="id">The unique id for this stroke</param>
		/// <param name="name">The name of this stroke</param>
		/// <param name="time">The time this stroke was created (pen lift)</param>
		/// <param name="type">The type of this stroke</param>
		public Stroke(string id, string name, string time, string type) : base(id, name, time, type)
		{
			points = new ArrayList();
		}

		
		/// <summary>
		/// Stroke constructor
		/// </summary>
		/// <param name="shape">The shape to base the stroke off of</param>
		public Stroke(Shape shape) : base(shape.Id, shape.Name, shape.Time, shape.Type)
		{
			points = new ArrayList();
			addFeatures(shape);
		}

		/// <summary>
		/// Stroke constructor
		/// </summary>
		/// <param name="shape">Base this stroke off of a shape</param>
		/// <param name="id">The unique id for this stroke</param>
		/// <param name="name">The name of this stroke</param>
		/// <param name="time">The time this stroke was created (pen lift)</param>
		/// <param name="type">The type of this stroke</param>
		public Stroke(Shape shape, string id, string name, string time, string type) : base(id, name, time, type)
		{
			points = new ArrayList();
			addFeatures(shape);
		}
		
		#endregion

		#region ADD TO STROKE

		/// <summary>
		/// Get relavent properties of a shape and add it to the stroke
		/// </summary>
		/// <param name="shape">The shape to take properties from</param>
		public void addFeatures(Shape shape)
		{
			//this.aliases = shape.Aliases;
			this.area = shape.Area;
			//this.args = shape.Args;
			this.author = shape.Author;
			this.color = shape.Color;
			this.control1 = shape.Control1;
			this.control2 = shape.Control2;
			this.end = shape.End;
			this.height = shape.Height;
			//this.id = shape.Id;
			this.laysInk = shape.LaysInk;
			this.leftx = shape.LaysInk;
			//this.name = shape.Name;
			this.orientation = shape.Orientation;
			this.p1 = shape.P1;
			this.p2 = shape.P2;
			this.penTip = shape.PenTip;
			this.raster = shape.Raster;
			this.source = shape.Source;
			this.start = shape.Start;
			this.substrokeOf = shape.SubstrokeOf;
			this.text = shape.Text;
			//this.time = shape.Time;
			this.topy = shape.TopY;
			//this.type = shape.Type;
			this.width = shape.Width;
			this.x = shape.X;
			this.y = shape.Y;
		}
		
		
		/// <summary>
		/// Get an array of Points
		/// </summary>
		public ArrayList Points
		{
			get
			{
				return points;
			}
		}
		
		
		/// <summary>
		/// Add an arg of type point.
		/// </summary>
		/// <param name="x">decimal. x coordinate</param>
		/// <param name="y">decimal. y coordinate</param>
		/// <param name="pressure">decimal. pressure for the point (0-255 on Tablet PC)</param>
		/// <param name="time">positive integer. time point was created in milliseconds since 1/1/1970 UTC</param>
		/// <param name="id">UUID. UUID for point</param>
		/// <param name="name">string. name of the point</param>
		public void addPoint(string x, string y, string pressure, string time, string id, string name)
		{
			addPoint(new Point(x, y, pressure, time, id, name));
		}


		/// <summary>
		/// Add an arg of type point.
		/// </summary>
		/// <param name="point">a point in the sketch</param>
		public void addPoint(Point point)
		{
			points.Add(point);
			this.Args.Add(new Shape.Arg("Point", point.Id));
		}


		/// <summary>
		/// Add args of type point.
		/// </summary>
		/// <param name="points">points in the sketch</param>
		public void addPoints(Point[] points)
		{
			foreach(Point p in points)
				addPoint(p);
		}


		/// <summary>
		/// Add args of type point.
		/// </summary>
		/// <param name="points">points in the sketch</param>
		public void addPoints(ArrayList points)
		{
			foreach(Point p in points)
				addPoint(p);
		}

		
		#endregion

		#region GETTERS & SETTERS

		/// <summary>
		/// Returns the arc length profile for the Stroke.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] ArcLengthProfile
		{
			get
			{
				if (arcLengthProfile == null)
				{
					this.arcLengthProfile = this.calcArcLengthProfile();
				}

				return this.arcLengthProfile;
			}
		}
		
		
		/// <summary>
		/// Returns the tangent angle profile for the Stroke.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] TanAngleProfile
		{
			get 
			{
				if (tanAngleProfile == null)
				{
					this.tanAngleProfile = this.calcTanAngleProfile();
				}
				
				return this.tanAngleProfile;
			}
		}


		/// <summary>
		/// Returns the normalized speed profile for the Stroke.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] SpeedProfile
		{
			get 
			{
				if (speedProfile == null)
				{
					this.speedProfile = this.calcNormSpeedProfile();
				}
				
				return this.speedProfile;
			}
		}


		/// <summary>
		/// Returns the curvature profile for the Stroke.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] CurvatureProfile
		{
			get 
			{
				if (curvatureProfile == null)
				{
					this.curvatureProfile = this.calcCurvProfile();
				}
				
				return this.curvatureProfile;
			}
		}

		
		/// <summary>
		/// Returns the corners for the Stroke.
		/// Calculates it once if no corners array currently exists.
		/// </summary>
		public int[] Corners
		{
			get
			{
				if (corners == null)
				{
					this.corners = findCorners();
				}

				return this.corners;
			}
		}

		#endregion

		#region FEATURES

		#region POINT

		/// <summary>
		/// Gets the timestamp of the first point, which is the startTime of the stroke
		/// </summary>
		/// <returns>Timestampe of first point of stroke</returns>
		public long getStartTime()
		{
			return Convert.ToInt64(getStartPoint().Time);
		}

		
		/// <summary>
		/// Gets the timestamp of the last point, which is the endTime of the stroke
		/// </summary>
		/// <returns>Timestamp of last point of stroke</returns>
		public long getEndTime()
		{
			return Convert.ToInt64(getEndPoint().Time);
		}

		
		/// <summary>
		/// Gets the first point of the stroke
		/// NOTE: Code is broke in the Stroke object doesn't have any points in it!!!
		/// </summary>
		/// <returns>First poitn of the stroke</returns>
		public Point getStartPoint()
		{
			return (Point)this.Points[0];
		}

		
		/// <summary>
		/// Gets the last point of the stroke
		/// </summary>
		/// <returns>Last point of the stroke</returns>
		public Point getEndPoint()
		{
			return (Point)this.Points[this.Points.Count - 1];
		}

	
		/// <summary>
		/// Computes the average x distance, y distance, and pressure of this stroke.
		/// </summary>
		/// <returns>The average point.</returns>
		public Point averagePoint()
		{
			//We need longs since we will be summing up a bunch of ints
			long xAvg = 0;
			long yAvg = 0;
			long pAvg = 0;
			
			//Sum everything up
			foreach(Point p in this.points)
			{
				xAvg += Convert.ToInt64(p.X);
				yAvg += Convert.ToInt64(p.Y);
				pAvg += Convert.ToInt64(p.Pressure);
			}

			//Divide by n
			xAvg /= this.points.Count;
			yAvg /= this.points.Count;
			pAvg /= this.points.Count;

			//return the average point
			return new Point(xAvg.ToString(), yAvg.ToString(), pAvg.ToString(), this.time, System.Guid.NewGuid().ToString(), "averagepoint"); 
		}

		
		#endregion

		#region ARC LENGTH

		/// <summary>
		/// Computes the arc length of this stroke.
		/// </summary>
		/// <returns>The arc length</returns>
		public double arcLength()
		{
			//Get the arc length from the first to last point
			//return arcLength(0, this.points.Count - 1);

			return ArcLengthProfile[this.arcLengthProfile.Length - 1];
		}

		
		/// <summary>
		/// Computes the arc length between to indices of a stroke
		/// </summary>
		/// <param name="a">Lower index</param>
		/// <param name="b">Upper index</param>
		/// <returns></returns>
		public double arcLength(int a, int b)
		{
			/*double length = 0.0;
			
			//Start one past a since we are using a i - 1.
			//Go up to i = b
			for(int i = a + 1; i <= b; ++i)
			{
				length += ((Point)this.points[i - 1]).distance((Point)this.points[i]);
			}
			return length;*/

			return (ArcLengthProfile[b] - ArcLengthProfile[a]);
		}


		/// <summary>
		/// Calculates the arc length profile of the stroke.
		/// </summary>
		/// <returns>The arc length profile of the stroke</returns>
		private double[] calcArcLengthProfile()
		{
			double[] profile = new double[this.points.Count];
			
			profile[0] = 0.0;
			for (int i = 1; i < profile.Length; i++)
			{
				profile[i] = profile[i - 1] + ((Point)this.points[i - 1]).distance((Point)this.points[i]);
			}
			
			return profile;
		}

		
		#endregion

		#region SLOPE

		/// <summary>
		/// Computes the slope of this stroke (rise over run)
		/// </summary>
		/// <param name="index">Index to compute the slope of</param>
		/// <returns></returns>
		public double slope(int index)
		{
			//We will estimate using the point to the left, and the point to the right of the index
			int a = index - 1;
			int b = index + 1;
			
			//Make sure we are using valid points
			if(a < 0)
			{
				a = 0;
			}
			if(b > this.points.Count - 1)
			{
				b = this.points.Count - 1;
			}
			
			//Compute the slope using the point to the left and to the right
			return slope(a, b);
		}

		
		/// <summary>
		/// Computes the slope between two indices (rise over run)
		/// </summary>
		/// <param name="a">Index for first point</param>
		/// <param name="b">Index for second point</param>
		/// <returns>The slope between point[a] point[b]</returns>
		public double slope(int a, int b)
		{
			//Here are the representative points
			Point p1 = (Point)this.points[a];
			Point p2 = (Point)this.points[b];

			//Get the run and rise
			int deltaX = Convert.ToInt32(p2.X) - Convert.ToInt32(p1.X);
			//We must use a -Y since the coordinate system is not cartesion
			int deltaY = Convert.ToInt32(p2.Y) - Convert.ToInt32(p1.Y);
			deltaY *= -1;
			
			//We do not want to divide by zero.
			//Set deltaX to the minimum value if it is 0.
			if(deltaX == 0)
				deltaX = 1;

			//Console.WriteLine("p1 x:{0} y:{1}", p1.X, p1.Y);
			//Console.WriteLine("p2 x:{0} y:{1}", p2.X, p2.Y);
			//Console.WriteLine("x:{0} y:{1}", deltaX, deltaY);

			//For some reason, slope returns a negative value from what we would expect.
			//Not sure why, but here I correct by multiplying by -1.0.
			//return ((double)deltaY) / ((double)deltaX);

			double deg = Math.Atan2((double)deltaY, (double)deltaX) * (180 / Math.PI);
			return deg;
		}


		/// <summary>
		/// Computes the slope angle between two indices Arctan(rise over run)
		/// </summary>
		/// <param name="a">Index for first point</param>
		/// <param name="b">Index for second point</param>
		/// <returns>The slope angle between point[a] point[b] in degrees</returns>
		public double slopeAngle(int a, int b)
		{
			//Here are the representative points
			Point p1 = (Point)this.points[a];
			Point p2 = (Point)this.points[b];

			//Get the run and rise
			int deltaX = Convert.ToInt32(p2.X) - Convert.ToInt32(p1.X);
			//We must use a -Y since the coordinate system is not cartesion
			int deltaY = Convert.ToInt32(p2.Y) - Convert.ToInt32(p1.Y);
			deltaY *= -1;
			
			//We do not want to divide by zero.
			//Set deltaX to the minimum value if it is 0.
			if(deltaX == 0)
				deltaX = 1;

			//Console.WriteLine("p1 x:{0} y:{1}", p1.X, p1.Y);
			//Console.WriteLine("p2 x:{0} y:{1}", p2.X, p2.Y);
			//Console.WriteLine("x:{0} y:{1}", deltaX, deltaY);
			
			double deg = Math.Atan2((double)deltaY, (double)deltaX) * (180 / Math.PI);
			return deg;
		}

		
		/// <summary>
		/// Computes the average slope by summing the slopes at every index and dividing by n
		/// </summary>
		/// <returns>The average slope</returns>
		public double averageSlope()
		{
			//Sum up all the point's slope
			double avgSlope = 0.0;
			for(int i = 0; i < this.points.Count; ++i)
			{
				avgSlope += slope(i);
			}

			//Divide by n
			avgSlope /= (double)this.points.Count;

			return avgSlope;
		}


		/// <summary>
		/// Computes the slope at every point
		/// </summary>
		/// <returns>A double array of size points.Count that is indexed with the slope information</returns>
		public double[] slopeProfile()
		{
			double[] profile = new double[this.points.Count];
			for(int i = 0; i < profile.Length; ++i)
			{
				profile[i] = slope(i);
			}
			return profile;
		}

		
		/// <summary>
		/// Computes the angle from the start of the stroke, to the end of the stroke
		/// </summary>
		/// <returns>The angle</returns>
		public double direction()
		{
			return direction(0, this.points.Count - 1);
		}
		

		/// <summary>
		/// Computes the angle of the vector starting at a, ending at b
		/// </summary>
		/// <param name="a">Tail of the vector</param>
		/// <param name="b">Head of the vector</param>
		/// <returns>Angle of the vector</returns>
		public double direction(int a, int b)
		{
			//Here are the representative points
			Point p1 = (Point)this.points[a];
			Point p2 = (Point)this.points[b];

			//It should be as if p1 was at the origin
			int x = Convert.ToInt32(p2.X) - Convert.ToInt32(p1.X);
			//Use a -Y since we are not in cartiesian
			int y = Convert.ToInt32(p1.Y) - Convert.ToInt32(p2.Y);

			/*
			Console.WriteLine("p1 x:{0} y:{1}", p1.X, p1.Y);
			Console.WriteLine("p2 x:{0} y:{1}", p2.X, p2.Y);
			Console.WriteLine("x:{0} y:{1}", x, y);
			*/
			
			//Argument takes y then x
			return Math.Atan2((double)y, (double)x);
		}

		
		/// <summary>
		/// Computes the angle of the tangent line at index
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The tangent angle</returns>
		public double tangentAngle(int index)
		{
			//We will estimate using the point to the left, and the point to the right of the index
			int a = index - 1;
			int b = index + 1;
			
			//Make sure we are using valid points
			if(a < 0)
			{
				a = 0;
			}
			if(b > this.points.Count - 1)
			{
				b = this.points.Count - 1;
			}

			return direction(a, b);
		}

		
		/// <summary>
		/// Computes the average tangent angle by summing the tangent angles at every index and dividing by n
		/// </summary>
		/// <returns>The average tangent angle</returns>
		public double averageTangentAngle()
		{
			//Sum up all the point's slope
			double avgTangentAngle = 0.0;
			for(int i = 0; i < this.points.Count; ++i)
			{
				avgTangentAngle += tangentAngle(i);
			}

			//Divide by n
			avgTangentAngle /= (double)this.points.Count;

			return avgTangentAngle;
		}

		
		/// <summary>
		/// Computes the tangent angle at every point
		/// </summary>
		/// <returns>A double array of size points.Count that is indexed with the tangent angle information</returns>
		public double[] tangentAngleProfile()
		{
			double[] profile = new double[this.points.Count];
			for(int i = 0; i < profile.Length; ++i)
			{
				profile[i] = slope(i);
			}
			return profile;
		}


		/// <summary>
		/// Computes the tangent angle at every point
		/// </summary>
		/// <returns>A double array of size points.Count that is indexed with the tangent angle information</returns>
		private double[] calcTanAngleProfile()
		{
			double[] profile = new double[this.Points.Count];

			for (int i = 0; i < profile.Length; i++)
			{
				profile[i] = findTangentAngle(i);
			}

			return profile;
		}


		/// <summary>
		/// Finds the tangent angle at the current index of the stroke.
		/// </summary>
		/// <param name="index">Index of the stroke</param>
		/// <returns>Tangent angle (in radians)</returns>
		public double findTangentAngle(int index)
		{
			// Use the points between (index - window) and (index + window)
			int window = 5;

			// Threshold percent that the least squares line fit error must be below
			double threshold = 0.50;

			// Tangent Angle to return
			double tanAngle = 0.0;

			// Indices for the points on the stroke
			int startIndex = index - window;
			int endIndex = index + window;

			// Fix the start and end indices so that they are not out of bounds
			if (startIndex < 0)
				startIndex = 0;
			if (endIndex > this.Points.Count - 1)
				endIndex = this.Points.Count - 1;

			System.Drawing.PointF[] pts = new System.Drawing.PointF[endIndex - startIndex + 1];

			for (int i = 0; i < pts.Length; i++)
			{
				Point currPt = (Point)this.Points[i + startIndex];
				pts[i] = new System.Drawing.PointF((float)currPt.X, (float)currPt.Y);
			}

			double m, b;
			double lsqlErr = leastSquaresLineFit(pts, out m, out b);

			// Straight up angle? x = ?
			if (lsqlErr == -1.0)
			{
				tanAngle = 0.0;
			}
			// If the LSQL error is small enough, we calculate the tangent angle from the line fit
			else if (lsqlErr < threshold)
			{
				tanAngle = Math.Atan(m);
			}
			// Otherwise we calculate an arc fit
			else
			{
				double x0, y0, r;
				double lsqcErr = leastSquaresCircleFit(pts, out x0, out y0, out r);

				circleTangent(pts[index - startIndex], x0, y0, r, out m, out b);

				tanAngle = Math.Atan(m);
			}

			return tanAngle;
		}


		/// <summary>
		/// Finds the least squares fit parameters for a line of type y = mx + b
		/// </summary>
		/// <param name="points">Points to fit a least squares line to</param>
		/// <param name="m">Slope of the line</param>
		/// <param name="b">Vertical shift of the line</param>
		/// <returns>Error of the line fit</returns>
		private double leastSquaresLineFit(System.Drawing.PointF[] points, out double m, out double b)
		{
			return leastSquaresLineFit(points, 0, points.Length - 1, out m, out b);
		}
		
		
		/// <summary>
		/// Finds the least squares fit parameters for a line of type y = mx + b
		/// </summary>
		/// <param name="points">Points to fit a least squares line to</param>
		/// <param name="startIndex">Start index of the points to use</param>
		/// <param name="endIndex">End index of the points to use</param>
		/// <param name="m">Slope of the line</param>
		/// <param name="b">Vertical shift of the line</param>
		/// <returns>Error of the line fit</returns>
		private double leastSquaresLineFit(System.Drawing.PointF[] points, int startIndex, int endIndex, out double m, out double b)
		{
			int n = endIndex - startIndex + 1;
			
			double sumX = 0.0;
			double sumY = 0.0;
			double sumXX = 0.0;
			double sumYY = 0.0;
			double sumXY = 0.0;
			
			double err     = 0.0;
			double sumErr  = 0.0;
			double sumTheo = 0.0;
			
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
					
					sumTheo += Math.Abs(y);
					sumErr += Math.Abs(y - points[i].Y);
				}
				
				//err = Math.Sqrt(sumErr / (n - 1));
				err = (sumErr / sumTheo) * 100;
			}
			else
			{
				m = 0.0;
				b = 0.0;
				err = -1.0;
			}
			
			// Returns error
			return err;
		}


		/// <summary>
		/// Finds the LSQ Circle fit for a series of points and returns the uncertainty.
		/// Uses out variables x0, y0, and r to represent a circle calculation.
		/// Circle: r = Math.Sqrt((x - x0)^2 + (y - y0)^2)  
		/// </summary>
		/// <param name="points">Points to use for the LSQ fit</param>
		/// <param name="x0">The circle's center point x-coordinate</param>
		/// <param name="y0">The circle's center point y-coordinate</param>
		/// <param name="r">The circle's radius</param>
		/// <returns>Error of the fit</returns>
		private double leastSquaresCircleFit(System.Drawing.PointF[] points, out double x0, out double y0, out double r)
		{
			return leastSquaresCircleFit(points, 0, points.Length - 1, out x0, out y0, out r);
		}
		
		
		/// <summary>
		/// Finds the LSQ Circle fit for a series of points and returns the uncertainty.
		/// Uses out variables x0, y0, and r to represent a circle calculation.
		/// Circle: r = Math.Sqrt((x - x0)^2 + (y - y0)^2)  
		/// </summary>
		/// <param name="points">Points to use for the LSQ fit</param>
		/// <param name="startIndex">Start index of the points to use</param>
		/// <param name="endIndex">End index of the points to use</param>
		/// <param name="x0">The circle's center point x-coordinate</param>
		/// <param name="y0">The circle's center point y-coordinate</param>
		/// <param name="r">The circle's radius</param>
		/// <returns>Error of the fit</returns>
		private double leastSquaresCircleFit(System.Drawing.PointF[] points, int startIndex, int endIndex, 
			out double x0, out double y0, out double r)
		{
			/**
			 * Calculation will be of the form:
			 * 
			 * [A B C]^T = M^-1 * V
			 * 
			 * [A]   [ sum (xi^2 + yi^2)^2    sum (xi^2 + yi^2)*xi   sum (xi^2 + yi^2)*yi ]^-1  [ sum (xi^2 + yi^2) ]
			 * [B] = [ sum (xi^2 + yi^2)*xi   sum (xi^2)             sum (xi * yi)        ]  *  [ sum (xi)          ]
			 * [C]   [ sum (xi^2 + yi^2)*yi   sum (xi * yi)          sum (yi^2)           ]     [ sum (yi)          ]
			 * 
			 * http://www.orbitals.com/self/least/least.htm
			 */ 
			 
			MathNet.Numerics.LinearAlgebra.Matrix ABC = new MathNet.Numerics.LinearAlgebra.Matrix(3, 1, 0.0);
			MathNet.Numerics.LinearAlgebra.Matrix M = new MathNet.Numerics.LinearAlgebra.Matrix(3, 3, 0.0);
			MathNet.Numerics.LinearAlgebra.Matrix V = new MathNet.Numerics.LinearAlgebra.Matrix(3, 1, 0.0);
			
			for (int i = startIndex; i <= endIndex; i++)
			{
				double x = points[i].X;
				double x2 = x * x;

				double y = points[i].Y;
				double y2 = y * y;

				double addx2y2 = x2 + y2;

				M[0,0] += addx2y2 * addx2y2;
				M[0,1] += addx2y2 * x;
				M[0,2] += addx2y2 * y;
				M[1,0] += addx2y2 * x;
				M[1,1] += x2;
				M[1,2] += x * y;
				M[2,0] += addx2y2 * y;
				M[2,1] += x * y;
				M[2,2] += y2;

				V[0,0] += addx2y2;
				V[1,0] += x;
				V[2,0] += y;
			}
			
			if (M.LUD().IsNonSingular)
				ABC = M.Inverse() * V;
			else
			{
				Console.WriteLine("Matrix M = " + M.ToString() + " has no inverse");
				double m, b;
				m = b = x0 = y0 = r = 0.0;
				return leastSquaresLineFit(points, out m, out b);
			}

			/**
			 * x0 = -B / (2 * A);
			 * y0 = -C / (2 * A);
			 * r  = Math.Sqrt((4 * A) + (B * B) + (C * C)) / (2 * A);
			 */

			x0 = -ABC[1,0] / (2 * ABC[0,0]);
			y0 = -ABC[2,0] / (2 * ABC[0,0]);
			r  = Math.Abs( Math.Sqrt( (4 * ABC[0,0]) + (ABC[1,0] * ABC[1,0]) + (ABC[2,0] * ABC[2,0]) ) / (2 * ABC[0,0]) );

			// Calculate the error of the fit
			double sumTheo = 0.0;
			double sumErr  = 0.0;
			double err     = 0.0;

			for (int i = startIndex; i <= endIndex; i++)
			{
				double y = Math.Sqrt( Math.Abs((r * r) - ((points[i].X - x0) * (points[i].X - x0))) ) + y0;
				
				sumTheo += Math.Abs(y);
				sumErr += Math.Abs(y - points[i].Y);
			}
				
			//err = Math.Sqrt(sumErr / (n - 1));
			err = (sumErr / sumTheo) * 100;

			// Uncertainty/Error (NOT CALCULATED)
			return err;
		}
		

		/// <summary>
		/// Finds the tangent of a point on the circle given
		/// </summary>
		/// <param name="p">Point on the circle</param>
		/// <param name="x0">Center x-coordinate of the circle</param>
		/// <param name="y0">Center y-coordinate of the circle</param>
		/// <param name="r">Radius of the circle</param>
		/// <param name="m">Slope of the tangent line</param>
		/// <param name="b">Vertical shift of the tangent line</param>
		public void circleTangent(System.Drawing.PointF p, double x0, double y0, double r, out double m, out double b)
		{
			// Slope through the center point
			double mcp = (p.Y - y0) / (p.X - x0);
			
			// Slope of the tangent, since mcp x mtgt = -1;
			double mtgt = -1 / mcp;

			m = mtgt;
			b = (mtgt * -p.X) + p.Y;
		}

		#endregion
		
		#region SPEED

		/// <summary>
		/// Computes the speed at the given index by averaging from one point ahead and one behind.
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The speed at the given index</returns>
		public double speed(int index)
		{
			//We will estimate using the point to the left, and the point to the right of the index
			int a = index - 1;
			int b = index + 1;
			
			//Make sure we are using valid points
			if(a < 0)
			{
				a = 0;
			}
			if(b > this.points.Count - 1)
			{
				b = this.points.Count - 1;
			}
			
			//Here are the representative points
			Point p1 = (Point)this.points[a];
			Point p2 = (Point)this.points[index];
			Point p3 = (Point)this.points[b];

			//Here is the distance
			double distance1 = p1.distance(p2);
			double distance2 = p2.distance(p3);

			//Here is the time 
			long deltaTime = Math.Abs(Convert.ToInt64(p1.Time) - Convert.ToInt64(p3.Time));

			//Make sure we do not divide by a zero time
			if(deltaTime == 0)
				deltaTime = 1;

			//Speed = distance / time
			return (distance1 + distance2) / (double)deltaTime;
		}


		/// <summary>
		/// Computes the average speed by summing up the speed at each index and dividing by n
		/// </summary>
		/// <returns>The average speed of the stroke</returns>
		public double averageSpeed()
		{
			double avgSpeed = 0.0;
		
			//Sum up all the point's speed
			for(int i = 0; i < this.points.Count; ++i)
			{
				avgSpeed += speed(i);
			}

			//Divide by n
			avgSpeed /= (double)this.points.Count;

			return avgSpeed;
		}


		/// <summary>
		/// Computes the speed at each point and creates a double array, where each index contains
		/// that Stroke point's corresponding speed.
		/// </summary>
		/// <returns>The double[] array of speed</returns>
		public double[] calcSpeedProfile()
		{
			double[] profile = new double[this.points.Count];
			
			for(int i = 0; i < profile.Length; ++i)
			{
				profile[i] = speed(i);

			}
			
			return profile;
		}


		/// <summary>
		/// Computes the normalized speed at each point and creates a double array, where each index contains
		/// that Stroke point's corresponding speed.
		/// </summary>
		/// <returns>The double[] array of speed</returns>
		public double[] calcNormSpeedProfile()
		{
			double[] profile = new double[this.points.Count];
			double avgSpeed = 0.0;

			for (int i = 0; i < profile.Length; ++i)
			{
				profile[i] = speed(i);
				avgSpeed += profile[i];
			}

			avgSpeed /= this.Points.Count;

			for (int i = 0; i < profile.Length; i++)
			{
				profile[i] /= avgSpeed;
			}

			return profile;
		}

		#endregion

		#region CURVATURE

		/*/// <summary>
		/// NOT SURE THIS WORKS
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public double curvature(int index)
		{
			//We will estimate using the point to the left
			int a = index - 1;
			
			//Make sure we are using valid points
			if(a < 0)
			{
				a = 0;
			}

			double thetaA = tangentAngle(a);
			double thetaB = tangentAngle(index);

			Point p1 = (Point)this.points[a];
			Point p2 = (Point)this.points[index];
			
			double distance1 = p1.distance(p2);
			if(distance1 < 0.001)
				distance1 = 0.001;

			double curve = Math.Abs((thetaB - thetaA) / distance1);

			return curve;
		}


		/// <summary>
		/// Computes the curvature at each point and creates a double array of it all
		/// </summary>
		/// <returns>The double array of curvature values</returns>
		public double[] curvatureProfile()
		{
			double[] profile = new double[this.points.Count];
			for(int i = 0; i < profile.Length; ++i)
			{
				profile[i] = curvature(i);
			}
			return profile;
		}*/
	
		
		/// <summary>
		/// Computes the curvature at each point and creates a double array of it all
		/// </summary>
		/// <returns>The double array of curvature values</returns>
		public double[] calcCurvProfile()
		{
			double[] profile = new double[this.points.Count];
			for(int i = 0; i < profile.Length; ++i)
			{
				profile[i] = Math.Abs(curvature(i));
			}

			return profile;
		}

		
		/// <summary>
		/// Computes the curvature at each point and creates a double array of it all
		/// </summary>
		/// <returns>The double array of curvature values</returns>
		public double[] calcNormCurvProfile()
		{
			double[] profile = new double[this.points.Count];
			double avgCurv = 0.0;
			
			for (int i = 0; i < profile.Length; i++)
			{
				double currCurv = curvature(i);
				avgCurv += Math.Abs(currCurv);
				profile[i] = Math.Abs(currCurv);
			}

			avgCurv /= profile.Length;
			
			for (int i = 0; i < profile.Length; i++)
			{
				profile[i] /= avgCurv;
			}

			return profile;
		}


		/// <summary>
		/// Calculates the curvature of a point at a given index. Curvature is equal to
		/// the LSQ Line slope of the tangent angle vs. arc length
		/// </summary>
		/// <param name="index">The index of the point to calculate curvature for</param>
		/// <returns>The curvature of the point</returns>
		public double curvature(int index)
		{
			// Use the points between (index - window) and (index + window)
			int window = 5;
			
			// Indices for the points on the stroke
			int startIndex = index - window;
			int endIndex = index + window;
			
			// Fix the start and end indices so that they are not out of bounds
			if (startIndex < 0)
				startIndex = 0;
			if (endIndex > this.points.Count - 1)
				endIndex = this.points.Count - 1;

			System.Drawing.PointF[] pts = new System.Drawing.PointF[endIndex - startIndex + 1];

			for (int i = 0; i < pts.Length; i++)
			{
				pts[i] = new System.Drawing.PointF((float)ArcLengthProfile[startIndex + i], 
					(float)TanAngleProfile[startIndex + i]);
			}

			double m, b;
			double err = leastSquaresLineFit(pts, out m, out b);

			// Convert from radians / pixel to degrees / pixel
			m = m * (180 / Math.PI);

			// Slope of the tangent line
			return m;
		}


		/// <summary>
		/// Compute the average curvature of a stroke
		/// </summary>
		/// <returns>The average curvature</returns>
		public double averageCurvature()
		{
			return average(this.curvatureProfile);
		}

		
		#endregion

		#region CORNER FINDING

		public int[] findCorners() 
		{
			int[] initCorners = findInitCorners(0.25, 0.20);
			int[] trimmedCorners = trimCloseCorners(initCorners, 50);
			
			int[] quietedCorners = trimNoisyCorners(trimmedCorners, 0.80, 0.05);
			//int[] mergedArcCorners = mergeByArc(quietedCorners);
			
			int[] mergedArcCorners = mergeByArc(trimmedCorners);
			int[] mergedLengthCorners = mergeByLength(mergedArcCorners);
			
			//int[] mergedLengthCorners = mergeByLength(trimmedCorners);
			//int[] mergedArcCorners = mergeByArc(mergedLengthCorners);
			
			//return initCorners;
			//return trimmedCorners;
			//return quietedCorners;
			//return mergedArcCorners;
			return mergedLengthCorners;
		}

		
		/// <summary>
		/// Find the initial corner estimations of a stroke, indicating likely places where a stroke can be split up into
		/// corresponding substrokes.
		/// </summary>
		/// <param name="speedThreshold">Percent of the average speed of the stroke</param>
		/// <param name="curveThreshold">Degrees per pixel</param>
		/// <returns>The indices where the stroke should be split up</returns>
		private int[] findInitCorners(double speedThreshold, double curveThreshold)
		{
			ArrayList initCorners = new ArrayList();
			
			// Add point indices that correspond to low speed
			for (int i = 0; i < this.SpeedProfile.Length; i++)
			{
				if (this.SpeedProfile[i] < speedThreshold) 
				{
					int loopExit;
					int minimaIndex = findLocalMinima(this.SpeedProfile, speedThreshold, i, out loopExit);

					initCorners.Add(minimaIndex);

					i = loopExit;
				}
			}

			// Add point indices that correspond to high curvature
			for (int i = 0; i < CurvatureProfile.Length; i++)
			{
				if (this.CurvatureProfile[i] > curveThreshold) 
				{
					int loopExit;
					int maximaIndex = findLocalMaxima(this.CurvatureProfile, curveThreshold, i, out loopExit);

					initCorners.Add(maximaIndex);

					i = loopExit;
				}
			}

			// Sort the initial corner estimations
			initCorners.Sort();
			
			return (int[])initCorners.ToArray(typeof(int));
		}


		/// <summary>
		/// Removes corners from an array that are too close together. Also removes points that have
		/// slow speed but very low curvature.
		/// </summary>
		/// <param name="corners">The corners in a stroke</param>
		/// <param name="threshold">Threshold between indices (minimum distance between 2)</param>
		/// <returns>The trimmed corners array</returns>
		private int[] trimCloseCorners(int[] corners, int threshold)
		{
			if (corners == null || corners.Length == 0)
				return corners;
			
			ArrayList trimmedCorners = new ArrayList();
			ArrayList currCluster = new ArrayList();

			// Goes through all the corner points and eliminates some points that are close together.
			// Also tries to eliminate "hooks" on strokes by trimming off any corners found too close to the
			// endpoints.
			for (int i = 0; i < corners.Length - 1; i++)
			{
				double dist = this.distance(corners[i], corners[i + 1]);
				
				if (currCluster.Count == 0)
				{
					currCluster.Add(corners[i]);
				}
				
				if (dist < threshold)
				{
					currCluster.Add(corners[i + 1]);
				}
				else
				{
					// Get the middle index of the current cluster group
					int goodCorner = (int)currCluster[ (int)(currCluster.Count / 2.0) ];

					// If the split point is not located too close to the endpoints of the stroke
					if (goodCorner > 15 && goodCorner < this.points.Count - 15)
						trimmedCorners.Add(goodCorner);
					
					currCluster.Clear();
				}
			}

			if (currCluster.Count > 0)
			{
				int goodCorner = (int)currCluster[ (int)((currCluster.Count - 1) / 2.0) ];
				trimmedCorners.Add(goodCorner);
			}		

			return (int[])trimmedCorners.ToArray(typeof(int));
		}


		/// <summary>
		/// Removes corners where from the inputted corners array where user specified speed and curvature
		/// thresholds are not met. In other words, we use this to combine our previous thresholds and see if certain
		/// corners fail to be included into both categories.
		/// 
		/// The basic idea is that you can relax thresholds here and see if some points fail miserably on
		/// a certain condition.
		/// </summary>
		/// <param name="corners">The corners in a stroke</param>
		/// <param name="speedThreshold">Speed the corner must be below</param>
		/// <param name="curvThreshold">Curvature the corner must be below</param>
		/// <returns>The trimmed corners array</returns>
		private int[] trimNoisyCorners(int[] corners, double speedThreshold, double curvThreshold)
		{
			if (corners == null || corners.Length == 0)
				return corners;
			
			ArrayList quietedCorners = new ArrayList();

			for (int i = 0; i < corners.Length; i++)
			{
				if (SpeedProfile[corners[i]] < speedThreshold && CurvatureProfile[corners[i]] > curvThreshold)
					quietedCorners.Add(corners[i]);
			}

			return (int[])quietedCorners.ToArray(typeof(int));
		}


		private void getDirection(int startIndex, int endIndex, out int xDir, out int yDir, out string largestShift)
		{
			int xVal = (int)( ((Point)this.Points[endIndex]).X - ((Point)this.Points[startIndex]).X );
			int yVal = (int)( ((Point)this.Points[endIndex]).Y - ((Point)this.Points[startIndex]).Y );

			int window = 50;

			if (xVal > window)
				xDir = 1;
			else if (xVal < -window)
				xDir = -1;
			else
				xDir = 0;

			if (yVal > window)
				yDir = 1;
			else if (yVal < -window)
				yDir = -1;
			else
				yDir = 0;

			if (xVal > yVal + window)
				largestShift = "X";
			else if (yVal > xVal + window)
				largestShift = "Y";
			else
				largestShift = "INSIG";
		}

		private double distance(int index1, int index2)
		{
			Point p1 = (Point)this.Points[index1];
			Point p2 = (Point)this.Points[index2];

			double x2 = Math.Pow(Convert.ToInt32(p1.X) - Convert.ToInt32(p2.X), 2.0);
			double y2 = Math.Pow(Convert.ToInt32(p1.Y) - Convert.ToInt32(p2.Y), 2.0);
			
			return Math.Sqrt(x2 + y2);
		}

		/// <summary>
		/// Combine substrokes whose combined arc or line segments are within some
		/// error of their individual segments summed.
		/// </summary>
		/// <param name="corners">The corners in a stroke</param>
		/// <returns>The trimmed corners array</returns>
		private int[] mergeByArc(int[] corners)
		{
			if (corners == null || corners.Length == 0)
				return corners;
			
			ArrayList mergedCorners = new ArrayList();
			
			double m, b, x0, y0, r;
			double errLine1, errLine2, errCirc1, errCirc2;
			double totalLineErr, totalCircErr;
			double slope1, slope2;

			// Initialize the point array we will be using
			System.Drawing.PointF[] pts = new System.Drawing.PointF[this.Points.Count];
			for (int i = 0; i < pts.Length; i++)
			{
				pts[i] = new System.Drawing.PointF((float)((Point)this.Points[i]).X, (float)((Point)this.Points[i]).Y);
			}
			
			// Make sure that we can have a valid window on our endpoints
			if (pts.Length > 30)
				mergedCorners.Add(10);
			else
				mergedCorners.Add(0);

			// Test the beginning stroke fragment up to the first break corner
			int xDir1, xDir2, yDir1, yDir2;
			string largestShift1, largestShift2;
			int lastCorner;
			
			// Merge the middle strokes
			for (int i = 1; i < corners.Length; i++)
			{
				lastCorner = (int)mergedCorners[mergedCorners.Count - 1];
				
				slope1 = this.slopeAngle(lastCorner, corners[i-1]);
				slope2 = this.slopeAngle(corners[i-1], corners[i]);

				if (Math.Abs(slope2 - slope1) < 30.0)
				{
					totalLineErr = leastSquaresLineFit(pts, lastCorner, corners[i], out m, out b);
					totalCircErr = leastSquaresCircleFit(pts, lastCorner, corners[i], out x0, out y0, out r);

					// Favors arcs over lines
					if (totalLineErr < (totalCircErr / 1.5))
					{
						errLine1 = leastSquaresLineFit(pts, lastCorner, corners[i - 1], out m, out b);
						errLine2 = leastSquaresLineFit(pts, corners[i - 1], corners[i], out m, out b);
						
						if (totalLineErr > (1.1 * (errLine1 + errLine2)))
							mergedCorners.Add(corners[i - 1]);
					}
					else
					{
						errCirc1 = leastSquaresCircleFit(pts, lastCorner, corners[i - 1], out x0, out y0, out r);
						errCirc2 = leastSquaresCircleFit(pts, corners[i - 1], corners[i], out x0, out y0, out r);
						
						if (totalCircErr > (1.5 * (errCirc1 + errCirc2)))
							mergedCorners.Add(corners[i - 1]);
					}
				}
				else
					mergedCorners.Add(corners[i - 1]);
			}


			lastCorner = (int)mergedCorners[mergedCorners.Count - 1];
			int lastPt;

			// Test the end stroke fragment from the last corner to the end point
			if (pts.Length > 30 && corners[corners.Length - 1] < pts.Length - 10)
				lastPt = pts.Length - 10;
			else
				lastPt = pts.Length - 1;

			slope1 = this.slopeAngle(lastCorner, corners[corners.Length - 1]);
			slope2 = this.slopeAngle(corners[corners.Length - 1], lastPt);
			
			if (Math.Abs(slope2 - slope1) < 30.0)
			{
				totalLineErr = leastSquaresLineFit(pts, lastCorner, lastPt, out m, out b);
				totalCircErr = leastSquaresCircleFit(pts, lastCorner, lastPt, out x0, out y0, out r);
					
				if (totalLineErr < totalCircErr)
				{
					errLine1 = leastSquaresLineFit(pts, lastCorner, corners[corners.Length - 1], out m, out b);
					errLine2 = leastSquaresLineFit(pts, lastCorner, corners[corners.Length - 1], out m, out b);
					
					if (totalLineErr > (1.1 * (errLine1 + errLine2)))
						mergedCorners.Add(corners[corners.Length - 1]);
				}
				else
				{
					errCirc1 = leastSquaresCircleFit(pts, lastCorner, corners[corners.Length - 1], out x0, out y0, out r);
					errCirc2 = leastSquaresCircleFit(pts, lastCorner, corners[corners.Length - 1], out x0, out y0, out r);
					
					if (totalCircErr > (1.1 * (errCirc1 + errCirc2)))
						mergedCorners.Add(corners[corners.Length - 1]);
				}
			}
			else
				mergedCorners.Add(corners[corners.Length - 1]);


			mergedCorners.Remove(0);
			mergedCorners.Remove(10);

			return (int[])mergedCorners.ToArray(typeof(int));
		} 






		/// <summary>
		/// Combines substrokes that are adjacent where one is less than 20% of the length of the
		/// other.
		/// </summary>
		/// <param name="corners">The corners in a stroke</param>
		/// <returns>The trimmed corners array</returns>
		private int[] mergeByLength(int[] corners)
		{
			if (corners == null || corners.Length == 0)
				return corners;
			
			ArrayList mergedCorners = new ArrayList();
			double threshold = 0.20;

			int lastCorner, lastPt;
			double slope1, slope2;
			mergedCorners.Add(0);
			
			for (int i = 1; i < corners.Length; i++)
			{
				lastCorner = (int)mergedCorners[mergedCorners.Count - 1];

				slope1 = this.slopeAngle(lastCorner, corners[i-1]);
				slope2 = this.slopeAngle(corners[i-1], corners[i]);

				if (Math.Abs(slope2 - slope1) < 30.0)
				{
					double length1 = arcLength(lastCorner, corners[i - 1]);
					double length2 = arcLength(corners[i - 1], corners[i]);

					if ( length1 / length2 > threshold && length2 / length1 > threshold )
					{
						mergedCorners.Add(corners[i - 1]);
					}
				}
				else
					mergedCorners.Add(corners[i - 1]);
			}

			lastCorner = (int)mergedCorners[mergedCorners.Count - 1];
			
			// Test the end stroke fragment from the last corner to the end point
			slope1 = this.slopeAngle(lastCorner, corners[corners.Length - 1]);
			slope2 = this.slopeAngle(corners[corners.Length - 1], this.Points.Count - 1);
			
			if (Math.Abs(slope2 - slope1) < 30.0)
			{
				if ( arcLength(lastCorner, corners[corners.Length - 1]) /
						arcLength(corners[corners.Length - 1], this.Points.Count - 1) > threshold 
					&& arcLength(corners[corners.Length - 1], this.Points.Count - 1) / 
						arcLength(lastCorner, corners[corners.Length - 1]) > threshold ) 
				{
					mergedCorners.Add(corners[corners.Length - 1]);
				}
			}
			else
				mergedCorners.Add(corners[corners.Length - 1]);

			mergedCorners.Remove(0);

			return (int[])mergedCorners.ToArray(typeof(int));
		}

		
		/// <summary>
		/// Finds a local minima within an array of doubles.
		/// </summary>
		/// <param name="values">Values to look for a local minima</param>
		/// <param name="threshold">The threshold line that the values must be less than before we assume a minima has been found</param>
		/// <param name="startIndex">Where to start looking for the minima in the values array</param>
		/// <param name="loopExit">Where we stopped looking</param>
		/// <returns>The local minima's index within values</returns>
		private int findLocalMinima(double[] values, double threshold, int startIndex, out int loopExit)
		{
			int i = startIndex;
			int minimaIndex = i;
			double minima = values[i];
			
			// Keep checking values until the points start increasing over the threshold again
			while (i < values.Length && values[i] <= threshold)
			{
				if (values[i] < minima)
				{
					minima = values[i];
					minimaIndex = i;
				}

				i++;
			}

			loopExit = i;

			return minimaIndex;
		}


		/// <summary>
		/// Finds local maxima within an array of doubles
		/// </summary>
		/// <param name="values">Values to look for a local maxima</param>
		/// <param name="threshold">The threshold line that the values must be greater than before we assume a maxima has been found</param>
		/// <param name="startIndex">Where to start looking for the maxima in the values array</param>
		/// <param name="loopExit">Where we stopped looking</param>
		/// <returns>The local maxima's index within values</returns>
		private int findLocalMaxima(double[] values, double threshold, int startIndex, out int loopExit)
		{
			int i = startIndex;
			int maximaIndex = i;
			double maxima = values[i];
			
			// Keep checking values until the points start decreasing under the threshold again
			while (i < values.Length && values[i] >= threshold)
			{
				if (values[i] > maxima)
				{
					maxima = values[i];
					maximaIndex = i;
				}

				i++;
			}

			loopExit = i;

			return maximaIndex;
		}

		#endregion

		#endregion
	}

}
