/*
 * File: Shape.cs
 *
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 * 
 * Updated by Sketchers 2011
 */

using Domain;
using System;
using System.Collections.Generic;
using System.Windows;
using Data;

namespace Sketch
{
	/// <summary>
	/// Shape class.
	/// </summary>
	[Serializable]
	public class Shape : IComparable, IMutable
    {

        #region INTERNALS

        /// <summary>
        /// Called when the shape geometry is modified. This is NOT called when recognition changes,
        /// when the shape is renamed, etc.
        /// 
        /// We may want to change this behavior later. -- Calvin Loncaric (July 2011)
        /// </summary>
        public event EventHandler ObjectChanged;

        /// <summary>
		/// Substrokes
		/// </summary>
		private List<Substroke> _substrokes;

		/// <summary>
		/// Xml attributes of the Shape
		/// </summary>
		private XmlStructs.XmlShapeAttrs _xmlAttributes;

        /// <summary>
        /// The string name of the template that the image recognizer used to classify the shape.
        /// Used to keep track of the usefullness of the templates.
        /// </summary>
        public string TemplateName;

        /// <summary>
        /// The bitmap representation of the template that the image recognizer used to classify the shape.
        /// </summary>
        public System.Drawing.Bitmap TemplateDrawing;

        /// <summary>
        /// Bool is true if the user has specified that this shape is correctly grouped.
        /// </summary>
        private bool _userSpecifiedGroup;

        /// <summary>
        /// Bool is true if the user has rotated the ghost gate.
        /// </summary>
        private bool _userSpecifiedOrientation;

        /// <summary>
        /// Bool is true if the user has specified that this shape is correctly labeled.
        /// </summary>
        private bool _userSpecifiedType;

        /// <summary>
        /// Bool is true if the shape has been labeled through recognition, not
        /// necessarily by the user
        /// </summary>
        private bool _labeled;

        /// <summary>
        /// Dictionary of alternate labels and their associated probabilities
        /// </summary>
        private Dictionary<ShapeType, double> _alternateTypes;

        /// <summary>
        /// This value is a marker that will tell Inksketch which subcircuit behavior this shape should have
        /// it will use this number if the shape is a subcircuit to look up the correct sub-InkSketch if this
        /// shape needs to be remade
        /// </summary>
        private int correspondingSubCircuit;

		#endregion

		#region CONSTRUCTORS
		
		/// <summary>
		/// Constructor
		/// </summary>
		public Shape() :
			this(new List<Substroke>(), XmlStructs.XmlShapeAttrs.CreateNew())
		{
			// Calls the main constructor
		}

		/// <summary>
		/// Constructor. Create from Shape. Shallow copy of subshapes, deep copy of substrokes.
		/// </summary>
		/// <param name="shape">A Shape.</param>
		public Shape(Shape shape)
		{
            this.Name = shape.Name;
			List<Substroke> newSubstrokes = new List<Substroke>(shape._substrokes.Count);

			foreach (Substroke s in shape._substrokes)
			{
				Substroke newS = s.CloneConstruct();
				newS.ParentShape = this;
				newSubstrokes.Add(newS);
			}

			_substrokes = new List<Substroke>(shape._substrokes.Count);
			foreach (Substroke s in newSubstrokes)
				AddSubstroke(s);
            _xmlAttributes = shape._xmlAttributes.Clone();

            AlreadyGrouped = shape.AlreadyGrouped;
            AlreadyLabeled = shape.AlreadyLabeled;
            UserLabeled = shape.UserLabeled;

            TemplateName = shape.TemplateName;
            TemplateDrawing = shape.TemplateDrawing;
		}

        /// <summary>
        /// Create a shape from the given substrokes.
        /// </summary>
        /// <param name="substrokes">a list of substrokes</param>
        public Shape(IEnumerable<Substroke> substrokes)
            :this(substrokes, XmlStructs.XmlShapeAttrs.CreateNew())
        {
            // Calls the main constructor
        }

	
		/// <summary>
		/// Construct a Shape with given Shapes, Substrokes, and XML attributes.
		/// </summary>
		/// <param name="substrokes">Substrokes to add</param>
		/// <param name="XmlAttrs">XML attributes of the Shape</param>
		public Shape(IEnumerable<Substroke> substrokes, XmlStructs.XmlShapeAttrs XmlAttrs)
		{
            correspondingSubCircuit = int.MinValue;
			this._substrokes = new List<Substroke>();

			this._xmlAttributes = XmlAttrs;

            this.AddSubstrokes(substrokes);

            AlreadyGrouped = false;
            AlreadyLabeled = false;
            UserLabeled = false;

            Type = (XmlAttrs.Type == null) ? (new ShapeType()) : (Domain.LogicDomain.getType(XmlAttrs.Type));
		}

		#endregion

		#region ADD SUBSTROKE(S)

		/// <summary>
		/// Add a Substroke to this shape. Calls AddSubstrokes() to add the substroke.
		/// </summary>
		/// <param name="substroke">A Substroke</param>
		public void AddSubstroke(Substroke substroke)
	    {
            AddSubstrokes(new Substroke[] { substroke });
		}

        /// <summary>
        /// Called when a substroke is added to this shape, and before it is added
        /// to the list of substrokes. By default this does the following:
        ///    - updates the substroke's parent shape
        ///    - connects this shape to everything the substroke is connected to
        /// </summary>
        /// <param name="substroke">the substroke to acquire</param>
        protected virtual void AcquireSubstroke(Substroke substroke)
        {
            if (substroke.ParentShape != null && substroke.ParentShape != this)
                throw new ArgumentException("Cannot add a substroke belonging to a different shape!");
            substroke.ParentShape = this;

            foreach (EndPoint endpoint in substroke.Endpoints)
            {
                Shape connected = endpoint.ConnectedShape;
                if (connected != null)
                {
                    ConnectedShapes.Add(connected);
                    connected.ConnectedShapes.Add(this);
                }
            }
        }
		
		/// <summary>
        /// Add Substrokes to this shape. 
        /// 
        /// Preconditions:
        ///    - none of the given substrokes belong to another shape
        ///    - none of the given substrokes are already in this shape
        /// 
        /// Postconditions:
        ///    - _substrokes is sorted
        ///    - the following properties are up-to-date:
        ///         X
        ///         Y
        ///         Width
        ///         Height
        ///         Start
        ///         End
        ///         Time
		/// </summary>
		/// <param name="substrokes">The Substrokes</param>
		public void AddSubstrokes(IEnumerable<Substroke> substrokes)
		{
            foreach (Substroke substroke in substrokes)
            {
                if (substroke == null)
                    throw new ArgumentNullException("Cannot add a null substroke to a shape");

                if (_substrokes.Contains(substroke))
                    throw new Exception(this + " already contains substroke " + substroke);

                AcquireSubstroke(substroke);
                _substrokes.Add(substroke);
            }

            UpdateAttributes();

            OnObjectChanged();
		}

		#endregion

        #region REMOVE SUBSTROKE(S)

        /// <summary>
		/// Removes a Substroke from the Shape.
		/// </summary>
		/// <param name="substroke">Substroke to remove</param>
		/// <returns>True iff the Substroke is removed</returns>
		public bool RemoveSubstroke(Substroke substroke)
		{
            return RemoveSubstrokes(new Substroke[] { substroke });
		}

        internal bool RemoveSubstrokes(IEnumerable<Substroke> toRemove, bool messWithConnections)
        {
            bool completelyRemoved = true;

            foreach (Substroke substroke in toRemove)
            {
                bool success;

                if (this._substrokes.Contains(substroke))
                {
                    substroke.ParentShape = null;

                    this._substrokes.Remove(substroke);

                    if (messWithConnections)
                    {
                        bool[] hasConnectionToEndpoint = new bool[2];
                        hasConnectionToEndpoint[0] = false;
                        hasConnectionToEndpoint[1] = false;

                        foreach (EndPoint endpoint in Endpoints)
                        {
                            if (endpoint.ConnectedShape == substroke.Endpoints[0].ConnectedShape)
                                hasConnectionToEndpoint[0] = true;
                            if (endpoint.ConnectedShape == substroke.Endpoints[1].ConnectedShape)
                                hasConnectionToEndpoint[1] = true;
                        }

                        if (!hasConnectionToEndpoint[0] && substroke.Endpoints[0].ConnectedShape != null)
                            disconnectFrom(substroke.Endpoints[0].ConnectedShape);
                        if (!hasConnectionToEndpoint[1] && substroke.Endpoints[1].ConnectedShape != null)
                            disconnectFrom(substroke.Endpoints[1].ConnectedShape);
                    }

                    success = true;
                }
                else
                {
                    success = false;
                }

                if (!success)
                    completelyRemoved = false;
            }

            ClearConnections();
            UpdateAttributes();
            OnObjectChanged();

            return completelyRemoved;
        }
		
		/// <summary>
		/// Removes a set of Substrokes from the Shape.
		/// </summary>
        /// <param name="toRemove">Substrokes to remove</param>
		/// <returns>True iff all Substrokes are removed</returns>
		public bool RemoveSubstrokes(IEnumerable<Substroke> toRemove)
		{
            return RemoveSubstrokes(toRemove, true);
		}

		#endregion

        #region CLEAR CONNECTIONS

        /// <summary>
        /// Removes all the connections from this shape to others. Also removes
        /// connections from substroke endpoints, if there are any.
        /// 
        /// Preconditions:
        ///    - For every shape "s" in ConnectedShapes, s.ConnectedShapes
        ///      contains this shape.
        /// 
        /// Postconditions:
        ///    - For every shape "s" in the old ConnectedShapes, s.ConnectedShapes
        ///      does not contain this shape and there is no endpoint in
        ///      s.Endpoints connected to this shape.
        ///    - ConnectedShapes is empty
        ///    - for every endpoint of every substroke, ConnectedShape is null
        /// </summary>
        public void ClearConnections()
        {
            List<Shape> connectedShapes = new List<Shape>(ConnectedShapes);
            foreach (Shape s in connectedShapes)
                disconnectFrom(s);
        }

        #endregion

        #region UPDATE ATTRIBUTES

        /// <summary>
		/// Updates the spatial attributes of the Shape. Also fires the ObjectChanged event.
        /// 
        /// Postconditions:
        ///    - _substrokes is sorted
        ///    - the following are up-to-date:
        ///         _xmlAttributes.X
        ///         _xmlAttributes.Y
        ///         _xmlAttributes.Width
        ///         _xmlAttributes.Height
        ///         _xmlAttributes.Start
        ///         _xmlAttributes.End
        ///         _xmlAttributes.Time
		/// </summary>
        protected void UpdateAttributes()
		{
            float minX = Single.PositiveInfinity;
            float maxX = Single.NegativeInfinity;

			float minY = Single.PositiveInfinity;
			float maxY = Single.NegativeInfinity;

			// Cycle through the Substrokes within the Shape
			foreach (Substroke s in _substrokes)
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
			this._substrokes.Sort();

			// Update the Start, End and Time attributes
			if (this._substrokes.Count > 0)
			{
				this._xmlAttributes.Start = this._substrokes[0].XmlAttrs.Id;
				this._xmlAttributes.End = this._substrokes[this._substrokes.Count - 1].XmlAttrs.Id;
				this._xmlAttributes.Time = this._substrokes[this._substrokes.Count - 1].XmlAttrs.Time;
			}
			else
			{
				this._xmlAttributes.Start = null;
				this._xmlAttributes.End = null;
				this._xmlAttributes.Time = null;
			}
		}

        /// <summary>
        /// Call this when the shape is modified. It calls the "ObjectChanged" event handler.
        /// </summary>
        protected virtual void OnObjectChanged()
        {
            OnObjectChanged(new EventArgs());
        }

        /// <summary>
        /// Call this when the shape is modified. It calls the "ObjectChanged" event handler.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnObjectChanged(EventArgs args)
        {
            if (ObjectChanged != null)
                ObjectChanged(this, args);
        }

        /// <summary>
        /// Updates a shape to include information from recognition.
        /// </summary>
        /// <param name="shapeType">The most likely type to describe the shape</param>
        /// <param name="typeProbability">The probability the given type is correct</param>
        /// <param name="alternateTypes">Alternative types and their probabilities</param>
        /// <param name="orientation">The direction the shape most likely faces, in radians</param>
        public void setRecognitionResults(
            ShapeType shapeType, 
            float typeProbability, 
            Dictionary<ShapeType, double> alternateTypes, 
            double orientation)
        {
            setRecognitionResults(shapeType, typeProbability);
            AlternateTypes = alternateTypes;
            Orientation = orientation;
        }

        /// <summary>
        /// Updates a shape to include information from adaptive image recognition.
        /// </summary>
        /// <param name="shapeType">The most likely type to describe the shape</param>
        /// <param name="typeProbability">The probability the given type is correct</param>
        /// <param name="alternateTypes">Alternate types and their probabilities (if any)</param>
        /// <param name="orientation">The direction the shape most likely faces, in radians</param>
        /// <param name="templateName">The string name of the template that was used to recognize this shape</param>
        /// <param name="templateBitmap">The bitmap representation of the template that was used to recognize the shape</param>
        public void setRecognitionResults(
            ShapeType shapeType,
            float typeProbability,
            Dictionary<ShapeType, double> alternateTypes,
            double orientation,
            string templateName,
            System.Drawing.Bitmap templateBitmap)
        {
            setRecognitionResults(shapeType, typeProbability, alternateTypes, orientation);
            TemplateName = templateName;
            TemplateDrawing = templateBitmap;
        }
        
        /// <summary>
        /// Updates a shape to include information from recognition.
        /// </summary>
        /// <param name="shapeType">The most likely type to describe the shape</param>
        /// <param name="typeProbability">The probability the given type is correct</param>
        public void setRecognitionResults(ShapeType shapeType, float typeProbability)
        {
            Type = shapeType;
            Probability = typeProbability;
        }

        /// <summary>
        /// Updates a shape to include information from recognition.
        /// </summary>
        /// <param name="shapeType">The most likely type to describe the shape</param>
        /// <param name="typeProbability">The probability the given type is correct</param>
        /// <param name="shapeName">A name to give the shape</param>
        public void setRecognitionResults(ShapeType shapeType, float typeProbability, string shapeName)
        {
            setRecognitionResults(shapeType, typeProbability);
            Name = shapeName;
        }

		#endregion

        #region CONNECTIONS

        /// <summary>
        /// Disconnect two shapes.
        /// 
        /// Note that this.disconnectFrom(shape) should be equivalent to
        /// shape.disconnectFrom(this).
        /// </summary>
        /// <param name="other"></param>
        public void disconnectFrom(Shape other)
        {
            ConnectedShapes.Remove(other);
            other.ConnectedShapes.Remove(this);

            foreach (EndPoint endpoint in Endpoints)
                if (endpoint.ConnectedShape == other)
                    endpoint.ConnectedShape = null;

            foreach (EndPoint endpoint in other.Endpoints)
                if (endpoint.ConnectedShape == this)
                    endpoint.ConnectedShape = null;
        }

        #endregion

        #region DEALING WITH ENDPOINTS

        /// <summary>
        /// Find the endpoint of the strokes of this shape which is closest to
        /// otherShape, and set otherShape to be that endpoint's connected shape.
        /// </summary>
        /// <param name="otherShape"></param>
        public void ConnectNearestEndpointTo(Shape otherShape)
        {
            // Get the endpoint in this shape closest to the other shape
            EndPoint endpt = ClosestEndpointTo(otherShape);
            endpt.ConnectedShape = otherShape;
        }

        /// <summary>
        /// Get the closest endpoint from this shape to the given shape.
        /// </summary>
        /// <param name="othershape">The shape to compare to</param>
        /// <returns>The closest endpoint in otherShape</returns>
        public EndPoint ClosestEndpointTo(Shape othershape)
        {
            return othershape.ClosestEndpointFrom(this);
        }

        /// <summary>
        /// Finds a given shape's closest endpoint to the invoking shape.
        /// </summary>
        /// <param name="otherShape">The shape with endpoints</param>
        /// <returns>The closest endpoint in otherShape</returns>
        public EndPoint ClosestEndpointFrom(Shape otherShape)
        {
            double distance = Double.MaxValue;
            Substroke closestSubstroke = null;
            EndPoint closestEndpoint = null;

            foreach (Substroke stroke in otherShape.Substrokes)
                foreach (EndPoint endpoint in stroke.Endpoints)
                {
                    double ptDist = Math.Sqrt(Math.Pow(endpoint.X - this.Centroid.X, 2) +
                                              Math.Pow(endpoint.Y - this.Centroid.Y, 2));
                    if (ptDist < distance)
                    {
                        distance = ptDist;
                        closestSubstroke = stroke;
                        closestEndpoint = endpoint;
                    }
                }

            return closestEndpoint;
        }

        #endregion

        #region GEOMETRY

        /// <summary>
        /// Find the shortests distance from a point in this shape to a
        /// given point.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="exclude">If this is set, that substroke will not be considered 
        /// when looking for close points</param>
        /// <returns>the shortest distance to a point in a substroke of this shape, or 
        /// Double.MaxValue if there are no points in this substroke</returns>
        public double minDistanceTo(double x, double y, Substroke exclude = null)
        {
            double bestDistance = Double.MaxValue;
            foreach (Substroke substroke in _substrokes)
            {
                if (substroke == exclude)
                    continue;

                double distance = substroke.minDistanceTo(x, y);
                if (distance < bestDistance)
                    bestDistance = distance;
            }
            return bestDistance;
        }

        #endregion

        #region GETTERS & SETTERS

        /// <summary>
        /// Return a string representing this shape. The string uses
        /// the shape's hash code and type to identify it.
        /// </summary>
        /// <returns>a string</returns>
        public override string ToString()
        {
            return String.Format(base.ToString() + " (type=" + Type + ", hash=0x{0:x})", GetHashCode());
        }


        /// <summary>
        /// A double representing the orientation of a shape, in radians. Get or Set.
        /// </summary>
        public double Orientation
        {
            get { return _xmlAttributes.Orientation.Value; }
            set { _xmlAttributes.Orientation = (float)value; }
        }

        /// <summary>
        /// The set of connected shapes.
        /// </summary>
        public HashSet<Shape> ConnectedShapes
        {
            get { return _xmlAttributes.Connections; }
            set { _xmlAttributes.Connections = value; }
        }

        /// <summary>
        /// The set of connected shapes, not including this shape.
        /// </summary>
        public List<Shape> ExternalConnectedShapes
        {
            get { return Data.Utils.filter(_xmlAttributes.Connections, delegate(Shape s) { return s != this; }); }
        }

        /// <summary>
        /// Get a dictionary of connected shapes of a given classification.
        /// </summary>
        /// <param name="classification">the classification to filter by</param>
        /// <returns></returns>
        public List<Shape> ConnectedShapesOfClass(string classification)
        {
            return Data.Utils.filter(ConnectedShapes, delegate(Shape shape) { return shape.Classification == classification; });
        }

        /// <summary>
        /// True iff the user grouped these strokes her/himself. Get or Set.
        /// </summary>
        public bool AlreadyGrouped
        {
            get { return _userSpecifiedGroup; }
            set { _userSpecifiedGroup = value; }
        }

        /// <summary>
        /// True iff the shape has been labeled already.
        /// </summary>
        public bool AlreadyLabeled
        {
            get { return _labeled; }
            set 
            {
                _labeled = value;
                if (value && Type == new ShapeType())
                    throw new Exception("Cannot set AlreadyLabeled on an unknown shape.");
            }
        }

        /// <summary>
        /// True iff the user assigned this label her/himself. Get or Set.
        /// </summary>
        public bool UserLabeled
        {
            get { return _userSpecifiedType; }
            set
            {
                _userSpecifiedType = value;

                // Only reset labeled if user
                // specifying the label has
                // labeled the shape for the first time
                if (!_labeled && value)
                    _labeled = value;
                if (value && Type == new ShapeType())
                    throw new Exception("Cannot set AlreadyLabeled on an unknown shape.");
            }
        }

        /// <summary>
        /// True iff the user rotated the ghost gate her/himself. Get or Set.
        /// </summary>
        public bool AlreadyOriented
        {
            get { return _userSpecifiedOrientation; }
            set
            {
                _userSpecifiedOrientation = value;
            }
        }

        /// <summary>
        /// Get this shape's XML attributes. NOTE: You should NOT set any values
        /// on the returned object, as you may inadvertently create an inconsistent
        /// state on this object. It is much safer to use the getters and setters on
        /// the shape itself; e.g. Color, Start, Source, etc.
        /// </summary>
        public XmlStructs.XmlShapeAttrs XmlAttrs
        {
            get { return _xmlAttributes; }
        }

        /// <summary>
        /// Get or set the color
        /// </summary>
        public int? Color
        {
            get { return _xmlAttributes.Color; }
            set { _xmlAttributes.Color = value; }
        }

        /// <summary>
        /// Get or set the start Guid
        /// </summary>
        public Guid? Start
        {
            get { return _xmlAttributes.Start; }
            set { _xmlAttributes.Start = value; }
        }

        /// <summary>
        /// Get or set the source
        /// </summary>
        public string Source
        {
            get { return _xmlAttributes.Source; }
            set { _xmlAttributes.Source = value; }
        }

        /// <summary>
        /// Get or set the source
        /// </summary>
        public bool? LaysInk
        {
            get { return _xmlAttributes.LaysInk; }
            set { _xmlAttributes.LaysInk = value; }
        }

        /// <summary>
        /// Get or set the source
        /// </summary>
        public ulong? Time
        {
            get { return _xmlAttributes.Time; }
            set { _xmlAttributes.Time = value; }
        }

        /// <summary>
        /// Get or set the raster
        /// </summary>
        public string Raster
        {
            get { return _xmlAttributes.Raster; }
            set { _xmlAttributes.Raster = value; }
        }

        /// <summary>
        /// Get or set the pen tip
        /// </summary>
        public string PenTip
        {
            get { return _xmlAttributes.PenTip; }
            set { _xmlAttributes.PenTip = value; }
        }

        /// <summary>
        /// Get or set the end Guid
        /// </summary>
        public Guid? End
        {
            get { return _xmlAttributes.End; }
            set { _xmlAttributes.End = value; }
        }

   		/// <summary>
		/// Get/Set the Type on a given Shape. Also updates the
        /// classifications of underlying substrokes.
		/// </summary>
		public ShapeType Type
		{
			get
			{
				return LogicDomain.getType(_xmlAttributes.Type);
			}
			set
			{
                Classification = value.Classification;
				_xmlAttributes.Type = value.Name;
                if (value == new ShapeType() && AlreadyLabeled)
                    throw new Exception("You should mark AlreadyLabeled on an unknown shape as false.");
			}
		}

		/// <summary>
		/// Get/Set the lower-case Type on a given Shape
		/// </summary>
		public string LowercasedType
		{
			get
			{
				return _xmlAttributes.Type.ToLower();
			}
			set
			{
				_xmlAttributes.Type = value.ToLower();
			}
		}

		/// <summary>
		/// Get the Probability of the Type on a given Shape
		/// </summary>
		public float Probability
		{
			get
			{
				if (_xmlAttributes.Probability == null)
				{
					return 0.0F;
				}
				else
				{
					return (float)_xmlAttributes.Probability;
				}
			}
            set
            {
                _xmlAttributes.Probability = value;
            }
		}

        /// <summary>
        /// True iff this shape contains no substrokes.
        /// </summary>
        public bool IsEmpty
        {
            get { return _substrokes.Count == 0; }
        }

		/// <summary>
		/// Get Substrokes
		/// </summary>
		public Substroke[] Substrokes
		{
			get
			{
				return this._substrokes.ToArray();
			}
		}

		/// <summary>
		/// Get a List of Substrokes
		/// </summary>
        public List<Substroke> SubstrokesL
        {
            get
            {
                return this._substrokes;
            }
        }

		/// <summary>
		/// The centroid of this shape. Returns coordinates of the form [x, y].
		/// </summary>
		public Point Centroid
		{
			get
			{
                float[] centroid = new float[2];
				int count = 0;
				foreach (Substroke ss in _substrokes)
				{
					foreach (Point p in ss.PointsL)
					{
						centroid[0] += p.X;
						centroid[1] += p.Y;
						++count;
					}
				}
				centroid[0] /= count;
				centroid[1] /= count;
				return new Point(centroid[0], centroid[1]);
			}
		}

		/// <summary>
		/// The GUID of this shape
		/// </summary>
        public Guid Id
        {
            get
            {
                return _xmlAttributes.Id;
            }
        }

        /// <summary>
        /// The points present in the shape. Get only.
        /// </summary>
        public List<Point> Points
        {
            get
            {
                List<Point> points = new List<Point>();
                foreach (Substroke sub in this._substrokes)
                {
                    points.AddRange(sub.PointsL);
                }
                return points;
            }
        }

        /// <summary>
        /// All the endpoints in this shape. Get only.
        /// </summary>
        public List<EndPoint> Endpoints
        {
            get
            {
                List<EndPoint> result = new List<EndPoint>();

                foreach (Substroke substroke in _substrokes)
                {
                    result.AddRange(substroke.Endpoints);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets or sets the unique name of this shape.
        /// </summary>
        public String Name
        {
            get { return _xmlAttributes.Name; }
            set { _xmlAttributes.Name = value; }
        }

        /// <summary>
        /// Gets or sets the dictionary of alternate types for this shape.
        /// </summary>
        public Dictionary<ShapeType, double> AlternateTypes
        {
            get { return this._alternateTypes; }
            set { this._alternateTypes = value; }
        }

        /// <summary>
        /// Get or set the classification of a shape. When you set the
        /// classification, the classification for every substroke is also
        /// updated.
        /// 
        /// WARNING: The returned value may not always agree with 
        /// this.Type.Classification.
        /// </summary>
        public string Classification
        {
            get
            {
                return _xmlAttributes.Classification;
            }
            set
            {
                _xmlAttributes.Classification = value;
                foreach (Substroke substroke in _substrokes)
                    substroke.Classification = value;
            }
        }

        /// <summary>
        /// The rectangle representing the bounds of the shape
        /// </summary>
        public Rect Bounds
        {
            get
            {
                if (_xmlAttributes.X == null || _xmlAttributes.Y == null || _xmlAttributes.Width == null || _xmlAttributes.Height == null)
                    UpdateAttributes();
                
                // If the shape is not in the sketch, we cannot make the bounds because some of these values may be negative.
                if (_xmlAttributes.Width < 0 || _xmlAttributes.Height < 0)
                    throw new Exception("Cannot get the bounds of this shape, shape is probably not in sketch!");

                return new Rect((double)_xmlAttributes.X, (double)_xmlAttributes.Y, (double)_xmlAttributes.Width, (double)_xmlAttributes.Height);
            }
        }

        /// <summary>
        /// Get or set the sub circuit look up number
        /// value will be int.MinValue if it's not a subcircuit
        /// </summary>
        public int SubCircuitNumber
        {
            get
            {
                return correspondingSubCircuit;
            }
            set
            {
                //This number only means something to sub circuit
                if (this.Type == LogicDomain.SUBCIRCUIT)
                {
                    correspondingSubCircuit = value;
                }
                else
                {
                    correspondingSubCircuit = int.MinValue;
                }
            }
        }

        /// <summary>
        /// True iff this shape is a gate
        /// </summary>
        [Obsolete("This property is not domain-independent. Use Domain.LogicDomain.IsGate(shape.Type) instead.")]
        public bool IsGate
        {
            get
            {
                return LogicDomain.IsGate(this.Type);
            }
        }

        /// <summary>
        /// True iff this shape is a wire
        /// </summary>
        [Obsolete("This property is not domain-independent. Use Domain.LogicDomain.IsWire(shape.Type) instead.")]
        public bool IsWire
        {
            get
            {
                return LogicDomain.IsWire(this.Type);
            }
        }

        /// <summary>
        /// True iff this shape is text
        /// </summary>
        [Obsolete("This property is not domain-independent. Use Domain.LogicDomain.IsText(shape.Type) instead.")]
        public bool IsText
        {
            get
            {
                return LogicDomain.IsText(this.Type);
            }
        }
		#endregion

		#region OTHER


		/// <summary>
		/// Get a deep copy of this shape.
		/// </summary>
		/// <returns>The Clone of this shape.</returns>
		public Shape Clone()
		{
			return new Shape(this);
		}

		/// <summary>
		/// Compare this Shape to another based on time.
		/// Returns less than 0 if this time is less than the other's.
		/// Returns 0 if this time is equal to the other's.
		/// Returns greater than if this time is greater than the other's.
		/// </summary>
		/// <param name="obj">The other Shape to compare this one to</param>
		/// <returns>An integer indicating how the Shape times compare</returns>
		int System.IComparable.CompareTo(Object obj)
		{
			return (int)(this.Time.Value - ((Shape)obj).Time.Value);
		}


        /// <summary>
        /// Get the unique, unchanging hash code of this shape.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determine whether this shape is equal to another object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Shape))
                return false;

            if (obj == this)
                return true;

            Shape shape = (Shape)obj;

            List<Substroke> mySubstrokes = _substrokes;
            List<Substroke> otherSubstrokes = shape._substrokes;

            if (mySubstrokes.Count != otherSubstrokes.Count)
                return false;

            for (int i = 0; i < mySubstrokes.Count; i++)
            {
                if (!mySubstrokes[i].GeometricEquals(otherSubstrokes[i]))
                {
                    mySubstrokes[i].GeometricEquals(otherSubstrokes[i]);
                    return false;
                }
            }

            return (
                Type == shape.Type &&
                Name == shape.Name);
        }

		/// <summary>
        /// Determines equality of Shapes based on whether their substrokes are 
        /// geometrically equal.
		/// </summary>
		/// <param name="other">The other shape the compare to</param>
		/// <returns>Whether or not the Shapes are equal</returns>
		public virtual bool GeometricEquals(Shape other)
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
        /// Creates a windows bitmap from the shape
        /// </summary>
        /// <param name="drawWidth"></param>
        /// <param name="drawHeight"></param>
        /// <param name="preserveAspect"></param>
        /// <returns></returns>
        public System.Drawing.Bitmap createBitmap(int drawWidth, int drawHeight, bool preserveAspect)
        {
            Rect bounds = this.Bounds;
            float x = (float)bounds.X;
            float y = (float)bounds.Y;

            // The bounds are calculated weirdly.
            // We need to make them a little wider so that the bitmap
            // doesn't get cut off.
            float w = (float)bounds.Width * (float)1.05;
            float h = (float)bounds.Height * (float)1.05;

            if (preserveAspect)
            {
                float nPercent = 0;
                float nPercentW = 0;
                float nPercentH = 0;

                nPercentW = drawWidth / w;
                nPercentH = drawHeight / h;

                if (nPercentH > nPercentW)
                    nPercent = nPercentH;
                else
                    nPercent = nPercentW;

                drawWidth = (int)Math.Ceiling(w * nPercent);
                drawHeight = (int)Math.Ceiling(h * nPercent);
            }

            float scaleX = (float)drawWidth / w;
            float scaleY = (float)drawHeight / h;

            System.Drawing.Bitmap result = new System.Drawing.Bitmap(drawWidth, drawHeight);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(result);

            // transparent background
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            System.Drawing.Brush clear = new System.Drawing.SolidBrush(System.Drawing.Color.Transparent);
            g.FillRectangle(clear, 0, 0, w, h);

            // draw over transparent background
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Black, 3);

            foreach (Substroke stroke in this.Substrokes)
            {

                int count = stroke.Points.Length;

                if (count <= 1)
                    continue;

                Point previous = stroke.Points[0];

                for (int i = 1; i < count; i++)
                {
                    Point current = stroke.Points[i];
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

        #endregion

    }
}
