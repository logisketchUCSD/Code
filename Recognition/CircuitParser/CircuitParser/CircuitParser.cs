using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows;
using SketchPanelLib;
using Sketch;
using CircuitSimLib;

namespace CircuitParser
{
    public class CircuitParser
    {

        #region INTERNALS

        /// <summary>
        /// Prints debug statements iff debug == true.
        /// </summary>
        private bool debug = false;

        /// <summary>
        /// Describes how components should be connected.
        /// </summary>
        ContextDomain.CircuitDomain _domain;

        /// <summary>
        /// Maps shapes to components (gates and IOs).
        /// </summary>
        Dictionary<Shape, LogicGate> _logicGates;

        /// <summary>
        /// Maps shapes to wiremeshes.
        /// </summary>
        Dictionary<Shape, WireMesh> _wireMeshes;

        /// <summary>
        /// Maps shapes to circuit's inputs.
        /// </summary>
        Dictionary<Shape, CircuitInput> _circuitInputs;

        /// <summary>
        /// Maps shapes to circuit's outputs.
        /// </summary>
        Dictionary<Shape, CircuitOutput> _circuitOutputs;

        /// <summary>
        /// True if the sketch could be successfully parsed into a circuit.
        /// </summary>
        bool _successfulParse;

        /// <summary>
        /// A list of all the errors that arose when parsing.
        /// </summary>
        List<ParseError> _parseErrors;

        /// <summary>
        /// A dictionary mapping subcircuit shapes to the actual circuit elements.
        /// </summary>
        Dictionary<Shape, CircuitElement> _subCircuitShapeToElement;

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Create a circuit parser for a sketch with no subcircuits.
        /// </summary>
        public CircuitParser()
            :this(new Dictionary<Shape, CircuitElement>())
        {
        }

        /// <summary>
        /// Create a circuit parser for a sketch with the given subcircuits.
        /// </summary>
        public CircuitParser(Dictionary<Shape, CircuitElement> subCircuitShapeToElement)
        {
            _domain = ContextDomain.CircuitDomain.GetInstance();
            resetCircuitModel();
            _subCircuitShapeToElement = subCircuitShapeToElement;
        }

        #endregion

        #region MAIN METHOD

        /// <summary>
        /// Determine whether a sketch can be successfully parsed, but do not return the
        /// actual computed circuit.
        /// </summary>
        /// <param name="sketch"></param>
        /// <returns></returns>
        public bool SuccessfullyParse(Sketch.Sketch sketch)
        {
            Circuit circuit;
            return SuccessfullyParse(sketch, out circuit);
        }

        /// <summary>
        /// Tries to parse a sketch into a circuit, and
        /// indicates whether or not it was successful.
        /// 
        /// If it returns true, then "circuit" will contain
        /// a non-null, valid circuit ready for simulation.
        /// 
        /// If it returns false, then "ParseErrors" will return
        /// a nonempty list of errors.
        /// 
        /// The main functionality in circuit recognition
        /// here is that it both determines what are inputs
        /// and outputs on both the gate and circuit level,
        /// and organizes the results into something the 
        /// circuit can be built off of for simulation.
        /// </summary>
        /// <param name="sketch">The sketch to parse</param>
        /// <param name="circuit">The parsed circuit</param>
        /// <returns>True if parsing was a success, 
        /// false if there were errors</returns>
        public bool SuccessfullyParse(Sketch.Sketch sketch, out Circuit circuit)
        {
            circuit = null;

            // We should have a cleared circuit model each time
            // we try to parse a sketch.
            resetCircuitModel();

            // Check to make sure the sketch was valid in the first place.
            // If not, do not continue.
            _successfulParse = CheckSketch(sketch);
            if (!_successfulParse) return false;

            loadWiresAndGates(sketch);
            connectGatesToWires();
            loadCircuitIO(sketch);
            connectComponentsToComponents();

            // Make sure that the circuit we just made is valid
            _successfulParse = _successfulParse && CheckCircuit();

            if (_successfulParse)
            {
                circuit = new Circuit(CircuitConnections,
                    CircuitOutputs,
                    CircuitInputs,
                    _subCircuitShapeToElement);

                _successfulParse = _successfulParse && CheckSubcircuits(circuit);
            }

            if (debug)
                printErrors();

            return _successfulParse;
        }
        #endregion

        #region HELPERS

        #region Reset / initialize circuit model

        /// <summary>
        /// Resets all the data members associated with a model of the
        /// circuit, so we can be sure to start afresh each time we
        /// parse a sketch.
        /// </summary>
        private void resetCircuitModel()
        {
            _logicGates = new Dictionary<Shape, LogicGate>();
            _wireMeshes = new Dictionary<Shape, WireMesh>();
            _circuitInputs = new Dictionary<Shape, CircuitInput>();
            _circuitOutputs = new Dictionary<Shape, CircuitOutput>();

            _successfulParse = true;    // Innocent until proven guilty
            _parseErrors = new List<ParseError>();
        }

        #endregion

        #region Loading wires and gates

        /// <summary>
        /// Creates a circuit component for every Wire or Gate shape in 
        /// a given sketch to store in the invoking circuit parser. 
        /// Note that it does not add labels (circuit inputs/outputs)
        /// or anything else that's not a wire or gate.
        /// 
        /// Assumption: every shape in the sketch has a unique name.
        /// 
        /// Note: we can remove this assumption by using the shape ID
        /// (GUIDs) as keys in CircuitParser's dictionaries. We use 
        /// the shape names primarily because it helps with debugging,
        /// and that circuit elements are differentiated by their names
        /// later along the line so names should be unique anyway.
        /// </summary>
        /// <param name="sketch"></param>
        private void loadWiresAndGates(Sketch.Sketch sketch)
        {
            foreach (Sketch.Shape shape in sketch.Shapes)
            {
                if (Domain.LogicDomain.IsWire(shape.Type))
                {
                    WireMesh newWire = new WireMesh(shape);
                    _wireMeshes.Add(shape, newWire);
                    if (debug) Console.WriteLine("Found wire: " + newWire.Name);
                }
                else if (Domain.LogicDomain.IsGate(shape.Type))
                {
                    LogicGate newGate = new LogicGate(shape);
                    _logicGates.Add(shape, newGate);
                    if (debug) Console.WriteLine("Found gate: " + newGate.Name);
                }
            }
        }

        #endregion

        #region Connecting gates to wires

        /// <summary>
        /// Connects every logic gate in the circuit to its appropriate
        /// input and output wires.
        /// </summary>
        private void connectGatesToWires()
        {
            foreach (LogicGate gate in _logicGates.Values)
                if (gate.Type == LogicDomain.NOTBUBBLE)
                    connectNotBubble(gate);
                else
                    connectWiresTo(gate);
        }

        /// <summary>
        /// Connects not bubbles to gates and wires
        /// </summary>
        /// <param name="gate"></param>
        private void connectNotBubble(LogicGate bubble)
        {
            LogicGate parentGate = null;
            WireMesh wire = null;

            // Get the components this not bubble is connected to 
            foreach (Shape connected in bubble.Shape.ConnectedShapes)
            {
                if (LogicDomain.IsGate(connected.Type) && parentGate == null)
                    parentGate = _logicGates[connected];
                else if (LogicDomain.IsWire(connected.Type))
                    wire = _wireMeshes[connected];
            }

            // If this is not connected to a gate, connect it like a normal logic gate
            if (parentGate == null)
            {
                connectWiresTo(bubble);
                return;
            }

            // Is this bubble on the output or the input?
            Sketch.EndPoint connectingEndpoint =
                parentGate.Shape.ClosestEndpointFrom(bubble.Shape);
            bool isInput = parentGate.ShouldBeInput(connectingEndpoint);

            if (isInput)
            {
                wire.ConnectDependent(bubble);
                parentGate.ConnectInput(bubble, 0);
            }
            else
            {
                wire.ConnectSource(bubble, 0);
                parentGate.ConnectOutput(bubble, 0);
            }
        }

        /// <summary>
        /// Figures out everything a given logic gate should be 
        /// connected to, and connects them appropriately.
        /// 
        /// Assumption: Every wire-mesh has a unique name.
        /// 
        /// Note: this currently only deals with connections to wires.
        /// Hence, it does not make any connections for notbubbles yet.
        /// </summary>
        /// <param name="gate">The gate to connect</param>
        private void connectWiresTo(LogicGate gate)
        {
            // We keep track of what wire-meshes are inputs and outputs 
            // so we can do sanity checks before actually connecting 
            // them to the logic gate.
            List<Shape> inputWires = new List<Shape>();
            List<Shape> outputWires = new List<Shape>();

            int maxOutputs = _domain.NumberOutputs(gate.Type).Max;

            // Cycle through everything connected to the gate's associated
            // shape and categorize their connection type accordingly
            foreach (Sketch.Shape connectedShape in gate.Shape.ConnectedShapes)
            {
                // We'll take care of the not bubbles when they come up seprately
                if (connectedShape.Type == LogicDomain.NOTBUBBLE)
                    continue;
                // If it's not a wire or a not bubble, something is wrong
                else if (!Domain.LogicDomain.IsWire(connectedShape.Type))
                    throw new Exception("Gate " + gate + " was connected to non-wire, non-notbubble shape " + connectedShape);

                Sketch.EndPoint connectingEndpoint =
                    gate.Shape.ClosestEndpointFrom(connectedShape);

                if (gate.ShouldBeInput(connectingEndpoint))
                    inputWires.Add(connectedShape);
                else
                    outputWires.Add(connectedShape);
            }

            // Make the connections.
            foreach (Shape wire in inputWires)
            {
                WireMesh inputWire = _wireMeshes[wire];
                gate.ConnectInput(inputWire);
            }
            foreach (Shape wire in outputWires)
            {
                WireMesh outputWire = _wireMeshes[wire];
                gate.ConnectOutput(outputWire);
            }
            gate.ConnectAllInputs();
            gate.ConnectAllOutputs();
        }

        #endregion

        #region Loading circuit inputs and outputs

        private void loadCircuitIO(Sketch.Sketch sketch)
        {
            foreach (Sketch.Shape shape in sketch.Shapes)
                if (shape.Type == LogicDomain.TEXT)
                    loadInputOrOutput(shape);
        }

        /// <summary>
        /// Determines whether or not a given shape is a circuit
        /// input, and creates and connects it accordingly.  
        /// Will ignore any text that is not connected to anything.
        /// 
        /// Assumptions: 
        ///   * The names of wire-meshes correspond directly to
        ///     their associated shapes.
        ///   * Wire-meshes all have unique names.
        ///   * The given shape is not a label connected to two wires.
        /// </summary>
        /// <param name="shape">The shape to analyze</param>
        /// <returns>True if loading was successful, false
        /// if the given shape cannot be recognized as an input 
        /// or output</returns>
        private void loadInputOrOutput(Sketch.Shape shape)
        {
            // Make sure we're actually dealing with something sensical.
            // Note: If there are no connected shapes, we don't make an input or output, it is just ignored in the circuit.
            if (shape.Type != LogicDomain.TEXT || shape.ConnectedShapes.Count == 0)
                return;

            // Retreive the wire connected to this label shape, while
            // also checking that the shape has the correct number and 
            // kinds of connections.
            List<WireMesh> connectedWires = new List<WireMesh>();
            
            // Assume that we have an input until we are told otherwise.
            bool input = true;

            foreach (Sketch.Shape connectedShape in shape.ConnectedShapes)
            {
                // Is this a wire?
                if (!LogicDomain.IsWire(connectedShape.Type)) continue;

                // Get the connected wire
                WireMesh connected = _wireMeshes[connectedShape];

                // Have we already seen this?
                if (connectedWires.Contains(connected)) continue;
                connectedWires.Add(connected);

                // If we're dealing with any wire that already has a source, this will not be an input
                if (connected.HasSource)
                    input = false;
            }

            if (input)
            {
                CircuitInput newInput = new CircuitInput(shape);
                foreach (WireMesh wire in connectedWires)
                    newInput.Connect(wire);
                _circuitInputs.Add(shape, newInput);
            }
            else
            {
                CircuitOutput newOutput = new CircuitOutput(shape);
                foreach (WireMesh wire in connectedWires)
                    newOutput.Connect(wire);
                _circuitOutputs.Add(shape, newOutput);
            }
        }

        #endregion

        #region Connect circuit components to circuit components

        /// <summary>
        /// Connects all the circuit components (logic gates, 
        /// circuit inputs, and circuit outputs) that share
        /// a wire-mesh to each other  
        /// </summary>
        private void connectComponentsToComponents()
        {
            foreach (CircuitComponent thing in AllCircuitComponents)
            {
                thing.ConnectAllInputs();
                thing.ConnectAllOutputs();
                thing.OrderInputs();
            }
        }

        #endregion

        #region Organize / get results

        /// <summary>
        /// Dictionary describing the connections in the circuit. For each shape we keep a dictionary of its inputs, and
        /// for each input we store the source shape and the source's output index.
        /// </summary>
        private Dictionary<Shape, Dictionary<int, Tuple<Shape, int>>> CircuitConnections
        {
            get
            {
                Dictionary<Shape, Dictionary<int, Tuple<Shape, int>>> connections
                    = new Dictionary<Shape, Dictionary<int, Tuple<Shape, int>>>();
                foreach (CircuitComponent component in _circuitInputs.Values)
                    connections = recordConnections(connections, component);
                foreach(CircuitComponent component in _circuitOutputs.Values)
                    connections = recordConnections(connections, component);
                foreach (CircuitComponent component in LogicGates)
                    connections = recordConnections(connections, component);
                return connections;
            }
        }

        /// <summary>
        /// Add connections between the given component and its inputs to the dictionary of circuit connections.
        /// (computes the input/output indices, then passes that info to connect(...) which actually adds the
        /// connection to the dictionary).
        /// </summary>
        /// <param name="connections">Existing dictionary of connctions.</param>
        /// <param name="component">Element whose inputs we add</param>
        /// <returns>The dictionary with the new connections added</returns>
        private Dictionary<Shape, Dictionary<int, Tuple<Shape, int>>> recordConnections(
            Dictionary<Shape, Dictionary<int, Tuple<Shape, int>>> connections,
            CircuitComponent component)
        {
            // Find the inputs to this component using the component's input wires.
            foreach (int destindex in component.InputComponents.Keys)
            {
                connections = recordConnection(connections, component.InputComponents[destindex].Item1.Shape, 
                    component.InputComponents[destindex].Item2, component.Shape, destindex);
            }
            return connections;
        }

        /// <summary>
        /// Actually add the connection to the dictionary.
        /// </summary>
        private Dictionary<Shape, Dictionary<int, Tuple<Shape, int>>> recordConnection(
            Dictionary<Shape, Dictionary<int, Tuple<Shape, int>>> connections,
            Shape sourceComponent, int sourceIndex,
            Shape destComponent, int destIndex)
        {
            Tuple<Shape, int> leftPort = Tuple.Create(sourceComponent, sourceIndex);
            if (!connections.ContainsKey(destComponent))
                connections.Add(destComponent, new Dictionary<int, Tuple<Shape, int>>());
            connections[destComponent].Add(destIndex, leftPort);
            return connections;
        }

        /// <summary>
        /// Gets a list of all the logic gates in the circuit model.
        /// </summary>
        private List<LogicGate> LogicGates
        {
            get
            {
                List<LogicGate> logicGates = new List<LogicGate>();
                foreach (LogicGate gate in _logicGates.Values)
                    logicGates.Add(gate);
                return logicGates;
            }
        }

        /// <summary>
        /// Gets a list of all the wires in the circuit model.
        /// </summary>
        public List<Shape> Wires
        {
            get
            {
                List<Shape> wireMeshes = new List<Shape>();
                foreach (WireMesh wire in _wireMeshes.Values)
                    wireMeshes.Add(wire.Shape);
                return wireMeshes;
            }
        }

        /// <summary>
        /// Get the shape that the given wire gets its value from.
        /// </summary>
        /// <param name="wire">a wire from the list of Wires</param>
        /// <returns></returns>
        public Shape SourceForWire(Shape wire)
        {
            return _wireMeshes[wire].Source.Shape;
        }

        /// <summary>
        /// Get the index of the source the given wire is connected to.
        /// </summary>
        /// <param name="wire">a wire from the list of Wires</param>
        /// <returns></returns>
        public int SourceIndexForWire(Shape wire)
        {
            return _wireMeshes[wire].SourceIndex;
        }

        /// <summary>
        /// Gets a list of shapes which the circuitParser parsed as inputs.
        /// </summary>
        public List<Shape> CircuitInputs
        {
            get
            {
                List<Shape> inputs = new List<Shape>();
                foreach (CircuitInput input in _circuitInputs.Values)
                    inputs.Add(input.Shape);
                return inputs;
            }
        }

        /// <summary>
        /// Gets a list of all the shapes which correspond to outputs.
        /// </summary>
        public List<Shape> CircuitOutputs
        {
            get
            {
                return new List<Shape>(_circuitOutputs.Keys);
            }
        }

        /// <summary>
        /// Gets a list of all inputs, outputs, and gates
        /// </summary>
        private List<CircuitComponent> AllCircuitComponents
        {
            get
            {
                List<CircuitComponent> allComponents = new List<CircuitComponent>();

                foreach (CircuitComponent gate in _logicGates.Values)
                    allComponents.Add(gate);
                foreach (CircuitComponent input in _circuitInputs.Values)
                    allComponents.Add(input);
                foreach (CircuitComponent output in _circuitOutputs.Values)
                    allComponents.Add(output);

                return allComponents;
            }
        }

        #endregion

        #region Handling errors

        /// <summary>
        /// Gets a list of the errors that arose in 
        /// parsing a circuit.
        /// </summary>
        public List<ParseError> ParseErrors
        {
            get { return _parseErrors; }
        }

        /// <summary>
        /// Prints errors from parsing the circuit.
        /// </summary>
        private void printErrors()
        {
            if (_parseErrors.Count == 0)
                return;

            Console.WriteLine("The following errors arose in parsing the circuit:");
            foreach (ParseError error in _parseErrors)
            {
                Console.WriteLine(error.Explanation);
                Sketch.Shape errShape = error.Where;
            }

            Console.WriteLine();
        }


        #endregion

        #region Checking Circuit & Sketch Validity

        /// <summary>
        /// Checks whether the circuit is valid, returns a bool indicating this.  
        /// Adds any errors it comes across to the _parseErrors list.
        /// 
        /// Looks for:
        ///    * Each wire has one source and at least one output
        ///    * No wire outputs to an input and no output is a wire's input
        ///    * Each gate has an allowable number of inputs and outputs
        ///    * Each circuit element and wire have reciprocal connections
        ///    * Each input and output are connected to something
        /// </summary>
        /// <returns>A bool, which is true if the circuit is valid</returns>
        private bool CheckCircuit()
        {
            // Assume the circuit is fine, decide otherwise later.
            bool valid = true;

            #region Check Wire Connections
            // Each wire should have one input and at least one output
            foreach (WireMesh mesh in _wireMeshes.Values)
            {
                // Do the connections exist?
                if (!mesh.HasSource)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The wire " + mesh.Name + " is missing a source.", 
                        "This wire does not have a source.  Try dragging its red endpoints to connect it to something.", mesh.Shape));
                }
                /*
                // This is actually ok right now, outputs can be drawn in the middle of wires to give intermediary values.
                else if (mesh.Source is CircuitOutput)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The wire " + mesh.Name + "'s source is an output, " + mesh.Source.Name + ".",
                        "This wire's source is an output, " + mesh.Source.Name + ".", mesh.Shape));
                }
                */
                if (mesh.Dependents.Count < 1)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The wire " + mesh.Name + " has no dependents.", 
                        "Nothing uses this wire's value! Please connect it to something by dragging its endpoints.", mesh.Shape));
                }
                foreach (CircuitComponent connected in mesh.Dependents)
                    if (connected is CircuitInput)
                    {
                        valid = false;
                        _parseErrors.Add(new ParseError("The wire " + mesh.Name + " is giving a value to the input" + connected.Name + ".",
                            "This wire is giving a value to the input" + connected.Name + ".", mesh.Shape));
                    }
            }
            #endregion

            #region Check Gate Connections
            // Each gate should have the correct number of inputs and outputs
            foreach (LogicGate gate in LogicGates)
            {
                // Get the minimum and maximum input and output numbers
                Utilities.IntRange inputs = _domain.NumberInputs(gate.Type);
                Utilities.IntRange outputs = _domain.NumberOutputs(gate.Type);

                // Check input numbers
                if (gate.InputComponents.Count < inputs.Min)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The gate " + gate.Name + " is missing some inputs.  It needs at least " + inputs.Min + " but has " + gate.InputComponents.Count + ".",
                        "This gate is missing some inputs.  It needs at least " + inputs.Min + " but has " + gate.InputComponents.Count + ".", gate.Shape));
                }
                else if (gate.InputComponents.Count > inputs.Max)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The gate " + gate.Name + " has too many inputs.  It needs at most " + inputs.Max + " but has " + gate.InputComponents.Count + ".",
                        "This gate has too many inputs.  It needs at most " + inputs.Max + " but has " + gate.InputComponents.Count + ".", gate.Shape));
                }

                // Check input connections
                foreach (int index in gate.InputComponents.Keys)
                {
                    CircuitComponent component = gate.InputComponents[index].Item1;
                    if (!component.HasOutput(gate, gate.InputComponents[index].Item2))
                    {
                        valid = false;
                        _parseErrors.Add(new ParseError("The gate " + gate.Name + " is connected to the component " + component.Name + ", but that component is not connected to the gate!",
                            "The gate " + gate.Name + " is connected to the component " + component.Name + ", but that component is not connected to the gate!", component.Shape));
                    }
                }

                // Check output numbers
                if (gate.OutputComponents.Count < outputs.Min)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The gate " + gate.Name + " is missing some outputs.  It needs at least " + outputs.Min + " but has " + gate.OutputComponents.Count + ".",
                        "This gate is missing some outputs.  It needs at least " + outputs.Min + " but has " + gate.OutputComponents.Count + ".", gate.Shape));
                }
                else if (gate.OutputComponents.Count > outputs.Max)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The gate " + gate.Name + " has too many outputs.  It needs at most " + outputs.Max + " but has " + gate.OutputComponents.Count + ".",
                        "This gate has too many outputs.  It needs at most " + outputs.Max + " but has " + gate.OutputComponents.Count + ".", gate.Shape));
                }

                // Check output connections
                foreach (WireMesh wire in gate.OutputWires)
                {
                    if (wire.Source != gate)
                    {
                        valid = false;
                        _parseErrors.Add(new ParseError("The gate " + gate.Name + " is connected to the wire " + wire.Name + ", but that wire is not connected to the gate!",
                           "The gate " + gate.Name + " is connected to the wire " + wire.Name + ", but that wire is not connected to the gate!", wire.Shape));
                    }
                }

                // Check output connections
                for (int i = 0; i < gate.OutputComponents.Count; i++)
                    foreach (CircuitComponent component in gate.OutputComponents[i])
                    {
                        if (!component.HasInput(gate, i))
                        {
                            valid = false;
                            _parseErrors.Add(new ParseError("The gate " + gate.Name + " is connected to the component " + component.Name + ", but that component is not connected to the gate!",
                               "The gate " + gate.Name + " is connected to the component " + component.Name + ", but that component is not connected to the gate!", component.Shape));
                        }
                    }
            }
            #endregion

            #region Check Input/Output connections
            // Each input should be connected correctly
            foreach (CircuitInput input in _circuitInputs.Values)
            {
                if (input.OutputWires.Count == 0)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The input " + input.Name + " is not connected to anything.",
                        "This input (" + input.Name + ") is not connected to anything.", input.Shape));
                }
                else
                {
                    foreach (WireMesh wire in input.OutputWires)
                        if (wire.Source != input)
                        {
                            valid = false;
                            _parseErrors.Add(new ParseError("The input " + input.Name + " is connected to the wire " + wire.Name + ", but that wire does not get it's value from the input!",
                                "This input (" + input.Name + ") is connected to the wire " + wire.Name + ", but that wire does not get it's value from thr input!", input.Shape));
                        }
                }
                if (input.InputWires.Count > 0)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The input " + input.Name + " connected as an output.",
                        "This input (" + input.Name + ") is connected as an output.", input.Shape));
                }
            }

            // Each output should be connected correctly
            foreach (CircuitOutput output in _circuitOutputs.Values)
            {
                if (output.InputWires.Count == 0)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The output " + output.Name + " is not getting a value from anything.",
                        "This output (" + output.Name + ") is not getting a value from anything.", output.Shape));
                }
                else if (output.InputWires.Count > 1)
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The output " + output.Name + " gets a value from more than one wire.",
                        "This output (" + output.Name + ") gets a value from more than one wire.", output.Shape));
                }
                else if (!output.SourceWire.Dependents.Contains(output))
                {
                    valid = false;
                    _parseErrors.Add(new ParseError("The output " + output.Name + " is connected to the wire " + output.SourceWire.Name + ", but that wire does not give it's value to the output!",
                        "This output (" + output.Name + ") is connected to the wire " + output.SourceWire.Name + ", but that wire does not give it's value to the output!", output.Shape));
                }
                foreach (WireMesh wire in output.OutputWires)
                {
                    if (wire.Source != output)
                    {
                        valid = false;
                        _parseErrors.Add(new ParseError("The output " + output.Name + " is connected to the wire " + wire.Name + ", but that wire does not get it's value from the output!",
                                "This output (" + output.Name + ") is connected to the wire " + wire.Name + ", but that wire does not get it's value from the output!", output.Shape));
                    }
                }
            }
            #endregion

            return valid;
        }

        /// <summary>
        /// check to see if if the inputs and outputs for any subcircuits are consistent, and raise
        /// ParseErrors if they are not
        /// </summary>
        /// <returns></returns>
        private bool CheckSubcircuits(Circuit circuit)
        {
            bool valid = true;

            if (circuit == null)
                throw new ArgumentNullException();

            foreach (CircuitSimLib.Gate gate in circuit.AllGates)
            {
                if (gate.GateType == LogicDomain.SUBCIRCUIT)
                {
                    CircuitSimLib.SubCircuit subCircuit = ((CircuitSimLib.SubCircuit)gate);
                    if (gate.InputPorts.Count != subCircuit.inputsRequired)
                    {
                        valid = false;
                        _parseErrors.Add(new ParseError("The Subcircuit " + gate.Name + " needs " + subCircuit.inputsRequired + " inputs, but has " + gate.InputPorts.Count,
                             "The Subcircuit " + gate.Name + " needs " + subCircuit.inputsRequired + " inputs, but has " + gate.InputPorts.Count, subCircuit.shape));
                    }
                    if (gate.Outputs.Count != subCircuit.outputsRequired)
                    {
                        valid = false;
                        _parseErrors.Add(new ParseError("The Subcircuit " + gate.Name + " needs " + subCircuit.outputsRequired + " outputs, but has " + gate.Outputs.Count,
                             "The Subcircuit " + gate.Name + " needs " + subCircuit.outputsRequired + " outputs, but has " + gate.Outputs.Count, subCircuit.shape));
                    }

                }
            }

            return valid;
        }

        /// <summary>
        /// Checks whether the sketch is valid, and returns a bool indicating this.
        /// Adds any errors it comes across to the _parseErrors list.
        /// 
        /// Looks for:
        ///    * Basic consistency via sketch.CheckConsistency
        ///    * Gates connected to only wires
        ///    * Wires not connected to wires
        ///    * Text connected to at most one wire
        /// </summary>
        /// <param name="sketch">The sketch to check the validity of</param>
        /// <returns>A bool, which is true if the sketch is valid</returns>
        private bool CheckSketch(Sketch.Sketch sketch)
        {
            bool valid = true;

            // Check basic sketch consistency, will (rightly) throw an exception if it finds something wrong.
            // Eventually we should not need this, for the sketch should always pass this, but it's a useful check for now.
            sketch.CheckConsistency();

            foreach (Sketch.Shape shape in sketch.Shapes)
            {
                #region Gate Checks
                if (LogicDomain.IsGate(shape.Type) && shape.Type != LogicDomain.NOTBUBBLE)
                {
                    // Each gate should be connected to only wires
                    foreach (Sketch.Shape connected in shape.ConnectedShapes)
                    {
                        if (!LogicDomain.IsWire(connected.Type) && connected.Type != LogicDomain.NOTBUBBLE)
                        {
                            valid = false;
                            _parseErrors.Add(new ParseError("Gate " + shape.Name + " is connected to something that is not a wire or a not bubbule, " + connected.Name + "of type " + connected.Type.Name,
                                "This gate is connected to something that is not a wire or a not bubble.  Draw wires between them or group them together.", shape));
                        }
                    }
                }
                #endregion

                #region Not Bubble Checks
                else if (shape.Type == LogicDomain.NOTBUBBLE)
                {
                    bool gateFound = false;
                    if (shape.ConnectedShapes.Count != 2)
                    {
                        valid = false;
                        _parseErrors.Add(new ParseError("Not bubble " + shape.Name + " is connected to " + shape.ConnectedShapes.Count + " shapes when it should be connected to 2.",
                            "This not bubble is connected to " + shape.ConnectedShapes.Count + " shapes when it should be connected to 2.", shape));
                    }
                    else
                    {
                        foreach (Shape connected in shape.ConnectedShapes)
                        {
                            if (LogicDomain.IsGate(connected.Type) && !gateFound)
                                gateFound = true;
                            else if (LogicDomain.IsGate(connected.Type) && gateFound)
                            {
                                valid = false;
                                _parseErrors.Add(new ParseError("Not bubble " + shape.Name + " is connected to more than one gate.",
                                    "This not bubble is connected to more than one gate.", shape));
                            }
                            else if (LogicDomain.IsText(connected.Type))
                            {
                                valid = false;
                                _parseErrors.Add(new ParseError("Not bubble " + shape.Name + " is connected to text.",
                                    "This not bubble is connected to text, it should only be connected to gates and wires.", shape));
                            }
                        }
                    }
                }
                #endregion

                #region Wire Checks
                else if (LogicDomain.IsWire(shape.Type))
                {
                    // Wires shouldn't be connected to other wires
                    foreach (Sketch.Shape connected in shape.ConnectedShapes)
                    {
                        if (shape != connected && LogicDomain.IsWire(connected.Type))
                        {
                            valid = false;
                            _parseErrors.Add(new ParseError("Wire " + shape.Name + " is connected to another wire, " + connected.Name,
                                "This wire is connected to another wire.  Try grouping these wires together.", shape));
                        }
                    }
                }
                #endregion

                #region Input/Output Checks
                else if (LogicDomain.IsText(shape.Type))
                {
                    // Text should only be connected to wires, if anything.
                    foreach (Sketch.Shape wire in shape.ConnectedShapes)
                        if (!LogicDomain.IsWire(wire.Type))
                        {
                            valid = false;
                            _parseErrors.Add(new ParseError("Text " + shape.Name + " should only be connected to a wire, but is connected to a " + wire.Type.Name + ".",
                                shape.Name + " should only be connected to a wire, but is connected to a " + wire.Type.Name + ".", shape));
                        }
                }
                #endregion
            }

            return valid;
        }

        #endregion

        #endregion
    }
}