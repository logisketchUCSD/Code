using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Recognizers
{
    public class WireRecognizer : RecognitionInterfaces.Recognizer
    {
        /// <summary>
        /// Recognizes a shape as a wire and updates the shape
        /// accordingly.
        /// </summary>
        /// <param name="shape">The shape to recognize</param>
        /// <param name="featureSketch">The sketch describing features of the shape</param>
        /// <returns>the most probable type of the shape</returns>
        public override RecognitionInterfaces.RecognitionResult recognize(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            // Let's assume we're 100% sure it's a wire, for now...
            double confidence = 1;
            double orientation = 0;
            return new RecognitionInterfaces.RecognitionResult(LogicDomain.WIRE, confidence, orientation);
        }

        /// <summary>
        /// This recognizer only recognizes wires.
        /// </summary>
        /// <param name="classification">a shape classification</param>
        /// <returns>true if classification is "Wire"</returns>
        public override bool canRecognize(string classification)
        {
            return classification == LogicDomain.WIRE_CLASS;
        }
    }
}
