/*
 * File: Substroke.cs
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
using System.Collections.ObjectModel;
using MathNet.Numerics.LinearAlgebra;
using Domain;

namespace Sketch
{
    /// <summary>
    /// Substroke class. A substroke is a semi-immutable structure that contains a list
    /// of points, a parent shape, two endpoints etc.
    /// 
    /// Only the parent shape, parent stroke, and certain XML attributes are mutable.
    /// 
    /// NOTE: Please don't try to make substroke geometry mutable. You may think it convenient,
    /// but it really isn't. A lot of code is much simpler if it can assume that substrokes
    /// don't change.
    /// </summary>
    [Serializable]
    public class Substroke : IComparable<Substroke>
    {

        #region Internals

        /// <summary>
        /// The points of the Substroke. Always kept sorted by time.
        /// </summary>
        private readonly ReadOnlyCollection<Point> _points;

        /// <summary>
        /// This is the parent stroke
        /// </summary>
        private Stroke _parentStroke;

        /// <summary>
        /// This is the parent shape
        /// </summary>
        private Shape _parentShape;

        /// <summary>
        /// The XML attributes of the Substroke
        /// </summary>
        private XmlStructs.XmlShapeAttrs _xmlAttributes;

        /// <summary>
        /// Spatial Length of the substroke
        /// </summary>
        private readonly Lazy<double> _spatialLength;

        /// <summary>
        /// An array of the substroke's endpoints. 
        /// 
        /// Note: there should be no more than two!
        /// </summary>
        private readonly Lazy<EndPoint[]> _endpoints;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public Substroke(IEnumerable<Point> points)
            : this(points, XmlStructs.XmlShapeAttrs.CreateNew())
        {
            // Calls the main constructor
        }

        /// <summary>
        /// Creates a Substroke from a List of Points and the XML attributes
        /// </summary>
        /// <param name="points">List of Points</param>
        /// <param name="XmlAttrs">The XML attributes of the stroke</param>
        public Substroke(IEnumerable<Point> points, XmlStructs.XmlShapeAttrs XmlAttrs)
        {
            List<Point> pts = new List<Point>(points);

            if (pts.Count == 0)
                throw new ArgumentException("Substrokes must contain a nonzero number of points");

            pts.Sort();
            _points = pts.AsReadOnly();
            _parentShape = null;
            _parentStroke = null;
            _endpoints = null;
            _xmlAttributes = XmlAttrs;

            _spatialLength = new Lazy<double>(calculateSpatialLength);
            _endpoints = new Lazy<EndPoint[]>(findEndpoints);

            if (_points.Count > 0)
                _xmlAttributes.Time = _points[_points.Count - 1].Time;
            else
                _xmlAttributes.Time = null;

            float minX = Single.PositiveInfinity;
            float maxX = Single.NegativeInfinity;

            float minY = Single.PositiveInfinity;
            float maxY = Single.NegativeInfinity;

            Point point;
            int len = _points.Count;
            int i;
            for (i = 0; i < len; ++i)
            {
                point = _points[i];

                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);

                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            _xmlAttributes.Type = "substroke";
            _xmlAttributes.X = minX;
            _xmlAttributes.Y = minY;
            _xmlAttributes.Width = maxX - minX;
            _xmlAttributes.Height = maxY - minY;
        }


        #endregion

        #region Getters & Setters

        /// <summary>
        /// The XML attributes for this substroke
        /// </summary>
        public XmlStructs.XmlShapeAttrs XmlAttrs
        {
            get { return _xmlAttributes; }
        }

        /// <summary>
        /// Get or set the name
        /// </summary>
        public string Name
        {
            get { return _xmlAttributes.Name; }
            set { _xmlAttributes.Name = value; }
        }

        /// <summary>
        /// Get or set the pen width
        /// </summary>
        public float? PenWidth
        {
            get { return _xmlAttributes.PenWidth; }
            set { _xmlAttributes.PenWidth = value; }
        }

        /// <summary>
        /// Get or set the pen height
        /// </summary>
        public float? PenHeight
        {
            get { return _xmlAttributes.PenHeight; }
            set { _xmlAttributes.PenHeight = value; }
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
        public ulong Time
        {
            get { return _xmlAttributes.Time.Value; }
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
        /// Returns the spatial length of this substroke
        /// </summary>
        public double SpatialLength
        {
            get { return _spatialLength.Value; }
        }

        /// <summary>
        /// Gets a Point[] of the points contained in the Substroke.
        /// </summary>
        public Point[] Points
        {
            get { return PointsL.ToArray(); }
        }

        /// <summary>
        /// Gets a copy of the list of the points contained in the Substroke.
        /// </summary>
        public List<Point> PointsL
        {
            get { return new List<Point>(_points); }
        }

        /// <summary>
        /// Get the Points in this substroke as an array of System.Drawing.PointFs.
        /// </summary>
        public System.Drawing.PointF[] PointsAsPointFs
        {
            get
            {
                System.Drawing.PointF[] pts = new System.Drawing.PointF[_points.Count];
                for (int i = 0; i < _points.Count; ++i)
                    pts[i] = _points[i].SysDrawPointF;
                return pts;
            }
        }

        /// <summary>
        /// The stroke this substroke belongs to
        /// </summary>
        public Stroke ParentStroke
        {
            get { return this._parentStroke; }
            internal set { this._parentStroke = value; }
        }

        /// <summary>
        /// The shape this substroke belongs to
        /// </summary>
        public Shape ParentShape
        {
            get { return _parentShape; }
            internal set { _parentShape = value; }
        }

        /// <summary>
        /// Get the labels associated with a Substroke.
        /// </summary>
        /// <returns></returns>
        public ShapeType Type
        {
            get
            {
                if (ParentShape != null)
                    return ParentShape.Type;
                return new ShapeType();
            }
        }

        /// <summary>
        /// The GUID associated with this substroke
        /// </summary>
        public Guid Id
        {
            get { return XmlAttrs.Id; }
        }

        /// <summary>
        /// Find the centroid for the substroke
        /// returns a double array {X,Y}
        /// </summary>
        public double[] Centroid
        {
            get
            {
                double[] res = new double[] { 0d, 0d };

                foreach (Point p in PointsL)
                {
                    res[0] += p.X;
                    res[1] += p.Y;
                }

                res[0] /= PointsL.Count;
                res[1] /= PointsL.Count;

                return res;
            }
        }
        /// <summary>
        /// Find the two endpoints of this substroke. Tolerant of overstroking.
        /// Preserves endpoint connections.
        /// </summary>
        private EndPoint[] findEndpoints()
        {
            if (_points.Count == 0)
                return null;

            // TODO: Find a better way to calculate endpoints
            EndPoint end1 = new EndPoint(_points[0], this);
            EndPoint end2 = new EndPoint(_points[_points.Count - 1], this);

            // Calculate endpoint slopes
            end1.DetermineSlope();
            end2.DetermineSlope();

            return new EndPoint[2] { end1, end2 };
        }

        /// <summary>
        /// Get the two endpoints of this substroke.
        /// </summary>
        public EndPoint[] Endpoints
        {
            get { return _endpoints.Value; }
        }

        /// <summary>
        /// This is the classification of the substroke as gate, label, or wire
        /// </summary>
        public String Classification
        {
            get
            {
                if (this.XmlAttrs.Classification == null)
                    return "Unknown";
                return this.XmlAttrs.Classification;
            }
            set
            {
                _xmlAttributes.Classification = value;
            }
        }

        /// <summary>
        /// This is the probability associated with the classification
        /// </summary>
        public float ClassificationBelief
        {
            get
            {
                if (this.XmlAttrs.ClassificationBelief == null)
                    return 0f;
                else
                    return (float)this.XmlAttrs.ClassificationBelief;
            }
            set { _xmlAttributes.ClassificationBelief = value; }
        }

        #endregion

        #region OTHER

        /// <summary>
        /// Find the endpoint of this substroke which is closest to
        /// otherShape, and set otherShape to be that endpoint's connected shape.
        /// </summary>
        /// <param name="otherShape"></param>
        public void ConnectNearestEndpointTo(Shape otherShape)
        {
            EndPoint endpt = ClosestEndpointTo(otherShape);
            endpt.ConnectedShape = otherShape;
        }

        /// <summary>
        /// Finds this substroke's closest endpoint to the invoking shape.
        /// </summary>
        /// <param name="otherShape">The shape with endpoints</param>
        /// <returns>The closest endpoint in otherShape</returns>
        public EndPoint ClosestEndpointTo(Shape otherShape)
        {
            double distance0 = otherShape.minDistanceTo(Endpoints[0].X, Endpoints[0].Y, this);
            double distance1 = otherShape.minDistanceTo(Endpoints[1].X, Endpoints[1].Y, this);

            if (distance0 < distance1)
                return Endpoints[0];
            return Endpoints[1];
        }

        /// <summary>
        /// Find the minimum distance.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>the shortest distance to a point in this substroke, or 
        /// Double.MaxValue if there are no points in this substroke</returns>
        public double minDistanceTo(double x, double y)
        {
            Point source = new Point((float)x, (float)y);
            return Sketch.MinDistance(new Point[] { source }, Points);
        }

        /// <summary>
        /// Calculate the cached spatial length
        /// </summary>
        private double calculateSpatialLength()
        {
            double dist = 0.0;
            List<Point> ps = PointsL;
            for (int i = 0; i < ps.Count - 1; ++i)
            {
                dist += ps[i].distance(ps[i + 1]);
            }
            return dist;
        }

        /// <summary>
        /// Get the unique, unchanging hash code of this substroke.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determine whether this substroke is equal to another object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Substroke))
                return false;

            if (obj == this)
                return true;

            Substroke substroke = (Substroke)obj;

            if (!GeometricEquals(substroke))
                return false;
            
            return (
                Classification == substroke.Classification &&
                Time == substroke.Time);
        }

        /// <summary>
        /// Compares Substrokes for geometric equality. Two substrokes are geometrically equal if they share all
        /// the same points in the same order (points are compared with their .Equals() method).
        /// </summary>
        /// <param name="substroke">the substroke to compare to</param>
        /// <returns></returns>
        public virtual bool GeometricEquals(Substroke substroke)
        {
            if (substroke == this)
                return true;

            if (substroke.Time != Time)
                return false;

            if (substroke.PointsL.Count != PointsL.Count)
                return false;

            for (int i = 0; i < _points.Count; i++)
            {
                if (!_points[i].Equals(substroke._points[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compare this Substroke to another based on time.
        /// Returns less than 0 if this time is less than the other's.
        /// Returns 0 if this time is equal to the other's.
        /// Returns greater than 0 if this time is greater than the other's.
        /// </summary>
        /// <param name="substroke">The other Substroke to compare this one to</param>
        /// <returns>An integer indicating how the Substroke times compare</returns>
        int System.IComparable<Substroke>.CompareTo(Substroke substroke)
        {
            return (int)(this.XmlAttrs.Time.Value - substroke.XmlAttrs.Time.Value);
        }

        /// <summary>
        /// Clones a Substroke (partial-deep copy. Sort of silly as it stands now).
        /// </summary>
        /// <returns>A clone of the current substroke</returns>
        public Substroke Clone()
        {
            Substroke clone = CloneConstruct();

            // What should we do with the parent shape reference?
            clone.ParentShape = null;

            // Do we want to deep clone parent stroke or should they be left as references?
            if (this._parentStroke != null)
                clone.ParentStroke = this._parentStroke.Clone();
            return clone;
        }

        /// <summary>
        /// Clone the substroke, but empty its parentShapes and parentStrokes
        /// </summary>
        /// <returns>The clone of the Substroke</returns>
        internal Substroke CloneConstruct()
        {
            return new Substroke(Points, _xmlAttributes.Clone());
        }

        /// <summary>
        /// Return a string representation of this substroke
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("Substroke {0}", Id);
        }

        #endregion


    }
}
