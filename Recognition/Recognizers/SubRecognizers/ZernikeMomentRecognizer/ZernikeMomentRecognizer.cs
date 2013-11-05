using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using Utilities.NaiveBayes;
using Utilities.Neuro;
using Utilities;

namespace SubRecognizer
{
    [Serializable]
    public class ZernikeMomentRecognizer : ISerializable
    {
        NeuralNetwork _network;

        public ZernikeMomentRecognizer(NeuralNetwork network)
        {
            _network = network;
        }

        public ZernikeMomentRecognizer()
        {
            _network = new NeuralNetwork(new NeuralNetworkInfo());
        }

        public string Recognize(List<Sketch.Substroke> strokes)
        {
            ZernikeMoment moment = new ZernikeMoment(strokes);

            string best = _network.Classify(moment.FeatureValues);          

            return best;
        }

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public ZernikeMomentRecognizer(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            _network = (NeuralNetwork)info.GetValue("network", typeof(NeuralNetwork));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("network", _network);
        }

        #endregion
        
        public void Save(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Create);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }

        public static ZernikeMomentRecognizer Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            ZernikeMomentRecognizer zernike = (ZernikeMomentRecognizer)bformatter.Deserialize(stream);
            stream.Close();

            return zernike;
        }
    }

    [Serializable]
    public class ZernikeMomentRecognizerUpdateable : ISerializable
    {
        NeuralNetwork _network;
        Dictionary<string, List<ZernikeMoment>> _momentInstances;

        public ZernikeMomentRecognizerUpdateable()
        {
            List<string> features = new List<string>(new string[] { 
                "A00", "A11", "A20", "A22", "A31", "A33", "A40", 
                "A42", "A44", "A51", "A53", "A55", "A60", "A62", 
                "A64", "A66", "A71", "A73", "A75", "A77", "A80", 
                "A82", "A84", "A86", "A88", "A91", "A93", "A95", 
                "A97", "A99", "A100", "A102", "A104", "A106", 
                "A108", "A1010" } );

            NeuralNetworkInfo info = new NeuralNetworkInfo("Digital Circuits", "All");
            info.Layers = new int[] { 20, 7 };
            info.NumInputs = features.Count;
            info.LearningRate = 0.3;
            info.Momentum = 0.3;
            info.NumTrainingEpochs = 2000;
            info.Teacher = "Back Propagation";
            info.FeatureNames = features;

            info.OutputTypeNames = new List<string>();
            foreach (ShapeType gate in LogicDomain.Gates)
                info.OutputTypeNames.Add(gate.Name);

            _network = new NeuralNetwork(info);
            _momentInstances = new Dictionary<string, List<ZernikeMoment>>();
        }
        
        /// <summary>
        /// Create a trained zernike moment recognizer on the given
        /// data. NOTE: this constructor trains the neural network, 
        /// and may be very very slow.
        /// </summary>
        /// <param name="data">the labeled sketches to train on</param>
        public ZernikeMomentRecognizerUpdateable(List<Sketch.Shape> data)
            :this() // initialize the default neural network
        {
            foreach (Sketch.Shape shape in data)
                Add(shape.Type.Name, shape.SubstrokesL);
            Train();
        }

        public void Add(string label, List<Sketch.Substroke> strokes)
        {
            ZernikeMoment moment = new ZernikeMoment(label, strokes);
            if (_momentInstances.ContainsKey(label))
                _momentInstances[label].Add(moment);
            else
                _momentInstances.Add(label, new List<ZernikeMoment>(new ZernikeMoment[] {moment }));
        }

        public void Train()
        {
            List<FeatureSet> features = new List<FeatureSet>();

            foreach (KeyValuePair<string, List<ZernikeMoment>> pair in _momentInstances)
            {
                int indexClass = -1;
                foreach (ShapeType entry in LogicDomain.Gates)
                {
                    if (entry.Name == pair.Key)
                        indexClass = LogicDomain.Gates.IndexOf(entry);
                }

                double[] classes = new double[LogicDomain.Gates.Count];
                for (int i = 0; i < classes.Length; i++)
                {
                    if (i == indexClass)
                        classes[i] = 1.0;
                    else
                        classes[i] = 0.0;
                }

                foreach (ZernikeMoment zm in pair.Value)
                    features.Add(new FeatureSet(zm.Values.ToArray(), classes));
            }

            _network.Train(features.ToArray());
        }

        public void PrintInstances(string filename)
        {
            StreamWriter writer = new StreamWriter(filename);

            PrintHeader(writer);

            foreach (KeyValuePair<string, List<ZernikeMoment>> pair in _momentInstances)
                foreach (ZernikeMoment zm in pair.Value)
                    zm.Print(writer);

            writer.Close();
        }

        private void PrintHeader(StreamWriter writer)
        {
            #region Write Header
            writer.WriteLine("@RELATION zernikeFeatures");
            writer.WriteLine();
            writer.WriteLine("@ATTRIBUTE A00 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A11 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A20 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A22 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A31 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A33 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A40 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A42 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A44 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A51 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A53 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A55 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A60 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A62 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A64 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A66 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A71 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A73 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A75 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A77 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A80 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A82 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A84 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A86 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A88 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A91 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A93 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A95 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A97 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A99 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A100 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A102 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A104 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A106 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A108 NUMERIC");
            writer.WriteLine("@ATTRIBUTE A1010 NUMERIC");
            writer.WriteLine("@ATTRIBUTE class {AND,OR,NAND,NOR,NOT,XOR,NOTBUBBLE}");
            writer.WriteLine();
            writer.WriteLine("@DATA");
            #endregion
        }

        public string Recognize(List<Sketch.Substroke> strokes)
        {
            ZernikeMoment moment = new ZernikeMoment(strokes);

            string best = _network.Classify(moment.FeatureValues);          

            return best;
        }

        public ZernikeMomentRecognizer LiteRecognizer
        {
            get { return new ZernikeMomentRecognizer(_network); }
        }

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public ZernikeMomentRecognizerUpdateable(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            _network = (NeuralNetwork)info.GetValue("network", typeof(NeuralNetwork));
            _momentInstances = (Dictionary<string, List<ZernikeMoment>>)info.GetValue("momentInstances", typeof(Dictionary<string, List<ZernikeMoment>>));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("network", _network);
            info.AddValue("momentInstances", _momentInstances);
        }

        #endregion
        
        public void Save(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Create);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }

        public static ZernikeMomentRecognizerUpdateable Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            ZernikeMomentRecognizerUpdateable zernike = (ZernikeMomentRecognizerUpdateable)bformatter.Deserialize(stream);
            stream.Close();

            return zernike;
        }
    }
}
