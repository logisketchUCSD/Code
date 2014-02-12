using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Sketch;
using CombinationRecognizer;
using SubRecognizer;
using Domain;
using System.IO;

namespace Recognizers 
{
    /// <summary>
    /// This recognizer is basically a wrapper around the BitmapSymbols.
    /// It has a list of templates (BitmapSymbols) that it attempts to 
    /// find the best match with with you call the Recognize(strokes) 
    /// function.
    /// </summary>
    [Serializable]
    public class AdaptiveImageRecognizer : ImageRecognizer
    {
        #region Internals

        const string myAdaptive = @"SubRecognizers\ImageRecognizer\myAdaptiveImage.air";

        /// <summary>
        /// The number of templates you want to store for each gate.
        /// </summary>
        int numTemplatesPerGate = 10;

        /// <summary>
        /// The amount to subtract from the number of times the template returned a correct
        /// value when the user corrects an error on that template. 
        /// </summary>
        const int errorSubtraction = 10;

        /// <summary>
        /// The number of times each template has been used. Larger numbers are more useful templates.
        /// Negative numbers indicate templates that produce errors.
        /// </summary>
        Dictionary<ShapeType, Dictionary<BitmapSymbol, int>> _templateUsage;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new, empty AdaptiveImageRecognizer
        /// </summary>
        public AdaptiveImageRecognizer()
        {
            _templateUsage = new Dictionary<ShapeType, Dictionary<BitmapSymbol, int>>();
            foreach (ShapeType gate in LogicDomain.Gates)
                _templateUsage.Add(gate, new Dictionary<BitmapSymbol, int>());
        }

        /// <summary>
        /// Creates a new AdaptiveImageRecognizer with the given templates
        /// </summary>
        /// <param name="templates">The list of BitmapSymbols to use as templates</param>
        public AdaptiveImageRecognizer(List<BitmapSymbol> templates)
            : this()
        {
            foreach (BitmapSymbol template in templates)
                _templateUsage[template.SymbolType].Add(template, 0);
        }

        /// <summary>
        /// Creates a new ImageRecognizer trained on a set of labeled data.
        /// </summary>
        /// <param name="data">a list of labeled shapes</param>
        public AdaptiveImageRecognizer(List<Shape> data)
            : this()
        {
            foreach (Shape shape in data)
                Add(shape.Type, shape.SubstrokesL, shape.createBitmap(bitmapSize, bitmapSize, true));
        }

        /// <summary>
        /// Clears all templates from the adaptive image recognizer by deleting the saved file and reloading.
        /// </summary>
        /// <param name="air"></param>
        public override void reset()
        {
            File.Delete(AppDomain.CurrentDomain.BaseDirectory + myAdaptive);
            AdaptiveImageRecognizer air = LoadDefault();

            this._shapesToSymbols = air._shapesToSymbols;
            this._templates = air._templates;
            this._templateUsage = air._templateUsage;
            this.numTemplatesPerGate = air.numTemplatesPerGate;
        }

        #endregion

        #region Interface Functions

        /// <summary>
        /// Update the recognizer to learn from an example shape.
        /// 
        /// Precondition: shape has a valid type
        /// </summary>
        /// <param name="shape">the shape to learn</param>
        public override void learnFromExample(Sketch.Shape shape)
        {
            // If we made a mistake in recognition, make note of that
            BitmapSymbol bs = base.findTemplate(shape.TemplateName);
            if (bs != null && bs.SymbolType != shape.Type)
                _templateUsage[bs.SymbolType][bs] -= errorSubtraction;

            // Update the list of templates accordingly
            removeExample(shape.Type);
            Add(shape.Type, shape.SubstrokesL, shape.createBitmap(bitmapSize, bitmapSize, true));
        }

        /// <summary>
        /// Removes an example from the list of templates based on the values in _templateUsage.
        /// Only removes an example if there are already at least numTemplatesPerGate templates for
        /// the specificed ShapeType.
        /// </summary>
        /// <param name="type">The ShapeType you want to remove an example of. Must be a gate.</param>
        public void removeExample(ShapeType type)
        {
            // first check that you actually want to remove an example
            if (_templateUsage[type].Count < numTemplatesPerGate)
                return;

            // if they're all equally bad, remove the first one of that type in the list
            BitmapSymbol first = null;
            foreach (BitmapSymbol template in _templates)
                if (template.SymbolType == type)
                    first = template;

            // lets us keep track of the worst template we've seen so far
            KeyValuePair<BitmapSymbol, int> worst = new KeyValuePair<BitmapSymbol,int>(first, _templateUsage[type][first]);

            // replace worst if we find a worse template
            foreach (BitmapSymbol template in _templateUsage[type].Keys)
                if (_templateUsage[type][template] < worst.Value)
                    worst = new KeyValuePair<BitmapSymbol, int>(template, _templateUsage[type][template]);

           #if DEBUG
            Console.WriteLine("Removing template " + worst.Key.Name + " which has score " + worst.Value);
           #endif

            // actually remove the template from both members that know about it
            _templates.Remove(worst.Key);
            _templateUsage[type].Remove(worst.Key);
        }

        /// <summary>
        /// Adds a new example to the list of templates.
        /// </summary>
        /// <param name="label">Class name/label for the shape</param>
        /// <param name="strokes">List of strokes in the shape</param>
        public override BitmapSymbol Add(ShapeType label, List<Substroke> strokes, System.Drawing.Bitmap bitmap)
        {
            BitmapSymbol bs = base.Add(label, strokes, bitmap);
            _templateUsage[bs.SymbolType].Add(bs, 0); // If bs.SymbolType is not in the dictionary, just cry.
            return bs;
        }

        #endregion

        #region Recognition

        /// <summary>
        /// Recognizes a shape and updates values in shape (through setRecognitionResults).
        /// Also notes which template was used for template ranking purposes.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="featureSketch"></param>
        /// <returns></returns>
        public override RecognitionInterfaces.RecognitionResult recognize(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            // Gaaghaghagahga
            // C# has one flaw, and I found it:
            // http://www.simple-talk.com/community/blogs/simonc/archive/2010/07/14/93495.aspx
            // In short, we have to cast the result from the base class because that method 
            // must return a generic "RecognitionResult" and cannot return the better 
            // "ImageRecognitionResult," even though doing so would be perfectly type-safe. =(

            ImageRecognitionResult result = (ImageRecognitionResult)base.recognize(shape, featureSketch);
            ShapeType type = result.Type;
            string templateName = result.TemplateName;
            ++_templateUsage[type][base.findTemplate(templateName)]; // increment the number of times that BitmapSymbol has been used
            return result;
        }

        #endregion

        #region Serialization, Saving, and Loading

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public AdaptiveImageRecognizer(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            _templateUsage = (Dictionary<ShapeType, Dictionary<BitmapSymbol, int>>)info.GetValue("templateUsage", typeof(Dictionary<ShapeType, Dictionary<BitmapSymbol, int>>));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("templateUsage", _templateUsage);
        }

        /// <summary>
        /// Saves the AdaptiveImageRecognizer in the designated location.
        /// </summary>
        public override void save()
        {
            Save(AppDomain.CurrentDomain.BaseDirectory + myAdaptive);
        }

        /// <summary>
        /// Calls AdaptiveImageRecognizer.Load(f), where f is the current directory + "SubRecognizers\ImageRecognizer\myAdaptiveImage.air". 
        /// If that fails, it loads the current directory + "SubRecognizers\ImageRecognizer\AdaptiveImage.air" instead.
        /// </summary>
        /// <returns>An AdaptiveImageRecognzier</returns>
        public static AdaptiveImageRecognizer LoadDefault()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;

            // In some cases (for instance, when running tests) the directory
            // does not contain a trailing '\' like we expect.
            if (directory[directory.Length - 1] != '\\')
                directory += '\\';

            AdaptiveImageRecognizer recognizer;
            try
            {
                // Try using the custom, user-specific combo recognizer.
                string trainedRecognizer = directory + myAdaptive;
                Console.WriteLine("Using user-specific combo recognizer at " + trainedRecognizer);
                recognizer = Load(trainedRecognizer);
            }
            catch
            {
                // Use the default combo recognizer if necessary.
                string trainedRecognizer = directory + @"SubRecognizers\ImageRecognizer\AdaptiveImage.air";
                Console.WriteLine("User-specific recognizer not found! Falling back to default combo recognizer at " + trainedRecognizer);
                recognizer = Load(trainedRecognizer);
            }
            return recognizer;
        }

        /// <summary>
        /// Loads a previously saved AdaptiveImageRecognizer from the given filename, 
        /// using the deserialization constructor
        /// </summary>
        /// <param name="filename">Filename which is the saved AdaptiveImageRecognizer</param>
        /// <returns>Re-instantiated AdaptiveImageRecognzier</returns>
        public new static AdaptiveImageRecognizer Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            AdaptiveImageRecognizer image = (AdaptiveImageRecognizer)bformatter.Deserialize(stream);
            stream.Close();

            #if DEBUG
            Console.WriteLine("Adaptive image recognizer loaded.");
            #endif

            return image;
        }

        /// <summary>
        /// Loads a previously saved AdaptiveImageRecognizer from the given filename,
        /// using the deserialization constructor.
        /// 
        /// This function is used primarily for testing, because the number of templates
        /// per gate should be fixed when using the program. This allows you to change it
        /// if you so desire.
        /// </summary>
        /// <param name="filename">Filename which is the saved AdaptiveImageRecognizer</param>
        /// <param name="NumTemplatesPerGate">The maximum number of templates per gate to store</param>
        /// <returns>Re-instantiated AdaptiveImageRecognizer</returns>
        public static AdaptiveImageRecognizer Load(string filename, int NumTemplatesPerGate)
        {
            AdaptiveImageRecognizer adaptive = Load(filename);
            adaptive.numTemplatesPerGate = NumTemplatesPerGate;
            return adaptive;
        }

        #endregion
    }
}
