using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Controls;

using CommandManagement;
using System.Windows;
using InkToSketchWPF;
using SketchPanelLib;

namespace SketchPanelLib.CommandList
{
    public delegate void EndpointMoveHandlerRegroup(List<Sketch.Shape> shape);
    public delegate void EndpointMoveHandler(Sketch.Shape shape);
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
    public class EndPointMoveCmd : Command
    {
        #region Internals

        private InkToSketchWPF.InkCanvasSketch inkSketch;
        private Sketch.EndPoint oldLocation;
        private bool oldInternalConnection;
        private bool newIsWire;
        private Point newLocation;
        private Sketch.Shape changedShape;
        private Sketch.Shape shapeAtNewLoc;
        private IEnumerable<Sketch.Substroke> substrokesInNewShape;
        private Stroke attachedStroke;
        private Stroke newStroke;
        public event EndpointMoveHandlerRegroup Regroup;
        public event EndpointMoveHandler Handle;

        #endregion


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sketch">SketchPanel to add a label to</param>
        /// <param name="inkStrokes">InkOverlay strokes</param>
        /// <param name="inkStrokes">A StrokeCollection strokes</param>
        /// <param name="label">Label to apply</param>
        /// <param name="domainInfo">DomainInfo for our Labeler</param>
        public EndPointMoveCmd(InkToSketchWPF.InkCanvasSketch inkSketch, Sketch.EndPoint oldLoc, Point newLoc, Stroke attachedStroke, Sketch.Shape shapeAtNewLoc)
        {
            isUndoable = true;
            this.inkSketch = inkSketch;
            oldLocation = oldLoc;
            newLocation = newLoc;
            this.attachedStroke = attachedStroke;
            this.shapeAtNewLoc = shapeAtNewLoc;
            this.changedShape = inkSketch.GetSketchSubstrokeByInk(attachedStroke).ParentShape;
            oldInternalConnection = changedShape.ConnectedShapes.Contains(changedShape);
            newIsWire = Domain.LogicDomain.IsWire(shapeAtNewLoc.Type);
            substrokesInNewShape = shapeAtNewLoc.SubstrokesL;

            // Make a new stroke comprising 100 points along the straight line between oldLocation and newLocation
            System.Windows.Input.StylusPointCollection line = new System.Windows.Input.StylusPointCollection();
            for (double m = 0; m <= 1; m += 0.01)
            {
                double midX = oldLocation.X + m * (newLocation.X - oldLocation.X);
                double midY = oldLocation.Y + m * (newLocation.Y - oldLocation.Y);
                line.Add(new System.Windows.Input.StylusPoint(midX, midY));
            }
            this.newStroke = new Stroke(line);
        }

        /// <summary>
        /// Type of this command, to tell it apart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "EndpointMove";
        }

        /// <summary>
        /// Applies a label to a group of substrokes.
        /// </summary>
        public override bool Execute()
        {
            // Add the new stroke to the sketch
            inkSketch.AddStroke(newStroke);

            // Update connections
            Sketch.Substroke newSubstroke = inkSketch.GetSketchSubstrokeByInk(newStroke);

            // If we're using on-the-fly recognition, the new substroke may already have a parent shape.  Get rid of it!
            if (newSubstroke.ParentShape != null)
            {
                Sketch.Shape parent = newSubstroke.ParentShape;
                parent.RemoveSubstroke(newSubstroke);
                if (parent.SubstrokesL.Count == 0)
                    inkSketch.Sketch.RemoveShape(parent);
            }

            changedShape.AddSubstroke(newSubstroke);
            oldLocation.ConnectedShape = changedShape;
            newSubstroke.Endpoints[0].ConnectedShape = changedShape;
            newSubstroke.Endpoints[1].ConnectedShape = shapeAtNewLoc;

            // Add the new wire to the old wire's shape
            inkSketch.Sketch.connectShapes(changedShape, changedShape); // wires connected to themselves indicates an internal connection
            inkSketch.Sketch.connectShapes(changedShape, shapeAtNewLoc);
            changedShape.AlreadyLabeled = true;

            // Pre-emptive merging! Helps us undo.
            if (newIsWire)
                inkSketch.Sketch.mergeShapes(changedShape, shapeAtNewLoc);

            // Regroup everything so highlighting/labels are correctly updated
            Handle(changedShape);
            Regroup(new List<Sketch.Shape> { changedShape });
            return true;
        }


        /// <summary>
        /// Undoes the endpoint move
        /// </summary>
        public override bool UnExecute()
        {
            // We merged these earlier, so let's break them up.
            if (newIsWire)
                shapeAtNewLoc =  inkSketch.Sketch.BreakOffShape(changedShape, substrokesInNewShape);

            // Disconnenct where we need to
            inkSketch.Sketch.disconnectShapes(changedShape, shapeAtNewLoc);
            if (!oldInternalConnection)
                inkSketch.Sketch.disconnectShapes(changedShape, changedShape);

            oldLocation.ConnectedShape = null;
            inkSketch.DeleteStroke(newStroke);

            Handle(changedShape);

            return true;
        }
    }
}
