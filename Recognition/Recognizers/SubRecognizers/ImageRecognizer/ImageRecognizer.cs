using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.Serialization;
using Sketch;
using CombinationRecognizer;
using SubRecognizer;
using Domain;
using Data;

namespace Recognizers
{

    /// <summary>
    /// This recognizer is basically a wrapper around the BitmapSymbols.
    /// It has a list of templates (BitmapSymbols) that it attempts to 
    /// find the best match with with you call the Recognize(strokes) 
    /// function.
    /// 
    ///  See http://www.andrew.cmu.edu/user/lkara/publications/kara_stahovich_CG2005.pdf
    /// </summary>
    [Serializable]
    public class ImageRecognizer : RecognitionInterfaces.ExtendedRecognizer, ISerializable, RecognitionInterfaces.IOrienter
    {

        #region Internals

        /// <summary>
        /// The desired number of recognitions to return.
        /// </summary>
        protected const int numRecognitions = 5;

        /// <summary>
        /// The length of one side of the bitmap of this symbol to create.
        /// </summary>
        protected const int bitmapSize = 100;

        /// <summary>
        /// List of the templates to compare to
        /// </summary>
        protected List<BitmapSymbol> _templates;

        /// <summary>
        /// Caches bitmap symbols for shapes.
        /// </summary>
        protected SmartCache<Shape, BitmapSymbol> _shapesToSymbols;

        /// <summary>
        ///  Turn on if you want to print debug info
        /// </summary>
        private bool debug = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new, empty AdaptiveImageRecognizer
        /// </summary>
        public ImageRecognizer()
            : this(new List<BitmapSymbol>())
        {
        }

        /// <summary>
        /// Creates a new ImageRecognizer trained on a set of labeled data.
        /// </summary>
        /// <param name="data">a list of labeled shapes</param>
        public ImageRecognizer(List<Shape> data)
            : this()
        {
            foreach (Shape shape in data)
                Add(shape.Type, shape.SubstrokesL, shape.createBitmap(bitmapSize, bitmapSize, true));
        }

        /// <summary>
        /// Creates a new AdaptiveImageRecognizer with the given templates
        /// </summary>
        /// <param name="templates"></param>
        public ImageRecognizer(List<BitmapSymbol> templates)
        {
            _shapesToSymbols = new SmartCache<Shape, BitmapSymbol>(createNewBitmapSymbolForShape);
            _templates = templates;
        }

        #endregion

        #region Interface Functions

        /// <summary>
        /// Adds a new example to the list of templates.
        /// </summary>
        /// <param name="label">Class name/label for the shape</param>
        /// <param name="strokes">List of strokes in the shape</param>
        public virtual BitmapSymbol Add(ShapeType label, List<Substroke> strokes, System.Drawing.Bitmap bitmap)
        {
            if (label.Classification != LogicDomain.GATE_CLASS)
                return new BitmapSymbol();

            BitmapSymbol bs = new BitmapSymbol(strokes, label, bitmap);

            // give the BitmapSymbol a unique name
            int templateCount = 0;
            List<string> alreadySeen = new List<string>();
            foreach (BitmapSymbol template in _templates)
                alreadySeen.Add(template.Name);
            while (alreadySeen.Contains(bs.SymbolType + "_" + templateCount))
                ++templateCount;
            bs.Name = bs.SymbolType + "_" + templateCount;

            if (debug) Console.WriteLine("Adding template " + bs.Name);

            _templates.Add(bs);
            return bs;
        }

        public override bool canRecognize(string classification)
        {
            return classification == LogicDomain.GATE_CLASS;
        }

        /// <summary>
        /// Finds a template based on the string name
        /// </summary>
        /// <param name="templateName">the name of the template</param>
        /// <returns>the BitmapSymbol template</returns>
        protected BitmapSymbol findTemplate(string templateName)
        {
            foreach (BitmapSymbol bs in _templates)
                if (templateName == bs.Name)
                    return bs;
            return null;
        }

        public virtual void orient(Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            ShapeType type = shape.Type;
            BitmapSymbol defn = new BitmapSymbol();

            // TODO: should we use multiple templates, or just one?
            foreach (BitmapSymbol template in _templates)
                if (template.SymbolType == type)
                {
                    defn = template;
                    break;
                }

            BitmapSymbol unknown = _shapesToSymbols[shape];
            shape.Orientation = unknown.bestOrientation(defn);

#if JESSI
            Console.WriteLine("The final shape orientation is " + shape.Orientation);
            Console.WriteLine();
#endif
        }

        /// <summary>
        /// Create a brand new bitmap symbol from the given shape.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private BitmapSymbol createNewBitmapSymbolForShape(Shape s)
        {
            return new BitmapSymbol(s.SubstrokesL);
        }

        /// <summary>
        /// Get a bitmap symbol for a given shape. If the symbol has been cached, this method returns the cached symbol.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public BitmapSymbol GetBitmapSymbolForShape(Shape s)
        {
            return _shapesToSymbols[s];
        }

        #endregion

        #region Recognition

        /// <summary>
        /// Recognize a shape.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="featureSketch"></param>
        /// <returns>An ImageRecognitionResult. If you know you are dealing with an ImageRecognizer, you can cast the returned
        /// RecognitionResult to an ImageRecognitionResult safely.</returns>
        public override RecognitionInterfaces.RecognitionResult recognize(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            // Gaaghaghagahga
            // C# has one flaw, and I found it:
            // http://www.simple-talk.com/community/blogs/simonc/archive/2010/07/14/93495.aspx
            // In short, this method must return a generic "RecognitionResult" and cannot return
            // the better "ImageRecognitionResult," even though doing so would be perfectly 
            // type-safe. =(

            BitmapSymbol unknown = _shapesToSymbols[shape];
            RecoResult allResults = unknown.Recognize(_templates);
            List<SymbolRank> results = allResults.SortedResults(ResultType.FUSION);
            
            if (results.Count > 0)
            {
                // Populate the dictionary of alterateTypes with all of the ShapeTypes in results
                var alternateTypes = new Dictionary<ShapeType, double>();

#if JESSI
                Console.WriteLine();
                Console.WriteLine("\nRecognition results: ");
#endif

                foreach (SymbolRank result in results)
                {
                    if (!alternateTypes.ContainsKey(result.SymbolType))
                        alternateTypes.Add(result.SymbolType, getProbabilityFromTotalDistance(result));
#if JESSI
                    if (debug)
                        Console.WriteLine(result.SymbolType + " with template " + result.SymbolName);
#endif
                }

                ShapeType type = results[0].SymbolType; // the most likely type

                float probability = (float)alternateTypes[type]; // grab the probability of our most likely type

                alternateTypes.Remove(type); // the most likely type is NOT an alternate

                double confidence = getProbabilityFromTotalDistance(results[0]);
                double orientation = results[0].BestOrientation;
                string templateName = results[0].SymbolName;
                System.Drawing.Bitmap templateBitmap = results[0].Symbol.toBitmap();

                double partialHausdorff = allResults.getSR(ResultType.PARTIAL_HAUSDORFF, results[0].Symbol).Distance;
                double modHausdorff = allResults.getSR(ResultType.MOD_HAUSDORFF, results[0].Symbol).Distance;
                double yule = allResults.getSR(ResultType.YULE, results[0].Symbol).Distance;
                double tanimoto = allResults.getSR(ResultType.TANIMOTO, results[0].Symbol).Distance;

                return new ImageRecognitionResult(
                    type, 
                    partialHausdorff, 
                    modHausdorff, 
                    yule, 
                    tanimoto, 
                    confidence, 
                    orientation, 
                    alternateTypes, 
                    templateName, 
                    templateBitmap);
            }

            throw new Exception("Image recognition failed on shape " + shape);
        }

        #region ComboRecognizer Compatability

        /// <summary>
        /// Recognizes the given strokes. Used by ComboRecognizer.
        /// </summary>
        /// <param name="strokes">The list of strokes to recognize</param>
        /// <returns>A ranked list of possible ShapeType matches</returns>
        public List<ShapeType> Recognize(List<Substroke> strokes)
        {
            BitmapSymbol unknown = new BitmapSymbol(strokes);
            List<SymbolRank> results = unknown.Recognize(_templates).SortedResults(ResultType.FUSION);
            List<ShapeType> output = new List<ShapeType>();
            foreach (SymbolRank sr in results)
                output.Add(sr.SymbolType);
            return output;
        }

            #endregion

        /// <summary>
        /// Recognize a shape as if it had the given type
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public override Dictionary<ShapeType, RecognitionInterfaces.RecognitionResult> RecognitionResults(Shape shape, IEnumerable<ShapeType> types)
        {
            return RecognitionResults(GetBitmapSymbolForShape(shape), types);
        }

        /// <summary>
        /// Recognize a shape as if it had the given type
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public Dictionary<ShapeType, RecognitionInterfaces.RecognitionResult> RecognitionResults(BitmapSymbol symbol, IEnumerable<ShapeType> types)
        {
            if (_templates.Count == 0)
                throw new Exception("Cannot recognize a symbol when there are no available templates");

            var recResults = new Dictionary<ShapeType, RecognitionInterfaces.RecognitionResult>();

            RecoResult allResults = symbol.Recognize(_templates);
            List<SymbolRank> results = allResults.SortedResults(ResultType.FUSION);

            if (results.Count == 0)
                throw new Exception("Image recognition failed on bitmap symbol " + symbol);

            Dictionary<ShapeType, double> alternateTypes = new Dictionary<ShapeType,double>();

            // Loop through them from most-likely to least-likely.
            foreach (SymbolRank result in results)
            {
                if (!recResults.ContainsKey(result.SymbolType))
                {

                    double partialHausdorff = allResults.getSR(ResultType.PARTIAL_HAUSDORFF, result.Symbol).Distance;
                    double modHausdorff = allResults.getSR(ResultType.MOD_HAUSDORFF, result.Symbol).Distance;
                    double yule = allResults.getSR(ResultType.YULE, result.Symbol).Distance;
                    double tanimoto = allResults.getSR(ResultType.TANIMOTO, result.Symbol).Distance;

                    ImageRecognitionResult r = new ImageRecognitionResult(
                        result.SymbolType,
                        partialHausdorff,
                        modHausdorff,
                        yule,
                        tanimoto,
                        getProbabilityFromTotalDistance(result),
                        result.BestOrientation,
                        alternateTypes,
                        result.SymbolName,
                        result.Symbol.toBitmap());

                    recResults.Add(result.SymbolType, r);
                }
                if (!alternateTypes.ContainsKey(result.SymbolType))
                    alternateTypes.Add(result.SymbolType, getProbabilityFromTotalDistance(result));
            }

            foreach (ShapeType type in types)
                if (!recResults.ContainsKey(type))
                    recResults.Add(type, new ImageRecognitionResult(
                        type,
                        0, 0, 0, 0, 0, 0, new Dictionary<ShapeType, double>(), "", null));

            return recResults;

        }

        public double getProbabilityFromTotalDistance(SymbolRank sr)
        {
            return 1 - sr.Distance / 4;
        }

        #endregion

        #region Serialization, Saving, and Loading

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public ImageRecognizer(SerializationInfo info, StreamingContext ctxt)
            :this((List<BitmapSymbol>)info.GetValue("templates", typeof(List<BitmapSymbol>)))
        {
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("templates", _templates);
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
        }

        /// <summary>
        /// Loads a previously saved ImageRecognizer from the given filename, 
        /// using the deserialization constructor
        /// </summary>
        /// <param name="filename">Filename which is the saved ImageRecognizer</param>
        /// <returns>Re-instantiated ImageRecognzier</returns>
        public static ImageRecognizer Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            ImageRecognizer image = (ImageRecognizer)bformatter.Deserialize(stream);
            stream.Close();

             #if DEBUG
            Console.WriteLine("Image recognizer loaded.");
             #endif

            return image;
        }

        #endregion

    }
}