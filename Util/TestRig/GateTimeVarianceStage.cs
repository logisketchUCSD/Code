using Data;
using Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TestRig
{
    /// <summary>
    /// Tests the changes in drawing per gate over time FOR ONE USER.
    /// This stage assumes that it is only being run on one user's sketches and alphabetical
    /// order corresponds to the order the sketches were drawn in.
    /// </summary>
    class GateTimeVarianceStage : ProcessStage
    {
        #region Internals
        /// <summary>
        /// Stores our recognition results. 
        /// Each entry in the list corresponds to a sketch.
        /// Each entry has a dictionary representing that sketch's results.
        /// Each dictionary gives you information about each gate in that sketch.
        /// For each gate, we store a pair of total shapes of that type, then correctly recognized shapes of that type.
        /// </summary>
        List<Dictionary<ShapeType, MutablePair<int, int>>> _results;

        /// <summary>
        /// Allows us to recognize things. Initialized and set up during start().
        /// </summary>
        RecognitionManager.RecognitionPipeline _pipeline;

        /// <summary>
        /// The first filename so that we can determine which user we're working on.
        /// </summary>
        string _filename;

        #endregion

        /// <summary>
        /// Constructor for the stage. Sets values used by TestRig.
        /// </summary>
        public GateTimeVarianceStage()
        {
            name = "Gate Time Variance Stage";
            shortname = "gtvs";
            outputFiletype = ".csv";
        }

        /// <summary>
        /// Initializes important things and adds our step to the pipeline.
        /// </summary>
        public override void start()
        {
            _pipeline = new RecognitionManager.RecognitionPipeline();
            _results = new List<Dictionary<ShapeType, MutablePair<int, int>>>();
            RecognitionInterfaces.Recognizer recognizer = RecognitionManager.RecognitionPipeline.createDefaultGateRecognizer();
            recognizer.reset();
            _pipeline.addStep(recognizer);
        }

        /// <summary>
        /// Runs the tests!
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="filename"></param>
        public override void run(Sketch.Sketch sketch, string filename)
        {
            // store the first filename as a way to tell what user we're working on.
            if (_filename == null)
                _filename = filename;

            Dictionary<ShapeType, MutablePair<int, int>> sketchResults = new Dictionary<ShapeType, MutablePair<int, int>>();
            foreach (ShapeType type in LogicDomain.Gates)
                sketchResults.Add(type, MutablePair.Create(0, 0));

            Sketch.Sketch handLabeled = sketch.Clone();

            _pipeline.process(sketch);

            foreach (Sketch.Shape correctShape in handLabeled.Shapes)
            {
                if (!LogicDomain.IsGate(correctShape.Type)) continue;

                Sketch.Shape resultShape = sketch.ShapesL.Find(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); });

                if (resultShape == null) throw new Exception("Could not find shape.");

                sketchResults[correctShape.Type].Item1++;
                if (resultShape.Type == correctShape.Type) 
                    sketchResults[correctShape.Type].Item2++;
            }

            _results.Add(sketchResults);
        }

        /// <summary>
        /// Write the results to a file.
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="path"></param>
        public override void finalize(TextWriter tw, string path)
        {
            tw.WriteLine(_filename);
            tw.Write("Total shapes, Correct shapes, Percent correct, ");
            foreach (ShapeType type in LogicDomain.Gates)
                tw.Write("Total " + type + ", Correct " + type + ", ");
            tw.WriteLine();

            foreach (var result in _results)
            {
                int totalShapes = 0;
                int correctShapes = 0;
                double percentCorrect = 0.0;
                foreach (ShapeType type in result.Keys)
                {
                    totalShapes += result[type].Item1;
                    correctShapes += result[type].Item2;
                }
                percentCorrect = (double)correctShapes / (double)totalShapes;

                tw.Write(totalShapes + "," + correctShapes + "," + percentCorrect + ",");
                foreach (ShapeType type in result.Keys)
                    tw.Write(result[type].Item1 + ", " + result[type].Item2 + ", ");
                tw.WriteLine();
            }
        }
    }
}
