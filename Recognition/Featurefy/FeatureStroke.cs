using System;
using System.IO;
using Sketch;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Featurefy
{
	/// <summary>
	/// FeatureStroke class. Takes in a regular Stroke and "Featurefies" it by
	/// assigning profiles for speed, arc length, slope, and curvature.
	/// </summary>
	[Serializable]
	public class FeatureStroke
	{
		#region INTERNALS

		/// <summary>
		/// The substroke itself
		/// </summary>
		private Substroke _substroke;

		/// <summary>
		/// Speed information for the given Substroke
		/// </summary>
		private Speed speed;

        /// <summary>
        /// An axially aligned bounding box around the substroke.
        /// </summary>
        private System.Drawing.RectangleF boundingBox;

		/// <summary>
		/// Arc length information for the given Substroke
		/// </summary>
		private ArcLength arcLength;

		/// <summary>
		/// Slope information for the given Substroke
		/// </summary>
		private Slope slope;

		/// <summary>
		/// Curvature information for the given Substroke
		/// </summary>
		private Curvature curvature;

        /// <summary>
        /// The angles calculated between consecutive sets of 3 points
        /// throughout the stroke.
        /// </summary>
        private double[] thetas;

		/// <summary>
		/// Spatial spatial information for the given Substroke
		/// </summary>
		private Spatial spatial;

		/// <summary>
		/// Line fit stuff
		/// </summary>
		private Fit fit;

		/// <summary>
		/// The Sketch.Stroke Id that maps this FeatureStroke to a regular Stroke
		/// </summary>
		private System.Guid id;

		/// <summary>
		/// What fraction of the arc length must the endpoints of a stroke be within to count as closed?
		/// </summary>
		private static double CLOSED_THRESHOLD = 0.10;

        /// <summary>
        /// A list of the calculated features, linked to string values indicating
        /// what feature it is.
        /// </summary>
        private Dictionary<string, Feature> features;

        /// <summary>
        /// A list that is input into the stroke class indicating which features
        /// to calculate and use.
        /// </summary>
        private Dictionary<string, bool> featuresToUse;

		#endregion		

		#region CONSTRUCTORS

		/// <summary>
		/// Constructs a new FeatureStroke from an existing one.
		/// </summary>
		/// <param name="fStroke">The given FeatureStroke to copy</param>
		public FeatureStroke(FeatureStroke fStroke) :
            this(new Substroke(new List<Point>(fStroke.Points), XmlStructs.XmlShapeAttrs.CreateNew()))
		{
			id = fStroke.id;
		}
		
		/// <summary>
		/// Finds the features of a Substroke
		/// </summary>
		/// <param name="substroke">Substroke to find features for</param>
		public FeatureStroke(Sketch.Substroke substroke)
		{
			Point[] points = substroke.Points;
			_substroke = substroke;
			spatial   = new Spatial(points);
			arcLength = new ArcLength(points);
			slope	   = new Slope(points);
			speed	   = new Speed(points);
			curvature = new Curvature(points, ArcLength.Profile, Slope.TanProfile);
			fit = new Fit(substroke);
            boundingBox = Compute.BoundingBox(substroke.Points);

			this.id = (System.Guid)substroke.Id;

            features = new Dictionary<string, Feature>();
            featuresToUse = new Dictionary<string, bool>();
            addAllFeatures();
            
            if (UseThetas)
            {
                thetas = Compute.FindThetas(substroke.Points);
                //thetas = Compute.Normalize(thetas, Math.PI * 2.0);
            }
            else
                thetas = new double[0];

            if (UseArcLength)
                features.Add("Arc Length", new InkLength(arcLength.TotalLength));

            if (UseClosedPath)
            {
                features.Add("Part of a Closed Path", new PartOfClosedPath(false));
                features.Add("Inside a Closed Path", new InsideClosedPath(false));
            }
		}

        /// <summary>
        /// Constructor for Single Stroke Features. Calculates Single Stroke Features
        /// </summary>
        /// <param name="substroke">Stroke to find feature values for</param>
        /// <param name="featureList">Which features to use</param>
        public FeatureStroke(Sketch.Substroke substroke, Dictionary<string, bool> featureList):
            this(substroke)
        {
            featuresToUse = featureList;

            CalculateSingleStrokeFeatures();
        }
	
		#endregion

		#region GETTERS & SETTERS

        /// <summary>
        /// If there is no featuretouse add every fearture 
        /// TODO finish adding more features this only takes care of what's needed in the constructor (others may not matter)
        /// </summary>
        private void addAllFeatures()
        {
            featuresToUse.Add("Sum of Thetas", true);
            featuresToUse.Add("Sum of Abs Value of Thetas", true);
            featuresToUse.Add("Sum of Squared Thetas", true);
            featuresToUse.Add("Sum of Sqrt of Thetas", true);
            featuresToUse.Add("Average Pen Speed", true);
            featuresToUse.Add("Maximum Pen Speed", true);
            featuresToUse.Add("Minimum Pen Speed", true);
            featuresToUse.Add("Difference Between Maximum and Minimum Pen Speed", true);
            featuresToUse.Add("Arc Length", true);
            featuresToUse.Add("Part of a Closed Path", true);
            featuresToUse.Add("Number of 'LL' Intersections", true);
            featuresToUse.Add("Number of 'LX' Intersections", true);
            featuresToUse.Add("Number of 'XL' Intersections", true);
            featuresToUse.Add("Number of 'XX' Intersections", true);
            featuresToUse.Add("Distance To Left or Right Edge", true);
            featuresToUse.Add("Distance To Top or Bottom Edge", true);
            featuresToUse.Add("Time to Previous Stroke", true);
            featuresToUse.Add("Time to Next Stroke", true);
        }

        /// <summary>
        /// Get/Set the dictionary of features for this stroke.
        /// </summary>
        public Dictionary<string, Feature> Features
        {
            get { return features; }
            set { features = value; }
        }

        /// <summary>
        /// Methods for checking the feature dictionary for features we are using.
        /// </summary>
        private bool UseThetas
        {
            get
            {
                if (featuresToUse.ContainsKey("Sum of Thetas") && featuresToUse["Sum of Thetas"])
                    return true;
                else if (featuresToUse.ContainsKey("Sum of Abs Value of Thetas") && featuresToUse["Sum of Abs Value of Thetas"])
                    return true;
                else if (featuresToUse.ContainsKey("Sum of Squared Thetas") && featuresToUse["Sum of Squared Thetas"])
                    return true;
                else if (featuresToUse.ContainsKey("Sum of Sqrt of Thetas") && featuresToUse["Sum of Sqrt of Thetas"])
                    return true;
                else
                    return false;
            }
        }

        private bool UseSpeeds
        {
            get
            {
                if (featuresToUse.ContainsKey("Average Pen Speed") && featuresToUse["Average Pen Speed"])
                    return true;
                else if (featuresToUse.ContainsKey("Maximum Pen Speed") && featuresToUse["Maximum Pen Speed"])
                    return true;
                else if (featuresToUse.ContainsKey("Minimum Pen Speed") && featuresToUse["Minimum Pen Speed"])
                    return true;
                else if (featuresToUse.ContainsKey("Difference Between Maximum and Minimum Pen Speed") && featuresToUse["Difference Between Maximum and Minimum Pen Speed"])
                    return true;
                else
                    return false;
            }
        }

        private bool UseArcLength
        {
            get
            {
                if (featuresToUse.ContainsKey("Arc Length") && featuresToUse["Arc Length"])
                    return true;
                else if (featuresToUse.ContainsKey("Path Density") && featuresToUse["Path Density"])
                    return true;
                else if (featuresToUse.ContainsKey("End Point to Arc Length Ratio") && featuresToUse["End Point to Arc Length Ratio"])
                    return true;
                else if (featuresToUse.ContainsKey("Self Enclosing") && featuresToUse["Self Enclosing"])
                    return true;
                else if (featuresToUse.ContainsKey("Average Pen Speed") && featuresToUse["Average Pen Speed"])
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Returns true if this is in a closed path or part of a closed path
        /// </summary>
        public bool UseClosedPath
        {
            get
            {
                if (featuresToUse.ContainsKey("Part of a Closed Path") && featuresToUse["Part of a Closed Path"])
                    return true;
                else if (featuresToUse.ContainsKey("Inside a Closed Path") && featuresToUse["Inside a Closed Path"])
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Returns true if this contains intersections
        /// </summary>
        public bool UseIntersections
        {
            get
            {
                if (featuresToUse.ContainsKey("Number of 'LL' Intersections") && featuresToUse["Number of 'LL' Intersections"])
                    return true;
                else if (featuresToUse.ContainsKey("Number of 'XX' Intersections") && featuresToUse["Number of 'XX' Intersections"])
                    return true;
                else if (featuresToUse.ContainsKey("Number of 'LX' Intersections") && featuresToUse["Number of 'LX' Intersections"])
                    return true;
                else if (featuresToUse.ContainsKey("Number of 'XL' Intersections") && featuresToUse["Number of 'XL' Intersections"])
                    return true;
                else
                    return false;
            }
        }

		/// <summary>
		/// Returns the Points of the Featurefied Stroke.
		/// </summary>
		public Point[] Points
		{
			get
			{
				return _substroke.Points;
			}
		}

		
		/// <summary>
		///  Returns the Spatial information for the substroke.
		/// </summary>
		public Spatial Spatial
		{
			get
			{
				return spatial;
			}
		}

		
		/// <summary>
		/// Returns the ArcLength information for the substroke.
		/// </summary>
		public ArcLength ArcLength
		{
			get
			{
				return arcLength;
			}
		}


		/// <summary>
		/// Returns the Slope information for the substroke.
		/// </summary>
		public Slope Slope
		{
			get
			{
				return slope;
			}
		}


		/// <summary>
		/// Returns the Speed information for the substroke.
		/// </summary>
		public Speed Speed
		{
			get
			{
				return speed;
			}
		}


		/// <summary>
		/// Returns the Curvature information for the FeatureStroke.
		/// </summary>
		public Curvature Curvature
		{
			get
			{
				return curvature;
			}
		}

		/// <summary>
		/// Returns the least-squares fitting information for this FeatureStroke
		/// </summary>
		public Fit Fit
		{
			get
			{
				return fit;
			}
		}

		
		/// <summary>
		/// Returns the Guid information for the FeatureStroke.
		/// </summary>
		public System.Guid Id
		{
			get
			{
				return id;
			}
		}

		/// <summary>
		/// Predicate for whether or not the stroke forms a closed shape by itself.
		/// Works based on endpoints, and thus might be over-sensitive to overstroking.
		/// </summary>
		public bool ClosedShape
		{
			get
			{
				return Spatial.DistanceFromFirstToLast < CLOSED_THRESHOLD * arcLength.TotalLength;
			}
		}

		/// <summary>
		/// The substroke for this FeatureStroke
		/// </summary>
		public Substroke Substroke
		{
			get
			{
				return _substroke;
			}
		}

		#endregion

        #region METHODS
        /// <summary>
        /// Calculates the features which don't depend on any
        /// other strokes. These values do not change
        /// </summary>
        public void CalculateSingleStrokeFeatures()
        {
            string key = "";
            #region Size Features

            // Arc Length already added if necessary
            key = "Bounding Box Width";
            if (featuresToUse.ContainsKey(key) && featuresToUse["Bounding Box Width"])
                features.Add("Bounding Box Width", new Width(boundingBox));

            key = "Bounding Box Height";
            if (featuresToUse.ContainsKey("Bounding Box Height") && featuresToUse["Bounding Box Height"])
                features.Add("Bounding Box Height", new Height(boundingBox));

            key = "Bounding Box Area";
            if (featuresToUse.ContainsKey("Bounding Box Area") && featuresToUse["Bounding Box Area"])
                features.Add("Bounding Box Area", new Area(boundingBox));

            key = "Path Density";
            if (featuresToUse.ContainsKey("Path Density") && featuresToUse["Path Density"] && features.ContainsKey("Arc Length"))
                features.Add("Path Density", new PathDensity(features["Arc Length"].Value, (double)(boundingBox.Width), (double)(boundingBox.Height)));

            key = "End Point to Arc Length Ratio";
            if (featuresToUse.ContainsKey("End Point to Arc Length Ratio") && featuresToUse["End Point to Arc Length Ratio"] && features.ContainsKey("Arc Length"))
                features.Add("End Point to Arc Length Ratio", new EndPt2LengthRatio(_substroke.Points, features["Arc Length"].Value));

            #endregion

            #region Curvature Features

            key = "Sum of Thetas";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new SumTheta(thetas));

            key = "Sum of Abs Value of Thetas";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new SumAbsTheta(thetas));

            key = "Sum of Squared Thetas";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new SumSquaredTheta(thetas));

            key = "Sum of Sqrt of Thetas";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new SumSqrtTheta(thetas));

            #endregion

            #region Speed / Time Features

            key = "Average Pen Speed";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key] && features.ContainsKey("Arc Length"))
                features.Add(key, new AvgSpeed(_substroke.Points, features["Arc Length"].Value));

            key = "Maximum Pen Speed";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new MaxSpeed(speed.Profile));

            key = "Minimum Pen Speed";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new MinSpeed(speed.Profile));

            key = "Difference Between Maximum and Minimum Pen Speed";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new MaxMinDiffSpeed(speed.Profile));

            key = "Time to Draw Stroke";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new StrokeTime(_substroke.Points));

            #endregion

            key = "Number of Self Intersections";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
                features.Add(key, new NumSelfIntersection(_substroke.Points));

            key = "Self Enclosing";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key] && features.ContainsKey("Arc Length"))
                features.Add(key, new SelfEnclosing(_substroke.Points, features["Arc Length"].Value));
        }

        /// <summary>
        /// Calculates the features which depend on other strokes 
        /// but are not going to change.
        /// </summary>
        public void CalculateMultiStrokeFeatures(SortedList<ulong, Substroke> order, System.Drawing.RectangleF sketchBox)
        {
            string key = "Time to Previous Stroke";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
            {
                if (!features.ContainsKey(key))
                    features.Add(key, new TimeToPrevious(order, _substroke));
                else
                    features[key] = new TimeToPrevious(order, _substroke);
            }

            key = "Time to Next Stroke";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
            {
                if (!features.ContainsKey(key))
                    features.Add(key, new TimeToNext(order, _substroke));
                else
                    features[key] = new TimeToNext(order, _substroke);
            }

            key = "Distance To Left or Right Edge";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
            {
                if (!features.ContainsKey(key))
                    features.Add(key, new DistanceToLREdge(sketchBox, boundingBox));
                else
                    features[key] = new DistanceToLREdge(sketchBox, boundingBox);
            }

            key = "Distance To Top or Bottom Edge";
            if (featuresToUse.ContainsKey(key) && featuresToUse[key])
            {
                if (!features.ContainsKey(key))
                    features.Add(key, new DistanceToTBEdge(sketchBox, boundingBox));
                else
                    features[key] = new DistanceToTBEdge(sketchBox, boundingBox);
            }
        }

        /// <summary>
        /// Sets the features of the given intersections
        /// </summary>
        /// <param name="intersections"></param>
        public void SetIntersectionFeatures(Dictionary<string, int> intersections)
        {
            if (intersections == null)
                throw new ArgumentNullException();

            foreach (KeyValuePair<string, int> pair in intersections)
            {
                if (pair.Key == "Number of 'LL' Intersections")
                {
                    if (!features.ContainsKey(pair.Key))
                        features.Add(pair.Key, new NumLLIntersection(pair.Value));
                    else
                        features[pair.Key] = new NumLLIntersection(pair.Value);
                }
                else if (pair.Key == "Number of 'XX' Intersections")
                {
                    if (!features.ContainsKey(pair.Key))
                        features.Add(pair.Key, new NumXXIntersection(pair.Value));
                    else
                        features[pair.Key] = new NumXXIntersection(pair.Value);
                }
                else if (pair.Key == "Number of 'LX' Intersections")
                {
                    if (!features.ContainsKey(pair.Key))
                        features.Add(pair.Key, new NumLXIntersection(pair.Value));
                    else
                        features[pair.Key] = new NumLXIntersection(pair.Value);
                }
                else if (pair.Key == "Number of 'XL' Intersections")
                {
                    if (!features.ContainsKey(pair.Key))
                        features.Add(pair.Key, new NumXLIntersection(pair.Value));
                    else
                        features[pair.Key] = new NumXLIntersection(pair.Value);
                }
                else
                    throw new Exception("Unknown type of intersection");
            }
        }
        #endregion

        #region COMPUTATION

        /// <summary>
		/// Computes all of the Profiles.
		/// This really isn't necessary. Everything is lazy and calculates when you need it.
		/// </summary>
		public void computeAll()
		{
			object temp;
			temp = ClosedShape;
			speed.computeAll();
			arcLength.computeAll();
			slope.computeAll();
			curvature.computeAll();
			spatial.computeAll();
			fit.computeAll();
		}

		#endregion

		#region OTHER

		/// <summary>
		/// Clones a FeatureStroke.
		/// </summary>
		/// <returns>A new, cloned FeatureStroke</returns>
		public FeatureStroke Clone()
		{
			return new FeatureStroke(this);
		}

        

		#endregion

		#region Serialization

		/// <summary>
		/// Serialize this FeatureStroke and write to a file
		/// Will compute all features ahead of time (what's the point of serialization
		/// if you still have to compute after you restore?)
		/// </summary>
		/// <param name="filename">The file to write to</param>
		public void writeToFile(string filename)
		{
			computeAll();
			Stream s = File.Open(filename, FileMode.Create);
			BinaryFormatter fmt = new BinaryFormatter();
			fmt.Serialize(s, this);
			s.Close();
		}

		/// <summary>
		/// Restore a FeatureStroke from a file
		/// </summary>
		/// <param name="filename">The file to read from</param>
		/// <returns>The stroke read in</returns>
		public FeatureStroke readFromFile(string filename)
		{
			Stream s = File.Open(filename, FileMode.Open);
			BinaryFormatter fmt = new BinaryFormatter();
			FeatureStroke fs = (FeatureStroke)fmt.Deserialize(s);
			s.Close();
			return fs;
		}

		#endregion
	}
}
