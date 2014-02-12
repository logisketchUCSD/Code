/*
 * Alexa Keizur and Alice Paul
 * Summer 2010
 * 
 * Drag-box Selection with Tapping
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Ink;
using SketchPanelLib;

namespace SelectionManager
{
    class DragBoxSelect : SelectorTemplate
    {
        #region Internals

        private static bool debug = false;
        private System.Windows.Rect currentRect;
        private System.Windows.Point start;
        private StrokeCollection selection;
        private System.Windows.Shapes.Rectangle selectBox;
        private const int DASHOFFSET = 1;
        private const int DASHTHICKNESS = 2;
        private const double SEARCH_RADIUS = 7.0;

        #endregion

        #region Constructor

        public DragBoxSelect(ref SketchPanelLib.SketchPanel sketchPanel)
            :base(ref sketchPanel)
        {
            selectionMade = false;
            makingSelection = false;
            this.selectBox = new System.Windows.Shapes.Rectangle();
        }

        #endregion

        #region Subscription

        /// <summary>
        /// Default will change the panel to editing mode
        /// Overrides will use events in the panel (mouse up, mouse down, etc.) to determine selection
        /// </summary>
        public override void SubscribeToPanel()
        {
            if (debug) System.Console.WriteLine("Subscribed");

            if (subscribed)
                return;

            // Set our selection
            selection = Selection;

            // Subscrbe to sketchPanel events
            sketchPanel.InkCanvas.StylusMove +=new System.Windows.Input.StylusEventHandler(InkCanvas_StylusMove);
            sketchPanel.InkCanvas.StylusLeave += new System.Windows.Input.StylusEventHandler(InkCanvas_StylusUp);
            sketchPanel.InkCanvas.StylusInAirMove += new System.Windows.Input.StylusEventHandler(InkCanvas_StylusInAirMove);

            sketchPanel.DisableDrawing();
            Selection = selection;

            // Add the selectBox to the InkCanvas and make hidden
            selectBox.Visibility = System.Windows.Visibility.Hidden;

            makingSelection = false;
            subscribed = true;
            selectionMade = false; // selection.Count > 0;
        }

        /// <summary>
        /// Clear selection and unsubscribe any events
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            if (debug) System.Console.WriteLine("Unsubscribed");

            if (sketchPanel == null || !subscribed)
                return;

            sketchPanel.EnableDrawing();
            currentRect = new System.Windows.Rect();
            selection = new StrokeCollection();
            Selection = selection;

            // Remove SketchPanel events
            sketchPanel.InkCanvas.StylusMove -= new System.Windows.Input.StylusEventHandler(InkCanvas_StylusMove);
            sketchPanel.InkCanvas.StylusLeave -= new System.Windows.Input.StylusEventHandler(InkCanvas_StylusUp);
            sketchPanel.InkCanvas.StylusInAirMove -= new System.Windows.Input.StylusEventHandler(InkCanvas_StylusInAirMove);

            // Remove the Rectangle from the InkCanvas and make hidden
            sketchPanel.InkCanvas.Children.Remove(selectBox);
            sketchPanel.UseCustomCursor = false;
            selectBox.Visibility = System.Windows.Visibility.Hidden;

            makingSelection = false;
            subscribed = false;
            selectionMade = false;
        }

        #endregion

        #region Events

        
        /// <summary>
        /// When we stylus down, we begin a new selection box
        /// </summary>
        /// <param name="sender"></param> Ignored :(
        /// <param name="e"></param> MouseEventArgs, contains mouse location
        protected override void InkCanvas_StylusDown(object sender, System.Windows.Input.StylusDownEventArgs e)
        {
            if (!e.Inverted)
                BeginSelection(e.GetPosition(sketchPanel.InkCanvas));
        }

        /// <summary>
        /// Begins a seletion at the specified point
        /// </summary>
        /// <param name="point"></param>
        public override void BeginSelection(System.Windows.Point point)
        {
            if (!subscribed)
                return;

            // We don't want to do anything if we clicked inside of our bounding box...
            System.Windows.Rect selectBounds = new System.Windows.Rect();
            selectBounds.Location = new System.Windows.Point(BoundingBox.X - 18, BoundingBox.Y - 18);
            selectBounds.Size = new System.Windows.Size(BoundingBox.Width + 36, BoundingBox.Height + 36);

            // Unless we are above a stroke
            Sketch.Substroke substrokeBelow = sketchPanel.InkSketch.Sketch.substrokeAtPoint(point.X, point.Y, SEARCH_RADIUS);

            if (substrokeBelow == null && selectBounds.Contains(point))
            {
                selection = new StrokeCollection();
                return;
            }
            else if (substrokeBelow != null)
                selection = Selection;
            else
                selection = new StrokeCollection();

            // Set the editing mode to none (removes selections)
            sketchPanel.DisableDrawing();

            // Begin new selection
            start = point;
            currentRect = new System.Windows.Rect(start.X, start.Y, 1.0, 1.0);

            selectionMade = false;
            makingSelection = true;
            DrawBox();
        }

        /// <summary>
        /// When we move the stylus, we change our selection box
        /// </summary>
        /// <param name="sender"></param> Ignored :(
        /// <param name="e"></param> MouseEventArgs, contains mouse location
        private void InkCanvas_StylusMove(object sender, System.Windows.Input.StylusEventArgs e)
        {
            if (!e.Inverted)
                ContinueSelection(e.GetPosition(sketchPanel.InkCanvas));
        }

        private void ContinueSelection(System.Windows.Point point)
        {
            //if (debug) System.Console.WriteLine("Box stylus move");
            if (!makingSelection || !subscribed)
                return;

            // Set box attributes
            System.Windows.Size rectSize = new System.Windows.Size(Math.Abs(start.X - point.X), Math.Abs(start.Y - point.Y));
            System.Windows.Point rectLocation = new System.Windows.Point(Math.Min(start.X, point.X), Math.Min(start.Y, point.Y));
            currentRect = new System.Windows.Rect(rectLocation, rectSize);
            selectionMade = false;

            // Update the box on the screen
            DrawBox();
        }

        /// <summary>
        /// When we remove the stylus, we make our selection
        /// </summary>
        /// <param name="sender">Ignored :(</param>
        /// <param name="e">MouseEventArgs, contains mouse location</param> 
        protected override void InkCanvas_StylusUp(object sender, System.Windows.Input.StylusEventArgs e)
        {
            if (!e.Inverted)
                EndSelection(e.GetPosition(sketchPanel.InkCanvas));
        }

        /// <summary>
        /// Finishes a selection at the specified point
        /// </summary>
        /// <param name="point"></param>
        public override void EndSelection(System.Windows.Point point)
        {
            if (!subscribed || !makingSelection)
                return;

            // Update the rectangle one more time
            ContinueSelection(point);

            // Select the strokes under the box
            StrokeCollection boxSelection = sketchPanel.InkCanvas.Strokes.HitTest(currentRect, 70);

            // Look for a stroke directly below us
            Sketch.Substroke substrokeBelow = sketchPanel.InkSketch.Sketch.substrokeAtPoint(point.X, point.Y, SEARCH_RADIUS);

            if (boxSelection.Count > 0)
                selection = boxSelection;
            else if (substrokeBelow != null)
            {
                Stroke singleStroke = sketchPanel.InkSketch.GetInkStrokeBySubstroke(substrokeBelow);
                if (selection.Contains(singleStroke))
                {
                    if (debug) System.Console.WriteLine("Stroke being removed");
                    selection.Remove(singleStroke);
                }
                else
                {
                    if (debug) System.Console.WriteLine("Stroke being added");
                    selection.Add(singleStroke);
                }
            }
            else if (substrokeBelow == null && boxSelection.Count == 0)
                selection.Clear();

            Selection = selection;
            selectBox.Visibility = System.Windows.Visibility.Hidden;
            sketchPanel.InkCanvas.Children.Remove(selectBox);

            if (makingSelection)
                selectionMade = selection.Count > 0;
            makingSelection = false;     
        }

        /// <summary>
        /// Updates the cursor inside the selection box to display whether stylus down will move the strokes
        /// or remove a stroke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StylusInAirMove(object sender, System.Windows.Input.StylusEventArgs e)
        {
            // If there are no strokes below us, use the automatic cursors.
            // Otherwise, use the pen cursor.
            System.Windows.Point point = e.GetPosition(sketchPanel.InkCanvas);
            Sketch.Substroke substrokeBelow = sketchPanel.InkSketch.Sketch.substrokeAtPoint(point.X, point.Y, SEARCH_RADIUS);
            
            if (substrokeBelow != null)
            {
                sketchPanel.UseCustomCursor = true;
                sketchPanel.Cursor = System.Windows.Input.Cursors.Pen;
            }
        }

        #endregion

        /// <summary>
        /// Removes the former selectBox from the canvas and makes and adds a new one 
        /// according to the placement and size of the currentRect.
        /// </summary>
        private void DrawBox()
        {
            sketchPanel.InkCanvas.Children.Remove(selectBox);

            // Drawing Attributes
            selectBox = new System.Windows.Shapes.Rectangle();
            selectBox.Height = currentRect.Height;
            selectBox.Width = currentRect.Width;
            System.Windows.Controls.InkCanvas.SetTop(selectBox, currentRect.Y);
            System.Windows.Controls.InkCanvas.SetLeft(selectBox, currentRect.X);
            selectBox.Stroke = Brushes.Red;
            selectBox.StrokeDashOffset = DASHOFFSET;
            selectBox.StrokeThickness = DASHTHICKNESS;
            selectBox.Fill = Brushes.Transparent;

            sketchPanel.InkCanvas.Children.Add(selectBox);
            selectBox.Visibility = System.Windows.Visibility.Visible;
        }
    }
}
