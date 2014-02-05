using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using RecognitionInterfaces;

namespace Recognizers
{
    /// <summary>
    /// Recognizes all kinds of shapes: wires, text, and gates. Uses several different sub-recognizers.
    /// </summary>
    public class UniversalRecognizer : Recognizer
    {

        #region Constants

        /// <summary>
        /// Whether or not to enable debug output in the console.
        /// </summary>
        private const bool DEBUG = false;

        #endregion

        #region Internals

        /// <summary>
        /// The recognizer that will be used to identify wires.
        /// </summary>
        private Recognizer _wireRecognizer;

        /// <summary>
        /// The recognizer that will be used to identify text.
        /// </summary>
        private Recognizer _textRecognizer;

        /// <summary>
        /// The recognizer that will be used to identify gates.
        /// </summary>
        private Recognizer _gateRecognizer;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a universal recognizer that using the specified 
        /// recognizers and caching enabled or disabled.
        /// </summary>
        /// <param name="wireRecognizer">the recognizer to use on wires</param>
        /// <param name="textRecognizer">the recognizer to use on text</param>
        /// <param name="gateRecognizer">the recognizer to use on gates</param>
        /// <exception cref="ArgumentException.ArgumentException">Thrown if 
        /// the provided recognizers cannot recognize what they are intended
        /// to (e.g., if the wireRecognizer reports that it cannot recognize
        /// wires).</exception>
        public UniversalRecognizer(
            Recognizer wireRecognizer = null,
            Recognizer textRecognizer = null,
            Recognizer gateRecognizer = null)
        {
            if (wireRecognizer == null)
                wireRecognizer = new WireRecognizer();
            if (textRecognizer == null)
                textRecognizer = new TextRecognizer();
            if (gateRecognizer == null)
            {

#if AIR_OFF
                gateRecognizer = new ImageRecognizer();
#else
                gateRecognizer = AdaptiveImageRecognizer.LoadDefault();
#endif
                    
            }

            if (!wireRecognizer.canRecognize("Wire"))
                throw new ArgumentException("The provided wire recognizer '" + wireRecognizer + "' cannot recognize wires!");
            if (!textRecognizer.canRecognize("Text"))
                throw new ArgumentException("The provided text recognizer '" + textRecognizer + "' cannot recognize text!");
            if (!gateRecognizer.canRecognize("Gate"))
                throw new ArgumentException("The provided gate recognizer '" + gateRecognizer + "' cannot recognize gates!");

            _wireRecognizer = wireRecognizer;
            _textRecognizer = textRecognizer;
            _gateRecognizer = gateRecognizer;
        }

        #endregion

        #region Helper Methods

        private Recognizer recognizerForClass(string classification)
        {
            if (classification == LogicDomain.WIRE_CLASS)
            {
                return _wireRecognizer;
            }
            else if (classification == LogicDomain.TEXT_CLASS)
            {
                return _textRecognizer;
            }
            else if (classification == LogicDomain.GATE_CLASS)
            {
                return _gateRecognizer;
            }

            return null;
        }

        #endregion

        #region Recognition Methods

        /// <summary>
        /// Recognizes a given shape and updates the shape accordingly.
        /// </summary>
        /// <param name="shape">The shape to recogize</param>
        /// <param name="featureSketch">A featuresketch</param>
        public override RecognitionInterfaces.RecognitionResult recognize(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            Recognizer recognizer = recognizerForClass(shape.Classification);
            return recognizer.recognize(shape, featureSketch);
        }

        /// <summary>
        /// Use the given shape as a learning example to update the recognizer. This
        /// trains only the relevant sub-recognizer depending on what type the
        /// shape is (gate, wire, or label).
        /// </summary>
        /// <param name="shape">the shape to learn from</param>
        public override void learnFromExample(Sketch.Shape shape)
        {
            Recognizer recognizer = recognizerForClass(shape.Classification);
            recognizer.learnFromExample(shape);
        }

        /// <summary>
        /// Reset all of the sub-recognizers.
        /// </summary>
        public override void reset()
        {
            _gateRecognizer.reset();
            _wireRecognizer.reset();
            _textRecognizer.reset();
        }

        /// <summary>
        /// Save settings for all sub-recognizers.
        /// </summary>
        public override void save()
        {
            _gateRecognizer.save();
            _wireRecognizer.save();
            _textRecognizer.save();
        }

        #endregion

    }

}
