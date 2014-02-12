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

namespace SimulationManager
{
    #region Events

    // An event to enable simulation of highlighted entries
    public delegate void RowHighlightEventHandler(Dictionary<Sketch.Shape, int> inputs);

    // An event to enable highlighting of corresponding input/output
    public delegate void HighlightEventHandler(Sketch.Shape shape);

    // An event to enable unhighlighting of corresponding input/output
    public delegate void UnhighlightEventHandler(Sketch.Shape shape);

    // An event to enable relabeling of input and outputs
    public delegate void RelabelStrokesEventHandler(Dictionary<Sketch.Shape, string> newNameDict);

    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TruthTableWindow : Window
    {
        #region Internals

        ///<summary>
        /// List on input and output names
        ///</summary>
        private List<Sketch.Shape> Headers;

        /// <summary>
        /// Dictionary of input and output strings
        /// </summary>
        private List<List<int>> Rows;

        /// <summary>
        /// Number of input entries
        /// </summary>
        private int numInputs;

        /// <summary>
        /// Number of output entries
        /// </summary>
        private int numOutputs;

        /// <summary>
        /// Timer for hovering and to trigger simulation and timer interval
        /// </summary>
        private System.Windows.Forms.Timer rowHoverTimer;
        private const int HOVER_INTERVAL = 1;

        /// <summary>
        /// Whether or not to simulate the row that is highlighted
        /// </summary>
        private bool simulate;

        /// <summary>
        /// Current inputs to simulate for when timer goes off
        /// </summary>
        private Dictionary<Sketch.Shape, int> currInputs;

        /// <summary>
        /// Maps columns of the truth table to the shapes they represent
        /// </summary>
        private Dictionary<TableColumn, Sketch.Shape> columnsToShapes;

        /// <summary>
        /// Our event for highlight simulation
        /// </summary>
        public event RowHighlightEventHandler SimulateRow;

        /// <summary>
        /// Our event for highlighting input/output strokes
        /// </summary>
        public event HighlightEventHandler Highlight;

        /// <summary>
        /// Our event for unhighlighting input/output strokes
        /// </summary>
        public event UnhighlightEventHandler UnHighlight;

        /// <summary>
        /// Our event for renaming
        /// </summary>
        public event RelabelStrokesEventHandler RelabelStrokes;

        #endregion

        #region Constructor and Initialization

        public TruthTableWindow(CircuitSimLib.TruthTable truthTable)
            : this(truthTable.TruthTableHeader, truthTable.TruthTableOutputs, truthTable.Inputs.Count)
        {
        }

        public TruthTableWindow(List<Sketch.Shape> headers, List<List<int>> rows, int inputs)
        {
            columnsToShapes = new Dictionary<TableColumn, Sketch.Shape>();
            InitializeComponent();

            // Set table properties
            this.Headers = headers;
            this.Rows = rows;
            this.numInputs = inputs;
            this.numOutputs = headers.Count - inputs;
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            // Create truth table and hover timer
            CreateTruthTable();
            simulate = true;
            this.rowHoverTimer = new System.Windows.Forms.Timer();
            this.rowHoverTimer.Interval = HOVER_INTERVAL;
            this.rowHoverTimer.Tick += new EventHandler(rowHoverTimer_Tick);
        }

        /// <summary>
        /// Creating a truth table window from a project. This doesn't allow the simulation options
        /// that the table usually provides, so no events are registered
        /// This is for the viewing of subcircuits
        /// </summary>
        /// <param name="project">Project to display the truth table of</param>
        public TruthTableWindow(Sketch.Project project, string title)
        {
            InitializeComponent();
            this.Title += " - " + title;
            // Set table properties
            this.Rows = SortedLists(project.behavior);
            this.numInputs = project.inputs.Count;
            this.numOutputs = project.outputs.Count;
            InputDock.Visibility = System.Windows.Visibility.Hidden;
            CheckBox.Visibility = System.Windows.Visibility.Hidden;

            // Create truth table and hover timer
            TruthTable.FontFamily = new FontFamily("Verdana");
            TruthTable.CellSpacing = 0;
            TableRowGroup tableRowGroup = new TableRowGroup();
            TruthTable.RowGroups.Add(tableRowGroup);

            int index = 0;

            // Create the Header Row
            TableRow HeaderRow = new TableRow();
            List<string> header = new List<string>();
            header.AddRange(project.inputs.Concat(project.outputs));
            foreach (string head in header)
            {
                index++;
                // Add a column with the name of the input
                TableColumn Column = new TableColumn();
                Column.Width = new GridLength((FlowDocReader.ActualWidth) / header.Count);
                if (FlowDocReader.ActualWidth > 50)
                    Column.Width = new GridLength((FlowDocReader.ActualWidth - 50) / header.Count);
                Column.Name = head;
                TruthTable.Columns.Add(Column);
                TableCell headerCell = new TableCell(new Paragraph(new Run(head)));
                headerCell.TextAlignment = TextAlignment.Center;

                // Color input and output objects differently to disinguish
                if (index > numInputs)
                    headerCell.Foreground = System.Windows.Media.Brushes.IndianRed;
                else
                    headerCell.Foreground = System.Windows.Media.Brushes.Navy;
                headerCell.BorderThickness = new Thickness(0, 0, 0, 1);
                headerCell.BorderBrush = System.Windows.Media.Brushes.Black;
                HeaderRow.Cells.Add(headerCell);
            }

            TruthTable.RowGroups[0].Rows.Add(HeaderRow);

            // Create the rest of the table
            foreach (List<int> row in Rows)
            {
                TableRow tableRow = new TableRow();
                int count = 1;
                foreach (int i in row)
                {
                    TableCell intCell = new TableCell(new Paragraph(new Run(i.ToString())));
                    intCell.TextAlignment = TextAlignment.Center;

                    // Create a border between inputs and outputs
                    intCell.BorderThickness = new Thickness(0, 0, 0, 0);
                    if (count == numInputs)
                        intCell.BorderThickness = new Thickness(0, 0, 1, 0);
                    intCell.BorderBrush = System.Windows.Media.Brushes.Black;

                    tableRow.Cells.Add(intCell);
                    count++;
                }
                TruthTable.RowGroups[0].Rows.Add(tableRow);
            }
        }
        /// <summary>
        /// Since Dictionaries are not sorted, this sorts the behavior.
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        private List<List<int>> SortedLists(Dictionary<List<int>, List<int>> behavior)
        {
            List<List<int>> sorted = new List<List<int>>();
            int index = 0;
            while (sorted.Count != behavior.Count)
            {
                List<int> row = new List<int>();
                foreach (List<int> inputs in behavior.Keys)
                {
                    int binary = 0;
                    foreach(int input in inputs)
                    {
                        binary *= 10;
                        binary += input;
                    }
                    String binaryString = binary.ToString();
                    int inputValue = (int)Convert.ToInt64(binaryString, 2);
                    if (inputValue == index)
                    {
                        index++;
                        List<int> newRow = new List<int>();
                        newRow.AddRange(inputs);
                        newRow.AddRange(behavior[inputs]);
                        sorted.Add(newRow);
                        break;
                    }
                }
            }
            return sorted;
        }

        /// <summary>
        /// Enters the entry into the window menu
        /// </summary>
        private void CreateTruthTable()
        {
            TruthTable.FontFamily = new FontFamily("Verdana");
            TruthTable.CellSpacing = 0;
            TableRowGroup tableRowGroup = new TableRowGroup();
            TruthTable.RowGroups.Add(tableRowGroup);

            int index = 0;

            // Create the Header Row
            TableRow HeaderRow = new TableRow();
            foreach (Sketch.Shape shape in Headers)
            {
                index++;
                // Add a column with the name of the input
                TableColumn Column = new TableColumn();
                columnsToShapes[Column] = shape;
                Column.Width = new GridLength((FlowDocReader.ActualWidth)/Headers.Count);
                if (FlowDocReader.ActualWidth > 50)
                    Column.Width = new GridLength((FlowDocReader.ActualWidth-50) / Headers.Count);
                Column.Name = shape.Name;
                TruthTable.Columns.Add(Column);
                TableCell headerCell = new TableCell(new Paragraph(new Run(shape.Name)));
                headerCell.TextAlignment = TextAlignment.Center;

                // Color input and output objects differently to disinguish
                if (index > numInputs)
                    headerCell.Foreground = System.Windows.Media.Brushes.IndianRed;
                else
                    headerCell.Foreground = System.Windows.Media.Brushes.Navy;
                headerCell.BorderThickness = new Thickness(0, 0, 0, 1);
                headerCell.BorderBrush = System.Windows.Media.Brushes.Black;
                HeaderRow.Cells.Add(headerCell);
                
                // Hook into events
                headerCell.StylusEnter += new StylusEventHandler(header_StylusEnter);
                headerCell.StylusLeave += new StylusEventHandler(header_StylusLeave);
                headerCell.StylusDown += new StylusDownEventHandler(header_StylusDown);
            }

            TruthTable.RowGroups[0].Rows.Add(HeaderRow);
        
            // Create the rest of the table
            foreach (List<int> row in Rows)
            {
                TableRow tableRow = new TableRow();
                int count = 1;
                foreach (int i in row)
                {
                    TableCell intCell = new TableCell(new Paragraph(new Run(i.ToString())));
                    intCell.TextAlignment = TextAlignment.Center;

                    // Create a border between inputs and outputs
                    intCell.BorderThickness = new Thickness(0, 0, 0, 0);
                    if ( count == numInputs)
                        intCell.BorderThickness = new Thickness(0, 0, 1, 0);
                    intCell.BorderBrush = System.Windows.Media.Brushes.Black;

                    tableRow.Cells.Add(intCell);
                    count++;
                }
                TruthTable.RowGroups[0].Rows.Add(tableRow);

                // Hook into row events for highlight simulation
                tableRow.StylusEnter += new StylusEventHandler(Row_StylusEnter);
                tableRow.StylusLeave += new StylusEventHandler(Row_StylusLeave);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Highlights the row the pen is above and sets current inputs for simulation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Row_StylusEnter(object sender, StylusEventArgs e)
        {
            rowHoverTimer.Start();

            // Highlight rows
            TableRow row = (TableRow)e.Source;
            row.Background = System.Windows.Media.Brushes.SlateGray;
            row.Foreground = System.Windows.Media.Brushes.White;
            
            // Check if we are simulating highlighted inputs
            if (!simulate)
                return;

            // Create the list of inputs
            Dictionary<Sketch.Shape, int> inputs = new Dictionary<Sketch.Shape, int>();
            for (int i = 0; i < numInputs; i++)
            {
                Paragraph para = (Paragraph)row.Cells[i].Blocks.LastBlock;
                Run run = (Run)para.Inlines.FirstInline;
                inputs.Add(columnsToShapes[TruthTable.Columns[i]], System.Convert.ToInt32(run.Text));
            }
            currInputs = inputs;
        }

        /// <summary>
        /// Unhighlights a row when the pen leaves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Row_StylusLeave(object sender, StylusEventArgs e)
        {
            rowHoverTimer.Stop();

            // Unhighlight the row
            TableRow row = (TableRow)e.Source;
            row.Background = System.Windows.Media.Brushes.Transparent;
            row.Foreground = System.Windows.Media.Brushes.Black;
        }

        /// <summary>
        /// Get the table column associated with the given cell
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private TableColumn columnForCell(TableCell cell)
        {
            foreach (TableRowGroup rowGroup in TruthTable.RowGroups)
            {
                foreach (TableRow row in rowGroup.Rows)
                {
                    int i = 0;
                    foreach (TableCell testCell in row.Cells)
                    {
                        if (testCell == cell)
                            return TruthTable.Columns[i];
                        i++;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Highlights the corresponding strokes of the input/output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void header_StylusEnter(object sender, StylusEventArgs e)
        {
            TableCell cell = (TableCell)e.Source;
            cell.Background = cell.Foreground;
            cell.Foreground = Brushes.White;

            if (Highlight != null)
            {
                Highlight(columnsToShapes[columnForCell(cell)]);
            }
        }

        /// <summary>
        /// Unhighlights the corresponding strokes of the input/output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void header_StylusLeave(object sender, StylusEventArgs e)
        {
            TableCell cell = (TableCell)e.Source;
            cell.Foreground = cell.Background;
            cell.Background = Brushes.Transparent;

            if (UnHighlight != null)
            {
                UnHighlight(columnsToShapes[columnForCell(cell)]);
            }
        }


        /// <summary>
        /// Brings up an editing box for recognized input/output names
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void header_StylusDown(object sender, StylusDownEventArgs e)
        {
            ReplaceNamesDialog replaceNamesDialog = new ReplaceNamesDialog(this.Headers);
            replaceNamesDialog.Show();
            replaceNamesDialog.ReplaceNames += new ReplaceNamesEventHandler(replaceNamesDialog_ReplaceNames);
        }

        /// <summary>
        /// Calls the event to simulate the inputs on the sketch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rowHoverTimer_Tick(object sender, EventArgs e)
        {
            rowHoverTimer.Stop();

            if (simulate && SimulateRow!=null)
                SimulateRow(currInputs);
        }

        /// <summary>
        /// Updates whether or not we are simulating highlighted rows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimulateCheckedChanged(object sender, RoutedEventArgs e)
        {
            CheckBox simulateBox = (CheckBox)e.Source;
            simulate = (bool)simulateBox.IsChecked;
            System.Console.WriteLine(simulate);
        }


        /// <summary>
        /// Allows users to enter a string of inputs to simulate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Clicked(object sender, RoutedEventArgs e)
        {
            string values = InputString.Text;
            if (values.Count() != numInputs)
            {
                MessageBox.Show("Number of inputs does not match string length.");
                return;
            }

            // Go through our string and enter values into our input dictionary
            Dictionary<Sketch.Shape, int> newInputs = new Dictionary<Sketch.Shape, int>();
            for (int index = 0; index < numInputs; index++)
            {
                // Convert our char into the int represented
                try
                {
                    int value = System.Convert.ToInt32(values[index].ToString());
                    if (value != 0 && value != 1)
                    {
                        MessageBox.Show("Inputs not valid.");
                        return;
                    }
                    newInputs.Add(columnsToShapes[TruthTable.Columns[index]], value);
                }
                catch
                {
                    MessageBox.Show("Inputs not valid. Please use 0's and 1's");
                    return;
                }

                
            }

            // Call the event
            if (SimulateRow != null)
            {
                SimulateRow(newInputs);
            }
        }

        private void EnterKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OKButton_Clicked(sender, e);
        }

        /// <summary>
        /// Updates the Truth Table headers and also alerts the dictionaries in
        /// SimulationManager
        /// </summary>
        /// <param name="newNameDict">Dictionary of old name keys to new name values</param>
        private void replaceNamesDialog_ReplaceNames(Dictionary<Sketch.Shape, string> newNameDict)
        {
            int index = 0;
            foreach (TableColumn col in TruthTable.Columns)
            {
                Sketch.Shape shape = columnsToShapes[col];
                // If the name has been changed, update the paragraph and column name
                if (newNameDict.ContainsKey(shape))
                {
                    //I guess the names have to be letters, so we have to change it back if it fails
                    string oldname = col.Name;
                    try
                    {
                        col.Name = newNameDict[shape];
                        Block newBlock = new Paragraph(new Run(newNameDict[shape]));
                        TruthTable.RowGroups[0].Rows[0].Cells[index].Blocks.Clear();
                        TruthTable.RowGroups[0].Rows[0].Cells[index].Blocks.Add(newBlock);
                    }
                    catch
                    {
                        MessageBox.Show("That's not a valid name, try using letters", "invalid name");
                        col.Name = oldname;
                        return;
                    }
                }
                index++;
            }

            RelabelStrokes(newNameDict);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FlowDocReader.Height = Math.Abs(Grid.ActualHeight - 55);

            if (FlowDocReader.Width != Grid.ActualWidth)
            {
                foreach (TableColumn Column in TruthTable.Columns)
                {
                    Column.Width = new GridLength((FlowDocReader.ActualWidth) / (numInputs + numOutputs));
                    if (FlowDocReader.ActualWidth > 50)
                        Column.Width = new GridLength((FlowDocReader.ActualWidth - 50) / (numInputs + numOutputs));
                }
                FlowDocReader.Width = Grid.ActualWidth;
            }

        }

        #endregion
    }
}
