using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommandManagement;
using System.Windows.Ink;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

namespace SketchPanelLib.CommandList
{
    class StrokeAddCmd : Command
    {
        private Stroke thisStroke;

        private InkToSketchWPF.InkCanvasSketch inkSketch;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="sketch"></param>
        /// <param name="stroke"></param>
        public StrokeAddCmd(ref InkToSketchWPF.InkCanvasSketch sketch, Stroke stroke)
        {
            isUndoable = true;
            inkSketch = sketch;
            thisStroke = stroke;
        }

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "StrokeAdd";
        }
        /// <summary>
        /// Add the stroke
        /// </summary>
        public override bool Execute()
        {
            inkSketch.AddStroke(thisStroke);
            return true;
        }

        /// <summary>
        /// Unexecute the Command
        /// </summary>
        public override bool UnExecute()
        {
            inkSketch.DeleteStroke(thisStroke);
            return true;
        }     
    }
}
