using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommandManagement;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows;
using System.Windows.Input;

namespace SketchPanelLib.CommandList
{
    class PasteCmd : Command
    {
        #region Internals
	
		/// <summary>
		/// Canvas that contains the strokes
		/// </summary>
        private InkCanvas inkCanvas;

        /// <summary>
        /// Makes sure that we do not calculate the pasting unecessarily
        /// </summary>
        private bool PastedOnce;

        /// <summary>
        /// The sketch that contains the strokes
        /// </summary>
        private InkToSketchWPF.InkCanvasSketch inkSketch;

        /// <summary> 
        /// Offset for pasting strokes
        /// </summary>
        private static int OFFSET = 5;
        
        /// <summary>
        /// The strokes to be replaced, if any
        /// </summary>
        private StrokeCollection ReplacedStrokes;

        /// <summary>
        /// The strokes to be pasted
        /// </summary>
        private StrokeCollection PastedStrokes;

        /// <summary>
        /// The original location of the pasted strokes
        /// </summary>
        private Rect OldLoc;

        /// <summary>
        /// The new point at which to paste the strokes
        /// </summary>
        private Point PastePoint;

        #endregion

		/// <summary>
		/// Constructor
		/// </summary>
		public PasteCmd(InkCanvas canvas, InkToSketchWPF.InkCanvasSketch sketch, Point pastePoint)
        {
            isUndoable = true;
            PastedOnce = false;
            inkCanvas = canvas;
            inkSketch = sketch;
            PastePoint = pastePoint;

            ReplacedStrokes = inkCanvas.GetSelectedStrokes();
		}

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "Paste";
        }


		/// <summary>
		/// Pastes a group of strokes to the inkCanvas
		/// </summary>
        public override bool Execute()
		{
            if (PastedStrokes == null)
            {
                Console.WriteLine("Cannot paste: no strokes to paste");
                return false;
            }

            // If the bounds of the pasted strokes and the old bounds are not 
            // the same, we've already pasted once and shouldn't recalculate.
            if (PastedOnce)
            {
                replace(ReplacedStrokes, PastedStrokes);
                return true;
            }

            // Determine where we want to paste the strokes
            if (ReplacedStrokes != null && ReplacedStrokes.Count > 0)
            {
                PastePoint = ReplacedStrokes.GetBounds().TopLeft;
            }
            else if (PastePoint.X == -1 && PastePoint.Y == -1)
            {
                PastePoint = new Point(OldLoc.X + OFFSET, OldLoc.Y + OFFSET);
            }

            StrokeCollection CopiedStrokes = new StrokeCollection();

            foreach (Stroke stroke in PastedStrokes)
            {
                StylusPoint styluspoint;
                StylusPointCollection styluspoints = new StylusPointCollection();

                for (int j = 0; j < stroke.StylusPoints.Count; j++)
                {
                    styluspoint = stroke.StylusPoints[j];
                    styluspoint.X = styluspoint.X - OldLoc.X + PastePoint.X;
                    styluspoint.Y = styluspoint.Y - OldLoc.Y + PastePoint.Y;
                    styluspoints.Add(styluspoint);
                }

                CopiedStrokes.Add(new Stroke(styluspoints));
            }

            PastedStrokes = CopiedStrokes;
            replace(ReplacedStrokes, PastedStrokes);
            //inkCanvas.Select(PastedStrokes);

            PastedOnce = true;
            return PastedStrokes.Count > 0;
		}

        /// <summary>
        /// Removes the old collection, inserts the newer one.
        /// </summary>
        /// <param name="old"></param>
        /// <param name="newer"></param>
        /// <returns></returns>
        private void replace(StrokeCollection old, StrokeCollection newer)
        {

            if (old != null)
            {
                inkSketch.DeleteStrokes(old);
            }

            if (newer != null)
            {
                inkSketch.AddStrokes(newer);
                inkCanvas.Select(newer);
            }
        }


		/// <summary>
		/// Removes the pasted strokes from the inkCanvas
		/// </summary>
        public override bool UnExecute()
        {
            if (PastedStrokes == null || PastedStrokes.Count == 0)
                return false;

            replace(PastedStrokes, ReplacedStrokes);
            inkCanvas.Select(ReplacedStrokes);

            return true;
        }

        public override KeyValuePair<StrokeCollection, Rect>? SetClipboard(KeyValuePair<StrokeCollection, Rect>? clipboard)
        {
            if (clipboard != null && ((KeyValuePair<StrokeCollection, Rect>)clipboard).Key.Count != 0)
            {
                PastedStrokes = ((KeyValuePair<StrokeCollection, Rect>)clipboard).Key;
                OldLoc = ((KeyValuePair<StrokeCollection, Rect>)clipboard).Value;
            }
            
            return clipboard;
        }
    }
}
