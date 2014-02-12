/*
 * File: StrokeClassifier.cs
 *
 * Author: Sketchers 2011
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2011.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Ink;
using Utilities;
using Featurefy;
using Sketch;
using Utilities.Concurrency;

namespace StrokeClassifier
{
    [Serializable]
    public class StrokeClassifier : RecognitionInterfaces.Classifier
    {
        #region Constants

        /// <summary>
        /// The stroke classifier filename. There should be a .arff 
        /// and a .model of this file in your working directory.
        /// </summary>
        private const string FILENAME = "strokeClassifier";

        #endregion

        #region Member Variables

        /// <summary>
        /// The stroke classification features and whether
        /// or not to actually use them in the decision tree.
        /// </summary>
        private Dictionary<string, bool> _featuresOn;

        /// <summary>
        /// Encapsulates all of the weka ugliness.
        /// </summary>
        private Future<WekaWrap> _wekaWrapper;

        /// <summary>
        /// The file to load features from.
        /// </summary>
        private string _filename;

        #endregion

        #region Constructor

        public StrokeClassifier()
            : this(Files.SettingsReader.readSettings()["FeaturesSingle"])
        {
        }

        /// <summary>
        /// Constructor, taking a name of the stroke features file
        /// </summary>
        /// <param name="settings"></param>
        public StrokeClassifier(string filename)
        {
            _filename = filename;

            // We should probably have a better way of doing this, but this is it for now.
            // Uncomment the following line if you want to retrain the classifier and the groupers.
            //WekaWrap.wekaSetUp(WekaWrap.Classifier.AdaBoost_J48, @"C:\Users\Research\Documents\sketch\Data\Grouper Training Data");

            _wekaWrapper = new Future<WekaWrap>(loadClassifier);
        }

        /// <summary>
        /// Load the classifier from a model file.
        /// 
        /// Precondition: _filename points to a file that exists.
        /// 
        /// Postcondition: _featuresOn and _wekaWrapper are initialized.
        /// </summary>
        private WekaWrap loadClassifier()
        {
            Console.WriteLine("Loading StrokeClassifier");

            _featuresOn = loadFeatures(_filename);

            string directory = AppDomain.CurrentDomain.BaseDirectory;
            string basefilename = directory + FILENAME;

            WekaWrap wrapper = new WekaWrap(basefilename);

            Console.WriteLine("Finished loading StrokeClassifier");

            return wrapper;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Wait for this to finish loading
        /// </summary>
        public void FinishLoading()
        {
            _wekaWrapper.WaitForValue();
        }

        /// <summary>
        /// Classifies using WEKA.
        /// </summary>
        public override string classify(Substroke substroke, FeatureSketch featureSketch)
        {

            // Get the features for this substroke
            double[] featureValues = featureSketch.GetValuesSingle(substroke);

            // classify using Weka
            string classification = _wekaWrapper.Value.classify(featureValues);
            return classification;

        }

        #endregion

        #region Saving and Loading

        /// <summary>
        /// Method for saving a stroke classifier.
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Create);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }

        /// <summary>
        /// Method for loading a stroke classifier.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static StrokeClassifier Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            StrokeClassifier strokeClassifier = (StrokeClassifier)bformatter.Deserialize(stream);
            stream.Close();

            return strokeClassifier;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Loads the stroke classification features list from a text file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>A dictionary of features to use.</returns>
        public static Dictionary<string, bool> loadFeatures(string filename)
        {
            System.IO.StreamReader reader = new System.IO.StreamReader(filename);

            Dictionary<string, bool> featureList = new Dictionary<string, bool>();
            string line;

            while ((line = reader.ReadLine()) != null && line != "")
            {
                string key = line.Substring(0, line.IndexOf("=") - 1);
                string value = line.Substring(line.IndexOf("=") + 2);
                if (value == "true")
                    featureList.Add(key, true);
                else if (value == "false")
                    featureList.Add(key, false);
                else
                    return null;
            }

            reader.Close();

            return featureList;
        }

        #endregion
    }
}
