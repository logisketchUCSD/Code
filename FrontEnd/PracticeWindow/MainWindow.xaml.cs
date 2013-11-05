using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Domain;
using SketchPanelLib;

namespace PracticeWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Internals

        /// <summary>
        /// The canvas on which the user draws
        /// </summary>
        private InkCanvas inkCanvas;

        /// <summary>
        /// The InkSketch connected to the inkCanvas
        /// </summary>
        private InkToSketchWPF.InkCanvasSketch inkSketch;

        /// <summary>
        /// Draws the gate templates
        /// </summary>
        private GateDrawing.GateDrawing gateDrawer;

        /// <summary>
        /// Image of the current gate
        /// </summary>
        private Image gateImage;

        /// <summary>
        /// Recognizer 
        /// </summary>
        private RecognitionInterfaces.Recognizer recognizer;

        /// <summary>
        /// String with the name of the current gate
        /// </summary>
        private ShapeType gate;

        /// <summary>
        /// Default drop down choice prompts user to choose a gate
        /// </summary>
        private ComboBoxItem defaultChoice;

        /// <summary>
        /// String which indicates the gateless choice
        /// </summary>
        private const string freehandString = "Freehand";

        /// <summary>
        /// Constants for gate drawing/centering
        /// </summary>
        private const int GATE_WIDTH = 100;
        private const int GATE_HEIGHT = 70;
        private const int NOTBUBBLE_DIAMETER = 24;

        #endregion

        #region Constructor

        public MainWindow(ref InkToSketchWPF.InkCanvasSketch sketch, ref RecognitionInterfaces.Recognizer recog)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // Log error
                System.Console.WriteLine(ex.InnerException.Message);
            }

            // Set up inkSketch
            this.inkSketch = sketch;        

            // Set up practice panel
            this.inkCanvas = inkSketch.InkCanvas;
            this.inkCanvas.Height = practiceDock.ActualHeight;
            this.inkCanvas.Width = practiceDock.ActualWidth;
            if (!practiceDock.Children.Contains(this.inkCanvas))
                practiceDock.Children.Add(this.inkCanvas);

            inkCanvas.Strokes.StrokesChanged += new System.Windows.Ink.StrokeCollectionChangedEventHandler(Strokes_StrokesChanged);
            inkCanvas.StrokeCollected += new InkCanvasStrokeCollectedEventHandler(Strokes_StrokesCollected);
            inkCanvas.StrokeErasing += new InkCanvasStrokeErasingEventHandler(Strokes_StrokesErasing);

            // Set up gate drawing
            gateDrawer = new GateDrawing.GateDrawing();
            gateDrawer.LockDrawingRatio = false;

            recognizer = recog;

            // Set up the list of gates
            defaultChoice = new ComboBoxItem();
            defaultChoice.Content = "Choose a gate";
            gateChooser.Items.Add(defaultChoice);
            gateChooser.SelectedItem = defaultChoice;

            foreach (ShapeType gate in LogicDomain.Gates)
            {
                if (gate == LogicDomain.SUBCIRCUIT)
                    continue;
                ComboBoxItem item = new ComboBoxItem();
                item.Content = gate.Name;
                gateChooser.Items.Add(item);
            }
            ComboBoxItem free = new ComboBoxItem();
            free.Content = freehandString;
            gateChooser.Items.Add(free);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event for clear button.  Clears strokes on panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Clear_ButtonClick(object sender, RoutedEventArgs e)
        {
            clearStrokes();
        }

        public void Window_SizeChanged(object sender, RoutedEventArgs e)
        {
            double prevHeight = inkCanvas.Height;
            double prevWidth = inkCanvas.Width;
            inkCanvas.Height = practiceDock.ActualHeight;
            inkCanvas.Width = practiceDock.ActualWidth;
            //must have a gate to scale the gate and strokes around
            if (gateImage != null)
            {
                float oldLeft = (float)InkCanvas.GetLeft(gateImage);
                float oldTop = (float)InkCanvas.GetTop(gateImage);

                Rect newBounds = centerDrawing();

                System.Windows.Media.Matrix resizeMatrix = new System.Windows.Media.Matrix();
                resizeMatrix.Translate(newBounds.X - oldLeft, newBounds.Y - oldTop);

                inkCanvas.Strokes.Transform(resizeMatrix, false);

                inkSketch.UpdateInkStrokes(inkCanvas.Strokes);
            }

            
        }

        /// <summary>
        /// Clear the panel when the window closes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Window_Closed(object sender, EventArgs e)
        {
            clearStrokes();
            removeGate();
            recognizer.save();
            practiceDock.Children.Clear();

            inkCanvas.Strokes.StrokesChanged -= new System.Windows.Ink.StrokeCollectionChangedEventHandler(Strokes_StrokesChanged);
            inkCanvas.StrokeCollected -= new InkCanvasStrokeCollectedEventHandler(Strokes_StrokesCollected);
            inkCanvas.StrokeErasing -= new InkCanvasStrokeErasingEventHandler(Strokes_StrokesErasing);
        }        

        /// <summary>
        /// Event to hide recognition text when the strokes change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Strokes_StrokesChanged(object sender, System.Windows.Ink.StrokeCollectionChangedEventArgs e)
        {
            recognizeFeedback.Visibility = System.Windows.Visibility.Hidden;
        }

        public void Strokes_StrokesCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            inkCanvas.Strokes.Remove(e.Stroke);
            inkSketch.AddStroke(e.Stroke);
        }

        public void Strokes_StrokesErasing(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            inkSketch.DeleteStroke(e.Stroke);
        }

        /// <summary>
        /// Ensures that we are able to display the "Choose a gate" text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gateChooser_DropDownClosed(object sender, EventArgs e)
        {
            if (gateChooser.SelectedItem != defaultChoice && gateChooser.Items.Contains(defaultChoice))
                gateChooser.Items.Remove(defaultChoice);
        }

        /// <summary>
        /// Event for gate chooser.  Updates gate image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gateChooser_SelectionChanged(object sender, RoutedEventArgs e)
        {
            clearStrokes();            
            recognizeFeedback.Visibility = System.Windows.Visibility.Hidden;
            centerDrawing();
            return;
        }
        private Rect centerDrawing()
        {
            removeGate();
            Rect bounds;

            if (gateChooser.SelectedItem == defaultChoice || (string)((ComboBoxItem)gateChooser.SelectedItem).Content == freehandString)
            {
                gate = null;
                return new Rect();
            }

            gate = LogicDomain.getType((string)((ComboBoxItem)(gateChooser.SelectedItem)).Content);

            // Bounds are currently arbitrary, place shape template in the middle of canvas
            if (gate != LogicDomain.NOTBUBBLE)
                bounds = new Rect(inkCanvas.Width/2 - GATE_WIDTH/2, inkCanvas.Height/2 - GATE_HEIGHT/2, GATE_WIDTH, GATE_HEIGHT);
            else
                bounds = new Rect(inkCanvas.Width/2 - NOTBUBBLE_DIAMETER/2, inkCanvas.Height/2 - NOTBUBBLE_DIAMETER/2, NOTBUBBLE_DIAMETER, NOTBUBBLE_DIAMETER);


            // This is so NANDs look like ANDs with NOTBUBBLEs
            // Same goes for all gates in the OR family
            if (gate == LogicDomain.AND)
                bounds.Width = bounds.Width - bounds.Width / 4;
            else if (gate == LogicDomain.OR)
                bounds.Width = bounds.Width - bounds.Width / 6 - bounds.Width / 4;
            else if (gate == LogicDomain.NOR)
                bounds.Width = bounds.Width - bounds.Width / 6;
            else if (gate == LogicDomain.XOR)
                bounds.Width = bounds.Width - bounds.Width / 4;

            DrawingImage drawingImage = new DrawingImage(gateDrawer.DrawGate(gate, bounds, false, true));
            gateImage = new Image();
            gateImage.Source = drawingImage;

            InkCanvas.SetLeft(gateImage, bounds.Left);
            InkCanvas.SetTop(gateImage, bounds.Top);

            inkCanvas.Children.Add(gateImage);
            return bounds;
        }
        
        #endregion

        #region Recognition

        /// <summary>
        /// Event which recognizes the strokes on the canvas through a button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recognizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Have the cursor indicate that we are thinking
            inkCanvas.UseCustomCursor = true;
            inkCanvas.Cursor = Cursors.Wait;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;

            // Make sure that nothing is in a shape or group
            inkSketch.Sketch.clearShapes();
            inkSketch.Sketch.RemoveLabelsAndGroups();

            // Add all the strokes to a single shape, label them all as belonging to a gate
            Sketch.Shape shape = new Sketch.Shape(inkSketch.Sketch.SubstrokesL, Sketch.XmlStructs.XmlShapeAttrs.CreateNew());
            shape.Classification = LogicDomain.GATE_CLASS;

            // Send the shape to the recognizer
            recognizer.processShape(shape, inkSketch.FeatureSketch);

            // Color all the strokes
            Color color = Colors.Black;
            if (shape.Type == gate || gate == null)
            {
                color = shape.Type.Color;
            }
            foreach (System.Windows.Ink.Stroke stroke in inkCanvas.Strokes)
            {
                stroke.DrawingAttributes.Color = color;
            }

            // Write what the gate was recognized as
            recognizeFeedback.Text = "Recognized as " + shape.Type + "\nwith confidence of " + (int)(shape.Probability * 100) + "%.";
            recognizeFeedback.Visibility = System.Windows.Visibility.Visible;

            // Save the shape to the inkSketch
            inkSketch.Sketch.AddShape(shape);

            Refiner.UniqueNamer uniqueNamer = new Refiner.UniqueNamer();
            uniqueNamer.process(inkSketch.Sketch);

            // Save the shape to the recognizer
            //if you don't know what they meant to draw, you can't learn from it
            if (gate != null)
            {
                shape.Type = gate;
                recognizer.learnFromExample(shape);
            }

            // Return the cursor to normal
            inkCanvas.UseCustomCursor = false;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
        }

        #endregion

        #region Saving
        private void menuSaveSketchAs_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();

            saveFileDialog.Filter = "MIT XML Files (*.xml)|*.xml";
            saveFileDialog.AddExtension = true;
            saveFileDialog.RestoreDirectory = false;

            if (saveFileDialog.ShowDialog() == true)
            {
                if (!saveFileDialog.FileName.Contains(".xml"))
                {
                    MessageBox.Show("Filename is not valid. Sketch must be saved as an XML.");
                    return;
                }
                else
                {
                    saveFile(saveFileDialog.FileName);
                }
            }
        }

        /// <summary>
        /// Saves all the project files
        /// 
        /// Precondition: all project file paths are valid and exist.
        /// </summary>
        private void saveFile(string filepath)
        {
            // Make sure project directory exists.
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filepath)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath));
            }

            inkSketch.SaveSketch(filepath);
        }
        #endregion

        #region Helpers

        /// <summary>
        /// Remove the gate image from the inkCanvas
        /// </summary>
        private void removeGate()
        {
            if (inkCanvas.Children.Contains(gateImage))
                inkCanvas.Children.Remove(gateImage);
        }

        private void clearStrokes()
        {
            inkCanvas.Strokes.Clear();
            inkSketch.Clear();
            recognizeFeedback.Visibility = System.Windows.Visibility.Hidden;
        }

        #endregion
    }
}
