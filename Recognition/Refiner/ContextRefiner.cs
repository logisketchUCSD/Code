using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RecognitionInterfaces;

namespace Refiner
{

    /// <summary>
    /// Performs refinement on the basis of context. The intuition here is that
    /// if a shape is not connected properly, it may be because it was mis-recognized.
    /// </summary>
    public class ContextRefiner : IRecognitionStep
    {

        #region Internals

        /// <summary>
        /// Turn on extended debugging messages
        /// </summary>
        private const bool debug = true;

        /// <summary>
        /// The recognizer used for correction
        /// </summary>
        private Recognizer _sketchRecognizer;

        /// <summary>
        /// The domain this recognizer operates in
        /// </summary>
        private ContextDomain.ContextDomain _domain;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new context refiner that uses the given domain for context
        /// assessment, and the given recognizer for recognition.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="recognizer"></param>
        public ContextRefiner(ContextDomain.ContextDomain domain, Recognizer recognizer)
        {
            _domain = domain;
            _sketchRecognizer = recognizer;
        }

        #endregion

        #region Context Refinement


        public string ProgressString
        {
            get { return "Refining (context refinement)"; }
        }

        /// <summary>
        /// After the sketch has been recognized, we can look at the context of each shape.
        /// If a shape is out of context (for example, if a NOT gate is connected to three wires),
        /// then we assign the shape the next most likely label.
        /// </summary>
        /// <param name="featureSketch">the sketch to process</param>
        public void process(Featurefy.FeatureSketch featureSketch)
        {
            if (debug)
                Console.WriteLine("Context Refinement");

            // Refine three times... (THIS IS A MAGIC NUMBER I PULLED OUT OF THE AIR).
            int CONTEXT_REFINE_ITERATIONS = 3;
            for (int i = 0; i < CONTEXT_REFINE_ITERATIONS; i++)
            {
                foreach (Sketch.Shape shape in featureSketch.Sketch.Shapes)
                {
                    bool valid = _domain.IsProperlyConnected(shape);

                    if (valid || shape.AlreadyGrouped)
                        continue;

                    ShapeType originalType = shape.Type;
                    string originalName = shape.Name;

                    // If a shape only has one connection, it is assumed to be a label
                    if (shape.ConnectedShapes.Count == 1)
                    {
                        shape.Classification = LogicDomain.TEXT_CLASS;
                        _sketchRecognizer.processShape(shape, featureSketch);
                    }
                    else
                    {
                        if (Domain.LogicDomain.IsGate(shape.Type))
                            nextBestLabel(shape);
                    }

                    if (debug && ((originalName != shape.Name) || (originalType != shape.Type))) // if it changed
                    {   
                        Console.WriteLine("    " + originalName + " (" + originalType + ") -> " + shape.Name + " (" + shape.Type + ")");
                    }
                }
            }
        }

        /// <summary>
        /// Reclassify a shape.
        /// If a shape is determined to be incorrectly classified,
        /// this function assigns it the next most likely label.
        /// </summary>
        /// <param name="shape">the shape to reclassify</param>
        private void nextBestLabel(Sketch.Shape shape)
        {
            ShapeType bestlabel = new ShapeType();
            double bestscore = 0;
            double total = 0F;

            foreach (KeyValuePair<ShapeType, double> pair in shape.AlternateTypes)
            {
                total += pair.Value;
                if (pair.Value > bestscore)
                {
                    bestlabel = pair.Key;
                    bestscore = pair.Value;
                }
            }

            shape.AlternateTypes.Remove(bestlabel);
            shape.Type = bestlabel;
            // Currently, the probabilities all add up to 1.
            // This means if we remove a label possibility,
            // the next probability will be unreasonably low.
            // To get around this, we update the new probability by
            // dividing it by the sum of all remaining probabilities.
            shape.Probability = (float)(bestscore / total);
            //shape.Name = bestlabel; // ?????
        }

        #endregion

    }
}
