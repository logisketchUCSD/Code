/*
 * File: Stroke.cs
 *
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Collections.Generic;

namespace Sketch
{
	/// <summary>
	/// Stroke class.
	/// </summary>
	[Serializable]
	public class Stroke : IComparable
	{

		#region INTERNALS

		/// <summary>
		/// What substrokes compose this Stroke
		/// </summary>
		private List<Substroke> _substrokes;

		/// <summary>
		/// The XML attributes of the Stroke
		/// </summary>
		private XmlStructs.XmlShapeAttrs _xmlAttributes;
		
		#endregion

		#region	CONSTRUCTORS
		
		/// <summary>
		/// Constructor
		/// </summary>
		public Stroke() : 
			this(new Substroke[] {}, XmlStructs.XmlShapeAttrs.CreateNew())
		{
			// Calls the main constructor
		}

        /// <summary>
        /// Construct given a single substroke
        /// </summary>
        /// <param name="substroke"></param>
        public Stroke(Substroke substroke)
            :this(new Substroke[] { substroke }, XmlStructs.XmlShapeAttrs.CreateNew())
        {
            // Calls the main constructor
        }

		/// <summary>
		/// Creates a Stroke from the corresponding Substrokes and XML attributes
		/// </summary>
		/// <param name="substrokes">Substrokes of the Stroke</param>
		/// <param name="attrs">The XML attributes of the stroke</param>
		public Stroke(IEnumerable<Substroke> substrokes, XmlStructs.XmlShapeAttrs attrs)
        {
            _substrokes = new List<Substroke>();
            _xmlAttributes = attrs;
            _xmlAttributes.Type = "stroke";

            AddSubstrokes(substrokes);
		}

        /// <summary>
        /// Create a stroke from an InkStroke. Used when adding the stroke.
        /// </summary>
        /// <param name="stroke"></param>
        /// <param name="dtGuid"></param>
        /// <param name="SAMPLE_RATE"></param>
        public Stroke(System.Windows.Ink.Stroke stroke, Guid dtGuid, float SAMPLE_RATE)
            : this()
        {
            // Get the timestamp for the function using a const Guid
            ulong theTime;
            if (stroke.ContainsPropertyData(dtGuid))
            {
                // MIT file format
                ulong fileTime = (ulong)stroke.GetPropertyData(dtGuid);
                theTime = (fileTime - 116444736000000000) / 10000;
            }
            else
            {
                theTime = ((ulong)DateTime.Now.ToFileTime() - 116444736000000000) / 10000;
            }

            // Set time data for each point and add it to the list of substroke's points
            List<Point> pointsToAdd = new List<Point>();
            System.Windows.Input.StylusPointCollection stylusPoints = stroke.StylusPoints;
            int numPoints = stylusPoints.Count;
            
            for (int i = 0; i < numPoints; i++)
            {
                // We believe this to be the standard sample rate.  The multiplication by 1,000 is to convert from
                // seconds to milliseconds.
                //
                // Our time is in the form of milliseconds since Jan 1, 1970
                //
                // NOTE: The timestamp for the stroke is made WHEN THE PEN IS LIFTED
                System.Windows.Input.StylusPoint styPoint = stylusPoints[i];
                ulong adjustedTime = theTime - (ulong)((1 / SAMPLE_RATE * 1000) * (numPoints - i));
                Point toAdd = new Point((float)styPoint.X, (float)styPoint.Y, (float)styPoint.PressureFactor, Convert.ToUInt64(adjustedTime), "point");
               // HACK: Add back in if debugging: if (!pointsToAdd.Contains(toAdd))
                pointsToAdd.Add(toAdd);
            }

            // Create the new substroke using its drawing attributes
            System.Windows.Ink.DrawingAttributes drawingAttributes = stroke.DrawingAttributes; 
            Substroke substroke = new Substroke(pointsToAdd);
            substroke.Name = "substroke";
            substroke.Color = drawingAttributes.Color.GetHashCode();
            substroke.PenTip = drawingAttributes.StylusTip.ToString();
            substroke.PenWidth = (float)drawingAttributes.Width;
            substroke.PenHeight = (float)drawingAttributes.Height;
            substroke.Source = "InkSketch.canvasStrokeToSketchStroke";
            substroke.Start = pointsToAdd[0].Id;
            substroke.End = pointsToAdd[pointsToAdd.Count - 1].Id;
            this.AddSubstroke(substroke);

            // Update the stroke's attributes
            this._xmlAttributes.Name = "stroke";
            this._xmlAttributes.Time = (ulong)theTime;
            this._xmlAttributes.Type = "stroke";
            this._xmlAttributes.Source = "Converter";
        }

		#endregion
	
		#region ADD TO STROKE

		/// <summary>
		/// Adds the Substroke to the Stroke's Substroke ArrayList.
		/// Calling this repeatedly may be much slower than calling
        /// AddSubstrokes(list).
		/// </summary>
		/// <param name="substroke">Substroke to add</param>
		public void AddSubstroke(Substroke substroke)
		{
            if (substroke == null)
                throw new ArgumentException("Cannot add a null substroke to a stroke!");

            AddSubstrokes(new Substroke[] { substroke });
		}

		
		/// <summary>
		/// Adds the Substrokes to the Stroke.
		/// </summary>
        /// <param name="toAdd">Substrokes to add</param>
		public void AddSubstrokes(IEnumerable<Substroke> toAdd)
		{
            foreach (Substroke sub in toAdd)
            {
                if (sub == null)
                    throw new ArgumentNullException("Cannot add a null substroke to a stroke");
                if (sub.ParentStroke != null)
                    throw new ArgumentException("Cannot add a substroke to this stroke -- it already has a parent stroke!");
                sub.ParentStroke = this;
            }
            _substrokes.AddRange(toAdd);
            UpdateAttributes();
		}

		#endregion

		#region REMOVE FROM STROKE

		/// <summary>
		/// Removes a Substroke from the Stroke.
		/// </summary>
		/// <param name="substroke">Substroke to remove</param>
		/// <returns>True iff the Substroke is removed</returns>
		public bool RemoveSubstroke(Substroke substroke)
		{
			if (this._substrokes.Contains(substroke))
			{
                RemoveSubstrokes(new Substroke[] { substroke });
				return true;
			}
		    
            return false;
		}
		
		/// <summary>
		/// Removes an ArrayList of Substrokes from the Stroke.
        /// 
        /// Precondition: all the given substrokes are non-null
        /// and are actually in this stroke.
		/// </summary>
		/// <param name="toRemove">Substrokes to remove</param>
		public void RemoveSubstrokes(IEnumerable<Substroke> toRemove)
		{
            foreach (Substroke substroke in toRemove)
                _substrokes.Remove(substroke);

            UpdateAttributes();
		}

		#endregion

		#region UPDATE ATTRIBUTES

		/// <summary>
		/// Updates the spatial attributes of the Shape, such as the origin
		/// and width of the shape. Also ensures that the substrokes list is
        /// sorted.
		/// </summary>
		private void UpdateAttributes()
		{
            // sort the substrokes
            _substrokes.Sort();

			float minX = Single.PositiveInfinity;
			float maxX = Single.NegativeInfinity;

			float minY = Single.PositiveInfinity;
			float maxY = Single.NegativeInfinity;

			// Cycle through the Substrokes within the Shape
			foreach (Substroke s in this._substrokes)
			{
				minX = Math.Min(minX, s.XmlAttrs.X.Value);
				minY = Math.Min(minY, s.XmlAttrs.Y.Value);

				maxX = Math.Max(maxX, s.XmlAttrs.X.Value + s.XmlAttrs.Width.Value);
				maxY = Math.Max(maxY, s.XmlAttrs.Y.Value + s.XmlAttrs.Height.Value);
			}

			// Set the origin at the top-left corner of the shape group
			this._xmlAttributes.X = minX;
			this._xmlAttributes.Y = minY;

			// Set the width and height of the shape
			this._xmlAttributes.Width = maxX - minX;
			this._xmlAttributes.Height = maxY - minY;

			// Sort the substrokes to ensure we are still in an ascending time order
			//this.substrokes.Sort();

			// Update the Start, End and Time attributes
			if (this._substrokes.Count > 0)
			{
				this._xmlAttributes.Start = (this._substrokes[0] as Substroke).XmlAttrs.Id;
				this._xmlAttributes.End = (this._substrokes[this._substrokes.Count - 1] as Substroke).XmlAttrs.Id;
				this._xmlAttributes.Time = (this._substrokes[this._substrokes.Count - 1] as Substroke).XmlAttrs.Time;
			}
			else
			{
				this._xmlAttributes.Start = null;
				this._xmlAttributes.End = null;
				this._xmlAttributes.Time = null;
			}
		}

		#endregion

		#region GETTERS & SETTERS

        /// <summary>
        /// Get the XML attributes for this stroke
        /// </summary>
        public XmlStructs.XmlShapeAttrs XmlAttrs
        {
            get { return _xmlAttributes; }
        }

        /// <summary>
        /// Get or set the time for this stroke
        /// </summary>
        public ulong? Time
        {
            get { return _xmlAttributes.Time; }
            set { _xmlAttributes.Time = value; }
        }

        /// <summary>
        /// Get or set the name of this stroke
        /// </summary>
        public string Name
        {
            get { return _xmlAttributes.Name; }
            set { _xmlAttributes.Name = value; }
        }

        /// <summary>
        /// Get or set the source of this stroke,
        /// which describes who created it.
        /// </summary>
        public string Source
        {
            get { return _xmlAttributes.Source; }
            set { _xmlAttributes.Source = value; }
        }

        /// <summary>
        /// Returns the first Substroke.
        /// </summary>
        public Substroke FirstSubstroke
        {
            get
            {
                return Substrokes[0];
            }
        }

		/// <summary>
		/// Returns the sorted Substroke array of the Stroke
		/// </summary>
		public Substroke[] Substrokes
		{
			get
			{
				// Sort the Substrokes in ascending order based on time
				// NOTE: Since we add substrokes in a sorted order, we should not need to sort afterward
				//substrokes.Sort();

				return this._substrokes.ToArray();
			}
		}

        /// <summary>
        /// Returns the list of Substrokes
        /// </summary>
        public List<Substroke> SubstrokesL
        {
            get
            {
                return this._substrokes;
            }
        }


		
		/// <summary>
		/// Get the Points of this Stroke
		/// </summary>
		public Point[] Points
		{
			get
			{
				return PointsL.ToArray();
			}
		}

		/// <summary>
		/// Get the Points of this stroke (in a list
		/// </summary>
		public List<Point> PointsL
		{
			get
			{
				List<Point> points = new List<Point>();
				
				int substrokeLength = this._substrokes.Count;
				int pointLength;
				int i;
				int j;
				Point[] ps;

				// Loop through all the substrokes
				for (i = 0; i < substrokeLength; ++i)
				{
					// Get the points
					ps = this._substrokes[i].Points;
					pointLength = ps.Length;

					// Add all the points
					for (j = 0; j < pointLength; ++j)
						points.Add(ps[j]);
				}
				
				// Sort the Points in ascending order based on time
				points.Sort();
				return points;
			}
		}

			
		
		/// <summary>
		/// Gets the points where this stroke is fragmented into substrokes.
		/// Returns an empty list if the stroke has 1 or fewer substrokes.
		/// </summary>
		public List<Point> FragmentationPoints
		{
			get
			{
				List<Point> fps = new List<Point>();
				if(_substrokes.Count > 1)
				{
					_substrokes.Sort();
					// Don't count the endpoint of the stroke as a fragmentation point
					for(int i = 0; i < _substrokes.Count - 1; ++i)
					{
						Substroke ss = _substrokes[i];
						fps.Add(ss.PointsL[ss.PointsL.Count - 1]);
					}
				}
				return fps;
			}
		}

		/// <summary>
		/// Returns the spatial length of the stroke (caution: this is not a speedy operation)
		/// </summary>
		public double Length
		{
			get
			{
				double dist = 0.0;
				foreach(Substroke ss in _substrokes)
					dist += ss.SpatialLength;
				return dist;
			}
		}


		#endregion

        #region COMPARISONS

        /// <summary>
        /// Get the unique, unchanging hash code of this stroke.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _xmlAttributes.Id.GetHashCode();
        }

        /// <summary>
        /// Determine whether this stroke is equal to another object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Stroke))
                return false;

            if (obj == this)
                return true;

            Stroke stroke = (Stroke)obj;

            List<Substroke> mySubstrokes = _substrokes;
            List<Substroke> otherSubstrokes = stroke._substrokes;

            if (mySubstrokes.Count != otherSubstrokes.Count)
                return false;

            for (int i = 0; i < mySubstrokes.Count; i++)
            {
                if (!mySubstrokes[i].GeometricEquals(otherSubstrokes[i]))
                    return false;
            }

            return (
                Time == stroke.Time &&
                Name == stroke.Name);
        }

        /// <summary>
        /// Determines equality of Strokes based on whether their substrokes are 
        /// geometrically equal.
        /// </summary>
        /// <param name="other">The other shape the compare to</param>
        /// <returns>Whether or not the Shapes are equal</returns>
        public virtual bool GeometricEquals(Stroke other)
        {
            if (other == null)
                return false;

            if (other == this)
                return true;

            List<Substroke> mySubstrokes = _substrokes;
            List<Substroke> otherSubstrokes = other._substrokes;

            if (mySubstrokes.Count != otherSubstrokes.Count)
                return false;

            for (int i = 0; i < mySubstrokes.Count; i++)
            {
                if (!mySubstrokes[i].GeometricEquals(otherSubstrokes[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compare this Stroke to another based on time.
        /// Returns less than 0 if this time is less than the other's.
        /// Returns 0 if this time is equal to the other's.
        /// Returns greater than if this time is greater than the other's.
        /// </summary>
        /// <param name="obj">The other Stroke to compare this one to</param>
        /// <returns>An integer indicating how the Stroke times compare</returns>
        int System.IComparable.CompareTo(Object obj)
        {
            return (int)(this._xmlAttributes.Time.Value - ((Stroke)obj)._xmlAttributes.Time.Value);
        }

        #endregion

        #region OTHER

        /// <summary>
		/// Creates a copy of the stroke
		/// </summary>
		/// <returns></returns>
        public Stroke Clone()
        {
            Stroke clone = new Stroke(this.CloneSubstrokes(), this._xmlAttributes.Clone());
            return clone;
        }

		/// <summary>
		/// Clone all substrokes
		/// </summary>
		/// <returns>A copy of our substrokes</returns>
        public List<Substroke> CloneSubstrokes()
        {
            return new List<Substroke>(this._substrokes);
        }

		#endregion	
	}
}
