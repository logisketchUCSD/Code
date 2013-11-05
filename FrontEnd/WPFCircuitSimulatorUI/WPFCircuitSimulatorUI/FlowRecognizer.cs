using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using ConverterXML;

using SketchPanelLib;
using Flow;
using CircuitRec;



namespace WPFCircuitSimulatorUI
{
    /// <summary>
    /// A SketchRecognizer that uses Flow to recogize a sketch of a 
    /// circuit.  Accepts an unlabeled, user drawn sketch and 
    /// returns a labeled (and likely fragmented) sketch.  
    /// Runs only when the user intentionally triggers recognition, 
    /// and does not maintain an internal state of the sketch between 
    /// recognition calls.
    /// </summary>
    public class FlowRecognizer : SketchRecognizer
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public FlowRecognizer()
            : base()
        {
        }

        /// <summary>
        /// Constructor.  Subscribes to the given
        /// SketchPanel. 
        /// </summary>
        /// <param name="panel">The SketchPanel to which we want to subscribe.</param>
        public FlowRecognizer(SketchPanel panel)
            : base(panel)
        {
        }

        /// <summary>
        /// Recognizes a sketch of a circuit using the Flow.
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
            Flow.Flow flow;
            try
            {
                // Try to run the flow
                flow = new Flow.Flow(args.Sketch);

                // Grab sketch
                result.Sketch = flow.SketchHolder;
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
    }
}
