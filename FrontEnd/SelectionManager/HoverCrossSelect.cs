using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SketchPanelLib;
using Microsoft.Ink;
using System.Windows.Input;

namespace SelectionManager
{
    public class HoverCrossSelect : SelectorTemplate
    {
        #region Internals
        
        private System.Windows.Forms.Timer hoverTimer;                                       // Timer to determine when to allow selection
        private bool allowSelection;                                    // Whether the user can select items
        public int HOVER_INTERVAL = 1500;                                // Time interval before selection is allowed
        private List<Popup> selectHandles;                          // Current handles on all strokes

        #endregion

        #region Constants for Handles

        private static int HANDLE_WIDTH = 5;
        private static int MAXIMUM_HANDLE_HEIGHT = 80;
        private static int MINIMUM_HANDLE_HEIGHT = 20;
        private static Brush HANDLE_SELECT_COLOR = Brushes.Aqua;
        private static Brush HANDLE_DESELECT_COLOR = Brushes.Red;
        private static Color HANDLE_BORDER_COLOR = (Color)ColorConverter.ConvertFromString("Black");
        private static double DIAMETER = MAXIMUM_HANDLE_HEIGHT / 2;

        #endregion

        #region Constructor

        public HoverCrossSelect(ref SketchPanel sketchPanel)
        {
            this.sketchPanel = sketchPanel;
            this.allowSelection = false;
        }

        #endregion

        #region Subscription to Panel

        public override void SubscribeToPanel()
        {
            // Subscribe to mouse events to interact with the timer
            sketchPanel.InkCanvas.StylusInRange += new StylusEventHandler(inHoverPosition);
            sketchPanel.InkCanvas.StylusOutOfRange += new StylusEventHandler(outOfHoverPosition);
            sketchPanel.InkCanvas.MouseUp += new MouseButtonEventHandler(inHoverPosition);
            sketchPanel.InkCanvas.MouseDown += new MouseButtonEventHandler(mouseDown);
            sketchPanel.InkCanvas.SelectionChanged += new EventHandler(InkCanvas_SelectionChanged);
            sketchPanel.InkCanvas.SelectionMoved += new EventHandler(InkCanvas_SelectionChanged);
            sketchPanel.InkCanvas.SelectionResized += new EventHandler(InkCanvas_SelectionChanged);
            //sketchPanel.InkCanvas.LayoutUpdated += new EventHandler(InkCanvas_SelectionChanged);

            // Set up the timer and add an event for when selection is allowed
            hoverTimer = new System.Windows.Forms.Timer();
            hoverTimer.Interval = HOVER_INTERVAL;
            hoverTimer.Tick += new EventHandler(hoverTimer_Tick);

            this.selectHandles = new List<Popup>();

        }

        public override void UnsubscribeFromPanel()
        {
            if (sketchPanel == null)
                return;

            // Unsubscribe from events
            sketchPanel.InkCanvas.StylusInRange -= new StylusEventHandler(inHoverPosition);
            sketchPanel.InkCanvas.StylusOutOfRange -= new StylusEventHandler(outOfHoverPosition);
            sketchPanel.InkCanvas.MouseUp -= new MouseButtonEventHandler(inHoverPosition);
            sketchPanel.InkCanvas.MouseDown -= new MouseButtonEventHandler(mouseDown);
            sketchPanel.InkCanvas.SelectionChanged -= new EventHandler(InkCanvas_SelectionChanged);
            sketchPanel.InkCanvas.SelectionMoved -= new EventHandler(InkCanvas_SelectionChanged);
            sketchPanel.InkCanvas.SelectionResized -= new EventHandler(InkCanvas_SelectionChanged);
            //sketchPanel.InkCanvas.LayoutUpdated -= new EventHandler(InkCanvas_SelectionChanged);

            // Clear the selection and make sure that the system is set to ink mode
            sketchPanel.InkCanvas.GetSelectedStrokes().Clear();
            removeSelectionHandles();
            sketchPanel.EnableDrawing();
            this.hoverTimer.Stop();

        }

        #endregion

        #region Mouse Event Handlers (Hover Time)

        /// <summary>
        /// Recognizes when the mouse is hovering and starts timer if necessary
        /// </summary>
        private void inHoverPosition(object sender, StylusEventArgs e)
        {
            if (hoverTimer.Enabled | allowSelection)
                return;
            else
                hoverTimer.Start();

        }

        /// <summary>
        /// Recognizes when the mouse is hovering and starts timer if necessary(for MouseEvents)
        /// </summary>
        private void inHoverPosition(object sender, MouseEventArgs e)
        {
            if (hoverTimer.Enabled | allowSelection)
                return;
            else
                hoverTimer.Start();
        }

        /// <summary>
        /// Recognizes when the mouse leaves the screen or starts drawing, stops timer, and removes selection
        /// </summary>
        private void outOfHoverPosition(object sender, StylusEventArgs e)
        {
            hoverTimer.Stop();
            removeSelectionHandles();
            allowSelection = false;
            sketchPanel.InkCanvas.GetSelectedStrokes().Clear();
            //selectionTool.RemoveMenu();
        }

        /// <summary>
        /// Recognizes when the mouse leaves the screen or starts drawing, stops timer, and removes selection (for MouseEvents)
        /// </summary>
        private void outOfHoverPosition(object sender, MouseEventArgs e)
        {
            hoverTimer.Stop();
            removeSelectionHandles();
            allowSelection = false;
            sketchPanel.InkCanvas.GetSelectedStrokes().Clear();
            //selectionTool.RemoveMenu();
        }

        /// <summary>
        /// Keeps selections when the mouse is down
        /// </summary>
        private void mouseDown(object sender, MouseEventArgs e)
        {
            hoverTimer.Stop();
            removeSelectionHandles();
            allowSelection = false;
        }

        /// <summary>
        /// Recognizes when the hover interval is over and selection is allowed
        /// </summary>
        private void hoverTimer_Tick(object sender, EventArgs e)
        {
            if (allowSelection)
                return;
            // If we are not currently selecting, then initialize selection
            showSelectionHandles();
            allowSelection = true;

        }

        #endregion

        #region Mouse Event Handlers (Selection)

        /// <summary>
        /// Adds a stroke to the selection if the mouse enters it's selection handle
        /// </summary>
        private void addToSelection(object sender, MouseEventArgs e)
        {
            System.Console.WriteLine("Adding to Selection");
            // Find the stroke closest to the crossed point and add it to the selection
            System.Windows.Ink.StrokeCollection strokes = sketchPanel.InkCanvas.Strokes.HitTest(e.GetPosition(sketchPanel.InkCanvas), DIAMETER);
            strokes.Add(sketchPanel.InkCanvas.GetSelectedStrokes());
            Selection = strokes;
        }


        /// <summary>
        /// Removes a stroke to the selection if the mouse enters it's deselection handle
        /// </summary>
        private void removeFromSelection(object sender, MouseEventArgs e)
        {
            System.Console.WriteLine("Removing from selection");
            // Find the stroke closest to the crossed point and add it to the selection
            System.Windows.Ink.StrokeCollection nearestStroke = sketchPanel.InkCanvas.Strokes.HitTest(e.GetPosition(sketchPanel.InkCanvas), 0.5);
            sketchPanel.InkCanvas.GetSelectedStrokes().Remove(nearestStroke);
        }

        /// <summary>
        /// When the selection changes, the handles should too
        /// </summary>
        private void InkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            removeSelectionHandles();
            showSelectionHandles();
        }

        #endregion

        #region Selection Handles

        /// <summary>
        /// Shows the selection handles for unselected strokes and deselected handles for selected strokes
        /// </summary>
        private void showSelectionHandles()
        {
            foreach (System.Windows.Ink.Stroke s in sketchPanel.InkCanvas.Strokes)
            {
                // add appropriate handles to each stroke
                if (sketchPanel.InkCanvas.GetSelectedStrokes().Contains(s))

                    selectHandles.Add(makeDeselectionHandle(s));

                else
                    selectHandles.Add(makeSelectionHandle(s));
            }

        }

        /// <summary>
        /// Removes the selection handles from the screen
        /// </summary>
        private void removeSelectionHandles()
        {
            Popup current = new Popup();

            for (int i = 0; i < selectHandles.Count; i++)
            {
                current = selectHandles[i];
                sketchPanel.InkCanvas.Children.Remove(current);
            }
            selectHandles.Clear();

        }

        /// <summary>
        /// Displays a Selection Handle over the stroke
        /// </summary>
        private Popup makeSelectionHandle(System.Windows.Ink.Stroke s)
        {
            Popup handle = new Popup();

            System.Windows.Shapes.Rectangle handleShape = new System.Windows.Shapes.Rectangle();
            // Set the width and height of the rectangle
            int width = HANDLE_WIDTH;
            int height = (int) s.GetBounds().Height / 2;
            if (height < MINIMUM_HANDLE_HEIGHT)
                height = MINIMUM_HANDLE_HEIGHT;
            if (height > MAXIMUM_HANDLE_HEIGHT)
                height = MAXIMUM_HANDLE_HEIGHT;

            handleShape.Width = width;
            handleShape.Height = height;

            handleShape.Fill = HANDLE_SELECT_COLOR;
            handle.Child = handleShape;
            // Find location based on stroke bounds
            System.Windows.Point point = new System.Windows.Point(s.GetBounds().X + s.GetBounds().Width / 2, s.GetBounds().Y + s.GetBounds().Height/2-height);
            handle.PlacementRectangle = new System.Windows.Rect(point, new System.Windows.Size(width,height));
            handle.Placement = PlacementMode.Top;
            handle.Placement = PlacementMode.Left;

            // Create event for selection
            sketchPanel.InkCanvas.Children.Add(handle);
            handle.MouseEnter += new MouseEventHandler(addToSelection);

            // Set image and bring into view
            handle.Visibility = System.Windows.Visibility.Visible;
            handle.IsOpen = true;

            return handle;
        }

        /// <summary>
        /// Displays a Deselection Handle over the stroke
        /// </summary>
        private Popup makeDeselectionHandle(System.Windows.Ink.Stroke s)
        {
            Popup handle = new Popup();

            System.Windows.Shapes.Rectangle handleShape = new System.Windows.Shapes.Rectangle();
            // Set the width and height of the rectangle
            int width = HANDLE_WIDTH;
            int height = (int) s.GetBounds().Height / 2;
            if (height < MINIMUM_HANDLE_HEIGHT)
                height = MINIMUM_HANDLE_HEIGHT;
            if (height > MAXIMUM_HANDLE_HEIGHT)
                height = MAXIMUM_HANDLE_HEIGHT;

            handleShape.Width = width;
            handleShape.Height = height;
            
            handleShape.Fill = HANDLE_DESELECT_COLOR;
            handle.Child = handleShape;
            // Find location based on stroke bounds
            System.Windows.Point point = new System.Windows.Point(s.GetBounds().X + s.GetBounds().Width / 2, s.GetBounds().Y + s.GetBounds().Height/2);
            handle.PlacementRectangle = new System.Windows.Rect(point, new System.Windows.Size(width,height));
            handle.Placement = PlacementMode.Top;
            handle.Placement = PlacementMode.Left;

            // Create event for selection
            sketchPanel.InkCanvas.Children.Add(handle);
            handle.MouseEnter += new MouseEventHandler(addToSelection);

            // Set image and bring into view
            handle.Visibility = System.Windows.Visibility.Visible;
            handle.IsOpen = true;

            return handle;
        }

        #endregion
    }
}
