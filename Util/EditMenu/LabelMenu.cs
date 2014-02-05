using System;
using System.Collections.Generic;

using System.Windows.Input;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Ink;
using CommandManagement;
using System.Windows.Media;
using Domain;



namespace EditMenu
{
    public delegate void ApplyLabelEventHandler(List<Sketch.Shape> shapes);
    public delegate void ErrorCorrectedEventHandler(Sketch.Shape shape);

	/// <summary>
	/// Summary description for LabelMenu.
	/// </summary>
	public class LabelMenu : System.Windows.Controls.Primitives.Popup
    {
        #region Internals

        private const int FONT_SIZE = 11;
		
		private CommandManagement.CommandManager CM;

        private SketchPanelLib.SketchPanel sketchPanel;

        public event ApplyLabelEventHandler applyLabel;

        public event ErrorCorrectedEventHandler errorCorrected;

        public event EditMenuFinishedEventHandler LabelMenuFinished;

        private ListBox labelList;

        #endregion

        #region Constructor

        public LabelMenu(ref SketchPanelLib.SketchPanel SP, CommandManagement.CommandManager CM)
		{
			InitializeComponent();

			// To know what strokes are selected
			sketchPanel = SP;
			this.CM = CM;

			// Add the panel to the main popup
            Child = labelList;
            PlacementTarget = sketchPanel.InkCanvas;
            Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint;
            Visibility = System.Windows.Visibility.Visible;
            IsOpen = false;

            // Make everything but the buttons invisible
            AllowsTransparency = true;
            labelList.BorderBrush = Brushes.Transparent;
            labelList.Background = Brushes.Transparent;
		}

        #endregion

        #region Initialize and Update

        private void InitializeComponent()
        {
            labelList = new ListBox();
            labelList.Name = "labelList";
        }

        public void LoadLabels()
		{
            labelList.Items.Clear();
			
            List<ShapeType> labels = LogicDomain.Types;
            foreach (ShapeType type in labels)
            {
                if (type != new ShapeType())
                {
                    // Add the button corresponding to that label
                    if (type == LogicDomain.SUBCIRCUIT)
                        continue;
                    Button button = new Button();
                    button.Content = type.Name;
                    button.Tag = type;
                    button.FontSize = FONT_SIZE;
                    button.Foreground = new SolidColorBrush(type.Color);
                    button.Width = FONT_SIZE * type.Name.Length / 1.5;
                    button.Height = FONT_SIZE * 2;
                    button.Padding = new Thickness(0.0);

                    this.labelList.Items.Add(button);
                    button.Click += new RoutedEventHandler(labelList_Checked);
                }
            }

            double max_width = 0;
            foreach (UIElement button in labelList.Items)
                max_width = Math.Max(max_width, ((Button)button).Width);
            max_width += 5;

            foreach (UIElement button in labelList.Items)
                ((Button)button).Width = max_width;

            // Resize the containers, adding padding to prevent scrolling
            this.Width = ((Button)labelList.Items[0]).Width + 10;
            this.Height = ((Button)labelList.Items[0]).Height * labelList.Items.Count + 10;
        }

		/// <summary>
		/// Gets label data for a given group of strokes
        /// Precondition: Some strokes are selected (not necessarily for one Shape)
		/// </summary>
		public void UpdateLabelMenu(System.Windows.Ink.StrokeCollection selection)
		{
            // Clear the old selection
            foreach (Button button in this.labelList.Items)
                button.Background = new SolidColorBrush(Colors.White);

            // Record the shape type for each stroke
            List<Domain.ShapeType> possibleShapes = new List<Domain.ShapeType>();
            foreach (Stroke stroke in selection)
            {
                Sketch.Substroke currSubstroke = sketchPanel.InkSketch.GetSketchSubstrokeByInk(stroke);

                Sketch.Shape parentShape = currSubstroke.ParentShape;
                if (parentShape != null)
                {
                    if (!possibleShapes.Contains(parentShape.Type))
                        possibleShapes.Add(parentShape.Type);
                }
            }
            // If all of the strokes have the same label and that label is valid, indicate that label by shading it gray
            if (possibleShapes.Count == 1 && !(possibleShapes[0] == null || possibleShapes[0] == new Domain.ShapeType()))
            {
                foreach (Button button in this.labelList.Items)
                {
                    if ((string)button.Content == possibleShapes[0].Name)
                        button.Background = new SolidColorBrush(Colors.LightGray);
                }
            }
            return;
		}

        #endregion

        #region Events
		
		private void labelList_Checked(object sender, RoutedEventArgs e)
		{
            // Close the label menu
            this.IsOpen = false;

            // The user should know that we're thinking
            sketchPanel.UseCustomCursor = true;
            sketchPanel.Cursor = System.Windows.Input.Cursors.Wait;

            // Apply the label with the given type
            Button labelButton = (Button)sender;
            ApplyLabelToSelection((ShapeType)labelButton.Tag);

            // Restore the cursor
            sketchPanel.UseCustomCursor = false;
            sketchPanel.Cursor = System.Windows.Input.Cursors.Pen;
        }

		private void labelList_MouseEnter(object sender, MouseEventArgs e)
		{
            this.labelList.Focus();
		}

        #endregion

        #region Helpers

        public void ApplyLabelToSelection(ShapeType label)
        {
            if (this.sketchPanel.InkCanvas.GetSelectedStrokes().Count > 0)
            {
                // Apply the label
                CommandList.ApplyLabelCmd applyLabelCmd = new CommandList.ApplyLabelCmd(sketchPanel,
                    sketchPanel.InkCanvas.GetSelectedStrokes(), label);
                applyLabelCmd.Regroup += new CommandList.RegroupEventHandler(applyLabel);
                applyLabelCmd.ErrorCorrected += new CommandList.ErrorCorrectedEventHandler(errorCorrected);
                CM.ExecuteCommand(applyLabelCmd);
            }

            if (LabelMenuFinished != null)
                LabelMenuFinished();
        }

        #endregion
    }
}
