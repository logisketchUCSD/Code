using System;
using System.Collections.Generic;

namespace SymbolRec
{
    /// <summary>
    /// A class that is a wrapper for an array of substrokes and its weighted center.
    /// This class prevents multiple calls to find the center :)
    /// </summary>
    public class Substrokes
    {
        #region INTERNALS

        /// <summary>
        /// Weighted center of the substrokes
        /// </summary>
        private Sketch.Point m_weightedCenter;

        private float? m_maxWeightedCenter;


        /// <summary>
        /// Center of the substrokes (center of bounding box)
        /// </summary>
        private Sketch.Point m_center;

        private float? m_maxCenter;



        private float? m_minX;

        private float? m_minY;
        
        private float? m_maxX;
        
        private float? m_maxY;


        /// <summary>
        /// Substrokes
        /// </summary>
        private List<Sketch.Substroke> m_substrokes;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Constructor
        /// </summary>
        public Substrokes()
        {
            m_substrokes = new List<Sketch.Substroke>();
            setNull();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="substrokes">The substrokes to store</param>
        public Substrokes(Sketch.Substroke[] substrokes)
        {
            m_substrokes = new List<Sketch.Substroke>(substrokes);
            setNull();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="substrokes"></param>
        public Substrokes(List<Sketch.Substroke> substrokes)
        {
            m_substrokes = substrokes;
            setNull();
        }

        #endregion

        #region ADD / REMOVE

        /// <summary>
        /// Add a Substroke 
        /// </summary>
        /// <param name="substroke">The Substroke to add</param>
        public void Add(Sketch.Substroke substroke)
        {
            m_substrokes.Add(substroke);
            setNull();
        }

        /// <summary>
        /// Remove a Substroke
        /// </summary>
        /// <param name="substroke">The Substroke to remove</param>
        public void Remove(Sketch.Substroke substroke)
        {
            m_substrokes.Remove(substroke);
            setNull();
        }

        /// <summary>
        /// Remove a Substroke at the specified index
        /// </summary>
        /// <param name="index">The index to remove from</param>
        public void Remove(int index)
        {
            m_substrokes.RemoveAt(index);
            setNull();
        }


        #endregion

		/// <summary>
		/// Resets all cached values to null
		/// </summary>
        private void setNull()
        {
            m_weightedCenter = null;
            m_center = null;

            m_maxWeightedCenter = null;
            m_maxCenter = null;

            m_minX = null;
            m_minY = null;
            m_maxX = null;
            m_maxY = null;
        }

		/// <summary>
		/// Rotate the substrokes around their weighted center
		/// </summary>
		/// <param name="theta">The angle to rotate by (in radians)</param>
        public void rotate(double theta)
        {
            float x = WeightedCenter.X;
            float y = WeightedCenter.Y;

            int len = m_substrokes.Count;
            for (int i = 0; i < len; ++i)
                m_substrokes[i].rotate(theta, x, y);

            //Set null, but the center stays the same :)
            Sketch.Point tempCenter = m_weightedCenter;
            setNull();
            m_weightedCenter = tempCenter;
        }

		/// <summary>
		/// Create a clone of these substrokes and rotate it around its weighted center
		/// </summary>
		/// <param name="theta">The angle to rotate by (in radians)</param>
		/// <returns>The rotated Substrokes</returns>
        public Substrokes rotateClone(double theta)
        {
            Substrokes clone = this.Clone();
            clone.rotate(theta);
            return clone;
        }

        #region COMPUTE

        /// <summary>
        /// Computes the weighted center of the substrokes
        /// </summary>
        private void computeWeightedCenter()
        {
            int i, len = m_substrokes.Count;
            Featurefy.Spatial spatial;
            Sketch.Point wAvg;
            float length;

            //Find weighted average point of many substrokes
            ulong XL = 0, YL = 0;
            float L = 0.0f;
            for (i = 0; i < len; ++i)
            {
                spatial = new Featurefy.Spatial(m_substrokes[i].Points);
                wAvg = spatial.WeightedAveragePoint;
                length = spatial.Length;
                
                //If there is only 1 point
                if (length == 0.0f)
                    length = float.Epsilon;

                XL += (ulong)(wAvg.X * length);
                YL += (ulong)(wAvg.Y * length);
                L += length;
            }

            m_weightedCenter = new Sketch.Point();
            m_weightedCenter.XmlAttrs.X = XL / L;
            m_weightedCenter.XmlAttrs.Y = YL / L;
        }

		/// <summary>
		/// Compute the bounding box center of these substrokes
		/// </summary>
        private void computeCenter()
        {
            float xCenter = (MaxX + MinX) / 2;
            float yCenter = (MaxY + MinY) / 2;
            m_center = new Sketch.Point();
            m_center.XmlAttrs.X = xCenter;
            m_center.XmlAttrs.Y = yCenter;
        }

		/// <summary>
		/// Compute the bounding box coordinates
		/// </summary>
        private void computeMinMax()
        {
            m_minX = float.PositiveInfinity;
            m_minY = float.PositiveInfinity;
            m_maxX = float.NegativeInfinity;
            m_maxY = float.NegativeInfinity;

            int i, j, len2, len = m_substrokes.Count;

            Sketch.Point[] points;
            Sketch.Point point;
            for (i = 0; i < len; ++i)
            {
                points = m_substrokes[i].Points;
                len2 = points.Length;
                for (j = 0; j < len2; ++j)
                {
                    point = points[j];
                    if (point.X < m_minX) m_minX = point.X;
                    if (point.Y < m_minY) m_minY = point.Y;
                    if (point.X > m_maxX) m_maxX = point.X;
                    if (point.Y > m_maxY) m_maxY = point.Y;
                }
            }
        }

        #endregion

        #region GETTERS

		/// <summary>
		/// Iterate through the substrokes
		/// </summary>
		public IEnumerable<Sketch.Substroke> Iter
		{
			get
			{
				foreach (Sketch.Substroke i in m_substrokes)
					yield return i;
			}
		}

        /// <summary>
        /// Get the substrokes
        /// </summary>
        public Sketch.Substroke[] SubStrokes
        {
            get
            {
                return m_substrokes.ToArray();
            }
        }

		/// <summary>
		/// Get a list of the substrokes
		/// </summary>
		public List<Sketch.Substroke> SubStrokesL
		{
			get
			{
				return m_substrokes;
			}
		}

        /// <summary>
        /// Get the weighted center of the Substrokes
        /// </summary>
        public Sketch.Point WeightedCenter
        {
            get
            {
                if (m_weightedCenter == null)
                    computeWeightedCenter();

                return m_weightedCenter;
            }
        }

        /// <summary>
        /// Get the center of the Substrokes (center of bounding box)
        /// </summary>
        public Sketch.Point Center
        {
            get
            {
                if (m_center == null)
                    computeCenter();

                return m_center;
            }
        }

		/// <summary>
		/// Get the number of substrokes
		/// </summary>
        public int Length
        {
            get
            {
                return m_substrokes.Count;
            }
        }

		/// <summary>
		/// Get the minimum X coordinate
		/// </summary>
        public float MinX
        {
            get
            {
                if (!m_minX.HasValue)
                    computeMinMax();

                return m_minX.Value;
            }
        }

		/// <summary>
		/// Get te minimum Y coordinate
		/// </summary>
        public float MinY
        {
            get
            {
                if (!m_minY.HasValue)
                    computeMinMax();

                return m_minY.Value;
            }
        }

		/// <summary>
		/// Get the maximum X coordinate
		/// </summary>
        public float MaxX
        {
            get
            {
                if (!m_maxX.HasValue)
                    computeMinMax();

                return m_maxX.Value;
            }
        }

		/// <summary>
		/// Get the maximum Y coordinate
		/// </summary>
        public float MaxY
        {
            get
            {
                if (!m_maxY.HasValue)
                    computeMinMax();

                return m_maxY.Value;
            }
        }

		/// <summary>
		/// Get the largest distance between the bounding box and the weighted center
		/// </summary>
        public float MaxToWeightedCenter
        {
            get
            {
                if (!m_maxWeightedCenter.HasValue)
                {
                    m_maxWeightedCenter = maxToPoint(WeightedCenter);
                }

                return m_maxWeightedCenter.Value;
            }
        }

		/// <summary>
		/// Get the largest distance between the bounding box and the center
		/// </summary>
        public float MaxToCenter
        {
            get
            {
                if (!m_maxCenter.HasValue)
                {
                    m_maxCenter = maxToPoint(Center);
                }

                return m_maxCenter.Value;
            }
        }

		/// <summary>
		/// Find the maximum distance between a point and the bounding box of these substrokes
		/// </summary>
		/// <param name="p">The point to compare</param>
		/// <returns>The max distance</returns>
        private float maxToPoint(Sketch.Point p)
        {
            return Math.Max(Math.Max(p.X - MinX, MaxX - p.X), Math.Max(p.Y - MinY, MaxY - p.Y));
        }
        

		/// <summary>
		/// Access substrokes in the array
		/// </summary>
		/// <param name="index">The index to look at</param>
		/// <returns>The substroke located at [index]</returns>
        public Sketch.Substroke this[int index]
        {
            get
            {
                return m_substrokes[index];
            }
        }

        #endregion

		/// <summary>
		/// Create a deep copy of this object
		/// </summary>
		/// <returns>The copy</returns>
        public Substrokes Clone()
        {
            int i, len = m_substrokes.Count;
            List<Sketch.Substroke> tempSubstrokes = new List<Sketch.Substroke>(len);
            for(i = 0; i < len; ++i)
                tempSubstrokes.Add(m_substrokes[i].Clone());

            Substrokes clone = new Substrokes();

            clone.m_substrokes = tempSubstrokes;
            
            clone.m_minX = m_minX;
            clone.m_minY = m_minY;
            clone.m_maxX = m_maxX;
            clone.m_maxY = m_maxY;

            
            if(m_weightedCenter != null)
                clone.m_weightedCenter = m_weightedCenter.Clone();

            if (m_center != null)
                clone.m_center = m_center.Clone();

            clone.m_maxCenter = m_maxCenter;
            clone.m_maxWeightedCenter = m_maxWeightedCenter;

            return clone;
        }
    }
}
