using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitParser
{

    /// <summary>
    /// A class to further differentiate circuit parts.
    /// Circuit components refer to gates, circuit inputs, and 
    /// circuit outputs. CircuitComponents are connected to
    /// each other by WireMeshes.
    /// </summary>
    /// <seealso cref="WireMesh"/>
    abstract class CircuitComponent : CircuitPart
    {

        #region INTERNALS

        /// <summary>
        /// A dictionary of all connected wires and the endpoints associated with them
        /// </summary>
        protected Dictionary<WireMesh, List<Sketch.EndPoint>> _inputWires;

        /// <summary>
        /// The wires connected as an outputs.
        /// </summary>
        protected List<WireMesh> _outputWires;

        /// <summary>
        /// Logic gates or circuit components that are indirectly 
        /// connected as inputs through a single wire-mesh.
        /// 
        /// Assumption: no one will put CircuitOutputs in the
        /// list of input components.
        /// </summary>
        protected Dictionary<CircuitComponent, List<int>> _inputComponents;

        /// <summary>
        /// A ditionary of input ports to input components
        /// </summary>
        protected Dictionary<int, Tuple<CircuitComponent, int>> _inputDictionary;

        /// <summary>
        /// Logic gates or circuit components that are indirectly 
        /// connected as outputs through a single wire-mesh.
        /// 
        /// Assumption: no one will put a CircuitInput in the list of
        /// outputs components.
        /// </summary>
        protected List<List<CircuitComponent>> _outputComponents;

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Constructs a new, unconnected logic gate 
        /// associated with a given shape.
        /// </summary>
        /// <param name="shape">The shape associated
        /// with the new logic gate</param>
        public CircuitComponent(Sketch.Shape shape)
            : base(shape)
        {
            _inputWires = new Dictionary<WireMesh, List<Sketch.EndPoint>>();
            _outputWires = new List<WireMesh>();
            _inputComponents = new Dictionary<CircuitComponent, List<int>>();
            _inputDictionary = new Dictionary<int, Tuple<CircuitComponent, int>>();
            _outputComponents = new List<List<CircuitComponent>>();
        }

        #endregion

        #region CONNECT CIRCUIT PARTS

        /// <summary>
        /// Connects a wire-mesh as an input to the invoking logic gate.
        /// </summary>
        /// <param name="wire">The wire-mesh to connect</param>
        public void ConnectInput(WireMesh wire)
        {
            List<Sketch.EndPoint> connectedEndpoints = wire.ConnectedEndpoints(this);
            _inputWires[wire] = connectedEndpoints;
        }

        /// <summary>
        /// Sorts and connects all inputs
        /// </summary>
        /// <returns></returns>
        public void ConnectAllInputs()
        {
            foreach (Sketch.EndPoint endpoint in InputEndpoints)
            {
                WireMesh wire = EndpointsToWires[endpoint];
                wire.ConnectDependent(this);//, _inputWires.IndexOf(wire));
            }
            OrderInputs();
        }

        /// <summary>
        /// Sorts inputs by where they are on screen, constructs the input dictionary
        /// </summary>
        public void OrderInputs()
        {
            // Get all the endpoints
            List<Sketch.EndPoint> connectedEndpoints = InputEndpoints;
            Dictionary<Sketch.EndPoint, CircuitComponent> endpointsToNotBubbles = new Dictionary<Sketch.EndPoint, CircuitComponent>();

            // Take care of any not bubbles
            foreach (CircuitComponent connected in _inputComponents.Keys)
                if (Domain.LogicDomain.IsGate(connected.Type) && connected.Shape.ConnectedShapes.Contains(Shape) && connected.InputEndpoints.Count > 0)
                {
                    connectedEndpoints.Add(connected.InputEndpoints[0]);
                    endpointsToNotBubbles[connected.InputEndpoints[0]] = connected;
                }

            // Sort all these endpoints
            connectedEndpoints.Sort(EndpointSort);

            // Make the input dictionary
            _inputDictionary.Clear();
            for (int index = 0; index < connectedEndpoints.Count; index++)
            {
                if (EndpointsToWires.ContainsKey(connectedEndpoints[index]))
                {
                    WireMesh wire = EndpointsToWires[connectedEndpoints[index]];
                    if (wire.HasSource)
                        _inputDictionary[index] = new Tuple<CircuitComponent, int>(wire.Source, wire.SourceIndex);
                }
                else
                {
                    CircuitComponent notBubble = endpointsToNotBubbles[connectedEndpoints[index]];
                    _inputDictionary[index] = new Tuple<CircuitComponent, int>(notBubble, 0);
                }

            }
        }

        /// <summary>
        /// Connects a wire-mesh as the output to the invoking logic gate.
        /// </summary>
        /// <param name="wire">The wire-mesh to connect</param>
        public void ConnectOutput(WireMesh wire)
        {
            // If we're already connected to this wire, we're done!
            if (_outputWires.Contains(wire))
                return;

            _outputWires.Add(wire);
            return;
        }

        /// <summary>
        /// Sorts and connects all outputs
        /// </summary>
        /// <returns></returns>
        public void ConnectAllOutputs()
        {
            List<WireMesh> outputWires = new List<WireMesh>(_outputWires);
            Dictionary<WireMesh, Tuple<CircuitComponent, int>> wiresToNotbubbles = new Dictionary<WireMesh, Tuple<CircuitComponent, int>>();

            // Take care of any not bubbles
            foreach (List<CircuitComponent> list in _outputComponents)
                foreach (CircuitComponent component in list)
                    if (Domain.LogicDomain.IsGate(component.Type) && //  component.Type == Domain.LogicDomain.NOTBUBBLE &&
                        Shape.ConnectedShapes.Contains(component.Shape) &&
                        component.OutputWires.Count > 0)
                    {
                        outputWires.Add(component.OutputWires[0]);
                        wiresToNotbubbles[component.OutputWires[0]] = new Tuple<CircuitComponent, int>(component, _outputComponents.IndexOf(list));
                    }

            outputWires.Sort(WireSort);

            foreach (WireMesh wire in outputWires)
            {
                if (wiresToNotbubbles.ContainsKey(wire))
                {
                    _outputComponents[wiresToNotbubbles[wire].Item2].Remove(wiresToNotbubbles[wire].Item1);
                    wiresToNotbubbles[wire].Item1.ConnectInput(this, outputWires.IndexOf(wire));
                    wiresToNotbubbles[wire].Item1.OrderInputs();
                }
                else
                    wire.ConnectSource(this, outputWires.IndexOf(wire));
            }
        }

        /// <summary>
        /// Connects a given circuit component as an input to the 
        /// invoking logic gate.
        /// </summary>
        /// <param name="wire">The component to connect</param>
        public void ConnectInput(CircuitComponent component, int sourceindex)
        {
            // If we already made this connection, we're done!
            if (!_inputComponents.ContainsKey(component))
                _inputComponents[component] = new List<int>();

            // Make the connection.
            _inputComponents[component].Add(sourceindex);

            // Ensure that the given component also has this connection.
            component.ConnectOutput(this, sourceindex);
        }

        /// <summary>
        /// Connects a circuit component to the output of the 
        /// invoking logic gate. In other words, the output of
        /// the invoking logic gate becomes directly tied with
        /// an input or value of a given circuit component.
        /// </summary>
        /// <param name="wire">The component to connect</param>
        public void ConnectOutput(CircuitComponent component, int index)
        {
            while (_outputComponents.Count < index + 1)
            {
                _outputComponents.Add(new List<CircuitComponent>());
            }

            // If we already made this connection, we're done!
            if (_outputComponents[index].Contains(component))
                return;

            // Make the connection.
            _outputComponents[index].Add(component);

            // Ensure that the given component also has this connection.
            component.ConnectInput(this, index);
        }

        /// <summary>
        /// Returns true if this circuit component has the given component and index connected as an input
        /// </summary>
        /// <param name="component"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool HasInput(CircuitComponent component, int index)
        {
            if (_inputComponents.ContainsKey(component))
                if (_inputComponents[component].Contains(index))
                    return true;
            return false;
        }

        public bool HasOutput(CircuitComponent component, int index)
        {
            if (OutputComponents.Count >= index + 1)
                if (OutputComponents[index].Contains(component))
                    return true;
            return false;
        }

        #endregion

        #region Wire and Endpoint Sorting

        /// <summary>
        /// Compares two wires on the basis of the Y value of the endpoint that connects to this gate
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int WireSort(WireMesh a, WireMesh b)
        {
            Sketch.EndPoint aEnd = Shape.ClosestEndpointFrom(a.Shape);
            Sketch.EndPoint bEnd = Shape.ClosestEndpointFrom(b.Shape);

            return EndpointSort(aEnd, bEnd);
        }

        /// <summary>
        /// Compares two endpoints on the basis of the Y value
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int EndpointSort(Sketch.EndPoint a, Sketch.EndPoint b)
        {
            // Get the y-coordinate the first endpoint would have if this component was
            // rotated about the origin until it was perfectly horizontal,
            // pointing left to right.
            double aY =
                a.Y * Math.Cos(Shape.Orientation) +
                a.X * Math.Sin(Shape.Orientation);

            // Get the y-coordinate of the second endpoint if it underwent
            // the same transformation.
            double bY =
                b.Y * Math.Cos(Shape.Orientation) +
                b.X * Math.Sin(Shape.Orientation);

            if (aY < bY) return -1;
            else if (aY > bY) return 1;
            return 0;
        }
        #endregion

        #region GETTERS

        /// <summary>
        /// Get all the wires connected as inputs to the logic gate.
        /// </summary>
        public Dictionary<WireMesh, List<Sketch.EndPoint>> InputWires
        {
            get { return _inputWires; }
        }

        /// <summary>
        /// All the endpoints connected to this shape as inputs
        /// </summary>
        public List<Sketch.EndPoint> InputEndpoints
        {
            get
            {
                List<Sketch.EndPoint> connectedEndpoints = new List<Sketch.EndPoint>();

                foreach (List<Sketch.EndPoint> list in _inputWires.Values)
                    foreach (Sketch.EndPoint endpoint in list)
                        connectedEndpoints.Add(endpoint);

                return connectedEndpoints;
            }
        }
        /// <summary>
        /// All the endpoints connected to this shape as outputs
        /// </summary>
        public List<Sketch.EndPoint> OutputEndpoints
        {
            get
            {
                List<Sketch.EndPoint> connectedEndpoints = new List<Sketch.EndPoint>();

                foreach (WireMesh wire in _outputWires)
                    foreach (Sketch.EndPoint endpoint in wire.ConnectedEndpoints(this))
                        connectedEndpoints.Add(endpoint);

                return connectedEndpoints;
            }
        }

        /// <summary>
        /// A mapping of endpoints to their parent wiremeshes
        /// </summary>
        protected Dictionary<Sketch.EndPoint, WireMesh> EndpointsToWires
        {
            get
            {
                Dictionary<Sketch.EndPoint, WireMesh> endpointsToWires = new Dictionary<Sketch.EndPoint, WireMesh>();

                foreach (WireMesh wire in _inputWires.Keys)
                    foreach (Sketch.EndPoint endpoint in _inputWires[wire])
                        endpointsToWires[endpoint] = wire;

                return endpointsToWires;
            }
        }

        /// <summary>
        /// Get thewires connected to the gate's outputs.
        /// </summary>
        public List<WireMesh> OutputWires
        {
            get { return _outputWires; }
        }

        /// <summary>
        /// Get logic gates or circuit components that are indirectly 
        /// connected as inputs through a single wire-mesh.
        /// </summary>
        public Dictionary<int, Tuple<CircuitComponent, int>> InputComponents
        {
            get
            {
                return _inputDictionary;
            }
        }

        /// <summary>
        /// Get logic gates or circuit components that are indirectly 
        /// connected as outputs through a single wire-mesh.
        /// </summary>
        public List<List<CircuitComponent>> OutputComponents
        {
            get { return _outputComponents; }
        }

        #endregion

    }

}
