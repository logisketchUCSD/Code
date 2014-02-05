using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Sketch;

namespace Featurefy
{
    /// <summary>
    /// Intersection Pair class
    /// </summary>
    [Serializable]
    public class IntersectionPair
    {
        #region Member Variables

        /// <summary>
        /// First Substroke
        /// </summary>
        private Substroke m_SubstrokeA;

        private List<Line> m_LinesA;

        private RectangleF m_BoxA;

        /// <summary>
        /// Second Substroke
        /// </summary>
        private Substroke m_SubstrokeB;

        private List<Line> m_LinesB;

        private RectangleF m_BoxB;

        /// <summary>
        /// List of intersections for two strokes
        /// </summary>
        private List<Intersection> m_Intersections;

        /// <summary>
        /// Unique Identification number for the intersection pair
        /// </summary>
        private Guid m_Id;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor which creates an intersection pair between 2 strokes
        /// and then populates the list of intersections between the strokes
        /// </summary>
        /// <param name="ssA">First Substroke</param>
        /// <param name="ssB">Second Substroke</param>
        /// <param name="boxA"></param>
        /// <param name="boxB"></param>
        /// <param name="linesA"></param>
        /// <param name="linesB"></param>
        public IntersectionPair(Substroke ssA, Substroke ssB, RectangleF boxA, RectangleF boxB, List<Line> linesA, List<Line> linesB)
        {
            m_Id = Guid.NewGuid();
            m_SubstrokeA = ssA;
            m_BoxA = boxA;
            m_LinesA = linesA;
            m_SubstrokeB = ssB;
            m_BoxB = boxB;
            m_LinesB = linesB;
            m_Intersections = Compute.Intersect(m_SubstrokeA, m_SubstrokeB, m_LinesA, m_LinesB, m_BoxA, m_BoxB, 0.0f);
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// Get the list of intersections
        /// </summary>
        public List<Intersection> Intersections
        {
            get { return m_Intersections; }
        }

        /// <summary>
        /// Get the first stroke in this pair
        /// </summary>
        public Sketch.Substroke StrokeA
        {
            get { return m_SubstrokeA; }
        }

        /// <summary>
        /// Get the second stroke in this pair
        /// </summary>
        public Sketch.Substroke StrokeB
        {
            get { return m_SubstrokeB; }
        }

        /// <summary>
        /// Get the ID of this pair
        /// </summary>
        public Guid Id
        {
            get { return m_Id; }
        }

        /// <summary>
        /// True if there are no intersections
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (m_Intersections.Count == 0)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// True iff this pair contains the substroke associated with the substroke ID
        /// </summary>
        /// <param name="substrokeID"></param>
        /// <returns></returns>
        public bool Contains(Guid substrokeID)
        {
            if (m_SubstrokeA.Id == substrokeID || m_SubstrokeB.Id == substrokeID)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Get the list of end point intersections
        /// </summary>
        public List<Intersection> EndPtIntersections
        {
            get
            {
                List<Intersection> list = new List<Intersection>();

                foreach (Intersection intersection in m_Intersections)
                {
                    float[] ptsIntersection = intersection.IntersectionPoints;
                    if (IsEndPtIntersection(ptsIntersection))
                        list.Add(intersection);
                }

                return list;
            }
        }

        /// <summary>
        /// Returns true if the first two intersection points are endpoints
        /// </summary>
        /// <param name="intersectionPts"></param>
        /// <returns></returns>
        private bool IsEndPtIntersection(float[] intersectionPts)
        {
            if (IsEndPt(intersectionPts[0]) && IsEndPt(intersectionPts[1]))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns true if the intersection point is an endpoint
        /// </summary>
        /// <param name="intersectionPt"></param>
        /// <returns></returns>
        private bool IsEndPt(float intersectionPt)
        {
            float threshold = Compute.THRESHOLD;
            if (intersectionPt > 0.0f - threshold && intersectionPt < 0.0f + threshold ||
                intersectionPt > 1.0f - threshold && intersectionPt < 1.0f + threshold)
                return true;
            else
                return false;
        }

        #endregion
    }
}
