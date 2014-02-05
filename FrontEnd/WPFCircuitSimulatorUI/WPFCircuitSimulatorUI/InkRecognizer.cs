using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ConverterXML;

using SketchPanelLib;
using Clusterer;
using ImageRecognizer;
using ImageRecognizerWrapper;
using Clusters;
using InkForm;
using CircuitRec;

namespace WPFCircuitSimulatorUI
{
    /// <summary>
    /// A SketchRecognizer that uses a Neural Network to recogize a sketch of a 
    /// circuit.  Accepts an unlabeled, user drawn sketch and 
    /// returns a labeled (and likely fragmented) sketch.  
    /// Runs only when the user intentionally triggers recognition, 
    /// and does not maintain an internal state of the sketch between 
    /// recognition calls.
    /// </summary>
    public class InkRecognizer : SketchRecognizer
    {
        //Tool for recognizing the sketch
        InkTool iTool;

        //Where to look up the settings
        String settingsFilename;

        //Directory
        String dir;

        //Flag to indicate if we have results
        bool haveResults;

        //sketch to generate
        Sketch.Sketch rSketch;
        

        /// <summary>
        /// Constructor
        /// </summary>
        public InkRecognizer()
            : base()
        {
        }

        /// <summary>
        /// Constructor.  Subscribes to the given
        /// SketchPanel. 
        /// </summary>
        /// <param name="panel">The SketchPanel to which we want to subscribe.</param>
        public InkRecognizer(SketchPanel panel)
            : base(panel)
        {

        }

        /// <summary>
        /// Recognizes the strokes of the sketch using the Ink.
        /// </summary>
        /// <param name="args">An unlabeled Sketch and a UserTriggered flag</param>
        /// <returns>A Labeled Sketch and </returns>
        public override RecognitionResult Recognize(RecognitionArgs args)
        {
            RecognitionResult result = new RecognitionResult();
            result.UserTriggered = args.UserTriggered;

            // Only recognize when necessary
            if (!args.UserTriggered)
                return result;

            // Run recognition and fill result
            Sketcher sketcher = new Sketcher();
            iTool = sketcher.InkPanel.InkTool;
            iTool.Clusterer.ImageRecognizer.RecognitionCompleted += new RecognitionCompletedEventHandler(ImageRecognizer_RecognitionCompleted);
            settingsFilename = sketcher.SettingsFilename;
            dir = sketcher.BaseDirectory;

            try
            {
                iTool.Clusterer.ImageRecognizer.m_RecognitionComplete = false;
                // Try to use InkTool
                bool success = iTool.ClassifySketch(args.Sketch, settingsFilename, dir);
                StrokeClassifier.StrokeClassifierResult clResult = iTool.Clusterer.Classifier.Classify();
                StrokeGrouper.StrokeGrouperResult grResult = iTool.Clusterer.Grouper.Group(clResult.AllClassifications);
                List<Cluster> initialClusters = new List<Cluster>();//iTool.Clusterer.CreateClusters(grResult);
                ImageRecognizerWrapper.ImageRecognizerResults imgResult = iTool.Clusterer.GetImageResults(initialClusters);
                //while (!iTool.Clusterer.InitialClustersDone);
                //iTool.Clusterer.ImageRecognizer.RecognizeST(iTool.Clusterer.
                rSketch = iTool.MakeSketch(imgResult.ScoredClusters);
                result.Sketch = rSketch;

                               
            }
            catch (Exception e)
            {
                // Catch all other exceptions
                System.Windows.MessageBox.Show("General Exception from sketch recognizer component: \n" + e.Message);

                // Return unrecognized sketch
                result.Sketch = args.Sketch;
            }

            return result;
        }




        void ImageRecognizer_RecognitionCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            try
            {
                List<Cluster> scored = (List<Cluster>)e.Result;
                rSketch = new Sketch.Sketch();
                haveResults = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ImageRecognizerCompleted: {0}", ex.Message);
            }
        }
    }
}
