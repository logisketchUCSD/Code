using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using SketchPanelLib;
using Sketch;
using System.Windows.Ink;

namespace DisplayManager
{
    /// <summary>
    /// UI Tool for displaying stroke labels
    /// </summary>
    public class DisplayLabelTool
    {
        #region Internals
        
        /// <summary>
        /// Panel that holds the display - the InkCanvas
        /// </summary>
        protected SketchPanel sketchPanel;

        /// <summary>
        /// Bool for showing label info
        /// </summary>
        internal bool debug = false;

        /// <summary>
        /// Current list of our tooltips
        /// </summary>
        private Dictionary<Shape,Popup> toolTips;

        /// <summary>
        /// Radius to measure whether a tool tip should be displayed
        /// </summary>
        private const int TOOLTIP_RADIUS = 30;

        /// <summary>
        /// Timer to keep track of how long we have been in the hover space
        /// </summary>
        private System.Windows.Forms.Timer hoverTimer;
        
        /// <summary>
        /// Indicates that we have been in the hover space long enough to bring up tooltips
        /// </summary>
        private bool hoverTimeUp;

        /// <summary>
        /// Timer interval - time in hover space before tooltips appear
        /// Helps to limit distraction to a user who is drawing or editing
        /// </summary>
        private const int INTERVAL = 1000;

        /// <summary>
        /// Makes sure we do not get over or under subscribed.
        /// </summary>
        private bool subscribed = false;

        /// <summary>
        /// For making sure that move doesn't remove all tooltips after a recognize
        /// </summary>
        private bool needsMoveBack = false;

        /// <summary>
        /// Set to true to turn on standard "tooltip" label behavior
        /// </summary>
        private bool StylusMoveOn = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="originalSketch"></param>
        public DisplayLabelTool(ref SketchPanel SP)
            : base()
        {
            // Set the sketch panel info and tooltips
            this.toolTips = new Dictionary<Shape,Popup>();
            this.sketchPanel = SP;

            // Timer
            this.hoverTimer = new System.Windows.Forms.Timer();
            this.hoverTimer.Interval = INTERVAL;
        }


        #endregion

        #region Initializers

        /// <summary>
        /// Goes through every shape in the sketch and creates a text box for it
        /// </summary>
        private void MakeTextBlocks()
        {
            //System.Console.WriteLine("Making Text Boxes");
            toolTips.Clear();
           
            foreach (Sketch.Shape shape in sketchPanel.Sketch.Shapes)
            {
                if (shape.Substrokes.Length == 0) return;

                // Set Content and Color
                Popup newTextBlock = new Popup();

                ContextDomain.ContextDomain contextDomain = ContextDomain.CircuitDomain.GetInstance();

                // Make text box.  Feel free to change FontWeight, FontSize, etc.
                TextBlock newChild = new TextBlock();

                if (debug)
                {
                    newChild.Text = shape.Name + " (" + shape.Type.Name + ")";
                    newChild.Text += "\nOrientation: " + shape.Orientation;
                    newChild.Text += "\nTemplate: " + shape.TemplateName;
                    //newChild.Text += "\nConnected shapes: ";
                    //foreach (Sketch.Shape connectedShape in shape.ConnectedShapes)
                        //newChild.Text += connectedShape.Name + ", ";
                    newChild.Text += "\nProperly Connected: " + (contextDomain.IsProperlyConnected(shape) ? "yes" : "no");
                }
                else
                    newChild.Text = shape.Type.Name;

                newChild.Background = Brushes.Transparent;
                newChild.FontWeight = System.Windows.FontWeights.Bold;

                newTextBlock.Child = newChild;
                newTextBlock.IsOpen = false;

                newTextBlock.AllowsTransparency = true;

                newTextBlock.Visibility = System.Windows.Visibility.Visible;
                newTextBlock.PlacementTarget = sketchPanel.InkCanvas;
                newTextBlock.Placement = PlacementMode.RelativePoint;

                // Find strokes
                StrokeCollection strokes = new StrokeCollection();
                foreach (Sketch.Substroke sub in shape.Substrokes)
                    strokes.Add(sketchPanel.InkSketch.GetInkStrokeBySubstroke(sub));

                // Set Position and Add to Canvas and our collection
                newTextBlock.VerticalOffset = strokes.GetBounds().Top+strokes.GetBounds().Height/2;
                newTextBlock.HorizontalOffset = strokes.GetBounds().Left+strokes.GetBounds().Width / 2;
                subscribed = true;
                //sketchPanel.InkCanvas.Children.Add(newTextBlock);
                toolTips.Add(shape,newTextBlock);
            }
        }

        /// <summary>
        /// Subscribes to SketchPanel.  Subscibe only when tool is selected.
        /// <see cref="SketchPanelLib.SketchPanelListener.SubscribeToPanel()"/>
        /// </summary>
        public void SubscribeToPanel(SketchPanel parentPanel)
        {
            if (subscribed) return;
            subscribed = true;

            // Update tooltips
            MakeTextBlocks();

            // Hook into SketchPanel stylus and stroke events (not displayed regularly right now)
            sketchPanel.InkCanvas.StylusDown += new StylusDownEventHandler(InkCanvas_StylusDown);
            sketchPanel.InkCanvas.StylusOutOfRange += new StylusEventHandler(InkCanvas_StylusOutOfRange);
            sketchPanel.InkCanvas.Strokes.StrokesChanged += new StrokeCollectionChangedEventHandler(InkCanvas_StrokesChanged);
            
            hoverTimer.Tick += new EventHandler(hoverTimer_Tick);
         }

        /// <summary>
        /// Unsubscribes from SketchPanel
        /// <see cref="SketchPanelLib.SketchPanelListener.UnSubscribeToPanel()"/>
        /// </summary>
        public void UnsubscribeFromPanel()
        {
            if (sketchPanel == null || !subscribed)
                return;
            subscribed = false;

            // Unsubscribe from stylus and stroke events (not displayed regularly right now)
            sketchPanel.InkCanvas.StylusDown -= new StylusDownEventHandler(InkCanvas_StylusDown);
            sketchPanel.InkCanvas.StylusOutOfRange -= new StylusEventHandler(InkCanvas_StylusOutOfRange);
            sketchPanel.InkCanvas.Strokes.StrokesChanged -= new StrokeCollectionChangedEventHandler(InkCanvas_StrokesChanged);

            hoverTimer.Tick -= new EventHandler(hoverTimer_Tick);

            //foreach (TextBlock box in toolTips.Values)
                //sketchPanel.InkCanvas.Children.Remove(box);
            ClosePopUps();
            toolTips.Clear();
        }
        /// <summary>
        /// Bring down the popups when the window is minimized
        ///
        /// </summary>
        public void ClosePopUps()
        {
            foreach (Popup pop in toolTips.Values)
            {
                pop.IsOpen = false;
                pop.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        #endregion

        #region Stylus Events

        /// <summary>
        /// Stops the timer and clears the tool tips when the stylus is on the canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            if (needsMoveBack)
            {
                needsMoveBack = false;
                if (StylusMoveOn) sketchPanel.InkCanvas.StylusInAirMove += new StylusEventHandler(InkCanvas_StylusMove);
            }
            hoverTimer.Stop();
            hoverTimeUp = false;
            HideAllTooltips();
        }

        /// <summary>
        /// Removes the label if a stroke is erased
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StrokeErased(object sender, StylusEventHandler e)
        {
            HideAllTooltips();
        }

        /// <summary>
        /// Updates position of mouse and calls display tooltips
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            if (hoverTimeUp)
                DisplayToolTip(e.GetPosition(sketchPanel.InkCanvas));
            else if (!hoverTimer.Enabled)
                hoverTimer.Start();
        }

        /// <summary>
        /// Hides all tooltips and stops the timer when the stylus leaves
        /// the InkCanvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StylusOutOfRange(object sender, StylusEventArgs e)
        {
            //HideAllTooltips();
            hoverTimeUp = false;
            hoverTimer.Stop();
        }

        /// <summary>
        /// Refreshes the tooltips to reflect changes to strokes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            /*foreach (Popup box in toolTips.Values)
            {
                //sketchPanel.InkCanvas.Children.Remove(box);
                box.Visibility = System.Windows.Visibility.Hidden;
                box.IsOpen = false;
            }
            toolTips.Clear();
            MakeTextBlocks();*/
        }

        /// <summary>
        /// Indicates that the stylus has been in the hover space long enough to bring
        /// up tool tips
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hoverTimer_Tick(object sender, EventArgs e)
        {
            hoverTimeUp = true;
        }

        #endregion

        #region Display and Hide Labels

        /// <summary>
        /// Turn on or off old-style (hovering) tool-tips
        /// </summary>
        public bool StylusTooltips
        {
            set
            {
                // When these tool-tips are on and we want them off, tell the handler we want them off
                if (StylusMoveOn && !value) 
                    sketchPanel.InkCanvas.StylusInAirMove -= new StylusEventHandler(InkCanvas_StylusMove);
                // When these tool-tips are off and we want them on, tell the handler we want them on
                else if (!StylusMoveOn && value) 
                    sketchPanel.InkCanvas.StylusInAirMove += new StylusEventHandler(InkCanvas_StylusMove);
                StylusMoveOn = value;
            }
        }

        /// <summary>
        /// Sets all tool tip visibilities to hidden
        /// </summary>
        private void HideAllTooltips()
        {
            foreach (Popup box in toolTips.Values)
            {
                box.Visibility = System.Windows.Visibility.Hidden;
                box.IsOpen = false;
            }

        }

        /// <summary>
        /// Displays a ToolTip for a Substroke based on where the mouse is located.
        /// </summary>
        /// <param name="coordinates">Mouse coordinates</param>
        private void DisplayToolTip(System.Windows.Point coordinates)
        {
            if (toolTips.Count != sketchPanel.Sketch.Shapes.Length)
                MakeTextBlocks();

            // Find the shapes that contain this point and update visibilities
            foreach (Sketch.Shape shape in sketchPanel.Sketch.Shapes)
            {
                // See if we can get the corresponding textblock
                Popup textBlock;
                toolTips.TryGetValue(shape, out textBlock);

                if (textBlock == null) // If we get here, remake the text blocks and try again.
                {
                    MakeTextBlocks();
                    DisplayToolTip(coordinates);
                    return;
                    // Note: It is not the end of the world if we get here (the program 
                    // will be fine), but we really shouldn't.
                    //throw new Exception("ERR: Shape " + shape.Type + " has no text block. Contained in sketch: "
                    //    + sketchPanel.InkSketch.Sketch.ShapesL.Contains(shape));
                }

                else
                {
                    StrokeCollection shapeStrokes = new StrokeCollection();
                    foreach (Substroke sub in shape.Substrokes)
                        shapeStrokes.Add(sketchPanel.InkSketch.GetInkStrokeBySubstroke(sub));

                    if (shapeStrokes.GetBounds().Contains(coordinates))
                    {
                        if (debug)
                        {
                            textBlock.HorizontalOffset = sketchPanel.Width / 2; ;
                            textBlock.VerticalOffset = 10;
                        }
                        else
                        {
                            textBlock.HorizontalOffset = coordinates.X;
                            textBlock.VerticalOffset = coordinates.Y-25;
                        }
                        textBlock.Visibility = System.Windows.Visibility.Visible;
                        double width = ((TextBlock)textBlock.Child).Width;
                        textBlock.IsOpen = true;
                    }
                    else
                    {
                        textBlock.Visibility = System.Windows.Visibility.Hidden;
                        textBlock.IsOpen = false;
                    }
                }
            }
        }

        /// <summary>
        /// Displays all tooltips on recognize as an option for feedback.
        /// </summary>
        /// <param name="coordinates">Mouse coordinates</param>
        public void DisplayAllToolTips()
        {
            if (toolTips.Count == 0)
                MakeTextBlocks();
            //remove this temporarily so that it doesn't remove all tooltips on move
            needsMoveBack = true;
            if (StylusMoveOn) sketchPanel.InkCanvas.StylusInAirMove -= new StylusEventHandler(InkCanvas_StylusMove);
            foreach (Sketch.Shape shape in sketchPanel.Sketch.Shapes)
            {
                // See if we can get the corresponding textblock
                Popup textBlock;
                toolTips.TryGetValue(shape, out textBlock);
                if (textBlock == null)
                {
                    // Note: It is not the end of the world if we get here (the program 
                    // will be fine), but we really shouldn't.
                    //throw new Exception("ERR: Shape " + shape.Type + " has no text block. Contained in sketch: "
                        //+ sketchPanel.InkSketch.Sketch.ShapesL.Contains(shape));
                }

                else
                {
                    textBlock.Visibility = System.Windows.Visibility.Visible;
                    textBlock.IsOpen = true;
                }
            }
        }
        #endregion

        #region Getters

        public bool Subscribed
        {
            get
            {
                return subscribed;
            }
        }

        #endregion
    }
}
