using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Shapes;

using System.Windows.Ink;
using System.Windows.Media.Imaging;


using Sketch;
using System.Windows.Controls;
using System.Windows.Media;

namespace SketchPanelLib
{
    #region Major Event Handlers

    /// <summary>
    /// Delegate for handling file load events sent from a SketchPanel.
    /// </summary>
    public delegate void SketchFileLoadedHandler();

    /// <summary>
    /// Delegate for passing on when ink is added, removed, moved, etc.
    /// </summary>
    public delegate void InkChangedEventHandler();

    /// <summary>
    /// Delegate for passing on when on the fly recognition should have something drawn
    /// DrawGate in mainwindow
    /// </summary>
    public delegate void InkChangedOnFlyRecHandler();

    /// <summary>
    /// Delegate for indicating when the panel thinks it has been recognized
    /// </summary>
    /// <param name="recognized"></param>
    public delegate void RecognizeChangedEventHandler(bool recognized);

    #endregion

    /// <summary>
    /// A Panel for sketching recognizeable diagrams.  
    /// </summary>
    public class SketchPanel : System.Windows.Controls.Canvas
    {
        #region Internals

        /// <summary>
        /// Stores the main sketch for this panel
        /// </summary>
        protected InkToSketchWPF.InkCanvasSketch inkSketch;

        /// <summary>
        /// The Ink display for this sketch
        /// </summary>
        protected InkCanvas inkCanvas;

        /// <summary>
        /// The border of the panel
        /// </summary>
        protected Border myBorder;

        /// <summary>
        /// True if sketch has been recognized, false if not
        /// </summary>
        private bool recognized = false;

        /// <summary>
        /// Sketch file loaded event.  This panel publishes to this
        /// event whenever the user loads a sketch file. 
        /// Refreshes feedbacks.
        /// </summary>
        public event SketchFileLoadedHandler SketchFileLoaded;

        /// <summary>
        /// Panel publishes to this event whenever ink is added or removed
        /// </summary>
        public event InkChangedEventHandler InkChanged;

        /// <summary>
        /// Keeps track of commands for undo/redo
        /// </summary>
        protected CommandManagement.CommandManager CM;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Constructor.  Creates a sketchPanel from scratch.
        /// </summary>
        public SketchPanel(CommandManagement.CommandManager commandManager)
            : this(commandManager, new InkToSketchWPF.InkCanvasSketch(new InkCanvas()))
        {
        }

        /// <summary>
        /// Constructor.  Creates a sketchPanel using the given commandManager and inkCanvasSketch.
        /// </summary>
        public SketchPanel(CommandManagement.CommandManager commandManager, InkToSketchWPF.InkCanvasSketch inkCanvasSketch)
            : base()
        {
            CM = commandManager;
            this.InitPanel(inkCanvasSketch);
        }

        /// <summary>
        /// Initializes the panel using the given InkCanvasSketch.
        /// </summary>
        public virtual void InitPanel(InkToSketchWPF.InkCanvasSketch inkCanvasSketch)
        {
            // Add the inkCanvas to the panel
            this.Children.Clear();
            inkSketch = inkCanvasSketch;
            inkCanvas = inkCanvasSketch.InkCanvas;
            setDefaultInkPicProps();
            this.Children.Add(inkSketch.InkCanvas);

            // Hook into ink events
            subscribeToEvents();
            Recognized = false;
        }

        /// <summary>
        /// (Re)Sets default InkCanvas properties (e.g. background color, stroke color, etc)
        /// </summary>
        private void setDefaultInkPicProps()
        {
            inkCanvas.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            inkCanvas.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            inkCanvas.Background = SketchPanelConstants.DefaultBackColor;
            inkCanvas.Width = this.Width;
            inkCanvas.Height = this.Height;
            inkCanvas.DefaultDrawingAttributes.Color = SketchPanelConstants.DefaultInkColor;
            inkCanvas.DefaultDrawingAttributes.FitToCurve = true;

            EnableDrawing();
        }

        /// <summary>
        /// (Re)Hooks into InkCanvas events
        /// </summary>
        private void subscribeToEvents()
        {
            inkCanvas.StrokeCollected += new InkCanvasStrokeCollectedEventHandler(inkCanvas_StrokeCollected);
            inkCanvas.StrokeErasing += new InkCanvasStrokeErasingEventHandler(inkCanvas_StrokeErasing);
            inkCanvas.SelectionMoved += new EventHandler(inkPic_SelectionMovedResized);
            inkCanvas.SelectionMoving += new InkCanvasSelectionEditingEventHandler(inkCanvas_SelectionMovingResizing);
            inkCanvas.SelectionResized += new EventHandler(inkPic_SelectionMovedResized);
            inkCanvas.SelectionResizing += new InkCanvasSelectionEditingEventHandler(inkCanvas_SelectionMovingResizing);
            inkCanvas.SelectionChanged += new EventHandler(inkPic_SelectionChanged);
            SizeChanged += new System.Windows.SizeChangedEventHandler(sketchPanel_SizeChanged);
        }
        #endregion

        #region Ink event handling

        private void sketchPanel_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            inkCanvas.Height = ActualHeight;
            inkCanvas.Width = ActualWidth;
            InvalidateVisual();
        }

        /// <summary>
        /// Removes strokes in an undo-able way. 
        /// Note: We are subscribing to the before-erase hook so we can get the event's stroke.
        /// </summary>
        protected void inkCanvas_StrokeErasing(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            CM.ExecuteCommand(new CommandList.StrokeRemoveCmd(ref inkSketch, e.Stroke));
            InkChanged();
        }

        /// <summary>
        /// Adds strokes in an undo-able way. 
        /// </summary>
        protected void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            // Remove it from the InkCanvas to let the command do the work
            InkCanvas.Strokes.Remove(e.Stroke);

            // Don't add the stroke if we think it's a dot
            if (e.Stroke.GetBounds().Width < 2 * e.Stroke.DrawingAttributes.Width &&
                e.Stroke.GetBounds().Height < 2 * e.Stroke.DrawingAttributes.Width)
            {
                InkChanged();
                return;
            }

            CM.ExecuteCommand(new CommandList.StrokeAddCmd(ref inkSketch, e.Stroke));
            InkChanged();
        }


        // Declare a temporary variable for use in before-event hooks
        CommandManagement.Command c;

        /// <summary>
        /// Create the move/resize command.
        /// Note: We are subscribing to the before-erase hook so we can get the stroke's bounds.
        /// </summary>
        protected void inkCanvas_SelectionMovingResizing(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            // Create the command with arguments from before-the-event hook
            c = new CommandList.MoveResizeCmd(ref inkSketch, InkCanvas.GetSelectedStrokes(),
                e.OldRectangle, e.NewRectangle);
        }

        /// <summary>
        /// Moves/resizes the selected strokes by executing the command. Selects the strokes afterwards.
        /// </summary>
        protected void inkPic_SelectionMovedResized(object sender, EventArgs e)
        {
            CM.ExecuteCommand(c);
            InkChanged();
            ((SketchPanelLib.CommandList.MoveResizeCmd)c).selectStoredStrokes();
        }

        protected void inkPic_SelectionChanged(object sender, EventArgs e)
        {
            // Do we need to display any menu?
        }

        #endregion
        
        #region Standard sketch procedures and transformations (open, save, cut/copy/paste, zoom, and change background)
        
        /// <summary>
        /// Loads a sketch file into the panel.
        /// 
        /// Precondition: given file path is valid, readable AND NOT NULL.
        /// </summary>
        /// <param name="filepath">Reads from this file path.</param>
        public void LoadSketch(string filepath, Delegate updateSim)
        {
            if (filepath == null)
                throw new Exception("Why'd you give me an empty filepath, jerk?");

            inkSketch.LoadSketch(filepath); 

            updateSim.DynamicInvoke();

            // See whether the sketch is recognized or not
            recognized = true;
            foreach (Substroke sub in Sketch.Substrokes)
            {
                if (sub.Type == new Domain.ShapeType())
                {
                    recognized = false;
                    break;
                }
            }

            if (SketchFileLoaded != null)
                SketchFileLoaded();
        }

        /// <summary>
        /// Saves the current sketch to the specified file path in XML format.
        /// Wrapper for InkSketch.SavePanel().  
        /// 
        /// Precondition: given file path is valid and writable.  
        /// </summary>
        /// <param name="filepath">Writes to this file path.</param>
        public void SaveSketch(string filepath)
        {
            inkSketch.SaveSketch(filepath);
        }

        /// <summary>
        /// Enlarges the sketch by a preset factor.
        /// </summary>
        public void ZoomIn()
        {
            StrokeCollection allStrokes = new StrokeCollection(InkCanvas.Strokes);
            if (allStrokes.Count == 0)
                return;
            System.Windows.Rect oldBounds = allStrokes.GetBounds();
            System.Windows.Rect biggerBounds = new System.Windows.Rect(oldBounds.TopLeft,
                new System.Windows.Size(oldBounds.Width * SketchPanelConstants.ZoomInFactor, 
                    oldBounds.Height * SketchPanelConstants.ZoomInFactor));
            Zoom(allStrokes, biggerBounds);
            
        }

        /// <summary>
        /// Shrinks the sketch by a preset factor.
        /// </summary>
        public void ZoomOut()
        {
            StrokeCollection allStrokes = new StrokeCollection(InkCanvas.Strokes);
            if (allStrokes.Count == 0)
                return;
            System.Windows.Rect oldBounds = allStrokes.GetBounds();
            System.Windows.Rect smallerBounds = new System.Windows.Rect(oldBounds.TopLeft,
                new System.Windows.Size(oldBounds.Width * SketchPanelConstants.ZoomOutFactor,
                    oldBounds.Height * SketchPanelConstants.ZoomOutFactor));
            Zoom(allStrokes, smallerBounds);
        }

        /// <summary>
        /// Zooms the sketch by a factor just enough to fit all the strokes in (preserves proportions).
        /// </summary>
        public void ZoomToFit()
        {
            StrokeCollection allStrokes = new StrokeCollection(InkCanvas.Strokes);
            if (allStrokes.Count == 0)
                return;
            System.Windows.Rect oldBounds = allStrokes.GetBounds();
            double padding = SketchPanelConstants.Padding;
            double scaleFactor = (1 - 2 * padding) * Math.Min(Width / oldBounds.Width, Height / oldBounds.Height);
            System.Windows.Rect fitBounds = new System.Windows.Rect(padding * Width, padding * Height, 
                scaleFactor * oldBounds.Width, scaleFactor * oldBounds.Height);

            Zoom(allStrokes, fitBounds);
        }

        /// <summary>
        /// Resizes the given strokes to the specified bounds.
        /// </summary>
        private void Zoom(StrokeCollection strokes, System.Windows.Rect newBounds)
        {
            CM.ExecuteCommand(new CommandList.MoveResizeCmd(ref inkSketch, strokes,
                strokes.GetBounds(), newBounds));

            InkChanged();
        }

        /// <summary>
        /// Copies strokes to the clipboard in InkSerializedFormat.  Copies selected strokes;
        /// if there is no selection, then all Ink is copied.  
        /// </summary>
        public void CopyStrokes()
        {
            CM.ExecuteCommand(new CommandList.CopyCmd(InkSketch));
        }


        /// <summary>
        /// Cuts strokes to the clipboard in InkSerializedFormat.  Cuts selected strokes;
        /// if there is no selection, then all Ink is cut.  
        /// </summary>
        public void CutStrokes()
        {
            CM.ExecuteCommand(new CommandList.CutCmd(InkSketch));
            EnableDrawing();

            InkChanged();
        }

        /// <summary>
        /// Undoes the last stroke/action, if possible
        /// Recurses until a valid redo occurs or the redo stack is empty.
        /// </summary>
        public void Undo()
        {
            while (CM.UndoValid)
                if (CM.Undo())
                    break;

            InkChanged();
        }

        /// <summary>
        /// Redoes the last stroke/action, if possible
        /// Recurses until a valid redo occurs or the redo stack is empty.
        /// </summary>
        public void Redo()
        {
            while (CM.RedoValid)
                if (CM.Redo())
                    break;

            InkChanged();
        }

        /// <summary>
        /// Erases all strokes from the sketch
        /// </summary>
        public void DeleteAllStrokes()
        {
            CM.ExecuteCommand(new CommandList.StrokeRemoveCmd(ref inkSketch, new StrokeCollection(inkSketch.InkCanvas.Strokes)));
            EnableDrawing();

            // When the panel is empty, we cannot say that it has been recognized.
            Recognized = false;

            InkChanged();
        }

        /// <summary>
        /// Erases all strokes from the sketch without side effects
        /// </summary>
        public void SimpleDeleteAllStrokes()
        {
            // Don't use the CM to avoid side-effects
            CommandList.StrokeRemoveCmd delete = new CommandList.StrokeRemoveCmd(ref inkSketch, new StrokeCollection(inkSketch.InkCanvas.Strokes));
            delete.Execute();

            // When the panel is empty, we cannot say that it has been recognized.
            Recognized = false;

            InkChanged();
        }

        /// <summary>
        /// Selects all strokes in the sketch
        /// </summary>
        public void SelectAllStrokes()
        {
            inkCanvas.Select(inkCanvas.Strokes);
        }

        /// <summary>
        /// Erases all selected strokes from the sketch
        /// </summary>
        public void DeleteStrokes()
        {
            CM.ExecuteCommand(new CommandList.StrokeRemoveCmd(ref inkSketch, inkSketch.InkCanvas.GetSelectedStrokes()));

            // If the panel is now empty, we cannot say that it is recognized
            if (inkCanvas.Strokes.Count == 0)
                Recognized = false;

            InkChanged();
        }

        /// <summary>
        /// Pastes strokes from the clipboard
        /// </summary>
        public void PasteStrokes(System.Windows.Point point)
        {
            CM.ExecuteCommand(new CommandList.PasteCmd(InkCanvas, InkSketch, point));
            InkChanged();
        }

        /// <summary>
        /// Makes the pen able to draw and erase on the ink canvas
        /// </summary>
        public void EnableDrawing()
        {
            EditingMode = InkCanvasEditingMode.InkAndGesture;
            EditingModeInverted = InkCanvasEditingMode.EraseByStroke;
        }


        /// <summary>
        /// Sets both the editing mode and inverted editing mode to none.
        /// </summary>
        public void DisableDrawing()
        {
            EditingMode = InkCanvasEditingMode.None;
            EditingModeInverted = InkCanvasEditingMode.None;
        }

        #endregion

        #region Recognizer Interface

        /// <summary>
        /// Loads a project from the given filepath. (for subcircuit embedding)
        /// </summary>
        public ConverterXML.ReadXML LoadXml(string filepath)
        {
            return inkSketch.LoadXml(filepath);
        }

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets the sketch in this panel.
        /// </summary>
        public Sketch.Sketch Sketch
		{
			get
			{
				return inkSketch.Sketch;
			}
		}

        /// <summary>
        /// Gets this panel's InkSketch.
        /// </summary>
        public InkToSketchWPF.InkCanvasSketch InkSketch
        {
            get
            {
                return this.inkSketch;
            }
        }

        /// <summary>
        /// Gets this panel's InkCanvas.
        /// </summary>
        public InkCanvas InkCanvas
        {
            get
            {
                return inkSketch.InkCanvas;
            }
        }

        /// <summary>
        /// Gets or sets the recognized status of the panel
        /// </summary>
        public bool Recognized
        {
            get
            {
                return recognized;
            }
            set
            {
                recognized = value;
            }
        }

        /// <summary>
        /// Gets this panel's InkCircuit.
        /// </summary>
        public CircuitSimLib.Circuit Circuit
        {
            get
            {
                return inkSketch.Circuit;
            }
            set
            {
                inkSketch.Circuit = value;
            }
        }

        /// <summary>
        /// Gets or sets the editing mode of the InkPicture. 
        /// </summary>
        public InkCanvasEditingMode EditingMode
        {
            get
            {
                return inkCanvas.EditingMode;
            }
            set
            {
                if (inkCanvas.EditingMode.Equals(value))
                    return;
                inkCanvas.EditingMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the inverted editing mode of the InkPicture.
        /// </summary>
        public InkCanvasEditingMode EditingModeInverted
        {
            get
            {
                return inkCanvas.EditingModeInverted;
            }
            set
            {
                if (value == inkCanvas.EditingModeInverted)
                    return;

                inkCanvas.EditingModeInverted = value;

            }
        }

        public bool UseCustomCursor
        {
            get { return InkCanvas.UseCustomCursor; }
            set 
            {
                if (value != InkCanvas.UseCustomCursor)
                    InkCanvas.UseCustomCursor = value;
            }
        }

        new public System.Windows.Input.Cursor Cursor
        {
            get { return InkCanvas.Cursor; }
            set
            {
                if (value != InkCanvas.Cursor && UseCustomCursor)
                    InkCanvas.Cursor = value;
            }
        }


        #endregion
    }

    #region SketchPanel Constants
    /// <summary>
    /// Stores constant parameters for SketchPanels
    /// </summary>
    public class SketchPanelConstants
    {
        /// <summary>
        /// Default background color for sketch panel
        /// </summary>
        public static Brush DefaultBackColor = Brushes.White;

        /// <summary>
        /// Default color for ink strokes
        /// </summary>
        public static Color DefaultInkColor = Colors.Black;

        /// <summary>
        /// Default Zoom-In factor, where 1.0 is nominal.
        /// </summary>
        public const float ZoomInFactor = 1.2F;

        /// <summary>
        /// Default Zoom-Out factor, where 1.0 is nominal.
        /// </summary>
        public const float ZoomOutFactor = 0.8F;

        /// <summary>
        /// Default padding for ZoomToFit (left/right margin as % of SketchPanel's size).
        /// </summary>
        public const float Padding = 0.05F;
    }
    #endregion
}

