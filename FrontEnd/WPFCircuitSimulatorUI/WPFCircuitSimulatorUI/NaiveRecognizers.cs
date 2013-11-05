using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Controls;

using ConverterXML;
using SketchPanelLib;
using Sketch;

namespace WPFCircuitSimulatorUI
{
    /// <summary>
    /// Dummy recognizer for testing.  Sends a standard, partially labeled sketch
    /// as a result.  
    /// </summary>
    public class TestSketchRecognizer : SketchRecognizer
    {
        /// <summary>
        /// Path to default sample result sketch.
        /// File is located in output directory.  
        /// </summary>
        public static string TestSketchFilePath = AppDomain.CurrentDomain.BaseDirectory + @"\TestSketch.xml";

        /// <summary>
        /// Constructor.  Subscribes to the given
        /// SketchPanel. 
        /// </summary>
        /// <param name="panel">The SketchPanel to which we want to subscribe.</param>
        public TestSketchRecognizer(SketchPanel panel)
        {
            SubscribeToPanel(panel);
        }

        public override RecognitionResult Recognize(RecognitionArgs args)
        {
            Console.WriteLine("Sending sample sketch as recognition result");
            Sketch.Sketch resultSketch = ((new ReadXML(TestSketchFilePath)).Sketch);

            RecognitionResult result = new RecognitionResult();
            result.UserTriggered = true;
            result.Sketch = resultSketch;

            return result;
        }
    }

    /// <summary>
    /// Dummy Wire-Error recognizer used for user studies.  Randomly
    /// labels a fixed percentage of strokes as erroneous.
    /// </summary>
    public class WireErrorRecognizer : SketchRecognizer
    {
        /// <summary>
        /// Random object for triggering error labels
        /// </summary>
        private Random randObj = new Random();

        /// <summary>
        /// Rate at which erroroneous labels are applied (e.g 0.15 = 15%)
        /// </summary>
        public const double ErrorRate = 0.15D;

        /// <summary>
        /// Labels for wires.  This label corresponds to 
        /// blue (wires) in the Labeler domain file that
        /// this feedback mechanism uses.
        /// </summary>
        public static string WireLabel = "Wire";

        /// <summary>
        /// Labels for Errors.  This label corresponds to 
        /// orange (NAND) in the Labeler domain file that
        /// this feedback mechanism uses.
        /// </summary
        public static string ErrorLabel = "NAND";

        /// <summary>
        /// Constructor.  Subscribes to the given
        /// SketchPanel. 
        /// </summary>
        /// <param name="panel">The SketchPanel to which we want to subscribe.</param>
        public WireErrorRecognizer(SketchPanel panel)
        {
            SubscribeToPanel(panel);
        }

        /// <summary>
        /// When the user manually triggers recognition, label everything
        /// in the sketch as a wire, and label a fixed percentage of a 
        /// random selection of substrokes as erroneous.
        /// </summary>
        public override RecognitionResult Recognize(RecognitionArgs args)
        {
            RecognitionResult result = new RecognitionResult();

            // Only operate on user triggered recognition events
            if (!args.UserTriggered)
                return result;

            if (args.Sketch == null)
                return result;

            Sketch.Sketch sketch = args.Sketch;

            // Label the substrokes
            foreach (Substroke sub in sketch.Substrokes)
            {
                if (randObj.NextDouble() < ErrorRate)
                {
                    sketch.AddLabel(sub, ErrorLabel, 1.0);
                }
                else
                {
                    sketch.AddLabel(sub, WireLabel, 1.0);
                }
            }

            // Create result
            result.UserTriggered = args.UserTriggered;
            result.Sketch = sketch;

            return result;
        }
    }
}
