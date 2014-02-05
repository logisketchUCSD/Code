using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Ink;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Domain;
using CircuitSimLib;
using SketchPanelLib;
using Sketch;
using System.Windows.Media;
using Data;
using InkToSketchWPF;
using CircuitParser;

namespace SimulationManager
{
    /// <summary>
    /// Event handler for telling when the truth table is closed.
    /// </summary>
    public delegate void TruthTableClosedHandler();

    /// <summary>
    /// Maps the ink a user sees to the circuit that calculates everything
    /// in the background for circuit simulation.
    /// </summary>
    public class SimulationManager
    {
        #region CONSTANTS

        private System.Windows.Media.Color ON_COLOR = System.Windows.Media.Colors.LightSkyBlue;
        private System.Windows.Media.Color OFF_COLOR = System.Windows.Media.Colors.MidnightBlue;
        private System.Windows.Media.Color ERROR_COLOR = System.Windows.Media.Colors.Red;

        #endregion

        #region INTERNALS

        /// <summary>
        /// The panel that the simulation manager is working on.
        /// </summary>
        private SketchPanel sketchPanel;

        /// <summary>
        /// Prints debugging statements if debug is set to true.
        /// </summary>
        private bool debug = false;

        /// <summary>
        /// A boolean indicating if the circuit is a valid circuit.
        /// A valid circuit has no wires connected to more than one output,
        /// and every gate has a valid input.
        /// </summary>
        private bool valid;

        /// <summary>
        /// The clean representation of the circuit.
        /// </summary>
        private CleanCircuit cleanCircuit;

        /// <summary>
        /// Makes sure we do not subscribe or unsubscribe when we shouldn't.
        /// </summary>
        private bool subscribed;

        /// <summary>
        /// Whether we are displaying a clean circuit or the sketch circuit
        /// </summary>
        private bool displayingClean;

        /// <summary>
        /// The window which displays the truth table
        /// </summary>
        private TruthTableWindow truthTableWindow;

        /// <summary>
        /// Lets us know when to uncheck the truth table checkbox
        /// </summary>
        public TruthTableClosedHandler TruthTableClosed;

        #region Circuit Related Internals

        /// <summary>
        /// A mapping of which Ink Stroke IDs in the sketch panel
        /// correspond to which input names in the Circuit.
        /// </summary>
        private Dictionary<Shape, ListSet<String>> inputMapping;

        /// <summary>
        /// A mapping of which Ink Stroke IDs in the sketch panel
        /// correspond to which output names in the Circuit.
        /// </summary>
        private Dictionary<Shape, ListSet<String>> outputMapping;

        /// <summary>
        /// A mapping of gates to the StrokeIDs of the wires connected to them, 
        /// organized by the index of the output they are connected to.
        /// </summary>
        private Dictionary<Shape, Dictionary<int, ListSet<String>>> wireToInputGateMapping;

        /// <summary>
        /// Maps the name of the shape to a circuit element for lookup with
        /// subcircuits
        /// </summary>
        public Dictionary<Shape, CircuitElement> subCircuitShapetoElement;

        /// <summary>
        /// The circuit recognizer that allows us to get the inputs, 
        /// outputs, and structure of the circuit
        /// </summary>
        private CircuitParser.CircuitParser circuitParser;

        #endregion

        #region Input/Output toggle related

        private CircuitValuePopups circuitValuePopups;

        private Dictionary<Shape, System.Windows.Point> oldPopupLocations;

        #endregion

        #endregion

        #region CONSTRUCTOR & INITIALIZERS

        /// <summary>
        /// Constructor for the simulation manager.
        /// </summary>
        /// <param name="panel">The panel that this manager controls</param>
        public SimulationManager(ref SketchPanel panel)
        {
            sketchPanel = panel;

            // Circuit related initializations
            inputMapping = new Dictionary<Shape, ListSet<String>>();
            outputMapping = new Dictionary<Shape, ListSet<String>>();
            wireToInputGateMapping = new Dictionary<Shape, Dictionary<int, ListSet<string>>>();
            subCircuitShapetoElement = new Dictionary<Shape, CircuitElement>();
        }

        #endregion

        #region Subscription

        /// <summary>
        /// Subscribes to panel- brings up popups and truth table for simulation
        /// </summary>
        public void SubscribeToPanel()
        {
            if (subscribed)
                return;
            subscribed = true;

            // Prepare to receive events from toggles
            MakeNewToggles();

            simulateCircuit();
        }

        public void UnsubscribeFromPanel()
        {
            if (!subscribed)
                return;
            subscribed = false;

            if (truthTableWindow != null && truthTableWindow.IsActive)
                truthTableWindow.Close();

            // Unsubsribe Popups
            circuitValuePopups.SetInputEvent -= new SetInputEventHandler(setInputValue);
            circuitValuePopups.ClearAllToggles();
        }

        #endregion

        #region Getters & Setters

        /// <summary>
        /// Determine whether the circuit sketch is valid for simulation
        /// </summary>
        public bool Valid
        {
            get { return valid; }
        }

        /// <summary>
        /// Determine whether the simulation manager is subscribed
        /// </summary>
        public bool Subscribed
        {
            get { return subscribed; }
        }

        /// <summary>
        /// Get the circuit parser this simulation manager is using
        /// </summary>
        public CircuitParser.CircuitParser CircuitParser
        {
            get { return circuitParser; }
        }

        /// <summary>
        /// Determine whether the clean circuit is being displayed
        /// </summary>
        public bool DisplayingClean
        {
            get { return displayingClean; }
            set { displayingClean = value; }
        }

        /// <summary>
        /// Get the sketch that we are simulating
        /// </summary>
        private Sketch.Sketch sketch
        {
            get { return sketchPanel.InkSketch.Sketch; }
        }

        /// <summary>
        /// The cleaned circuit
        /// </summary>
        public CleanCircuit CleanCircuit
        {
            get { return cleanCircuit; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create the clean circuit, to seperate it from making the circuit
        /// </summary>
        /// <param name="sketchPanel"></param>
        private void createCleanCircuit(ref SketchPanel sketchPanel)
        {
            cleanCircuit = new CleanCircuit(ref sketchPanel, false);
        }

        /// <summary>
        /// Updates SimulationManager's dictionaries and returns the circuit produced from the parser.
        /// 
        /// Precondition: the parser has already successfully parsed the circuit.
        /// </summary>
        private Circuit createCircuit(CircuitParser.CircuitParser parser, Circuit circuit)
        {
            // Reset Dictionaries
            inputMapping.Clear();
            outputMapping.Clear();
            wireToInputGateMapping.Clear();

            // Update the mappings with the circuit inputs.
            foreach (Shape input in parser.CircuitInputs)
                addInput(input);

            // Update the mappings with the circuit outputs.
            foreach (Shape output in circuitParser.CircuitOutputs)
                addOutput(output);

            // Update the mappings with meshes and their input components
            foreach (Shape wire in parser.Wires)
                addWire(wire, parser.SourceForWire(wire), parser.SourceIndexForWire(wire));

            sketchPanel.InkSketch.Circuit = circuit;

            return sketchPanel.InkSketch.Circuit;
        }

        /// <summary>
        /// Sets all the inputs to the circuit to 0 and computes
        /// the values for all the other parts of the circuit.
        /// </summary>
        private void simulateCircuit()
        {
            // Set all the inputs to have the value 0
            foreach (Shape input in inputMapping.Keys)
                sketchPanel.Circuit.setInputValue(input, 0);

            colorWiresByValue();
        }

        /// <summary>
        /// Color wires according to whether they have a value of 0 or 1.
        /// </summary>
        public void colorWiresByValue()
        {
            calculateOutputs();
            if (cleanCircuit != null)
                cleanCircuit.UpdateWires();

            foreach (var pair in wireToInputGateMapping)
            {
                Shape gate = pair.Key;
                foreach (int index in pair.Value.Keys)
                {
                    int value = sketchPanel.Circuit.gateValue(gate, index);
                    System.Windows.Media.Color wireColor = ERROR_COLOR;
                    if (value == 1)
                        wireColor = ON_COLOR;
                    else if (value == 0)
                        wireColor = OFF_COLOR;

                    ListSet<string> wire = pair.Value[index];
                    foreach (String wireStroke in wire)
                        sketchPanel.InkSketch.GetInkStrokeById(wireStroke).DrawingAttributes.Color = wireColor;
                }
            }
        }

        /// <summary>
        /// Recognize a circuit after the sketch has been labeled.
        /// </summary>
        public bool recognizeCircuit()
        {
            circuitParser = new CircuitParser.CircuitParser(subCircuitShapetoElement);
            return recognizeCircuit(circuitParser);
        }

        private bool recognizeCircuit(CircuitParser.CircuitParser parser)
        {
            if (debug) Console.WriteLine("========== Circuit Parsing Has Begun! ==========");
            //valid = false;

            // Determine the circuit structure and trigger event for feedback mechanisms
            CircuitSimLib.Circuit circuit;
            valid = parser.SuccessfullyParse(sketch, out circuit);

            if (valid)
            {
                createCircuit(parser, circuit);
                createCleanCircuit(ref sketchPanel);
            }
            else
                if (debug)
                    Console.WriteLine("The circuit parser could not parse the circuit.");

            if (!valid)
            {
                if (debug)
                    Console.WriteLine("SimulationManager.makeCircuit() failed.");
            }


            return valid;
        }


        /// <summary>
        /// Displays the clean version of the circuit on the given Canvas.
        /// </summary>
        /// <param name="cleanCanvas"></param>
        public void DisplayCleanCircuit(DockPanel dock)
        {
            dock.Children.Remove(this.cleanCircuit);
            
            // Otherwise create the circuit and set the layout
            cleanCircuit = new CleanCircuit(ref sketchPanel, true);
            dock.Children.Add(this.cleanCircuit);

            cleanCircuit.Width = dock.ActualWidth;
            cleanCircuit.Height = dock.ActualHeight;

            displayingClean = true;
            cleanCircuit.EditingMode = InkCanvasEditingMode.None;
            cleanCircuit.EditingModeInverted = InkCanvasEditingMode.None;

            MakeNewToggles();
        }

        /// <summary>
        /// Removes the clean circuit
        /// </summary>
        public void RemoveCleanCircuit(DockPanel dock)
        {
            dock.Children.Remove(this.cleanCircuit);
            displayingClean = false;

            MakeNewToggles();
        }

        /// <summary>
        /// Remakes all the input/ouput popups
        /// </summary>
        public void MakeNewToggles()
        {
            if (circuitValuePopups == null || !circuitValuePopups.HasPopups)
            {
                circuitValuePopups = new CircuitValuePopups(inputMapping, sketchPanel, outputMapping);
                oldPopupLocations = circuitValuePopups.Locations;
                circuitValuePopups.SetInputEvent += new SetInputEventHandler(setInputValue);
            }

            if (displayingClean)
            {
                circuitValuePopups.Locations = cleanCircuit.LabelLocations;
                circuitValuePopups.MakeNewToggles(cleanCircuit);
            }
            else
            {
                circuitValuePopups.Locations = oldPopupLocations;
                circuitValuePopups.MakeNewToggles(sketchPanel.InkCanvas);
            }

        }

        /// <summary>
        /// Closes all toggles
        /// </summary>
        public void HideToggles()
        {
            if (circuitValuePopups != null)
                circuitValuePopups.HideToggles();
        }
        /// <summary>
        /// Opens all toggles
        /// </summary>
        public void ShowToggles()
        {
            if (circuitValuePopups == null)
                MakeNewToggles();

            if (debug) Console.WriteLine("Displaying toggles");
            if (!sketchPanel.Circuit.IsOscillating)
                circuitValuePopups.ShowToggles();
        }

        #endregion

        #region UPDATE DICTIONARIES

        /// <summary>
        /// Update the input mapping dictionary to reflect an input
        /// recently added to the circuit
        /// </summary>
        /// <param name="input">The shape (label) corresponding to the input</param>
        private void addInput(Sketch.Shape inputShape)
        {
            if (debug)
                Console.WriteLine("Trying to add " + inputShape.Type.Name + " called " + inputShape.Name + " to inputs");
               
            ListSet<String> strokeIDs = new ListSet<String>();
            foreach (Sketch.Substroke substroke in inputShape.Substrokes)
                strokeIDs.Add(sketchPanel.InkSketch.GetInkStrokeIDBySubstroke(substroke));

            if (!inputMapping.ContainsKey(inputShape))
                inputMapping.Add(inputShape,strokeIDs);
        }

        /// <summary>
        /// Update the output mapping dictionary to reflect an output
        /// recently added to the circuit
        /// </summary>
        /// <param name="ouput">The shape (label) corresponding to the output</param>
        private void addOutput(Sketch.Shape outputShape)
        {
            if (debug)
                Console.WriteLine("Trying to add " + outputShape.Type + " called " + outputShape.Name + " to outputs");
             
            ListSet<String> strokeIDs = new ListSet<string>();
            foreach (Sketch.Substroke substroke in outputShape.Substrokes)
                strokeIDs.Add(sketchPanel.InkSketch.GetInkStrokeIDBySubstroke(substroke));

            if(!outputMapping.ContainsKey(outputShape))
                outputMapping.Add(outputShape, strokeIDs);
        }

        /// <summary>
        /// Update the wire-to-input-gate mapping dictionary to reflect
        /// the addition of a gate to the circuit
        /// </summary>
        /// <param name="wire">the wire to add</param>
        /// <param name="source">the shape the given wire gets its value from</param>
        private void addWire(Shape wire, Shape source, int sourceIndex)
        {
            if (debug)
                Console.WriteLine("Trying to add " + wire.Type + " called " + wire.Name + " to wires of " + source.Type);

            if(!wireToInputGateMapping.ContainsKey(source))
                wireToInputGateMapping.Add(source, new Dictionary<int, ListSet<String>>());
            if (!wireToInputGateMapping[source].ContainsKey(sourceIndex))
                wireToInputGateMapping[source][sourceIndex] = new ListSet<string>();

            foreach (Sketch.Substroke substroke in wire.Substrokes)
                wireToInputGateMapping[source][sourceIndex].Add(sketchPanel.InkSketch.GetInkStrokeIDBySubstroke(substroke));
        }

        #endregion

        #region Input/Output Functions

        /// <summary>
        /// Change the value of a specified input in the circuit
        /// and update the ink colors accordingly
        /// </summary>
        /// <param name="name">the name of the input used in the circuit</param>
        /// <param name="value">the value (0 or 1) to set it to</param>
        private void setInputValue(Sketch.Shape inputShape, int value)
        {
            sketchPanel.Circuit.setInputValue(inputShape, value);

            colorWiresByValue();
            if (displayingClean)
                ShowToggles();
        }

        /// <summary>
        /// Calculates the values (0's or 1's) across the entire circuit
        /// and updates the display accordingly
        /// </summary>
        private void calculateOutputs()
        {
            sketchPanel.Circuit.calculateOutputs();
            // Only display the toggles if the circuit can be calculated
            if (!sketchPanel.Circuit.IsOscillating)
            {
                foreach (Shape output in outputMapping.Keys)
                {
                    int value = sketchPanel.Circuit.gateValue(output, 0);
                    circuitValuePopups.SetPopup(output, value, false);
                }
            }
            else
            {
                circuitValuePopups.HideToggles();
            }
        }

        #endregion

        #region Truth Table

        /// <summary>
        /// Brings up the truth table window and hooks into truth table events
        /// </summary>
        public void DisplayTruthTable()
        {
            truthTableWindow = new TruthTableWindow(new TruthTable(sketchPanel.Circuit));

            truthTableWindow.Closed +=new EventHandler(truthTableWindow_Closed);
            
            truthTableWindow.Show();
            truthTableWindow.SimulateRow += new RowHighlightEventHandler(truthTableWindow_SimulateRow);
            truthTableWindow.Highlight += new HighlightEventHandler(truthTableWindow_HighlightLabel);
            truthTableWindow.UnHighlight += new UnhighlightEventHandler(truthTableWindow_UnHighlightLabel);
            truthTableWindow.RelabelStrokes += new RelabelStrokesEventHandler(truthTableWindow_RelabelStrokes);
        }

        public void CloseTruthTable()
        {
            if (truthTableWindow != null && truthTableWindow.IsEnabled)
            {
                truthTableWindow.SimulateRow -= new RowHighlightEventHandler(truthTableWindow_SimulateRow);
                truthTableWindow.Close();
            }
        }


        /// <summary>
        /// Sets input values to those of highlighted row in truth table
        /// </summary>
        /// <param name="inputs">List of input values</param>
        private void truthTableWindow_SimulateRow(Dictionary<Shape, int> inputs)
        {
            foreach (var pair in inputs)
            {
                sketchPanel.Circuit.setInputValue(pair.Key, pair.Value);
                circuitValuePopups.SetPopup(pair.Key, pair.Value, true);
            }

            colorWiresByValue();
        }


        /// <summary>
        /// Highlights the corresponding strokes to the input
        /// </summary>
        /// <param name="shape"></param>
        private void truthTableWindow_HighlightLabel(Shape shape)
        {
            // If it is not in the dictionary - return

            ListSet<string> strokeSet = new ListSet<string>();
            if (inputMapping.ContainsKey(shape))
                strokeSet = inputMapping[shape];
            else if (outputMapping.ContainsKey(shape))
                strokeSet = outputMapping[shape];
            else
                return;

            foreach (string s in strokeSet)
            {
                System.Windows.Ink.Stroke stroke = sketchPanel.InkSketch.GetInkStrokeById(s);
                stroke.DrawingAttributes.Color = Colors.DarkRed;
            }
        }

        /// <summary>
        /// Unhighlights the corresponding strokes to the input
        /// </summary>
        /// <param name="shape"></param>
        private void truthTableWindow_UnHighlightLabel(Shape shape)
        {
            // If it is not in the dictionary - return
            ListSet<string> strokeSet = new ListSet<string>();
            if (inputMapping.ContainsKey(shape))
                strokeSet = inputMapping[shape];
            else if (outputMapping.ContainsKey(shape))
                strokeSet = outputMapping[shape];
            else
                return;

            foreach (string s in strokeSet)
            {
                System.Windows.Ink.Stroke stroke = sketchPanel.InkSketch.GetInkStrokeById(s);
                Substroke sub = sketchPanel.InkSketch.GetSketchSubstrokeByInk(stroke);
                stroke.DrawingAttributes.Color = sub.Type.Color;
            }
        }

        /// <summary>
        /// Updates the dictionaries based on the new names of the inputs/outputs
        /// </summary>
        /// <param name="newNameDict">Dictionary of old name keys to new name values</param>
        private void truthTableWindow_RelabelStrokes(Dictionary<Shape, string> newNameDict)
        {
            foreach (KeyValuePair<Shape, string> pair in newNameDict)
            {
                Shape s = pair.Key;
                string newName = pair.Value;
                s.Name = newName;
            }

            // Update popup names
            cleanCircuit.UpdateLabel(newNameDict);
            ShowToggles();
        }

        /// <summary>
        /// Event triggered when the truth table window is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void truthTableWindow_Closed(object sender, EventArgs e)
        {
            if(TruthTableClosed != null)
                TruthTableClosed();
        }
        #endregion
        
        #region Helpers

        /// <summary>
        /// For load you need to reset the sub circuit stuff
        /// </summary>
        public void ClearSubCircuits()
        {
            subCircuitShapetoElement = new Dictionary<Shape, CircuitElement>();

        }

        #endregion
    }
}