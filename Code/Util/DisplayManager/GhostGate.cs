using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using CircuitParser;
using System.Windows.Controls.Primitives;

namespace DisplayManager
{
    public delegate void GateRotatedHandler(Sketch.Shape rotatedShape, double newOrientation);

    class GhostGate
    {
        #region Internals

        /// <summary>
        /// Image that has the drawing as a source and is added to the inkcanvas. Also Handles events
        /// </summary>
        private System.Windows.Controls.Image relabelImage;

        private GateDrawing.GateDrawing gateDrawer;

        /// <summary>
        /// The associated sketch panel
        /// </summary>
        private SketchPanelLib.SketchPanel sketchPanel;
        /// <summary>
        /// Shape that this gate is drawing the computer model of. It is kept around to update orientation
        /// if user specified.
        /// </summary>
        private Sketch.Shape shape;

        /// <summary>
        /// For user specified orientation, where the user pen downed
        /// </summary>
        private System.Windows.Point startPoint;

        /// <summary>
        /// For user specified orientation, where the user pens up after starting the rotation
        /// </summary>
        private System.Windows.Point endPoint;

        /// <summary>
        /// be absolutely sure not to subscribe if you already have the events
        /// </summary>
        private bool subscribed;

        /// <summary>
        /// If it's a subcircuit, display the name as part of the ghost gate
        /// </summary>
        DockPanel SubCircuitDock;

        /// <summary>
        /// Handler that changes the gate's orientation
        /// </summary>
        private GateRotatedHandler gateRotated;

        private KeyValuePair<List<string>, List<string>> IO;

        private Popup drawingAdvice;

        #endregion

        #region Constructor
        /// <summary>
        /// The ghost gate to be drawn and added to the Sketch. The ghost gates are tracked in
        /// Edit Menu in currGhosts. It delegates when to draw and undraw these. The Ghosts have
        /// the shape that is associated with it so that it can update Orientation.
        /// 
        /// Also the name Ghost gate is super cool
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="SketchPanel"></param>
        public GhostGate(Sketch.Shape shape, ref SketchPanelLib.SketchPanel sketchPanel, 
            ref GateDrawing.GateDrawing gateDrawer, GateRotatedHandler gateRotated, KeyValuePair<List<string>, List<string>> IO = new KeyValuePair<List<string>, List<string>>())
        {
            // Initialize everything
            startPoint = new System.Windows.Point();
            endPoint = new System.Windows.Point();
            subscribed = false;
            this.shape = shape;
            this.sketchPanel = sketchPanel;
            this.gateDrawer = gateDrawer;
            this.gateRotated = gateRotated;
            this.SubCircuitDock = new DockPanel();
            this.IO = IO;

            if (!Domain.LogicDomain.IsGate(shape.Type))
                return;

            // It makes no sense to lock the drawing ratio if this is a subcircuit, but we need to set it back to normal after
            bool shouldLock = this.gateDrawer.LockDrawingRatio;
            if (shape.Type == Domain.LogicDomain.SUBCIRCUIT)
                gateDrawer.LockDrawingRatio = false;

            // Make the desired image
            GeometryDrawing ghostGate = gateDrawer.DrawGate(shape.Type, shape.Bounds, false, true, shape.Orientation);
            
            // Make textbox to go with the image
            Popup popup = new Popup();
            TextBlock popupText = new TextBlock();
            popupText.Text = gateDrawer.DrawingAdvice;
            popupText.TextWrapping = System.Windows.TextWrapping.Wrap;
            popupText.Background = Brushes.Pink;
            popupText.Foreground = Brushes.Black;
            popup.Child = popupText;
            popup.IsOpen = false;
            popup.AllowsTransparency = true;
            popup.Visibility = System.Windows.Visibility.Visible;
            popup.PlacementTarget = sketchPanel.InkCanvas;
            popup.Placement = PlacementMode.RelativePoint;
            popup.HorizontalOffset = 20;
            popup.VerticalOffset = 50;
            drawingAdvice = popup;

            gateDrawer.LockDrawingRatio = shouldLock;
    
            DrawingImage drawingImage = new DrawingImage(ghostGate);
            relabelImage = new System.Windows.Controls.Image();
            relabelImage.Source = drawingImage;
            relabelImage.Width = drawingImage.Width;
            relabelImage.Height = drawingImage.Height;
            
            // Actually add the image
            InkCanvas.SetLeft(relabelImage, shape.Bounds.Left);
            InkCanvas.SetTop(relabelImage, shape.Bounds.Top);

            // If it's a subcircuit we need to display the name and where inputs/ouputs should be
            if (shape.Type == Domain.LogicDomain.SUBCIRCUIT)
                addIO(shape.Orientation);

            sketchPanel.InkCanvas.Children.Add(relabelImage);
            sketchPanel.InkCanvas.Children.Add(drawingAdvice);
        }

        #endregion

        #region sub circuit stuff
        /// <summary>
        /// Add the io stuff to the gate
        /// </summary>
        private void addIO(double orientation)
        {
            int MULTIPLY = 3;
            int DIVIDE = 4;
            int FIX_HEIGHT = 14;
            if (IO.Key == null)
                return;
            SubCircuitDock = new DockPanel();
            SubCircuitDock.Width = shape.Bounds.Width;
            SubCircuitDock.Height = shape.Bounds.Height;

            TextBlock name = new TextBlock();
            name.Text = shape.Name.Substring(0, shape.Name.Length - 2);
            name.TextWrapping = TextWrapping.Wrap;
            name.Width = MULTIPLY * SubCircuitDock.Width / DIVIDE;

            // Input and output padding calculate exactly the same way
            // 1) find the amount of free space after considering the space taken by text
            // 2) figure out how many pixels per spacer (one fewer spacer than text a  1 b  2 c)
            // 3) if there is still more inputs add a margin to the textblock
            int emptyspacei =(int) SubCircuitDock.Height - (FIX_HEIGHT * (IO.Key.Count + 1));
            int numberofspacersi = IO.Key.Count - 1;
            int pixelsperspacei; //pixels per space for the inputs
  
            // There is only one input
            if (numberofspacersi == 0)
                pixelsperspacei = emptyspacei;
            else 
                pixelsperspacei = emptyspacei / numberofspacersi;

            DockPanel inputs = new DockPanel();
            foreach (string input in IO.Key)
            {
                TextBlock inputText = new TextBlock();
                //Dock on the top for all so that dockpanel takes care of placing them
                DockPanel.SetDock(inputText, Dock.Top);
                // space gets it off the left wall
                inputText.Text = " " + input;

                //Add the margin below the text if there are still more things to be added
                if (IO.Key.IndexOf(input) < numberofspacersi)
                    inputText.Margin = new Thickness(0, 0, 0, pixelsperspacei);
                inputs.Children.Add(inputText);
            }

            DockPanel.SetDock(inputs, Dock.Left);

            int emptyspaceo = (int)SubCircuitDock.Height - (FIX_HEIGHT * (IO.Value.Count + 1));
            int numberofspacerso = IO.Value.Count - 1;
            int pixelsperspaceo; // Pixels per space for the output

            // There is only 1 output
            if (numberofspacerso == 0)
                pixelsperspaceo = emptyspaceo;
            else
                pixelsperspaceo = emptyspaceo / numberofspacerso;

            DockPanel outputs = new DockPanel();
            foreach (string output in IO.Value)
            {
                TextBlock outputText = new TextBlock();
                DockPanel.SetDock(outputText, Dock.Top);
                outputText.Text = output + "  ";

                if (IO.Value.IndexOf(output) < numberofspacerso)
                    outputText.Margin = new Thickness(0, 0, 0, pixelsperspaceo);
                outputs.Children.Add(outputText);
                
            }

            DockPanel.SetDock(outputs, Dock.Right);

            //left dock
            SubCircuitDock.Children.Add(inputs);
            //right dock
            SubCircuitDock.Children.Add(outputs);
            //fill the rest
            SubCircuitDock.Children.Add(name);

            //place it on the screen
            InkCanvas.SetLeft(SubCircuitDock, GhostBounds.Left);
            InkCanvas.SetTop(SubCircuitDock, GhostBounds.Top);

            RotateTransform rotate = new RotateTransform(orientation * (180.0 / Math.PI));
            SubCircuitDock.LayoutTransform = rotate;
            sketchPanel.InkCanvas.Children.Add(SubCircuitDock);
        }

        /// <summary>
        /// Take away the IO stuff from the drawing
        /// </summary>
        private void removeIO()
        {
            if (sketchPanel.InkCanvas.Children.Contains(SubCircuitDock))
                sketchPanel.InkCanvas.Children.Remove(SubCircuitDock);
            SubCircuitDock = new DockPanel();
        }
        #endregion

        #region Event Subscription
        /// <summary>
        /// Add the handlers (doesn't do anything right now)
        /// </summary>
        public void SubscribeEvents()
        {
            if (subscribed)
                return;
            subscribed = true;
        }

        /// <summary>
        /// Add the handlers (doesn't do anything right now)
        /// </summary>
        public void UnSubscribeEvents()
        {
            if (!subscribed)
                return;
            subscribed = false;
        }
        #endregion

        #region Undraw

        /// <summary>
        /// Undraw the image from the screen
        /// </summary>
        public void unDraw()
        {
            removeIO();
            if (CurrentlyShowing)
            {
                sketchPanel.InkCanvas.Children.Remove(relabelImage);
                sketchPanel.InkCanvas.Children.Remove(drawingAdvice);
                drawingAdvice.IsOpen = false;
            }
        }

        public bool remove()
        {
            UnSubscribeEvents();
            unDraw();
            return true;
        }

        #endregion

        #region Handlers
        /// <summary>
        /// Handles when you pen down on the image that is being drawn, basically just collects the
        /// point for angle calculations later
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void relabelImage_StylusDown(Object sender, StylusEventArgs e)
        {
            activate(e.GetPosition(sketchPanel.InkCanvas));
        }

        private void relabelImage_StylusUp(Object sender, StylusEventArgs e)
        {
            finish(e.GetPosition(sketchPanel.InkCanvas));
        }

        /* HACK: In GhostGateFeedback, we invoke these functions whenever we 
         * detect a StylusDown/StylusMoved/StylusUp events, as we can't seem to subscribe
         * the actual GhostGate to these events.
         */
        /// <summary>
        /// Record the location of the pen-down.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool activate(Point point)
        {
            if (!canActivate(point)) return false;
            startPoint = point;
            return true;
        }

        /// <summary>
        /// Would a pen-down at this point activate rotation?
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool canActivate(Point point)
        {
            if (!sketchPanel.InkSketch.Sketch.containsShape(shape)) return false;
            else if (Point.Subtract(point, Center).Length > Diagonal.Length / 4) return false;
            return true;
        }

        /// <summary>
        /// Keep track of the last location we dragged to.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool update(Point point)
        {
            rotateGhostTo(newOrientation(shape.Orientation, startPoint, point));
            return true;
        }

        /// <summary>
        /// Actually enact the rotation.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool finish(Point point)
        {
            endPoint = point;
            gateRotated(shape, newOrientation(shape.Orientation, startPoint, endPoint));
            deactivate();
            return true;
        }

        /// <summary>
        /// Clear our saved information about the user's rotation attempt.
        /// </summary>
        public void deactivate()
        {
            startPoint = new Point();
            endPoint = new Point();
        }

        /// <summary>
        /// Re-render the ghost using the given orientation.
        /// </summary>
        /// <param name="newOrientation"></param>
        private void rotateGhostTo(double newOrientation)
        {
            // It makes no sense to lock the drawing ratio if this is a subcircuit
            bool shouldLock = this.gateDrawer.LockDrawingRatio;
            if (shape.Type == Domain.LogicDomain.SUBCIRCUIT)
                gateDrawer.LockDrawingRatio = false;

            GeometryDrawing ghostGate = gateDrawer.DrawGate(shape.Type, shape.Bounds, false, true, newOrientation);

            gateDrawer.LockDrawingRatio = shouldLock;

            DrawingImage drawingImage = new DrawingImage(ghostGate);
            relabelImage.Source = drawingImage;
            removeIO();
            addIO(newOrientation);
        }

        /// <summary>
        /// Calculate the new orientation from the old Orientation, the location where we started dragging, and the location
        /// where we finish dragging.
        /// </summary>
        private double newOrientation(double oldOrientation, Point start, Point end)
        {
            if (start == null || end == null)
                throw new Exception("when you re-orient, you should have already rotated");
            double degAngle = Vector.AngleBetween(Point.Subtract(start, Center), Point.Subtract(end, Center));
            degAngle = degAngle % 360 + 360; // Make sure degAngle is positive.

            double radAngle = degAngle * (Math.PI / 180);
            return (oldOrientation + radAngle) % (2 * Math.PI);
        }

        #endregion

        #region Getters and Setters
        public Point Center
        {
            get
            {
                Point middle = GhostBounds.TopLeft + 0.5 * Diagonal;
                return middle;
            }
        }

        public Vector Diagonal
        {
            get
            {
                return Point.Subtract(GhostBounds.BottomRight, GhostBounds.TopLeft); ;
            }
        }

        public Rect GhostBounds
        {
            get
            {
                return new System.Windows.Rect(shape.Bounds.TopLeft, 
                    new System.Windows.Vector(relabelImage.Width, relabelImage.Height));
            }
        }

        public Sketch.Shape Shape
        {
            get
            {
                return shape;
            }
        }

        /// <summary>
        /// Returns true if this ghost gate's image is currently on the ink canvas
        /// </summary>
        public bool CurrentlyShowing
        {
            get
            {
                return sketchPanel.InkCanvas.Children.Contains(relabelImage);
            }
        }

        /// <summary>
        ///  Set the popup drawing advice to be open
        /// </summary>
        public void ShowDrawingAdvice()
        {
            drawingAdvice.IsOpen = true;
        }

        #endregion
    }
}
