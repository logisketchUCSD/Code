using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Utilities.Neuro
{
    [Serializable()]

    /// <summary>
    /// Results from testing a neural network
    /// </summary>
    public class NeuralNetworkResults : ISerializable
    {
        #region Member Variables

        /// <summary>
        /// Confusion matrix for classification, each row is
        /// the expected class, each column is the calculated class
        /// </summary>
        private int[][] m_ConfusionMatrix;

        /// <summary>
        /// Information about the network used to train and test
        /// </summary>
        private NeuralNetworkInfo m_NetworkInfo;

        /// <summary>
        /// BitString used for network
        /// </summary>
        private string m_BitString;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for results
        /// </summary>
        /// <param name="results"></param>
        /// <param name="expected"></param>
        /// <param name="info"></param>
        public NeuralNetworkResults(double[][] results, double[][] expected, NeuralNetworkInfo info, string bitString)
        {
            m_NetworkInfo = info;
            m_BitString = bitString;

            int numClasses = expected[0].Length;
            if (numClasses == 1)
                numClasses = 2;

            m_ConfusionMatrix = new int[numClasses][];
            for (int i = 0; i < numClasses; i++)
                m_ConfusionMatrix[i] = new int[numClasses];

            for (int i = 0; i < results.GetLength(0); i++)
            {
                int maxResult = DetermineMax(results[i]);
                int maxExpected = DetermineMax(expected[i]);

                m_ConfusionMatrix[maxExpected][maxResult]++;
            }
        }

        /// <summary>
        /// Constructor for results
        /// </summary>
        /// <param name="results"></param>
        /// <param name="expected"></param>
        /// <param name="info"></param>
        public NeuralNetworkResults(double[][] results, double[][] expected, NeuralNetworkInfo info)
        {
            m_NetworkInfo = info;
            m_BitString = "";

            int numClasses = expected[0].Length;
            if (numClasses == 1)
                numClasses = 2;

            m_ConfusionMatrix = new int[numClasses][];
            for (int i = 0; i < numClasses; i++)
                m_ConfusionMatrix[i] = new int[numClasses];

            for (int i = 0; i < results.GetLength(0); i++)
            {
                int maxResult = DetermineMax(results[i]);
                int maxExpected = DetermineMax(expected[i]);

                m_ConfusionMatrix[maxExpected][maxResult]++;
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Determine which element has the max value.
        /// If there is only one element, determine whether 
        /// it is greater than or less than 0.5
        /// (Expected Range == (0.0-1.0))
        /// </summary>
        /// <param name="values">Array of values to determine max from</param>
        /// <returns>Element number for max value</returns>
        private int DetermineMax(double[] values)
        {
            // Two-Way -- One output number
            if (values.Length == 1)
            {
                if (values[0] > 0.5)
                    return 1;
                else
                    return 0;
            }

            double maxResult = double.NegativeInfinity;

            // More than Two-Way
            foreach (double r in values)
                maxResult = Math.Max(maxResult, r);

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == maxResult)
                    return i;
            }

            return -1;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public NeuralNetworkResults(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            m_ConfusionMatrix = (int[][])info.GetValue("Matrix", typeof(int[][]));
            m_NetworkInfo = (NeuralNetworkInfo)info.GetValue("Info", typeof(NeuralNetworkInfo));
            m_BitString = (string)info.GetValue("BitString", typeof(string));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("Matrix", m_ConfusionMatrix);
            info.AddValue("Info", m_NetworkInfo);
            info.AddValue("BitString", m_BitString);
        }

        #endregion

        #region Getters

        /// <summary>
        /// Get the number of classes for the network
        /// </summary>
        public int NumClasses
        {
            get { return m_ConfusionMatrix[0].Length; }
        }

        /// <summary>
        /// Get the overall accuracy of the network
        /// </summary>
        public double Accuracy
        {
            get
            {
                int numRight = 0;
                int numWrong = 0;

                // Expected Class
                for (int i = 0; i < NumClasses; i++)
                {
                    // Calculated Class
                    for (int j = 0; j < NumClasses; j++)
                    {
                        // Expected class == Calculated class
                        if (i == j)
                            numRight += m_ConfusionMatrix[i][j];
                        else
                            numWrong += m_ConfusionMatrix[i][j];
                    }
                }

                return (double)numRight / ((double)(numRight + numWrong));
            }
        }

        /// <summary>
        /// Get the accuracies for each class
        /// </summary>
        public double[] ClassAccuracy
        {
            get
            {
                int[] numRight = new int[NumClasses];
                int[] numWrong = new int[NumClasses];

                // Expected Class
                for (int i = 0; i < NumClasses; i++)
                {
                    // Calculated Class
                    for (int j = 0; j < NumClasses; j++)
                    {
                        // Expected class == Calculated class
                        if (i == j)
                            numRight[i] += m_ConfusionMatrix[i][j];
                        else
                            numWrong[i] += m_ConfusionMatrix[i][j];
                    }
                }

                double[] accuracies = new double[NumClasses];
                for (int i = 0; i < NumClasses; i++)
                    accuracies[i] = (double)numRight[i] / ((double)(numRight[i] + numWrong[i]));

                return accuracies;
            }
        }

        /// <summary>
        /// Get the complete confusion matrix
        /// </summary>
        public int[][] ConfusionMatrix
        {
            get { return m_ConfusionMatrix; }
        }

        /// <summary>
        /// Get the general network information
        /// </summary>
        public NeuralNetworkInfo NetworkInfo
        {
            get { return m_NetworkInfo; }
        }

        /// <summary>
        /// BitString used to select features
        /// </summary>
        public string BitString
        {
            get { return m_BitString; }
        }

        #endregion
    }
}
