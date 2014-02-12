/*
 * File: SymbolStage.cs
 *
 * Authors: Marty Field, James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RecognitionInterfaces;
using RecognitionManager;

namespace TestRig
{
    /// <summary>
    /// This stage is for symbol recognition (for example, AND, OR, etc.)
    /// </summary>
    public class SymbolStage : ProcessStage
    {
        private HashSet<ShapeType> _typesSeen;
        private Dictionary<ShapeType, Dictionary<ShapeType, int>> _confusionMatrix;
        private List<int[]> _results;
        private RecognitionPipeline _pipeline;
        private RecognitionInterfaces.Recognizer _recognizer;
        private bool _useRefinement;
        private bool _useLearning;
        private bool _isPure;
        private string[] _args;
        
        /// <summary>
        /// Construct a new SymbolStage
        /// </summary>
        public SymbolStage()
        {
            name = "Symbol Recognition";
            shortname = "sym";
            inputFiletype = ".xml";
            outputFiletype = ".csv"; // comma-separated values; readable by Excel

            _typesSeen = new HashSet<ShapeType>();
            _confusionMatrix = new Dictionary<ShapeType, Dictionary<ShapeType, int>>();
            _results = new List<int[]>();
            _pipeline = new RecognitionPipeline();

            _useRefinement = false;
            _isPure = true;
            _useLearning = false;
            _args = null;

            _recognizer = RecognitionPipeline.createDefaultRecognizer();
        }

        /// <summary>
        /// Determine whether learning is turned on
        /// </summary>
        public bool Learning
        {
            get { return _useLearning; }
            set { _useLearning = value; }
        }

        /// <summary>
        /// Get the recognizer this stage is using
        /// </summary>
        public RecognitionInterfaces.Recognizer Recognizer
        {
            get { return _recognizer; }
            set { _recognizer = value; }
        }
        
        /// <summary>
        /// Ensures that dictionary[s1][s2] exists. Sets it to 0 if it does not.
        /// </summary>
        /// <param name="dictionary">the dictionary to work with</param>
        /// <param name="s1">shape type 1</param>
        /// <param name="s2">shape type 2</param>
        private static void ensureKeyExists2D(Dictionary<ShapeType, Dictionary<ShapeType, int>> dictionary, ShapeType s1, ShapeType s2)
        {
            if (!dictionary.ContainsKey(s1))
            {
                dictionary.Add(s1, new Dictionary<ShapeType, int>());
            }
            if (!dictionary[s1].ContainsKey(s2))
            {
                dictionary[s1].Add(s2, 0);
            }
        }

        /// <summary>
        /// Process the arguments for the symbol stage.
        /// </summary>
        /// <param name="args">the arguments to process</param>
        public override void processArgs(string[] args)
        {
            _args = args;
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-pure":           // A pure test is one in which the symbol stage uses the correct
                        _isPure = true;     // groups in the sketch to assume that a "perfect" grouper has
                        break;              // already been run. In an impure test, we run the classifier and
                    case "-impure":         // grouper first and get results for how well the entire pipeline 
                        _isPure = false;    // works up to the grouping step.
                        break;
                    case "-refine":
                        _useRefinement = true;
                        break;
                    case "-norefine":
                        _useRefinement = false;
                        break;
                    case "-learn":
                        _useLearning = true;
                        break;
                    case "-nolearn":
                        _useLearning = false;
                        break;
                }
            }
        }


        /// <summary>
        /// Initializes the pipeline after arguments have been processed.
        /// </summary>
        public override void start()
        {
            if (_isPure)
            {
                Console.WriteLine("Symbol recognition test will be pure");
            }
            else
            {
                Console.WriteLine("Symbol recognition test will be impure");
                _pipeline.addStep(RecognitionPipeline.createDefaultClassifier());
                _pipeline.addStep(RecognitionPipeline.createDefaultGrouper());
            }

            _pipeline.addStep(_recognizer);

            if (_useRefinement)
            {
                Console.WriteLine("Symbol recognition test will use the refiner");
                Connector connector = RecognitionPipeline.createDefaultConnector();
                RecognitionInterfaces.IRecognitionStep refiner = RecognitionPipeline.createDefaultRefiner();
                _pipeline.addStep(connector);
                _pipeline.addStep(refiner);
            }
            else
            {
                Console.WriteLine("Symbol recognition test will NOT use the refiner");
            }

            if (_useLearning)
            {
                Console.WriteLine("Symbol recognition test will train the recognizer as it runs");
            }
            else
            {
                Console.WriteLine("Symbol recognition test will NOT train the recognizer as it runs");
            }
        }

        /// <summary>
        /// Execute the SymbolStage
        /// </summary>
        /// <param name="sketch">A mutable sketch</param>
        /// <param name="filename">the name of the file being loaded</param>
        public override void run(Sketch.Sketch sketch, string filename)
        {
            Sketch.Sketch handLabeled = sketch.Clone();
            if (!_isPure)
            {
                sketch.RemoveLabels();
                sketch.resetShapes();
            }


            // Run!
            _pipeline.process(sketch);

            // Assessment
            int numShapes = handLabeled.Shapes.Length;
            int numCorrect = 0;
            int numMisgrouped = 0;
            int numMisrecognized = 0;
            foreach (Sketch.Shape correctShape in handLabeled.Shapes)
            {
                if (!_recognizer.canRecognize(correctShape.Classification))
                {
                    // Decrement the number of shapes so this one doesn't count
                    // against us.
                    numShapes--;
                    continue;
                }

                ShapeType originalType = correctShape.Type;

                Sketch.Shape resultShape = sketch.ShapesL.Find(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); });

                if (resultShape == null)
                {
                    numMisgrouped++;
                    continue;
                }

                ShapeType resultType = resultShape.Type;

                _typesSeen.Add(originalType);
                _typesSeen.Add(resultType);

                ensureKeyExists2D(_confusionMatrix, originalType, resultType);

                // Record stats
                _confusionMatrix[originalType][resultType]++;
                if (originalType == resultType)
                {
                    numCorrect++;
                }
                else 
                {
                    numMisrecognized++;
                    if (_useLearning)
                    {
                        resultShape.Type = correctShape.Type; // not using originalType means we preserve case
                        _recognizer.learnFromExample(resultShape);
                    }
                }
           }
            _results.Add(new int[] {
                _results.Count + 1,
                numShapes,
                numCorrect,
                numMisgrouped,
                numMisrecognized
            });

            Console.WriteLine("   --> Correct recognitions: " + numCorrect + "/" + numShapes);
            Console.WriteLine("   --> Misgrouped: " + numMisgrouped);
            Console.WriteLine("   --> Misrecognized: " + numMisrecognized);
        }

        /// <summary>
        /// Write cached results to a file
        /// </summary>
        /// <param name="tw">place to write to</param>
        /// <param name="path">folder the file is in</param>
        public override void finalize(TextWriter tw, string path)
        {
            // Writes a CSV (comma-separated values) file which can be read by Excel.

            // information parameters
            tw.Write("Arguments: ");
            if (_args != null)
            {
                foreach (string arg in _args)
                {
                    tw.Write(arg + " ");
                }
            }
            else
            {
                tw.Write("(none)");
            }
            tw.WriteLine();
            tw.WriteLine();
            
            // confusion matrix
            List<ShapeType> types = new List<ShapeType>(_typesSeen);
            types.Sort(delegate(ShapeType one, ShapeType two) { return one.ToString().CompareTo(two.ToString()); });

            tw.Write("Columns are original cassifications and rows are actual classifications. (So matrix[col=x][row=y] is how many x's were misclassified as y's.)");
            foreach (ShapeType originalType in types)
            {
                tw.Write("," + originalType);
            } 
            tw.WriteLine();

            foreach (ShapeType resultType in types)
            {
                tw.Write(resultType);
                foreach (ShapeType originalType in types)
                {
                    ensureKeyExists2D(_confusionMatrix, originalType, resultType);
                    tw.Write("," + _confusionMatrix[originalType][resultType]);
                }
                tw.WriteLine();
            }

            tw.WriteLine();
            tw.WriteLine();
            tw.WriteLine();


            // per-test results

            tw.Write("Test Number,");
            tw.Write("Shapes,");
            tw.Write("Correct Classifications,");
            tw.Write("Mis-grouped,");
            tw.Write("Mis-recognized");

            tw.WriteLine();
            foreach (int[] result in _results)
            {
                bool first = true;
                foreach (int value in result)
                {
                    if (!first)
                        tw.Write(",");
                    first = false;
                    tw.Write(value);
                }
                tw.WriteLine();
            }
        }


    }
}
