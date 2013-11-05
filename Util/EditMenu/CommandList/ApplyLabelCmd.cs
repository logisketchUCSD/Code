using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Ink;

using CommandManagement;
using Domain;
using System.Windows;
using InkToSketchWPF;
using SketchPanelLib;

namespace EditMenu.CommandList
{
    public delegate void RegroupEventHandler(List<Sketch.Shape> shapes);
    public delegate void ErrorCorrectedEventHandler(Sketch.Shape shape);

	/// <summary>
	/// This class allows users to re-classify or re-label
    /// strokes as a different type. For example, a set of
    /// strokes might be accidentally recognized as part of a
    /// wire, in which case the user can select the portion
    /// he or she desires and re-label it as an AND gate or
    /// whatnot.
    /// 
    /// This class specifically deals with the relabeling 
    /// and regrouping portion for sketch substrokes and shapes.
	/// </summary>
	public class ApplyLabelCmd : Command
    {
        #region Internals
	
		/// <summary>
		/// Sketch to contain the labeled shape
		/// </summary>
        private SketchPanel sketchPanel;

		/// <summary>
		/// Necessary to apply and undo InkOverlay changes
		/// </summary>
		private StrokeCollection inkStrokes;

        /// <summary>
        /// Label to apply
        /// </summary>
        private ShapeType labelType;

		/// <summary>
		/// Labeled shape resulting from applying a label
		/// </summary>
		private Sketch.Shape labeledShape;

		/// <summary>
		/// Necessary to undo label changes when applying a label
		/// </summary>
        private Dictionary<Sketch.Shape, Tuple<ShapeType, StrokeCollection>> originalLabels;

        /// <summary>
        /// Strokes that did not have a label at the beginning of the command
        /// </summary>
        private StrokeCollection unlabeledStrokes;

        /// <summary>
        /// number associated with subcircuits, if valid
        /// </summary>
        private int tagNumber;

        /// <summary>
        /// Called on shapes to regroup
        /// </summary>
        public event RegroupEventHandler Regroup;

        public event ErrorCorrectedEventHandler ErrorCorrected;

        private bool userSpecifiedGroup;

        private bool userSpecifiedLabel;

        #endregion

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sketch">SketchPanel to add a label to</param>
		/// <param name="inkStrokes">InkOverlay strokes</param>
		/// <param name="inkStrokes">A StrokeCollection strokes</param>
		/// <param name="label">Label to apply</param>

        public ApplyLabelCmd(SketchPanel sketch, StrokeCollection inkStrokes,
            ShapeType labelType, bool userSpecifiedGroup = true, bool userSpecifiedLabel = true, int tagNumber = int.MinValue)
        {
            isUndoable = true;

            this.sketchPanel = sketch;
			this.inkStrokes = inkStrokes;
            this.userSpecifiedGroup = userSpecifiedGroup;
            this.userSpecifiedLabel = userSpecifiedLabel;
            this.tagNumber = tagNumber;
            this.labelType = labelType;

			// Save the original labels of the substrokes
            originalLabels = new Dictionary<Sketch.Shape, Tuple<ShapeType, StrokeCollection>>();
            unlabeledStrokes = new StrokeCollection();

            foreach (Stroke stroke in inkStrokes)
            {
                // If the stroke is classified, save its original shape.
                Sketch.Substroke sub = sketchPanel.InkSketch.GetSketchSubstrokeByInk(stroke);
                if (sub.ParentShape != null)
                {
                    if (!originalLabels.ContainsKey(sub.ParentShape))
                        originalLabels[sub.ParentShape] = new Tuple<ShapeType, StrokeCollection>(sub.ParentShape.Type, new StrokeCollection());
                    originalLabels[sub.ParentShape].Item2.Add(stroke);
                }
                else // Record that it wasn't labeled.
                    unlabeledStrokes.Add(stroke);
            }
		}

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "ApplyLabel";
        }

		/// <summary>
		/// Applies a label to a group of substrokes.
		/// </summary>
        public override bool Execute()
        {
            // Get the sketch we are working with
            Sketch.Sketch sketch = sketchPanel.InkSketch.Sketch;

            // Accumulate the list of substrokes that are about to be relabeled
            List<Sketch.Substroke> substrokes = new List<Sketch.Substroke>();

            // If there is only one shape that you are relabeling, then you shouldn't change the orientation
            bool onlyOneShape = (originalLabels.Count == 1);
            double? orientation = null;

            foreach (Stroke stroke in inkStrokes)
			{
                // Find the corresponding substroke in the sketch and add it to our list
                Sketch.Substroke sub = sketchPanel.InkSketch.GetSketchSubstrokeByInk(stroke);
                substrokes.Add(sub);

                // Detect if we're grouping unrecognized strokes
                if (sub.ParentShape == null)
                    onlyOneShape = false;

                // If the shape has a user specified orientation, then keep it
                if (onlyOneShape && sub.ParentShape.AlreadyOriented)
                    orientation = sub.ParentShape.Orientation;
			}

            // Make a new shape out of these substrokes
            List<Sketch.Shape> shapesToRegroup;
            labeledShape = sketch.MakeNewShapeFromSubstrokes(out shapesToRegroup, substrokes, 
                labelType, 0, tagNumber, probability: 1.0);

            // Actually assign the orientation and mark it as untouchable
            if (orientation != null)
            {
                labeledShape.Orientation = orientation.Value;
                labeledShape.AlreadyOriented = true;
            }

            if (userSpecifiedLabel)
            {
                // Updates the orientation for the sake of the ghost gate
                if (labeledShape.Type.Classification == LogicDomain.GATE_CLASS && !labeledShape.AlreadyOriented)
                {
                    RecognitionInterfaces.Orienter orienter = RecognitionManager.RecognitionPipeline.createDefaultOrienter();
                    orienter.orient(labeledShape, sketchPanel.InkSketch.FeatureSketch);
                }

                // Update the shape name for text
                else if (labeledShape.Type.Classification == LogicDomain.TEXT_CLASS)
                {
                    RecognitionInterfaces.Recognizer recognizer = new Recognizers.TextRecognizer();
                    recognizer.processShape(labeledShape, sketchPanel.InkSketch.FeatureSketch);
                }
            }

            // Record the fact that user specified this grouping or label, 
            // so we don't accidentally change it in the future.
            labeledShape.AlreadyGrouped = userSpecifiedGroup;
            if (labelType != new ShapeType())
            {
                labeledShape.UserLabeled = userSpecifiedLabel;

                // Also, update the recognition to take this into account
                if (userSpecifiedLabel && ErrorCorrected != null) 
                    ErrorCorrected(labeledShape);
            }

            // Fun Fix
            // Problem description:
            //   Suppose you draw wire -> notgate -> wire
            //                      --------|>o--------
            //   Then you label the notgate as a wire, so the
            //   whole ensamble becomes a single wire. Then you
            //   relabel it as a notgate again. The two wires
            //   which *were* distinct are still part of the same
            //   wire mesh.
            // Problem solution:
            //   Here, when you apply a label to a group of 
            //   substrokes, we will "explode" all the wire shapes
            //   that were changed (break them into single-substroke
            //   shapes). We expect that they will be reconnected 
            //   later.

            List<Sketch.Shape> newShapesToRegroup = new List<Sketch.Shape>(shapesToRegroup);
            foreach (Sketch.Shape modifiedShape in shapesToRegroup)
            {
                if (Domain.LogicDomain.IsWire(modifiedShape.Type))
                {
                    List<Sketch.Shape> newShapes = sketch.ExplodeShape(modifiedShape);
                    newShapesToRegroup.Remove(modifiedShape);
                    newShapesToRegroup.AddRange(newShapes);
                }
            }
            shapesToRegroup = newShapesToRegroup;

            // Make sure the old connected shapes are updated with their
            // relationships to the newly labeled shape
            if (Regroup != null)
            {
                // Regroup everything so highlighting/labels are correctly updated
                // Ensures that the newly labeled shape's relationship to its
                // connected shapes are updated as well
                shapesToRegroup.Add(labeledShape);
                Regroup(new List<Sketch.Shape>(shapesToRegroup));
            }

            sketchPanel.EnableDrawing();
            return true;
		}


		/// <summary>
		/// Removes a labeled shape from a sketch.
        /// Postcondition: Original shapes are restored.
		/// </summary>
        public override bool UnExecute()
        {
            // Go through original shapes and restore them.
            foreach (var originalLabel in originalLabels)
            {
                Sketch.Shape oldShape = originalLabel.Key;

                List<Sketch.Substroke> originalSubstrokes = new List<Stroke>(originalLabel.Value.Item2).
                    ConvertAll(sketchPanel.InkSketch.GetSketchSubstrokeByInk);

                // Free our substrokes from their new shape.
                sketchPanel.InkSketch.Sketch.FreeSubstrokes(originalSubstrokes);

                // Put our substrokes back into the old shape.
                foreach (Sketch.Substroke substroke in originalSubstrokes)
                {
                    oldShape.AddSubstroke(substroke);
                }

                // Put the shape back into the sketch.
                if (!sketchPanel.InkSketch.Sketch.containsShape(oldShape))
                    sketchPanel.InkSketch.Sketch.AddShape(oldShape);
            }

            // Unlabel everything that was not labeled before.
            List<Sketch.Substroke> unknownSubstrokes = new List<Stroke>(unlabeledStrokes).
                ConvertAll(sketchPanel.InkSketch.GetSketchSubstrokeByInk);
            sketchPanel.InkSketch.Sketch.FreeSubstrokes(unknownSubstrokes);

            return true;
        }
	}
}
