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
    class StrokeRemoveCmd : Command
    {
        /// <summary>
        /// The strokes to be removed
        /// </summary>
        private StrokeCollection removedStrokes;

        /// <summary>
        /// A mapping of removed strokes to their parent shapes
        /// </summary>
        private Dictionary<Stroke, Sketch.Shape> strokesToShapes;

        /// <summary>
        /// The ink canvas sketch we're dealing with
        /// </summary>
        private InkToSketchWPF.InkCanvasSketch inkSketch;

        /// <summary>
        /// Constructor for removing a single stroke
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="stroke"></param>
        public StrokeRemoveCmd(ref InkToSketchWPF.InkCanvasSketch sketch, Stroke stroke)
            : this(ref sketch, new StrokeCollection(Data.Utils.singleEntryList(stroke)))
        {
        }

        /// <summary>
        /// Constructor for removing multiple strokes
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="strokes"></param>
        public StrokeRemoveCmd(ref InkToSketchWPF.InkCanvasSketch sketch, StrokeCollection strokes)
        {
            isUndoable = true;
            inkSketch = sketch;
            removedStrokes = strokes;
            MakeDictionary();
        }

        /// <summary>
        /// Make a mapping from ink strokes to parent shapes
        /// </summary>
        private void MakeDictionary()
        {
            strokesToShapes = new Dictionary<Stroke, Sketch.Shape>();
            foreach (Stroke stroke in removedStrokes)
            {
                Sketch.Shape parent = inkSketch.GetSketchSubstrokeByInk(stroke).ParentShape;
                strokesToShapes[stroke] = parent;
            }
        }

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "StrokeRemove";
        }

        /// <summary>
        /// Remove the stroke
        /// Postcondition: It's not in the inkcanvas's list nor the inksketch's list
        /// </summary>
        public override bool Execute()
        {
            inkSketch.DeleteStrokes(removedStrokes);
            return true;
        }

        /// <summary>
        /// Unexecute the Command
        /// Precondition: The stroke has been removed from the inkcanvas's list and the inksketch's list
        /// Postcondition: It's in the inkcanvas's list and the inksketch's list
        /// The original shapes should still be around.
        /// </summary>
        public override bool UnExecute()
        {
            foreach (Stroke stroke in strokesToShapes.Keys)
            {
                inkSketch.AddStroke(stroke);

                Sketch.Shape parent = strokesToShapes[stroke];
                Sketch.Substroke sub = inkSketch.GetSketchSubstrokeByInk(stroke);

                if (sub != null && parent != null)
                {
                    parent.AddSubstroke(sub);

                    if (!inkSketch.Sketch.Shapes.Contains(parent))
                        inkSketch.Sketch.AddShape(parent);
                }
            }

            return true;
        }
    }
}
