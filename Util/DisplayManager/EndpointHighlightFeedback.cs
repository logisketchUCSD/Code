using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Collections;
using System.Drawing.Drawing2D;
using System.Windows.Controls;

using System.Windows.Ink;
using System.Windows.Input;
using Sketch;
using SketchPanelLib;
using CircuitParser;
using System.Windows;


namespace DisplayManager
{
    /// <summary>
    /// Event that handles when the connections between shapes have been changed through the endpoint feedback
    /// </summary>
    public delegate void EndpointsChangedHandler();

    /// <summary>
    /// Type of the delegate called when an endpoint is moved. See "endPointMoved" below.
    /// </summary>
    /// <param name="oldLocation">the endpoint that the user is moving</param>
    /// <param name="newLocation">the point it was dragged to</param>
    /// <param name="attachedStroke">the stroke that oldLocation belonged to</param>
    /// <returns>true if the move succeeded, or false otherwise</returns>
    public delegate bool EndPointMovedHandler(Sketch.EndPoint oldLocation, System.Windows.Point newLocation, System.Windows.Ink.Stroke attachedStroke, Sketch.Shape shapeAtNewLoc);

    #region Feedback Mechanism

    /// <summary>
    /// Highlights endpoints using dots and other symbols.
    /// Requires a CircuitParser instance for
    /// Circuit Structure data.
    /// </summary>
    public class EndPointHighlightFeedback : FeedbackMechanism
    {
        
        #region Internals

        /// <summary>
        /// Prints debug statements iff true
        /// </summary>
        private bool debug = false;

        /// <summary>
        /// Determines whether or not we are displaying internal endpoints
        /// </summary>
        private bool displayInternalEndpoints = false;

        /// <summary>
        /// True iff endpoint highlighting is enabled
        /// </summary>
        protected bool endPointHighlightingEnabled = true;

        /// <summary>
        /// Map of Ink Stroke Ids to EndPoint Painter Ids.
        /// </summary>
        protected Dictionary<System.Windows.Ink.Stroke, List<int>> iStrokeId2EndPtPainterId;

        /// <summary>
        /// Map of EndPoint Painter Ids to EndPoint Painters.  
        /// All EndPoint Painters (Dictionary Values) are 
        /// Paint()ed on every InkPicture.Paint() event.
        /// </summary>
        protected Dictionary<int, EndPointPainter> endPointPainterMap;

        /// <summary>
        /// The endpoint that had been recently clicked
        /// </summary>
        protected int? clickedIndex;

        /// <summary>
        /// How far away a click can be from the center of an endpoint to be counted as a click
        /// </summary>
        protected const int CLICK_RADIUS = 7;

        /// <summary>
        /// Event triggers when an endpoint is moved
        /// </summary>
        private EndPointMovedHandler EndPointMoved;

        #endregion

        #region Constructors, Intialization, and Subscription

        /// <summary>
        /// Constructors
        /// </summary>
        public EndPointHighlightFeedback(ref SketchPanel parentPanel, EndPointMovedHandler endPointMovedFunction)
            : base(ref parentPanel) 
        { 
            init(); 
            Clear();
            EndPointMoved = endPointMovedFunction;
        }

        /// <summary>
        /// Instantiates bookkeeping data structures.
        /// </summary>
        private void init()
        {
            iStrokeId2EndPtPainterId = new Dictionary<System.Windows.Ink.Stroke, List<int>>();
            endPointPainterMap = new Dictionary<int, EndPointPainter>();
            clickedIndex = null;
        }

        /// <summary>
        /// <see cref="FeedbackMechanism.SubscribeToPanel"/>
        /// </summary>
        public override void SubscribeToPanel(ref SketchPanel parentPanel)
        {
            base.SubscribeToPanel(ref parentPanel);
            
            UnpauseEvents();
        }

        /// <summary>
        /// <see cref="FeedbackMechanism.UnsubscribeFromPanel"/>
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            PauseEvents();
            Clear();
        }

        /// <summary>
        /// Unsubscribes events without removing endpoints
        /// </summary>
        public void PauseEvents()
        {
            if (!subscribed)
                return;
            subscribed = false;
            clickedIndex = null;

            if (debug)
                Console.WriteLine("Unsubbing events");

            sketchPanel.SketchFileLoaded -= new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
            sketchPanel.InkCanvas.StylusDown -= new StylusDownEventHandler(InkCanvas_StylusDown);
            sketchPanel.InkCanvas.StylusMove -= new StylusEventHandler(InkCanvas_StylusMove);
            sketchPanel.InkCanvas.StylusUp -= new StylusEventHandler(InkCanvas_StylusUp);
        }

        /// <summary>
        /// Subscribes endpoint highlighting events
        /// </summary>
        public void UnpauseEvents()
        {
            if (subscribed)
                return;
            subscribed = true;
            clickedIndex = null;

            if (debug)
                Console.WriteLine("Subbing events");

            //sketchPanel.ResultReceived += new RecognitionResultReceivedHandler(sketchPanel_ResultReceived);
            sketchPanel.SketchFileLoaded += new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
            sketchPanel.InkCanvas.StylusDown += new StylusDownEventHandler(InkCanvas_StylusDown);
            sketchPanel.InkCanvas.StylusMove += new StylusEventHandler(InkCanvas_StylusMove);
            sketchPanel.InkCanvas.StylusUp += new StylusEventHandler(InkCanvas_StylusUp);
        }

        /// <summary>
        /// Clears/initializes the bookkeeping data structures
        /// </summary>
        public void Clear()
        {
            iStrokeId2EndPtPainterId.Clear();

            // Remove all rectangles from the picture
            foreach (EndPointPainter painter in endPointPainterMap.Values)
                painter.Clear();

            endPointPainterMap.Clear();

            UnpauseEvents();
            UpdateEndpoints();
        }

        #endregion

        #region Ink and Panel Events

        /// <summary>
        /// Creates new EndpointPainters whenever
        /// a circuit is created
        /// </summary>
        public void UpdateEndpoints()
        {
            if (!subscribed) return;
            // Reinit

            // Highlight endpoints
            foreach (Shape shape in sketchPanel.InkSketch.Sketch.Shapes)
                if (LogicDomain.IsWire(shape.Type))
                    highlightEndpoints(shape);
        }

        /// <summary>
        /// Clears this feedback mechanism whenever a file is loaded
        /// </summary>
        void sketchPanel_SketchFileLoaded()
        {
            Clear();
        }

        /// <summary>
        /// Allows the user to connect shapes by dragging a connection
        /// Stylus down gets the endpoint that was clicked, stores it in clickedPoint.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void InkCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            if (debug) 
                Console.WriteLine("Endpoint stylus down received.");

            System.Windows.Point clickPoint = e.GetPosition(sketchPanel.InkCanvas);
            
            // Get the endpoint that was clicked
            foreach (EndPointPainter endPoint in endPointPainterMap.Values)
            {
                double distance = Math.Sqrt(Math.Pow(endPoint.InkPoint.X - clickPoint.X, 2) + 
                    Math.Pow(endPoint.InkPoint.Y - clickPoint.Y, 2));

                if (distance <= CLICK_RADIUS)
                {
                    clickedIndex = endPoint.Id;

                    EndPointPainter clickedPoint;
                    endPointPainterMap.TryGetValue((int)clickedIndex, out clickedPoint);

                    if (clickedPoint == null || clickedPoint.EPoint.IsConnected)
                    {
                        clickedIndex = null;
                        return;
                    }

                    sketchPanel.DisableDrawing();
                    break;
                }
            }
        }

        /// <summary>
        /// Allows the user to connect shapes by dragging a connection
        /// Stylus move moves the enpoint painter to wherever the sylus currently is.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void InkCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            if (debug) 
                Console.WriteLine("Endpoint stylus move recieved.");

            // If nothing has actually been clicked, nothing to do.
            if (clickedIndex == null) return;

            EndPointPainter clickedPoint;
            endPointPainterMap.TryGetValue((int)clickedIndex, out clickedPoint);

            clickedPoint.TempMoveTo(e.GetPosition(sketchPanel.InkCanvas));
            sketchPanel.DisableDrawing();
        }

        /// <summary>
        /// Allows the user to connect shapes by dragging a connection
        /// Stylus up connects the wire to the nearest target 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void InkCanvas_StylusUp(object sender, StylusEventArgs e)
        {
            if (debug) 
                Console.WriteLine("Endpoint stylus up recieved.");

            // If no point has been clicked, return
            if (clickedIndex == null) return;

            EndPointPainter clickedPoint;
            endPointPainterMap.TryGetValue((int)clickedIndex, out clickedPoint);

            // Check if there are strokes close-by
            Sketch.EndPoint oldLocation = clickedPoint.EPoint;
            System.Windows.Point oldLocationPoint = new System.Windows.Point(oldLocation.X, oldLocation.Y);
            System.Windows.Point newLocation = e.GetPosition(sketchPanel.InkCanvas);
            System.Windows.Ink.Stroke attachedStroke = sketchPanel.InkSketch.GetInkStrokeBySubstroke(clickedPoint.EPoint.ParentSub);

            // Find the shape that the endpoint was dragged to 
            // You can't connect a wire to itself!
            Shape closest = sketchPanel.Sketch.shapeAtPoint(newLocation.X, newLocation.Y, 10, clickedPoint.EPoint.ParentShape);

            if (closest != null && attachedStroke != null )
            {
                clickedPoint.Clear();
                clickedPoint.PointShape = null;
                endPointPainterMap.Remove((int)clickedIndex);
                
                // Pass the endpoint moved event to the display manager
                EndPointMoved(oldLocation, newLocation, attachedStroke, closest);
            }
            else
            {
                clickedPoint.ResetMove();
            }
            clickedPoint.PaintMe(displayInternalEndpoints);

            // Reset the clickedIndex and the editing mode
            sketchPanel.EnableDrawing();
            clickedIndex = null;
        }


        /// <summary>
        /// Paints all EndPoints.
        /// </summary>
        void PaintAllEndpoints()
        {
            if (debug)
                Console.WriteLine("InkCanvas Painted Received");
            
            if (!EndPointHighlightingEnabled)
                return;

            foreach (EndPointPainter epp in endPointPainterMap.Values)
                epp.PaintMe(displayInternalEndpoints);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Enables or disables EndPoint highlighting
        /// </summary>
        public bool EndPointHighlightingEnabled
        {
            get
            {
                return endPointHighlightingEnabled;
            }

            set
            {
                endPointHighlightingEnabled = value;
                sketchPanel.InvalidateVisual();
            }
        }

        /// <summary>
        /// Enables or disables the painting of internal endpoints
        /// </summary>
        public bool ShowInternalEndpoints
        {
            get
            {
                return displayInternalEndpoints;
            }
            set
            {
                bool repaint = value != displayInternalEndpoints;
                displayInternalEndpoints = value;
                if (repaint)
                {
                    Clear();
                }
            }
        }

        #endregion

        #region Endpoint Highlighting Helper Functions

        /// <summary>
        /// Highlights the endpoints of a single Wire by
        /// creating EndPointPainters for all endpoints.
        /// 
        /// Main function for endpoint highlighting.
        /// 
        /// Wire must have "AlreadyLabeled" set to true.
        /// </summary>
        /// <param name="wire">The Wire to highlight</param>
        /// <param name="g">The graphics handle to use while creating painters</param>
        private void highlightEndpoints(Shape wire)
        {
            // If this hasn't been recognized or isn't a wire, don not highlight.
            if (!wire.AlreadyLabeled || !LogicDomain.IsWire(wire.Type))
                return;

            List<int> newEppIds = new List<int>();
            EndPointPainter epp;
            int eppId;

            foreach (Sketch.EndPoint endpoint in wire.Endpoints)
            {
                epp = createEndpointPainter(endpoint);
                eppId = endPointPainterMap.Keys.Count + 1;
                epp.Id = eppId;
                newEppIds.Add(eppId);
                endPointPainterMap.Add(eppId, epp);
            }

            // Update stroke map to point to endpoint painters
            foreach (Sketch.Substroke substroke in wire.Substrokes)
            {
                String iStrokeId = sketchPanel.InkSketch.GetInkStrokeIdBySubstrokeId(substroke.XmlAttrs.Id);
                System.Windows.Ink.Stroke iStroke = sketchPanel.InkSketch.GetInkStrokeById(iStrokeId);

                if (iStrokeId2EndPtPainterId.ContainsKey(iStroke))
                    throw new ApplicationException("Error: encountered one stroke " +
                        "that is part of two or more unique meshes");

                if (iStrokeId != null)
                    iStrokeId2EndPtPainterId[iStroke] = newEppIds;
            }

            PaintAllEndpoints();
        }
              
        /// <summary>
        /// Creates a single EndPointPainter.
        /// 
        /// Helper function.
        /// </summary>
        /// <param name="w">The endpoint to paint</param>
        /// <returns>A new EndPointPainter</returns>
        private EndPointPainter createEndpointPainter(Sketch.EndPoint endpoint)
        {
            // Create painter
            EndPointPainter epp = new EndPointPainter(endpoint, sketchPanel.InkCanvas);

            // Set painter type
            if (!endpoint.IsConnected)
                epp.Type = EndPointPainter.EndPointType.Unconnected;
            else if (Domain.LogicDomain.IsGate(endpoint.ConnectedShape.Type))
                epp.Type = EndPointPainter.EndPointType.ConnectedToSymbol;
            else if (Domain.LogicDomain.IsText(endpoint.ConnectedShape.Type))
                epp.Type = EndPointPainter.EndPointType.ConnectedToLabel;
            else if (Domain.LogicDomain.IsWire(endpoint.ConnectedShape.Type))
                epp.Type = EndPointPainter.EndPointType.ConnectedToWire;
            else 
                epp.Type = EndPointPainter.EndPointType.UnknownConnection;
                    
            return epp;
        }

        #endregion
    }

    #endregion

    #region EndPoint Painter Class

    /// <summary>
    /// EndPointPainter mimicks the Algorithm design pattern.  An EndPointPainter 
    /// encapsulates the Paint()ing code used to highlight an endpoint on screen.
    /// </summary>
    public class EndPointPainter
    {
        #region Internals

        /// <summary>
        /// Prints debug statements iff true
        /// </summary>
        private bool debug = false;

        /// <summary>
        /// The ID for this endPoint painter
        /// </summary>
        public int? Id;

        /// <summary>
        /// The wrapped endpoint
        /// </summary>
        private EndPoint endPoint;

        /// <summary>
        /// The centerpoint of the endpoint in ink space
        /// </summary>
        public System.Windows.Point InkPoint;

        /// <summary>
        /// The shape that represents the endpoint
        /// </summary>
        public System.Windows.Shapes.Shape PointShape;

        /// <summary>
        /// The diameter from which to search for a shape to connect to
        /// </summary>
        private const double CONNECT_DIAMETER = 10;

        /// <summary>
        /// Bool which indicates wheather or not the endpoint's shape should be changed.
        /// </summary>
        private bool updateShape;

        /// <summary>
        /// InkCanvas on which the endpoints are drawn
        /// </summary>
        private InkCanvas inkCanvas;

        /// <summary>
        /// The type of the EndPoint
        /// </summary>
        public enum EndPointType
        {
            UnknownConnection,
            ConnectedToWire,
            ConnectedToSymbol, 
            ConnectedToLabel, 
            Unconnected
        };

        public EndPointType Type;

        /// <summary>
        /// Flags set by CircuitParser to denote endpoint types
        /// </summary>
        public static string ConnectedToWireFlag = "wire";
        public static string ConnectedToSymbolFlag = "symbol";
        public static string ConnectedToLabelFlag = "input/output";
        public static string UnknownConnection = "unknown";

        /// <summary>
        /// Dimensions of endpoint highlighting
        /// </summary>
        public float WIDTH = 9.0F;
        public float HEIGHT = 9.0F;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public EndPointPainter(EndPoint p, InkCanvas inkCanvas)
        {
            InkPoint = new System.Windows.Point();
            InkPoint.X = p.X;
            InkPoint.Y = p.Y;

            this.inkCanvas = inkCanvas;

            endPoint = p;
            updateShape = true;
        }

        public void Reset(EndPoint p)
        {
            endPoint = p;
            InkPoint.X = endPoint.X;
            InkPoint.Y = endPoint.Y;

            updateShape = true;
        }

        /// <summary>
        /// Removes the associated shape from the ink canvas
        /// </summary>
        public void Clear()
        {
            if (inkCanvas.Children.Contains(PointShape))
                inkCanvas.Children.Remove(PointShape);
        }

        #region Endpoint Shapes

        /// <summary>
        /// Creates the green "valid" circle
        /// </summary>
        /// <returns></returns>
        private System.Windows.Shapes.Shape Valid()
        {
            System.Windows.Shapes.Ellipse Circle = new System.Windows.Shapes.Ellipse();
            Circle.Height = HEIGHT;
            Circle.Width = WIDTH;
            Circle.Fill = Brushes.Green;
            Circle.Stroke = Brushes.Black;
            Circle.StrokeThickness = 1;
            return Circle;
        }

        /// <summary>
        /// Creates the transparent "internal" circle
        /// </summary>
        /// <returns></returns>
        private System.Windows.Shapes.Shape Internal()
        {
            System.Windows.Shapes.Ellipse Circle = new System.Windows.Shapes.Ellipse();
            Circle.Height = HEIGHT;
            Circle.Width = WIDTH;
            Circle.Fill = Brushes.Transparent;
            Circle.Stroke = Brushes.Black;
            Circle.StrokeThickness = 1;
            return Circle;
        }

        /// <summary>
        /// Creates yellow "unkown" square
        /// </summary>
        /// <returns></returns>
        private System.Windows.Shapes.Shape Unknown()
        {
            System.Windows.Shapes.Rectangle Square = new System.Windows.Shapes.Rectangle();
            Square.Height = HEIGHT;
            Square.Width = WIDTH;
            Square.Fill = Brushes.Yellow;
            Square.Stroke = Brushes.Black;
            Square.StrokeThickness = 1;
            return Square;
        }

        /// <summary>
        /// Creates red "invalid" x
        /// </summary>
        /// <returns></returns>
        private System.Windows.Shapes.Shape Invalid()
        {
            System.Windows.Shapes.Polygon Cross = new System.Windows.Shapes.Polygon();
            Cross.Stroke = Brushes.Red;
            Cross.Fill = Brushes.Red;
            Cross.StrokeThickness = 1;
            #region Cross Points
            // This is more complicated than it needs to be.  If someone could get polyline 
            // drawing (System.Windows.Shapes.Polyline) to work, that would be excellent.

            // Create a cross
            System.Windows.Point P1 = new System.Windows.Point(0, HEIGHT / 3.0);
            System.Windows.Point P2 = new System.Windows.Point(WIDTH / 3.0, HEIGHT / 3.0);
            System.Windows.Point P3 = new System.Windows.Point(WIDTH / 3.0, 0);
            System.Windows.Point P4 = new System.Windows.Point(2 * WIDTH / 3.0, 0);
            System.Windows.Point P5 = new System.Windows.Point(2 * WIDTH / 3.0, HEIGHT / 3.0);
            System.Windows.Point P6 = new System.Windows.Point(WIDTH, HEIGHT / 3.0);
            System.Windows.Point P7 = new System.Windows.Point(WIDTH, 2 * HEIGHT / 3.0);
            System.Windows.Point P8 = new System.Windows.Point(2 * WIDTH / 3.0, 2 * HEIGHT / 3.0);
            System.Windows.Point P9 = new System.Windows.Point(2 * WIDTH / 3.0, HEIGHT);
            System.Windows.Point P10 = new System.Windows.Point(WIDTH / 3.0, HEIGHT);
            System.Windows.Point P11 = new System.Windows.Point(WIDTH / 3.0, 2 * HEIGHT / 3.0);
            System.Windows.Point P12 = new System.Windows.Point(0, 2 * HEIGHT / 3.0);

            PointCollection CrossPoints = new PointCollection();
            CrossPoints.Add(P1);
            CrossPoints.Add(P2);
            CrossPoints.Add(P3);
            CrossPoints.Add(P4);
            CrossPoints.Add(P5);
            CrossPoints.Add(P6);
            CrossPoints.Add(P7);
            CrossPoints.Add(P8);
            CrossPoints.Add(P9);
            CrossPoints.Add(P10);
            CrossPoints.Add(P11);
            CrossPoints.Add(P12);

            // Ok, now rotate it 
            for (int i = 0; i < 12; i++)
            {
                double X = WIDTH / 2.0 + ((CrossPoints[i].X - WIDTH / 2.0) * Math.Cos(Math.PI / 4.0) - (CrossPoints[i].Y - HEIGHT / 2.0) * Math.Sin(Math.PI / 4.0));
                double Y = WIDTH / 2.0 + ((CrossPoints[i].X - WIDTH / 2.0) * Math.Sin(Math.PI / 4.0) + (CrossPoints[i].Y - HEIGHT / 2.0) * Math.Cos(Math.PI / 4.0));
                CrossPoints[i] = new System.Windows.Point(X, Y);
            }
            #endregion
            Cross.Points = CrossPoints;
            return Cross;
        }

        #endregion

        /// <summary>
        /// Paints the endpoint to screen
        /// Precondition: Graphics.PageUnit = GraphicsUnit.Pixel
        /// Precondition: Graphics.SmoothingMode = SmoothingMode.AntiAlias
        /// </summary>
        public void PaintMe(bool paintInternalEndpoint)
        {
            if (Double.IsInfinity(InkPoint.Y)) InkPoint.Y = 100;
            if (Double.IsInfinity(InkPoint.X)) InkPoint.X = 100;

            Clear();

            if (updateShape)
            {
                // Get Endpoint Shape
                if (endPoint.InternalEndPoint)
                {
                    if (debug)
                        Console.WriteLine("Internal Endpoint");

                    if (paintInternalEndpoint)
                        PointShape = Internal();
                    else PointShape = null;
                }
                else if (Type == EndPointType.ConnectedToSymbol || Type == EndPointType.ConnectedToWire || Type == EndPointType.ConnectedToLabel)
                {
                    if (debug)
                        Console.WriteLine("Connected");

                    PointShape = Valid();
                }
                else if (Type == EndPointType.Unconnected)
                {
                    if (debug)
                        Console.WriteLine("Unconnected");

                    PointShape = Invalid();
                }
                else
                {
                    if (debug)
                        Console.WriteLine("Connection unknown");

                    PointShape = Unknown();
                }

                updateShape = false;
            }

            // If we're not drawing this endpoint, don't try to draw it!
            if (PointShape == null)
                return;

            InkCanvas.SetTop(PointShape, InkPoint.Y - HEIGHT / 2);
            InkCanvas.SetLeft(PointShape, InkPoint.X - WIDTH / 2);

            // Add rect to Picture
            if (!inkCanvas.Children.Contains(PointShape))
                inkCanvas.Children.Add(PointShape);
            PointShape.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Move the endpoint to the specified new location, without changing the internal representation 
        /// </summary>
        /// <param name="newLoc">A System.Windows.Point, the new location of the endpoint.</param>
        public void TempMoveTo(System.Windows.Point newLoc)
        {
            InkCanvas.SetLeft(PointShape, newLoc.X);
            InkCanvas.SetTop(PointShape, newLoc.Y);
        }

        /// <summary>
        /// Changes the on-screen location to match the internal representation.
        /// </summary>
        public void ResetMove()
        {
            InkCanvas.SetLeft(PointShape, InkPoint.X);
            InkCanvas.SetTop(PointShape, InkPoint.Y);
        }

        public Shape getParentWire()
        {
            return endPoint.ParentSub.ParentShape;
        }
        /// <summary>
        /// Updates endpoint's connection type based on location of end point painter
        /// </summary>
        /// <returns>True if the endpoint was properly reconnected</returns>
        public System.Windows.Ink.Stroke closestInkStroke(System.Windows.Point searchPoint, SketchPanel sketchPanel)
        {
            // Get the parent shape (a wire)
            Shape wire = endPoint.ParentSub.ParentShape;
            
            // Find the shape that we're closest to
            StrokeCollection closeStrokes = sketchPanel.InkCanvas.Strokes.HitTest(searchPoint, CONNECT_DIAMETER);
            
            if (closeStrokes.Count == 0)
            {
                if (debug) Console.WriteLine("Endpoint moved: No Strokes in range");
                return null;
            }

            // If we're trying to connect the wire to itself, it shouldn't work.
            Shape closestShape = sketchPanel.InkSketch.GetSketchSubstrokeByInk(closeStrokes[0]).ParentShape;
            //if (endPoint.ParentSub.ParentShapes[0] == closestShape)
                //return null;

            return closeStrokes[0];
        }

        /// <summary>
        /// Gets the endpoint wrapped by the painter
        /// </summary>
        public EndPoint EPoint
        {
            get 
            {
                return endPoint;
            }
        }
    }

    #endregion
}
