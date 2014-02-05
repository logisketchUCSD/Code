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
using System.Windows.Threading;
using System.Windows.Ink;
using System.Collections;

using System.Windows.Controls.Primitives; 
using System.Windows.Media.Animation;
using System.Threading;

using SketchPanelLib; 
using ConverterXML;
using Domain;

namespace WPFCircuitSimulatorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Front end.
    /// Contains panels, menu item callbacks, button callbacks, etc.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Internals

        /// <summary>
        /// Set to true to print debug statements
        /// </summary>
        private bool debug = false;

        /// <summary>
        /// Bool for drawing style adaptations user study.  Setting to true will record metrics and save sketches upon recognition
        /// </summary>
        private bool UserStudy = false;

        #region Panels

        /// <summary>
        /// Panel for drawing the circuit schematic
        /// </summary>
        private SketchPanel circuitPanel;

        /// <summary>
        /// Panel for drawing notes
        /// </summary>
        private InkCanvas notesPanel;

        /// <summary>
        /// Panel for displaying the subcircuit that is loaded
        /// </summary>
        private InkCanvas displaySubCircuitPanel;

        /// <summary>
        /// Recognizer for practice area
        /// </summary>
        private RecognitionInterfaces.Recognizer practiceRecognizer;

        /// <summary>
        /// InkCanvasSketch for the practice window.
        /// </summary>
        private InkToSketchWPF.InkCanvasSketch practiceSketch;

        /// <summary>
        /// Notes window with notesPanel
        /// </summary>
        private NotesWindow.MainWindow notesWindow;

        /// <summary>
        /// Window to display the Subcircuits in
        /// </summary>
        private SubCircuitWindow.MainWindow subCircuitWindow;

        /// <summary>
        /// Window for practicing gate drawing
        /// </summary>
        private PracticeWindow.MainWindow practiceWindow;

        /// <summary>
        /// Window which shows recognition progress.
        /// 
        /// To update, call UpdateProgress.  Please refrain from accessing it otherwise!
        /// </summary>
        private ProgressWindow.MainWindow progressWindow;

        /// <summary>
        /// Window that shows a matching template
        /// </summary>
        private TemplateWindow.MainWindow templateWindow;

        #endregion

        #region Managers and Recognizers

        /// <summary>
        /// Recognizes circuit
        /// </summary>
        private RecognitionManager.RecognitionManager recognitionManager;

        /// <summary>
        /// Used to simulate the circuit after recognition
        /// </summary>
        private SimulationManager.SimulationManager simulationManager;

        /// <summary>
        /// If we have already saved once, the user does not need to go through the 
        /// save window again.
        /// </summary>
        private bool firstSave = true;

        /// <summary>
        /// Indicates if the sketch has been modified since it was last saved.
        /// </summary>
        private bool dirty = false;

        /// <summary>
        /// keep track of if the recognition of on the fly is on
        /// </summary>
        private bool onFlyTurnedOn = false;

        /// <summary>
        /// The path of the current sketch
        /// </summary>
        private string currentFilePath = null;

        /// <summary>
        /// List of filepaths for sketches that were ever saved.
        /// </summary>
        private HashSet<string> pastSavedSketches = new HashSet<string>();

        /// <summary>
        /// Handles displaying the results on the screen.
        /// </summary>
        private DisplayManager.DisplayManager displayManager;

        /// <summary>
        /// Selection tool so that we actually get the power of the labeler
        /// </summary>
        private SelectionManager.SelectionManager selectionTool;

        /// <summary>
        /// Command Manager for providing Undo/Redo
        /// </summary>
        private CommandManagement.CommandManager commandManager;

        #endregion

        #region Status Bar

        public delegate void doNextStep();

        public enum Status
        {
            /// <summary>
            /// UI is idle
            /// </summary>
            Idle,

            /// <summary>
            /// UI is currently running a recognition process
            /// </summary>
            Recognizing,

            /// <summary>
            /// UI is currently simulating the circuit
            /// </summary>
            Simulating,

            /// <summary>
            /// UI is currently loading a sketch
            /// </summary>
            Loading,
        }

        /// <summary>
        /// The current status of the system. 
        /// Access through CurrentStatus
        /// </summary>
        private Status currentStatus;

        /// <summary>
        /// Use this to access currentStatus and statusStripLabel.  Sets the status strip label text to reflect new status.
        /// </summary>
        private Status CurrentStatus
        {
            get
            {
                return currentStatus;
            }
            set
            {
                currentStatus = value;
                if (currentStatus == Status.Idle)
                    statusStripLabel.Content = "Idle.";
                else if (currentStatus == Status.Recognizing)
                    statusStripLabel.Content = "Recognizing...";
                else if (currentStatus == Status.Simulating)
                    statusStripLabel.Content = "Simulating...";
                else if (currentStatus == Status.Loading)
                    statusStripLabel.Content = "Loading...";
            }
        }

        #endregion

        #region Simulation Modes
        /// <summary>
        /// Indicates when strokes have been changed and it needs to be re-recognized.
        /// </summary>
        private bool needsRerec = true;

        /// <summary>
        /// Indicates when the sketch has been changed and it needs to be re-simulated.
        /// OR when the circuit parser failed? (If you know, change this. Fiona 24-Jun-2011
        /// </summary>
        private bool needsResim = true;

        #endregion

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Constructor, initializes the window
        /// </summary>
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitializeEverything();
            }
            catch (Exception e)
            {
                //Small bug of wpf doesn't allow you to open message boxes until a wpf window is opened, so this
                //is a small work around to get the message to stay
                Window WpfBugWindow = new Window()
                {
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStyle = WindowStyle.None,
                    Top = 0,
                    Left = 0,
                    Width = 1,
                    Height = 1,
                    ShowInTaskbar = false
                };

                WpfBugWindow.Show();

                //throw e;
                Console.WriteLine(e.InnerException.Message);
                Console.WriteLine(e.StackTrace);

                // This message box closes immediately when it is shown.  If you're bored, fix it!
                MessageBoxResult result = MessageBox.Show("The program failed to open. Try running as an administrator.\n\n" +
                    "If that doesn't work, some files may be missing, and you should try reinstalling the program.", "Oh Noes!", MessageBoxButton.OK);
                Close();
            }
        }

        /// <summary>
        /// Initializes everything that the main window uses.
        /// </summary>
        private void InitializeEverything()
        {
            // Set up sketch panel
            commandManager = new CommandManagement.CommandManager(new Func<bool>(commandManager_updateEditMenu), new Func<bool>(commandManager_moveUpdate), UserStudy);
            circuitPanel = new SketchPanel(commandManager);
            notesPanel = new InkCanvas();
            displaySubCircuitPanel = new InkCanvas();

            // Add panels and configure layout
            circuitDock.Children.Clear();
            circuitDock.Children.Add(circuitPanel);

            circuitPanel.Name = "Circuit";
            circuitPanel.Height = circuitDock.ActualHeight;
            circuitPanel.Width = circuitDock.ActualWidth;

            // Add recognizers
            if (debug) Console.WriteLine("RecognitionManager");
            recognitionManager = new RecognitionManager.RecognitionManager();

            // Set up managers
            selectionTool = new SelectionManager.SelectionManager(ref commandManager, ref circuitPanel);
            displayManager = new DisplayManager.DisplayManager(ref circuitPanel, endPointChange, gateRotated);

            // Set project name
            this.Title = FilenameConstants.ProgramName + " - New Project";

            // Set up the SimulationManager
            simulationManager = new SimulationManager.SimulationManager(ref circuitPanel);

            SubscribeEverything();
            commandManager_updateEditMenu();

            #region Practice Window Stuff
            InkCanvas practiceCanvas = new InkCanvas();
            practiceSketch = new InkToSketchWPF.InkCanvasSketch(practiceCanvas);

            templateWindow = new TemplateWindow.MainWindow();
            // Set Editing Modes
            practiceCanvas.EditingMode = InkCanvasEditingMode.Ink;
            practiceCanvas.EditingModeInverted = InkCanvasEditingMode.EraseByStroke;

            // Set up recognizer
            practiceRecognizer = RecognitionManager.RecognitionPipeline.createDefaultRecognizer();
            #endregion

            // Set up user study things
            if (UserStudy)
                UserStudyOn();

            onFlyTurnedOn = (bool)shapesWhileDrawing.IsChecked;
            if (onFlyTurnedOn)
                recognitionManager.FinishLoading();

            // Set current status
            CurrentStatus = Status.Idle;
            circuitPanel.EnableDrawing();

            loadEmbedPanel();
        }

        /// <summary>
        /// Pre-loads circuits that LogiSketch knows about into the Embed Panel.
        /// </summary>
        private void loadEmbedPanel()
        {
            if (!System.IO.File.Exists(FilenameConstants.PreviouslySavedCircuits))
                return;
            
            foreach(string filepath in System.IO.File.ReadLines(FilenameConstants.PreviouslySavedCircuits))
            {
                if (!verifyLoadValid(filepath))
                    continue;
                try
                {
                    openSubCircuit(filepath, true);
                    pastSavedSketches.Add(filepath);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine("Could not load file: " + filepath + "\r\n" +
                     " With error message: " + ex.Message);
                    throw ex;
#endif
                }
            }
        }

        /// <summary>
        /// Writes the list of all LogiSketch files. (for the embed panel)
        /// </summary>
        private void recordLogiSketchFilePaths()
        {
            System.IO.File.WriteAllLines(FilenameConstants.PreviouslySavedCircuits, pastSavedSketches);
        }

        #endregion 

        #region Subscription

        /// <summary>
        /// Handles main window subscription
        /// </summary>
        private void SubscribeEverything()
        {
            // Set up highlighting-of-current-panel feature (We only have one panel, so not used)
            //mainMenu.MouseEnter += new MouseEventHandler(menu_MouseEnter);
            //circuitPanel.InkCanvas.MouseEnter += new MouseEventHandler(sketchArea_MouseEnter);

            // Gesture Recognition (Not really used right now!)
            circuitPanel.InkCanvas.SetEnabledGestures(new ApplicationGesture[]
                { //ApplicationGesture.Triangle,
                //ApplicationGesture.Curlicue,
                ApplicationGesture.ScratchOut,
                ApplicationGesture.Exclamation });
            circuitPanel.InkCanvas.Gesture += new InkCanvasGestureEventHandler(GestureHandler);

            // Configure recognizers
            circuitPanel.SketchFileLoaded += new SketchFileLoadedHandler(circuitPanel_SketchFileLoaded);

            // Selection tool
            selectionTool.SubscribeToPanel();

            // Make sure menu cut and copy are enabled at the right times
            circuitPanel.InkCanvas.SelectionChanged += new EventHandler(InkCanvas_SelectionChanged);
            commandManager.Clear();
            enableEditing();

            // Set up mode indicator
            circuitPanel.InkCanvas.ActiveEditingModeChanged += new RoutedEventHandler(InkCanvas_EditingModeChanged);
            modeIndicator.Text = "Current Mode: Ink";

            // Set up so we know when we need to re-recognize and re-build the circuit
            selectionTool.InkRerecognized += new SelectionManager.InkRerecognizedEventHandler(InkChanged);
            selectionTool.GroupTogether += new SelectionManager.RegroupShapesHandler(rerecognizeShapesAndReconnect);
            selectionTool.Learn += new SelectionManager.LearningEventHandler(recognitionManager.Recognizer.learnFromExample);
            selectionTool.Selecting +=  new SelectionManager.SelectingEventHandler(selectionTool_Selecting);
            selectionTool.LoadSub += new SelectionManager.LoadSubCircuit(selectionTool_LoadSub);
            circuitPanel.InkChanged += new InkChangedEventHandler(InkChanged);
            subscribeToSubcircuitChanges(circuitPanel.InkSketch.Sketch);

            // Set the content of the recognize button based on our simulation mode
            recognizeButton.Content = "Recognize";
            simulationButton.Content = "Simulate";

            recognizeButton.Checked += new RoutedEventHandler(recognizeButton_Click);
            simulationButton.Checked += new RoutedEventHandler(delayedSimButton_Click);

            embedButton.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Resets main window by unsubscribing event handlers and clearing command list
        /// </summary>
        private void UnsubscribeEverything()
        {
            // Gesture Recognition
            circuitPanel.InkCanvas.Gesture -= new InkCanvasGestureEventHandler(GestureHandler);

            // De-configure recognizers
            circuitPanel.SketchFileLoaded -= new SketchFileLoadedHandler(circuitPanel_SketchFileLoaded);

            // Unsubscribe feedback and tools
            displayManager.UnsubscribeFromPanel();
            selectionTool.UnsubscribeFromPanel();
            simulationManager.UnsubscribeFromPanel();

            // Make sure menu cut and copy are enabled at the right times
            circuitPanel.InkCanvas.SelectionChanged -= new EventHandler(InkCanvas_SelectionChanged);

            // Remove mode indicator event
            circuitPanel.InkCanvas.ActiveEditingModeChanged -= new RoutedEventHandler(InkCanvas_EditingModeChanged);

            // Remove project manager events (project manager not currently used)
            //projectManagerForm.ProjectSaving -= new ProjectSavingHandler(saveProjectFiles);
            //projectManagerForm.ProjectOpening -= new ProjectOpeningHandler(openFiles);

            // Remove ink and recognition change evnts
            selectionTool.InkRerecognized -= new SelectionManager.InkRerecognizedEventHandler(InkChanged);
            selectionTool.GroupTogether -= new SelectionManager.RegroupShapesHandler(rerecognizeShapesAndReconnect);
            selectionTool.Learn -= new SelectionManager.LearningEventHandler(recognitionManager.Recognizer.learnFromExample);
            selectionTool.Selecting -= new SelectionManager.SelectingEventHandler(selectionTool_Selecting);
            selectionTool.LoadSub -= new SelectionManager.LoadSubCircuit(selectionTool_LoadSub);
            circuitPanel.InkChanged -= new InkChangedEventHandler(InkChanged);
            //circuitPanel.drawGates -= new InkChangedOnFlyRecHandler(drawGates);
            circuitPanel.InkSketch.Sketch.subCircuitRemoved -= new Sketch.RemoveSubCircuitFromSimulation(removeSubCircuit);
            circuitPanel.InkSketch.Sketch.subCircuitAdded -= new Sketch.AddSubCircuitToSimulation(addSubCircuit);
            //circuitPanel.InkSketch.OnFlyRecognition -= new InkToSketchWPF.OnFlyRecognitionHandler(circuitPanel_OnFlyRecognition);

            // Unsubscribe simulaion/recognition buttons
            recognizeButton.Checked -= new RoutedEventHandler(recognizeButton_Click);
            simulationButton.Checked -= new RoutedEventHandler(delayedSimButton_Click);
        }

        #endregion

        #region CLEAR

        /// <summary>
        /// Resets the state of the GUI to the "no project loaded" state
        /// </summary>
        public void Clear()
        {
            SimToEdit();

            simulationButton.Visibility = System.Windows.Visibility.Hidden;
            notesPanel.Strokes.Clear();
            displaySubCircuitPanel.Strokes.Clear();
            displayManager.RemoveFeedbacks();

            circuitPanel.SimpleDeleteAllStrokes();
            commandManager.Clear();
            enableEditing();
            circuitPanel.EnableDrawing();

            // Set project name
            this.Title = FilenameConstants.ProgramName + " - New Project";

            // Reset some variables
            needsRerec = true;
            needsResim = true;
            firstSave = true;
            dirty = false;
            currentFilePath = null;
        }

        #endregion

        #region Main Window Changing Events

        /// <summary>
        /// When the window is resized, we also resize the sketchPanels.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_SizeChanged(object sender, RoutedEventArgs e)
        {
            circuitPanel.Height = circuitDock.ActualHeight;
            circuitPanel.Width = circuitDock.ActualWidth;
            if (simulationManager.DisplayingClean)
            {
                simulationManager.CleanCircuit.Height = circuitDock.ActualHeight;
                simulationManager.CleanCircuit.Width = circuitDock.ActualWidth;
            }

            if (selectionTool.Subscribed)
                selectionTool.RemoveAllWidgets();

            if (displayManager.Subscribed)
                displayManager.RefreshFeedbacks();

            if (simulationManager.Subscribed)
            {
                simulationManager.MakeNewToggles();
                simulationManager.ShowToggles();
            }
        }

        /// <summary>
        /// When the window is minimized or resized, the popups should be closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Maximized:
                    if (simulationManager.Subscribed)
                    {
                        simulationManager.MakeNewToggles();
                        simulationManager.ShowToggles();
                    }
                    displayManager.RefreshFeedbacks();
                    break;
                case WindowState.Minimized:
                    if (selectionTool.Subscribed)
                        selectionTool.RemoveAllWidgets();
                    if (simulationManager.Subscribed)
                        simulationManager.HideToggles();
                    displayManager.RemoveFeedbacks();
                    break;
                case WindowState.Normal:
                    if (simulationManager.Subscribed)
                        simulationManager.ShowToggles();
                    displayManager.RefreshFeedbacks();
                    break;
            }
        }

        
        /// <summary>
        /// Changes the mode indicator to reflect the current editing mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_EditingModeChanged(object sender, RoutedEventArgs e)
        {
            InkCanvasEditingMode mode = circuitPanel.InkCanvas.ActiveEditingMode;

            if (mode == InkCanvasEditingMode.InkAndGesture ||
                mode == InkCanvasEditingMode.Ink)
                modeIndicator.Text = "Current Mode: Ink";
            else if (mode == InkCanvasEditingMode.Select ||
                mode == InkCanvasEditingMode.None)
                modeIndicator.Text = "Current Mode: Selection";
            else if (mode == InkCanvasEditingMode.EraseByStroke ||
                mode == InkCanvasEditingMode.EraseByPoint)
                modeIndicator.Text = "Current Mode: Erasing";
            else
                modeIndicator.Text = "Current Mode: Unknown";
        }

        /// <summary>
        /// When the window is deactivated, we want to close all the input/output toggles and take away other popups
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_Deactivated(object sender, EventArgs e)
        {
            if (selectionTool.Subscribed)
                selectionTool.RemoveAllWidgets();

            if (simulationManager != null && simulationManager.Subscribed)
                simulationManager.HideToggles();

            if (displayManager.Subscribed)
                displayManager.RemoveFeedbacks();
        }

        /// <summary>
        /// When the window is activated, we want to bring back the input/output toggles
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_Activated(object sender, EventArgs e)
        {
            if (simulationManager != null && simulationManager.Subscribed)
                simulationManager.ShowToggles();
            else
                displayManager.RefreshFeedbacks();
        }
        
        #endregion

        #region Callbacks

        /// <summary>
        /// Clears any adaptations the adaptive image recognizer has made
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearAdaptations_Click(object sender, RoutedEventArgs e)
        {
            recognitionManager.Recognizer.reset();
        }

        #region Project File I/O Callbacks


        /// <summary>
        /// Open the xml for a subcircuit and update the dictionaries in InkCanvasSketch
        /// 
        /// </summary>
        private void openSubCircuit(string filepath, bool initialLoad = false)
        {
            if (circuitPanel.InkSketch.alreadyLoaded.Contains(filepath))
            {
                MessageBox.Show(this, "The chosen file has already been loaded to the embed panel", "Already Loaded");
                return;
            }
            Wait();

            // Set status and load files
            CurrentStatus = Status.Loading;

            try
            {
                ReadXML readXml = circuitPanel.LoadXml(filepath);
                Sketch.Project addedProject = circuitPanel.InkSketch.createProject(readXml);
                // Check to see if it's valid to add to the list
                if (addedProject.behavior.Count == 0)
                {
                    if (!initialLoad)
                    {
                        EndWait();
                        MessageBox.Show(this, "The chosen file does not contain a valid circuit", "Invalid Circuit");
                    }
                    return;
                }
                //make the new button to add to the list
                // the tag is the index in the subsketch list to look up
                string name = System.IO.Path.GetFileNameWithoutExtension(filepath);
                int tag = circuitPanel.InkSketch.newSubSketch(addedProject, filepath);

                AddEmbedOption(name, tag);
            }
            catch
            {
                MessageBoxResult result = MessageBox.Show("The file you selected failed to create a project.\n\n" +
                    "Make sure the file is valid or try a different one.", "Oh Noes!", MessageBoxButton.OK);
            }

            CurrentStatus = Status.Idle;
            EndWait();
        }

        /// <summary>
        /// Adding the appropriate buttons to the Embed Panel. This takes in a shape, so this function is executed
        /// When shapes exist, both on loading a file and adding through the embed button.
        /// </summary>
        /// <param name="shape"></param>
        private void AddToList(Sketch.Shape shape)
        {
            //Add to the list, since names are like sub_1 we want to remove the number for the text of the subcircuit.
            AddEmbedOption(shape.Name.Split('_')[0], shape.SubCircuitNumber);
        }

        /// <summary>
        /// Function for adding things to the embed panel, this can happen a couple of ways
        /// From a shape which happens with the embed widget or loading from a file
        /// and from the add to list feature of the panel
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tagNumber"></param>
        private void AddEmbedOption(string name, int tagNumber)
        {
            DockPanel buttonDock = new DockPanel();
            TextBlock displayName = new TextBlock();
            displayName.Text = name;
            
            //Create the adding button. The Tag value for the button is the tag number
            // to lookup the sub project in the dictionary
            Button newSub = new Button();
            newSub.Content = "Add";
            newSub.Click += new RoutedEventHandler(AddSubcircuit);
            newSub.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            DockPanel.SetDock(newSub, Dock.Right);
            newSub.Tag = new List<object>();
            ((List<object>)newSub.Tag).Add(tagNumber);
            ((List<object>)newSub.Tag).Add(name);
            
            //Make the view button
            Button view = new Button();
            view.Content = "View";
            view.Click += new RoutedEventHandler(viewSubcircuit);
            view.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            view.Tag = new List<object>();
            ((List<object>)view.Tag).Add(tagNumber);
            ((List<object>)view.Tag).Add(name);
            DockPanel.SetDock(view, Dock.Right);

            //Adding in this order because we want the buttons to be stacked to the right
            //Which is taken care of by docking them both to the right and adding them, then
            //the text fills the rest of the space.
            buttonDock.Children.Add(newSub);
            buttonDock.Children.Add(view);
            buttonDock.Children.Add(displayName);

            embedPanel.Children.Add(buttonDock);
        }

        /// <summary>
        /// Does the actual drawing of the subcircuit shape and adds the the lookup in InkCanvasSketch
        /// Tells the shape the number it cooresponds to and creates the Circuit Element
        /// </summary>
        private void AddSubcircuit(object sender, RoutedEventArgs e)
        {
            //Getting the tag number from the button (see addEmbedOption for where this is declared)
            int subIndex = (int)((List<object>)((Button)sender).Tag)[0];
            if (!circuitPanel.InkSketch.Sketch.tagToName.ContainsKey(subIndex))
                circuitPanel.InkSketch.Sketch.tagToName.Add(subIndex, (string)((List<object>)((Button)sender).Tag)[1]);
            //Make the added stroke a subcircuit gate
            commandManager.ExecuteCommand(new EditMenu.CommandList.AddSubCircuitCmd(ref circuitPanel, new Point(200,150), subIndex));
        }

        /// <summary>
        /// Display an image of the subcircuit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewSubcircuit(object sender, RoutedEventArgs e)
        {
            //get the sub project based on the tag saved to this view button
            int subIndex = (int)((List<object>)((Button)sender).Tag)[0];
            Sketch.Project subSketch = circuitPanel.InkSketch.project.subProjectLookup[subIndex];

            //Open up the window
            subCircuitWindow = new SubCircuitWindow.MainWindow(ref subSketch);
            subCircuitWindow.Show();
            subCircuitWindow.Closed += new EventHandler(subCircuitWindow_Closed);

            //Open a truth table from a project. This does not open the window with the ability to simulate things in the 
            // sketch.
            SimulationManager.TruthTableWindow truthTableWindow = new SimulationManager.TruthTableWindow(subSketch, (string)((List<object>)((Button)sender).Tag)[1]);
            truthTableWindow.Show();

        }


        /// <summary>
        /// When loading a sketch from a file you need to update simulation manager
        /// </summary>
        /// <param name="sketch"></param>
        private bool loadSubCircuitInformation()
        {
            //reset the current sub circuit information
            simulationManager.ClearSubCircuits();
            circuitPanel.InkSketch.FeatureSketch.Sketch.numSubCircuits = 0;
            circuitPanel.InkSketch.alreadyLoaded = new List<string>();

            //remove buttons from the left panel since they dont apply anymore
            List<UIElement> toRemove = new List<UIElement>();
            foreach (UIElement element in embedPanel.Children)
            {
                if (element is DockPanel)
                    toRemove.Add(element);
            }
            foreach (UIElement element in toRemove)
            {
                embedPanel.Children.Remove(element);
            }

            //The loaded sketch will have shapes, so the subcircuits in those shapes will have to
            // update the embed panel as well as dictionaries in simulation and project
            Dictionary<int, int> oldToNewTag = new Dictionary<int, int>();
            foreach (Sketch.Shape shape in circuitPanel.Sketch.Shapes)
            {
                if (shape.Type == LogicDomain.SUBCIRCUIT)
                {
                    //Since the loaded stuff keeps around sub circuit numbers, we need to push them back down
                    // to agree with the embed panel
                    if (oldToNewTag.ContainsKey(shape.SubCircuitNumber))
                        shape.SubCircuitNumber = oldToNewTag[shape.SubCircuitNumber];
                    else
                    {
                        int newTag = oldToNewTag.Count;
                        oldToNewTag.Add(shape.SubCircuitNumber, newTag);
                        shape.SubCircuitNumber = newTag;
                        AddToList(shape);
                    }
                    addSubCircuit(shape);
                    // Sketch keeps track for naming future subcircuits
                    circuitPanel.InkSketch.project.sketch.numSubCircuits++;
                }
            }
            return true;
        }

        /// <summary>
        /// Open the xml file
        /// </summary>
        private void loadFile(string filepath)
        {
            Clear();
            circuitPanel.LoadSketch(filepath, new Func<bool>(loadSubCircuitInformation));
            subscribeToSubcircuitChanges(circuitPanel.InkSketch.Sketch);
            recognitionManager.ConnectSketch(circuitPanel.InkSketch.FeatureSketch);
        }


        /// <summary>
        /// Saves all the project files
        /// 
        /// Precondition: all project file paths are valid and exist.
        /// </summary>
        private void saveProjectFiles(string filepath)
        {
            Wait();

            circuitPanel.SaveSketch(filepath);

            firstSave = false;
            dirty = false;

            this.Title = FilenameConstants.ProgramName + " - " + System.IO.Path.GetFileNameWithoutExtension(filepath);

            EndWait();
        }

        private void saveLogisim(string filepath)
        {
            Wait();

            circuitPanel.InkSketch.ExportToLogiSim(filepath);

            EndWait();
        }

        /// <summary>
        /// Clears the data structures.
        /// </summary>
        private void menuNewSketch_Click(object sender, RoutedEventArgs e)
        {
            if (dirty)
            {
                MessageBoxResult result = MessageBox.Show(this, "Sketch has not been saved. Do you want to save?",
                    "Warning: Unsaved Sketch", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    if (!successfullyPromptAndSave())
                        return;
                }
                else if (result == MessageBoxResult.Cancel)
                    return;
            }

            Wait();

            Clear();

            EndWait();
        }

        /// <summary>
        /// Exits the application.  Prompts the user to save the project if the project is currently
        /// unsaved.
        /// </summary>
        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (recognitionManager != null)
                recognitionManager.Recognizer.save();
            recordLogiSketchFilePaths();
            closeProgressWindow();
            closePractice(null, null);
            closeNotes(null, null);
            if (!respondedToSave())
                e.Cancel = true;
            else
                System.Environment.Exit(0); // kills all background threads too
        }

        /// <summary>
        /// Handler for the embed button to display the menu for embeding circuits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void embedButton_Click(object sender, RoutedEventArgs e)
        {
            subCircuitPanel.Visibility = Visibility.Visible;

            // Adjust z-order to ensure the pane is on top:
            Grid.SetZIndex(subCircuitPanel, 1);
            Canvas.SetZIndex(circuitPanel, 1);

            DoubleAnimation animate = new DoubleAnimation();
            animate.From = 0;
            animate.To = 220;
            animate.Duration = TimeSpan.FromMilliseconds(250);
            ActionPanel.BeginAnimation(Border.WidthProperty, animate);

        }
        private void closeEmbed_Click(object sender, RoutedEventArgs e)
        {
            // Adjust z order to ensure the pane is on top:
            Grid.SetZIndex(subCircuitPanel, 1);
            Canvas.SetZIndex(circuitPanel, 1);

            DoubleAnimation animate = new DoubleAnimation();
            animate.From = 220;
            animate.To = 0;
            animate.Duration = TimeSpan.FromMilliseconds(250);
            ActionPanel.BeginAnimation(Border.WidthProperty, animate);
        }

        /// <summary>
        /// Asks if the user wants to save their sketch
        /// </summary>
        /// <returns>true if the user makes a choice, false if the user cancels</returns>
        private bool respondedToSave()
        {
            if (dirty && !UserStudy)
            {
                string message = "Would you like to save your sketch before exiting?";
                string title = "Confirm Application Exit";
                System.Windows.MessageBoxButton buttons = MessageBoxButton.YesNoCancel;
                MessageBoxResult result = MessageBox.Show(this, message, title, buttons);

                if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
                else if (result == MessageBoxResult.Yes)
                    return successfullySave();
            }
            return true;
        }

        /// <summary>
        /// Displays a help window with instructions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.Show();
        }

        private void loadSubCircuit_Click(object sender, RoutedEventArgs e)
        {
            string filepath = getLoadFilepath();
            if (filepath == null) // did not choose a file
                return;
            
            openSubCircuit(filepath);
            pastSavedSketches.Add(filepath);
        }

        private void menuLoadSketch_Click(object sender, RoutedEventArgs e)
        {
            // Prompt the user to save their dirty sketch (ha ha)
            if (!respondedToSave()) // cancelled loading
                return;

            // Prompt the user to choose a filepath.
            string filepath = getLoadFilepath();
            if (filepath == null) // did not choose a file
                return;

            Wait();
            CurrentStatus = Status.Loading;

            try
            {
                loadFile(filepath);

                this.Title = FilenameConstants.ProgramName + " - " + System.IO.Path.GetFileNameWithoutExtension(filepath);
                firstSave = false;
                dirty = false;
                currentFilePath = filepath;
            }

            catch (Exception ex)
            {
                MessageBoxResult result = MessageBox.Show("The file you tried to load failed to open. Make sure the file is valid.\n\n" +
                    "If that doesn't work, try loading a different sketch.", "Oh Noes!", MessageBoxButton.OK);
                menuNewSketch_Click(sender, e);
#if DEBUG
                //throw ex;
#endif
            }

            CurrentStatus = Status.Idle;
            EndWait();
        }

        private void menuSaveSketch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                successfullySave();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to save file. Error was: " + ex.Message);
#if DEBUG
                throw ex;
#endif
            }

        }

        private void menuSaveSketchAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                successfullyPromptAndSave();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to save file. Error was: " + ex.Message);
#if DEBUG
                throw ex;
#endif
            }
        }

        /// <summary>
        /// Saves the sketch -- if the sketch already has a current filepath, saves to that path.
        /// Otherwise, prompts the user to choose a filepath.
        /// </summary>
        /// <returns>true iff the file was saved</returns>
        private bool successfullySave()
        {
            if (!firstSave)
            {
                if (currentFilePath == null)
                    throw new Exception("Not first save, but currentFilepath was null!");
                saveProjectFiles(currentFilePath);
                return true;
            }
            else
                return successfullyPromptAndSave();
        }

        /// <summary>
        /// Prompts the user to choose a filepath and saves to that path. 
        /// </summary>
        /// <returns>true iff a path was chosen</returns>
        private bool successfullyPromptAndSave()
        {
            string filepath = getSaveFilepath("LogiSketch XML Files (*.xml)|*.xml", FilenameConstants.DefaultXMLExtension);
            if (filepath == null)
                return false;
            saveProjectFiles(filepath);
            pastSavedSketches.Add(filepath); // Record the list of all LogiSketch files.
            currentFilePath = filepath;
            return true;
        }

        /// <summary>
        /// Prompts the user to choose a filepath and saves the LogiSim exported file to that location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuExportToLogiSim_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filepath = getSaveFilepath("LogiSim (*.circ)|*.circ", FilenameConstants.DefaultLogisimExtension);
                if (filepath == null) return;
                saveLogisim(filepath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to export file. Exception was: " + ex.Message);
#if DEBUG
                throw ex;
#endif
            }
        }

        /// <summary>
        /// Brings up a save dialog and prompts user to enter a file name.
        /// </summary>
        /// <param name="extensionName">Example: "LogiSim (*.circ)|*.circ"</param>
        /// <param name="extension">Example: ".circ"</param>
        /// <returns>null if we didn't get a filename successfully</returns>
        private string getSaveFilepath(string filter, string extension)
        {
            string filepath = null;

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.AddExtension = true;
            saveFileDialog.RestoreDirectory = false;
            saveFileDialog.Filter = filter;

            if (saveFileDialog.ShowDialog() == true)
            {
                string dialogPath = saveFileDialog.FileName;
                if (System.IO.Path.GetExtension(dialogPath) != extension)
                {
                    MessageBox.Show(this, "Filename is not valid. Must be saved as " + extension + ".", "Invalid");
                }
                else
                {
                    // Make sure directory exists.
                    if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(dialogPath)))
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dialogPath));

                    filepath = dialogPath;
                }
            }
            return filepath;
        }

        /// <summary>
        /// Brings up a load dialog and prompts user to choose a sketch (.xml).
        /// </summary>
        /// <returns>null if we didn't get a filename</returns>
        private string getLoadFilepath()
        {
            string filepath = null;

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            openFileDialog.Title = "Load a Sketch";
            openFileDialog.Filter = "LogiSketch XML (*.xml)|*.xml";
            openFileDialog.RestoreDirectory = false;

            if (openFileDialog.ShowDialog() == true)
            {
                if (verifyLoadValid(openFileDialog.FileName))
                    filepath = openFileDialog.FileName;
                else
                    MessageBox.Show(this, "Could not open file. Check that the extension is .xml and the filepath exists.", "Invalid");
            }
            return filepath;
        }

        /// <summary>
        /// Checks that the file path exists and has a valid .xml extension.
        /// </summary>
        private bool verifyLoadValid(string filepath)
        {
            return System.IO.File.Exists(filepath) && 
                (System.IO.Path.GetExtension(filepath) == FilenameConstants.DefaultXMLExtension);
        }

        #region Unused Menu Options

        /// <summary>
        /// Opens a project using the project file manager.
        /// </summary>
        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            //projectManagerForm.LaunchProjectManager(ProjectManagerForm.ProjectManagerContext.Open);
        }


        /// <summary>
        /// Allows the user to edit the current project files with the project file manager
        /// </summary>
        private void menuRenameProject_Click(object sender, RoutedEventArgs e)
        {
            //projectManagerForm.LaunchProjectManager(ProjectManagerForm.ProjectManagerContext.Edit);
        }


        /// <summary>
        /// Saves the current project using the project file manager
        /// </summary>
        private void menuSave_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (firstSave || !projectManagerForm.ValidateFilepaths(false) ||
                !projectManagerForm.ValidateFilesExist(false))
            {
                bool? result =
                    projectManagerForm.LaunchProjectManager(ProjectManagerForm.ProjectManagerContext.Save);

                if (result != true)
                    return;
            }
            else
            {
                // Write out files
                saveProjectFiles();
            }
         */
        }



        private void menuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            //projectManagerForm.LaunchProjectManager(ProjectManagerForm.ProjectManagerContext.Save);
        }



        private void menuConnect_Click(object sender, RoutedEventArgs e)
        {
            /*System.Windows.Forms.FolderBrowserDialog sourceFolderDialog= new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }

            System.Windows.Forms.FolderBrowserDialog targetFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            targetFolderDialog.Description = "Choose the target directory";

            if (targetFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(targetFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.labelData(sourceFolderDialog.SelectedPath, targetFolderDialog.SelectedPath);*/
        }

        private void menuClassify_Click(object sender, RoutedEventArgs e)
        {
            /*System.Windows.Forms.FolderBrowserDialog sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }

            System.Windows.Forms.FolderBrowserDialog targetFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            targetFolderDialog.Description = "Choose the target directory";

            if (targetFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(targetFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.classifyData(sourceFolderDialog.SelectedPath, targetFolderDialog.SelectedPath);*/
        }

        private void menuTestGrouper_Click(object sender, RoutedEventArgs e)
        {
            /* System.Windows.Forms.FolderBrowserDialog sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.testGrouper(sourceFolderDialog.SelectedPath); */
        }

        private void menuTestRecog_Click(object sender, RoutedEventArgs e)
        {
            /*System.Windows.Forms.FolderBrowserDialog sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.testRecognizer(sourceFolderDialog.SelectedPath);*/
        }

        private void menuTestClassifier_Click(object sender, RoutedEventArgs e)
        {
            /*System.Windows.Forms.FolderBrowserDialog sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.testClassifier(sourceFolderDialog.SelectedPath);*/
        }

        private void menuTestOverall_Click(object sender, RoutedEventArgs e)
        {
            /* System.Windows.Forms.FolderBrowserDialog sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.testOverallRecognition(sourceFolderDialog.SelectedPath);*/
        }

        private void menuTestRefiner_Click(object sender, RoutedEventArgs e)
        {
            /*System.Windows.Forms.FolderBrowserDialog sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.testRefiner(sourceFolderDialog.SelectedPath); */
        }

        private void menuProccess_Click(object sender, RoutedEventArgs e)
        {
            /*System.Windows.Forms.FolderBrowserDialog sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }

            System.Windows.Forms.FolderBrowserDialog targetFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            targetFolderDialog.Description = "Choose the target directory";

            if (targetFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(targetFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.proccessData(sourceFolderDialog.SelectedPath, targetFolderDialog.SelectedPath); */
        }

        private void menuClean_Click(object sender, EventArgs e)
        {
            /* System.Windows.Forms.FolderBrowserDialog sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            sourceFolderDialog.Description = "Choose the source directory";

            if (sourceFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists(sourceFolderDialog.SelectedPath))
                {
                    MessageBox.Show("Error: target folder does not exist");
                }
            }
            recognitionManager.cleanData(sourceFolderDialog.SelectedPath); */
        }

        #endregion

        #endregion

        #region Toolbar Button Callbacks

        /// <summary>
        /// Triggers CircuitParser Recognition on circuit panel
        /// </summary>
        private void recognizeCircuitButton_Click(object sender, RoutedEventArgs e)
        {
            recognizeCircuit(true);
            displayManager.ColorStrokesByType();
        }

        /// <summary>
        /// Triggers label recognition on the panel.
        /// </summary
        private void recognizeInkButton_Click(object sender, RoutedEventArgs e)
        {
            recognizeSketch();
            displayManager.ColorStrokesByType();
        }

        private void refineButton_Click(object sender, RoutedEventArgs e)
        {
            recognitionManager.RefineSketch(circuitPanel.InkSketch.FeatureSketch);
            displayManager.ColorStrokesByType();
        }

        /// <summary>
        /// Trigers single-stroke classification on the panel.
        /// </summary>
        private void classifyButton_Click(object sender, RoutedEventArgs e)
        {
            classifyStrokes();
            displayManager.displayClassification();
        }

        /// <summary>
        /// Triggers the connection recognition on the panel.
        /// </summary>
        private void connectShapesButton_Click(object sender, RoutedEventArgs e)
        {
            connectSketch();
            displayManager.ColorStrokesByType();
        }

        /// <summary>
        /// Triggers stroke grouping on the panel.
        /// </summary>
        private void groupStrokesButton_Click(object sender, RoutedEventArgs e)
        {
            groupStrokes();
            displayManager.displayGroups();
        }

        #region Tools

        /// <summary>
        /// Calls appropriate gesture event
        /// </summary>
        private void GestureHandler(object sender, InkCanvasGestureEventArgs e)
        {

            // recognize the stroke as some gestures, if any
            System.Collections.ObjectModel.ReadOnlyCollection<GestureRecognitionResult> gestureResults = e.GetGestureRecognitionResults();

            // Perform various actions depending on the recognized gesture
            switch (gestureResults[0].ApplicationGesture)
            {
                case ApplicationGesture.Curlicue: // Switch between ink and selection modes
                    Curlicue(sender, e);
                    System.Console.WriteLine("Curlicue");
                    break;
                case ApplicationGesture.ScratchOut: // Delete scratched out strokes
                    Scratchout(sender, e);
                    System.Console.WriteLine("Scratchout");
                    break;
                case ApplicationGesture.Exclamation:   // Draw a triangle on the canvas
                    Exclamation(sender, e);
                    System.Console.WriteLine("Exclamation");
                    break;
                case ApplicationGesture.Triangle:   // Draw a triangle on the canvas
                    System.Console.WriteLine("Triangle");
                    selectionTool.SetEditWidget(new System.Windows.Point(e.Strokes.GetBounds().X, e.Strokes.GetBounds().Y));
                    break;
                default:    // Do nothing if no desired gesture is recognized
                    break;
            }
        }


        /// <summary> 
        /// Handles switching between Ink and Edit mode when a curlicue is drawn
        /// </summary>
        private void Curlicue(object sender, InkCanvasGestureEventArgs e)
        {
            // For now do nothing
        }

        /// <summary>
        /// Handles deleting objects that are scratched out
        /// </summary>
        private void Scratchout(object sender, InkCanvasGestureEventArgs e)
        {
            // Don't know if this works
            // Deletes any strokes that are 50% contained within the scribble's bounding box
            // Probably not ideal method
            Rect rect = e.Strokes.GetBounds();

            StrokeCollection strokesHitTest = circuitPanel.InkCanvas.Strokes.HitTest(rect, 50);
            circuitPanel.InkCanvas.Strokes.Remove(strokesHitTest);
        }

        /// <summary>
        /// Brings up the edit menu when an exclamation mark is drawn
        /// </summary>
        private void Exclamation(object sender, InkCanvasGestureEventArgs e)
        {
            System.Windows.Rect bounds = e.Strokes.GetBounds();
            selectionTool.SetEditWidget(new System.Windows.Point(bounds.X, bounds.Y));
        }

        #endregion

        #endregion

        #region Edit Menu Callbacks

        /// <summary>
        /// If nothing is selected, cut and copy should not be enabled on the edit menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            if (circuitPanel.InkCanvas.GetSelectedStrokes().Count == 0)
            {
                menuCut.IsEnabled = false;
                menuCopy.IsEnabled = false;
                menuDelete.IsEnabled = false;
            }
            else
            {
                menuCut.IsEnabled = true;
                menuCopy.IsEnabled = true;
                menuDelete.IsEnabled = true;
            }
        }

        /// <summary>
        /// Copies selection of Sketch in focus
        /// </summary>
        private void menuCopy_Click(object sender, RoutedEventArgs e)
        {
            circuitPanel.CopyStrokes();

            // Keep selection widget open even after copying
            // selectionTool.RemoveSelectWidget();
        }

        /// <summary>
        /// Cuts selection of Sketch in focus
        /// </summary>
        private void menuCut_Click(object sender, RoutedEventArgs e)
        {
            circuitPanel.CutStrokes();

            selectionTool.RemoveSelectWidget();
        }

        /// <summary>
        /// Pastes to selection of Sketch in focus
        /// </summary>
        private void menuPaste_Click(object sender, RoutedEventArgs e)
        {
            circuitPanel.PasteStrokes(new System.Windows.Point(-1, -1));

            selectionTool.SetSelectWidget();
        }

        /// <summary>
        /// Deletes selection of Sketch in focus
        /// </summary>
        private void menuDelete_Click(object sender, RoutedEventArgs e)
        {
            circuitPanel.DeleteStrokes();

            selectionTool.RemoveSelectWidget();
        }

        /// <summary>
        /// Undoes the last command
        /// </summary>
        private void menuUndo_Click(object sender, RoutedEventArgs e)
        {
            circuitPanel.Undo();

            if (circuitPanel.InkCanvas.GetSelectedStrokes().Count > 0)
                selectionTool.SetSelectWidget();
        }

        /// <summary>
        /// Redoes the last command
        /// </summary>
        private void menuRedo_Click(object sender, EventArgs e)
        {
            circuitPanel.Redo();

            if (circuitPanel.InkCanvas.GetSelectedStrokes().Count > 0)
                selectionTool.SetSelectWidget();
        }

        /// <summary>
        /// Selects all of the strokes
        /// </summary>
        private void menuSelectAll_Click(object sender, RoutedEventArgs e)
        {
            // Don't select nothing
            if (circuitPanel.InkCanvas.Strokes.Count == 0)
                return;
            circuitPanel.SelectAllStrokes();
            selectionTool.SetSelectWidget();
        }

        /// <summary>
        /// Deletes all of the panels' strokes
        /// </summary>
        private void menuDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            circuitPanel.DeleteAllStrokes();
            selectionTool.RemoveSelectWidget();
        }

        private void menuZoomIn_Click(object sender, RoutedEventArgs e)
        {
            Wait();
            circuitPanel.ZoomIn();
            EndWait();
         }

        private void menuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            Wait();
            circuitPanel.ZoomOut();
            EndWait();
        }


        private void menuZoomToFit_Click(object sender, RoutedEventArgs e)
        {
            Wait();
            circuitPanel.ZoomToFit();
            EndWait();
        }

        /// <summary>
        /// When clicked, takes us out of simulation mode and back into edit mode.
        /// </summary>
        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton editButton = (ToggleButton)sender;

            editButton.IsChecked = false;
            SimToEdit();
        }



        #endregion

        #region View Menu Callbacks

        private void menuShowClassification_Click(object sender, RoutedEventArgs e)
        {
            displayManager.displayClassification();
        }

        private void menuGroups_Click(object sender, RoutedEventArgs e)
        {
            displayManager.displayGroups();
        }

        private void menuLabels_Click(object sender, RoutedEventArgs e)
        {
            displayManager.ColorStrokesByType();
        }

        private void menuAdjacencyMesh_Click(object sender, RoutedEventArgs e)
        {
            displayManager.displayAdjacency();
        }

        private void menuValidity_Click(object sender, RoutedEventArgs e)
        {
            displayManager.displayValidity(recognitionManager.TestValidity(circuitPanel.InkSketch.FeatureSketch));
        }

        private void menuPoints_Click(object sender, RoutedEventArgs e)
        {
            displayManager.showPoints();
        }

        private void menuProcess_Click(object sender, RoutedEventArgs e)
        {
            // We don't have anything here yet...
        }

        #endregion

        #region Feedback Menu Callbacks

        private void labelAfterRec_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.RecognitionTooltips = (bool)labelsAfterRec.IsChecked;
        }

        /// <summary>
        /// Called when you check or uncheck this box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shapesAfterRec_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.GatesOnRec = (bool)shapesAfterRec.IsChecked;
        }

        /// <summary>
        /// Called when you check or uncheck this box. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void highlightAfterRec_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.ErrorHighlighting = (bool)HighlightAfterRec.IsChecked;
        }

        /// <summary>
        /// Called when you check or uncheck this box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shapesAfterLabel_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.GatesOnLabel = (bool)shapesAfterLabel.IsChecked;
        }


        private void shapesOnHover_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
            {
                if (displayManager.AllowRotateGates)
                    shapesOnHover.IsChecked = true;
                displayManager.GatesOnHovering = (bool)shapesOnHover.IsChecked;
            }
        }

        /// <summary>
        /// Called when you check or uncheck this box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shapesWhileDrawing_Click(object sender, RoutedEventArgs e)
        {
            recognitionManager.FinishLoading();
            
            onFlyTurnedOn = (bool)shapesWhileDrawing.IsChecked;
        }

        /// <summary>
        /// Called when you check or uncheck this box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void colorShapes_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.StrokeColoring = (bool)colorShapes.IsChecked;
        }

        /// <summary>
        /// Called when you check or uncheck this box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void highlightEndpoints_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.EndpointHighlighting = (bool)highlightEndpoints.IsChecked;
        }

        /// <summary>
        /// Called when you check or uncheck this box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void meshHighlight_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.MeshHighlighting = (bool)meshHighlight.IsChecked;
        }

        /// <summary>
        /// Called when you check or uncheck this box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tooltipsOn_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.StylusTooltips = (bool)tooltipsOn.IsChecked;
        }

        /// <summary>
        /// Called when you check or uncheck this box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void displayInternalEndpoints_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
                displayManager.ShowInternalEndpoints = (bool)displayInternalEndpoints.IsChecked;
        }

        /// <summary>
        /// Turns on anf off allowing gate rotation.  If hover gates are turned off and you turn this on, the hover gates will be turned on.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void allowRotateGates_Click(object sender, RoutedEventArgs e)
        {
            if (displayManager != null)
            {
                displayManager.AllowRotateGates = (bool)allowRotateGates.IsChecked;
                if (displayManager.AllowRotateGates)
                    shapesOnHover.IsChecked = true;
            }
        }

        #endregion

        #region Endpoint and Gate Rotation Callbacks
        /// <summary>
        /// Function for updating the wires when an endpoint is snapped
        /// </summary>
        /// <param name="movedPoint"></param>
        /// <returns></returns>
        public bool endPointChange(Sketch.EndPoint oldLocation, Point newLocation, Stroke attachedStroke, Sketch.Shape shapeAtNewLoc)
        {
            SketchPanelLib.CommandList.EndPointMoveCmd mover = new SketchPanelLib.CommandList.EndPointMoveCmd(circuitPanel.InkSketch, 
                oldLocation, newLocation, attachedStroke, shapeAtNewLoc);
            
            mover.Regroup += new SketchPanelLib.CommandList.EndpointMoveHandlerRegroup(rerecognizeShapesAndReconnect);
            mover.Handle += new SketchPanelLib.CommandList.EndpointMoveHandler(endpointMoveCallback);
            commandManager.ExecuteCommand(mover);
            return true;
        }

        /// <summary>
        /// Function for rotating the shape.
        /// </summary>
        public void gateRotated(Sketch.Shape rotatedShape, double newOrientation)
        {
            SketchPanelLib.CommandList.ShapeRotateCmd rotater =
                new SketchPanelLib.CommandList.ShapeRotateCmd(rotatedShape, newOrientation);
            rotater.Rerecognize += new SketchPanelLib.CommandList.RerecognizeShapes(rerecognizeShapesAndReconnect);
            commandManager.ExecuteCommand(rotater);
        }

        private void endpointMoveCallback(Sketch.Shape shape)
        {
            InkChanged();
        }

        #endregion

        #region Recognition on the Fly Callback

        private void OnFlyRecognition()
        {
            // Create a pipeline comprising the first three recognition stages and run it
            RecognitionManager.RecognitionPipeline pipe = RecognitionManager.RecognitionPipeline.GetOnFlyPipeline();
            pipe.process(circuitPanel.InkSketch.FeatureSketch);
            displayManager.OnTheFlyFeedback();

            circuitPanel.UseCustomCursor = false;
        }

        #endregion

        #region Recognition Button Callbacks

        /// <summary>
        /// Main recognition function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recognizeButton_Click(object sender, RoutedEventArgs e)
        {

            if (CurrentStatus == Status.Recognizing)
                return;

            // Clear the undo/redo lists and update buttons
            commandManager.ClearLists();
            refreshUndoRedoPaste();
            if (UserStudy && trialNum == 0) SaveUserStudyFile();

            // The user should not be able to edit during recognition
            selectionTool.UnsubscribeFromPanel();
            modeIndicator.Visibility = Visibility.Hidden;

            Wait();
            
            // Clean up old display elements
            displayManager.Pause();
            if (selectionTool.widgetsShowing)
                selectionTool.HideAllWidgets();
            if (selectionTool.selectionActive)
                selectionTool.RemoveMenu();

            // Check if it is safe to recognize
#if DEBUG
            int x = 0;
            if (!circuitPanel.InkSketch.FeatureSketch.hasConsistentSubstrokes())
                throw new Exception("Sketch and/or FeatureSketch are inconsistent.");
#endif

            // Start recognizing
            CurrentStatus = Status.Recognizing;
            UpdateProgress("Starting recognition", 0);
            
            var pipeline = recognitionManager.DefaultPipeline;
            RecognitionManager.SketchProcessor processor = pipeline.getProcessor(circuitPanel.InkSketch.FeatureSketch);

            circuitPanel.InkSketch.project.resetUsedCircuits();
            queueRunProcessor(processor);

            recognizeButton.IsChecked = false;
            dirty = true;
        }

        /// <summary>
        /// Inform the dispatcher that we want to run the next step of recognition.
        /// </summary>
        /// <param name="processor"></param>
        private void queueRunProcessor(RecognitionManager.SketchProcessor processor)
        {
            recognizeButton.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.SystemIdle,
                    new doNextStep(delegate() { runProcessor(processor); }));
        }

        /// <summary>
        /// Runs a single stage of the recognition pipeline. Do not call this directly; instead, use
        /// queueRunProcessor.
        /// </summary>
        /// <param name="processor"></param>
        private void runProcessor(RecognitionManager.SketchProcessor processor)
        {
            if (processor.isFinished())
            {
                // Update some things
                circuitPanel.InvalidateVisual();
                //circuitPanel.InkSketch.project.resetUsedCircuits();

                // Prepare for simulation. This will also hide the
                // progress window.
                recognizeCircuit(true);

                // We are fully recognized!
                circuitPanel.Recognized = true;
            }
            else
            {
                UpdateProgress(processor.NextStep.ProgressString, (int)(processor.Progress * 100));
                processor.executeNextStep();
                queueRunProcessor(processor);
            }
        }

        /// <summary>
        /// Re-recognizes each shape in the given list, recognizes the sketch, and updates the feedback.
        /// </summary>
        /// <param name="shapesToBeGrouped"></param>
        private void rerecognizeShapesAndReconnect(List<Sketch.Shape> shapesToBeGrouped)
        {
            foreach (Sketch.Shape shapeToGroup in shapesToBeGrouped)
            {
                recognitionManager.RerecognizeGroup(shapeToGroup, circuitPanel.InkSketch.FeatureSketch);
                shapeToGroup.AlreadyLabeled = true;
                // I think the following is true
                shapeToGroup.UserLabeled = true;
            }

            InkChanged();
            displayManager.AlertFeedback(simulationManager.CircuitParser, shapesToBeGrouped);
            circuitPanel.EnableDrawing();
        }



        /// <summary>
        /// Tells the display manager when it should start and stop doing interactive feedback
        /// </summary>
        /// <param name="isSelecting"></param>
        private void selectionTool_Selecting(bool isSelecting)
        {
            if (isSelecting)
                displayManager.Pause();
            else
                displayManager.Unpause();
        }

        /// <summary>
        /// Event handler called when the simulation button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delayedSimButton_Click(object sender, RoutedEventArgs e)
        {
            if (simulationManager.Valid)
                EditToSim();
            else // Why was this button even clickable?
                throw new Exception("Simulation button was clickable when circuit wasn't valid!");

            if (sender == simulationButton)
                simulationButton.IsChecked = false;
        }

        #endregion

        #region Subcircuit Callbacks

        /// <summary>
        /// when a sub circuit is removed, the simulation manager shouldn't keep holding on to the 
        /// name and circuit component associated with it. This is passed down to sketch when removing
        /// shapes
        /// </summary>
        /// <param name="shapeName">name of the removed sub circuit</param>
        private void removeSubCircuit(Sketch.Shape shape)
        {
            circuitPanel.InkSketch.project.subCircuitRemoved(shape.SubCircuitNumber);
            displayManager.removeIO(shape);
            simulationManager.subCircuitShapetoElement = 
                Data.Utils.filterKeys(simulationManager.subCircuitShapetoElement, a => { return a != shape; });
        }

        /// <summary>
        /// From a shape, creates the circuit element of the sub circuit if that's what the shape is.
        /// Adds the association to simulation manager to keep track of for creating the circuit.
        /// </summary>
        /// <param name="subCircuitShape"></param>
        private void addSubCircuit(Sketch.Shape subCircuitShape)
        {
            if (subCircuitShape.Type != LogicDomain.SUBCIRCUIT) return;

            Sketch.Project subSketch = circuitPanel.InkSketch.project.subProjectLookup[subCircuitShape.SubCircuitNumber];
            CircuitSimLib.SubCircuit subCircuit = new CircuitSimLib.SubCircuit(subCircuitShape.Name, subCircuitShape.Bounds,
                                        subCircuitShape, subSketch.inputs, subSketch.outputs, subSketch.behavior);

            //update the dictionary in simulation manager correctly and safely
            if (simulationManager.subCircuitShapetoElement.ContainsKey(subCircuitShape))
                simulationManager.subCircuitShapetoElement[subCircuitShape] = subCircuit;
            else
                simulationManager.subCircuitShapetoElement.Add(subCircuitShape, subCircuit);

            circuitPanel.InkSketch.project.subCiruitAdded(subCircuitShape.SubCircuitNumber);

            // Update the display Manager for Ghost gate io stuff for subcircuits
            KeyValuePair<List<string>, List<string>> ioAdder = new KeyValuePair<List<string>, List<string>>(subCircuit.inputs, subCircuit.outputs);
            displayManager.addIO(subCircuitShape, ioAdder);
        }

        /// <summary>
        /// Listen to the sketch so we know when a subcircuit has been added (when a shape
        /// tagged with a subcircuit number is added).
        /// </summary>
        private bool subscribeToSubcircuitChanges(Sketch.Sketch sketch)
        {
            sketch.subCircuitAdded += new Sketch.AddSubCircuitToSimulation(addSubCircuit);
            sketch.subCircuitRemoved += new Sketch.RemoveSubCircuitFromSimulation(removeSubCircuit);
            return true;
        }
        
        /// <summary>
        /// Tell the sketch to add in a subcircuit. Handler passed to the selection Manager, from embed widget
        /// </summary>
        /// <param name="point"></param>
        private void selectionTool_LoadSub(Point point)
        {
            string filepath = getLoadFilepath();
            if (filepath == null)
                return;
            loadSubFromEmbed(filepath, point);
        }

        /// <summary>
        /// Called from the callback in embed widget to actually create the shape that the subcircuit is
        /// </summary>
        /// <param name="point"></param>
        private void loadSubFromEmbed(string filepath, Point point)
        {
            Wait();

            ReadXML readXml = circuitPanel.LoadXml(filepath);
            Sketch.Project addedProject = circuitPanel.InkSketch.createProject(readXml);

            // Keep track of if it's in the list of things loaded so we know if we should add it to the embed list
            bool addToList = false;
            int subIndex;
            if (!circuitPanel.InkSketch.alreadyLoaded.Contains(filepath))
            {
                addToList = true;
                subIndex = circuitPanel.InkSketch.newSubSketch(addedProject, filepath);
            }
            else
                subIndex = circuitPanel.InkSketch.alreadyLoaded.IndexOf(filepath);

            if (!circuitPanel.InkSketch.Sketch.tagToName.ContainsKey(subIndex))
                circuitPanel.InkSketch.Sketch.tagToName.Add(subIndex, System.IO.Path.GetFileNameWithoutExtension(filepath));

            EditMenu.CommandList.AddSubCircuitCmd subCircuitAdder = new EditMenu.CommandList.AddSubCircuitCmd(ref circuitPanel, point, subIndex, addToList);
            subCircuitAdder.embedCallback += new EditMenu.CommandList.EmbedCallback(subCircuitAdder_embedCallback);
            commandManager.ExecuteCommand(subCircuitAdder);

            EndWait();
        }

        /// <summary>
        /// Callback for the embed widget to add things to the embed list from a shape
        /// </summary>
        /// <param name="associatedShape"></param>
        public void subCircuitAdder_embedCallback(Sketch.Shape associatedShape, bool addToList)
        {
            //Add to embed panel if applicable
            if (addToList)
                AddToList(associatedShape);
        }

        #endregion

        #region Rerec and Resim callbacks

        /// <summary>
        /// Handles when ink in the panel has been changed
        /// </summary>
        private void InkChanged()
        {
            // If we have on the fly, recognize things now.
            if (onFlyTurnedOn)
                OnFlyRecognition();

            recognitionManager.ConnectSketch(circuitPanel.InkSketch.FeatureSketch);

            circuitPanel.UseCustomCursor = false;

            if (circuitPanel.Sketch.Substrokes.Length == 0)
            {
                NeedsRecognition();
                return;
            }

            foreach (Sketch.Substroke sub in circuitPanel.Sketch.Substrokes)
            {
                if (sub.ParentShape == null || !sub.ParentShape.AlreadyLabeled)
                {
                    NeedsRecognition();
                    return;
                }
                else
                    circuitPanel.Recognized = true;
            }

            RecognitionChanged();
        }

        /// <summary>
        /// Handles when ink in the panel has been re-labeled or re-grouped
        /// </summary>
        private void RecognitionChanged()
        {
            needsResim = true;
            needsRerec = false;
            dirty = true;

            recognizeCircuit(false);

            displayManager.AlertFeedback(simulationManager.CircuitParser, false);

            circuitPanel.UseCustomCursor = false;
        }

        /// <summary>
        /// Handles setting the sim button and bools for when the sketch needs recognition
        /// </summary>
        private void NeedsRecognition()
        {
            needsRerec = true;
            needsResim = true;
            dirty = true;
            circuitPanel.Circuit = null;

            displayManager.AlertFeedback(simulationManager.CircuitParser, false);

            simulationButton.Visibility = System.Windows.Visibility.Hidden;
        }

        #endregion

        #region Simulation-Editing Switches
        /// <summary>
        /// Updates the recognition and simulation buttons upon simulation.
        /// </summary>
        private void EditToSim()
        {
            if (simulationManager.CircuitParser.ParseErrors.Count > 0 || simulationManager.Subscribed)
                return;

            CurrentStatus = Status.Simulating;
            modeIndicator.Visibility = System.Windows.Visibility.Hidden;

            // Clear the command stack and update the edit menu
            commandManager.ClearLists();
            refreshUndoRedoPaste();

            simulationManager.SubscribeToPanel();
            selectionTool.UnsubscribeFromPanel();
            displayManager.Pause();

            // Display our simulation toggles
            cleanToggle.Visibility = System.Windows.Visibility.Visible;
            truthTableToggle.Visibility = System.Windows.Visibility.Visible;
            embedButton.Visibility = System.Windows.Visibility.Hidden;
            subCircuitPanel.Visibility = System.Windows.Visibility.Hidden;

            // Set editing mode
            circuitPanel.DisableDrawing();

            // Disable editing from the edit menu
            this.disableEditing();

            // Convert simulation button to edit button
            recognizeButton.IsEnabled = false;
            simulationButton.Content = "Edit";
            simulationButton.Checked -= new RoutedEventHandler(delayedSimButton_Click);
            simulationButton.Checked += new RoutedEventHandler(editButton_Click);

            //Can't simulate oscillating circuits, and we shouldn't try to because this causes
            // errors in truthtable
            if (circuitPanel.Circuit.IsOscillating)
                SimToEdit();
        }

        /// <summary>
        /// Takes us out of simulation mode and puts us in edit mode
        /// </summary>
        private void SimToEdit()
        {
            // If you're not simulating, why are you here?
            if (!simulationManager.Subscribed) return;

            CurrentStatus = Status.Idle;

            modeIndicator.Visibility = System.Windows.Visibility.Visible;

            // Remove our simulation toggles
            cleanToggle.Visibility = System.Windows.Visibility.Hidden;
            truthTableToggle.Visibility = System.Windows.Visibility.Hidden;

            embedButton.Visibility = System.Windows.Visibility.Visible;

            cleanToggle.IsChecked = false;
            truthTableToggle.IsChecked = false;

            simulationManager.UnsubscribeFromPanel();
            selectionTool.SubscribeToPanel();
            displayManager.Unpause();

            // Convert edit button to simulate button
            recognizeButton.IsEnabled = true;
            simulationButton.Content = "Simulate";
            simulationButton.Checked -= new RoutedEventHandler(editButton_Click);
            simulationButton.Checked += new RoutedEventHandler(delayedSimButton_Click);
            if (needsRerec)
                simulationButton.Visibility = System.Windows.Visibility.Hidden;

            // Selectively enable editing
            this.enableEditing();

            circuitPanel.EnableDrawing();
            displayManager.AlertFeedback(simulationManager.CircuitParser, false);
        }

        #endregion

        #endregion

        #region Helper functions

        /// <summary>
        /// Event for when a sketch is loaded. Looks to see if it has been recognized
        /// </summary>
        private void circuitPanel_SketchFileLoaded()
        {
            //recognitionManager.Featuresketch = circuitPanel.InkSketch.FeatureSketch;
            recognitionManager.ConnectSketch(circuitPanel.InkSketch.FeatureSketch);
            if (circuitPanel.Recognized)
            {
                needsRerec = circuitPanel.Recognized;
                RecognitionChanged();
            }
        }

        /// <summary>
        /// Since MenuItems don't callback till sub items are called, this function is a
        /// helper that updates the Edit Menu values so only sensical things are clickable.
        /// They must have a non void return type to pass.
        /// </summary>
        private bool commandManager_updateEditMenu()
        {
            if (selectionTool.widgetsShowing)
                selectionTool.HideAllWidgets();
            return refreshUndoRedoPaste();
        }

        private bool commandManager_moveUpdate()
        {
            selectionTool.closeAllButSelect();
            return refreshUndoRedoPaste();
        }

        /// <summary>
        /// Selectively enables the Undo, Redo and Paste options
        /// </summary>
        /// <returns></returns>
        private bool refreshUndoRedoPaste()
        {
            menuUndo.IsEnabled = commandManager.UndoValid;
            menuRedo.IsEnabled = commandManager.RedoValid;
            menuPaste.IsEnabled = !commandManager.ClipboardEmpty;
            return true;
        }

        /// <summary>
        /// Disables all the editing commands on the dropdown Edit Menu (since you shouldn't be able to edit in Simulation mode)
        /// </summary>
        /// <returns></returns>
        private bool disableEditing()
        {
            menuCut.IsEnabled = false;
            menuCopy.IsEnabled = false;
            menuPaste.IsEnabled = false;
            menuDelete.IsEnabled = false;
            menuDeleteAll.IsEnabled = false;
            menuSelectAll.IsEnabled = false; 
            menuUndo.IsEnabled = false;
            menuRedo.IsEnabled = false;
            menuPaste.IsEnabled = false;
            return true;
        }

        /// <summary>
        /// Enables delete all/select all on the dropdown Edit Menu (when returning to Edit mode from Simulation mode)
        /// </summary>
        /// <returns></returns>
        private bool enableEditing()
        {
            // Won't be enabled at first
            menuCut.IsEnabled = false;
            menuCopy.IsEnabled = false;
            menuPaste.IsEnabled = false;
            menuDelete.IsEnabled = false;

            // Should always be enabled (except in Simulation mode)
            menuDeleteAll.IsEnabled = true;
            menuSelectAll.IsEnabled = true;
            return refreshUndoRedoPaste();
        }

        /// <summary>
        /// Displays the wait cursor and disallows drawing.
        /// To end this, call EndWait().
        /// </summary>
        private void Wait()
        {
            circuitPanel.UseCustomCursor = true;
            circuitPanel.Cursor = Cursors.Wait;
            circuitPanel.DisableDrawing();
        }

        /// <summary>
        /// Returns cursor to normal and reallows drawing.
        /// Generally called after Wait()
        /// </summary>
        private void EndWait()
        {
            displayManager.SubscribeToPanel();
            circuitPanel.UseCustomCursor = false;
            circuitPanel.EnableDrawing();
        }

        #endregion

        #region Recognition Steps

        /// <summary>
        /// Classifies the sketch's strokes
        /// </summary>
        private void classifyStrokes()
        {
            recognitionManager.ClassifySingleStrokes(circuitPanel.InkSketch.FeatureSketch);

            if (debug)           
                displayManager.ColorStrokesByType();
            circuitPanel.InvalidateVisual();

            // If we're currently trying to do the entire recognition process,
            // continue on to the next step.
            if (CurrentStatus == Status.Recognizing)
            {
                UpdateProgress("Grouping strokes...", 20);

                recognizeButton.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.SystemIdle,
                    new doNextStep(groupStrokes));
            }
        }

        /// <summary>
        /// Groups strokes into shapes.
        /// </summary>
        private void groupStrokes()
        {
            // since the gouping here seems to be adding back in the shapes,
            // the list of used circuits needs to be reset to not double count
            circuitPanel.InkSketch.project.resetUsedCircuits();
            recognitionManager.GroupStrokes(circuitPanel.InkSketch.FeatureSketch);

            if (debug)
            {
                displayManager.displayGroups();
                circuitPanel.InvalidateVisual();
            }

            // If we're currently trying to do the entire recognition process,
            // continue on to the next step.
            if (CurrentStatus == Status.Recognizing)
            {
                UpdateProgress("Recognizing ink...", 60);

                recognizeButton.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.SystemIdle,
                    new doNextStep(recognizeSketch));
            }
        }

        /// <summary>
        /// Recognizes shapes in the sketch.
        /// </summary>
        private void recognizeSketch()
        {
            recognitionManager.Recognize(circuitPanel.InkSketch.FeatureSketch);

            if (debug)
                displayManager.ColorStrokesByType();

            // No matter what, we'll want to start connecting shapes.
            UpdateProgress("Connecting shapes...", 80);

            recognizeButton.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.SystemIdle,
                    new doNextStep(connectSketch));
        }

        /// <summary>
        /// Connects substrokes in the sketch
        /// </summary>
        private void connectSketch()
        {
            recognitionManager.ConnectSketch(circuitPanel.InkSketch.FeatureSketch);

            // If we're currently trying to do the entire recognition process,
            // continue on to the next step.
            if (CurrentStatus == Status.Recognizing)
            {
                UpdateProgress("Refining recognition...", 90);

                recognizeButton.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.SystemIdle,
                    new doNextStep(refineRecognition));
            }
        }

        /// <summary>
        /// Tries to fix up mistakes in the sketch recognition
        /// </summary>
        private void refineRecognition()
        {
            recognitionManager.RefineSketch(circuitPanel.InkSketch.FeatureSketch);
           
            // If we're currently trying to do the entire recognition process,
            // continue on to the next step.
            if (CurrentStatus == Status.Recognizing)
            {
                UpdateProgress("Constructing circuit...", 95);
                recognizeCircuit(true);
            }

            circuitPanel.Recognized = true;
        }

        /// <summary>
        /// Triggers CircuitParser Recognition on circuit panel
        /// </summary>
        private void recognizeCircuit(bool showMessage)
        {
            recognitionManager.MakeShapeNamesUnique(circuitPanel.InkSketch.FeatureSketch);

            // Recognize the circuit if we need to 
            if (!UserStudy && (needsResim || (bool)HighlightAfterRec.IsChecked))
                simulationManager.recognizeCircuit();

            if (CurrentStatus == Status.Recognizing)
                UpdateProgress("Done.", 100, true);

            // Update information
            needsRerec = false;

            if (!UserStudy)
            {
                simulationButton.Visibility = System.Windows.Visibility.Visible;
                needsResim = !simulationManager.Valid;

                // Show the message if we have an invalid circuit
                if (needsResim && showMessage)
                    MessageBox.Show(this, "The recognized circuit has errors. Please fix these errors before hitting 'Simulate' to continue.", "Errors!");
                else if (simulationManager.CircuitParser.ParseErrors.Count > 0)
                {
                    // Disable the simulation button if we have parse errors
                    needsResim = true;

                    if (showMessage)
                    {
                        string text = simulationManager.CircuitParser.ParseErrors[0].Explanation;
                        MessageBox.Show(this, text, "Errors!");
                    }
                }
                // Selectively show simulation button
                simulationButton.IsEnabled = !needsResim;
            }

            // Make it so the user can edit again
            selectionTool.SubscribeToPanel();
            modeIndicator.Visibility = Visibility.Visible;

            displayManager.Unpause();

            // Update the display
            displayManager.AlertFeedback(simulationManager.CircuitParser, showMessage);
            CurrentStatus = Status.Idle;

            if (UserStudy) SaveUserStudyFile();
            EndWait();
        }

        #endregion

        #region Progress Window

        /// <summary>
        /// Makes and shows the window with the recognition progress bar
        /// </summary>
        private void bringUpProgressWindow()
        {
            progressWindow = new ProgressWindow.MainWindow();
            progressWindow.Show();
        }

        /// <summary>
        /// Closes the progress window
        /// </summary>
        private void closeProgressWindow()
        {
            if (progressWindow != null)
                progressWindow.Close();
            progressWindow = null;
        }

        /// <summary>
        /// Displays the given message and progress in the progress window, closes the window after if closeWindow is set to true.
        /// 
        /// Will bring up the progress window if it is null.
        /// </summary>
        /// <param name="message">The message the progress window should display</param>
        /// <param name="progress">The percentage the progress bar should be at</param>
        /// <param name="closeWindow">Set to true if we should close the window after displaying the message</param>
        private void UpdateProgress(string message, int progress, bool closeWindow = false)
        {
            if (progressWindow == null)
                bringUpProgressWindow();

            progressWindow.setText(message);
            progressWindow.setProgress(progress);

            if (closeWindow)
                closeProgressWindow();
        }

        #endregion

        #region Notes Window

        private void bringUpNotes(object sender, RoutedEventArgs e)
        {
            notesWindow = new NotesWindow.MainWindow(ref notesPanel);
            notesWindow.Show();
            notesWindow.Closed += new EventHandler(notesWindow_Closed);
        }

        private void closeNotes(object sender, RoutedEventArgs e)
        {
            if (notesWindow != null && notesWindow.IsEnabled)
            {
                notesWindow.Close();
                notesWindow.Closed -= new EventHandler(notesWindow_Closed);
            }
        }

        private void notesWindow_Closed(object sender, EventArgs e)
        {
            NoteExpander.IsExpanded = false;
        }

        private void subCircuitWindow_Closed(object sender, EventArgs e)
        {
            subCircuitWindow.Window_Closed(sender, e);
            subCircuitWindow.Closed -= new EventHandler(subCircuitWindow_Closed);
        }

        #endregion

        #region Practice Window

        private void bringUpPractice(object sender, RoutedEventArgs e)
        {
            Wait();

            practiceWindow = new PracticeWindow.MainWindow(ref practiceSketch, ref practiceRecognizer);
            practiceWindow.Show();
            practiceWindow.Closed += new EventHandler(practiceWindow_Closed);

            EndWait();
        }

        private void closePractice(object sender, RoutedEventArgs e)
        {
            if (practiceWindow != null && practiceWindow.IsEnabled)
            {
                practiceWindow.Close();
                practiceWindow.Closed -= new EventHandler(practiceWindow_Closed);
            }
        }

        private void practiceWindow_Closed(object sender, EventArgs e)
        {
            PracticeExpander.IsExpanded = false;
        }

        #endregion

        #region Template Window

        /// <summary>
        /// Handler for avtivating the window when the user clicks the expander
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bringUpTemplate(object sender, RoutedEventArgs e)
        {
            Wait();

            //Need to add the event to hovering to update the template when over a new shape
            circuitPanel.StylusInAirMove += new StylusEventHandler(updateTemplate);
            templateWindow = new TemplateWindow.MainWindow();
            templateWindow.Closed += new EventHandler(templateWindow_Closed);
            templateWindow.Show();

            EndWait();

        }

        /// <summary>
        /// Hanler passed to hovering to tell the template which shape it's over
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateTemplate(object sender, StylusEventArgs e)
        {
            Point hoverPoint = e.GetPosition(circuitPanel.InkCanvas);
            //Handy little function to get the shape
            Sketch.Shape shapeOver = circuitPanel.InkSketch.Sketch.shapeAtPoint(hoverPoint.X, hoverPoint.Y, 100);
            templateWindow.updateTemplate(shapeOver);
        }

        /// <summary>
        /// close the template window
        /// </summary>
        private void closeTemplate(object sender, RoutedEventArgs e)
        {
            //Take away events
            circuitPanel.StylusInAirMove -= new StylusEventHandler(updateTemplate);
            templateWindow.Closed -= new EventHandler(templateWindow_Closed);
            templateWindow.Close();
            TemplateExpander.IsExpanded = false;
        }

        //Handler for when the user closes the template window with the X on the window instead of the expander
        private void templateWindow_Closed(object sender, EventArgs e)
        {
            closeTemplate(sender, new RoutedEventArgs());
        }

        #endregion

        #region Clean Circuit

        private void cleanToggle_Checked(object sender, RoutedEventArgs e)
        {
            circuitDock.Children.Remove(circuitPanel);
            simulationManager.DisplayCleanCircuit(circuitDock);
        }

        private void cleanToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            simulationManager.RemoveCleanCircuit(circuitDock);
            circuitDock.Children.Add(circuitPanel);
            displayManager.AlertFeedback(simulationManager.CircuitParser, false);
            simulationManager.colorWiresByValue();
        }
        
        #endregion

        #region Truth Table

        private void truthTableToggle_Checked(object sender, RoutedEventArgs e)
        {
            simulationManager.TruthTableClosed += new SimulationManager.TruthTableClosedHandler(TruthTableClosed);
            simulationManager.DisplayTruthTable();
        }

        private void truthTableToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            simulationManager.TruthTableClosed -= new SimulationManager.TruthTableClosedHandler(TruthTableClosed);
            simulationManager.CloseTruthTable();
        }

        private void TruthTableClosed()
        {
            truthTableToggle.IsChecked = false;
        }

        #endregion

        #region User Study

            #region User Study Constants

        /// <summary>
        /// The number of times the user must draw each gate or circuit
        /// </summary>
        private const int NUMITERATIONS = 2;

        /// <summary>
        /// The number of recognized and corrected circuits the user will draw
        /// </summary>
        private const int NUMCIRCUITS = 5;

        /// <summary>
        /// The number of unrecognized circuits the user will draw at the end
        /// </summary>
        private const int NUMFINALCIRCUITS = 2;

            #endregion

            #region User Study internals

        /// <summary>
        /// The "trial number" is the number of times the user has recognized this equation
        /// When it is changed, call trialNumChanged()
        /// </summary>
        private int trialNum = 1;

        /// <summary>
        /// The number of the current user
        /// </summary>
        private int userNum = 1;

        /// <summary>
        /// The current equation
        /// </summary>
        private int eqIndex = 0;

        /// <summary>
        /// The current warmup gate
        /// </summary>
        private int gateIndex = 0;

        /// <summary>
        /// The number of attempts the user has made at this equation or gate
        /// </summary>
        private int curIteration = 1;

        /// <summary>
        /// All the gate names we want the users to practice with
        /// </summary>
        private Dictionary<int, string> gateNames;

        /// <summary>
        /// All the equations, along with their reference number
        /// </summary>
        private Dictionary<int, string> equations;

        /// <summary>
        /// The numbers of the chosen equations
        /// </summary>
        private List<int> chosenEqs;

        /// <summary>
        /// Are we during the warmup phase?
        /// </summary>
        private bool warmup = true;
            #endregion

            #region User Study Initialization

        /// <summary>
        /// Initializes the lists used in the user study
        /// </summary>
        private void initUserStudy()
        {
            // Clear the canvas
            circuitPanel.SimpleDeleteAllStrokes();

            // Initialize lists
            chosenEqs = new List<int>();
            gateNames = new Dictionary<int, string>();
            equations = new Dictionary<int, string>();

            if (recognizeButton != null)
                recognizeButton.Visibility = System.Windows.Visibility.Hidden;

            // Fill in the gateNames list
            int i = 0;
            foreach (ShapeType gate in LogicDomain.Gates)
                if (gate != LogicDomain.NOTBUBBLE && gate != LogicDomain.SUBCIRCUIT)
                {
                    gateNames[i] = gate.Name;
                    i += 1;
                }

            // Reset some counters
            warmup = true;
            gateIndex = 0;
            userNum = 1;
            curIteration = 1;
            eqIndex = -1;
            trialNum = 0;
            trialNumChanged();
            UniqueUser();
            UserStudySetIndicators();
            studyProgress.Value = 0;
            progressLabel.Text = (int)studyProgress.Value + "% Completed";

            // Baseline Order
            // Equation number listed on side is the original equation numbering from previous
            // versions of the code (version 6093 and prior)
            equations[1] = "d = (a NOR b) NAND [(NOT b) OR c]\ne = (b XNOR c)";
            equations[2] = "d = (b OR c) NOR c \ne = (a XNOR b) AND (NOT c)";
            equations[3] = "d = a OR (b XOR c) \ne = [a NAND (b XNOR c)] AND a";
            equations[4] = "d = a XNOR (NOT b)\ne = [a NOR b] NAND (b OR c)";
            equations[5] = "d = [a AND (b NOR c)] \ne= (NOT c) NAND (b XOR c)";
            equations[6] = "d = (a NOR b) XOR (NOT c) \ne = (a NAND b) OR (a AND c)";
            equations[7] = "d = a OR [(NOT a) AND b] \ne = a XNOR [(NOT b) XOR c]";
            
            

            // Choose equations to use
            chosenEqs = equations.Keys.ToList();

        }

            #endregion

            #region Saving Study Files

        /// <summary>
        /// Returns a filename and path for the current user, equation, and trial
        /// </summary>
        /// <returns></returns>
        private string UserStudyFilename()
        {
            string directory, filename;

            // Recognition team gets its own filenames...
            if (ForRecognition)
            {
                directory = FilenameConstants.DataRoot + @"GateRecognitionData\User" + userNum + @"\";
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);
                filename = ((ComboBoxItem)gateChooser.SelectedItem).Content.ToString() +"_" + curIteration + FilenameConstants.DefaultXMLExtension; 
                return directory + filename;
            }

            // We're saving this data in Data\DrawingStyleStudyData
            directory = FilenameConstants.DataRoot + @"DrawingStyleStudyData\User" + userNum + @"\";
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            if (warmup)
                filename = gateNames[gateIndex] + "_Iter" + curIteration + "_" + trialNum + FilenameConstants.DefaultXMLExtension;
            else
            {
                string gates = "GatesOff";
                string recognized = "Recognized" + trialNum;
                if ((bool)cond1.IsChecked) gates = "GatesOn";
                if (trialNum == 0) recognized = "Unrecognized";

                filename = eqIndex + "_EQ" + chosenEqs[eqIndex] + "_" + gates + "_Iter" + curIteration + "_" + recognized + FilenameConstants.DefaultXMLExtension;
            }
            return directory + filename;
        }

        /// <summary>
        /// Saves the current sketch to the study data folder with a unique filename
        /// </summary>
        private void SaveUserStudyFile()
        {
            // Please don't save if there is nothing in the sketch, or if nothing has changed
            if (circuitPanel.InkCanvas.Strokes.Count == 0 || (commandManager.CommandCounts.Keys.Count == 0 && trialNum != 1)) return;

            Wait();

            // If we're in warmup, label the shape before we save it.
            if (warmup)
            {
                // Make sure that nothing is in a shape or group
                circuitPanel.InkSketch.Sketch.clearShapes();
                circuitPanel.InkSketch.Sketch.RemoveLabelsAndGroups();

                // Add all the strokes to a single shape, label them all as belonging to a gate
                Sketch.Shape shape = new Sketch.Shape(circuitPanel.InkSketch.Sketch.SubstrokesL, Sketch.XmlStructs.XmlShapeAttrs.CreateNew());
                foreach (Sketch.Substroke substroke in shape.Substrokes)
                    substroke.Classification = LogicDomain.GATE_CLASS;
                shape.Type = LogicDomain.getType(gateNames[gateIndex]);
                
                if (ForRecognition)
                    shape.Type = LogicDomain.getType(((ComboBoxItem)gateChooser.SelectedItem).Content.ToString());

                shape.Orientation = 0;

                circuitPanel.InkSketch.Sketch.AddShape(shape);
                recognitionManager.MakeShapeNamesUnique(circuitPanel.InkSketch.FeatureSketch);
            }


            // Let's not write over exising data...
            string filename = UserStudyFilename();
            while (System.IO.File.Exists(filename))
            {
                trialNum += 1;

                filename = UserStudyFilename();
            }

            if (commandManager.CommandCounts.Keys.Count != 0 && !ForRecognition)
                SaveUserCommands();
            circuitPanel.SaveSketch(filename);

            // Iterate the trial number
            trialNum += 1;
            trialNumChanged();

            EndWait();
        }

        /// <summary>
        /// Prints the number of times the user has performed various commands to a text file.
        /// </summary>
        private void SaveUserCommands()
        {
            // Create a new file for this user.  If the file already exists, add to it
            string filename = FilenameConstants.DataRoot + @"DrawingStyleStudyData\User" + userNum + @"\Commands.txt";
            System.IO.StreamWriter writer = new System.IO.StreamWriter(filename, true);
            Dictionary<string, int> counts = commandManager.CommandCounts;

            // For now, recording all commands rather than specific ones.
            if (warmup)
                writer.WriteLine("Gate " + gateNames[gateIndex] + ", Iter " + curIteration + ", Trial " + trialNum);
            else
                writer.WriteLine(eqIndex + ") EQ " + chosenEqs[eqIndex] + ", Iter " + curIteration + ", Trial " + trialNum);
            writer.WriteLine("---------------------------------------------------");
            foreach (string command in counts.Keys)
            {
                writer.WriteLine(command + ": " + counts[command].ToString());
            }
            writer.WriteLine();
            writer.Close();
        }

            #endregion

            #region Advancing The Study

        /// <summary>
        /// When we change the trial, clear the command manager counts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trialNumChanged()
        {
            if (commandManager != null)
                commandManager.Clear();
            if (trialNum == 0 && !warmup)
                nextButton.IsEnabled = false;
            else
                nextButton.IsEnabled = true;
        }

        /// <summary>
        /// Save everything form the last user, iterate the user number
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newUserButton_Click(object sender, RoutedEventArgs e)
        {
            SaveUserStudyFile();
            initUserStudy();
        }

        /// <summary>
        /// Get a user number that hasn't been used before
        /// </summary>
        /// <returns></returns>
        private void UniqueUser()
        {
            string directory;
            int num = userNum;

            if (ForRecognition)
                directory = FilenameConstants.DataRoot + @"GateRecognitionData\User" + num + @"\";
            else
                directory = FilenameConstants.DataRoot + @"DrawingStyleStudyData\User" + num + @"\";

            while (System.IO.Directory.Exists(directory))
            {
                num += 1;
                if (ForRecognition)
                    directory = FilenameConstants.DataRoot + @"GateRecognitionData\User" + num + @"\";
                else
                    directory = FilenameConstants.DataRoot + @"DrawingStyleStudyData\User" + num + @"\";
            }

            userNum = num;

            this.Title = FilenameConstants.ProgramName + " - User " + userNum;
            userIDIndicator.Text = "User ID: " + userNum;
        }

        /// <summary>
        /// Saves the sketch, clears the screen, and displays the next equation or gate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            SaveUserStudyFile();
            circuitPanel.SimpleDeleteAllStrokes();
            circuitPanel.EnableDrawing();
            displayManager.RemoveFeedbacks();

            trialNum = 0;
            trialNumChanged();

            if (ForRecognition)
            {
                GateRecognitionStudyNext();
                return;
            }

            // Are we doing the current gate/circuit again?
            if (curIteration < NUMITERATIONS)
            {
                curIteration += 1;
            }
            else
            {
                curIteration = 1;

                // We're in the gates-only section
                if (warmup)
                {
                    gateIndex += 1;

                    // You have hit the end of the gates-only section!
                    if (gateIndex == gateNames.Count)
                    {
                        MessageBox.Show(this, "You are now done with the warmup section of the study.\nFeel free to take a short break.", "Warmup Over");

                        // Set up for non-warmup tasks
                        warmup = false;
                        recognizeButton.Visibility = System.Windows.Visibility.Visible;
                        nextButton.IsEnabled = false;

                        circuitPanel.EnableDrawing();
                    }
                }

                // Circuits!
                if (!warmup)
                {
                    eqIndex += 1;
                    if (eqIndex == (int)(chosenEqs.Count) / 2)
                        MessageBox.Show(this, "You are halfway done with the circuit portion of the study.\nFeel free to take a short break.", "Halfway done!");
                }
            }

            // Set the progress bar, equation indicator, and everything else
            UserStudySetIndicators();
        }

        /// <summary>
        /// Sets the text of the equation and iteration indicators, as well as the value of the progress bar
        /// </summary>
        private void UserStudySetIndicators()
        {
            // Indicates how many times you have done this same gate/circuit
            if (NUMITERATIONS > 1)
                iterationIndicator.Text = "Trial #" + curIteration;

            // Set the progress bar and equation indicator, depending on where we are.
            // Circuits are counted as three times the progress of individual gates.
            double totalProgress = (gateNames.Count + 3 * chosenEqs.Count) * NUMITERATIONS;
            if (warmup) // Gate section
            {
                equationIndicator.Text = gateNames[gateIndex];
                studyProgress.Value += 1 / totalProgress * 100;
            }
            else if (eqIndex < chosenEqs.Count) // Circuits section
            {
                equationIndicator.Text = equations[chosenEqs[eqIndex]];
                studyProgress.Value += 3 / totalProgress * 100;
            }
            else // End of study
            {
                equationIndicator.Text = "Thank you for participating!";
                studyProgress.Value = 100;
                MessageBox.Show(this, "You are done with the test!\nThank you for participating!", "You're the Best!");
            }
            progressLabel.Text = (int)studyProgress.Value + "% Completed";
        }

            #endregion

            #region Study Conditions

        /// <summary>
        /// The first condition for the study.  Enables Ghost Gates.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cond1_Click(object sender, RoutedEventArgs e)
        {
            highlightEndpoints.IsChecked = false;
            meshHighlight.IsChecked = false;
            allowRotateGates.IsChecked = true;
            shapesAfterLabel.IsChecked = true;
            shapesAfterRec.IsChecked = true;
            shapesOnHover.IsChecked = true;
            labelsAfterRec.IsChecked = false;
            shapesWhileDrawing.IsChecked = false;
            colorShapes.IsChecked = true;
            tooltipsOn.IsChecked = false;
            displayManager.EndpointHighlighting = false;
            displayInternalEndpoints.IsChecked = false;
        }

        /// <summary>
        /// The second condition for the study. Disables Ghost Gates.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cond2_Click(object sender, RoutedEventArgs e)
        {
            highlightEndpoints.IsChecked = false;
            meshHighlight.IsChecked = false;
            allowRotateGates.IsChecked = false;
            shapesAfterLabel.IsChecked = false;
            shapesAfterRec.IsChecked = false;
            shapesOnHover.IsChecked = false;
            labelsAfterRec.IsChecked = true;
            shapesWhileDrawing.IsChecked = false;
            colorShapes.IsChecked = true;
            tooltipsOn.IsChecked = false;
            displayManager.EndpointHighlighting = false;
            displayInternalEndpoints.IsChecked = false;
        }

            #endregion

            #region User study on/off

        /// <summary>
        /// Sets up the UI for the user study
        /// </summary>
        private void UserStudyOn()
        {
            Clear();
            commandManager.CountCommands = true;
            commandManager.Clear();

            // Make special user study things visible
            headerUserStudyMenu.Visibility = System.Windows.Visibility.Visible;
            nextButton.Visibility = System.Windows.Visibility.Visible;
            equationIndicator.Visibility = System.Windows.Visibility.Visible;
            studyProgress.Visibility = System.Windows.Visibility.Visible;
            progressLabel.Visibility = System.Windows.Visibility.Visible;
            if (NUMITERATIONS > 1) iterationIndicator.Visibility = System.Windows.Visibility.Visible;

            // Make some normal things invisible, or at least disabled (or visible)
            headerFeedbackMenu.IsEnabled = false;
            headerFileMenu.IsEnabled = false;
            //headerViewMenu.IsEnabled = false;
            PracticeExpander.Visibility = System.Windows.Visibility.Hidden;
            recognizeButton.Visibility = System.Windows.Visibility.Hidden;
            TemplateExpander.Visibility = System.Windows.Visibility.Hidden;
            embedButton.Visibility = System.Windows.Visibility.Hidden;
            nextButton.IsEnabled = true;

            // Initialize everything!
            cond1.IsChecked = true;
            initUserStudy();
        }

        /// <summary>
        /// Puts everything back to normal after the user study
        /// </summary>
        private void UserStudyOff()
        {
            Clear();
            commandManager.CountCommands = false;
            commandManager.Clear();

            // Make special user study things invisible
            headerUserStudyMenu.Visibility = System.Windows.Visibility.Hidden;
            nextButton.Visibility = System.Windows.Visibility.Hidden;
            equationIndicator.Visibility = System.Windows.Visibility.Hidden;
            studyProgress.Visibility = System.Windows.Visibility.Hidden;
            progressLabel.Visibility = System.Windows.Visibility.Hidden;
            iterationIndicator.Visibility = System.Windows.Visibility.Hidden;

            // Make some normal things invisible, or at least disabled
            headerFeedbackMenu.IsEnabled = true;
            headerFileMenu.IsEnabled = true;
            //headerViewMenu.IsEnabled = true;
            PracticeExpander.Visibility = System.Windows.Visibility.Visible;
            recognizeButton.Visibility = System.Windows.Visibility.Visible;
            TemplateExpander.Visibility = System.Windows.Visibility.Visible;
            embedButton.Visibility = System.Windows.Visibility.Visible;

            // We probably want endpoints and mesh highlighting on...
            meshHighlight.IsChecked = true;
            highlightEndpoints.IsChecked = true;
        }

        /// <summary>
        /// When you check the user study checkbox, call this event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void userStudyOption_Click(object sender, RoutedEventArgs e)
        {
            UserStudy = !UserStudy;
            if (UserStudy)
                UserStudyOn();
            else
                UserStudyOff();
        }

            #endregion

            #region Stuff for Recognition

        /// <summary>
        /// So, the recognition team wants their own user study mode to gather N of the same gate at a time.
        /// </summary>
        private bool ForRecognition = false;

        /// <summary>
        /// The number of copies of each gate we want
        /// </summary>
        private const int NUMGATEITERATIONS = 50;

        /// <summary>
        /// Sets bool
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void forRecognition_Click(object sender, RoutedEventArgs e)
        {
            ForRecognition = (bool)forRecognition.IsChecked;
            if (ForRecognition)
            {
                curIteration = 1;
                gateChooser.Visibility = System.Windows.Visibility.Visible;
                MakeGateChooser();
                iterationIndicator.Text = "Gate #" + curIteration;
                studyProgress.Value = 0;
                progressLabel.Text = (int)studyProgress.Value + "% Completed";
                userNum = 1;
                warmup = true;
                UniqueUser();
            }
            else
            {
                gateChooser.Visibility = System.Windows.Visibility.Collapsed;
                initUserStudy();
            }
        }

        /// <summary>
        /// Populates the gate chooser
        /// </summary>
        private void MakeGateChooser()
        {
            gateChooser.Items.Clear();
            foreach (ShapeType gate in LogicDomain.Gates)
            {
                if (gate == LogicDomain.SUBCIRCUIT)
                    continue;
                ComboBoxItem item = new ComboBoxItem();
                item.Content = gate.Name;
                gateChooser.Items.Add(item);
                gateChooser.SelectedItem = item;
            }
        }

        private void GateRecognitionStudyNext()
        {
            if (curIteration < NUMGATEITERATIONS)
            {
                curIteration += 1;
                equationIndicator.Text = ((ComboBoxItem)gateChooser.SelectedItem).Content.ToString();
                iterationIndicator.Text = "Gate #" + curIteration;
                studyProgress.Value += 1 / (double)NUMGATEITERATIONS * 100;
                progressLabel.Text = (int)studyProgress.Value + "% Completed";
            }
            else
            {
                equationIndicator.Text = "Thank you for participating!";
                studyProgress.Value = 100;
                progressLabel.Text = (int)studyProgress.Value + "% Completed";
                MessageBox.Show(this, "You are done with the test!\nThank you for participating!", "You're the Best!");
                userStudyOption_Click(null, null);
                return;
            }

        }

        /// <summary>
        /// Set the equation indicator when the gate choice has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gateChooser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            equationIndicator.Text = ((ComboBoxItem)gateChooser.SelectedItem).Content.ToString();
        }

            #endregion

        #endregion
    }
}
