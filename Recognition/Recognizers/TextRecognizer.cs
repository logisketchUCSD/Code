using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;

namespace Recognizers
{

    #region TextRecognitionResult helper class

    public class TextRecognitionResult : RecognitionInterfaces.RecognitionResult
    {

        private string _text;

        public TextRecognitionResult(double confidence, string text)
            :base(LogicDomain.TEXT, confidence, 0)
        {
            _text = text;
        }

        public override void ApplyToShape(Sketch.Shape s)
        {
            base.ApplyToShape(s);
            s.Name = _text;
        }

        public string Text
        {
            get { return _text; }
        }

    }

    #endregion

    /// <summary>
    /// Recognizes text only.
    /// </summary>
    public class TextRecognizer : RecognitionInterfaces.Recognizer
    {

        #region Internals

        /// <summary>
        /// The text recognizer from Microsoft.Ink that we harness
        /// </summary>
        RecognizerContext _microsoftTextRecognizer;
        
        #endregion

        #region Constructor, Destructor, and Initializers

        public TextRecognizer()
        {
            _microsoftTextRecognizer = new RecognizerContext();    
            
            // Specify what words should be recognizable, to enhance accuracy
            _microsoftTextRecognizer.WordList = createWordList();

            // Indicate that we want to only use words from this wordlist.
            _microsoftTextRecognizer.Factoid = Factoid.WordList;
            _microsoftTextRecognizer.RecognitionFlags = RecognitionModes.WordMode | RecognitionModes.Coerce;
        }

        ~ TextRecognizer()
        {
            /*
             * From http://msdn.microsoft.com/en-us/library/ms828542.aspx :
             * "To avoid a memory leak you must explicitly call the Dispose 
             * method on any RecognizerContext collection to which an event 
             * handler has been attached before the collection goes out of 
             * scope."
             * 
             * We don't attach an event handler, but a little care never 
             * hurt anyone.
             */
            _microsoftTextRecognizer.Dispose();
        }

        private static WordList createWordList()
        {
            // Create an array of words for the default WordList.
            // Note that when words are added to a WordList, it
            // capitalized versions are also implicitly added.
            string[] words = { 
                "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", 
                "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", 
                "w", "x", "y", "z",

                "a'", "b'", "c'", "d'", "e'", "f'", "g'", "h'", "i'",
                "j'", "k'", "l'", "m'", "n'", "o'", "p'", "q'", "r'",
                "s'", "t'","u'", "v'", "w'", "x'", "y'", "z'",

                "out", "in", "cout", "cin", "reset", "clock", "en", 
                "Sa", "Sb", "Sc", "Sd", "Se", "Sf", "Sg", "Sout", "Sin", 
                "S1", "S0", "S1'", "S0'", "AB", "A+B", "SW", 
                 };

            // Create the WordList
            WordList wordList = new WordList();
            foreach (string word in words)
                wordList.Add(word);

            return wordList;
        }

        #endregion

        #region Recognition

        /// <summary>
        /// Read a shape as text
        /// </summary>
        /// <param name="shape">the shape to recognize</param>
        /// <returns>the text string</returns>
        public string read(Sketch.Shape shape)
        {
            double confidence;
            return read(shape, out confidence);
        }

        /// <summary>
        /// Read a shape as text
        /// </summary>
        /// <param name="shape">the shape to recognize</param>
        /// <param name="confidence">how confident the reading is</param>
        /// <returns>the text string</returns>
        public string read(Sketch.Shape shape, out double confidence)
        {
            // Prepare the shape for recognition
            SketchOrInkConverter converter = new SketchOrInkConverter();
            _microsoftTextRecognizer.Strokes = converter.convertToInk(shape);

            // Try to recognize the shape
            RecognitionStatus status;
            RecognitionResult result;
            lock (_microsoftTextRecognizer)
            {
                result = _microsoftTextRecognizer.Recognize(out status);
            }

            // Origanize the results
            string shapeName = "";
            confidence = 0;
            if ((result != null) && (status == RecognitionStatus.NoError))
            {
                shapeName = result.TopString;
                switch (result.TopConfidence)
                {
                    case RecognitionConfidence.Poor:
                        confidence = .1;
                        break;
                    case RecognitionConfidence.Intermediate:
                        confidence = .5;
                        break;
                    case RecognitionConfidence.Strong:
                        confidence = .9;
                        break;
                }
            }

            return shapeName;
        }

        /// <summary>
        /// Recognizes the text a shape forms, and updates the
        /// shape with the recognition results (the text and the
        /// likelihood that the recognition was correct).
        /// 
        /// This method does not use the featureSketch argument.
        /// </summary>
        /// <param name="shape">The shape to recognize</param>
        /// <param name="featureSketch">Not used.</param>
        /// <returns>the most probable type of the shape</returns>
        public override RecognitionInterfaces.RecognitionResult recognize(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            // Gaaghaghagahga
            // C# has one flaw, and I found it:
            // http://www.simple-talk.com/community/blogs/simonc/archive/2010/07/14/93495.aspx
            // In short, this method must return a generic "RecognitionResult" and cannot return
            // the better "TextRecognitionResult," even though doing so would be perfectly 
            // type-safe. =(
            double confidence;
            string name = read(shape, out confidence);
            return new TextRecognitionResult(confidence, name);
        }

        #endregion

        #region Other Methods

        /// <summary>
        /// This recognizer only recognizes text.
        /// </summary>
        /// <param name="classification">a shape classification</param>
        /// <returns>true if classification is "Label"</returns>
        public override bool canRecognize(string classification)
        {
            return classification == LogicDomain.TEXT_CLASS;
        }

        #endregion

    }

}
