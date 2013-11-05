/*
 * Circuit Value Popups
 * by Alice Paul, Summer 2010
 * 
 * Class for the input and output toggles that appear during circuit simulation
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Data;

namespace SimulationManager
{
    public delegate void SetInputEventHandler(Sketch.Shape input, int value);

    class CircuitValuePopups
    {

        #region Internals

        /// <summary>
        /// Panel for getting the strokes of an input
        /// </summary>
        private SketchPanelLib.SketchPanel panel;

        /// <summary>
        /// Canvas for display
        /// </summary>
        private InkCanvas parent;

        /// <summary>
        /// Dictionary of strokes and circuit input names
        /// </summary>
        private Dictionary<Sketch.Shape, ListSet<string>> inputDict;

        /// <summary>
        /// Dictionary of strokes and circuit output names
        /// </summary>
        private Dictionary<Sketch.Shape, ListSet<string>> outputDict;

        /// <summary>
        /// Current list of input toggles on the screen
        /// </summary>
        private List<Popup> inputPopups;

        /// <summary>
        /// Current list of output displays on the screen
        /// </summary>
        private List<Popup> outputPopups;

        /// <summary>
        /// Dictionary of displays and locations
        /// </summary>
        private Dictionary<Sketch.Shape, System.Windows.Point> popupLocations;

        /// <summary>
        /// Event for Handling when a user toggles an input
        /// </summary>
        public event SetInputEventHandler SetInputEvent;

        /// <summary>
        /// The circuit value popup which is currently being moved
        /// </summary>
        private Popup Moving;

        /// <summary>
        /// Maps popups to the shapes being moved
        /// </summary>
        private Dictionary<Popup, Sketch.Shape> MovingShapes;

        /// <summary>
        /// Point for toggle moving
        /// </summary>
        private Point OrigPoint;

        private bool eventSubscribed;

        #endregion

        #region Constants

        // Input state strings and colors
        private const string ON = "1";
        private Brush ON_COLOR = Brushes.LightSkyBlue;
        private const string OFF = "0";
        private Brush OFF_COLOR = Brushes.MidnightBlue;
        private Brush BACKGROUND = Brushes.White;
        private const int POPUP_SIZE = 25;
        private const int POPUP_BUFFER = 3;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for InputToggles
        /// </summary>
        /// <param name="inputDict"></param>
        /// <param name="sketchPanel"></param>
        public CircuitValuePopups(Dictionary<Sketch.Shape, ListSet<string>> inputDict, SketchPanelLib.SketchPanel sketchPanel,
                            Dictionary<Sketch.Shape, ListSet<string>> outputDict)
        {
            this.eventSubscribed = false;
            this.panel = sketchPanel;
            foreach (Sketch.Shape input in inputDict.Keys)
                sketchPanel.Circuit.setInputValue(input, 0);
            this.parent = panel.InkCanvas;
            this.inputDict = inputDict;
            this.outputDict = outputDict;

            this.inputPopups = new List<Popup>();
            this.outputPopups = new List<Popup>();
            this.popupLocations = new Dictionary<Sketch.Shape, System.Windows.Point>();

            MovingShapes = new Dictionary<Popup, Sketch.Shape>();

            // Add Input Toggles Locations
            foreach (Sketch.Shape input in inputDict.Keys)
            {
                StrokeCollection strokes = GetStrokes(inputDict[input]);
                popupLocations.Add(input, new System.Windows.Point(strokes.GetBounds().Left, strokes.GetBounds().Top));
            }

            // Add Output Display Locations 
            foreach (Sketch.Shape output in outputDict.Keys)
            {
                StrokeCollection strokes = GetStrokes(outputDict[output]);
                popupLocations.Add(output, new System.Windows.Point(strokes.GetBounds().Right, strokes.GetBounds().Top));
            }

            InitializePopups();
        }

        /// <summary>
        /// Goes through the dictionary and creates toggles for each input StrokeCollection
        /// </summary>
        private void InitializePopups()
        {
            // Add Input Toggles
            foreach (Sketch.Shape input in inputDict.Keys)
                AddPopup(input, popupLocations[input], true);

            // Add Output Displays
            foreach (Sketch.Shape output in outputDict.Keys)
                AddPopup(output, popupLocations[output], false);
            if (!eventSubscribed)
            {
                parent.StylusUp += new StylusEventHandler(Canvas_StylusUp);
                eventSubscribed = true;
            }
        }

        #endregion

        #region Toggle Buttons

        /// <summary>
        /// Adds a circuit value popup at the given point.  Will not display if the point is off the window.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bounds"></param>
        /// <param name="input">0=Output 1=Input</param>
        private void AddPopup(Sketch.Shape shape, Point bounds, bool input)
        {
            // If the point is not in the window, don't display it!
            if (panel.RenderSize.Width < bounds.X || panel.RenderSize.Height < bounds.Y)
                return;

            Popup newToggle = new Popup();
            
            // Set Placement and Size
            newToggle.PlacementTarget = parent;
            newToggle.Placement = PlacementMode.RelativePoint;
            newToggle.VerticalOffset = bounds.Y;

            if (input)
                newToggle.HorizontalOffset = bounds.X - POPUP_BUFFER;
            else
                newToggle.HorizontalOffset = bounds.X + POPUP_SIZE + POPUP_BUFFER;

            newToggle.Width = POPUP_SIZE;
            newToggle.Height = POPUP_SIZE;

            // Set name and content
            TextBlock content = new TextBlock();
            content.Text = panel.Circuit.gateValue(shape, 0).ToString();
            content.FontSize = 14;
            content.TextAlignment = System.Windows.TextAlignment.Center;
            content.Width = POPUP_SIZE -3;
            content.Height = POPUP_SIZE -3;
            if (content.Text == OFF)
                content.Foreground = OFF_COLOR;
            else
                content.Foreground = ON_COLOR;
            content.Background = BACKGROUND;
            newToggle.Child = content;
            newToggle.Uid = shape.Id.ToString();
            MovingShapes[newToggle] = shape;

            if (!input)
            {
                newToggle.AllowsTransparency = true;

                // Make a circular background for the output text block
                GeometryDrawing background = new GeometryDrawing(Brushes.White,
                    new Pen(Brushes.Black, 2),
                    new EllipseGeometry(bounds,
                        content.ActualWidth / 2 + 8, content.ActualHeight / 2 + 8));

                content.Background = new DrawingBrush(background);
            }

            newToggle.IsOpen = true;

            // Add popup to proper list, tapping events
            if (input)
            {
                newToggle.StylusUp += new StylusEventHandler(toggle_StylusUp);
                inputPopups.Add(newToggle);
            }
            else
                outputPopups.Add(newToggle);

            newToggle.StylusDown += new StylusDownEventHandler(toggle_StylusDown);
        }

        /// <summary>
        /// Hides all toggles
        /// </summary>
        public void HideToggles()
        {
            foreach (Popup popup in AllPopups)
                HideToggle(popup);
        }

        /// <summary>
        /// Hides a single popup
        /// </summary>
        /// <param name="popup"></param>
        private void HideToggle(Popup popup)
        {
            popup.IsOpen = false;
            parent.Children.Remove(popup);
        }

        /// <summary>
        /// Shows all toggles
        /// </summary>
        public void ShowToggles()
        {
            foreach (Popup popup in AllPopups)
                ShowToggle(popup);
        }

        /// <summary>
        /// Displays a single popup
        /// </summary>
        /// <param name="popup"></param>
        private void ShowToggle(Popup popup)
        {
            popup.IsOpen = true;
            if (!parent.Children.Contains(popup))
                parent.Children.Add(popup);
        }

        /// <summary>
        /// Makes completely new toggles
        /// </summary>
        /// <param name="par"></param>
        public void MakeNewToggles(InkCanvas par)
        {
            ClearAllToggles();

            this.parent = par;

            InitializePopups();
        }

        /// <summary>
        /// Removes all toggles, clears lists
        /// </summary>
        public void ClearAllToggles()
        {
            if (parent == null)
                return;

            // Remove popups from InkCanvas and lists
            HideToggles();

            inputPopups.Clear();
            outputPopups.Clear();
        }

        #region Events

        /// <summary>
        /// Stylus Down Event - gets stylus position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggle_StylusDown(object sender, StylusEventArgs e)
        {
            OrigPoint = e.GetPosition(parent);
            Popup popupMoved = null;

            TextBlock textBoxClicked = (TextBlock)e.Source;
            foreach (Popup pop in AllPopups)
                if (pop.Child == textBoxClicked)
                {
                    popupMoved = pop;
                    break;
                }

            if (popupMoved == null)
                return;

            Moving = popupMoved;
        }

        /// <summary>
        /// Moves the selected popup to the location of the stylus up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_StylusUp(object sender, StylusEventArgs e)
        {
             if (Moving == null) return;

            Point curr = e.GetPosition(parent);
            double distance = Math.Sqrt(Math.Pow(curr.X - OrigPoint.X, 2) + Math.Pow(curr.Y - OrigPoint.Y, 2));

            // Update positions if we are moving a popup and it has been moved far enough
            if (distance > POPUP_SIZE / 2)
            {
                double Xcoord = Moving.HorizontalOffset + (curr.X - OrigPoint.X);
                double Ycoord = Moving.VerticalOffset + (curr.Y - OrigPoint.Y);

                popupLocations[MovingShapes[Moving]] = new Point(Xcoord, Ycoord);
                ResetPopupLoc(Moving);
            }
            Moving = null;
        }

        /// <summary>
        /// Event for when the stylus is lifted from a toggle. Updates input values.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggle_StylusUp(object sender, StylusEventArgs e)
        {
            TextBlock textBoxClicked = (TextBlock)e.Source;

            // Set the content of the popup
            if (textBoxClicked.Text == OFF)
            {
                textBoxClicked.Text = ON;
                textBoxClicked.Foreground = ON_COLOR;
            }
            else
            {
                textBoxClicked.Text = OFF;
                textBoxClicked.Foreground = OFF_COLOR;
            }

            // Find associated parent popup
            Popup toggleClicked = null;
            foreach (Popup popup in inputPopups)
                if (popup.Child == textBoxClicked)
                {
                    toggleClicked = popup;
                    break;
                }

            // Raise the event to alert SimulationManager
            if (SetInputEvent != null && toggleClicked != null)
            {
                Sketch.Shape shape = MovingShapes[toggleClicked];
                SetInputEvent(shape, Convert.ToInt32(textBoxClicked.Text));
            }

        }

        #endregion

        #endregion

        #region Helpers and Setters

        /// <summary>
        /// A dictionary with the names of inputs and outputs and the point at which their popup should appear
        /// </summary>
        public Dictionary<Sketch.Shape, System.Windows.Point> Locations
        {
            get 
            { 
                return popupLocations; 
            }
            set 
            { 
                popupLocations = value;
            }
        }

        /// <summary>
        /// Returns a list of all input and output popups
        /// </summary>
        private List<Popup> AllPopups
        {
            get
            {
                List<Popup> allPopups = inputPopups;
                foreach (Popup popup in outputPopups)
                    allPopups.Add(popup);

                return allPopups;
            }
        }

        /// <summary>
        /// Returns true if there are any existing popups
        /// </summary>
        public bool HasPopups
        {
            get
            {
                return (AllPopups.Count > 0);
            }
        }

        /// <summary>
        /// Returns a list of InkStrokes from a listset of stroke ids
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        private StrokeCollection GetStrokes(ListSet<string> set)
        {
            StrokeCollection strokes = new StrokeCollection();
            foreach (string strokeId in set)
                strokes.Add(panel.InkSketch.GetInkStrokeById(strokeId));

            return strokes;
        }

        /// <summary>
        /// Sets the content of an input or output popup
        /// </summary>
        /// <param name="name">The name of the desired popup</param>
        /// <param name="value">1 or 0</param>
        /// <param name="input">true if the desired popup is an input</param>
        public void SetPopup(Sketch.Shape shape, int value, bool input)
        {
            // Find popup
            Popup setpopup = null;

            List<Popup> listPopups = outputPopups;
            if (input)
                listPopups = inputPopups;

            foreach (Popup popup in listPopups)
                if (popup.Uid == shape.Id.ToString())
                {
                    setpopup = popup;
                    break;
                }

            if (setpopup == null)
                return;

            // Set Content and Color
            TextBlock newState = (TextBlock)setpopup.Child;
            if (value == 0)
            {
                newState.Text = OFF;
                newState.Foreground = OFF_COLOR;
            }
            else
            {
                newState.Text = ON;
                newState.Foreground = ON_COLOR;
            }
        }

        /// <summary>
        /// Sets the location of a single given popup to the location stored in popupLocations
        /// </summary>
        /// <param name="popup"></param>
        /// <param name="newPoint"></param>
        private void ResetPopupLoc(Popup popup)
        {
            Sketch.Shape shape = MovingShapes[popup];
            Point newLoc = popupLocations[shape];
            popup.VerticalOffset = newLoc.Y;
            popup.HorizontalOffset = newLoc.X;
        }

        #endregion
    }
}
