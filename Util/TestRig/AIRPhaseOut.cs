using Data;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestRig
{
    class AIRPhaseOut : ProcessStage
    {
        Recognizers.AdaptiveImageRecognizer _recognizer;

        //The maximum number of templates that you want to add
        int _maxTemplates = 100;

        /*This number does not include the templates that you already trained
         * the AIR on!
         */
        int _numTemplates = 0;

        string _filename;

        //This number is the percentage of templates that you want in your
        //testing set
        //Starts at 1:1 ratio for training:testing
        double _tempPercent = 0.5;

        Dictionary<int, double> _results;

        //A comprehensive list of the gates in the sketch
        //Can probably be removed once the other four lists are in place
        List<Sketch.Shape> _gates;

        //A list of the gates that were recognized correctly
        List<Sketch.Shape> _right = new List<Sketch.Shape>();

        //A list of the gates that were recognized inccorrectly
        List<Sketch.Shape> _wrong = new List<Sketch.Shape>();

        //A list of the gates that we will test on
        List<Sketch.Shape> _test = new List<Sketch.Shape>();

        //A list of the gates that we will train on
        List<Sketch.Shape> _train = new List<Sketch.Shape>();

        public AIRPhaseOut()
        {
            name = "Adaptive Image Phase Out";
            shortname = "aipo";
            outputFiletype = ".csv";
            _gates = new List<Sketch.Shape>();
            _results = new Dictionary<int, double>();
        }

        public override void processArgs(string[] args)
        {
            if (args[0] == "-t")
                _maxTemplates = int.Parse(args[1]);
        }

        public override void start()
        {
            _recognizer = Recognizers.AdaptiveImageRecognizer.Load(
                AppDomain.CurrentDomain.BaseDirectory + @"SubRecognizers\ImageRecognizer\AdaptiveImage.air", _maxTemplates);
        }

        public override void run(Sketch.Sketch sketch, string filename)
        {
            if (_filename == null)
                _filename = filename;

            foreach (Sketch.Shape shape in sketch.Shapes)
                if (_recognizer.canRecognize(shape.Classification))
                    _gates.Add(shape);
        }

        public override void finalize(System.IO.TextWriter handle, string path)
        {
            foreach (Sketch.Shape shape in _gates)
            {
                if (recognizeShape(shape))
                    _right.Add(shape);
                else
                    _wrong.Add(shape);
            }

            while (_numTemplates < _maxTemplates)
            {

                //We need to add some wront templates to the train set, and others to the
                //test set.  This is the percentage that is added to the testing set.
                _tempPercent = 0.5;

                //The loop only stops when there are no more wrongly recognized shapes anywhere.
                if (Math.Ceiling(_wrong.Count * _tempPercent) > _wrong.Count)
                    break;

                _train.RemoveRange(0, _train.Count);
                _test.RemoveRange(0, _test.Count);
                shuffleList(ref _right);
                shuffleList(ref _wrong);
                //add all of the "right" list to the test set
                //this is because having a "right" template in the
                //train set does us no good.
                for (int i = 0; i < _right.Count ; i++)
                    _test.Add(_right[i]);
                //add a percentage of the "wrong" list to the test set
                for (int i = 0; i < _wrong.Count * _tempPercent; i++)
                    _test.Add(_wrong[i]);
                
                //add a percentage of the "wrong" list to the train set
                for (int i = (int) Math.Ceiling(_wrong.Count * _tempPercent); i < _wrong.Count; i++)
                    _train.Add(_wrong[i]);

                if (_train.Count == 0)
                    break;
                
                //just for good measure, to make sure they're random
                shuffleList(ref _test);
                shuffleList(ref _train);

                #region Learn One Gate
                // chose a random gate from your list

                Sketch.Shape learn = new Sketch.Shape();
                int index = (new Random()).Next(_train.Count-1);
                learn = _train[index];

                //We don't want to test on what we train on, and
                //we don't want to train on what we already have
                //trained on.
                _gates.Remove(learn);

                // learnFromExample requires a shape to have all it's attributes updated by the recognizer
                // (ie. - template used and recognition results)
                // However, we don't want to change the shapes in _gates, so clone it first.
                Sketch.Shape clone = learn.Clone();
                _recognizer.processShape(clone, null);
                clone.Type = learn.Type;

                // Teach the recognizer the correct recognition for the shape, then
                // update dictionaries, etc. accordingly.
                _recognizer.learnFromExample(clone);
                _numTemplates++;
                #endregion

                

                // We empty the right and wrong lists so that we can refill them with
                // new recognition results based on the adaptation
                _right.RemoveRange(0, _right.Count);
                _wrong.RemoveRange(0, _wrong.Count);

                foreach (Sketch.Shape shape in _gates)
                {
                    if (recognizeShape(shape))
                        _right.Add(shape);
                    else
                        _wrong.Add(shape);
                }

                if (_right.Count > _test.Count)
                    break;

                _results.Add(_numTemplates, (double)_right.Count / (double)_test.Count);

                if (_right.Count == _test.Count)
                    break;
                
            }

            #region Write Results
            // general information about the tests we ran
            handle.WriteLine("Total number of gates: " + _gates.Count);
            handle.WriteLine("Max templates per gate: " + _maxTemplates);
            handle.WriteLine("First file: " + _filename);

            handle.WriteLine();

            // headers
            handle.Write("Num templates added,");
            handle.Write("Percent accuracy,");

            handle.WriteLine();

            // our results
            foreach (var result in _results)
                handle.WriteLine(result.Key + "," + result.Value);
            #endregion
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
                return true;
            return false;
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
       
    }
}
