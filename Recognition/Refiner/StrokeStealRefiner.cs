using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RecognitionInterfaces;

namespace Refiner
{

    public class StrokeStealRefiner : IRecognitionStep
    {

        #region Internals

        private ContextDomain.ContextDomain _domain;
        private Recognizer _sketchRecognizer;
        private Connector _connector;

        #endregion

        #region Constructor

        public StrokeStealRefiner(Recognizer recognizer, Connector connector)
        {
            _sketchRecognizer = recognizer;
            _connector = connector;
            _domain = _connector.Domain;
        }

        #endregion

        #region Stroke Steal Refine


        public string ProgressString
        {
            get { return "Refining (stroke steal)"; }
        }

        /// <summary>
        /// Refine a feature sketch.
        /// </summary>
        /// <param name="featureSketch">the sketch to process</param>
        public virtual void process(Featurefy.FeatureSketch featureSketch)
        {
            StrokeStealRefine(featureSketch);
        }

        /// <summary>
        /// Pick out the shapes that still that do not match their context, 
        /// try grouping neighboring strokes into them (i.e. they steal a neighboring stroke), 
        /// and if this puts it in context, keep it.
        /// 
        /// NOTE: we're only considering gates at the moment
        /// </summary>
        public void StrokeStealRefine(Featurefy.FeatureSketch featureSketch)
        {
            foreach (Sketch.Shape shape in featureSketch.Sketch.Shapes)
            {
                bool valid = _domain.IsProperlyConnected(shape);

                // If the shape was not specified by the user, doesn't fit its 
                // context, and is a gate
                if (!valid && !shape.AlreadyGrouped && shape.Classification == LogicDomain.GATE_CLASS)
                {
                    float bestProb = 0F;
                    Sketch.Substroke bestSubstroke = null;

                    // For each neighboring shape, try stealing a stroke until  
                    // one fits well given that the user did not specify it
                    List<Sketch.Shape> neighbors = featureSketch.Sketch.neighboringShapes(shape);
                    foreach (Sketch.Shape neighbor in neighbors)
                    {
                        if (!neighbor.AlreadyGrouped)
                            foreach (Sketch.Substroke substroke in neighbor.Substrokes)
                            {
                                float stealProb = strokeStealHelpfulness(shape, substroke, featureSketch);
                                if (stealProb > bestProb)
                                {
                                    bestProb = stealProb;
                                    bestSubstroke = substroke;
                                }
                            }
                    }

                    // Only steal the substroke that gives the best new shape
                    stealStroke(shape, bestSubstroke, featureSketch);
                }
            }
        }

        /// <summary>
        /// Calculate the probability of how well a shape would be
        /// recognized if it stole a given substroke.
        /// 
        /// ASSUMPTIONS: 
        ///   * Each shape has at least 1 substroke
        /// 
        /// NOTE: Currently we brute-force this and try every substroke in the victim shape. 
        ///       This can be steamlined by picking only adjacent substrokes.
        /// </summary>
        /// <param name="strokeThief"> The shape that tries to steal a substroke </param>
        /// <param name="strokeToSteal"> The substroke to steal </param>
        /// <returns> A float, indicating how successful the steal is. </returns>
        private float strokeStealHelpfulness(Sketch.Shape strokeThief, Sketch.Substroke strokeToSteal, Featurefy.FeatureSketch featureSketch)
        {
            float helpfulness = 0F;

            // Store the original state
            Sketch.Shape strokeLoser = strokeToSteal.ParentShape;
            string substrokeOrigClassification = strokeToSteal.Classification;
            List<Sketch.Shape> changedShapes = new List<Sketch.Shape>();
            changedShapes.Add(strokeThief);
            changedShapes.Add(strokeLoser);

            // Steal a stroke
            stealStroke(strokeThief, strokeToSteal, featureSketch);

            // See if the stroke thief is now valid
            bool validStrokeThief = (_domain.IsProperlyConnected(strokeThief));

            // See if the stroke loser is a valid shape, 
            // given that it still exists
            bool validStrokeLoser;
            if (strokeLoser.SubstrokesL.Count > 0)
            {
                validStrokeLoser = (_domain.IsProperlyConnected(strokeLoser));
            }
            else
            {
                validStrokeLoser = true;
            }

            // If stealing the substroke made the shapes valid
            if (validStrokeThief && validStrokeLoser)
            {
                // Make sure we don't have empty shapes 
                // (from removing its only stroke earlier)
                if (strokeLoser.SubstrokesL.Count == 0)
                    featureSketch.Sketch.RemoveShape(strokeLoser);

                // Calculate the new probability
                helpfulness = (strokeThief.Probability + strokeLoser.Probability) / 2;
            }

            ///////////////////////////////////////////////////////
            // Revert back to the original versions of these shapes
            ///////////////////////////////////////////////////////

            // Revert the substroke back to its original classification
            strokeToSteal.Classification = substrokeOrigClassification;

            // Move the substroke back to its original shape
            strokeThief.RemoveSubstroke(strokeToSteal);
            strokeLoser.AddSubstroke(strokeToSteal);

            // Revert what the shapes are recognized as
            _sketchRecognizer.processShape(strokeThief, featureSketch);
            _sketchRecognizer.processShape(strokeLoser, featureSketch);

            // Reset what all the shapes are connected to
            _connector.recomputeConnectedShapes(changedShapes, featureSketch.Sketch);

            return helpfulness;
        }

        /// <summary>
        /// Take a substroke and add it to a given shape.
        /// </summary>
        /// <param name="shape">The shape that steals</param>
        /// <param name="substroke">The substroke to steal</param>
        private void stealStroke(Sketch.Shape strokeThief, Sketch.Substroke strokeToSteal, Featurefy.FeatureSketch featureSketch)
        {
            if (strokeToSteal != null)
            {
                // Move the substroke we're trying to steal
                Sketch.Shape strokeLoser = strokeToSteal.ParentShape;
                strokeLoser.RemoveSubstroke(strokeToSteal);
                strokeThief.AddSubstroke(strokeToSteal);

                // Update what these recombinant shapes and their 
                // connected shapes are connected to
                List<Sketch.Shape> changedShapes = new List<Sketch.Shape>();
                changedShapes.Add(strokeThief);
                changedShapes.Add(strokeLoser);
                _connector.recomputeConnectedShapes(changedShapes, featureSketch.Sketch);

                // Also update the substroke to match its new shape
                strokeToSteal.Classification = "Gate";

                // See if the stroke thief is a valid shape
                _sketchRecognizer.processShape(strokeThief, featureSketch);

                // See if the stroke loser is a valid shape
                if (strokeLoser.SubstrokesL.Count > 0)
                    _sketchRecognizer.processShape(strokeLoser, featureSketch);
            }
        }

        #endregion

    }

}
