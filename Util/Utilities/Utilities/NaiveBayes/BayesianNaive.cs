using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities.NaiveBayes
{
    public class BayesianNaive
    {
        #region Constants and Parameters

        const double MIN_PROBABILITY = 0.0000000001;

        #endregion

        #region Member Variables

        private int m_NumExamples = 0;

        /// <summary>
        /// Distribution of occurances
        /// FeatureName --> ClassName --> FeatureValue --> Count
        /// </summary>
        private Dictionary<string, Dictionary<string, Dictionary<object, int>>> m_Distributions;

        private Dictionary<string, int> m_ClassDistrubition;

        #endregion

        #region Constructors

        public BayesianNaive(List<string> features, List<string> classes)
        {
            m_ClassDistrubition = new Dictionary<string, int>();
            foreach (string cls in classes)
                m_ClassDistrubition.Add(cls, 0);

            m_Distributions = new Dictionary<string, Dictionary<string, Dictionary<object, int>>>();
            foreach (string feature in features)
            {
                m_Distributions.Add(feature, new Dictionary<string, Dictionary<object, int>>());
                foreach (string cls in classes)
                    m_Distributions[feature].Add(cls, new Dictionary<object, int>());
            }
        }

        #endregion

        #region Interface Functions

        public void AddExample(string className, Dictionary<string, object> instance)
        {
            if (!FeatureNamesSame(instance))
                return;

            if (!m_ClassDistrubition.ContainsKey(className))
                return;

            m_NumExamples++;
            m_ClassDistrubition[className]++;

            foreach (string key in instance.Keys)
            {
                if (m_Distributions.ContainsKey(key) && m_Distributions[key].ContainsKey(className))
                {
                    if (m_Distributions[key][className].ContainsKey(instance[key]))
                        m_Distributions[key][className][instance[key]]++;
                    else
                        m_Distributions[key][className].Add(instance[key], 1);
                }
            }
        }

        public Dictionary<string, double> Classify(Dictionary<string, object> instance)
        {
            Dictionary<string, double> probabilities = new Dictionary<string, double>();
            
            // Initialize with class prior probability
            foreach (string cls in m_ClassDistrubition.Keys)
                probabilities.Add(cls, GetProbability(m_ClassDistrubition[cls], m_NumExamples));

            // Go through each feature
            foreach (string feature in instance.Keys)
            {
                Dictionary<string, Dictionary<object, int>> dict;
                bool found = m_Distributions.TryGetValue(feature, out dict);

                Dictionary<string, double> temp = new Dictionary<string, double>();
                foreach (string cls in probabilities.Keys)
                {
                    if (dict.ContainsKey(cls))
                    {
                        double prob = MIN_PROBABILITY;
                        if (dict[cls].ContainsKey(instance[feature]))
                            prob = GetProbability(dict[cls][instance[feature]], m_ClassDistrubition[cls]);
                        prob = Math.Max(prob, MIN_PROBABILITY);
                        temp.Add(cls, probabilities[cls] * prob);
                    }
                }
                foreach (KeyValuePair<string, double> kv in temp)
                    probabilities[kv.Key] = kv.Value;
            }

            Dictionary<string, double> normalized = Normalize(probabilities);
            Dictionary<string, double> results = SortValues(normalized);

            return results;
        }

        #endregion

        #region Helpers

        private Dictionary<string, double> SortValues(Dictionary<string, double> normalized)
        {
            List<double> scores = new List<double>(normalized.Values);

            scores.Sort();
            scores.Reverse();
            Dictionary<string, double> sorted = new Dictionary<string, double>();
            foreach (double score in scores)
            {
                List<string> keys = new List<string>();
                foreach (KeyValuePair<string, double> kv in normalized)
                    if (kv.Value == score)
                        keys.Add(kv.Key);

                foreach (string key in keys)
                {
                    if (!sorted.ContainsKey(key))
                        sorted.Add(key, score);
                }
            }

            return sorted;
        }

        private Dictionary<string, double> Normalize(Dictionary<string, double> probabilities)
        {
            double sum = 0.0;
            foreach (double val in probabilities.Values)
                sum += val;

            Dictionary<string, double> results = new Dictionary<string, double>();
            foreach (string cls in probabilities.Keys)
                results.Add(cls, probabilities[cls] / sum);

            return results;
        }

        private double GetProbability(int p, int total)
        {
            if (total > 0)
                return (double)p / total;
            else
                return 0.0;
        }

        private bool FeatureNamesSame(Dictionary<string, object> instance)
        {
            List<string> fNames = new List<string>(m_Distributions.Keys);
            List<string> features = new List<string>(instance.Keys);

            foreach (string f in features)
                if (!fNames.Contains(f))
                    return false;

            return true;
        }

        #endregion
    }
}
