using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommandManagement;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows;
using SketchPanelLib;

namespace SelectionManager.CommandList
{
    public delegate void StrokesAddedEventHandler(StrokeCollection added, string classification);

    class RedrawCmd : Command
    {
        #region Internals

        /// <summary>
        /// Is the command undoable?
        /// </summary>
        private bool isUndoable = true;

        /// <summary>
        /// Sketch that contains the strokes
        /// </summary>
        private SketchPanel sketchPanel;

        /// <summary>
        /// Alerts the redrawing tool when strokes have been added.
        /// </summary>
        public event StrokesAddedEventHandler StrokesAdded;

        /// <summary>
        /// The strokes that are being drawn over
        /// </summary>
        private StrokeCollection OldStrokes;

        /// <summary>
        /// The strokes that are replacing the old ones
        /// </summary>
        private StrokeCollection NewStrokes;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public RedrawCmd(SketchPanel panel, StrokeCollection oldStrokes, StrokeCollection newStrokes)
        {
            sketchPanel = panel;
            OldStrokes = new StrokeCollection();
            NewStrokes = new StrokeCollection();

            NewStrokes.Add(newStrokes);
            OldStrokes.Add(oldStrokes);
        }

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string getType()
        {
            return "redraw";
        }


        /// <summary>
        /// Replace the old strokes with the new strokes
        /// </summary>
        public override bool Execute()
        {
            if (OldStrokes.Count == 0 || NewStrokes.Count == 0)
                return false;

            string classification = sketchPanel.InkSketch.GetSketchSubstrokeByInk(OldStrokes[0]).Classification;

            foreach (Stroke stroke in OldStrokes)
            {
                if (sketchPanel.InkCanvas.Strokes.Contains(stroke))
                {
                    sketchPanel.InkCanvas.Strokes.Remove(stroke);
                    sketchPanel.InkSketch.mStrokes.Remove(stroke);
                }
            }

            foreach (Stroke stroke in NewStrokes)
            {
                if (!sketchPanel.InkCanvas.Strokes.Contains(stroke))
                {
                    sketchPanel.InkCanvas.Strokes.Add(stroke);
                    sketchPanel.InkSketch.mStrokes.Add(stroke);
                }
            }

            StrokesAdded(NewStrokes, classification);

            return true;
        }


        /// <summary>
        /// Replace the new strokes with the old strokes
        /// </summary>
        public override bool UnExecute()
        {
            if (OldStrokes.Count == 0 || NewStrokes.Count == 0)
                return false; 
            
            string classification = sketchPanel.InkSketch.GetSketchSubstrokeByInk(NewStrokes[0]).Classification;
            
            foreach (Stroke stroke in NewStrokes)
            {
                if (sketchPanel.InkCanvas.Strokes.Contains(stroke))
                {
                    sketchPanel.InkCanvas.Strokes.Remove(stroke);
                    sketchPanel.InkSketch.mStrokes.Remove(stroke);
                }
            }

            foreach (Stroke stroke in OldStrokes)
            {
                if (!sketchPanel.InkCanvas.Strokes.Contains(stroke))
                {
                    sketchPanel.InkCanvas.Strokes.Add(stroke);
                    sketchPanel.InkSketch.mStrokes.Add(stroke);
                }
            }

            StrokesAdded(OldStrokes, classification);

            return true;
        }

        public override KeyValuePair<StrokeCollection, Rect>? SetClipboard(KeyValuePair<StrokeCollection, Rect>? clipboard)
        {
            return clipboard;
        }

        /// <summary>
        /// Returns if the command is undoable.
        /// </summary>
        /// <returns>True iff the command is undoable</returns>
        public override bool IsUndoable()
        {
            return this.isUndoable;
        }
    }
}
