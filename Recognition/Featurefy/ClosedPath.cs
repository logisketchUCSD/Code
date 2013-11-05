using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Featurefy
{

    /// <summary>
    /// Used for storing a set of strokes that form a closed path. Also
    /// responsible for computing the bounding box of those strokes.
    /// </summary>
    public class ClosedPath
    {

        #region Internals

        /// <summary>
        /// The strokes in this closed path
        /// </summary>
        private HashSet<Substroke> strokes;

        /// <summary>
        /// The bounding box of this closed path. Computed as-needed.
        /// </summary>
        private Lazy<System.Drawing.Rectangle> boundingBox;

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a closed path from a set of substrokes.
        /// </summary>
        /// <param name="substrokes"></param>
        public ClosedPath(IEnumerable<Substroke> substrokes)
        {
            strokes = new HashSet<Substroke>(substrokes);
            recomputeBoundingBox();
        }

        /// <summary>
        /// Construct a new closed path from a single substroke.
        /// </summary>
        /// <param name="s"></param>
        public ClosedPath(Substroke s)
            :this(new Substroke[] { s })
        {
        }

        /// <summary>
        /// Construct a closed path containing no substrokes.
        /// Note that the bounding box for this closed path will
        /// be meaningless until strokes are added.
        /// </summary>
        public ClosedPath()
            :this(new Substroke[] {})
        {
        }

        #endregion

        #region Bounding Box Computation

        private void recomputeBoundingBox()
        {
            boundingBox = new Lazy<System.Drawing.Rectangle>(GetBoundingBox);
        }

        private System.Drawing.Rectangle GetBoundingBox()
        {
            float maxX = float.MinValue;
            float minX = float.MaxValue;
            float maxY = float.MinValue;
            float minY = float.MaxValue;

            foreach (Substroke stroke in strokes)
            {
                Point[] points = stroke.Points;
                for (int i = 1; i < points.Length; i++)
                {
                    maxX = Math.Max(points[i].X, maxX);
                    minX = Math.Min(points[i].X, minX);
                    maxY = Math.Max(points[i].Y, maxY);
                    minY = Math.Min(points[i].Y, minY);
                }
            }
            return new System.Drawing.Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }

        #endregion

        #region Comparison

        /// <summary>
        /// Two closed paths are equal if strokes inside each closed path match.
        /// </summary>
        /// <param name="path">the path to compare to</param>
        /// <returns>true if the two are equal</returns>
        public bool Equals(ClosedPath path)
        {
            if (path.Substrokes.Count != Substrokes.Count)
                return false;

            foreach (Substroke s1 in Substrokes)
                if (!path.Substrokes.Contains(s1))
                    return false;

            return true;
        }

        #endregion

        #region Adding Subtrokes

        /// <summary>
        /// Add a substroke to this closed path. The bounding
        /// box will be recomputed.
        /// </summary>
        /// <param name="s"></param>
        public void AddSubtroke(Substroke s)
        {
            strokes.Add(s);
            recomputeBoundingBox();
        }

        #endregion

        #region Getters & Setters

        /// <summary>
        /// Get the substrokes in this closed path.
        /// </summary>
        public HashSet<Substroke> Substrokes
        {
            get { return strokes; }
        }

        /// <summary>
        /// Get the bounding box of this closed path.
        /// </summary>
        public System.Drawing.Rectangle BoundingBox
        {
            get { return boundingBox.Value; }
        }

        #endregion

    }
}
