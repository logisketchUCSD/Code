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
    /// This is a modified version of ContextRefiner which is more delicate. It
    /// hesitates and won't reidentify a shape unless the probabilities are high.
    /// </summary>
    public class CarefulContextRefiner : IRecognitionStep
    {

        #region Internals

        /// <summary>
        /// Turn on extended debugging messages
        /// </summary>
        private const bool DEBUG = true;

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
        public CarefulContextRefiner(ContextDomain.ContextDomain domain, Recognizer recognizer)
        {
            _domain = domain;
            _sketchRecognizer = recognizer;
        }

        #endregion

        #region Context Refinement

        public string ProgressString
        {
            get { return "Refining (careful context refinement)"; }
        }

        /// <summary>
        /// After the sketch has been recognized, we can look at the context of each shape.
        /// If a shape is out of context (for example, if a NOT gate is connected to three wires),
        /// then we assign the shape the next most likely label.
        /// </summary>
        /// <param name="featureSketch">the sketch to process</param>
        public virtual void process(Featurefy.FeatureSketch featureSketch)
        {
            if (DEBUG)
                Console.WriteLine("\nCareful Context Refinement");

            foreach (Sketch.Shape shape in featureSketch.Sketch.Shapes)
            {
                bool valid = _domain.IsProperlyConnected(shape);

                if (valid || shape.AlreadyGrouped)
                    continue;

                ShapeType originalType = shape.Type;
                string originalName = shape.Name;
                float originalProbability = shape.Probability;

                // If a shape only has one connection,
                // it is assumed to be a label.
                if (shape.ConnectedShapes.Count == 1)
                {
                    foreach (Sketch.Substroke substroke in shape.Substrokes)
                        substroke.Classification = LogicDomain.TEXT_CLASS;
                    _sketchRecognizer.processShape(shape, featureSketch);
                    if (shape.Probability < 0.5)
                    {
                        // we probably made a mistake, so revert.
                        shape.Type = originalType;
                        shape.Name = originalName;
                        shape.Probability = originalProbability;
                    }
                }

                if (Domain.LogicDomain.IsGate(shape.Type))
                {
                    nextBestLabel(shape);
                    if (!_domain.IsProperlyConnected(shape))
                    {
                        // the next best type wasn't properly connected either
                        // so we probably want to stick with our first type
                        shape.Type = originalType;
                        shape.Name = originalName;
                        shape.Probability = originalProbability;
                    }
                        
                }

                if (DEBUG && ((originalName != shape.Name) || (originalType != shape.Type))) // if it changed
                    Console.WriteLine("    " + originalName + " (" + originalType + ") -> " + shape.Name + " (" + shape.Type + "); confidence = " + shape.Probability);
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
            if (shape.AlternateTypes == null || shape.AlternateTypes.Count == 0)
            {
                Console.WriteLine("    WARNING: '" + shape + "' has no alternate types");
                return;
            }

            ShapeType bestlabel = new ShapeType();
            double bestscore = 0;
            double total = 0;
            bool foundAlternate = false;

            foreach (KeyValuePair<ShapeType, double> pair in shape.AlternateTypes)
            {
                total += pair.Value;
                if (pair.Value > bestscore)
                {
                    bestlabel = pair.Key;
                    bestscore = pair.Value;
                    foundAlternate = true;
                }
            }

            if (!foundAlternate)
            {
                Console.WriteLine("    WARNING: could not find an alternate type for '"+shape+"'");
                return;
            }

            shape.AlternateTypes.Remove(bestlabel);
            shape.Type = bestlabel;
            // Currently, the probabilities all add up to 1.
            // This means if we remove a label possibility,
            // the next probability will be unreasonably low.
            // To get around this, we update the new probability by
            // dividing it by the sum of all remaining probabilities.
            shape.Probability = (float)(bestscore / total);
        }

        #endregion

    }
}
