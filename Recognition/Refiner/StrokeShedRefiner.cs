using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RecognitionInterfaces;
using System.Diagnostics;

namespace Refiner
{

    public class StrokeShedRefiner : IRecognitionStep
    {

        #region Internals

        /// <summary>
        /// The default threshold shape probability for refining.
        /// If a shape has a probability lower than the threshold,
        /// we want to perform refining. Kind of arbitrary.
        /// </summary>
        private const float DEFAULT_THRESHOLD_PROB = .7F;

        private ContextDomain.ContextDomain _domain;
        private Recognizer _sketchRecognizer;
        private Connector _connector;
        private double _thresholdProbability;

        #endregion

        #region Constructors

        public StrokeShedRefiner(Recognizer sketchRecognizer, Connector connector)
            : this(sketchRecognizer, connector, DEFAULT_THRESHOLD_PROB)
        {
        }

        public StrokeShedRefiner(Recognizer sketchRecognizer, Connector connector, double thresholdProbability)
        {
            _connector = connector;
            _domain = _connector.Domain;
            _sketchRecognizer = sketchRecognizer;
            _thresholdProbability = thresholdProbability;
        }

        #endregion

        #region Stroke Shed Refine


        public string ProgressString
        {
            get { return "Refining (stroke shed)"; }
        }

        /// <summary>
        /// Refine a feature sketch.
        /// </summary>
        /// <param name="featureSketch">the sketch to process</param>
        public virtual void process(Featurefy.FeatureSketch featureSketch)
        {
            StrokeShedRefine(featureSketch);
        }

        /// <summary>
        /// Look at any out-of-context shapes in the sketch and refine them by
        /// removing a substroke. (We may want to try removing more strokes later).
        /// 
        /// We assume that the substroke should be a wire, and we remove it
        /// from the shape. Then we regroup the shape without the substroke,
        /// re-recognize it, and check to see if it makes sense. If it does,
        /// keep it. If not, try again with a different substroke.
        /// </summary>
        public void StrokeShedRefine(Featurefy.FeatureSketch featureSketch)
        {

            foreach (Sketch.Shape shape in featureSketch.Sketch.Shapes)
            {
                bool valid = _domain.IsProperlyConnected(shape);

                // If the shape wansn't specified by the user, doesn't fit
                // its context, and is a gate
                if ((!shape.AlreadyGrouped) &&
                    (!valid || (shape.Probability < _thresholdProbability)) &&
                    (shape.Classification == LogicDomain.GATE_CLASS))
                {
                    float bestProb = 0F;
                    Sketch.Substroke bestSubstroke = null;

                    foreach (Sketch.Substroke substroke in shape.Substrokes)
                    {
                        // See how helpful it is to let the shape 
                        // shed this stroke
                        float shedProb = strokeShedHelpfulness(shape, substroke, featureSketch);
                        if (shedProb > bestProb)
                        {
                            bestProb = shedProb;
                            bestSubstroke = substroke;
                        }
                    }

                    // If hypotheticaly shedding one of the substrokes 
                    // enhanced the recognition of the shape, do it for real
                    //if (bestProb > shape.Probability) // NOT SURE IF WE WANT TO KEEP THIS
                    shedStroke(shape, bestSubstroke, featureSketch);
                }
            }
        }

        /// <summary>
        /// Calculate the probability of how well a shape would be
        /// recognized if it shed a given substroke.
        /// 
        /// ASSUMPTIONS: 
        ///   * The removed substroke should be a wire.
        ///   * The shape has at least 1 substroke
        /// </summary>
        /// <param name="shape">The shape that would shed</param>
        /// <param name="substroke">The stroke to shed</param>
        /// <returns>How well the shape was recognized</returns>
        private float strokeShedHelpfulness(Sketch.Shape origShape, Sketch.Substroke substroke, Featurefy.FeatureSketch featureSketch)
        {
            Debug.Assert(origShape.SubstrokesL.Contains(substroke), "The substroke is not in the shape!");

            // Store the substroke's original state
            string substrokeOrigClassification = substroke.Classification;

            // Shed the stroke and keep track of the new shape 
            // created from this shedded stroke
            List<Sketch.Shape> shedShapes = shedStroke(origShape, substroke, featureSketch);

            // Store whether the original shape was eliminated upon shedding a substroke
            bool origShapeEliminated = (origShape.SubstrokesL.Count == 0);

            ///////////////////////////////////////////////////////
            // Caclulate how helpful the shed was
            ///////////////////////////////////////////////////////

            // We start off assuming it is not helpful
            float probHelpful = 0F;

            // Check that all the shed shapes are valid in their contexts
            bool shedShapeValid = true;
            foreach (Sketch.Shape s in shedShapes)
                if (shedShapeValid)
                    shedShapeValid = (_domain.IsProperlyConnected(s));

            // If the new version of the shape has no more
            // substrokes, we'll assume this is good
            if (origShapeEliminated && shedShapeValid)
                probHelpful = 1F;

            // Otherwise, update the probability
            else
            {
                bool origShapeValid = (_domain.IsProperlyConnected(origShape));
                if (origShapeValid && shedShapeValid)
                {
                    // Calculate the average of all the probabilities
                    // Don't include the last shape... it's the new wire!
                    for (int i = 0; i < shedShapes.Count - 1; i++)
                        probHelpful += shedShapes[i].Probability;
                    probHelpful += origShape.Probability;
                    probHelpful /= shedShapes.Count;
                }
            }

            ///////////////////////////////////////////////////////
            // Revert back to the original versions of these shapes
            ///////////////////////////////////////////////////////

            // Revert the substroke back to its original classification
            substroke.Classification = substrokeOrigClassification;

            // Merge the shapes back together
            foreach (Sketch.Shape s in shedShapes)
                featureSketch.Sketch.mergeShapes(origShape, s);

            // If the shape was eliminated from the sketch upon removing
            // its substroke, add it back in
            if (origShapeEliminated)
                featureSketch.Sketch.AddShape(origShape);

            // Revert what the shapes are recognized as
            _sketchRecognizer.processShape(origShape, featureSketch);

            return probHelpful;
        }

        /// <summary>
        /// Remove a substroke from a shape and turn it into a wire, and
        /// update the remains of the shape. If the shape ends up breaking
        /// into multiple shapes upon removing the substroke, the function
        /// returns a a list of these broken off shapes. If the shape 
        /// originally had one substroke and therefore ends up with none,
        /// the empty shape gets removed from the sketch.
        /// 
        /// ASSUMPTIONS: 
        ///   * The removed substroke should be a wire.
        ///   * The shape has at least 1 substroke
        /// </summary>
        /// <param name="shape">The shape that sheds</param>
        /// <param name="substroke">The substroke to shed</param>
        /// <returns>A list of any shapes that were broken off of the 
        /// original shape in the process.</returns>
        private List<Sketch.Shape> shedStroke(Sketch.Shape shape, Sketch.Substroke substroke, Featurefy.FeatureSketch featureSketch)
        {
            if (substroke == null)
                return null;
            else
            {
                Debug.Assert(shape.SubstrokesL.Contains(substroke), "The substroke is not in the shape!");

                // Change substroke classification to wire.
                substroke.Classification = "Wire";

                // Remove substroke from the shape, and make it a new shape.
                List<Sketch.Substroke> strokesToShed = new List<Sketch.Substroke>();
                strokesToShed.Add(substroke);
                Sketch.Shape newShape = featureSketch.Sketch.BreakOffShape(shape, strokesToShed);

                // Recognize the new shape.
                _sketchRecognizer.processShape(newShape, featureSketch);

                // Check if removing the substroke split the shape into smaller, unconnected shapes
                List<Sketch.Shape> brokenOffShapes = maybeBreakOffShapes(shape, featureSketch.Sketch);

                // Reconnect the shape(s) and the new, broken-off shape.
                List<Sketch.Shape> changedShapes = new List<Sketch.Shape>();
                changedShapes.Add(shape);
                changedShapes.Add(newShape);
                foreach (Sketch.Shape subshape in brokenOffShapes)
                    changedShapes.Add(subshape);
                _connector.recomputeConnectedShapes(changedShapes, featureSketch.Sketch);

                // If the shape no longer exists, remove it.
                if (shape.SubstrokesL.Count == 0)
                    featureSketch.Sketch.RemoveShape(shape);

                // Otherwise, rerecognize the new version of the shape(s)
                else
                {
                    _sketchRecognizer.processShape(shape, featureSketch);
                    foreach (Sketch.Shape subshape in brokenOffShapes)
                        _sketchRecognizer.processShape(subshape, featureSketch);
                }

                // Add shedded shape to the end of the list of broken off shapes
                brokenOffShapes.Add(newShape);
                return brokenOffShapes;
            }
        }

        #endregion

        #region Break Off Shapes

        /// <summary>
        /// Finds all the substrokes in a shape that should be
        /// broken off into a separate shape (if any), and does so.
        /// </summary>
        /// <param name="shape">The shape to investigate</param>
        /// <returns>A list of broken off shapes NOT including the original</returns>
        private List<Sketch.Shape> maybeBreakOffShapes(Sketch.Shape shape, Sketch.Sketch sketch)
        {
            List<Sketch.Shape> brokenOffShapes = new List<Sketch.Shape>();
            Dictionary<Guid, Dictionary<Guid, int>> adjacency = Util.makeAdjacency(sketch);
            List<List<Sketch.Substroke>> subShapes = findShapeSubgroups(shape, adjacency);

            // While the shape is composed of more than one sub-shape
            while (subShapes.Count > 1)
            {
                // Break off the sub-shape
                Sketch.Shape newShape = sketch.BreakOffShape(shape, subShapes[0]);
                subShapes.RemoveAt(0);
                brokenOffShapes.Add(newShape);
            }

            return brokenOffShapes;
        }



        /// <summary>
        /// Find all subgroups within a shape, where subgroups
        /// are sets of adjacent substrokes.
        /// </summary>
        /// <param name="shape"></param>
        /// <returns>A list containing lists of connected substrokes</returns>
        private List<List<Sketch.Substroke>> findShapeSubgroups(Sketch.Shape shape, Dictionary<Guid, Dictionary<Guid, int>> adjacency)
        {
            List<List<Sketch.Substroke>> shapeSubgroups = new List<List<Sketch.Substroke>>();

            // For every stroke in the shape
            foreach (Sketch.Substroke substroke in shape.Substrokes)
            {
                // Calculate whether this substroke is in a new subgroup
                bool belongsToNewSubgroup = true;
                foreach (List<Sketch.Substroke> subList in shapeSubgroups)
                    if (subList.Contains(substroke))
                        belongsToNewSubgroup = false;

                // If it does belong to a new subgroup, find and add
                // this entire subgroup to the list
                if (belongsToNewSubgroup)
                {
                    List<Sketch.Substroke> subgroup = new List<Sketch.Substroke>();
                    oneShapeSubgroup(substroke, shape, subgroup, adjacency);
                    shapeSubgroups.Add(subgroup);
                }
            }

            return shapeSubgroups;

        }

        /// <summary>
        /// Find all the substrokes that compose one subgroup
        /// within a given shape, starting with a given
        /// seed substroke, and store these substrokes in a
        /// given list.
        /// 
        /// A subgroup is defined as a set of adjacent substrokes.
        /// This function uses a depth first search.
        /// 
        /// NOTE: the shape must contain the given substroke
        /// </summary>
        /// <param name="seedSubstroke">A substroke in the desired subgroup</param>
        /// <param name="shape">The shape to investigate</param>
        /// <param name="adjacentSubstrokes">The list to store the desired substrokes</param>
        /// <param name="adjacency">The adjacency matrix for strokes</param>
        private void oneShapeSubgroup(Sketch.Substroke seedSubstroke,
            Sketch.Shape shape, List<Sketch.Substroke> adjacentSubstrokes, Dictionary<Guid, Dictionary<Guid, int>> adjacency)
        {
            Debug.Assert(shape.SubstrokesL.Contains(seedSubstroke), "Shape does not contain given substroke");

            // Make a list of adjacent substrokes not yet accounted for 
            List<Sketch.Substroke> additionalSubstrokes = new List<Sketch.Substroke>();
            foreach (Sketch.Substroke substroke in shape.Substrokes)
                if ((!adjacentSubstrokes.Contains(substroke)) &&
                    ((seedSubstroke.Id == substroke.Id) || (adjacency[seedSubstroke.Id][substroke.Id] > 0)))
                    additionalSubstrokes.Add(substroke);

            // Recurse on each of the found adjacent substrokes
            if (additionalSubstrokes.Count > 0)
            {
                foreach (Sketch.Substroke substroke in additionalSubstrokes)
                    adjacentSubstrokes.Add(substroke);
                foreach (Sketch.Substroke substroke in additionalSubstrokes)
                    oneShapeSubgroup(substroke, shape, adjacentSubstrokes, adjacency);
            }
        }

        #endregion

    }

}
