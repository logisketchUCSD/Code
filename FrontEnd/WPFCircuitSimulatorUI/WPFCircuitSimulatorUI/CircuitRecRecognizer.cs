using System;
using System.Collections.Generic;
using System.Text;

using SketchPanelLib;
using ConverterXML;
using CircuitRec;

namespace WPFCircuitSimulatorUI
{
    /// <summary>
    /// Recognizer that runs CircuitRec on a labeled sketch.  Returns the 
    /// original sketch and the CircuitRec instance that contains circuit
    /// information deduced from the sketch.  Optionally runs on a hand-labeled
    /// test sketch.
    /// 
    /// All sketches that this recognizer processes must be already labeled.
    /// </summary>
    public class CircuitRecRecognizer : SketchRecognizer
    {
        /// <summary>
        /// DEBUG
        /// 
        /// Path to default sample result sketch.
        /// File is located in output directory.
        /// </summary>
        public static string TestSketchFilePath =
            AppDomain.CurrentDomain.BaseDirectory + @"\CircuitRecTestSketch.xml";

        /// <summary>
        /// DEBUG
        /// 
        /// True iff this recognizer should read TestSketchFilePath instead
        /// of the given recognition argument sketch.
        /// </summary>
        public bool ReadFromFile = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public CircuitRecRecognizer()
            : base()
        {
        }

        /// <summary>
        /// Constructor.  Subscribes to the given
        /// SketchPanel. 
        /// </summary>
        /// <param name="panel">The SketchPanel to which we want to subscribe.</param>
        public CircuitRecRecognizer(SketchPanel panel)
            : base(panel)
        {
        }

        /// <summary>
        /// Runs CircuitRec on a sketch (optionally a the test sketch located at TestSketchFilePath).  
        /// Retrieves a CircuitRec instance that contains the following data:
        ///    * Map of substroke Ids to circuit elements (e.g. symbols, wires, and labels)
        ///    * Endpoints
        ///    * List of Parse error (if any)
        ///    * Text recognition results of recognizing labels
        /// 
        /// Precondition: args contains a labeled sketch; exceptions will occur (and be caught)
        /// in CircuitRec if the labels are illogical or missing.  This is never called?
        /// </summary>
        public override RecognitionResult Recognize(RecognitionArgs args)
        {
            // Get sketch to recognize
            Sketch.Sketch resultSketch;

            // DEBUG
            if (ReadFromFile)
            {
                resultSketch = ((new ReadXML(TestSketchFilePath)).Sketch);
            }
            else
            {
                resultSketch = args.Sketch;
            }

            // Init CircuitRec
            CircuitRecResult result = new CircuitRecResult();
            Domain domain = new Domain(FilenameConstants.DefaultCircuitRecDomainFilepath);
            Microsoft.Ink.WordList wordList = TextRecognition.TextRecognition.createLabelWordList();
            CircuitRec.CircuitRec crec = new CircuitRec.CircuitRec(domain, wordList);
            
            // Run CircuitRec
            try
            {
                crec.Run(resultSketch);
            }
            catch (ParseError e)
            {
                // TEMP deprecated?
                result.ParseError = e;
            }
            catch (ApplicationException e)
            {
                System.Windows.MessageBox.Show("Error (ApplicationException): CircuitRec could not recognize circuit properties: \n" + e.Message);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Error (General Exception): \n" + e.Message);
            }

            // Fill in result
            result.UserTriggered = args.UserTriggered;
            result.Sketch = resultSketch;
            result.CircuitRecInstance = crec;

            return result;
        }
    }

    /// <summary>
    /// Result structure for CircuitRecognizer.  Contains a 
    /// labeled (and fragmented) Sketch, and the CircuitRec
    /// recognizer that contains structural information
    /// about the circuit (e.g. wire connections, etc).
    /// </summary>
    public class CircuitRecResult : RecognitionResult
    {
        /// <summary>
        /// The CircuitRec instance used to recognize
        /// this circuit.
        /// </summary>
        public CircuitRec.CircuitRec CircuitRecInstance;

        /// <summary>
        /// TEMP DEBUG DEPRECATED
        /// 
        /// The Parse Error (if any) encountered when CircuitRec
        /// recongized the Sketch.  
        /// </summary>
        public CircuitRec.ParseError ParseError;
    }
}
