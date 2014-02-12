using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace CircuitSimLib
{
    #region CircuitElement

    public abstract class CircuitElement
    {
        #region Internals

        /// <summary>
        /// the outputs represented in 1 or 0
        /// </summary>
        protected int[] outputValues;
        
        /// <summary>
        /// the previous output
        /// </summary>
        protected int[] previousOutputValues;

        /// <summary>
        /// name of this circuit
        /// </summary>
        protected String name;

        /// <summary>
        /// type of CircuitElement (i.e. Gate, Input, or Output)
        /// </summary>
        protected String type;

        /// <summary>
        /// List of inputs for each input port of this element. Each port has a connection, which consists of an 
        /// element and its output port.
        /// </summary>
        private Dictionary<int, Tuple<CircuitElement, int>> inputConnections;

        /// <summary>
        /// list of all parent CircuitElement of this CircuitElement.  Each sublist corresponds to an output of the gate
        /// Read as Dictionary<output port, Dictionary<element, List<input ports>>>
        /// </summary>
        protected Dictionary<int, Dictionary<CircuitElement, List<int>>> outputComponents;

        /// <summary>
        /// The bounds of this element's corresponding shape
        /// </summary>
        protected Rect bounds;

        /// <summary>
        /// The bounds of this element in the clean circuit
        /// </summary>
        private Rect cleanBounds;

        /// <summary>
        /// The orientation of this element, in radians.
        /// </summary>
        protected double orientation;

        #endregion

        #region Constructor

        /// <summary>
        /// Create empty lists.
        /// </summary>
        public CircuitElement(string name, Rect bounds, double orientation)
        {
            inputConnections = new Dictionary<int, Tuple<CircuitElement, int>>();
            outputComponents = new Dictionary<int, Dictionary<CircuitElement, List<int>>>();
            this.orientation = orientation;
            this.bounds = bounds;
            this.name = name;
        }

        #endregion

        #region Getters and Setters
        /// <summary>
        ///gives the name of the CircuitElement 
        /// </summary>
        /// <returns></returns>
        public String Name
        {
            get
            {
                return this.name;
            }
            set
            {
                name = value;
            }
        }

        /// <summary>
        /// gives the output of the CircuitElement
        /// </summary>
        /// <returns></returns>
        public int[] Output
        {
            get { return outputValues; }
        }

        /// <summary>
        /// For each input index, we store the source element and the source index.
        /// </summary>
        public Dictionary<int, Tuple<CircuitElement, int>> InputPorts
        {
            get { return inputConnections; }
        }

        /// <summary>
        /// For each output index, we store a list of destination elements and
        /// the destination indices.
        /// </summary>
        public Dictionary<int, Dictionary<CircuitElement, List<int>>> Outputs
        {
            get { return outputComponents; }
        }

        /// <summary>
        /// gives the type of CircuitElement
        /// </summary>
        public String Type
        {
            get { return type; }
        }

        /// <summary>
        /// The bounds of the shape corresponding to this circuit element
        /// </summary>
        public Rect Bounds
        {
            get { return bounds; }
        }

        /// <summary>
        /// The bounds of the elemen in the clean circuit
        /// </summary>
        public Rect CleanBounds
        {
            get { return cleanBounds; }
            set { cleanBounds = value; }
        }

        /// <summary>
        /// The orientation of this shape, in radians
        /// </summary>
        public double Orientation
        {
            get { return orientation; }
        }

        /// <summary>
        /// The angle of the shape, rounded to the nearest 90 degrees
        /// </summary>
        public int Angle
        {
            get
            {
                // Convert orientation in radians to degrees.
                double angle = orientation * 180 / Math.PI;

                // Round to the nearest 90 between 0 and 360.
                int rightAngle = (int)(90 * Math.Round(angle / 90)) % 360;
                return rightAngle;
            }
        }
        #endregion
        
        #region Methods

        /// <summary>
        /// wires two CircuitElements (makes an added element its input)
        /// </summary>
        /// <param name="sourceElement"></param>
        public void addInput(CircuitElement sourceElement, int sourceIndex, int destIndex)
        {
            inputConnections[destIndex] = Tuple.Create(sourceElement, sourceIndex);
            sourceElement.addOutput(sourceIndex, this, destIndex);
        }

        /// <summary>
        /// adds the parent circuitelement so that the child can refer to it (helper for addchild)
        /// (you are the left element)
        /// </summary>
        /// <param name="destElement"></param>
        private void addOutput(int sourceIndex, CircuitElement destElement, int destIndex)
        {
            if (!outputComponents.ContainsKey(sourceIndex))
                outputComponents[sourceIndex] = new Dictionary<CircuitElement, List<int>>();
            if (!outputComponents[sourceIndex].ContainsKey(destElement))
                outputComponents[sourceIndex][destElement] = new List<int>();
            if (!outputComponents[sourceIndex][destElement].Contains(destIndex))
                outputComponents[sourceIndex][destElement].Add(destIndex);
        }

        /// <summary>
        /// Returns true if this element has the given parent
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public bool hasParent(CircuitElement parent)
        {
            foreach (Dictionary<CircuitElement, List<int>> dict in Outputs.Values)
                foreach (CircuitElement elem in dict.Keys)
                    if (elem == parent) return true;
            return false;
        }

        /// <summary>
        /// given input values, CircuitElements gives list of ouput values
        /// </summary>
        /// <returns></returns>
        public abstract int[] calculate();

        /// <summary>
        /// Gets where a wire should connect to be an input to this element
        /// </summary>
        /// <param name="numInputsAdded"></param>
        /// <returns></returns>
        public Point InPoint(int inputIndex, bool clean = false)
        {
            // Are we in the clean circuit?
            Rect theseBounds = Bounds;
            if (clean && CleanBounds != null) theseBounds = CleanBounds;
            inputIndex += 1;

            // Get the point depending on how we're oriented
            Point end = new Point();
            if (Angle == 0)
            {
                end = theseBounds.TopLeft;
                end.Y += inputIndex * theseBounds.Height / (InputPorts.Count + 1);
            }
            else if (Angle == 90)
            {
                end = theseBounds.TopRight;
                end.X -= inputIndex * theseBounds.Width / (InputPorts.Count + 1);
            }
            else if (Angle == 180)
            {
                end = theseBounds.BottomRight;
                end.Y -= inputIndex * theseBounds.Height / (InputPorts.Count + 1);
            }
            else if (Angle == 270)
            {
                end = theseBounds.BottomLeft;
                end.X += inputIndex * theseBounds.Width / (InputPorts.Count + 1);
            }
            else  // If we're not snapping angles in Gate Drawing, we'll get here
            {
                end = theseBounds.TopLeft;
                end.Y += inputIndex * theseBounds.Height / (InputPorts.Count + 1);
            }
            return end;
        }

        /// <summary>
        /// Returns the point where wires should connect to this element to recieve its output given the index of the output 
        /// Default index is 0.
        /// </summary>
        public Point OutPoint(int index = 0, bool clean = false)
        {
            // Are we in the clean circuit?
            Rect theseBounds = Bounds;
            if (clean && CleanBounds != null) theseBounds = CleanBounds;

            // Get the point based on the shape's orientation
            Point outPoint = new Point();
            if (Angle == 0)
                outPoint = new Point(theseBounds.Right, theseBounds.Top + (index + 1) * theseBounds.Height / (outputValues.Count() + 1));
            else if (Angle == 90)
                outPoint = new Point(theseBounds.Right - (index + 1) * theseBounds.Width / (outputValues.Count() + 1), theseBounds.Bottom);
            else if (Angle == 180)
                outPoint = new Point(theseBounds.Left, theseBounds.Bottom - (index + 1) * theseBounds.Height / (outputValues.Count() + 1));
            else if (Angle == 270)
                outPoint = new Point(theseBounds.Left + (index + 1) * theseBounds.Width / (outputValues.Count() + 1), theseBounds.Top);
            else  // If we're not snapping angles in Gate Drawing, we'll get here
                outPoint = new Point(theseBounds.Right, theseBounds.Top + (index + 1) * theseBounds.Height / (outputValues.Count() + 1));

            return outPoint;
        }

        #endregion
    }

    #endregion

    #region Logic Gates

    #region Gates

    /// <summary>
    /// One concrete subclass of the CircuitElement, also a superclass
    /// </summary>
    public abstract class Gate : CircuitElement
    {
        #region Internals

        /// <summary>
        /// type of gate (AND, OR, NOT, etc.)
        /// </summary>
        protected ShapeType gateType;

        protected bool CurrentlyCalculating;

        #endregion

        #region Getters

        /// <summary>
        /// returns the type of gate
        /// </summary>
        public ShapeType GateType
        {
            get { return this.gateType; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor of a Gate given a name
        /// </summary>
        /// <param name="gateName"></param>
        public Gate(String gateName, Rect bounds, double orientation = 0)
            : base(gateName, bounds, orientation)
        {
            type = "Gate";
            outputValues = new int[1];
            outputValues[0] = 0;
            previousOutputValues = outputValues;
            CurrentlyCalculating = false;
        }

        #endregion

        #region Calculate

        public override int[] calculate()
        {
            if (!startFirstTime())
                return null;

            int[] inputs = new int[InputPorts.Count];

            // Go through each input and check its value.
            foreach (var rightIndexAndleftPort in InputPorts)
            {
                int rightIndex = rightIndexAndleftPort.Key;
                Tuple<CircuitElement, int> leftPort = rightIndexAndleftPort.Value;
                CircuitElement leftElement = leftPort.Item1;
                int leftIndex = leftPort.Item2;
                int[] childCalc = leftElement.calculate();
                if (childCalc == null)
                    return null;
                inputs[rightIndex] = childCalc[leftIndex];
            }

            outputValues = computeOutput(inputs.ToArray());

            if (outputValues.Length != Outputs.Count)
                throw new Exception("Gate returned " + outputValues.Length + " outputs; should have returned " + Outputs.Count);

            endCalculation();

            return outputValues;
        }

        /// <summary>
        /// Compute the output values given a list of inputs. Inputs are in the correct order.
        /// Outputs are expected to be in the correct order as well.
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public abstract int[] computeOutput(int[] inputs);

        /// <summary>
        /// Uses the currentlycalculating flag to determine if it's already trying to calculate
        /// returns false if it is already calculating
        /// </summary>
        /// <returns></returns>
        public bool startFirstTime()
        {
            if (CurrentlyCalculating)
                return false;
            CurrentlyCalculating = true;
            return true;
        }

        public void endCalculation()
        {
            CurrentlyCalculating = false;
        }


        #endregion

    }
    
    /// <summary>
    /// A gate with only one output.
    /// </summary>
    public abstract class SingleOutputGate : Gate
    {
        public SingleOutputGate(String gateName, Rect bounds, double orientation = 0)
            : base(gateName, bounds, orientation)
        {
        }

        public abstract int computeOutputValue(int[] inputs);

        public override int[] computeOutput(int[] inputs)
        {
            int result = computeOutputValue(inputs);
            return new int[] { result };
        }
    }

    /// <summary>
    /// Negates the output from another gate.
    /// </summary>
    public class NegatedGate : Gate
    {
        private Gate _innerGate;
        public NegatedGate(Gate gate)
            :base(gate.Name, gate.Bounds, gate.Orientation)
        {
            _innerGate = gate;
        }
        public override int[] computeOutput(int[] inputs)
        {
            int[] results = _innerGate.computeOutput(inputs);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] == 1)
                    results[i] = 0;
                else
                    results[i] = 1;
            }
            return results;
        }
    }

    #endregion 

    #region AND

    public class AND : SingleOutputGate
    {
       #region Constructor
        /// <summary>
        /// Constructor of an AND gate, given a name
        /// </summary>
        /// <param name="gateName"></param>
       public AND(String gateName, Rect bounds, double orientation = 0): 
           base(gateName, bounds, orientation)
        {
            gateType = LogicDomain.AND;
        }

        #endregion

       #region Methods

       /// <summary>
       /// gives an output, using properties of an AND gate
       /// </summary>
       /// <returns></returns>
       public override int computeOutputValue(int[] inputs)
       {
           foreach (int i in inputs)
               if (i == 0)
                   return 0;
           return 1;
       }

       #endregion

    }

    #endregion

    #region OR

    public class OR : SingleOutputGate
    {
        #region Constructor

        /// <summary>
        /// constructor of an OR gate, given a name
        /// </summary>
        /// <param name="gateName"></param>
        public OR(String gateName, Rect bounds, double orientation = 0): 
            base(gateName, bounds, orientation)
        {
            gateType = LogicDomain.OR;
        }

        #endregion

        #region Methods

        /// <summary>
        /// gives an output, using properties of an OR gate
        /// </summary>
        /// <returns></returns>
        public override int  computeOutputValue(int[] inputs)
        {
            foreach (int i in inputs)
                if (i == 1)
                    return 1;
            return 0;
        }

        #endregion
    }

    #endregion

    #region NOT

    public class NOT : SingleOutputGate
    {
        #region Constructor

        /// <summary>
        /// Constructor of a NOT gate, given a name
        /// </summary>
        /// <param name="gateName"></param>
        public NOT(String gateName, Rect bounds, double orientation = 0) :
            base(gateName, bounds, orientation)
        {
            gateType = LogicDomain.NOT;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the output of a NOT gate.
        /// </summary>
        /// <returns></returns>
        public override int computeOutputValue(int[] inputs)
        {
            if (inputs.Length != 1)
                throw new Exception("NOT gate should have a single input!");
            return (inputs[0] == 0) ? 1 : 0;
        }

        #endregion
    }

    #endregion

    #region XOR

    public class XOR : SingleOutputGate
    {
        #region Constructor

        /// <summary>
        /// Constructor of a XOR, given a name
        /// </summary>
        /// <param name="gateName"></param>
        public XOR(String gateName, Rect bounds, double orientation = 0) :
            base(gateName, bounds, orientation)
        {
            gateType = LogicDomain.XOR;
        }

        #endregion

        #region Methods

        public override int computeOutputValue(int[] inputs)
        {
            int result = 0;
            foreach (int i in inputs)
                result ^= i;
            return result;
        }

        #endregion

    }

    #endregion

    #region XNOR

    public class XNOR : NegatedGate
    {

        #region Constructor

        /// <summary>
        /// Constructor of a XNOR gate, give a name
        /// </summary>
        /// <param name="gateName"></param>
        public XNOR(String gateName, Rect bounds, double orientation = 0) :
            base(new XOR(gateName, bounds, orientation))
        {
            gateType = LogicDomain.XNOR;
        }

        #endregion

    }

    #endregion

    #region NAND

    public class NAND : NegatedGate
    {
        #region Constructor

        /// <summary>
        /// Constructor of a NAND gate, given a name
        /// </summary>
        /// <param name="gateName"></param>
        public NAND(String gateName, Rect bounds, double orientation = 0) :
            base(new AND(gateName, bounds, orientation))
        {
            gateType = LogicDomain.NAND;
        }

        #endregion
    }

    #endregion

    #region NOR

    public class NOR : NegatedGate
    {
        #region Constructor

        /// <summary>
        /// Constructor of a NOR gate, given a name
        /// </summary>
        /// <param name="gateName"></param>
        public NOR(String gateName, Rect bounds, double orientation = 0) :
            base(new OR(gateName, bounds, orientation))
        {
            gateType = LogicDomain.NOR;
        }

        #endregion

    }
    #endregion

    #region NOTBUBBLE

    public class NOTBUBBLE : NOT
    {
        #region Constructor

        /// <summary>
        /// Constructor of a NOTBUBBLE gate, given a name
        /// </summary>
        /// <param name="gateName"></param>
        public NOTBUBBLE(String gateName, Rect bounds, double orientation = 0) :
            base(gateName, bounds, orientation)
        {
            gateType = LogicDomain.NOTBUBBLE;
        }

        #endregion
    }


    #endregion

    #endregion

    #region Sub-Circuit

    public class SubCircuit : Gate
    {
        /// <summary>
        /// A dictionary to look up what this gate is supposed to do. To be loaded in
        /// </summary>
        public Dictionary<List<int>, List<int>> behavior;

        /// <summary>
        /// The number of inputs that this circuit needs to function
        /// </summary>
        public int inputsRequired;

        /// <summary>
        /// The number of outputs that this circuit needs to function
        /// </summary>
        public int outputsRequired;

        /// <summary>
        /// List of input names.
        /// </summary>
        public List<string> inputs;

        /// <summary>
        /// List of output names.
        /// </summary>
        public List<string> outputs;

        public Sketch.Shape shape;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="gateName"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        public SubCircuit(String gateName, Rect bounds, Sketch.Shape shape,
                            List<string> inputs, List<string> outputs, Dictionary<List<int>, List<int>> behavior, double orientation = 0) :
            base(gateName, bounds, orientation)
        {
            this.inputs = inputs;
            this.outputs = outputs;
            gateType = LogicDomain.SUBCIRCUIT;
            this.shape = shape;
            this.behavior = behavior;
            train(inputs, outputs, behavior);
        }

        public void train(List<string> inputs, List<string> outputs, Dictionary<List<int>, List<int>> behavior)
        {
            inputsRequired = inputs.Count;
            outputsRequired = outputs.Count;
            outputValues = new int[outputs.Count];

            this.behavior = behavior;
        }

        public override int[] computeOutput(int[] inputs)
        {
            int[] result = null;
            // Get the output list from the behavior.
            foreach (KeyValuePair<List<int>, List<int>> pair in behavior)
            {
                bool matchedBehavior = true;

                // Is this the correct row?
                for (int i = 0; i < pair.Key.Count; i++)
                {
                    if (inputs[i] != pair.Key[i])
                    {
                        matchedBehavior = false;
                        break;
                    }
                }
                if (matchedBehavior)
                {
                    result = pair.Value.ToArray();
                    break;
                }

            }
            return result;
        }

    }
    #endregion

    #region OUTPUT

    public class OUTPUT : CircuitElement
    {

        #region Constructor

        /// <summary>
        /// Constructor of an output, given a name and the gate in which it is connected
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sourceGate"></param>
        public OUTPUT(String name, Rect bounds, double orientation = 0)
            : base(name, bounds, orientation)
        {
            type = "Output";
            outputValues = new int[1];
        }

        #endregion

        #region Methods

        /// <summary>
        /// gives the output of the connected gate
        /// </summary>
        /// <returns></returns>
        public override int[] calculate()
        {
            if (InputPorts.Count > 1)
                throw new Exception("output has too many inputs!");
            if (!InputPorts.ContainsKey(0))
                throw new Exception("output is missing input 0!");

            Tuple<CircuitElement, int> source = InputPorts[0];
            int[] childVals = source.Item1.calculate();
            if (childVals == null)
                return null;
            outputValues[0] = childVals[source.Item2];
            return outputValues;
        }

        #endregion

    }

    #endregion

    #region INPUT

    public class INPUT : CircuitElement
    {
        #region Constructor
        
        /// <summary>
        /// Constructor of an input, given a name
        /// </summary>
        /// <param name="name"></param>
        public INPUT(string name, Rect bounds, double orientation = 0)
            : base(name, bounds, orientation)
        {
            type = "Input";
            outputValues = new int[1];
        }

        #endregion

        #region Getters and Setters

        /// <summary>
        /// get or set the value of the input
        /// </summary>
        public int Value
        {
            get { return outputValues[0]; }
            set { outputValues[0] = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// gives the input value
        /// </summary>
        /// <returns></returns>
        public override int[] calculate()
        {
            return outputValues;
        }

        #endregion

    }

    #endregion 
}