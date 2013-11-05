using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using RecognitionInterfaces;
using Utilities;
using Utilities.Neuro;
using Featurefy;
using Sketch;
using SubRecognizer;
using Domain;

namespace Recognizers
{

    /// <summary>
    /// The NeuralImageRecognizer is a wrapper around another ImageRecognizer (adaptive or plain)
    /// and fills a very specific niche. The confidence value returned by the ImageRecognizer is
    /// useful only for determining how confident the result is RELATIVE TO OTHER TEMPLATES. What
    /// we need is a confidence value which can ALSO tell us HOW CONFIDENT ARE WE THAT IT IS A GATE.
    /// This is useful for the search refiner.
    /// 
    /// This recognizer uses a neural network to filter the confidence value returned by the inner
    /// ImageRecognizer. The inputs to the network are the 4 major coefficients (partial/modified
    /// Hausdorff, Yule, and Tanimoto) and the output is a single value between 0 (not a gate) and
    /// 1 (is a gate). The returned confidence is the product of the network output and the image
    /// recognizer confidence.
    /// 
    /// Visual representation:
    /// 
    ///            +-----------------------------------------------+
    ///            |           ImageRecognitionResult              |
    ///            +-----------------------------------------------+
    ///                |      |        |          |            |
    ///                |     Yule  Tanimoto  P.Hausdorff  M.Hausdorff
    ///                |       \       |          |           /
    ///              Other      \      |          |          /
    ///           Attributes   +-------------------------------+
    ///                |       |     Trained Neural Network    |
    ///                |       +-------------------------------+
    ///                |                      |
    ///                |               Better Confidence
    ///                |                      |
    ///                v                      v
    ///            +-----------------------------------------------+
    ///            |           New ImageRecognitionResult          |
    ///            +-----------------------------------------------+
    /// 
    /// </summary>
    [Serializable]
    public class NeuralImageRecognizer : ExtendedRecognizer
    {

        #region Constants
        private static readonly string SAVE_PATH = @"SubRecognizers\ImageRecognizer\myNeuralImage.nir";
        #endregion

        #region Internals
        private ImageRecognizer _imageRecognizer;
        private NeuralNetwork _neuralNetwork;
        #endregion

        #region Static Methods

        private static double[] inputVectorForResult(ImageRecognitionResult result)
        {
            return new double[] { result.PartialHausdorff, result.ModifiedHausdorff, result.Yule, result.Tanimoto };
        }

        /// <summary>
        /// Construct the training data set that this recognizer will use to train the neural network.
        /// </summary>
        /// <param name="recognizer"></param>
        /// <param name="goodGates"></param>
        /// <param name="badGates"></param>
        /// <returns></returns>
        public static List<FeatureSet> ConstructTrainingSet(ImageRecognizer recognizer, IEnumerable<Shape> goodGates, IEnumerable<Shape> badGates)
        {
            List<FeatureSet> trainingData = new List<FeatureSet>();
            foreach (Shape goodGate in goodGates)
            {
                ImageRecognitionResult result = (ImageRecognitionResult)recognizer.recognize(goodGate, null);
                trainingData.Add(new FeatureSet(inputVectorForResult(result), new double[] { 1 }));
            }
            foreach (Shape badGate in badGates)
            {
                ImageRecognitionResult result = (ImageRecognitionResult)recognizer.recognize(badGate, null);
                trainingData.Add(new FeatureSet(inputVectorForResult(result), new double[] { 0 }));
            }
            return trainingData;
        }

        /// <summary>
        /// Write a WEKA ARFF file to the given text writer for the given data. 
        /// Neither the ARFF file nor Weka is used by this class, but the format
        /// is useful since Weka provides a very nice data exploration tool
        /// (see http://www.cs.waikato.ac.nz/ml/weka/).
        /// NOTE: This function does not close the given writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="recognizer"></param>
        /// <param name="goodGates"></param>
        /// <param name="badGates"></param>
        public static void WriteARFF(System.IO.TextWriter writer, ImageRecognizer recognizer, IEnumerable<Shape> goodGates, IEnumerable<Shape> badGates)
        {
            writer.WriteLine("@RELATION neuralImageRecognizerConfidence");
            writer.WriteLine("@ATTRIBUTE partialHausdorff  NUMERIC");
            writer.WriteLine("@ATTRIBUTE modifiedHausdorff NUMERIC");
            writer.WriteLine("@ATTRIBUTE yule              NUMERIC");
            writer.WriteLine("@ATTRIBUTE tanimoto          NUMERIC");
            writer.WriteLine("@ATTRIBUTE imageConfidence   NUMERIC");
            writer.WriteLine("@ATTRIBUTE imageType         {AND,OR,XOR,XNOR,NOR,NOT,NAND,NotBubble}");
            writer.WriteLine("@ATTRIBUTE idealConfidence   NUMERIC");

            writer.WriteLine("@DATA");

            foreach (Shape gate in goodGates)
            {
                ImageRecognitionResult result = (ImageRecognitionResult)recognizer.recognize(gate, null);
                writer.WriteLine(
                    result.PartialHausdorff + "," + 
                    result.ModifiedHausdorff + "," + 
                    result.Yule + "," +
                    result.Tanimoto + "," +
                    result.Confidence + "," + 
                    result.Type.Name + ",1");
            }

            foreach (Shape gate in badGates)
            {
                ImageRecognitionResult result = (ImageRecognitionResult)recognizer.recognize(gate, null);
                writer.WriteLine(
                    result.PartialHausdorff + "," +
                    result.ModifiedHausdorff + "," +
                    result.Yule + "," +
                    result.Tanimoto + "," +
                    result.Confidence + "," + 
                    result.Type.Name + ",0");
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Train a new neural image recognizer. The trained recognizer
        /// will use the given image recognizer and will be trained on 
        /// the given list of good gates (shapes that are actually
        /// gates) and bad gates (shapes that aren't gates).
        /// </summary>
        /// <param name="recognizer"></param>
        /// <param name="goodGates"></param>
        /// <param name="badGates"></param>
        /// <param name="info">The network information. NOTE: You do not need to set info.NumInputs; that will be taken care of for you.</param>
        public NeuralImageRecognizer(ImageRecognizer recognizer, IEnumerable<Shape> goodGates, IEnumerable<Shape> badGates, NeuralNetworkInfo info)
        {
            _imageRecognizer = recognizer;

            info.NumInputs = 4;

            // Assemble training data
            List<FeatureSet> trainingData = ConstructTrainingSet(_imageRecognizer, goodGates, badGates);

            // Create network
            _neuralNetwork = new NeuralNetwork(info);
            _neuralNetwork.Train(trainingData.ToArray());
        }

        /// <summary>
        /// Construct a neural image recognizer from the given recognizer and pre-trained network
        /// </summary>
        /// <param name="recognizer"></param>
        /// <param name="network"></param>
        public NeuralImageRecognizer(ImageRecognizer recognizer, NeuralNetwork network) 
        {
            _imageRecognizer = recognizer;
            _neuralNetwork = network;
        }

        #endregion

        #region Methods

        public RecognitionResult filterResult(ImageRecognitionResult result)
        {
            double confidence = _neuralNetwork.Compute(inputVectorForResult(result))[0] * result.Confidence;
            return new ImageRecognitionResult(
                result.Type,
                result.PartialHausdorff,
                result.ModifiedHausdorff,
                result.Yule,
                result.Tanimoto,
                confidence,               // use modified confidence value
                result.Orientation,
                result.AlternateTypes,
                result.TemplateName,
                result.TemplateBitmap);
        }

        public override RecognitionResult recognize(Shape shape, FeatureSketch featureSketch)
        {
            ImageRecognitionResult result = (ImageRecognitionResult)_imageRecognizer.recognize(shape, featureSketch);
            return filterResult(result);
        }

        /// <summary>
        /// Recognize a shape as if it had the given type
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public override Dictionary<ShapeType, RecognitionResult> RecognitionResults(Shape shape, IEnumerable<ShapeType> types)
        {
            Dictionary<ShapeType, RecognitionResult> result = _imageRecognizer.RecognitionResults(shape, types);
            return Data.Utils.replaceValues(result, r => { return filterResult((ImageRecognitionResult)r); });
        }

        public override void learnFromExample(Shape shape)
        {
            _imageRecognizer.learnFromExample(shape);
        }

        public override bool canRecognize(string classification)
        {
            return _imageRecognizer.canRecognize(classification);
        }

        public override void reset()
        {
            _imageRecognizer.reset();
        }

        #endregion

        #region Serialization, Saving, and Loading

        public override void save()
        {
            Save(AppDomain.CurrentDomain.BaseDirectory + SAVE_PATH);
        }

        /// <summary>
        /// Serializes the object and saves it to the specified filename
        /// </summary>
        /// <param name="filename">Filename to save the object as</param>
        public void Save(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Create);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }

        /// <summary>
        /// Loads a previously saved NeuralImageRecognizer from the given filename
        /// </summary>
        /// <param name="filename">Filename which is the saved ImageRecognizer</param>
        /// <returns>Re-instantiated ImageRecognzier</returns>
        public static NeuralImageRecognizer Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            NeuralImageRecognizer image = (NeuralImageRecognizer)bformatter.Deserialize(stream);
            stream.Close();

#if DEBUG
            Console.WriteLine("Neural recognizer loaded.");
#endif

            return image;
        }

        /// <summary>
        /// Calls Load(f), where f is the current directory + "SubRecognizers\ImageRecognizer\myNeuralImage.nir". 
        /// If that fails, it loads the current directory + "SubRecognizers\ImageRecognizer\NeuralImage.nir" instead.
        /// </summary>
        /// <returns>An AdaptiveImageRecognzier</returns>
        public static NeuralImageRecognizer LoadDefault()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;

            // In some cases (for instance, when running tests) the directory
            // does not contain a trailing '\' like we expect.
            if (directory[directory.Length - 1] != '\\')
                directory += '\\';

            NeuralImageRecognizer recognizer;
            try
            {
                // Try using the custom, user-specific recognizer.
                string trainedRecognizer = directory + SAVE_PATH;
                Console.WriteLine("Using user-specific neural image recognizer at " + trainedRecognizer);
                recognizer = Load(trainedRecognizer);
            }
            catch
            {
                // Use the default if necessary.
                string trainedRecognizer = directory + @"SubRecognizers\ImageRecognizer\NeuralImage.nir";
                Console.WriteLine("User-specific neural image recognizer not found! Falling back to default at " + trainedRecognizer);
                recognizer = Load(trainedRecognizer);
            }
            return recognizer;
        }

        #endregion

    }
}
