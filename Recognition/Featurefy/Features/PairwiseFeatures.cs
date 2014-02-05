using System;
using System.Collections.Generic;
using System.Text;

namespace Featurefy
{
    /// <summary>
    /// Minimum distance feature class
    /// </summary>
    [Serializable]
    public class MinimumDistance : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        public MinimumDistance(double distance)
            : base("Minimum Distance", distance, Scope.Pair_Static)
        { }
    }

    /// <summary>
    /// Maximum distance feature class
    /// </summary>
    [Serializable]
    public class MaximumDistance : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        public MaximumDistance(double distance)
            : base("Maximum Distance", distance, Scope.Pair_Static)
        { }
    }

    /// <summary>
    /// Centroid distance feature class
    /// </summary>
    [Serializable]
    public class CentroidDistance : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        public CentroidDistance(double distance)
            : base("Centroid Distance", distance, Scope.Pair_Static)
        { }
    }

    /// <summary>
    /// Minimum endpoint to endpoint distance feature class
    /// </summary>
    [Serializable]
    public class MinEndpointToEndpointDistance : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        public MinEndpointToEndpointDistance(double distance)
            : base("Minimum Endpoint to Endpoint Distance", distance, Scope.Pair_Static)
        { }
    }

    /// <summary>
    /// Minimum endpoint to any point distance feature class
    /// </summary>
    [Serializable]
    public class MinEndpointToAnyPointDistance : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        public MinEndpointToAnyPointDistance(double distance)
            : base("Minimum Endpoint to Any Point Distance", distance, Scope.Pair_Static)
        { }
    }

    /// <summary>
    /// Ratio of min endpoint to endpoint distance feature class
    /// </summary>
    [Serializable]
    public class RatioMinEndpointToEndpointDistance : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distanceRatio"></param>
        public RatioMinEndpointToEndpointDistance(double distanceRatio)
            : base("Ratio Minimum Endpoint to Endpoint Distance to Overall Minimum Distance", distanceRatio, Scope.Pair_Static)
        {
            m_Normalizer = 1.0;
        }
    }

    /// <summary>
    /// Ratio of min endpoint to any point distance feature class
    /// </summary>
    [Serializable]
    public class RatioMinEndpointToAnyPointDistance : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distanceRatio"></param>
        public RatioMinEndpointToAnyPointDistance(double distanceRatio)
            : base("Ratio Minimum Endpoint to Any Point Distance to Overall Minimum Distance", distanceRatio, Scope.Pair_Static)
        {
            m_Normalizer = 1.0;
        }
    }

    /// <summary>
    /// Same closed path feature class
    /// </summary>
    [Serializable]
    public class SameClosedPath : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isTrue"></param>
        public SameClosedPath(double isTrue)
            : base("Part of Same Closed Path", isTrue, Scope.Pair_Static)
        { }
    }

    /// <summary>
    /// X overlap feature class
    /// </summary>
    [Serializable]
    public class XOverlap : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        public XOverlap(double distance)
            : base("X-Overlap", distance, Scope.Pair_Static)
        { }
    }

    /// <summary>
    /// Y overlap feature class
    /// </summary>
    [Serializable]
    public class YOverlap : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        public YOverlap(double distance)
            : base("Y-Overlap", distance, Scope.Pair_Static)
        { }
    }

    /// <summary>
    /// Time gap feature class
    /// </summary>
    [Serializable]
    public class TimeGap : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        public TimeGap(double distance)
            : base("Time Gap", distance, Scope.Pair_Static)
        {
            m_Normalizer = 1.0;
        }
    }

    /// <summary>
    /// Distance ratio A feature class
    /// </summary>
    [Serializable]
    public class DistanceRatioA : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distanceRatio"></param>
        public DistanceRatioA(double distanceRatio)
            : base("Distance Ratio A", distanceRatio, Scope.Pair_Dynamic)
        {
            m_Normalizer = 1.0;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="MinDistance"></param>
        public DistanceRatioA(double distance, double MinDistance)
            : base("Distance Ratio A", distance, Scope.Pair_Dynamic)
        {
            double denominator = (1.0 + distance / Compute.PairwiseDistanceFactor);
            double numerator = (1.0 + MinDistance / Compute.PairwiseDistanceFactor);
            m_Value = numerator / denominator;

            m_Normalizer = 1.0;
        }
    }

    /// <summary>
    /// Distance Ratio B feature class
    /// </summary>
    [Serializable]
    public class DistanceRatioB : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distanceRatio"></param>
        public DistanceRatioB(double distanceRatio)
            : base("Distance Ratio B", distanceRatio, Scope.Pair_Dynamic)
        {
            m_Normalizer = 1.0;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="MinDistance"></param>
        public DistanceRatioB(double distance, double MinDistance)
            : base("Distance Ratio B", distance, Scope.Pair_Dynamic)
        {
            double denominator = (1.0 + distance / Compute.PairwiseDistanceFactor);
            double numerator = (1.0 + MinDistance / Compute.PairwiseDistanceFactor);
            m_Value = numerator / denominator;

            m_Normalizer = 1.0;
        }
    }
}
