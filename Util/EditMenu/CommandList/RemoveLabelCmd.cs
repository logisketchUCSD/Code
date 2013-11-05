using System;
using System.Collections.Generic;
using System.Windows.Ink;

using CommandManagement;
using Domain;
using System.Windows;
using SketchPanelLib;
using InkToSketchWPF;
using System.Windows.Controls;

namespace EditMenu.CommandList
{
    /// <summary>
	/// Summary description for RemoveLabelCmd.
    /// 
    /// WARNING: Not currently used, so probably out of date.
    /// ApplyLabelCmd should be used for all labeling needs
	/// </summary>
	public class RemoveLabelCmd : Command
    {
        #region Internals
	
		/// <summary>
		/// SketchPanel to contain the labeled shape
		/// </summary>
        private SketchPanel sketchPanel;
	
		/// <summary>
		/// Necessary to apply and undo InkOverlay changes
		/// </summary>
		private StrokeCollection inkStrokes;
		
		/// <summary>
		/// Label to apply
		/// </summary>
		private ShapeType label;

		/// <summary>
		/// Label's color
		/// </summary>
		private System.Windows.Media.Color labelColor;

		/// <summary>
		/// Labeled shape resulting from applying a label
		/// </summary>
		private Sketch.Shape labeledShape;

        private StrokeCollection labeledStrokes;

        #endregion

        /// <summary>
		/// Constructor
		/// </summary>
		public RemoveLabelCmd(SketchPanel sketch, StrokeCollection inkStrokes, string label)
		{
            isUndoable = true;

            sketchPanel = sketch;
			this.inkStrokes = inkStrokes;
            this.label = Domain.LogicDomain.getType(label);

            labelColor = LogicDomain.getType(label).Color;

            labeledStrokes = new StrokeCollection();
            foreach (Stroke stroke in inkStrokes)
            {
                if (stroke.DrawingAttributes.Color == labelColor)
                    labeledStrokes.Add(stroke);
            }
		}

        public override string Type()
        {
            return "RemoveLabel";
        }


		/// <summary>
		/// Removes a labeled shape from a Sketch.
		/// </summary>
        public override bool Execute()
		{
            bool success = false;
            foreach (Stroke stroke in labeledStrokes)
            {
                Sketch.Substroke sub = sketchPanel.InkSketch.GetSketchSubstrokeByInk(stroke);
                labeledShape = sub.RemoveLabel(label);

                if (this.labeledShape != null && this.labeledShape.Substrokes.Length == 0)
                    success = sketchPanel.InkSketch.Sketch.RemoveShape(this.labeledShape);
            }
            return success;
		}


		/// <summary>
		/// Applies a label to a group of substrokes.
		/// </summary>
        public override bool UnExecute()
		{

            List<Sketch.Shape> modifiedShapes;
			foreach (System.Windows.Ink.Stroke stroke in labeledStrokes)
			{
				stroke.DrawingAttributes.Color = labelColor;
                Sketch.Substroke sub = sketchPanel.InkSketch.GetSketchSubstrokeByInk(stroke);
                sketchPanel.InkSketch.Sketch.MakeNewShapeFromSubstroke(out modifiedShapes, sub, label);
			}

            return true;
		}
	}
}
