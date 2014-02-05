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
    class CopyCmd : Command
    {
		/// <summary>
		/// Sketch that contains the strokes
		/// </summary>
        protected InkToSketchWPF.InkCanvasSketch inkSketch;

        /// <summary>
        /// The strokes that have been copied to the clipboard
        /// </summary>
        protected StrokeCollection StoredStrokes;

		/// <summary>
		/// Constructor
		/// </summary>
        public CopyCmd(InkToSketchWPF.InkCanvasSketch sketch)
        {
            isUndoable = false;
            inkSketch = sketch;

            StoredStrokes = inkSketch.InkCanvas.GetSelectedStrokes();
		}

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "Copy";
        }


		/// <summary>
		/// Copies a group of strokes to the clipboard
		/// </summary>
        public override bool Execute()
	    {
            inkSketch.InkCanvas.Select(StoredStrokes);
            return StoredStrokes.Count > 0;
		}


		/// <summary>
		/// Selects the strokes that were originally copied.  Will not be called as long as isUndoable == false
		/// </summary>
        public override bool UnExecute()
		{
            inkSketch.InkCanvas.Select(StoredStrokes);
            return StoredStrokes.Count > 0;
		}

        public override KeyValuePair<StrokeCollection, Rect>? SetClipboard(KeyValuePair<StrokeCollection, Rect>? clipboard)
        {
            return new KeyValuePair<StrokeCollection, Rect>(StoredStrokes, StoredStrokes.GetBounds());
        }
    }
}
