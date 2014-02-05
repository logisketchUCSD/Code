using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandManagement;
using System.Windows.Ink;

namespace SketchPanelLib.CommandList
{
    // This re-recognizes the selected shapes and reconnects the circuit.
    public delegate void RerecognizeShapes(List<Sketch.Shape> shapes);

    public class ShapeRotateCmd : Command
    {
        private double oldOrientation;
        private bool oldOrientationWasUserSpecified;
        private double newOrientation;
        private Sketch.Shape shape;
        public event RerecognizeShapes Rerecognize;

        public ShapeRotateCmd(Sketch.Shape shape, double newOrientation)
        {
            this.shape = shape;
            this.oldOrientation = shape.Orientation;
            this.oldOrientationWasUserSpecified = shape.AlreadyOriented;
            this.newOrientation = newOrientation;
            this.isUndoable = true;
        }

        public override string Type()
        {
            return "RotateShape";
        }

        /// <summary>
        /// Reset the orientation of the shape.
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            shape.Orientation = newOrientation;
            shape.AlreadyOriented = true;
            Rerecognize(Data.Utils.singleEntryList(shape));
            return true;
        }

        public override bool UnExecute()
        {
            shape.Orientation = oldOrientation;
            shape.AlreadyOriented = oldOrientationWasUserSpecified;
            Rerecognize(Data.Utils.singleEntryList(shape));
            return true;
        }

    }
}
