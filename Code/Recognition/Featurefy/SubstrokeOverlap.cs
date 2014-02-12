using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sketch;

namespace Featurefy
{
    /// <summary>
    /// Horizontal (x) and vertical (y) overlaps between a pair of substrokes
    /// </summary>
    [Serializable]
    class SubstrokeOverlap
    {
        #region Member Variables

        Substroke m_StrokeA, m_StrokeB;
        double m_xOverlap, m_yOverlap;

        #endregion

        #region Constructors

        public SubstrokeOverlap(Substroke strokeA, Substroke strokeB)
        {
            m_StrokeA = strokeA;
            m_StrokeB = strokeB;

            ComputeAll();
        }

        #endregion

        #region Methods

        private void ComputeAll()
        {
            System.Drawing.RectangleF boxA = Compute.BoundingBox(m_StrokeA.Points);
            System.Drawing.RectangleF boxB = Compute.BoundingBox(m_StrokeB.Points);

            bool aLeft, aRight, aTop, aBottom;
            aLeft = aRight = aTop = aBottom = false;

            if (boxA.X <= boxB.X)
                aLeft = true;
            if (boxA.X + boxA.Width >= boxB.X + boxB.Width)
                aRight = true;
            if (boxA.Y <= boxB.Y)
                aTop = true;
            if (boxA.Y + boxA.Height >= boxB.Y + boxB.Height)
                aBottom = true;

            if (aLeft && aRight) // 'b' enclosed in 'a' (horizontal)
                m_xOverlap = (double)boxB.Width;
            else if (!aLeft && !aRight) // 'a' enclosed in 'b' (horizontal)
                m_xOverlap = (double)boxA.Width;
            else if (aLeft) // 'a' left of 'b'
                m_xOverlap = (double)(boxA.X + boxA.Width - boxB.X);
            else // 'b' left of 'a'
                m_xOverlap = (double)(boxB.X + boxB.Width - boxA.X);

            if (aTop && aBottom)
                m_yOverlap = (double)boxB.Height;
            else if (!aTop && !aBottom)
                m_yOverlap = (double)boxA.Height;
            else if (aTop)
                m_yOverlap = (double)(boxA.Y + boxA.Height - boxB.Y);
            else
                m_yOverlap = (double)(boxB.Y + boxB.Height - boxA.Y);
        }

        #endregion

        #region Getters

        public double xOverlap
        {
            get { return m_xOverlap; }
        }

        public double yOverlap
        {
            get { return m_yOverlap; }
        }

        public Substroke StrokeA
        {
            get { return m_StrokeA; }
        }

        public Substroke StrokeB
        {
            get { return m_StrokeB; }
        }

        #endregion
    }
}
