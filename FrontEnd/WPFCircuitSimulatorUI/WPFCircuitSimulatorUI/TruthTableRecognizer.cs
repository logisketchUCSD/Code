using System;
using System.Collections.Generic;
using System.Text;

using SketchPanelLib;
using TruthTables;
using uiWorkPath;

namespace WPFCircuitSimulatorUI
{
    /// <summary>
    /// Truth Table Recognizer for Circuit Simulator.  Wraps
    /// the Truth Table recognition module for use with the 
    /// GUI.  
    /// </summary>
    public class TruthTableRecognizer : SketchRecognizer
    {
        /// <summary>
        /// Constructor.  Subscribes to the given
        /// SketchPanel and initializes this recognizer
        /// </summary>
        /// <param name="panel">The SketchPanel to which we want to subscribe.</param>
        public TruthTableRecognizer(SketchPanel panel)
        {
            SubscribeToPanel(panel);
        }

        public override RecognitionResult Recognize(RecognitionArgs args)
        {
            TruthTableRecognitionResult result = new TruthTableRecognitionResult();

            // Only recognizer on user triggered event
            if (!args.UserTriggered)
                return result;

            // Extract number of columns, if available
            // TEMP Removed because TruthTable.assign(int) is no longer available
            //int numCols = -1;
            //if (args is TruthTableRecognitionArgs)
            //{
            //    numCols = ((TruthTableRecognitionArgs)args).NumCols;
            //}

            // Run recognition and create result
            result.Sketch = args.Sketch;
            TruthTable ttRecognizer = new TruthTable(result.Sketch);

            //if (numCols == -1)
            //{
                result.DataMatrix = ttRecognizer.assign();
            //}
            //else
            //{
            //    result.DataMatrix = ttRecognizer.assign(numCols);
            //}

            


            result.DataPins = ttRecognizer.createPins(result.DataMatrix);
            result.LabelPins = ttRecognizer.outputLabels();
            result.NumCols = ttRecognizer.NumCols;
            result.NumRows = ttRecognizer.NumRows;
            result.DividerIndex = ttRecognizer.DivIndex;
            result.UserTriggered = args.UserTriggered;
            //result.Sketch = ttRecognizer.LabeledSketch;

            #region DEBUG

            /*Console.WriteLine("printing labels " + args.Sketch.LabelStrings.ToString());

            foreach (Sketch.Shape sh in result.Sketch.Shapes)
            {
                Console.WriteLine("sketch shape " + sh.XmlAttrs.Type);
                if (sh.XmlAttrs.Type.Equals("Divider"))
                {
                    foreach (Sketch.Substroke sub in sh.Substrokes)
                    {
                        sub.XmlAttrs.Color = System.Drawing.Color.Red.ToArgb();
                        Console.WriteLine("divider has substroke with props: " + sub.XmlAttrs.Id + "\n" + sub.Points[0]);
                    }
                }
            }

            foreach (Sketch.Substroke sub in result.Sketch.Substrokes)
            {
                if (sub.ParentShapes.Count == 0)
                {
                    Console.WriteLine("no parent shapes: " + sub.XmlAttrs.Id + "\n " + sub.Points[0]);
                }
                else if (sub.ParentShapes[0].XmlAttrs.Type.Equals("Divider"))
                {
                    Console.WriteLine("has a parent as divider: " + sub.XmlAttrs.Id + "\n " + sub.Points[0]);
                }
            }*/

            
            //
            // TEMP START Debug code from TruthTables/Debugging.cs
            //
            /*int cols = result.NumCols;
            Console.WriteLine("cols: " + cols);
            int rows = result.NumRows;
            Console.WriteLine("rows: " + rows);

            // print out the labels
            ttRecognizer.outputLabels();

            // put the data values into the matrix,
            // including the underscore between inputs and outputs
            if (rows == -1)
                return result;
            string[] list = new string[rows];
            int d = result.DividerIndex;
            Console.WriteLine("divider index: " + d);

            for (int j = 0; j < rows; j++)
            {
                for (int k = 0; k < cols; k++)
                {
                    if (k == d)
                    {
                        list[j] += "_"; // separate inputs from outputs
                    }
                    if (result.DataMatrix[j, k] == -1)
                    {
                        list[j] += "X";
                    }
                    else
                    {
                        list[j] += result.DataMatrix[j, k];
                    }
                }
            }

            // print out the matrix
            for (int u = 0; u < list.Length; u++)
            {
                Console.WriteLine(list[u]);
            }

            //
            // TEMP END Debug code from TruthTables/Debugging.cs
            //
            */

            #endregion

            return result;
        }
    }

    #region Results structure

    /// <summary>
    /// Result structure for TruthTableRecognizer
    /// </summary>
    public class TruthTableRecognitionResult : RecognitionResult
    {
        /// <summary>
        /// Matrix of Truth Table Data (e.g. the 1's and 0's 
        /// of the table)
        /// </summary>
        public MathNet.Numerics.LinearAlgebra.Matrix DataMatrix;

        /// <summary>
        /// 2 dimensional list of Truth Table Data (in Pin format).
        /// </summary>
        public List<List<Pins.Pin>> DataPins;

        /// <summary>
        /// List of input/output Labels.
        /// </summary>
        public List<Pins.Pin> LabelPins;

        /// <summary>
        /// The index of the verticle divider between input/output.
        /// This number represents how many columns of data there are
        /// before the vertical divider stroke.
        /// </summary>
        public int DividerIndex;

        /// <summary>
        /// Number of data rows recognized in truth table.
        /// </summary>
        public int NumRows;

        /// <summary>
        /// Number of data columns recognized in truth table
        /// </summary>
        public int NumCols;
    }

    #endregion

    #region Arguments structure

    /// <summary>
    /// Arguments structure for TruthTableRecognizer
    /// </summary>
    public class TruthTableRecognitionArgs : RecognitionArgs
    {
        /// <summary>
        /// Number of columns in truth table.  Used to prime
        /// truth table recognition when this data is available.
        /// Default value of -1 specifies that number of columns
        /// is unavailable.
        /// </summary>
        public int NumCols = -1;
    }

    #endregion
}
