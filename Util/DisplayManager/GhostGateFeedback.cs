using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sketch;
using SketchPanelLib;
using GateDrawing;
using System.Windows.Input;

namespace DisplayManager
{
    public class GhostGateFeedback : FeedbackMechanism
    {
        #region Internals

        /// <summary>
        /// The actual drawing
        /// </summary>
        private GateDrawing.GateDrawing gateDrawer;

        /// <summary>
        /// The currently showing ghost gates
        /// </summary>
        private List<GhostGate> currGhosts;

        /// <summary>
        /// The gate which apprears when you hover over the shape
        /// </summary>
        private GhostGate hoverGate;

        /// <summary>
        ///  The gate which is currently being rotated (none if no gate is currently being rotated)
        /// </summary>
        private GhostGate activatedGate;

        /// <summary>
        /// True if shapes should appear when the pen is held over strokes
        /// </summary>
        private bool shapesOnHover = true;

        /// <summary>
        /// True iff we should be rotating gates right now
        /// </summary>
        private bool rotateGates = true;

        /// <summary>
        /// Handler that changes the gate's orientation in the Sketch
        /// </summary>
        private GateRotatedHandler gateRotated;

        /// <summary>
        /// Mapping shapes to important subcircuit information for the Ghost gate
        /// </summary>
        public Dictionary<Sketch.Shape, KeyValuePair<List<string>, List<string>>> ShapeToIO;

        /// <summary>
        /// A mapping of shapes to their associated ghost gate
        /// </summary>
        private Dictionary<Sketch.Shape, GhostGate> ShapeToGate;

        private DisplayHelpTool displayHelpTool;
        #endregion

        #region Constructor and Subscription

        /// <summary>
        /// Constructor.  Makes gate drawer, initializes lists, subscribes to supplied panel
        /// </summary>
        /// <param name="parent"></param>
        public GhostGateFeedback(ref SketchPanel parent, GateRotatedHandler gateRotated)
            : base(ref parent)
        {
            // Set up gate drawer
            gateDrawer = new GateDrawing.GateDrawing();
            gateDrawer.RotateGates = true;
            gateDrawer.SnapRotation = false;
            gateDrawer.LockDrawingRatio = true;
            this.gateRotated = gateRotated;
            

            // Initialize our list of ghosts
            this.currGhosts = new List<GhostGate>();

            ShapeToIO = new Dictionary<Sketch.Shape, KeyValuePair<List<string>, List<string>>>();
            ShapeToGate = new Dictionary<Shape, GhostGate>();

            displayHelpTool = new DisplayHelpTool(ref sketchPanel);

            SubscribeToPanel();
        }

        /// <summary>
        /// Subscribe to events on the provided panel
        /// </summary>
        /// <param name="newParent"></param>
        public override void SubscribeToPanel(ref SketchPanel newParent)
        {
            base.SubscribeToPanel(ref newParent);
            SubscribeToPanel();
        }

        /// <summary>
        /// Subscribe to all events
        /// </summary>
        public void SubscribeToPanel()
        {
            if (subscribed)
                return;
            subscribed = true;

            sketchPanel.StylusDown += new StylusDownEventHandler(sketchPanel_StylusDown);
            sketchPanel.StylusInAirMove += new StylusEventHandler(sketchPanel_StylusInAirMove);
            sketchPanel.StylusUp += new StylusEventHandler(sketchPanel_StylusUp);
            sketchPanel.StylusMove += new StylusEventHandler(sketchPanel_StylusMove);
            
        }

        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            if (!subscribed)
                return;
            subscribed = false;

            RemoveAllGates();

            sketchPanel.StylusDown -= new System.Windows.Input.StylusDownEventHandler(sketchPanel_StylusDown);
            sketchPanel.StylusInAirMove -= new System.Windows.Input.StylusEventHandler(sketchPanel_StylusInAirMove);
            sketchPanel.StylusUp -= new StylusEventHandler(sketchPanel_StylusUp);
            sketchPanel.StylusMove -= new StylusEventHandler(sketchPanel_StylusMove);
        }

        #endregion

        #region Drawing and Removing Gates

        /// <summary>
        /// Draws ghost gates for all gates present on the InkCanvas
        /// </summary>
        public void DrawAllGates()
        {
            DrawFeedback(sketchPanel.Sketch.Shapes);
            sketchPanel.EnableDrawing();
        }

        /// <summary>
        /// Draw feedback for all the given shapes
        /// </summary>
        /// <param name="shapes"></param>
        public void DrawFeedback(IEnumerable<Sketch.Shape> shapes)
        {
            foreach (Sketch.Shape shape in shapes)
                DrawFeedback(shape, false, false);
        }

        /// <summary>
        /// Draws "ghost" gate for the supplied shape and sets the hoverGate (does nothing if shape is not
        /// a gate)
        /// </summary>
        /// <param name="gate"></param>
        /// <param name="pos"></param>
        public void DrawFeedback(Sketch.Shape shape, bool isHoverGate = false, bool drawingOneGate = true)
        {
            if (!Domain.LogicDomain.IsGate(shape.Type) || ShapeToGate.ContainsKey(shape))
                return;

            KeyValuePair<List<string>, List<string>> shapeIO;
            ShapeToIO.TryGetValue(shape, out shapeIO);
            GhostGate ghostGate = new GhostGate(shape, ref sketchPanel, ref gateDrawer, gateRotated, shapeIO);

            ShapeToGate[shape] = ghostGate;
            
            ghostGate.SubscribeEvents();

            currGhosts.Add(ghostGate);

            if (isHoverGate)
            {
                hoverGate = ghostGate;

                //if (shape.UserLabeled)
                  //  hoverGate.ShowDrawingAdvice();
            }

            if (shape.UserLabeled)
                ghostGate.ShowDrawingAdvice();

        }

        /// <summary>
        /// Removes all ghost gates from the canvas
        /// </summary>
        public void RemoveAllGates()
        {
            List<GhostGate> dummyList = new List<GhostGate>(currGhosts);
            foreach (GhostGate ghost in dummyList)
                RemoveGate(ghost);

            currGhosts.Clear();
        }

        /// <summary>
        /// Remove a single gate from the canvas 
        /// </summary>
        /// <param name="gate"></param>
        private void RemoveGate(GhostGate gate)
        {
            if (gate == null) return;

            if (gate == hoverGate)
                hoverGate = null;

            gate.remove();
            currGhosts.Remove(gate);
            ShapeToGate.Remove(gate.Shape);
        }

        /// <summary>
        /// Checks that each ghostgate is still valid (its Shape is still in the Sketch)
        /// </summary>
        public void Validate()
        {
            foreach (GhostGate gate in new List<GhostGate>(currGhosts))
                if (!sketchPanel.InkSketch.Sketch.containsShape(gate.Shape))
                    RemoveGate(gate);
        }

        #endregion

        #region Event Handlers
        /// <summary>
        /// Show ghost gate when you hover near a shape.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sketchPanel_StylusInAirMove(object sender, System.Windows.Input.StylusEventArgs e)
        {
            // Don't show gates when erasing, or if we don't have the right feedbacks turned on
            if ((!GatesOnHovering && !RotateGates) || e.StylusDevice.Inverted)
                return;

            // Get the shape we're looking for
            System.Windows.Point point = e.GetPosition(sketchPanel.InkCanvas);
            Shape shape = sketchPanel.Sketch.shapeAtPoint(point.X, point.Y, 100);

            //Don't allow rotation if anything is selected
            System.Windows.Ink.StrokeCollection selected = sketchPanel.InkCanvas.GetSelectedStrokes();
            if (selected.Count != 0)
            {
                RotateGates = false;
            }

            // Remove the old hovergate if it's not the one we're going to draw anyways
            if (shape == null || (hoverGate != null && hoverGate.Shape != shape))
                RemoveGate(hoverGate);

            // Draw the new hovergate, if possible and neccessary. Otherwise, grab it from the dictionary
            if (shape != null && !ShapeToGate.ContainsKey(shape))
                DrawFeedback(shape, true);

            // Change the cursor if we need to
            if (RotateGates && shape != null && shape.AlreadyLabeled && ShapeToGate.ContainsKey(shape) && ShapeToGate[shape].canActivate(point))
            {
                sketchPanel.UseCustomCursor = true;
                sketchPanel.Cursor = Cursors.Hand;
            }
            else
                sketchPanel.UseCustomCursor = false;
        }

        /// <summary>
        /// Tells the nearby shape that it's being rotated (if there's one) and hides the others.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sketchPanel_StylusDown(object sender, System.Windows.Input.StylusDownEventArgs e)
        {
            // Remove all gates on a pen down no matter what
            RemoveAllGates();

            // If we're erasing or not rotating gates, don't do anything else
            if (e.StylusDevice.Inverted || !RotateGates)
                return;

            // Clear past information
            activatedGate = null;
   
            // Get the nearest shape
            System.Windows.Point clickPoint = e.GetPosition(sketchPanel.InkCanvas);
            Shape nearbyShape = sketchPanel.Sketch.shapeAtPoint(clickPoint.X, clickPoint.Y, 100);

            // Only rotate AlreadyLabeled shapes
            if (nearbyShape == null || !nearbyShape.AlreadyLabeled)
                return;

            // Get the ghost gate for this shape
            DrawFeedback(nearbyShape, true);
            if (!ShapeToGate.ContainsKey(nearbyShape))
                return;
            GhostGate nearbyGate = ShapeToGate[nearbyShape];

            // If there was a viable gate nearby, activate it
            if (nearbyGate.canActivate(clickPoint))
            {
                activatedGate = nearbyGate;
                activatedGate.activate(clickPoint);

                sketchPanel.UseCustomCursor = true;
                sketchPanel.Cursor = Cursors.Hand;
                sketchPanel.DisableDrawing();
            }
            // Otherwise, take away the gate we just drew
            else
                RemoveAllGates();
        }

        /// <summary>
        /// Tells the gate that's currently being rotated that it's being rotated to a new point.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sketchPanel_StylusMove(object sender, System.Windows.Input.StylusEventArgs e)
        {
            if (RotateGates && activatedGate != null)
            {
                System.Windows.Point point = e.GetPosition(sketchPanel.InkCanvas);
                activatedGate.update(point);
            }
        }

        /// <summary>
        /// Tells the gate that's currently being rotated that it's been dropped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sketchPanel_StylusUp(object sender, System.Windows.Input.StylusEventArgs e)
        {
            if (RotateGates && activatedGate != null)
            {
                System.Windows.Point point = e.GetPosition(sketchPanel.InkCanvas);
                activatedGate.finish(point);
                RemoveGate(activatedGate);
                activatedGate = null;
                sketchPanel.UseCustomCursor = false;
                sketchPanel.EnableDrawing();
            }
        }

        #endregion

        #region Getters and Setters

        /// <summary>
        /// Get and set whether or not we are showing gates when the stylus is over the shape.
        /// 
        /// If we're able to rotate gates, we also need to be showing gates upon hovering
        /// </summary>
        public bool GatesOnHovering
        {
            get { return shapesOnHover; }
            set 
            { 
                shapesOnHover = value;
                if (RotateGates)
                    shapesOnHover = true;
            }
        }

        /// <summary>
        /// Gets and sets whether or not we can rotate gates.
        /// 
        /// If we're able to rotate gates, we also need to be showing gates upon hovering
        /// </summary>
        public bool RotateGates
        {
            get { return rotateGates; }
            set 
            { 
                rotateGates = value;
                if (rotateGates)
                    GatesOnHovering = true;
            }
        }

        #endregion
    }
}
