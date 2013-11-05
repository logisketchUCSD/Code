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

namespace SubCircuitWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Internals

        /// <summary>
        /// Wraps the given project in order to display the ink
        /// </summary>
        private InkToSketchWPF.InkCanvasSketch WrapperSketch;
        /// <summary>
        /// passed in project to display (has a sketch)
        /// </summary>
        private Sketch.Project subSketch;
        /// <summary>
        /// personal inkCanvas for the window
        /// </summary>
        private InkCanvas inkCanvas;

        #endregion

        /// <summary>
        /// Main constructor of the subcircuit window. Takes in a project that it will wrap into an inkcanvas sketch
        /// in order to display.
        /// </summary>
        /// <param name="subSketch"></param>
        public MainWindow(ref Sketch.Project subSketch)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // Log error
                System.Console.WriteLine(ex.InnerException.Message);
                //System.Console.WriteLine(ex.ListTrace);
            }

            // Set up notes panel
            this.subSketch = subSketch;
            WrapperSketch = new InkToSketchWPF.InkCanvasSketch(new InkCanvas());
            WrapperSketch.project = subSketch;
            this.WrapperSketch.ClearButNotSketch();
            // Makes the ink to display
            WrapperSketch.CreateInkStrokesFromSketch();

            //Set up the ink holder
            this.inkCanvas = WrapperSketch.InkCanvas;
            this.inkCanvas.Height = dockPanel.Height;
            this.inkCanvas.Width = dockPanel.Width;

            //The dockPanel is in a view box, which means that it will scale (and scale its children)
            //to fit the screen.
            TextBlock helpText = new TextBlock();
            helpText.Text = "You can click a sub-circuit if one exists to view it";
            helpText.FontSize = 16;

            dockPanel.Children.Clear();
            dockPanel.Children.Add(this.inkCanvas);
            this.inkCanvas.Children.Add(helpText);

            //Actually color the strokes on the inkcanvas according to the recognition
            //This is valid because only simulatable circuits can be viewed here
            foreach (System.Windows.Ink.Stroke inkStroke in this.inkCanvas.Strokes)
            {
                Sketch.Substroke substroke = (Sketch.Substroke)WrapperSketch.GetSketchSubstrokeByInk(inkStroke);
                Domain.ShapeType label = substroke.Type;

                Color color = label.Color;

                inkStroke.DrawingAttributes.Color = color;
            }

            //Draws the inkcanvas with the new colors
            this.WrapperSketch.InkCanvas.InvalidateVisual();
            this.inkCanvas.UpdateLayout();

            

            // Set Editing Modes
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.EditingModeInverted = InkCanvasEditingMode.None;
            inkCanvas.StylusDown += new StylusDownEventHandler(inkCanvas_StylusDown);
        }

        /// <summary>
        /// Default constructor (not used)
        /// </summary>
        public MainWindow()
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

            this.subSketch = null;
            this.inkCanvas = new InkCanvas();

            // Set Editing Modes
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.EditingModeInverted = InkCanvasEditingMode.None;
        }

        /// <summary>
        /// Handler to deal with the things in the view window when it is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Window_Closed(object sender, EventArgs e)
        {
            inkCanvas.Children.Clear();
            dockPanel.Children.Clear();
        }

        /// <summary>
        /// Test to see if you pen downed on a subcircuit in the picture, and display it
        /// </summary>
        private void inkCanvas_StylusDown(object sender, StylusEventArgs e)
        {
            //gather all strokes within 50 pixels of the pen down
            System.Windows.Ink.StrokeCollection strokesHit = inkCanvas.Strokes.HitTest(e.GetPosition(inkCanvas),50);
            foreach (System.Windows.Ink.Stroke stroke in strokesHit)
            {
                Sketch.Substroke substroke = (Sketch.Substroke)WrapperSketch.GetSketchSubstrokeByInk(stroke);
                // Check if you landed on a subcircuit
                if (substroke.ParentShape.Type == Domain.LogicDomain.SUBCIRCUIT)
                {
                    //Bring up a new window and truth table window for the new project
                    Sketch.Project subProject = subSketch.subProjectLookup[substroke.ParentShape.SubCircuitNumber];
                    MainWindow subCircuitWindow = new SubCircuitWindow.MainWindow(ref subProject);
                    SimulationManager.TruthTableWindow subTruthWindow = new SimulationManager.TruthTableWindow(subProject, substroke.ParentShape.Name);
                    subCircuitWindow.Show();
                    subTruthWindow.Show();
                    break;
                }
            }

        }

    }
}
