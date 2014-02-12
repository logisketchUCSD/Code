/*
 * File: StrokeGrouper.cs
 *
 * Author: Sketchers 2010
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2010.
 *
 */

using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Sketch;
using Featurefy;
using Microsoft.Ink;
using Utilities;
using Utilities.Concurrency;

namespace StrokeGrouper
{
    [Serializable]
    public class StrokeGrouper : RecognitionInterfaces.Grouper
    {
        #region Constants

        /// <summary>
        /// The file from which to load the weka classifier.
        /// </summary>
        private const string GATE_GROUPER_FILE = "gateGrouper";
        private const string WIRE_GROUPER_FILE = "wireGrouper";
        private const string TEXT_GROUPER_FILE = "textGrouper";

        private const string GATE = "Gate";
        private const string WIRE = "Wire";
        private const string TEXT = "Text";

        private bool debug = true;

        #endregion

        #region Member Variables

        /// <summary>
        /// Holds Weka classifiers
        /// </summary>
        private Future<WekaWrap> _gateClassifier;
        private Future<WekaWrap> _textClassifier;

        #endregion

        #region Constructors
        
        public StrokeGrouper()
            : this(Files.SettingsReader.readSettings()["FeaturesGroup"])
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The file containing the features for grouping. Not actually used anywhere.</param>
        public StrokeGrouper(string filename)
        {
            _gateClassifier = new Future<WekaWrap>(loadGateGrouper);
            _textClassifier = new Future<WekaWrap>(loadTextGrouper);
        }

        /// <summary>
        /// Loads the gate grouper.
        /// </summary>
        private WekaWrap loadGateGrouper()
        {
            if (debug) Console.WriteLine("Loading GateGrouper");
            string gatemodel = AppDomain.CurrentDomain.BaseDirectory + GATE_GROUPER_FILE;
            if (debug) Console.WriteLine("Finished loading GateGrouper");
            return new WekaWrap(gatemodel);
        }

        /// <summary>
        /// Loads the text grouper.
        /// </summary>
        private WekaWrap loadTextGrouper()
        {
            if (debug) Console.WriteLine("Loading TextGrouper");
            string textmodel = AppDomain.CurrentDomain.BaseDirectory + TEXT_GROUPER_FILE;
            if (debug) Console.WriteLine("Finished loading TextGrouper");
            return new WekaWrap(textmodel);
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// wait for this to finish loading
        /// </summary>
        public void FinishLoading()
        {
            _gateClassifier.WaitForValue();
            _textClassifier.WaitForValue();
        }

        /// <summary>
        /// Uses WEKA to decide if each pair of strokes should be joined, then merges the paired strokes into shapes.
        /// </summary>
        public override void group(Featurefy.FeatureSketch featureSketch)
        {
            // Compile the substroke classification dictionary.
            Dictionary<Substroke, string> strokeClassifications = new Dictionary<Substroke, string>();
            foreach (Substroke s in featureSketch.Sketch.Substrokes)
                strokeClassifications.Add(s, s.Classification);

            // Calculate pairwise feature values.
            Dictionary<string, Dictionary<FeatureStrokePair, double[]>> pairwiseValuesPerClass = featureSketch.GetValuesPairwise(strokeClassifications);

            List<StrokePair> pairsToJoin = groupWithWeka(pairwiseValuesPerClass);

            // Using the results from the StrokeGrouper,
            // make shapes by combining joined strokes.
            try
            {
                makeShapes(pairsToJoin, featureSketch.Sketch);
            }
            catch (NullReferenceException nre)
            {
                Console.WriteLine("Cannot call makeShapes(). \n" + nre.Message);
            }
        }

        #endregion

        #region Group Using WEKA

        /// <summary>
        /// Uses WEKA classifiers to decide if each pair of strokes should be grouped. Uses distinct classifiers for each type of shape.
        /// </summary>
        /// <returns>A list of StrokePairs that should be joined.</returns>
        private List<StrokePair> groupWithWeka(Dictionary<string, Dictionary<FeatureStrokePair, double[]>> pairwiseValuesPerClass)
        {
            List<StrokePair> joinedPairs = new List<StrokePair>();

            foreach (string strokeClassification in pairwiseValuesPerClass.Keys)
            {
                foreach (FeatureStrokePair pair in pairwiseValuesPerClass[strokeClassification].Keys)
                {
                    double[] featureValues = pairwiseValuesPerClass[strokeClassification][pair];
                        
                    string groupClassification = null;
                    switch (strokeClassification)
                    {
                        case GATE:
                            groupClassification = _gateClassifier.Value.classify(featureValues);
                            break;
                        case TEXT:
                            groupClassification = _textClassifier.Value.classify(featureValues);
                            break;
                        default:
                            groupClassification = "Ignore";
                            //Console.WriteLine("Unknown stroke class: '" + strokeClassification + "'. Grouping failed.");
                            break;
                    }
                    if (groupClassification.StartsWith("Join"))
                        joinedPairs.Add(pair);
                }
            }
            return joinedPairs;
        }

        #endregion

        #region Make Shapes

        /// <summary>
        /// Once strokes are grouped, turn the stroke pairs into shapes.
        /// </summary>
        /// <param name="pairs">The list of stroke pairs that should be merged into shapes.</param>
        /// <param name="sketch">The sketch the strokes exist in; where the shapes should be created.</param>
        private void makeShapes(List<Sketch.StrokePair> pairs, Sketch.Sketch sketch)
        {
            foreach (Sketch.StrokePair pair in pairs)
            {
                Sketch.Shape shape1 = pair.Item1.ParentShape;
                Sketch.Shape shape2 = pair.Item2.ParentShape;

                // If they've already been merged, forget it.
                if (shape1 == shape2)
                    continue;

                // If either one is null, we have a problem!
                if (shape1 == null || shape2 == null)
                    throw new Exception("Cannot merge substrokes " + pair.Item1 + " and " + pair.Item2 + "; one or both are missing a parent shape");

                // Don't merge any user-specified shapes.
                if (shape1.AlreadyGrouped || shape2.AlreadyGrouped)
                    continue;
                
                sketch.mergeShapes(shape1, shape2);

            }

#if DEBUG
            Console.WriteLine("Your sketch has " + sketch.Shapes.Length + " distinct shapes.");
            int gates = 0;
            foreach (Sketch.Shape shape in sketch.Shapes)
                if (shape.Classification == LogicDomain.GATE_CLASS)
                    gates++;
            Console.WriteLine(gates + " of those shapes are gates.");
#endif
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Read a list of features to use from a text file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static Dictionary<string, bool> loadFeatures(string filename)
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
