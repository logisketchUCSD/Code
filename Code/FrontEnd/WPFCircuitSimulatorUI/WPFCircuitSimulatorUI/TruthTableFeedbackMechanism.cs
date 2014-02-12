using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using System.Drawing;
using SketchPanelLib;
using Sketch;
using System.Windows.Controls;

namespace WPFCircuitSimulatorUI
{
    /// <summary>
    /// TEMP Temporary feedback mechanism that draws a datagrid next
    /// to a truth table with the recognition result.
    /// </summary>
    public class TTDataGridFeedbackMechanism : FeedbackMechanism
    {
        #region Internals

        /// <summary>
        /// Current Truth Table Result being displayed
        /// </summary>
        private TruthTableRecognitionResult currentResult;

        /// <summary>
        /// The current DataGrid being displayed
        /// </summary>
        private DataGridView ttGrid;

        #endregion

        #region Constructors and Intialization

        /// <summary>
        /// Constructors
        /// </summary>
        public TTDataGridFeedbackMechanism()
            : base() { }

        public TTDataGridFeedbackMechanism(SketchPanel parentPanel)
            : base(parentPanel) { }

        /// <summary>
        /// <see cref="FeedbackMechanism.SubscribeToPanel"/>
        /// </summary>
        public override void SubscribeToPanel(SketchPanel parentPanel)
        {
            sketchPanel = parentPanel;

            sketchPanel.ResultReceived += new SketchPanelLib.RecognitionResultReceivedHandler(sketchPanel_ResultReceived);

            /*sketchPanel.ResultReceived += new RecognitionResultReceivedHandler(sketchPanel_ResultReceived);
            sketchPanel.InkPicture.MouseMove += new System.Windows.Forms.MouseEventHandler(InkPicture_MouseMove);
            sketchPanel.InkPicture.CursorInRange += new Microsoft.Ink.InkCollectorCursorInRangeEventHandler(InkPicture_CursorInRange);
            sketchPanel.InkPicture.Painted += new InkOverlayPaintedEventHandler(InkPicture_Painted);
            sketchPanel.InkPicture.StrokesDeleting += new InkOverlayStrokesDeletingEventHandler(InkPicture_StrokesDeleting);
            sketchPanel.ZoomEvent += new ZoomEventHandler(sketchPanel_ZoomEvent);
            sketchPanel.SketchFileLoaded += new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
             * */
        }

        /// <summary>
        /// <see cref="FeedbackMechanism.UnsubscribeFromPanel"/>
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            sketchPanel.ResultReceived -= new SketchPanelLib.RecognitionResultReceivedHandler(sketchPanel_ResultReceived);

            if (ttGrid != null)
            {
                sketchPanel.InkPicture.Controls.Remove(ttGrid);
            }

            ttGrid = null;
            currentResult = null;

            /*sketchPanel.ResultReceived -= new RecognitionResultReceivedHandler(sketchPanel_ResultReceived);
            sketchPanel.InkPicture.MouseMove -= new System.Windows.Forms.MouseEventHandler(InkPicture_MouseMove);
            sketchPanel.InkPicture.CursorInRange -= new Microsoft.Ink.InkCollectorCursorInRangeEventHandler(InkPicture_CursorInRange);
            sketchPanel.InkPicture.Painted -= new InkOverlayPaintedEventHandler(InkPicture_Painted);
            sketchPanel.InkPicture.StrokesDeleting -= new InkOverlayStrokesDeletingEventHandler(InkPicture_StrokesDeleting);
            sketchPanel.ZoomEvent -= new ZoomEventHandler(sketchPanel_ZoomEvent);
            sketchPanel.SketchFileLoaded -= new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
             * */
        }

        #endregion



        private void clear()
        {
            if (ttGrid != null)
            {
                sketchPanel.InkCanvas.Controls.Remove(ttGrid);
            }

            ttGrid = null;
            currentResult = null;
        }

        /// <summary>
        /// TEMP Display result when possible
        /// </summary>
        private void sketchPanel_ResultReceived(SketchPanelLib.RecognitionResult result)
        {
            // Get the result
            TruthTableRecognitionResult ttResult;
            if (result is TruthTableRecognitionResult && result.UserTriggered)
                ttResult = (TruthTableRecognitionResult)result;
            else
                return; // ignore other results

            // Clear display
            clear();

            // Validate result as displayable; display error message otherwise
            if (ttResult.NumCols > 1 && // Need at two columns
                ttResult.NumRows > 0 && // Need at least one row
                ttResult.LabelPins.Count == ttResult.NumCols) // Need # labels == # columns
            {
                // Display the result
                currentResult = ttResult;
                displayResult();
            }
            else
            {
                System.Windows.MessageBox.Show("Error: Truth table could not be recognized.  " +
                    "Please ensure that truth table is drawn according to necessary conventions");
            }
        }

        /// <summary>
        /// TEMP draw DataGrid near truth table result
        /// </summary>
        private void displayResult()
        {
            Rectangle inkBoundingBox = sketchPanel.InkPicture.Ink.Strokes.GetBoundingBox();
            System.Drawing.Point drawPoint = new System.Drawing.Point(inkBoundingBox.Right + 200, inkBoundingBox.Top);

            Microsoft.Ink.InkPicture inkPic = sketchPanel.InkPicture;
            using (Graphics g = inkPic.CreateGraphics())
            {
                inkPic.Renderer.InkSpaceToPixel(g, ref drawPoint);
            }


            int gridWidth = inkBoundingBox.Width;
            int gridHeight = inkBoundingBox.Height;

            ttGrid = new DataGridView();
            ttGrid.DefaultCellStyle.Font = new Font(ttGrid.DefaultCellStyle.Font.FontFamily, 18.0F);
            ttGrid.Location = drawPoint;
            ttGrid.ColumnCount = currentResult.NumCols;
            ttGrid.ColumnHeadersVisible = false;
            ttGrid.RowHeadersVisible = false;

            for (int i = 0; i < currentResult.NumRows + 1; ++i)
            {
                string[] row = new string[currentResult.NumCols];
                for (int j = 0; j < currentResult.NumCols; ++j)
                {
                    if (i == 0)
                    {
                        row[j] = currentResult.LabelPins[j].PinName;
                    }
                    else
                    {
                        row[j] = currentResult.DataMatrix[i - 1, j].ToString();
                    }
                }
                ttGrid.Rows.Add(row);
            }

            ttGrid.Enabled = true;
            sketchPanel.InkPicture.Controls.Add(ttGrid);

            ttGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            ttGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            ttGrid.AutoResizeColumns();
            ttGrid.AutoResizeRows();

            ttGrid.Refresh();

            int colWidth = 0;
            int rowHeight = 0;

            for (int c = 0; c < ttGrid.ColumnCount; ++c)
            {
                colWidth += ttGrid.Columns[c].Width;
                if (c < currentResult.DividerIndex)
                {
                    ttGrid.Columns[c].DefaultCellStyle.BackColor = Color.LightCoral;
                }
                else
                {
                    ttGrid.Columns[c].DefaultCellStyle.BackColor = Color.LightBlue;
                }
            }

            for (int r = 0; r < ttGrid.RowCount; ++r)
            {
                rowHeight += ttGrid.Rows[r].Height;
            }

            ttGrid.Size = new Size(colWidth+30, rowHeight+30);
            
            ttGrid.Refresh();

        }

    }

    /// <summary>
    /// TEMP Temporary feedback mechanism that colors truth table 
    /// elements based upon a domain and displays tooltip labels
    /// when the user hovers the stylus
    /// </summary>
    public class TooltipFeedbackMechanism : FeedbackMechanism
    {
        private ToolTip toolTip;

        private Hashtable ink2sketchLabelTable = new Hashtable();

        private bool enabled;

        /// <summary>
        /// Constructors
        /// </summary>
        public TooltipFeedbackMechanism()
            : base() { enabled = true; }

        public TooltipFeedbackMechanism(SketchPanel parentPanel)
            : base(parentPanel) { enabled = true; }

        public override void SubscribeToPanel(SketchPanel parentPanel)
        {
            base.SubscribeToPanel(parentPanel);

            toolTip = new ToolTip();
            toolTip.InitialDelay = 100;
            sketchPanel.InkPicture.MouseMove += new MouseEventHandler(InkPicture_MouseMove);
            sketchPanel.SketchFileLoaded += new SketchFileLoadedHandler(enableMe);
            sketchPanel.ResultReceived += new RecognitionResultReceivedHandler(sketchPanel_ResultReceived);
        }

        void sketchPanel_ResultReceived(RecognitionResult result)
        {
            enableMe();
        }

        void enableMe()
        {
            enabled = true;
        }

        public override void UnsubscribeFromPanel()
        {
            sketchPanel.InkPicture.MouseMove -= new MouseEventHandler(InkPicture_MouseMove);
        }

        void InkPicture_MouseMove(object sender, MouseEventArgs e)
        {
            if (!enabled)
                return;

            // Get the current mouse position and convert it into InkSpace
            System.Drawing.Point mousePt = new System.Drawing.Point(e.X, e.Y);
            Graphics graphics = sketchPanel.InkPicture.CreateGraphics();
            sketchPanel.InkPicture.Renderer.PixelToInkSpace(graphics, ref mousePt);

            // Get the Microsoft Stroke closest to the mouse pointer
            float strokePt, distance;
            Microsoft.Ink.Stroke closestMSubstroke = sketchPanel.InkPicture.Ink.NearestPoint(mousePt, out strokePt, out distance);

            if (closestMSubstroke == null)
                return;


            // Fire tooltip if the mouse pointer is close enough
            if (distance < MeshHighlightingFeedback.MeshHighlightDistanceThreshold)
            {
                // Get the stroke's label, if it has one
                Substroke sub = sketchPanel.InkSketch.GetSketchSubstrokeByInkId(closestMSubstroke.Id);

                if (sub == null)
                    return;
                
                string label = sub.FirstLabel;

//Console.WriteLine(label);
                /*foreach (Shape sh in sub.ParentShapes)
                {
                    Console.WriteLine(sh.XmlAttrs.Type);
                }*/

                if (label == null || label.Equals("unlabeled"))
                    return;

                // HACK!!
                /*if (label.Equals("Label"))
                {
                    label = sub.ParentShapes[0].XmlAttrs.Text;

                    /*foreach (Shape sh in sub.ParentShapes)
                    {
                        if (!sh.XmlAttrs.Type.Equals("Label"))
                            label = sh.XmlAttrs.Type;
                    }
                }*/


                // Show the ToolTip
                this.toolTip.SetToolTip(sketchPanel.InkPicture, label);
                this.toolTip.Active = true;
            }
            else
            {
                // Don't show the ToolTip if the mouse pointer is not close
                this.toolTip.Active = false;
            }
        }

        #region Properties

        /// <summary>
        /// Gets or sets whether this feedback mechanism will fire ToolTips.
        /// </summary>
        public bool Enabled
        {
            set
            {
                enabled = value;
            }

            get
            {
                return enabled;
            }
        }

        #endregion
    }

    /// <summary>
    /// TEMP
    /// HACK
    /// </summary>
    public class TextReplacementFeedbackMechanism : FeedbackMechanism
    {
        /// <summary>
        /// Constructors
        /// </summary>
        public TextReplacementFeedbackMechanism()
            : base() { }

        public TextReplacementFeedbackMechanism(SketchPanel parentPanel)
            : base(parentPanel) { }

        private Dictionary<int, int> strokes2LabelIDs;
        private List<Label> labels;

        /// <summary>
        /// <see cref="FeedbackMechanism.SubscribeToPanel"/>
        /// </summary>
        public override void SubscribeToPanel(SketchPanel parentPanel)
        {
            base.SubscribeToPanel(parentPanel);

            labels = new List<Label>();

            sketchPanel.ResultReceived += new RecognitionResultReceivedHandler(sketchPanel_ResultReceived);
            sketchPanel.SketchFileLoaded += new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
            sketchPanel.InkCanvas.StrokeErasing += new System.Windows.Controls.InkCanvasStrokeErasingEventHandler(InkCanvas_StrokesDeleting);
        }

        /// <summary>
        /// <see cref="FeedbackMechanism.UnsubscribeFromPanel"/>
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            sketchPanel.ResultReceived -= new RecognitionResultReceivedHandler(sketchPanel_ResultReceived);
            sketchPanel.SketchFileLoaded -= new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
            sketchPanel.InkCanvas.StrokeErasing -= new System.Windows.Controls.InkCanvasStrokeErasingEventHandler(InkCanvas_StrokesDeleting);
        }

        /// <summary>
        /// Attempts to color the Ink strokes using the domain when a file is loaded
        /// </summary>
        private void sketchPanel_SketchFileLoaded()
        {
            replaceStrokes();
        }

        /// <summary>
        /// Colors the Ink strokes when a recognition result is received.
        /// </summary>
        private void sketchPanel_ResultReceived(RecognitionResult result)
        {
            strokes2LabelIDs = new Dictionary<int, int>();
            foreach (System.Windows.Controls.Label l in labels)
            {
                sketchPanel.InkCanvas.Children.Remove(l);
                l.Enabled = false;
                l.Visible = false;
                l.Dispose();
            }
            labels = new List<Label>();

            replaceStrokes();
        }

        /// <summary>
        /// HACK TEMP replaces shapes
        /// </summary>
        protected virtual void replaceStrokes()
        {
            // Create labels
            ///xxxList<Microsoft.Ink.Stroke> strokes2Delete = new List<Microsoft.Ink.Stroke>();
            foreach (Shape sh in sketchPanel.InkSketch.Sketch.Shapes)
            {
                if (!sh.XmlAttrs.Type.Equals("Label")
                    && !sh.XmlAttrs.Type.Equals("HorizontalDivider")
                    && !sh.XmlAttrs.Type.Equals("VerticalDivider")
                    && !sh.XmlAttrs.Type.Equals("Divider")
                    && !sh.XmlAttrs.Type.Contains("I"))
                {
                    Label l = new Label();
                    if (sh.XmlAttrs.Type.Equals("True"))
                    {
                        l.Text = "1";
                    }
                    else if (sh.XmlAttrs.Type.Equals("False"))
                    {
                        l.Text = "0";
                    }
                    // TODO don't cares
                    else
                    {
                        l.Text = sh.XmlAttrs.Type;
                    }
                    System.Drawing.Point loc = new System.Drawing.Point((int)sh.XmlAttrs.X, (int)sh.XmlAttrs.Y);
                    using (Graphics g = sketchPanel.InkPicture.CreateGraphics())
                    {
                        sketchPanel.InkPicture.Renderer.InkSpaceToPixel(g, ref loc);
                    }
                    l.Location = loc;
                    l.AutoSize = true;
                    l.Width = (int)sh.XmlAttrs.Width;
                    l.Height = (int)sh.XmlAttrs.Height;
                    l.Font = new Font(l.Font.FontFamily, (int)(sh.XmlAttrs.Height/50.0));
                    l.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                    l.Enabled = true;
                    sketchPanel.InkPicture.Controls.Add(l);
                    l.BringToFront();

                    labels.Add(l);

                    int labelID = labels.Count - 1;

                    foreach (Substroke sub in sh.Substrokes)
                    {
                        Microsoft.Ink.Stroke iStroke = sketchPanel.InkSketch.GetInkStrokeBySubstrokeId(sub.XmlAttrs.Id);
                        if (iStroke != null)
                        {
                            //strokes2LabelIDs.Add(iStroke.Id, labelID);
                        }
                    }
                }
            }

            // xxxDelete strokes


            sketchPanel.InkCanvas.InvalidateVisual();
        }

        void InkCanvas_StrokesDeleting(object sender, System.Windows.Controls.InkCanvasStrokeErasingEventArgs e)
        {
            // Nothing for now
        }

    }
}
