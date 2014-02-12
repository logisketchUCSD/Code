using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Runtime.Serialization;
using Utilities.Neuro.Learning;

namespace Utilities.Neuro
{
    /// <summary>
    /// Contains the network, along with relevant
    /// information and a teacher.
    /// </summary>
    [Serializable()]
    public class NeuralNetwork : ISerializable
    {
        #region Member Variables

        /// <summary>
        /// AForge Neural network
        /// </summary>
        private ActivationNetwork m_Network;

        /// <summary>
        /// General information about the network
        /// </summary>
        private NeuralNetworkInfo m_Info;

        /// <summary>
        /// Supervised Learning method used for the network
        /// </summary>
        private ISupervisedLearning m_Teacher;

        /// <summary>
        /// Flag to know whether the network has been trained already
        /// </summary>
        private bool m_Trained= false;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor which takes in parameters necessary to
        /// create a new network.
        /// </summary>
        /// <param name="layers">Each int is a layer, value of int is #neurons in layer</param>
        /// <param name="numInputs">Number of inputs to the network</param>
        /// <param name="activationFunction">Name of the activation function to use</param>
        /// <param name="info">Network information</param>
        public NeuralNetwork(NeuralNetworkInfo info)
        {
            m_Info = info;

            SetUpNetwork(new ActivationNetwork(info.ActivationFunction, info.NumInputs, info.Layers), info);
            
        }

        /// <summary>
        /// Uses a preconstructed ActivationNetwork to initialize.
        /// </summary>
        /// <param name="network">Existing network</param>
        /// <param name="info">Network Information</param>
        public NeuralNetwork(ActivationNetwork network, NeuralNetworkInfo info)
        {
            m_Info = info;
            SetUpNetwork(network, info);
        }

        /// <summary>
        /// Initializes the member network and the member teacher.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="teacher"></param>
        private void SetUpNetwork(ActivationNetwork network, NeuralNetworkInfo info)
        {
            string teacher = info.Teacher;
            m_Network = network;

            if (teacher == "BackPropagation")
            {
                BackPropagationLearning learner = new BackPropagationLearning(m_Network);
                learner.LearningRate = info.LearningRate;
                learner.Momentum = info.Momentum;
                m_Teacher = learner;
            }
            else if (teacher == "Perceptron")
            {
                PerceptronLearning learner = new PerceptronLearning(m_Network);
                learner.LearningRate = info.LearningRate;
                m_Teacher = learner;
            }
            else if (teacher == "DeltaRule")
            {
                DeltaRuleLearning learner = new DeltaRuleLearning(m_Network);
                learner.LearningRate = info.LearningRate;
                m_Teacher = learner;
            }
            else
            {
                BackPropagationLearning learner = new BackPropagationLearning(m_Network);
                learner.LearningRate = info.LearningRate;
                learner.Momentum = info.Momentum;
                m_Teacher = learner;
            }

            
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public NeuralNetwork(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            m_Info = (NeuralNetworkInfo)info.GetValue("NeuralNetworkInfo", typeof(NeuralNetworkInfo));
            m_Network = (ActivationNetwork)info.GetValue("Network", typeof(ActivationNetwork));
            m_Teacher = (ISupervisedLearning)info.GetValue("Teacher", typeof(ISupervisedLearning));
            m_Trained = (bool)info.GetValue("Trained", typeof(bool));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("NeuralNetworkInfo", m_Info);
            info.AddValue("Network", m_Network);
            info.AddValue("Teacher", m_Teacher);
            info.AddValue("Trained", m_Trained);
        }

        #endregion

        #region Action Functions

        /// <summary>
        /// Train the network using the input data
        /// </summary>
        /// <param name="trainData"></param>
        public void Train(FeatureSet[] trainData)
        {
            int numExamples = trainData.Length;

            // Get the input and output data correctly formatted
            double[][] input = new double[numExamples][];
            double[][] output = new double[numExamples][];
            for (int i = 0; i < numExamples; i++)
            {
                input[i] = trainData[i].Features;
                output[i] = trainData[i].Classes;
            }

            // Set up Error stuff
            double currentError = 0.0;

            // Train the network
            for (int i = 0; i < m_Info.NumTrainingEpochs; i++)
            {
                currentError = m_Teacher.RunEpoch(input, output) / numExamples;
                if (i % 100 == 0)
                {
                    if (!m_Info.NetworkTrainingErrorValues.ContainsKey(i))
                        m_Info.NetworkTrainingErrorValues.Add(i, currentError);
                }
            }
            if (m_Info.NetworkTrainingErrorValues.ContainsKey(m_Info.NumTrainingEpochs))
                m_Info.NetworkTrainingErrorValues.Add(m_Info.NumTrainingEpochs, currentError);

            m_Trained = true;
        }

        /// <summary>
        /// Train the network using the input data
        /// </summary>
        /// <param name="trainData"></param>
        public void TrainBackground(FeatureSet[] trainData, BackgroundWorker worker)
        {
            int numExamples = trainData.Length;

            // Get the input and output data correctly formatted
            double[][] input = new double[numExamples][];
            double[][] output = new double[numExamples][];
            for (int i = 0; i < numExamples; i++)
            {
                input[i] = trainData[i].Features;
                output[i] = trainData[i].Classes;
            }

            // Set up Error stuff
            double currentError = 0.0;

            // Train the network
            for (int i = 0; i < m_Info.NumTrainingEpochs; i++)
            {
                worker.ReportProgress((int)(i * 100 / m_Info.NumTrainingEpochs));
                currentError = m_Teacher.RunEpoch(input, output) / numExamples;
                if (i % 100 == 0)
                {
                    if (!m_Info.NetworkTrainingErrorValues.ContainsKey(i))
                        m_Info.NetworkTrainingErrorValues.Add(i, currentError);
                }
            }
            if (m_Info.NetworkTrainingErrorValues.ContainsKey(m_Info.NumTrainingEpochs))
                m_Info.NetworkTrainingErrorValues.Add(m_Info.NumTrainingEpochs, currentError);

            m_Trained = true;
        }

        /// <summary>
        /// Test the network while NOT knowing the expected classes
        /// </summary>
        /// <param name="testSet">feature values</param>
        /// <param name="success">whether the testSet was evaluated</param>
        /// <returns></returns>
        public double[][] Test(double[][] testSet, out bool success)
        {
            success = false;
            if (!m_Trained)
                return new double[1][];

            double[][] score = new double[testSet.GetLength(0)][];

            for (int i = 0; i < testSet.GetLength(0); i++)
            {
                double[] scores = m_Network.Compute((double[])testSet[i]);
                score[i] = (double[])scores.Clone();
            }

            success = true;
            return score;
        }

        /// <summary>
        /// Compute the raw output vector given a raw input vector
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public double[] Compute(double[] input)
        {
            return m_Network.Compute(input);
        }

        public string Classify(Dictionary<string, double> featureValues)
        {
            double[][] testSet = new double[1][];
            List<double> values = new List<double>(featureValues.Values);
            testSet[0] = values.ToArray();

            bool success;
            double[][] results = Test(testSet, out success);
            if (!success)
                return "N/A";

            double bestR = results[0][0];
            int bestIndex = 0;

            for (int i = 1; i < results[0].Length; i++)
            {
                if (results[0][i] > bestR)
                {
                    bestR = results[0][i];
                    bestIndex = i;
                }
            }
            
            return m_Info.GetClass(bestIndex);
        }

        /// <summary>
        /// Evaluate the network with known class values
        /// </summary>
        /// <param name="testData"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        public NeuralNetworkResults Evaluate(FeatureSet[] testData, string bitString, out bool success)
        {
            success = false;
            double[][] testSet = new double[testData.Length][];
            double[][] expected = new double[testData.Length][];

            for (int i = 0; i < testData.Length; i++)
            {
                testSet[i] = testData[i].Features;
                expected[i] = testData[i].Classes;
            }

            bool s;
            double[][] results = Test(testSet, out s);
            //PrintResultsToFile(results);
            success = s;

            return new NeuralNetworkResults(results, expected, m_Info, bitString);
        }

        private void PrintResultsToFile(double[][] results)
        {
            string filename = "C:\\";
            filename += DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString() + DateTime.Now.Millisecond.ToString() + ".txt";
            System.IO.StreamWriter writer = new System.IO.StreamWriter(filename);

            for (int i = 0; i < results.Length; i++)
                writer.WriteLine("{0}", results[i][0].ToString("#0"));

            writer.Close();
        }

        public void Save(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Create);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }

        #endregion

        #region Getters

        /// <summary>
        /// Gets the network
        /// </summary>
        public ActivationNetwork Network
        {
            get { return m_Network; }
        }

        /// <summary>
        /// Gets the information relevant to the network
        /// </summary>
        public NeuralNetworkInfo Info
        {
            get { return m_Info; }
            set { m_Info = value; }
        }

        /// <summary>
        /// Is the network trained?
        /// </summary>
        public bool Trained
        {
            get { return m_Trained; }
            set { m_Trained = value; }
        }

        #endregion

        public static NeuralNetwork LoadNetworkFromFile(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            NeuralNetwork network = (NeuralNetwork)bformatter.Deserialize(stream);
            stream.Close();

            return network;
        }

        public static NeuralNetwork CreateNetworkFromWekaOutput(string filename)
        {
            System.IO.StreamReader reader = new System.IO.StreamReader(filename);

            List<string> attributes = new List<string>();
            Dictionary<string, Dictionary<string, object>> weights = 
                new Dictionary<string, Dictionary<string, object>>();

            string line;
            bool atAttributes = false;
            bool atClassifierModel = false;
            bool atRunInformation = false;
            string currentNode = "";
            string currentClass = "";

            #region Read file

            while ((line = reader.ReadLine()) != null)
            {
                #region set markers for where we are in file
                if (line.Contains("=== Run information"))
                {
                    atRunInformation = true;
                    atClassifierModel = false;
                }
                else if (line.Contains("=== Classifier model"))
                {
                    atRunInformation = false;
                    atClassifierModel = true;
                }
                else if (line.Contains("=== Predictions on test data"))
                {
                    atRunInformation = false;
                    atClassifierModel = false;
                }
                else if (line.Contains("=== Confusion Matrix"))
                {
                    atRunInformation = false;
                    atClassifierModel = false;
                }

                if (atRunInformation && line.Contains("Attributes:"))
                {
                    atAttributes = true;
                    continue;
                }

                #endregion

                if (atAttributes)
                {
                    string name = line.Trim();
                    if (name == "class")
                        atAttributes = false;
                    else
                        attributes.Add(name);

                    continue;
                }

                if (atClassifierModel && line.StartsWith("Sigmoid Node"))
                {
                    currentNode = line.Substring(line.IndexOf("Node"));
                    weights.Add(currentNode, new Dictionary<string, object>());
                    continue;
                }
                else if (atClassifierModel && line.StartsWith("Class"))
                {
                    currentClass = line.Substring(line.IndexOf("Class"));
                    if (currentNode != "Class")
                    {
                        currentNode = "Class";
                        weights.Add(currentNode, new Dictionary<string, object>());
                    }
                    continue;
                }

                if (atClassifierModel && currentNode != "" && currentNode != "Class")
                {
                    if (line.Contains("Threshold"))
                    {
                        string value = line.Substring(line.IndexOf("Threshold") + 13);
                        double num;
                        bool success = double.TryParse(value, out num);
                        if (success)
                            weights[currentNode].Add("Threshold", (object)num);
                    }
                    else if (line.Contains("Inputs"))
                    {
                    }
                    else if (line.Contains("Attrib"))
                    {
                        int index = line.LastIndexOf(" ");
                        int index2 = line.IndexOf("Attrib") + 7;
                        string name = line.Substring(index2, index - index2);
                        name = name.Trim();
                        string value = line.Substring(index);
                        double num;
                        bool success = double.TryParse(value, out num);
                        if (success)
                            weights[currentNode].Add(name, (object)num);
                    }
                    else if (line.Contains("Node"))
                    {
                        int index = line.LastIndexOf(" ");
                        int index2 = line.IndexOf("Node");
                        string name = line.Substring(index2, index - index2);
                        name = name.Trim();
                        string value = line.Substring(index);
                        double num;
                        bool success = double.TryParse(value, out num);
                        if (success)
                            weights[currentNode].Add(name, (object)num);
                    }
                    continue;
                }
                else if (atClassifierModel && currentNode == "Class")
                {
                    if (line.Contains("Node"))
                    {
                        string lShort = line.Trim();
                        weights[currentNode].Add(currentClass, (object)lShort);
                    }
                    continue;
                }
            }

            reader.Close();

            #endregion

            #region Generate Network

            NeuralNetworkInfo info = new NeuralNetworkInfo();
            info.FeatureNames = new List<string>(attributes);
            
            Dictionary<int, List<string>> layerStructure = new Dictionary<int, List<string>>();
            int currentLayer = 0;
            Dictionary<int, List<string>> children = new Dictionary<int, List<string>>();

            foreach (KeyValuePair<string, Dictionary<string, object>> kvp in weights)
            {
                string nodeName = kvp.Key;
                if (nodeName == "Class")
                    continue;

                // Find the current layer for this nodeName
                int childLayer = -1;
                foreach (string node in kvp.Value.Keys)
                {
                    bool found = false;
                    foreach (KeyValuePair<int, List<string>> kvp2 in children)
                    {
                        if (kvp2.Value.Contains(node))
                        {
                            childLayer = kvp2.Key;
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }
                if (childLayer != -1)
                    currentLayer = childLayer;
                else
                {
                    bool found = false;
                    foreach (KeyValuePair<int, List<string>> kvp2 in children)
                    {
                        if (kvp2.Value.Contains(nodeName))
                        {
                            currentLayer = kvp2.Key + 1;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        int highest = -1;
                        foreach (int num in layerStructure.Keys)
                            highest = Math.Max(highest, num);

                        currentLayer = highest + 1;
                    }
                }

                if (!layerStructure.ContainsKey(currentLayer))
                    layerStructure.Add(currentLayer, new List<string>());

                if (!layerStructure[currentLayer].Contains(nodeName))
                    layerStructure[currentLayer].Add(nodeName);

                foreach (string node in kvp.Value.Keys)
                {
                    if (node.Contains("Threshold"))
                        continue;
                    else if (!node.Contains("Node"))
                    {

                        if (!children.ContainsKey(currentLayer))
                            children.Add(currentLayer, new List<string>());

                        if (!children[currentLayer].Contains(node))
                            children[currentLayer].Add(node);

                        continue;
                    }

                    if (!layerStructure.ContainsKey(currentLayer + 1))
                        layerStructure.Add(currentLayer + 1, new List<string>());

                    if (!layerStructure[currentLayer + 1].Contains(node))
                        layerStructure[currentLayer + 1].Add(node);

                    if (!children.ContainsKey(currentLayer))
                        children.Add(currentLayer, new List<string>());

                    if (!children[currentLayer].Contains(node))
                        children[currentLayer].Add(node);
                }
            }

            List<int> layers = new List<int>();
            foreach (KeyValuePair<int, List<string>> kvp in layerStructure)
            {
                layers.Add(kvp.Value.Count);
            }
            layers.Reverse();

            info.Layers = layers.ToArray();
            info.NumInputs = attributes.Count;

            NeuralNetwork network = new NeuralNetwork(info);

            for (int i = 0; i < layers.Count; i++)
            {
                int index = layers.Count - 1 - i;
                for (int j = 0; j < layers[index]; j++)
                {
                    int k = 0;
                    foreach (KeyValuePair<string, object> kvp in weights[layerStructure[i][j]])
                    {
                        if (kvp.Key == "Threshold")
                            network.m_Network[index][j].Threshold = (double)kvp.Value;
                        else
                        {
                            network.m_Network[index][j][k] = (double)kvp.Value;
                            k++;
                        }
                    }
                }
            }

            network.m_Trained = true;

            #endregion

            return network;
        }
    }
}
