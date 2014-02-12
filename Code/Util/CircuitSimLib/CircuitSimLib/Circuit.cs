using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows;
using Sketch;

namespace CircuitSimLib
{
    public class Circuit
    {
        #region Internals

        private List<Gate> gates;
    
        private List<INPUT> globalInputs;

        private List<OUTPUT> globalOutputs;

        private Dictionary<Sketch.Shape, CircuitElement> shapesToElements;

        private bool isOscillating;

        public bool inputsHoriz;
        public bool outputsHoriz;

        #endregion

        #region Constructors

        /// <summary>
        /// Set up an empty circuit.
        /// </summary>
        private Circuit()
        {
            globalInputs = new List<INPUT>();
            gates = new List<Gate>();
            globalOutputs = new List<OUTPUT>();
            shapesToElements = new Dictionary<Shape, CircuitElement>();
            isOscillating = false;

        }

        /// <summary>
        /// Constructs a Circuit given a Dictionary of all the gates, 
        /// List of all outputs, and List of all inputs
        /// </summary>
        public Circuit(Dictionary<Sketch.Shape, Dictionary<int, Tuple<Shape, int>>> shapesAndTheirInputs,
                        List<Shape> outputs, List<Shape> inputs,
                        Dictionary<Sketch.Shape, CircuitElement> subCircuits)
            : this()
        {
            // Build global inputs
            foreach (Shape inputShape in inputs)
            {
                INPUT input = new INPUT(inputShape.Name, inputShape.Bounds, inputShape.Orientation);
                shapesToElements.Add(inputShape, input);
                globalInputs.Add(input);
            }

            // Build global outputs
            foreach (Shape outputShape in outputs)
            {
                OUTPUT output = new OUTPUT(outputShape.Name, outputShape.Bounds);
                shapesToElements.Add(outputShape, output);
                globalOutputs.Add(output);
            }

            // Build logic gates within the circuit
            foreach (var shapeAndInputs in shapesAndTheirInputs)
            {
                Shape gateShape = shapeAndInputs.Key;
                Gate gateElement;

                // If the shape is not a gate, or is already in the circuit, skip it.
                if (!LogicDomain.IsGate(gateShape.Type) || shapesToElements.ContainsKey(gateShape))
                    continue;
   
                // If it's a subcircuit, roll our own circuit element.
                else if (subCircuits.Keys.Contains(gateShape))
                {
                    SubCircuit associatedSub = (SubCircuit)subCircuits[gateShape];
                    associatedSub = new SubCircuit(gateShape.Name, gateShape.Bounds, gateShape, associatedSub.inputs, associatedSub.outputs,
                                    associatedSub.behavior, gateShape.Orientation);
                    gateElement = associatedSub;
                    subCircuits[gateShape] = associatedSub;
                }
                  
                // Otherwise, create an element normally.
                else
                    gateElement = createGate(gateShape.Name, gateShape.Bounds, gateShape.Orientation, gateShape.Type);

                if (gateElement == null)
                    throw new Exception("failed to create gate!");

                shapesToElements.Add(gateShape, gateElement);
                gates.Add(gateElement);
            }

            connectEverything(shapesAndTheirInputs);
        }

        /// <summary>
        /// Connects elements using a dictionary that describes the inputs for each Shape.
        /// </summary>
        /// <param name="elementAndItsInputs"></param>
        private void connectEverything(Dictionary<Sketch.Shape, Dictionary<int, Tuple<Shape, int>>> elementAndItsInputs)
        {
            foreach (var shapeAndInputs in elementAndItsInputs)
            {
                Shape rightShape = shapeAndInputs.Key;
                var inputPorts = shapeAndInputs.Value;
                foreach (var inputPort in inputPorts)
                {
                    CircuitElement rightElement = shapesToElements[rightShape];
                    int rightIndex = inputPort.Key;
                    CircuitElement leftElement = shapesToElements[inputPort.Value.Item1];
                    int leftIndex = inputPort.Value.Item2;
                    rightElement.addInput(sourceElement: leftElement, sourceIndex: leftIndex, destIndex: rightIndex);
                }
            }
        }

        /// <summary>
        /// Create a new gate.
        /// </summary>
        /// <param name="gateName"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static Gate createGate(string gateName, Rect bounds, double orientation, ShapeType type)
        {
            Gate tempGate = null;
            if (type == LogicDomain.AND)
                tempGate = new AND(gateName, bounds, orientation);
            else if (type == LogicDomain.NAND)
                tempGate = new NAND(gateName, bounds, orientation);
            else if (type == LogicDomain.OR)
                tempGate = new OR(gateName, bounds, orientation);
            else if (type == LogicDomain.NOR)
                tempGate = new NOR(gateName, bounds, orientation);
            else if (type == LogicDomain.XOR)
                tempGate = new XOR(gateName, bounds, orientation);
            else if (type == LogicDomain.XNOR)
                tempGate = new XNOR(gateName, bounds, orientation);
            else if (type == LogicDomain.NOT)
                tempGate = new NOT(gateName, bounds, orientation);
            else if (type == LogicDomain.NOTBUBBLE)
                tempGate = new NOTBUBBLE(gateName, bounds, orientation);
            else
                throw new Exception("type unrecognized: " + type.Name);
            return tempGate;
        }

        #endregion

        #region Getters and Setters
        /// <summary>
        /// Get a list of sketch shapes corresponding to the given list of circuit elements.
        /// </summary>
        public List<Sketch.Shape> shapesForElements<T>(IEnumerable<T> elements) where T : CircuitElement
        {

            /*
             * You may ask yourself: why do we need generics here? Why not just take a list of CircuitElements as
             * an argument? The answer is simple: a list of INPUTs is not a list of CircuitElements,
             * even though an INPUT is a CircuitElement. (As explanation, I can insert an OUTPUT into a list
             * of CircuitElements, but not a list of INPUTs, so they cannot be treated the same.) We use generics 
             * here so that you can pass this method a list of INPUTs instead of CircuitElements so C# doesn't 
             * complain. The syntax is no different, and type safety is preserved!
             */

            return new List<Sketch.Shape>(Data.Utils.filterValues(shapesToElements, x => { return elements.Contains(x); }).Keys);
        }

        /// <summary>
        /// Returns the list of shapes corresponding to inputs.
        /// </summary>
        public List<Shape> InputShapes
        {
            get
            {
                // Following code is attempts at sorting the inputs
                List<Sketch.Shape> inputs = shapesForElements(globalInputs);
                if (areInputsHorizontal(inputs))
                    inputs.Sort(CompareByWidth);
                else
                    inputs.Sort(CompareByHeight);
                return inputs;
            }
        }

        public List<Shape> OutputShapes
        {
            get
            {
                // Following code is attempts at sorting the outputs

                List<Sketch.Shape> outputs = shapesForElements(globalOutputs);
                if (areOutputsHorizontal(outputs))
                    outputs.Sort(CompareByWidth);
                else
                    outputs.Sort(CompareByHeight);
                return outputs;
            }
        }

        public List<OUTPUT> Outputs
        {
            get
            {
                return globalOutputs;
            }
        }

        public List<Gate> AllGates
        {
            get
            {
                return this.gates;
            }
        }

        /// <summary>
        /// Determine whether the circuit is in an oscillating state.
        /// </summary>
        public bool IsOscillating
        {
            get { return isOscillating; }
        }

        /// <summary>
        /// Will be necessary when we want to sort global IOs.
        /// </summary>
        private static int CompareByHeight(Shape x, Shape y)
        {
            return (int)Math.Ceiling(x.Bounds.TopLeft.Y - y.Bounds.TopLeft.Y);
        }

        private static int CompareByWidth(Shape x, Shape y)
        {
            return (int)Math.Ceiling(x.Bounds.Left - y.Bounds.Left);
        }

        /// <summary>
        /// given the shapes of the inputs, find out if the horizontal range
        /// is greater than the vertical to get a good guess at the orientation
        /// of the inputs. 
        /// returns true if it is horizontal and false if vertical
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <returns></returns>
        private bool areInputsHorizontal(List<Shape> inputs)
        {
            double highestInputX = 0;
            double lowestInputX = double.PositiveInfinity;
            double highestInputY = 0;
            double lowestInputY = double.PositiveInfinity;

            foreach (Shape input in inputs)
            {
                if (input.Bounds.Left < lowestInputX)
                    lowestInputX = input.Bounds.Left;
                if (input.Bounds.Right > highestInputX)
                    highestInputX = input.Bounds.Right;
                if (input.Bounds.Top < lowestInputY)
                    lowestInputY = input.Bounds.Top;
                if (input.Bounds.Bottom > highestInputY)
                    highestInputY = input.Bounds.Bottom;
            }
            inputsHoriz = ((highestInputX - lowestInputX) > (highestInputY - lowestInputY));
            return inputsHoriz;

        }

        /// <summary>
        /// given the shapes outputs, find out if the horizontal range
        ///  is greater than the vertical to get a good guess at the orientation
        /// of the outputs. 
        /// returns true if it is horizontal and false if vertical
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <returns></returns>
        private bool areOutputsHorizontal(List<Shape> outputs)
        {
            double highestOutputX = 0;
            double lowestOutputX = double.PositiveInfinity;
            double highestOutputY = 0;
            double lowestOutputY = double.PositiveInfinity;
            foreach (Shape output in outputs)
            {
                if (output.Bounds.Left < lowestOutputX)
                    lowestOutputX = output.Bounds.Left;
                if (output.Bounds.Right > highestOutputX)
                    highestOutputX = output.Bounds.Right;
                if (output.Bounds.Top < lowestOutputY)
                    lowestOutputY = output.Bounds.Top;
                if (output.Bounds.Bottom > highestOutputY)
                    highestOutputY = output.Bounds.Bottom;

            }
            outputsHoriz = ((highestOutputX - lowestOutputX) > (highestOutputY - lowestOutputY));
            return outputsHoriz;

        }


        /// <summary>
        /// Returns the dictionary containing the circuit elements in the circuit.
        /// </summary>
        public Dictionary<Sketch.Shape, CircuitElement> ShapesToElements
        {
            get { return shapesToElements; }
        }

        #endregion
        
        #region Public Methods
       
        /// <summary>
        /// calculate all the values of the outputs of the circuit, given input values. also takes care of oscillations
        /// </summary>
        public void calculateOutputs()
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            foreach (OUTPUT output in globalOutputs)
            {
                int[] outputValueArray = output.calculate();
                if (outputValueArray == null)
                    isOscillating = true;
                else
                    dict[output.Name] = outputValueArray[0];
            }

            if (isOscillating)
            {
                MessageBox.Show("An Oscillation has been detected, which cannot be simulated", "Oscillation Apparent");
            }
        }


        /// <summary>
        /// Change a value of an individual input
        /// </summary>
        /// <param name="inputName"></param>
        /// <param name="inputValue"></param>
        public void setInputValue(Shape inputShape, int inputValue)
        {
            ((INPUT)shapesToElements[inputShape]).Value = inputValue;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get output "index" from the given gate
        /// </summary>
        /// <param name="gateName"></param>
        /// <returns></returns>
        public int gateValue(Shape gate, int index)
        {
            return shapesToElements[gate].Output[index];
        }

        #endregion

    }

}
