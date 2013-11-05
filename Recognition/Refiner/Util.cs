using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;

namespace Refiner
{
    class Util
    {
        #region Adjacency

        /// <summary>
        /// Find the number of times substrokes 1 and 2 touch.
        /// </summary>
        /// <param name="stroke1">First substroke</param>
        /// <param name="stroke2">Second substroke</param>
        /// <returns>Number of times substrokes 1 and 2 touch.</returns>
        private static int Adjacent(Sketch.Substroke stroke1, Sketch.Substroke stroke2)
        {
            double totaldistance = 0;
            int count = 0;
            for (int i = 0; i < stroke1.PointsL.Count - 2; i++)
            {
                totaldistance += stroke1.Points[i].distance(stroke1.Points[i + 1]);
                count++;
            }
            for (int i = 0; i < stroke2.PointsL.Count - 2; i++)
            {
                totaldistance += stroke2.Points[i].distance(stroke2.Points[i + 1]);
                count++;
            }
            double avgDistance = totaldistance / count * 10;
            int numTouch = 0;
            foreach (Sketch.Point endpoint in stroke1.Endpoints)
            {
                foreach (Sketch.Point point in stroke2.Points)
                    if (endpoint.distance(point) < avgDistance)
                    {
                        numTouch++;
                        break;
                    }
            }
            foreach (Sketch.Point endpoint in stroke2.Endpoints)
            {
                foreach (Sketch.Point point in stroke1.Points)
                    if (endpoint.distance(point) < avgDistance)
                    {
                        numTouch++;
                        break;
                    }
            }
            foreach (Sketch.Point endpoint1 in stroke1.Endpoints)
                foreach (Sketch.Point endpoint2 in stroke2.Endpoints)
                    if (endpoint1.distance(endpoint2) < avgDistance)
                    {
                        numTouch--;
                    }
            return numTouch;
        }

        /// <summary>
        /// Generate the adjacency matrix for a sketch. Returns a dictionary of dictionaries
        /// (effectively a 2D array, indexed by GUIDs of substrokes) where result[x][y] is
        /// the number of times the substroke with GUID x intersects with the substroke of
        /// GUID y.
        /// </summary>
        public static Dictionary<Guid, Dictionary<Guid, int>> makeAdjacency(Sketch.Sketch sketch)
        {
            Dictionary<Guid, Dictionary<Guid, int>> adjacency = new Dictionary<Guid, Dictionary<Guid, int>>();
            foreach (Sketch.Substroke stroke1 in sketch.SubstrokesL)
            {
                if (!adjacency.ContainsKey(stroke1.Id))
                    adjacency.Add(stroke1.Id, new Dictionary<Guid, int>());

                foreach (Sketch.Substroke stroke2 in sketch.SubstrokesL)
                    if (stroke1.Id != stroke2.Id && !adjacency[stroke1.Id].ContainsKey(stroke2.Id))
                        adjacency[stroke1.Id].Add(stroke2.Id, Adjacent(stroke1, stroke2));

            }
            return adjacency;
        }

        #endregion

        #region OLD CODE

        #region Stroke exchanging

        ///// <summary>
        ///// Attempts to give strokes from shape1 to shape2, in order to improve the overall
        ///// matching between the shapes.
        ///// </summary>
        ///// <param name="shape1">The shape to give away strokes.</param>
        ///// <param name="shape2">The shape to accept strokes.</param>
        //private bool exchange(Sketch.Shape shape1, Sketch.Shape shape2)
        //{
        //    foreach (Sketch.Substroke substroke in shape1.Substrokes)
        //    {
        //        // We only consider those substrokes that are close to the other shape.
        //        if (touching(substroke, shape2))
        //        {
        //            Sketch.Shape shape1c = shape1.Clone();
        //            Sketch.Shape shape2c = shape2.Clone();

        //            int numshapes = 2;

        //            // Move the substroke.
        //            Sketch.Substroke substrokecopy = substroke.Clone();
        //            shape1c.RemoveSubstrokeByID(substrokecopy);
        //            substrokecopy.ParentShapes.Clear();
        //            shape2c.AddSubstrokeByID(substrokecopy);

        //            if (shape1c.SubstrokesL.Count != 0)
        //            {
        //                shape1c.Probability = sketchRecognizer.RecognizeShape(shape1c, featuresketch).Probability;
        //            }
        //            else
        //            {
        //                shape1c.Probability = 0F;
        //                numshapes -= 1;
        //            }

        //            if (shape2c.SubstrokesL.Count != 0)
        //                shape2c.Probability = sketchRecognizer.RecognizeShape(shape2c, featuresketch).Probability;
        //            else
        //            {
        //                shape2c.Probability = 0F;
        //                numshapes -= 1;
        //            }

        //            // Check if this caused an improvement.
        //            if ((shape1c.Probability + shape2c.Probability) / numshapes > (shape1.Probability + shape2.Probability) / 2)
        //            {
        //                shape1.RemoveSubstroke(substroke);
        //                shape2.AddSubstroke(substroke);
        //                if (shape1.SubstrokesL.Count != 0)
        //                {
        //                    Sketch.Shape result1 = sketchRecognizer.RecognizeShape(shape1, featuresketch);

        //                    shape1.Label = result1.Label;
        //                    shape1.Probability = result1.Probability;
        //                    shape1.Name = result1.Name;
        //                }
        //                else
        //                    featuresketch.Sketch.RemoveShape(shape1);
        //                if (shape2.SubstrokesL.Count != 0)
        //                {
        //                    Sketch.Shape result2 = sketchRecognizer.RecognizeShape(shape2, featuresketch);

        //                    shape2.Label = result2.Label;
        //                    shape2.Probability = result2.Probability;
        //                    shape2.Name = result2.Name;
        //                }
        //                else
        //                    featuresketch.Sketch.RemoveShape(shape2);

        //                return true;
        //            }
        //            // Otherwise do nothing.
        //        }
        //    }

        //    return false;
        //}

        ///// <summary>
        ///// Attempt to split this shape into two subshapes, and see if that increases the overall score.
        ///// </summary>
        ///// <param name="shape">The shape we want to split.</param>
        ///// <returns>True if the shape was split.</returns>
        //private bool partition(Sketch.Shape shape)
        //{
        //    if (shape.SubstrokesL.Count < 2)
        //        return false;
        //    return false;

        //}

        #endregion

        #region Reclassify Shapes

        ///// <summary>
        ///// Attempt to reclassify the shape. For now, this is done by getting the next most likely label.
        ///// </summary>
        ///// <param name="shape">The shape to reclassify.</param>
        //public void reclassify(Sketch.Shape inShape)
        //{
        //    Sketch.Shape shape = new Sketch.Shape(inShape);

        //    string bestclass = NO_CLASS;
        //    string okclass = NO_CLASS;
        //    float okprob = 0.0F;
        //    float bestprob = 0.0F;

        //    foreach (string classification in domain.getclasses())
        //    {
        //        float misclassprob = 1F;
        //        foreach (Sketch.Substroke sub in inShape.SubstrokesL)
        //        {
        //            misclassprob *= domain.errorprobability(sub.XmlAttrs.Classification, classification);
        //        }
        //        Sketch.Shape result = NextBestShape(shape); // change this!!
        //        applyclass(ref shape, classification);
        //        Dictionary<Sketch.Shape, string> neighbours = shape.ConnectedShapes;
        //        float resultprob = result.Probability;
        //        float contextprob = domain.contextscore(shape, neighbours);
        //        resultprob *= misclassprob;


        //        if (resultprob * contextprob > bestprob)
        //        {
        //            bestclass = classification;
        //            bestprob = resultprob * contextprob;
        //        }
        //        if (resultprob > okprob)
        //        {
        //            okclass = classification;
        //            okprob = resultprob;
        //        }
        //    }
        //    if (!bestclass.Equals(NO_CLASS))
        //        applyclass(ref inShape, bestclass);
        //    else
        //        applyclass(ref inShape, okclass);

        //}

        //private void applyclass(ref Sketch.Shape shape, string classification)
        //{
        //    shape.XmlAttrs.Classification = classification;
        //    shape.XmlAttrs.ClassificationBelief = .75F;
        //    Sketch.Shape result = NextBestShape(shape); // change this!!
        //    shape.Label = result.Label;
        //    shape.Name = result.Name;
        //    shape.Probability = result.Probability;

        //    foreach (Sketch.Substroke substroke in shape.SubstrokesL)
        //    {
        //        substroke.Classification = shape.XmlAttrs.Classification;
        //        substroke.ClassificationBelief = (float)shape.XmlAttrs.ClassificationBelief;
        //    }
        //}
        #endregion

        #region HELPERS

        ///// <summary>
        ///// Performs a single iteration of the refinement proccess, and returns true if the sketch has been changed.
        ///// </summary>
        ///// <returns>true iff the sketch has been changed</returns>
        //private bool performIteration()
        //{
        //    foreach (Sketch.Shape shape1 in featuresketch.Sketch.Shapes)
        //        foreach (Sketch.Shape shape2 in featuresketch.Sketch.Shapes)
        //            if (!shape1.Equals(shape2))
        //                if (exchange(shape1, shape2))
        //                    return true;
        //    return false;
        //}

        ///// <summary>
        ///// Check if the two shapes are touching.
        ///// </summary>
        ///// <param name="?">The first shape.</param>
        ///// <param name="?">The second shape.</param>
        ///// <returns>True if the shapes are touching.</returns>
        //private bool touching(Sketch.Shape shape1, Sketch.Shape shape2)
        //{
        //    foreach (Sketch.Substroke stroke1 in shape1.Substrokes)
        //        foreach (Sketch.Substroke stroke2 in shape2.Substrokes)
        //        {
        //            if (touching(stroke1, stroke2) || touching(stroke2, stroke1))
        //            {
        //                return true;
        //            }
        //        }
        //    return false;
        //}

        ///// <summary>
        ///// Check if the substroke is touching the shape.
        ///// </summary>
        ///// <param name="?">The first shape.</param>
        ///// <param name="?">The second shape.</param>
        ///// <returns>True if the shapes are touching.</returns>
        //private bool touching(Sketch.Substroke substroke, Sketch.Shape shape)
        //{
        //    foreach (Sketch.Substroke stroke1 in shape.Substrokes)
        //        if (touching(stroke1, substroke))
        //        {
        //            return true;
        //        }
        //    return false;
        //}

        ///// <summary>
        ///// Check if the two substrokes are touching.
        ///// </summary>
        ///// <param name="stroke1"></param>
        ///// <param name="stroke2"></param>
        ///// <returns></returns>
        //private bool touching(Sketch.Substroke stroke1, Sketch.Substroke stroke2)
        //{
        //    return adjacency[stroke1.Id][stroke2.Id] > 0;
        //}
        #endregion

        #endregion
    }
}
