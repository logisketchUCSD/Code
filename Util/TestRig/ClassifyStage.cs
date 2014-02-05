using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RecognitionManager;
using TestRig.Utils;

namespace TestRig
{
    /// <summary>
    /// Tests the classification stage of sketch recognition.
    /// </summary>
    class ClassifyStage : ProcessStage
    {
        private Table _results;
        private ConfusionMatrix<string> _confusion;
        private RecognitionPipeline _pipeline;

        /// <summary>
        /// Create a ClassifyStage.
        /// </summary>
        public ClassifyStage()
        {
            name = "Classifier";
            shortname = "cls";
            _results = new Table(new string[] { "Test File", "Substrokes", "Correct" });
            _confusion = new ConfusionMatrix<string>();
            outputFiletype = ".csv"; // comma-separated values; readable by Excel
            _pipeline = new RecognitionPipeline();
            _pipeline.addStep(RecognitionPipeline.createDefaultClassifier());
        }

        
        /// <summary>
        /// Run the test on a single sketch.
        /// </summary>
        /// <param name="sketch">the sketch to classify</param>
        /// <param name="filename">the name of the file being tested</param>
        public override void run(Sketch.Sketch sketch, string filename)
        {
            // Record original classifications
            Dictionary<Sketch.Substroke, string> original_classes = new Dictionary<Sketch.Substroke, string>();
            foreach (Sketch.Substroke substroke in sketch.Substrokes)
            {
                original_classes.Add(substroke, substroke.Classification);
            }

            // Run the classifier!
            sketch.RemoveLabels();
            _pipeline.process(sketch);

            // Evaluate the classifier!
            int numSubstrokes = 0;
            int numCorrect = 0;
            foreach (Sketch.Substroke s in sketch.Substrokes)
            {
                string correct_class = original_classes[s];

                // Skip unknown substrokes
                if (correct_class.ToLower() == "unknown")
                    continue;

                string result_class = s.Classification;

                _confusion.increment(correct_class, result_class);

                numSubstrokes++;
                if (correct_class.Equals(result_class))
                {
                    numCorrect++;
                }
            }

            Console.WriteLine("   --> Correct classifications: " + numCorrect + "/" + numSubstrokes);

            // Assemble the results!
            _results.addResult(new string[] {
                filename,
                "" + numSubstrokes,
                "" + numCorrect
            });
        }

        /// <summary>
        /// Write the results to a file.
        /// </summary>
        /// <param name="tw">an open handle to the file</param>
        /// <param name="path">the folder the file is in</param>
        public override void finalize(System.IO.TextWriter tw, string path)
        {
            // Writes a CSV (comma-separated values) file which can be read by Excel.

            tw.WriteLine("Confusion Matrix");
            _confusion.writeCSV(tw);

            tw.WriteLine();

            tw.WriteLine("Results");
            _results.writeCSV(tw);

        }

    }
}
