using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using Utilities;

namespace Featurefy
{
    /// <summary>
    /// Feature Stroke Pair class
    /// </summary>
    [Serializable]
    public class FeatureStrokePair : StrokePair
    {

        #region Member Variables and Private Structs

        /// <summary>
        /// A list of the calculated features, linked to string values indicating
        /// what feature it is.
        /// </summary>
        private Dictionary<string, Feature> m_Features;

        /// <summary>
        /// Passed in with constructor, tells the object what features to use
        /// </summary>
        private Dictionary<string, bool> m_FeaturesToUse;

        /// <summary>
        /// All relevant distance between the two strokes
        /// </summary>
        private SubstrokeDistance m_Distances;

        /// <summary>
        /// All relevant overlaps between the two strokes
        /// </summary>
        private SubstrokeOverlap m_Overlaps;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pair">the pair of strokes to use</param>
        /// <param name="featureList">the list of enabled features</param>
        public FeatureStrokePair(StrokePair pair, Dictionary<string, bool> featureList)
            :base(pair.Item1, pair.Item2)
        {
            m_Distances = new SubstrokeDistance(Item1, Item2);
            m_Overlaps = new SubstrokeOverlap(Item1, Item2);

            m_FeaturesToUse = featureList;
            m_Features = new Dictionary<string, Feature>();

            AssignFeatureValues();
        }

        #endregion

        #region Methods

        private void AssignFeatureValues()
        {
            foreach (KeyValuePair<string, bool> pair in m_FeaturesToUse)
            {
                if (pair.Value)
                {
                    string key = pair.Key;
                    switch (key)
                    {
                        case "Minimum Distance":
                            MinimumDistance value = new MinimumDistance(m_Distances.Min);
                            m_Features.Add(key, value);
                            break;
                        case "Maximum Distance":
                            m_Features.Add(key, new MaximumDistance(m_Distances.Max));
                            break;
                        case "Centroid Distance":
                            m_Features.Add(key, new CentroidDistance(m_Distances.Avg));
                            break;
                        case "Horizontal Overlap":
                            m_Features.Add(key, new XOverlap(m_Overlaps.xOverlap));
                            break;
                        case "Vertical Overlap":
                            m_Features.Add(key, new YOverlap(m_Overlaps.yOverlap));
                            break;
                        case "Time Gap":
                            m_Features.Add(key, new TimeGap(m_Distances.Time));
                            break;
                        case "Distance Ratio A":
                            //m_Features.Add(key, new DistanceRatioA());
                            break;
                        case "Distance Ratio B":
                            //m_Features.Add(key, new DistanceRatioB());
                            break;
                        case "Minimum Endpoint to Endpoint Distance":
                            m_Features.Add(key, new MinEndpointToEndpointDistance(m_Distances.Endpoint));
                            break;
                        case "Minimum Endpoint to Any Point Distance":
                            m_Features.Add(key, new MinEndpointToAnyPointDistance(m_Distances.EndpointToAnyPoint));
                            break;
                        case "Ratio Minimum Endpoint to Endpoint Distance to Overall Minimum Distance":
                            m_Features.Add(key, new RatioMinEndpointToEndpointDistance(Compute.GetDistanceRatio(m_Distances.Endpoint, m_Distances.Min)));
                            break;
                        case "Ratio Minimum Endpoint to Any Point Distance to Overall Minimum Distance":
                            m_Features.Add(key, new RatioMinEndpointToAnyPointDistance(Compute.GetDistanceRatio(m_Distances.EndpointToAnyPoint, m_Distances.Min)));
                            break;
                        case "Part of Same Closed Path":
                            // FIX need some way to know if they are in the same closed path
                            // Or should we assume it is 0, then change if 1 when we get pairwise values??
                            m_Features.Add(key, new SameClosedPath(0.0));
                            break;
                        default:
                            break;
                    }
                    if (key == "")
                        m_Features.Add(pair.Key, new Feature());
                }
            }
        }

        private int FeatureCount
        {
            get
            {
                int count = 0;
                foreach (KeyValuePair<string, bool> pair in m_FeaturesToUse)
                {
                    if (pair.Value)
                        count++;
                }

                return count;
            }
        }


        #endregion

        #region Getters

        /// <summary>
        /// Get or set the dictionary of features 
        /// </summary>
        public Dictionary<string, Feature> Features
        {
            get { return m_Features; }
            set { m_Features = value; }
        }

        /// <summary>
        /// Gets the distance between the two substrokes
        /// </summary>
        internal SubstrokeDistance SubstrokeDistance
        {
            get { return m_Distances; }
        }

        /// <summary>
        /// Gets the horizontal and vertical overlaps of the substrokes
        /// </summary>
        internal SubstrokeOverlap SubstrokeOverlap
        {
            get { return m_Overlaps; }
        }

        /// <summary>
        /// Gets the values of the feature pair
        /// </summary>
        /// <param name="minDistanceA"></param>
        /// <param name="minDistanceB"></param>
        /// <returns></returns>
        public double[] Values(double minDistanceA, double minDistanceB)
        {
            int count = FeatureCount;
            double[] values = new double[count];

            int n = 0;
            string key = "Minimum Distance";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Distances.Min / Compute.PairwiseDistanceFactor;
            n++;

            key = "Time Gap";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = (double)m_Distances.Time / Compute.PairwiseTimeFactor;
            n++;

            key = "Horizontal Overlap";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Overlaps.xOverlap / Compute.PairwiseDistanceFactor;
            n++;

            key = "Vertical Overlap";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Overlaps.yOverlap / Compute.PairwiseDistanceFactor;
            n++;

            key = "Maximum Distance";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Distances.Max / Compute.PairwiseDistanceFactor;
            n++;

            key = "Centroid Distance";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Distances.Avg / Compute.PairwiseDistanceFactor;
            n++;

            key = "RatioA";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
            {
                double denominator = (1.0 + m_Distances.Min / Compute.PairwiseDistanceFactor);
                double numerator = (1.0 + minDistanceA / Compute.PairwiseDistanceFactor);
                double ratio = numerator / denominator;
                values[n] = ratio;
            }
            n++;

            key = "RatioB";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
            {
                double denominator = (1.0 + m_Distances.Min / Compute.PairwiseDistanceFactor);
                double numerator = (1.0 + minDistanceB/ Compute.PairwiseDistanceFactor);
                double ratio = numerator / denominator;
                values[n] = ratio;
            }

            return values;
        }

        #endregion

    }


}
