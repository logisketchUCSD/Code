using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.IO;

namespace Utilities
{
    [Serializable()]
    public class FeatureSet : ISerializable
    {
        #region Member Variables

        /// <summary>
        /// Array of features
        /// </summary>
        private double[] m_FeatureValues;

        /// <summary>
        /// Array containing class (expected)
        /// </summary>
        private double[] m_ClassValues;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="features"></param>
        /// <param name="classes"></param>
        public FeatureSet(double[] features, double[] classes)
        {
            m_FeatureValues = features;
            m_ClassValues = classes;
        }


        public FeatureSet(double[] features, double[] classes, double[][] normalizationFactors)
        {
            m_ClassValues = classes;
            m_FeatureValues = new double[features.Length];
            for (int i = 0; i < features.Length; i++)
                m_FeatureValues[i] = NormalizeSoftMaxLogistic(features[i], normalizationFactors[i]);
        }

        #endregion

        #region Static


        /// <summary>
        /// Normalize a value using a Soft-Max Logistic function
        /// factors[0] is the mean value
        /// factors[1] is the standard deviation
        /// factors[2] is the lambda value - which indicates how wide the linear range is
        /// </summary>
        /// <param name="value"></param>
        /// <param name="factors"></param>
        /// <returns></returns>
        static public double NormalizeSoftMaxLogistic(double value, double[] factors)
        {
            double mean = factors[0];
            double stdDev = factors[1];
            double lambda = factors[2];
            double x_prime = (value - mean);
            // This is the standard softmax denominator
            double denominator = stdDev;
            // The following denominator was used with a modified softmax. 
            //denominator = lambda * (stdDev / (2.0 * Math.PI));

            x_prime = x_prime / denominator;

            return 1.0 / (1.0 + Math.Exp(-x_prime));
        }

        public static string AddFeatureSetsFromFile(string filename, ref List<FeatureSet> featureSets)
        {
            try
            {
                FeatureSet[] features = GetFeatureSetsFromFile(filename);
                foreach (FeatureSet set in features)
                    featureSets.Add(set);

                return "";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static FeatureSet[] GetFeatureSetsFromFile(string filename)
        {
            List<FeatureSet> featureSets = new List<FeatureSet>(100);

            StreamReader reader = new StreamReader(filename);

            string line = reader.ReadLine();
            int numFeatures = Convert.ToInt32(line);

            line = reader.ReadLine();
            int numClasses = Convert.ToInt32(line);

            while ((line = reader.ReadLine()) != null && line != "")
            {
                string[] words;
                if (line.Contains(","))
                    words = line.Split(",".ToCharArray());
                else
                    words = line.Split(" ".ToCharArray());

                if (words.Length != numFeatures + numClasses)
                    break;

                double[] features = new double[numFeatures];
                double[] classes = new double[numClasses];

                for (int i = 0; i < numFeatures && i < words.Length; i++)
                    features[i] = Convert.ToDouble(words[i]);

                for (int i = numFeatures; i < numFeatures + numClasses && i < words.Length; i++)
                    classes[i - numFeatures] = Convert.ToDouble(words[i]);

                FeatureSet set = new FeatureSet(features, classes);

                featureSets.Add(set);
            }

            reader.Close();

            return featureSets.ToArray();
        }

        #endregion

        #region Getters

        /// <summary>
        /// Array of features
        /// </summary>
        public double[] Features
        {
            get { return m_FeatureValues; }
        }

        /// <summary>
        /// Array containing class (expected)
        /// </summary>
        public double[] Classes
        {
            get { return m_ClassValues; }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public FeatureSet(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            m_FeatureValues = (double[])info.GetValue("features", typeof(double[]));
            m_ClassValues = (double[])info.GetValue("classes", typeof(double[]));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("features", m_FeatureValues);
            info.AddValue("classes", m_ClassValues);
        }

        #endregion
    }
}
