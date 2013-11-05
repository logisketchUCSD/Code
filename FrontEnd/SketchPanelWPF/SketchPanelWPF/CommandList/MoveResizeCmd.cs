using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommandManagement;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows;

namespace SketchPanelLib.CommandList
{
    class MoveResizeCmd : Command
    {
        #region Internals

        /// <summary>
        /// SKetch that contains the strokes
        /// </summary>
        private InkToSketchWPF.InkCanvasSketch inkSketch;

        /// <summary>
        /// The strokes that have been resized
        /// </summary>
        private StrokeCollection resizedStrokes;

        /// <summary>
        /// A dictionary of the shapes in the selection to the shape they were broken off of, if any.
        /// </summary>
        private Dictionary<Sketch.Shape, Sketch.Shape> oldShapesToNewShapes;

        /// <summary>
        /// The old location of the strokes
        /// </summary>
        private Rect oldBounds;

        /// <summary>
        /// The new location of the strokes
        /// </summary>
        private Rect newBounds;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MoveResizeCmd(ref InkToSketchWPF.InkCanvasSketch sketch, StrokeCollection resizedStrokes,
            Rect oldBounds, Rect newBounds)
        {
            isUndoable = true;
            inkSketch = sketch;
            this.oldBounds = oldBounds;
            this.newBounds = newBounds;
            this.resizedStrokes = resizedStrokes;
            oldShapesToNewShapes = new Dictionary<Sketch.Shape, Sketch.Shape>();
        }

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "MoveResize";
        }


        /// <summary>
        /// Resizes a group of strokes
        /// Preconditions: The strokes are in the inkcanvas.
        /// </summary>
        public override bool Execute()
        {
            Resize(newBounds);
            BreakShapes();

            return resizedStrokes.Count > 0;
        }


        /// <summary>
        /// Undos a resize of strokes
        /// Precondition: The strokes are in the Inkcanvas.
        /// </summary>
        public override bool UnExecute()
        {
            UnbreakShapes();
            Resize(oldBounds);
            inkSketch.InkCanvas.Select(resizedStrokes);

            return resizedStrokes.Count > 0;
        }

        /// <summary>
        /// Resizes the ink strokes and updates our data structures.
        /// Preconditions: The strokes are already in the inkcanvas.
        /// </summary>
        /// <param name="NewSize"></param>
        private void Resize(Rect NewSize)
        {
            Rect CurrSize = resizedStrokes.GetBounds();
            System.Windows.Media.Matrix resizeMatrix = new System.Windows.Media.Matrix();
            resizeMatrix.ScaleAt(NewSize.Width / CurrSize.Width, NewSize.Height / CurrSize.Height, CurrSize.X, CurrSize.Y);
            resizeMatrix.Translate(NewSize.X - CurrSize.X, NewSize.Y - CurrSize.Y);
            resizedStrokes.Transform(resizeMatrix, false);
            inkSketch.UpdateInkStrokes(resizedStrokes);
        }

        /// <summary>
        /// Break any shapes which are only partially in this selection
        /// </summary>
        private void BreakShapes()
        {
            foreach (Stroke stroke in resizedStrokes)
            {
                Sketch.Substroke substroke = inkSketch.GetSketchSubstrokeByInk(stroke);
                Sketch.Shape parent = substroke.ParentShape;

                if (parent != null && !oldShapesToNewShapes.ContainsKey(parent))
                {
                    bool breakShape = false;
                    List<Sketch.Substroke> substrokes = new List<Sketch.Substroke>();

                    foreach (Sketch.Substroke sub in parent.Substrokes)
                    {
                        if (!resizedStrokes.Contains(inkSketch.GetInkStrokeBySubstroke(sub)))
                            breakShape = true;
                        else
                            substrokes.Add(sub);
                    }

                    if (breakShape)
                    {
                        Sketch.Shape newShape = inkSketch.Sketch.BreakOffShape(parent, substrokes);
                        oldShapesToNewShapes[newShape] = parent;
                    }
                }
            }
        }

        /// <summary>
        /// Heals any shapes which were only partially in this selection
        /// </summary>
        private void UnbreakShapes()
        {
            foreach (Sketch.Shape newParent in oldShapesToNewShapes.Keys)
                inkSketch.Sketch.mergeShapes(oldShapesToNewShapes[newParent], newParent);
            oldShapesToNewShapes.Clear();
        }


        /// <summary>
        /// Reselect the strokes after the inkchanged command has completed.
        /// </summary>
        public void selectStoredStrokes()
        {
            inkSketch.InkCanvas.Select(resizedStrokes);
        }
    }
}
