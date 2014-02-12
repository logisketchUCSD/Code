using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Ink;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Input;

namespace SelectionManager
{
    public delegate void RerecognizeGroup(Sketch.Shape substrokes);   // event for recognition manager
    public delegate void MessageBoxShowing();                         // event to alert the selmanager to remove widgets

    class RedrawingTool
    {

        #region Internals

        private System.Windows.Forms.Timer drawEndTimer;    // Timer to determine end of resketch

        private bool messageBoxOpen;                        // Makes sure we do not open more than one message box at a time

        private const int INTERVAL = 2500;                  // Amount of time for hover timer

        private const double DRAW_RADIUS = 250;             // Bounding radius for remaining within a collection of strokes

        private const double OVERLAP_RADIUS = 100;           // Allowed distance between the replacement stroke and original stroke bounding boxes

        private const double STYLUSPOINT_DIST = 5;          // Allowed distance between points to say they overlap

        private const double POINT_OVERLAP_PERCENT = 20;    // Percent overlapping points to determine a match of strokes

        private const double BOUNDS_RADIUS = 20;            // Bounding radius for replacing a stroke

        private StrokeCollection overlapStrokes;            // New strokes to replace old ones

        private StrokeCollection originalStrokes;           // Redrawn strokes

        private SketchPanelLib.SketchPanel sketchPanel;     // SketchPanel to hook into events

        private CommandManagement.CommandManager commandManager;    // CM for executing redraw event

        private bool subscribed = false;                    // Makes sure we do not over/under subscribe

        //private Refiner.Recognizer recognizer;                      // Recognizer for regrouping

        public event RerecognizeGroup GroupTogether;

        public event MessageBoxShowing messageBox;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for a RedrawingTool
        /// </summary>
        /// <param name="sketchPanel"></param>
        public RedrawingTool(ref SketchPanelLib.SketchPanel sketchPanel, 
            ref CommandManagement.CommandManager CM)
        {
            this.sketchPanel = sketchPanel;
            commandManager = CM;

            // Set up timer
            this.drawEndTimer = new System.Windows.Forms.Timer();
            this.drawEndTimer.Interval = INTERVAL;

            // Hook into stroke changes for highlighting
            this.originalStrokes = new StrokeCollection();
            this.overlapStrokes = new StrokeCollection();

            subscribed = false;
        }

        #endregion

        #region Subscription
        
        /// <summary>
        /// Subscribes the tool to the panel stroke events
        /// </summary>
        public void SubscribeToPanel()
        {
            if (subscribed) return;
            subscribed = true;

            // Hook into Events
            sketchPanel.InkCanvas.StrokeCollected += new InkCanvasStrokeCollectedEventHandler(InkCanvas_StrokeCollected);
            sketchPanel.InkCanvas.StrokeErased += new RoutedEventHandler(InkCanvas_StrokeErased);
            sketchPanel.InkCanvas.StrokesReplaced += new InkCanvasStrokesReplacedEventHandler(InkCanvas_StrokesReplaced);
            sketchPanel.InkCanvas.EditingModeChanged += new RoutedEventHandler(InkCanvas_EditingModeChanged);

            // Hook into stroke changes for highlighting
            originalStrokes.StrokesChanged += new StrokeCollectionChangedEventHandler(originalStrokes_StrokesChanged);

            // Subscribe Timer Tick Event
            drawEndTimer.Tick += new EventHandler(timer_Tick);
        }

        /// <summary>
        /// Unsubscribes the tool from the stroke events
        /// </summary>
        public void UnsubscribeFromPanel()
        {
            if (!subscribed) return;
            subscribed = false;

            // Release Events
            sketchPanel.InkCanvas.StrokeCollected -= new InkCanvasStrokeCollectedEventHandler(InkCanvas_StrokeCollected);
            sketchPanel.InkCanvas.StrokeErased -= new RoutedEventHandler(InkCanvas_StrokeErased);
            sketchPanel.InkCanvas.StrokesReplaced -= new InkCanvasStrokesReplacedEventHandler(InkCanvas_StrokesReplaced);
            sketchPanel.InkCanvas.EditingModeChanged -= new RoutedEventHandler(InkCanvas_EditingModeChanged);

            // Release stroke changes event for highlighting
            this.originalStrokes.StrokesChanged -= new StrokeCollectionChangedEventHandler(originalStrokes_StrokesChanged);

            // Unsubscribe Timer Tick Event
            drawEndTimer.Tick -= new EventHandler(timer_Tick);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a stroke is added to the InkCanvas
        /// Starts the redraw timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (outsideDrawRadius(e.Stroke))
                overlapStrokes.Clear();
            detectOverlap(e.Stroke);
            drawEndTimer.Start();
        }

        /// <summary>
        /// Occurs when a stroke is erased
        /// Stops the redraw timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StrokeErased(object sender, System.Windows.RoutedEventArgs e)
        {
            ClearStrokes();
        }

        /// <summary>
        /// Occurs when strokes are replaced
        /// Stop the timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StrokesReplaced(object sender, InkCanvasStrokesReplacedEventArgs e)
        {
            ClearStrokes();
        }

        /// <summary>
        /// Occurs when the editing mode is changed
        /// Stops the timer if necessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_EditingModeChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            ClearStrokes();   
        }

        /// <summary>
        /// Occurs when the timer ticks
        /// Evaluates stroke overlapping
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            maybeReplaceStrokes();
            ClearStrokes();
        }

        private void ClearStrokes()
        {
            drawEndTimer.Stop();
            overlapStrokes.Clear();
            originalStrokes.Clear();
        }
        #endregion

        #region Replacement

        /// <summary>
        /// Evaluates whether the strokes are retraces
        /// </summary>
        private void maybeReplaceStrokes()
        {
            if (originalStrokes.Count == 0 || overlapStrokes.Count == 0 || messageBoxOpen)
                return;

            messageBoxOpen = true;
            messageBox();
            MessageBoxResult result = MessageBox.Show("Replace strokes?", "", MessageBoxButton.YesNo);
            
            if (result == MessageBoxResult.Yes)
            {
                CommandList.RedrawCmd redrawCmd = new CommandList.RedrawCmd(sketchPanel, originalStrokes, overlapStrokes);
                redrawCmd.StrokesAdded += new CommandList.StrokesAddedEventHandler(Group);

                commandManager.ExecuteCommand(redrawCmd);
            }

            originalStrokes.Clear();
            overlapStrokes.Clear();

            messageBoxOpen = false;

        }

        /// <summary>
        /// Returns whether or not the stroke is within the drawing radius
        /// If it is not in the drawing radius, we start a new overlap collection
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        private bool outsideDrawRadius(System.Windows.Ink.Stroke stroke)
        {
            Point strokeCenter = new Point(stroke.GetBounds().X +stroke.GetBounds().Width/2,
                                                stroke.GetBounds().Y+stroke.GetBounds().Height/2);
            Point strokeCollCenter = new Point(overlapStrokes.GetBounds().X + overlapStrokes.GetBounds().Width / 2,
                                                    overlapStrokes.GetBounds().Y + overlapStrokes.GetBounds().Height / 2);
            double distance = Math.Sqrt(Math.Pow(strokeCenter.X - strokeCollCenter.X, 2) + Math.Pow(strokeCenter.Y - strokeCollCenter.Y, 2));
            return (distance > DRAW_RADIUS);
        }


        /// <summary>
        /// Determines whether or not the stroke overlaps any existing strokes
        /// If so, it adds the stroke to the overlap collection
        /// </summary>
        /// <param name="stroke"></param>
        private void detectOverlap(Stroke stroke)
        {
            Point strokeCenter = new Point(stroke.GetBounds().X +stroke.GetBounds().Width/2,
                                                stroke.GetBounds().Y+stroke.GetBounds().Height/2);
            StrokeCollection nearbyStrokes = sketchPanel.InkCanvas.Strokes.HitTest(strokeCenter, OVERLAP_RADIUS);

            // Check overlapping foreach nearby stroke and update stroke collections accordingly
            foreach (Stroke nearby in nearbyStrokes)
            {
                if (nearby == stroke || overlapStrokes.Contains(nearby))
                    break;
                if (pointsOverlap(stroke, nearby) || boundsOverlap(stroke, nearby))
                {
                    if (!originalStrokes.Contains(nearby))
                        originalStrokes.Add(nearby);
                    if (!overlapStrokes.Contains(stroke))
                        overlapStrokes.Add(stroke);
                }
            }
        }

        /// <summary>
        /// Determines with what percentage the points in the strokes overlay each other 
        /// Returns true if equal or above the overlap percentage
        /// </summary>
        /// <param name="replacement"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        private bool pointsOverlap(Stroke replacement, Stroke original)
        {
            int numOverlap = 0;

            // Go through all points and find matches based on the distance between points
            foreach (StylusPoint repPoint in replacement.StylusPoints)
                foreach (StylusPoint origPoint in original.StylusPoints)
                {
                    double distance = Math.Sqrt(Math.Pow(repPoint.X - origPoint.X, 2) + Math.Pow(repPoint.Y - origPoint.Y, 2));
                    if (distance <= STYLUSPOINT_DIST)
                    {
                        numOverlap++;
                        break;  // Only allow one match per point
                    }
                }

            // Return true if atleast one of the strokes meets the overlap requirements
            double repOverlap = (double)numOverlap / replacement.StylusPoints.Count;
            double origOverlap = (double)numOverlap / original.StylusPoints.Count;
            bool overlap = ((repOverlap >= POINT_OVERLAP_PERCENT) || (origOverlap >= POINT_OVERLAP_PERCENT));
            
            //System.Console.WriteLine("Point overlap: " + overlap);
            return overlap;
        }

        /// <summary>
        /// Determines the distance between two strokes' bounding boxes 
        /// Returns true if the centers are close enough to warrant a match
        /// </summary>
        /// <param name="replacement"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        private bool boundsOverlap(Stroke replacement, Stroke original)
        {
            Point repCenter = new Point(replacement.GetBounds().X + replacement.GetBounds().Width / 2,
                                                replacement.GetBounds().Y + replacement.GetBounds().Height / 2);
            Point origCenter = new Point(original.GetBounds().X + original.GetBounds().Width / 2,
                                                    original.GetBounds().Y + original.GetBounds().Height / 2);
            double distance = Math.Sqrt(Math.Pow(repCenter.X - origCenter.X, 2) + Math.Pow(repCenter.Y - origCenter.Y, 2));
            
            //System.Console.WriteLine("Bounds Overlap: "+(distance <= BOUNDS_RADIUS));
            return (distance <= BOUNDS_RADIUS);
        }


        #endregion

        #region Stroke Highlighting

        /// <summary>
        /// Updates the stroke highlighting based on our original stroke collection content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void originalStrokes_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            foreach (Stroke removedStroke in e.Removed)
                unhighlightStroke(removedStroke);

            foreach (Stroke addedStroke in originalStrokes)
                highlightStroke(addedStroke);
        }

        /// <summary>
        /// Returns the stroke to it's original drawing attributes
        /// </summary>
        /// <param name="stroke"></param>
        private void unhighlightStroke(Stroke stroke)
        {
            if (sketchPanel.InkCanvas.Strokes.Contains(stroke))
            {
                Sketch.Substroke substroke = sketchPanel.InkSketch.GetSketchSubstrokeByInk(stroke);
                stroke.DrawingAttributes.Color = substroke.Type.Color;
            }
        }

        /// <summary>
        /// Highlights the stroke
        /// </summary>
        /// <param name="stroke"></param>
        private void highlightStroke(Stroke stroke)
        {
            stroke.DrawingAttributes.Color = System.Windows.Media.Colors.Silver;
        }

        #endregion

        #region Grouping
        
        /// <summary>
        /// Strokes to Group
        /// </summary>
        /// <param name="strokes"></param>
        private void Group(StrokeCollection strokes, string classification)
        {
            List<Sketch.Substroke> listSubstrokes = new List<Sketch.Substroke>();
            Domain.ShapeType temporaryType = new Domain.ShapeType();

            foreach (Stroke s in strokes)
            {
                Sketch.Substroke sub = sketchPanel.InkSketch.GetSketchSubstrokeByInk(s);
                sub.Classification = classification;
                listSubstrokes.Add(sub);
                s.DrawingAttributes.Color = (new Domain.ShapeType()).Color;
            }

            Sketch.Shape labelShape = sketchPanel.Sketch.AddLabel(listSubstrokes, temporaryType);

            labelShape.AlreadyGrouped = true;
            labelShape.AlreadyLabeled = false;

            GroupTogether(labelShape);

        }

        #endregion

    }
}
