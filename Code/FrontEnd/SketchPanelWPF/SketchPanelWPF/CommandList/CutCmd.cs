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
    class CutCmd : CopyCmd
    {	
		/// <summary>
		/// Constructor
		/// </summary>
        public CutCmd(InkToSketchWPF.InkCanvasSketch sketch) : base(sketch)
        {
            isUndoable = true;
        }

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "Cut";
        }


		/// <summary>
		/// Removes the strokes from the sketch
        /// Precondition: The strokes should be in the inkCanvas and the inkCanvasSketch.
		/// </summary>
        public override bool Execute()
		{
            inkSketch.DeleteStrokes(StoredStrokes);

            inkSketch.InkCanvas.Select(new StrokeCollection());

            return StoredStrokes.Count > 0;
		}


		/// <summary>
		/// Removes the cut strokes from the clipboard and pastes them back to the inkCanvas
		/// </summary>
        public override bool UnExecute()
        {

            if (StoredStrokes == null || StoredStrokes.Count == 0)
                return false;

            inkSketch.AddStrokes(StoredStrokes);

            inkSketch.InkCanvas.Select(StoredStrokes);

            return true;
        }
    }
}
