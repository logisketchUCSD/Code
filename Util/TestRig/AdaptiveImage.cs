using Data;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TestRig
{
    /// <summary>
    /// Tests the adaptive image recognizer
    /// 
    /// Assumes all of the sketches you get are from one user.
    /// </summary>
    class AdaptiveImage : ProcessStage
    {
        #region Internals

        /// <summary>
        /// The percentage of gates that should go into the training set.
        /// </summary>
        const double TRAIN_PERCENTAGE = 0.5;

        /// <summary>
        /// Stores our AIR. Loaded in start();
        /// </summary>
        Recognizers.AdaptiveImageRecognizer _recognizer;

        /// <summary>
        /// The first of the filenames that we run into. Used to keep track of which user we're testing on.
        /// </summary>
        string _filename;

        /// <summary>
        /// The maximum number of templates to allow per gate. Can be changed by command line arguments (-t)
        /// </summary>
        int _maxTemplates = 10;

        /// <summary>
        /// Stores the list of all gates from this user. Once it is filled in run, it should not change.
        /// </summary>
        List<Sketch.Shape> _gates;

        /// <summary>
        /// Maps the number of additional templates added to pairs of percent accuracy and time spent.
        /// (Accuracy is the percent correctly recognized from the testing set.)
        /// </summary>
        Dictionary<int, Tuple<double, TimeSpan>> _results;

        /// <summary>
        /// Maps our templates to whether or not they were correctly recognized in the last iteration.
        /// </summary>
        Dictionary<Sketch.Shape, bool> _trainSet;
        Dictionary<Sketch.Shape, bool> _testSet;
        Dictionary<Sketch.Shape, bool> _alreadySet;

        /// <summary>
        /// Lists of correctly and incorrectly recognized shapes.
        /// </summary>
        List<Sketch.Shape> _right;
        List<Sketch.Shape> _wrong;

        /// <summary>
        /// The number of templates that have been added so far.
        /// </summary>
        int _numTemplatesAdded = 0;

        /// <summary>
        /// The percentage of ALL the gates that were recognized correctly.
        /// </summary>
        double _percentCorrect;

        #endregion

        #region Initialization
        /// <summary>
        /// Constructor for the stage. Initializes all of our member variables.
        /// </summary>
        public AdaptiveImage()
        {
            name = "Adaptive Image Optimization Stage";
            shortname = "airo"; 
            outputFiletype = ".csv";
            _gates = new List<Sketch.Shape>();
            _results = new Dictionary<int, Tuple<double, TimeSpan>>();
            _trainSet = new Dictionary<Sketch.Shape, bool>();
            _testSet = new Dictionary<Sketch.Shape, bool>();
            _alreadySet = new Dictionary<Sketch.Shape, bool>();
            _right = new List<Sketch.Shape>();
            _wrong = new List<Sketch.Shape>();
        }

        /// <summary>
        /// The only argument for this stage is the maximum number of templates to allow per gates.
        /// Should be proceeded by a "-t" flag.
        /// Defaults to 10 templates per gate if value is not specified.
        /// </summary>
        public override void processArgs(string[] args)
        {
            if (args[0] == "-t")
                _maxTemplates = int.Parse(args[1]);
        }

        /// <summary>
        /// Initializes the adpative image recognizer by loading it from file.
        /// </summary>
        public override void start()
        {
            _recognizer = Recognizers.AdaptiveImageRecognizer.Load(
                AppDomain.CurrentDomain.BaseDirectory + @"SubRecognizers\ImageRecognizer\AdaptiveImage.air", _maxTemplates);

            Console.WriteLine("Training set will be " + TRAIN_PERCENTAGE + " of total.");
        }

        /// <summary>
        /// Takes the gates from each file and adds them to a list for later processing.
        /// (We need to have access to all of the gates before we can start the testing process.)
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="filename"></param>
        public override void run(Sketch.Sketch sketch, string filename)
        {
            if (_filename == null)
            {
                _filename = filename;
            }

            foreach (Sketch.Shape shape in sketch.Shapes)
            {
                if (_recognizer.canRecognize(shape.Classification))
                {
                    // Once all of the gates have been added, _gates should not change.
                    _gates.Add(shape);

                    // Add all gates to the testSet initially for the first round of recognition.
                    _testSet.Add(shape, false);
                }
            }
        }

        #endregion

        #region Testing
        /// <summary>
        /// Runs the recognition procedure, then calls the method to write the results to a file.
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="path"></param>
        public override void finalize(TextWriter tw, string path)
        {
            recognize();

            int iterations = 0;

            // This loop should end if it can correctly recognize all of the gates
            // OR if it gets stuck on a certain number of templates. (We force it to
            // stop after 10 tries with the same number of templates.)
            while (_percentCorrect < 1 && iterations < 10)
            {
                DateTime startTime = DateTime.Now;
                split();
                learnOne();
                double accuracy = recognize();
                DateTime endTime = DateTime.Now;

                if (!_results.ContainsKey(_numTemplatesAdded))
                {
                    // make note of the results keeping track of the percent accuracy and the time it took to run
                    _results.Add(_numTemplatesAdded, Tuple.Create(accuracy, endTime - startTime));
                    iterations = 0;
                }
                else
                    iterations++;
            }

            // write the results to the specified file
            writeResults(tw, path);
        }

        /// <summary>
        /// Recognize all shapes in the three Dictionaries, updating the dictionary values and
        /// noting the percent accuracy and percent correct.
        /// </summary>
        /// <returns>The accuracy. (The percent correct from the TESTSET).</returns>
        private double recognize()
        {
            int numCorrect = 0;
            foreach (Sketch.Shape shape in _trainSet.Keys)
                if (recognizeShape(shape))
                    numCorrect++;

            foreach (Sketch.Shape shape in _alreadySet.Keys)
            {
                ShapeType newtype = _recognizer.recognize(shape, null).Type;
                if (newtype == shape.Type)
                    numCorrect++;
            }

            int testCorrect = 0;
            foreach (Sketch.Shape shape in _testSet.Keys)
                if (recognizeShape(shape))
                {
                    numCorrect++;
                    testCorrect++;
                }

            // The percent correct from ALL GATES.
            _percentCorrect = (double)numCorrect / _gates.Count;

            Console.WriteLine("Number of templates added: " + _numTemplatesAdded);
            Console.WriteLine("Percent correct: " + numCorrect + " / " + _gates.Count);
            Console.WriteLine("Percent accuracy: " + testCorrect + " / " + _testSet.Count);

            return (double)testCorrect / _testSet.Count;
        }

        /// <summary>
        /// Recognizes an individual shape and updates the _right and _wrong dictionaries accordingly.
        /// </summary>
        /// <param name="shape"></param>
        /// <returns>Whether the shape was recognized correctly.</returns>
        private bool recognizeShape(Sketch.Shape shape)
        {
            ShapeType newtype = _recognizer.recognize(shape, null).Type;

            if (newtype == shape.Type)
            {
                _right.Add(shape);
                return true;
            }

            _wrong.Add(shape);
            return false;
        }

        /// <summary>
        /// Trains the adaptive image recognizer on one of the incorrectly recognized gates
        /// in the training set.
        /// </summary>
        private void learnOne()
        {
            // Make a list of the incorrectly recognized gates from the training set
            List<Sketch.Shape> wrong = new List<Sketch.Shape>();
            foreach (Sketch.Shape train in _trainSet.Keys)
                if (!_trainSet[train])
                    wrong.Add(train);

            if (wrong.Count == 0)
                return;

            // chose a random gate from your list
            int index = (new Random()).Next(wrong.Count);
            Sketch.Shape learn = wrong[index];

            // learnFromExample requires a shape to have all it's attributes updated by the recognizer
            // (ie. - template used and recognition results)
            // However, we don't want to change the shapes in _gates, so clone it first.
            Sketch.Shape clone = learn.Clone();
            _recognizer.processShape(clone, null);
            clone.Type = learn.Type;

            // Teach the recognizer the correct recognition for the shape, then
            // update dictionaries, etc. accordingly.
            _recognizer.learnFromExample(clone);
            _numTemplatesAdded++;
            _trainSet.Remove(learn);
            _alreadySet.Add(learn, false);
        }

        /// <summary>
        /// Shuffle the lists of correctly and incorrectly recognized shapes, then split them
        /// into a train set and a test set.
        /// </summary>
        private void split()
        {
            shuffleList(ref _wrong);
            shuffleList(ref _right);

            // The number of gates from each recognition set that will go into the training set.
            int wrongSplit = (int)Math.Floor(TRAIN_PERCENTAGE * _wrong.Count);
            int rightSplit = (int)Math.Floor(TRAIN_PERCENTAGE * _right.Count);

            _trainSet.Clear();
            _testSet.Clear();

            // split the incorrectly recognized shapes.
            foreach (Sketch.Shape shape in _wrong.GetRange(0, wrongSplit))
                _trainSet.Add(shape, false);
            foreach (Sketch.Shape shape in _wrong.GetRange(wrongSplit, _wrong.Count - wrongSplit))
                _testSet.Add(shape, false);

            // split the correctly recognized shapes.
            foreach (Sketch.Shape shape in _right.GetRange(0, rightSplit))
                _trainSet.Add(shape, true);
            foreach (Sketch.Shape shape in _right.GetRange(rightSplit, _right.Count - rightSplit))
                _testSet.Add(shape, true);

            _wrong.Clear();
            _right.Clear();
        }

        /// <summary>
        /// Shuffles a list based on the Fisher-Yates shuffling algorithm.
        /// http://en.wikipedia.org/wiki/Fisher-Yates_shuffle
        /// </summary>
        /// <param name="list">The reference to the list to shuffle.</param>
        private void shuffleList(ref List<Sketch.Shape> list)
        {
            Random rng = new Random();
            int n = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                int swapIndex = rng.Next(i, list.Count);
                if (swapIndex != i)
                {
                    Sketch.Shape temp = list[i];
                    list[i] = list[swapIndex];
                    list[swapIndex] = temp;
                }
            }
        }

        /// <summary>
        /// Write the results to the appropriate file.
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="path"></param>
        private void writeResults(TextWriter tw, string path)
        {
            // general information about the tests we ran
            tw.WriteLine("Total number of gates: " + _gates.Count);
            tw.WriteLine("Percentage used for training: " + TRAIN_PERCENTAGE);
            tw.WriteLine("Max templates per gate: " + _maxTemplates);
            tw.WriteLine("First file: " + _filename);

            tw.WriteLine();

            // headers
            tw.Write("Num templates added,");
            tw.Write("Percent accuracy,");
            tw.Write("Time spent");

            tw.WriteLine();

            // our results
            foreach (var result in _results)
                tw.WriteLine(result.Key + "," + result.Value.Item1 + "," + result.Value.Item2);
        }
        #endregion
    }
}