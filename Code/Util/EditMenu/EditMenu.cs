using Domain;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using SketchPanelLib;
using System.Windows;
using System.Windows.Input;
using EditMenu;
using CommandManagement;
using InkToSketchWPF;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EditMenu
{
    #region Event Handlers

    public delegate void RegroupEventHandler(List<Sketch.Shape> shape);

    public delegate void SetSelectWidgetHandler();

    /// <summary>
    /// Delegate for handling adding a learning example when an error is corrected
    /// </summary>
    /// <param name="shape"></param>
    public delegate void LearningEventHandler(Sketch.Shape shape);

    /// <summary>
    /// Delegate for handling when ink is re-labeled or re-grouped.
    /// </summary>
    public delegate void InkRerecognizedEventHandler();

    /// <summary>
    /// Delegate for handling when the edit menu is completely finished with its selection
    /// </summary>
    public delegate void EditMenuFinishedEventHandler();

    #endregion

    public class EditMenu : UIElement
    {
        #region Global Constants

        int BUTTON_HEIGHT = 18;
        int BUTTON_WIDTH = 32;
        int BUTTON_HORZ_SEPARATION = 33; // Distance from left of one button to left of another
        int BUTTON_VERT_SEPARATION = 18; // Distance from top of one button to top of another

        #endregion

        #region Internals

        private bool debug = false;

        // Events
        public event RegroupEventHandler RegroupShapes;
        public event LearningEventHandler LearnFromCorrection;
        public event InkRerecognizedEventHandler InkRerecognized;
        public event SetSelectWidgetHandler SetSelectWidget;
        public event EditMenuFinishedEventHandler EditMenuFinished;

        protected SketchPanel sketchPanel;
        private List<Popup> activeButtonList;   
        private LabelMenu labelMenu;
        private CommandManagement.CommandManager commandManager;
        private System.Windows.Point drawPoint;
        private bool labelFuncSet;
        private bool visible;
        private bool buttonPressed;
        private bool labelPressed;
        private bool overButton;
        private System.Windows.Rect labelRect;

        // Button images and brushes
        private System.Windows.Controls.Image undoImage;
        private System.Windows.Controls.Image redoImage;
        private System.Windows.Controls.Image undoImageGray;
        private System.Windows.Controls.Image redoImageGray;
        private System.Windows.Media.Effects.DropShadowEffect highlightEffect;
        private System.Windows.Media.LinearGradientBrush highlightBrush;
        private System.Windows.Media.LinearGradientBrush normalBrush;

        #endregion

        #region Constructor

        public EditMenu(ref SketchPanel sp, CommandManagement.CommandManager commandManager)
        {
            sketchPanel = sp;
            activeButtonList = new List<Popup>();
            this.commandManager = commandManager;
            visible = false;
            labelFuncSet = false;
            overButton = false;
            drawPoint = new Point();

            #region Initialize the images for undo/redo, brush effects

            undoImage = new System.Windows.Controls.Image();
            System.Windows.Media.Imaging.BitmapImage undoBitmap = new System.Windows.Media.Imaging.BitmapImage( new Uri(
                                        (AppDomain.CurrentDomain.BaseDirectory + @"EditIcons\UndoIcon.bmp")));
            undoImage.Source = undoBitmap;

            redoImage = new System.Windows.Controls.Image();
            System.Windows.Media.Imaging.BitmapImage redoBitmap = new System.Windows.Media.Imaging.BitmapImage( new Uri(
                                        (AppDomain.CurrentDomain.BaseDirectory + @"EditIcons\RedoIcon.bmp")));
            redoImage.Source = redoBitmap;

            undoImageGray = new System.Windows.Controls.Image();
            System.Windows.Media.Imaging.BitmapImage undoGrayBitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(
                                        (AppDomain.CurrentDomain.BaseDirectory + @"EditIcons\UndoIconGray.bmp")));
            undoImageGray.Source = undoGrayBitmap;

            redoImageGray = new System.Windows.Controls.Image();
            System.Windows.Media.Imaging.BitmapImage redoGrayBitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(
                                        (AppDomain.CurrentDomain.BaseDirectory + @"EditIcons\RedoIconGray.bmp")));
            redoImageGray.Source = redoGrayBitmap;

            // Initialize Button Effects and Brushes
            highlightBrush = new System.Windows.Media.LinearGradientBrush(System.Windows.Media.Colors.White, System.Windows.Media.Colors.SkyBlue,
                                                                    new System.Windows.Point(0, 0), new System.Windows.Point(0, 1));
            normalBrush = new System.Windows.Media.LinearGradientBrush(System.Windows.Media.Colors.White, System.Windows.Media.Colors.Silver,
                                                                    new System.Windows.Point(0, 0), new System.Windows.Point(0, 1));

            highlightEffect = new System.Windows.Media.Effects.DropShadowEffect();
            highlightEffect.ShadowDepth = 1.0;
            highlightEffect.Color = System.Windows.Media.Colors.Silver;
            
            #endregion

            InitializeLabelMenu();
        }

        /// <summary>
        /// shows messages easily for exception, and for hiding them
        /// when we are not in debugging mode
        /// </summary>
        /// <param name="methodName">the method in which exception is thrown</param>
        /// <param name="e">the exception</param>
        public void debugExceptionMessage(String methodName, Exception e)
        {
            System.Windows.MessageBox.Show(methodName + " threw an exception: " + e.ToString());
        }

        #endregion 

        #region Display/Remove Menu

        /// <summary>
        /// Display the context based menu, which contains a ring of buttons
        /// </summary>
        public void displayContextMenu(System.Windows.Point point)
        {
            int moveX = (int)point.X;
            int moveY = (int)point.Y;
            drawPoint = point;

            labelPressed = false;

            // Get the booleans that describe the context
            bool recognized = sketchPanel.Recognized;
            bool hasStroke = (sketchPanel.InkCanvas.GetSelectedStrokes().Count != 0);

            if (visible)
                return;
            visible = true;

            // Display various different menus depending on the items in the selection, as
            // denoted by the two flags
            Point coord = sketchPanel.PointToScreen(new Point(moveX, moveY));
            PresentationSource source = PresentationSource.FromVisual(sketchPanel);
            System.Windows.Point targetPoint = source.CompositionTarget.TransformFromDevice.Transform(coord);
            int targetX = (int)targetPoint.X;
            int targetY = (int)targetPoint.Y;
            if (recognized && hasStroke)
            {
                displayPasteButton(targetX, targetY);
                displayLabelButton(targetX, targetY);
                displayGroupButton(targetX, targetY);
                displayCutButton(targetX, targetY);
                displayCopyButton(targetX, targetY);
                displayDeleteButton(targetX, targetY);
                displayUndoRedoButton(targetX, targetY);
            }

            else if (hasStroke)
            {
                displayPasteButton(targetX, targetY);
                displayCutButton(targetX, targetY);
                displayCopyButton(targetX, targetY);
                displayDeleteButton(targetX, targetY);
                displayUndoRedoButton(targetX, targetY);
            }

            // If nothing is selected, then only paste is a reasonable thing to do,
            else
            {
                displayPasteButton(targetX, targetY);
                displayDeleteAllButton(targetX, targetY);
                displayUndoRedoButton(targetX, targetY);
            }
        }

        /// <summary>
        /// Remove the context menu
        /// </summary>
        public void removeMenu()
        {
            foreach (Popup current in activeButtonList)
            {
                current.Visibility = System.Windows.Visibility.Hidden;
                current.IsOpen = false;
                RemoveButtonEvents(current);
                ((Button)current.Child).Content = null;
                current.Child = null;
            }
            activeButtonList.Clear();

            visible = false;
            buttonPressed = false;
        }

        #region Display Buttons

        /// <summary>
        /// Generic function to make popups for a button, and keep track of them in a list
        /// </summary>
        /// <param name="button"> The button with an event to really make POP!!</param>

        private void makePopUp(Button button, int x, int y)
        {
            Popup pop = new Popup();

            pop.Child = button;
            pop.Width = 50;
            pop.Height = 25;
            pop.AllowsTransparency = true;
            pop.IsOpen = true;
            pop.Visibility = System.Windows.Visibility.Visible;

            pop.HorizontalOffset = x;
            pop.VerticalOffset = y;

            activeButtonList.Add(pop);
        }

        /// <summary>
        /// Sets button properties and displays button
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        private System.Windows.Controls.Button setButtonProperties(int x, int y, string label)
        {
            Button newButton = new Button();
            newButton.Height = BUTTON_HEIGHT;
            newButton.Name = label;

            newButton.StylusInAirMove += new StylusEventHandler(button_StylusOver);
            newButton.StylusLeave += new StylusEventHandler(button_StylusAway);

            if (label != "Undo" && label != "Redo")
            {
                newButton.Width = BUTTON_WIDTH;
                newButton.Content = label;
            }
            else if (label == "Undo")
            {
                newButton.Width = BUTTON_WIDTH / 2;
                if (commandManager.UndoValid)
                    newButton.Content = undoImageGray;
                else
                {
                    newButton.Content = undoImage;
                    newButton.IsEnabled = false;
                }
            }
            else
            {
                newButton.Width = BUTTON_WIDTH / 2;
                if (commandManager.RedoValid)
                    newButton.Content = redoImageGray;
                else
                {
                    newButton.Content = redoImage;
                    newButton.IsEnabled = false;
                }
            }

            newButton.FontSize = 9;

            InkCanvas.SetLeft(newButton, x);
            InkCanvas.SetTop(newButton, y - newButton.Height);
            newButton.BringIntoView();

            newButton.Visibility = System.Windows.Visibility.Visible;

            return newButton;
        }

        #region Display Individual Buttons

        /// <summary>
        /// Display the group button
        /// </summary>
        /// <param name="x">the x coordinate at which to show the button</param>
        /// <param name="y">the y coordinate at which to show the button</param>
        private void displayGroupButton(int x, int y)
        {
            System.Windows.Controls.Button groupButton = setButtonProperties(0,0,"Group");//x + BUTTON_HORZ_SEPARATION*3/4, y - BUTTON_VERT_SEPARATION, "Group");
            groupButton.Click += new RoutedEventHandler(groupButton_Click);

            makePopUp(groupButton, x + BUTTON_WIDTH / 2 + BUTTON_HORZ_SEPARATION * 5 / 4, y - BUTTON_HEIGHT / 2 - BUTTON_VERT_SEPARATION);
        }


        /// <summary>
        /// Display the delete button
        /// </summary>
        /// <param name="x">the x coordinate at which to show the button</param>
        /// <param name="y">the y coordinate at which to show the button</param>
        private void displayDeleteButton(int x, int y)
        {
            Button deleteButton = setButtonProperties(0,0, "Delete");
            deleteButton.Click += new RoutedEventHandler(deleteButton_Click);

            makePopUp(deleteButton, x + BUTTON_WIDTH / 2, y - BUTTON_HEIGHT / 2 + BUTTON_VERT_SEPARATION * 3);
        }

        /// <summary>
        /// Display the paste button
        /// </summary>
        /// <param name="x">the x coordinate at which to show the button</param>
        /// <param name="y">the y coordinate at which to show the button</param>
        private void displayPasteButton(int x, int y)
        {
            Button pasteButton = setButtonProperties(0,0 , "Paste");
            pasteButton.StylusUp += new StylusEventHandler(pasteButton_Click);

            if (commandManager.ClipboardEmpty)
                pasteButton.IsEnabled = false;
            
            makePopUp(pasteButton, x + BUTTON_WIDTH/2, y + BUTTON_VERT_SEPARATION * 2 - BUTTON_HEIGHT/2);
        }

        /// <summary>
        /// Display the select-all button
        /// </summary>
        /// <param name="x">the x coordinate at which to show the button</param>
        /// <param name="y">the y coordinate at which to show the button</param>
        private void displaySelectAllButton(int x, int y)
        {
            Button selectAllButton = setButtonProperties(0,0, "SelAll");
            selectAllButton.Click += new RoutedEventHandler(selectAllButton_Click);

            makePopUp(selectAllButton, x + BUTTON_WIDTH / 2, y - BUTTON_HEIGHT / 2 + BUTTON_VERT_SEPARATION * 4);
        }

        /// <summary>
        /// Displays the Undo and Redo Buttons
        /// </summary>
        private void displayUndoRedoButton(int x, int y)
        {
            Button undoButton = setButtonProperties(0,0, "Undo");
            Button redoButton = setButtonProperties(0,0, "Redo");
            
            undoButton.Click += new RoutedEventHandler(undoButton_Click);
            redoButton.Click += new RoutedEventHandler(redoButton_Click);

            makePopUp(undoButton, x + BUTTON_WIDTH / 2 - BUTTON_WIDTH / 2, y - BUTTON_HEIGHT / 2 + BUTTON_VERT_SEPARATION * 4);
            makePopUp(redoButton, x + BUTTON_WIDTH / 2, y - BUTTON_HEIGHT / 2 + BUTTON_VERT_SEPARATION * 4);
        }

        /// <summary>
        /// Display the delete-all button
        /// </summary>
        /// <param name="x">the x coordinate at which to show the button</param>
        /// <param name="y">the y coordinate at which to show the button</param>
        private void displayDeleteAllButton(int x, int y)
        {
            Button deleteAllButton = setButtonProperties(0,0, "DelAll");
            deleteAllButton.Click += new RoutedEventHandler(deleteAllButton_Click);

            makePopUp(deleteAllButton, x + BUTTON_WIDTH / 2, y - BUTTON_HEIGHT / 2 + BUTTON_VERT_SEPARATION * 3);
        }

        /// <summary>
        /// Display the cut button
        /// </summary>
        /// <param name="x">the x coordinate at which to show the button</param>
        /// <param name="y">the y coordinate at which to show the button</param>
        private void displayCutButton(int x, int y)
        {
            Button cutButton = setButtonProperties(0,0, "Cut");
            cutButton.Click += new RoutedEventHandler(cutButton_Click);

            makePopUp(cutButton, x + BUTTON_WIDTH / 2 - BUTTON_HORZ_SEPARATION, y - BUTTON_HEIGHT / 2 + BUTTON_VERT_SEPARATION * 2);
        }

        /// <summary>
        /// Display the copy button
        /// </summary>
        /// <param name="x">the x coordinate at which to show the button</param>
        /// <param name="y">the y coordinate at which to show the button</param>
        private void displayCopyButton(int x, int y)
        {
            Button copyButton = setButtonProperties(0,0, "Copy");
            copyButton.Click += new RoutedEventHandler(copyButton_Click);

            makePopUp(copyButton, x + BUTTON_WIDTH / 2 + BUTTON_HORZ_SEPARATION * 3 / 4, y - BUTTON_HEIGHT / 2 + BUTTON_VERT_SEPARATION);
        }

        /// <summary>
        /// Display the label button
        /// </summary>
        /// <param name="x">the x coordinate at which to show the button</param>
        /// <param name="y">the y coordinate at which to show the button</param>
        private void displayLabelButton(int x, int y)
        {
            Button labelButton = setButtonProperties(0,0, "Label");
            labelButton.StylusUp += new StylusEventHandler(labelButton_Click);

            makePopUp(labelButton, x + BUTTON_WIDTH / 2 + BUTTON_HORZ_SEPARATION, y - BUTTON_HEIGHT / 2);
        }
        #endregion

        #endregion

        #endregion

        #region Button Events

        /// <summary>
        /// Highlights any EditMenu button that the stylus is over.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_StylusOver(object sender, StylusEventArgs e)
        {
            Button buttonOver = (Button) sender;
            overButton = true;
            
            if (buttonOver != null && buttonOver.IsEnabled)
            {
                buttonOver.Effect = highlightEffect;
                buttonOver.Background = highlightBrush;
            }
        }

        /// <summary>
        /// Un-highlights any EditMenu button when the stylus leaves its airspace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_StylusAway(object sender, StylusEventArgs e)
        {
            Button button = (Button)sender;
            overButton = false;

            if (button != null && button.IsEnabled)
            {
                button.Effect = null;
                button.Background = normalBrush;
            }
        }

        /// <summary>
        /// Removes and redisplays the context menu.  Will not redisplay if shouldRemoveMenu is set to true.
        /// </summary>
        /// <param name="shouldRemoveMenu"></param>
        private void updateMenu(bool shouldRemoveMenu = false)
        {
            closeLabelMenu();
            removeMenu();

            if (shouldRemoveMenu)
            {
                if (EditMenuFinished != null)
                    EditMenuFinished();
                return;
            }

            // If we still have selected strokes, move the menu
            if (sketchPanel.InkCanvas.GetSelectedStrokes().Count > 0)
                displayContextMenu(sketchPanel.InkCanvas.GetSelectionBounds().BottomRight);
            else
                displayContextMenu(drawPoint);
        }

        #region Specific button clicks

        private void groupButton_Click(object sender, RoutedEventArgs e)
        {
            if (debug) Console.WriteLine("___Group from button");

            // Let the user know we're thinking
            // by using the overriding wait cursor
            // (I don't know why we have to use this one as opposed
            // to using UseCustomCursor [= )
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = Cursors.Wait;

            Group();
            buttonPressed = true;
            
            // Turn the wait cursor off
            sketchPanel.UseCustomCursor = false;

            updateMenu(true);
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (debug) Console.WriteLine("___Delete from button");

            // Let the user know we're thinking
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = Cursors.Wait;

            sketchPanel.DeleteStrokes();
            buttonPressed = true;

            updateMenu(true);           
            
        }

        private void pasteButton_Click(object sender, StylusEventArgs e)
        {
            if (debug) Console.WriteLine("___Paste from button");

            // Let the user know we're thinking
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = Cursors.Wait;

            Point pos = e.GetPosition(sketchPanel.InkCanvas);
            sketchPanel.PasteStrokes(new System.Windows.Point(pos.X, pos.Y));
            buttonPressed = true;

            updateMenu();
        }

        private void selectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (debug) Console.WriteLine("___SelAll from button");
            sketchPanel.SelectAllStrokes();
            if (SetSelectWidget != null)
                SetSelectWidget();
            buttonPressed = true;
        }

        private void undoButton_Click(object sender, RoutedEventArgs e)
        {
            if (debug) Console.WriteLine("___Undo from button");

            // Let the user know we're thinking
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = Cursors.Wait;

            sketchPanel.Undo();
            buttonPressed = true;

            updateMenu();
        }

        private void redoButton_Click(object sender, RoutedEventArgs e)
        {
            if (debug) Console.WriteLine("___Redo from button");

            // Let the user know we're thinking
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = Cursors.Wait;

            sketchPanel.Redo();
            buttonPressed = true;

            updateMenu();
        }

        private void deleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (debug) Console.WriteLine("___DelAll from button");

            // Let the user know we're thinking
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = Cursors.Wait;

            sketchPanel.DeleteAllStrokes();

            buttonPressed = true;
            updateMenu(true);
        }

        private void cutButton_Click(object sender, RoutedEventArgs e)
        {
            if (debug) Console.WriteLine("___Cut from button");

            // Let the user know we're thinking
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = Cursors.Wait;

            sketchPanel.CutStrokes();
            buttonPressed = true;

            updateMenu(true);
        }

        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            if (debug) Console.WriteLine("___Copy from button");
            sketchPanel.CopyStrokes();
            buttonPressed = true;
        }

        private void labelButton_Click(object sender, StylusEventArgs e)
        {
            if (debug) Console.WriteLine("___Label from button");
            Point pos = e.GetPosition(sketchPanel.InkCanvas);
            DisplayLabelMenu(pos);
        }

        #endregion

        #endregion

        #region Getters & Setters

        public bool Visible
        {
            get
            {
                return visible;
            }
        }

        public bool ButtonPressed
        {
            get
            {
                return buttonPressed;
            }
        }

        public bool LabelPressed
        {
            get
            {
                return labelPressed;
            }
        }

        public bool OverButton
        {
            get
            {
                return overButton;
            }
        }

        #endregion

        #region Edit Functions

        /// <summary>
        /// Display the label menu when activated through the Tools widget or the Label widget
        /// Precondition: Strokes to be labeled are already selected
        /// </summary>
        /// <param name="location"></param>
        public void DisplayLabelMenu(System.Windows.Point location)
        {
            // Let the user know we're thinking
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = Cursors.Wait;

            if (!sketchPanel.InkCanvas.Children.Contains(labelMenu))
                InitializeLabelMenu();

            // Style the label menu
            if (location.Y + this.labelMenu.Height > sketchPanel.Height)
                location.Y -= (int)(this.labelMenu.Height - BUTTON_HEIGHT);
            labelMenu.HorizontalOffset = location.X;
            labelMenu.VerticalOffset = location.Y;
            this.labelRect = new Rect();
            this.labelRect.Location = new System.Windows.Point(location.X -3 -labelMenu.Width , location.Y-3);
            this.labelRect.Size = new System.Windows.Size(labelMenu.Width+6, labelMenu.Height+6);

            // Update it and open it
            this.labelMenu.UpdateLabelMenu(sketchPanel.InkCanvas.GetSelectedStrokes());
            this.labelMenu.IsOpen = true;

            // Hook into relabeling event
            if (!labelFuncSet)
            {
                labelMenu.errorCorrected += new ErrorCorrectedEventHandler(labelMenu_errorCorrected);
                labelMenu.applyLabel += new ApplyLabelEventHandler(applyLabel_Regroup);
                labelMenu.LabelMenuFinished += new EditMenuFinishedEventHandler(EditMenuFinished);
                labelFuncSet = true;
            }


            sketchPanel.UseCustomCursor = false;
        }

        /// <summary>
        /// Groups selected strokes. 
        /// </summary>
        public void Group()
        {
            Group(new ShapeType(), sketchPanel.InkCanvas.GetSelectedStrokes());

            sketchPanel.EnableDrawing();
        }


        /// <summary>
        /// Groups the given StrokeCollection with the given label. 
        /// </summary>
        public void Group(ShapeType labelType, System.Windows.Ink.StrokeCollection selectedStrokes)
        {
            // Apply the label
            if (selectedStrokes.Count > 0)
            {
                CommandList.ApplyLabelCmd applyLabel = new CommandList.ApplyLabelCmd(sketchPanel,
                    selectedStrokes, labelType);

                applyLabel.Regroup += new CommandList.RegroupEventHandler(applyLabel_Regroup);

                commandManager.ExecuteCommand(applyLabel);
            }
            
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Handles learning from error correction event.
        /// </summary>
        /// <param name="shape"></param>
        public void labelMenu_errorCorrected(Sketch.Shape shape)
        {
            if (LearnFromCorrection != null)
                LearnFromCorrection(shape);
        }

        /// <summary>
        /// Handles regroup events
        /// </summary>
        /// <param name="shape">The last element should be the one that you wish to draw feedback for</param>
        public void applyLabel_Regroup(List<Sketch.Shape> shapes)
        {
            if (RegroupShapes != null)
                RegroupShapes(shapes);
            else if (InkRerecognized != null)
                InkRerecognized();
        }

        /// <summary>
        /// Removes the click event of the given button.
        /// </summary>
        /// <param name="clicked"></param>
        private void RemoveButtonEvents(Popup pop)
        {
            Button button = (Button)pop.Child;
            button.StylusInAirMove -= new StylusEventHandler(button_StylusOver);
            button.StylusLeave -= new StylusEventHandler(button_StylusAway);


            switch ((string)button.Name)
            {
                case "Copy":
                    button.Click -= new RoutedEventHandler(copyButton_Click);
                    break;
                case "Cut":
                    button.Click -= new RoutedEventHandler(cutButton_Click);
                    break;
                case "Paste":
                    button.StylusUp -= new StylusEventHandler(pasteButton_Click);
                    break;
                case "Delete":
                    button.Click -= new RoutedEventHandler(deleteButton_Click);
                    break;
                case "DelAll":
                    button.Click -= new RoutedEventHandler(deleteAllButton_Click);
                    break;
                case "SelAll":
                    button.Click -= new RoutedEventHandler(selectAllButton_Click);
                    break;
                case "Group":
                    button.Click -= new RoutedEventHandler(groupButton_Click);
                    break;
                case "Label":
                    button.StylusUp -= new StylusEventHandler(labelButton_Click);
                    break;
                case "Undo":
                    button.Click -= new RoutedEventHandler(undoButton_Click);
                    break;
                case "Redo":
                    button.Click -= new RoutedEventHandler(redoButton_Click);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Label Menu Stuff

        /// <summary>
        /// Initializes the label menu
        /// </summary>
        private void InitializeLabelMenu()
        {
            labelMenu = new LabelMenu(ref this.sketchPanel, this.commandManager);

            sketchPanel.InkCanvas.Children.Add(labelMenu);
            labelMenu.LoadLabels();
            closeLabelMenu();
        }

        public void closeLabelMenu()
        {
            labelMenu.IsOpen = false;
        }

        public System.Windows.Rect LabelRectangle
        {
            get { return this.labelRect; }
        }

        public bool LabelMenuIsOpen
        {
            get { return labelMenu.IsOpen; }
        }
        #endregion

        #region Unused Code

        /// <summary>
        /// Executes the functionality of the given button.  Not currently used.
        /// </summary>
        /// <param name="clicked"></param>
        private void ButtonResult(Button clicked, Point position)
        {
            if (clicked.Name != "SelAll" && clicked.Name != "Copy")
            {
                // Let the user know we're thinking
                sketchPanel.UseCustomCursor = true;
                sketchPanel.Cursor = Cursors.Wait;
            }

            labelPressed = false;
            switch ((string)clicked.Name)
            {
                case "Copy":
                    sketchPanel.CopyStrokes();
                    break;
                case "Cut":
                    sketchPanel.CutStrokes();
                    break;
                case "Paste":
                    sketchPanel.PasteStrokes(position);
                    break;
                case "Delete":
                    sketchPanel.DeleteStrokes();
                    break;
                case "DelAll":
                    sketchPanel.DeleteAllStrokes();
                    break;
                case "SelAll":
                    sketchPanel.SelectAllStrokes();
                    break;
                case "Group":
                    Group();
                    break;
                case "Label":
                    labelPressed = true;
                    break;
                case "Undo":
                    sketchPanel.Undo();
                    break;
                case "Redo":
                    sketchPanel.Redo();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Returns the button at the given position, if any. Not currently used.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool FindButton(System.Windows.Point point)
        {
            Rect buttonLoc = new Rect();

            foreach (Popup pop in activeButtonList)
            {
                Button button = (Button)pop.Child;
                buttonLoc.X = InkCanvas.GetLeft(button);
                buttonLoc.Y = InkCanvas.GetTop(button);
                buttonLoc.Size = button.RenderSize;

                if (buttonLoc.Contains(point))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return image for gate (not currently used)
        /// </summary>
        /// <param name="gate"></param>
        /// <returns></returns>
        private System.Windows.Media.ImageDrawing DrawGate(string gate, Rect bounds)
        {
            System.Windows.Media.ImageDrawing gateImage = new System.Windows.Media.ImageDrawing();
            string uriString = "Gate Images\\";
            uriString += gate;
            uriString += ".gif";
            gateImage.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + uriString));
            gateImage.Rect = bounds;
            return gateImage;
        }

        #endregion
    }
}
