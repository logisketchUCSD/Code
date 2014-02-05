/*
 * File: RecognitionManager.cs
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
using RecognitionInterfaces;

namespace RecognitionManager
{
    /// <summary>
    /// The RecognitionManager listens for events on a sketch panel and responds
    /// accordingly. It is the middle ground between the UI and the recognition
    /// sides of the program.
    /// </summary>
    public class RecognitionManager
    {

        #region Members

        /// <summary>
        /// The filenames of the settings we'll use for recognition.
        /// </summary>
        private Dictionary<string, string> _filenames;

        /// <summary>
        /// The domain this manager operates in. Specifically, the
        /// circuit domain.
        /// </summary>
        private ContextDomain.ContextDomain _domain;

        /// <summary>
        /// The classifier that handles single stroke classification.
        /// </summary>
        private Classifier _strokeClassifier;

        /// <summary>
        /// The grouper that handles grouping strokes into shapes.
        /// </summary>
        private Grouper _strokeGrouper;

        /// <summary>
        /// The recognizer used for recognizing shapes.
        /// </summary>
        private Recognizer _sketchRecognizer;

        /// <summary>
        /// The connector used for making connections between shapes.
        /// </summary>
        private Connector _connector;

        /// <summary>
        /// The refiner pipeline runs several refiners to improve recognition after 
        /// everything else has happened.
        /// </summary>
        private IRecognitionStep _refinement;

        /// <summary>
        /// Set to true to pring sebugging info
        /// </summary>
        private bool debug = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a Recognition Manager for the given sketch panel with default settings.
        /// Settings are loaded from "settings.txt".
        /// </summary>
        /// <param name="p">a sketch panel to manage</param>
        public RecognitionManager()
        {
            // Load settings from text file
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            string SettingsFilename = directory + "//settings.txt";
            _filenames = Files.SettingsReader.readSettings(SettingsFilename);

            // Initialize the recognition machines
            _domain = ContextDomain.CircuitDomain.GetInstance();
            _strokeClassifier = RecognitionPipeline.createDefaultClassifier();
            _strokeGrouper = RecognitionPipeline.createDefaultGrouper();
            _sketchRecognizer = RecognitionPipeline.createDefaultRecognizer();
            _connector = RecognitionPipeline.createDefaultConnector();
            _refinement = RecognitionPipeline.createDefaultRefiner();
        }

        #endregion

        #region Recognition Steps

        ////////////////////////////////////////////////
        // The order of recognition steps is as follows:
        //   1: Classify Single Strokes
        //   2: Group Strokes into Shapes
        //   3: Recognize Shapes
        //   4: Connect Shapes
        //   5: Refine Recognition
        /////////////////////////////////////////////////

        /// <summary>
        /// Applies the single stroke recognizer to the internal sketch.
        /// </summary>
        public void ClassifySingleStrokes(Featurefy.FeatureSketch _featuresketch)
        {
            _strokeClassifier.process(_featuresketch);
        }

        /// <summary>
        /// Applies the stroke grouper to the sketch, and forms the resulting shapes in the sketch.
        /// 
        /// Precondition: ClassifySingleStrokes() has been called.
        /// </summary>
        public void GroupStrokes(Featurefy.FeatureSketch _featuresketch)
        {
            _strokeGrouper.process(_featuresketch);
        }

        /// <summary>
        /// Recognizes each shape in the sketch with the shape recognizer.
        /// 
        /// Precondition: GroupStrokes() has been called.
        /// </summary>
        public void Recognize(Featurefy.FeatureSketch _featuresketch)
        {
            _sketchRecognizer.process(_featuresketch);
        }

        /// <summary>
        /// Connects the sketch based on class or label.
        /// 
        /// Precondition: Recognize() has been called.
        /// </summary>
        public void ConnectSketch(Featurefy.FeatureSketch _featuresketch)
        {
            _connector.process(_featuresketch);
        }

        /// <summary>
        /// Refines the recognition after the shapes have all been recognized.
        /// 
        /// Precondition: ConnectSketch() has been called.
        /// </summary>
        public void RefineSketch(Featurefy.FeatureSketch _featuresketch)
        {
            _refinement.process(_featuresketch);

            if (debug)
            {
                Console.WriteLine("Connections:");
                foreach (Sketch.Shape shape in _featuresketch.Sketch.Shapes)
                {
                    Console.Write("  --> " + shape.Name + ": ");
                    bool first = true;
                    foreach (Sketch.Shape connectedShape in shape.ConnectedShapes)
                    {
                        if (!first)
                            Console.Write(", ");
                        first = false;

                        Console.Write(connectedShape.Name);
                    }
                    Console.WriteLine();
                }
            }
        }

        #endregion

        #region Other

        /// <summary>
        /// Ensure that all the shape names in the sketch are unique.
        /// </summary>
        public void MakeShapeNamesUnique(Featurefy.FeatureSketch _featuresketch)
        {
            new Refiner.UniqueNamer().process(_featuresketch);
        }

        /// <summary>
        /// Ensure that all the stages of the pipeline are completely loaded. This
        /// method will block the current thread until loading finishes.
        /// </summary>
        public void FinishLoading()
        {
            ((StrokeClassifier.StrokeClassifier)_strokeClassifier).FinishLoading();
            ((StrokeGrouper.StrokeGrouper)_strokeGrouper).FinishLoading();
        }

        /// <summary>
	    /// Test the validity of each labeled shape.
	    /// </summary>
	    /// <returns>A dictionary mapping shapes to validity.</returns>
        public Dictionary<Sketch.Shape, bool> TestValidity(Featurefy.FeatureSketch _featuresketch)
	    {
            Sketch.Sketch sketch = _featuresketch.Sketch;
	        Dictionary<Sketch.Shape, bool> dict = new Dictionary<Sketch.Shape, bool>();
	        foreach (Sketch.Shape shape in sketch.Shapes)
	        {
	            dict.Add(shape, _domain.IsProperlyConnected(shape));
	        }
	
	        return dict;
	    }

        /// <summary>
        /// Rerecognizes the strokes given as a single group
        /// </summary>
        public void RerecognizeGroup(Sketch.Shape shape, Featurefy.FeatureSketch _featuresketch)
        {
            regroupShape(shape, _featuresketch);
            shape.ClearConnections();
            _connector.connect(shape, _featuresketch.Sketch);
        }

        /// <summary>
        /// Regroups and recognizes the shape
        /// </summary>
        /// <param name="shape"></param>
        private void regroupShape(Sketch.Shape shape, Featurefy.FeatureSketch _featuresketch)
        {
            if (shape.Type == new ShapeType())
                _strokeClassifier.process(_featuresketch, shape.Substrokes);

            if (shape.Substrokes.Length > 0)
            {

                // Determine the majority classification
                Dictionary<string, int> counts = new Dictionary<string, int>();
                foreach (Sketch.Substroke substroke in shape.Substrokes)
                {
                    if (!counts.ContainsKey(substroke.Classification))
                        counts.Add(substroke.Classification, 0);
                    counts[substroke.Classification]++;
                }

                string bestClassification = LogicDomain.GATE_CLASS;
                int bestCount = 0;
                foreach (KeyValuePair<string, int> pair in counts)
                {
                    if (pair.Value > bestCount)
                    {
                        bestCount = pair.Value;
                        bestClassification = pair.Key;
                    }
                }

                // Give all the substrokes the same classification since they are in the same group
                shape.Classification = bestClassification;
                if (!shape.AlreadyLabeled)
                    _sketchRecognizer.processShape(shape, _featuresketch);
            }

            else
                Console.WriteLine("WARNING No substrokes available");

            MakeShapeNamesUnique(_featuresketch);
        }

        #endregion

        #region Getters and Setters

        /// <summary>
        /// Gets and sets the recognizer used to identify shapes
        /// </summary>
        public RecognitionInterfaces.Recognizer Recognizer
        {
            get { return _sketchRecognizer; }
        }

        /// <summary>
        /// Get the default recognition pipeline.
        /// </summary>
        public RecognitionPipeline DefaultPipeline
        {
            get { return RecognitionPipeline.createDefaultPipeline(_filenames); }
        }

        #endregion

    }
}
