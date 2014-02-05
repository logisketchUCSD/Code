using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sketch;

namespace Featurefy
{
    /// <summary>
    /// Min/Max/Avg distance between a pair of substrokes
    /// </summary>
    [Serializable]
    class SubstrokeDistance
    {
        #region Member Variables

        double m_Min, m_Max, m_Avg, m_Endpoint, m_EndpointToAnyPoint;
        ulong m_Time;
        Substroke m_StrokeA, m_StrokeB;

        #endregion

        #region Constructors

        public SubstrokeDistance(Substroke strokeA, Substroke strokeB)
        {
            m_StrokeA = strokeA;
            m_StrokeB = strokeB;

            ComputeAll();
        }

        #endregion

        #region Methods

        private void ComputeAll()
        {
            double max2 = Double.NegativeInfinity;
            double min2 = Double.PositiveInfinity;
            double min2EtoA = Double.PositiveInfinity;
            m_Avg = Compute.EuclideanDistance(m_StrokeA.Centroid, m_StrokeB.Centroid);
            List<Point> pointsA = m_StrokeA.PointsL;
            List<Point> pointsB = m_StrokeB.PointsL;
            for (int i = 0; i < pointsA.Count; i++)
            {
                Point lhsp = pointsA[i];
                for (int j = 0; j < pointsB.Count; j++)
                {
                    Point rhsp = pointsB[j];

                    double d = Compute.EuclideanDistanceSquared(lhsp, rhsp);
                    max2 = Math.Max(max2, d);
                    min2 = Math.Min(min2, d);

                    if (i == 0 || j == 0 || i == pointsA.Count || j == pointsB.Count)
                        min2EtoA = Math.Min(min2EtoA, d);
                }
            }

            m_Max = Math.Sqrt(max2);
            m_Min = Math.Sqrt(min2);
            m_EndpointToAnyPoint = Math.Sqrt(min2EtoA);
            m_Endpoint = MinDistBetweenEndpoints(m_StrokeA, m_StrokeB);

            if (pointsA[0].Time > pointsB[0].Time)
                m_Time = pointsA[0].Time - pointsB[pointsB.Count - 1].Time;
            else
                m_Time = pointsB[0].Time - pointsA[pointsA.Count - 1].Time;
        }

        private double MinDistBetweenEndpoints(Substroke first, Substroke subsequent)
        {
            double d00 = first.Endpoints[0].distance(subsequent.Endpoints[0]);
            double d01 = first.Endpoints[0].distance(subsequent.Endpoints[1]);
            double d10 = first.Endpoints[1].distance(subsequent.Endpoints[0]);
            double d11 = first.Endpoints[1].distance(subsequent.Endpoints[1]);

            double min1 = Math.Min(d00, d01);
            double min2 = Math.Min(d10, d11);

            return Math.Min(min1, min2);
        }

        #endregion

        #region Getters

        public double Min
        {
            get { return m_Min; }
        }

        public double Max
        {
            get { return m_Max; }
        }

        public double Avg
        {
            get { return m_Avg; }
        }

        public double Endpoint
        {
            get { return m_Endpoint; }
        }

        public double EndpointToAnyPoint
        {
            get { return m_EndpointToAnyPoint; }
        }

        public ulong Time
        {
            get { return m_Time; }
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
