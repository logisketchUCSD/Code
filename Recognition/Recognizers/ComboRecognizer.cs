using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Sketch;
using Utilities.NaiveBayes;
using SubRecognizer;
using Recognizers;

namespace CombinationRecognizer
{
    /// <summary>
    /// The ComboRecognizer is a Recognizer designed to recognize gates.
    /// It uses a Naive Bayesian Classifier to recognize a shape (or 
    /// list of substrokes). The input for the classifier mostly come 
    /// from shape or stroke recognizers. Currently it uses: 
    ///     -the number of strokes in the shape 
    ///     -what the top 5 results from the ImageRecognizer are
    ///     -what the RubineRecognizer thinks each stroke is
    /// It is also set up to include these features (however they seem to 
    /// to be detrimental to classification and are therefore left out):
    ///     -what the DollarRecognizer thinks each stroke is
    ///     -what the DollarRecognizer thinks each stroke is using Average Templates
    ///         -the Average Templates are created by averaging the points
    ///             in a the DollarTemplates for a given class
    ///     -what the ZernikeMomentRecognizer thinks the shape is
    /// 
    /// The way these features are made from the recognizer is by counting 
    /// then number of occurances, e.g. one feature is Image_AND, which 
    /// means: how many templates in the top 5 from the ImageRecognizer 
    /// have the label "AND". Another example: Rubine_FrontArc is the number 
    /// of strokes the RubineRecognizer thinks are FrontArcs in the shape.
    /// 
    /// 
    /// Example Usage:
    /// You have already trained each of the individual recognizers (Rubine, Dollar, 
    /// Zernike, and Image), you have also trained the NaiveBayes classifier. You 
    /// instantiate a new ComboRecognizer (and you name it 'combo') using all 5 of 
    /// those trained recognizers/classifiers. Now you have a shape containing 
    /// some substrokes that you would like to recognize. You call combo.Recognize(shape);
    /// This returns a list of the most likely symbols (sorted) with their associated 
    /// probabilities. 
    /// For instance, say the 'shape' you sent in was an AND gate, your results may be 
    ///     1: AND - 0.985, 
    ///     2: OR - 0.010, 
    ///     3: NAND - 0.005, 
    ///     4: LabelBox - 0.005, 
    ///     5: NOR - 0.000, 
    ///     6: XOR - 0.000, 
    ///     7: NOTBUBBLE - 0.000,
    ///     8: NOT - 0.000
    /// </summary>
    [Serializable]
    public class ComboRecognizer : RecognitionInterfaces.Recognizer, ISerializable
    {
        #region Member Variables

        /// <summary>
        /// Pre-trained Rubine Recognizer
        /// Works at the stroke level.
        /// </summary>
        RubineRecognizer _rubine;

        /// <summary>
        /// Pre-trained Dollar Recognizer (both regular and "Average")
        /// Works at the stroke level.
        /// </summary>
        DollarRecognizer _dollar;

        /// <summary>
        /// Pre-trained Zernike Recognizer
        /// Works at the shape level.
        /// </summary>
        ZernikeMomentRecognizer _zernike;

        /// <summary>
        /// Pre-trained Image Recognizer
        /// Works at the shape level.
        /// </summary>
        ImageRecognizer _image;

        /// <summary>
        /// Naive Bayesian Classifier which combines
        /// the outputs from the different recognizers.
        /// </summary>
        NaiveBayesUpdateable _comboClassifier;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor, avoid using unless you're about to replace it
        /// </summary>
        public ComboRecognizer()
            :this(new RubineRecognizer(), new DollarRecognizer(), new ZernikeMomentRecognizer(), new ImageRecognizer())
        {
        }

        /// <summary>
        /// Constructor which takes in pre-trained recognizers
        /// </summary>
        /// <param name="rubine">Pre-Trained Rubine Recognizer</param>
        /// <param name="dollar">Pre-Trained Dollar Recognizer</param>
        /// <param name="zernike">Pre-Trained Zernike Recognizer</param>
        /// <param name="image">Pre-Trained Image Recognizer</param>
        public ComboRecognizer(RubineRecognizer rubine, DollarRecognizer dollar, ZernikeMomentRecognizer zernike, ImageRecognizer image)
            :this(rubine, dollar, zernike, image, new NaiveBayesUpdateable())
        {
        }

        /// <summary>
        /// Constructor which takes in pre-trained recognizers, as well
        /// as a pre-trained Classifier (Naive Bayes)
        /// </summary>
        /// <param name="rubine">Pre-Trained Rubine Recognizer</param>
        /// <param name="dollar">Pre-Trained Dollar Recognizer</param>
        /// <param name="zernike">Pre-Trained Zernike Recognizer</param>
        /// <param name="image">Pre-Trained Image Recognizer</param>
        /// <param name="classifier">Pre-Trained NaiveBayes Classifier</param>
        public ComboRecognizer(RubineRecognizer rubine, DollarRecognizer dollar, ZernikeMomentRecognizer zernike, ImageRecognizer image, NaiveBayesUpdateable classifier)
        {
            _rubine = rubine;
            _dollar = dollar;
            _zernike = zernike;
            _image = image;
            _comboClassifier = classifier;
        }

        #endregion

        #region Recognition

        /// <summary>
        /// The combo recognizer is only for gates.
        /// </summary>
        /// <param name="classification">the classification to check</param>
        /// <returns>true if classification is "Gate"</returns>
        public override bool canRecognize(string classification)
        {
            return classification == LogicDomain.GATE_CLASS;
        }

        /// <summary>
        /// Recognize what gate a shape is using the ComboRecgonizer.
        /// </summary>
        /// <param name="shape">The shape to recognize (should be a gate)</param>
        /// <param name="featureSketch">the featuresketch to use</param>
        /// <returns>the most probable type of the shape</returns>
        public override RecognitionInterfaces.RecognitionResult recognize(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            // Initialize variables to store results in
            ShapeType bestType = new ShapeType();
            float bestProbability = 0F;
            double bestOrientation;

            // Recognize the shape and pick out the best option
            Dictionary<ShapeType, double> alternativeTypes = Recognize(shape, out bestOrientation);
            foreach (KeyValuePair<ShapeType, double> pair in alternativeTypes)
            {
                ShapeType type = pair.Key;
                double prob = pair.Value;
                if (prob > bestProbability)
                {
                    bestType = type;
                    bestProbability = (float)prob;
                }
            }

            return new RecognitionInterfaces.RecognitionResult(bestType, bestProbability, bestOrientation);
        }

        /// <summary>
        /// Recognizes a shape, returns a ranked list of likely candidates
        /// </summary>
        /// <param name="shape">Shape containing Substrokes to be recognized</param>
        /// <returns>Ranked list of likely candidates with their "probabilities"</returns>
        public Dictionary<ShapeType, double> Recognize(Shape shape, out double believedOrientation)
        {
            Dictionary<string, object> features = GetIndRecognitionResults(shape, out believedOrientation);

            Dictionary<ShapeType, double> results = _comboClassifier.Classify(features);
            
            return results;
        }


        /// <summary>
        /// Recognize this shape and return only the best result's name
        /// </summary>
        /// <param name="shape">Shape to be recognized</param>
        /// <param name="probability">"Probability" that this match is good</param>
        /// <returns>Name of the best-matching class</returns>
        public ShapeType RecognizeBest(Shape shape, out double probability)
        {
            double orientation;
            Dictionary<ShapeType, double> results = Recognize(shape, out orientation);

            if (results.Count == 0)
            {
                probability = 0.0;
                return new ShapeType();
            }
            
            List<ShapeType> names = new List<ShapeType>(results.Keys);
            List<double> probs = new List<double>(results.Values);
            probability = probs[0];
            return names[0];
        }

        /// <summary>
        /// Used to classify a set of features using the Naive Bayes classifier.
        /// Probably would never be used in typical usage, however may be useful
        /// when testing on previously collected feature data.
        /// </summary>
        /// <param name="features">List of features - Names to Values</param>
        /// <returns>Best matching class</returns>
        public ShapeType Classify(Dictionary<string, object> features)
        {
            // The results are sorted
            Dictionary<ShapeType, double> results = _comboClassifier.Classify(features);

            List<ShapeType> labels = new List<ShapeType>(results.Keys);
            if (labels.Count > 0)
                return labels[0];
            else
                return new ShapeType();
        }

        public Dictionary<ShapeType, double> GetClassificationDistributions(Dictionary<string, object> features)
        {
            return _comboClassifier.Classify(features);
        }

        public Dictionary<string, object> GetIndRecognitionResults(Shape shape, out double believedOrientation)
        {
            List<Substroke> strokes = shape.SubstrokesL;
            int shapeHash = shape.GetHashCode();

            int numStrokes = strokes.Count;

            List<string> DollarAvgResults = new List<string>(numStrokes);
            List<string> DollarResults = new List<string>(numStrokes);
            List<string> RubineResults = new List<string>(numStrokes);
            List<ShapeType> ImageResults;
            string ZernikeResult;

            ZernikeResult = _zernike.Recognize(strokes);
            ImageResults = _image.Recognize(strokes);

            foreach (Substroke s in strokes)
            {
                int hash = s.Id.GetHashCode();
                string res;

                res = _dollar.RecognizeAverage(s);
                DollarAvgResults.Add(res);

                res = _dollar.Recognize(s);
                DollarResults.Add(res);

                res = _rubine.Recognize(s);
                RubineResults.Add(res);
            }

            Dictionary<string, object> features = GetFeatures(numStrokes, ZernikeResult, ImageResults, RubineResults, DollarAvgResults, DollarResults);

            believedOrientation = 0.0;

            return features;
        }

        #endregion

        #region Training

        /// <summary>
        /// Update the Naive Bayes classifier to learn from an example shape.
        /// </summary>
        /// <param name="shape"></param>
        public void Learn(Sketch.Shape shape)
        {
            double believedOrientation;
            Dictionary<string, object> features = GetIndRecognitionResults(shape, out believedOrientation);
            
            // Since the combo classifier is trained with such a large
            // number of training examples (1600+), each additional 
            // learning example has an extremely small effect.
            // However, we want the error-corrected learning to have a
            // more meaningful effect. So, we add the same example
            // multiple times to the bayes classifier.

            for (int i=0; i<5; i++) // MAGIC NUMBER
                _comboClassifier.AddExample(shape.Type, features);

            _comboClassifier.UpdateClassifier();
        }

        /// <summary>
        /// Trains the Naive Bayes classifier using pre-computed data. This data
        /// is in the form of a List which contains key-value-pairs of intended class
        /// to a dictionary containing feature values indexed by feature names. 
        /// </summary>
        /// <param name="featureNames">Names of all the features so that their values 
        /// can be looked up in the dictionary</param>
        /// <param name="data">All the pre-computed feature data</param>
        public void TrainCombo(List<string> featureNames, List<KeyValuePair<ShapeType, Dictionary<string, object>>> data)
        {
            NaiveBayesUpdateable bayes = new NaiveBayesUpdateable(featureNames);

            foreach (KeyValuePair<ShapeType, Dictionary<string, object>> example in data)
                bayes.AddExample(example.Key, example.Value);

            bayes.UpdateClassifier();

            _comboClassifier = bayes;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates a dictionary of the feature names to feature values.
        /// Uses the string outputs from each of the recognizers to build this dictionary.
        /// </summary>
        /// <param name="numStrokes">Number of strokes in the shape</param>
        /// <param name="zernike">The result from the Zernike recognizer</param>
        /// <param name="image">The result from the Image recognizer</param>
        /// <param name="rubine">The result from the Rubine recognizer</param>
        /// <param name="dollarAvg">The result from the Dollar recognizer, using "average" templates</param>
        /// <param name="dollar">The result from the Dollar recognizer</param>
        /// <returns>Dictionary of feature names to feature values</returns>
        public static Dictionary<string, object> GetFeatures(int numStrokes,
            string zernike, List<ShapeType> image, List<string> rubine,
            List<string> dollarAvg, List<string> dollar)
        {
            Dictionary<string, object> features = new Dictionary<string, object>();

            features.Add("NumStrokes", (object)numStrokes);

            // Zernike reco result - IN USE
            features.Add("Zernike", (object)zernike);

            // Image reco results - IN USE
            #region Get image counts and assign values - MAKE THIS BETTER

            foreach (ShapeType gate in LogicDomain.Gates)
            {
                if (image.Contains(gate))
                    features.Add("Image_" + gate.Name, 1);
                else
                    features.Add("Image_" + gate.Name, 0);
            }
            #endregion

            // Rubine reco results - IN USE
            #region Get rubine counts and assign values
            int rnumBackLine = 0;
            int rnumFrontArc = 0;
            int rnumBackArc = 0;
            int rnumTopArc = 0;
            int rnumBottomArc = 0;
            int rnumBubble = 0;
            int rnumTopLine = 0;
            int rnumBottomLine = 0;
            int rnumTriangle = 0;
            int rnumGreaterThan = 0;
            int rnumTouchUp = 0;
            int rnumJunk = 0;
            int rnumNot_V = 0;
            int rnumNot_Hat = 0;
            int rnumEntire_OR = 0;
            int rnumEntire_AND = 0;
            foreach (string shape in rubine)
            {
                switch (shape)
                {
                    case ("BackLine"):
                        rnumBackLine++;
                        break;
                    case ("FrontArc"):
                        rnumFrontArc++;
                        break;
                    case ("BackArc"):
                        rnumBackArc++;
                        break;
                    case ("TopArc"):
                        rnumTopArc++;
                        break;
                    case ("BottomArc"):
                        rnumBottomArc++;
                        break;
                    case ("Bubble"):
                        rnumBubble++;
                        break;
                    case ("TopLine"):
                        rnumTopLine++;
                        break;
                    case ("BottomLine"):
                        rnumBottomLine++;
                        break;
                    case ("Triangle"):
                        rnumTriangle++;
                        break;
                    case ("GreaterThan"):
                        rnumGreaterThan++;
                        break;
                    case ("TouchUp"):
                        rnumTouchUp++;
                        break;
                    case ("Junk"):
                        rnumJunk++;
                        break;
                    case ("Not_V"):
                        rnumNot_V++;
                        break;
                    case ("Not_Hat"):
                        rnumNot_Hat++;
                        break;
                    case ("Entire_OR"):
                        rnumEntire_OR++;
                        break;
                    case ("Entire_AND"):
                        rnumEntire_AND++;
                        break;
                }
            }
            
            features.Add("Rubine_BackLine", (object)rnumBackLine);
            features.Add("Rubine_FrontArc", (object)rnumFrontArc);
            features.Add("Rubine_BackArc", (object)rnumBackArc);
            features.Add("Rubine_TopArc", (object)rnumTopArc);
            features.Add("Rubine_BottomArc", (object)rnumBottomArc);
            features.Add("Rubine_Bubble", (object)rnumBubble);
            features.Add("Rubine_TopLine", (object)rnumTopLine);
            features.Add("Rubine_BottomLine", (object)rnumBottomLine);
            features.Add("Rubine_Triangle", (object)rnumTriangle);
            features.Add("Rubine_GreaterThan", (object)rnumGreaterThan);
            features.Add("Rubine_TouchUp", (object)rnumTouchUp);
            features.Add("Rubine_Junk", (object)rnumJunk);
            features.Add("Rubine_Not_V", (object)rnumNot_V);
            features.Add("Rubine_Not_Hat", (object)rnumNot_Hat);
            features.Add("Rubine_Entire_OR", (object)rnumEntire_OR);
            features.Add("Rubine_Entire_AND", (object)rnumEntire_AND);
            #endregion

            // Dollar reco results - IN USE
            #region Get dollar results and assign values
            // Dollar -- Unused currently
            // Avg Dollar reco results
            #region Get avg dollar counts
            int dAvgnumBackLine = 0;
            int dAvgnumFrontArc = 0;
            int dAvgnumBackArc = 0;
            int dAvgnumTopArc = 0;
            int dAvgnumBottomArc = 0;
            int dAvgnumBubble = 0;
            int dAvgnumTopLine = 0;
            int dAvgnumBottomLine = 0;
            int dAvgnumTriangle = 0;
            int dAvgnumGreaterThan = 0;
            int dAvgnumTouchUp = 0;
            int dAvgnumJunk = 0;
            int dAvgnumNot_V = 0;
            int dAvgnumNot_Hat = 0;
            int dAvgnumEntire_OR = 0;
            int dAvgnumEntire_AND = 0;
            foreach (string shape in dollar)
            {
                switch (shape)
                {
                    case ("BackLine"):
                        dAvgnumBackLine++;
                        break;
                    case ("FrontArc"):
                        dAvgnumFrontArc++;
                        break;
                    case ("BackArc"):
                        dAvgnumBackArc++;
                        break;
                    case ("TopArc"):
                        dAvgnumTopArc++;
                        break;
                    case ("BottomArc"):
                        dAvgnumBottomArc++;
                        break;
                    case ("Bubble"):
                        dAvgnumBubble++;
                        break;
                    case ("TopLine"):
                        dAvgnumTopLine++;
                        break;
                    case ("BottomLine"):
                        dAvgnumBottomLine++;
                        break;
                    case ("Triangle"):
                        dAvgnumTriangle++;
                        break;
                    case ("GreaterThan"):
                        dAvgnumGreaterThan++;
                        break;
                    case ("TouchUp"):
                        dAvgnumTouchUp++;
                        break;
                    case ("Junk"):
                        dAvgnumJunk++;
                        break;
                    case ("Not_V"):
                        dAvgnumNot_V++;
                        break;
                    case ("Not_Hat"):
                        dAvgnumNot_Hat++;
                        break;
                    case ("Entire_OR"):
                        dAvgnumEntire_OR++;
                        break;
                    case ("Entire_AND"):
                        dAvgnumEntire_AND++;
                        break;
                }
            }
            #endregion
            features.Add("AvgDollar_BackLine", (object)dAvgnumBackLine);
            features.Add("AvgDollar_FrontArc", (object)dAvgnumFrontArc);
            features.Add("AvgDollar_BackArc", (object)dAvgnumBackArc);
            features.Add("AvgDollar_TopArc", (object)dAvgnumTopArc);
            features.Add("AvgDollar_BottomArc", (object)dAvgnumBottomArc);
            features.Add("AvgDollar_Bubble", (object)dAvgnumBubble);
            features.Add("AvgDollar_TopLine", (object)dAvgnumTopLine);
            features.Add("AvgDollar_BottomLine", (object)dAvgnumBottomLine);
            features.Add("AvgDollar_Triangle", (object)dAvgnumTriangle);
            features.Add("AvgDollar_GreaterThan", (object)dAvgnumGreaterThan);
            features.Add("AvgDollar_TouchUp", (object)dAvgnumTouchUp);
            features.Add("AvgDollar_Junk", (object)dAvgnumJunk);
            features.Add("AvgDollar_Not_V", (object)dAvgnumNot_V);
            features.Add("AvgDollar_Not_Hat", (object)dAvgnumNot_Hat);
            features.Add("AvgDollar_Entire_OR", (object)dAvgnumEntire_OR);
            features.Add("AvgDollar_Entire_AND", (object)dAvgnumEntire_AND);

            // Dollar reco results
            #region Get dollar counts
            int dnumBackLine = 0;
            int dnumFrontArc = 0;
            int dnumBackArc = 0;
            int dnumTopArc = 0;
            int dnumBottomArc = 0;
            int dnumBubble = 0;
            int dnumTopLine = 0;
            int dnumBottomLine = 0;
            int dnumTriangle = 0;
            int dnumGreaterThan = 0;
            int dnumTouchUp = 0;
            int dnumJunk = 0;
            int dnumNot_V = 0;
            int dnumNot_Hat = 0;
            int dnumEntire_OR = 0;
            int dnumEntire_AND = 0;
            foreach (string shape in dollar)
            {
                switch (shape)
                {
                    case ("BackLine"):
                        dnumBackLine++;
                        break;
                    case ("FrontArc"):
                        dnumFrontArc++;
                        break;
                    case ("BackArc"):
                        dnumBackArc++;
                        break;
                    case ("TopArc"):
                        dnumTopArc++;
                        break;
                    case ("BottomArc"):
                        dnumBottomArc++;
                        break;
                    case ("Bubble"):
                        dnumBubble++;
                        break;
                    case ("TopLine"):
                        dnumTopLine++;
                        break;
                    case ("BottomLine"):
                        dnumBottomLine++;
                        break;
                    case ("Triangle"):
                        dnumTriangle++;
                        break;
                    case ("GreaterThan"):
                        dnumGreaterThan++;
                        break;
                    case ("TouchUp"):
                        dnumTouchUp++;
                        break;
                    case ("Junk"):
                        dnumJunk++;
                        break;
                    case ("Not_V"):
                        dnumNot_V++;
                        break;
                    case ("Not_Hat"):
                        dnumNot_Hat++;
                        break;
                    case ("Entire_OR"):
                        dnumEntire_OR++;
                        break;
                    case ("Entire_AND"):
                        dnumEntire_AND++;
                        break;
                }
            }
            #endregion
            features.Add("Dollar_BackLine", (object)dnumBackLine);
            features.Add("Dollar_FrontArc", (object)dnumFrontArc);
            features.Add("Dollar_BackArc", (object)dnumBackArc);
            features.Add("Dollar_TopArc", (object)dnumTopArc);
            features.Add("Dollar_BottomArc", (object)dnumBottomArc);
            features.Add("Dollar_Bubble", (object)dnumBubble);
            features.Add("Dollar_TopLine", (object)dnumTopLine);
            features.Add("Dollar_BottomLine", (object)dnumBottomLine);
            features.Add("Dollar_Triangle", (object)dnumTriangle);
            features.Add("Dollar_GreaterThan", (object)dnumGreaterThan);
            features.Add("Dollar_TouchUp", (object)dnumTouchUp);
            features.Add("Dollar_Junk", (object)dnumJunk);
            features.Add("Dollar_Not_V", (object)dnumNot_V);
            features.Add("Dollar_Not_Hat", (object)dnumNot_Hat);
            features.Add("Dollar_Entire_OR", (object)dnumEntire_OR);
            features.Add("Dollar_Entire_AND", (object)dnumEntire_AND);
            #endregion

            return features;
        }

        #endregion

        #region Getters/Setters

        public RubineRecognizer Rubine
        {
            get { return _rubine; }
            set { _rubine = value; }
        }

        public DollarRecognizer Dollar
        {
            get { return _dollar; }
            set { _dollar = value; }
        }

        public ZernikeMomentRecognizer Zernike
        {
            get { return _zernike; }
            set { _zernike = value; }
        }

        public ImageRecognizer Image
        {
            get { return _image; }
            set { _image = value; }
        }

        public NaiveBayesUpdateable ComboClassifier
        {
            get { return _comboClassifier; }
            set { _comboClassifier = value; }
        }

        #endregion

        #region Serialization, Saving, and Loading

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public ComboRecognizer(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            _dollar = (DollarRecognizer)info.GetValue("dollar", typeof(DollarRecognizer));
            _image = (ImageRecognizer)info.GetValue("image", typeof(ImageRecognizer));
            _rubine = (RubineRecognizer)info.GetValue("rubine", typeof(RubineRecognizer));
            _zernike = (ZernikeMomentRecognizer)info.GetValue("zernike", typeof(ZernikeMomentRecognizer));
            _comboClassifier = (NaiveBayesUpdateable)info.GetValue("comboClassifier", typeof(NaiveBayesUpdateable));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("dollar", _dollar);
            info.AddValue("image", _image);
            info.AddValue("rubine", _rubine);
            info.AddValue("zernike", _zernike);
            info.AddValue("comboClassifier", _comboClassifier);
        }

        public override void save()
        {
            Save(AppDomain.CurrentDomain.BaseDirectory + @"Trained Recognizers\MyCombo.cru");
        }
        
        /// <summary>
        /// Serializes the object and saves it to the specified filename
        /// </summary>
        /// <param name="filename">Filename to save the object as</param>
        public void Save(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Create);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();

            Console.WriteLine("Saved combo recognizer to " + filename);
        }

        /// <summary>
        /// Loads a previously saved ComboRecognizer from the given filename, 
        /// using the deserialization constructor
        /// </summary>
        /// <param name="filename">Filename which is the saved ComboRecognizer</param>
        /// <returns>Re-instantiated ComboRecognzier</returns>
        public static ComboRecognizer Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            ComboRecognizer combo = (ComboRecognizer)bformatter.Deserialize(stream);
            stream.Close();

            Console.WriteLine("Loaded combo recognizer from " + filename);

            return combo;
        }

        /// <summary>
        /// Calls ComboRecognizer.Load(f), where f is the current directory + "Trained Recognizers\MyCombo.cru". If that fails, it loads
        /// the current directory + "Trained Recognizers\Combo.cru" instead.
        /// </summary>
        /// <returns>A ComboRecognzier</returns>
        public static ComboRecognizer LoadDefault()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            ComboRecognizer recognizer;
            try
            {
                // Try using the custom, user-specific combo recognizer.
                string trainedRecognizer = directory + @"Trained Recognizers\MyCombo.cru";
                Console.WriteLine("Using user-specific combo recognizer at " + trainedRecognizer);
                recognizer = Load(trainedRecognizer);
            }
            catch
            {
                // Use the default combo recognizer if necessary.
                string trainedRecognizer = directory + @"Trained Recognizers\Combo.cru";
                Console.WriteLine("User-specific recognizer not found! Falling back to default combo recognizer at " + trainedRecognizer);
                recognizer = Load(trainedRecognizer);
            }
            return recognizer;
        }

        #endregion
    }
}
