using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CircuitParser
{

    class WireMesh : CircuitPart
    {
        #region INTERNALS

        /// <summary>
        /// In essence, the wire's input:
        /// the one circuit component that serves as the source
        /// of whatever signal travels through this wire mesh.
        /// </summary>
        CircuitComponent _sourceComponent;

        /// <summary>
        /// The index of the input we're connected to
        /// </summary>
        int _sourceIndex;

        /// <summary>      
        /// In essence, the wire's outputs:
        /// All the circuit components that take and depend on 
        /// signals from this wire mesh as inputs.
        /// </summary>
        List<CircuitComponent> _dependentComponents;

        /// <summary>
        /// The endpoints of the wire-mesh.
        /// </summary>
        HashSet<Sketch.EndPoint> _endPoints;

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Default constructor for making a new, 
        /// unconnected wire-mesh associated with a
        /// given shape.
        /// </summary>
        /// <param name="shape">The shape associated
        /// with the new wire</param>
        public WireMesh(Sketch.Shape shape)
            : base(shape)
        {
            _sourceComponent = null;
            _dependentComponents = new List<CircuitComponent>();
            loadEndpoints();
        }

        #endregion

        #region CONNECT CIRCUIT PARTS

        /// <summary>
        /// Connects a circuit component as the input/source of the
        /// invoking wire.
        /// </summary>
        /// <param name="component">The component to connect</param>
        /// <returns>A Parse Error if an error arises, otherwise null</returns>
        public void ConnectSource(CircuitComponent component, int index)
        {
            // If we already made this connection, we're done!
            if (_sourceComponent == component)
                return;

            // Make the connection.
            _sourceComponent = component;
            _sourceIndex = index;

            foreach (CircuitComponent child in this.Dependents)
                child.ConnectInput(component, index);

            // Make sure the circuit component also has this connection
            if (component is LogicGate)
                ((LogicGate)component).ConnectOutput(this);
            else if (component is CircuitInput)
                ((CircuitInput)component).Connect(this);
            else if (component is CircuitOutput)
                ((CircuitOutput)component).Connect(this);
        }

        /// <summary>
        /// Connects a circuit component as an output/dependent of the
        /// invoking wire.
        /// </summary>
        /// <param name="component">The component to connect</param>
        public void ConnectDependent(CircuitComponent component)
        {
            // If we already made this connection, we're done!
            if (!_dependentComponents.Contains(component))
                _dependentComponents.Add(component);
            else return;

            if (Source != null)
                component.ConnectInput(Source, SourceIndex);

            // Make sure the circuit component also has this connection
            if (component is LogicGate)
            {
                ((LogicGate)component).ConnectInput(this);
            }
            else if (component is CircuitOutput)
                ((CircuitOutput)component).Connect(this);
            else if (component is CircuitInput)
                ((CircuitInput)component).Connect(this);
        }

        #endregion

        #region ENDPOINTS

        /// <summary>
        /// Gets the endpoints in this wiremeesh which are connected to the specified circuit component.
        /// </summary>
        /// <param name="otherShape">The circuit component we're looking for connections to</param>
        /// <returns>A list of the endpoints in this shape connected to the specified component</returns>
        public List<Sketch.EndPoint> ConnectedEndpoints(CircuitComponent otherShape)
        {
            List<Sketch.EndPoint> connectedEndpoints = new List<Sketch.EndPoint>();

            foreach (Sketch.EndPoint endpoint in Endpoints)
                if (endpoint.ConnectedShape == otherShape.Shape)
                    connectedEndpoints.Add(endpoint);

            return connectedEndpoints;
        }

        private void myAssert(bool cond)
        {
#if DEBUG
            if (!cond)
                throw new Exception();
#else
            Console.WriteLine("Assert failed in WireMesh, ignoring...");
#endif
        }

        private void loadEndpoints()
        {
            // Here we make a bunch of assertions about what must be true.
            // It is the responsibility of CircuitDomain to set these.

            // For every connected shape, there is an endpoint connected to it.
            foreach (Sketch.Shape shape in Shape.ConnectedShapes)
            {
                bool found = false;
                Sketch.EndPoint theEndpoint = null;
                foreach (Sketch.EndPoint endpoint in Shape.Endpoints)
                {
                    if (endpoint.ConnectedShape == shape)
                    {
                        found = true;
                        theEndpoint = endpoint;
                        break;
                    }
                }

                myAssert(found);
            }

            // For every endpoint, if it is connected to a shape, that shape is in the list of connected shapes
            foreach (Sketch.EndPoint endpoint in Shape.Endpoints)
                if (endpoint.ConnectedShape != null)
                    myAssert(Shape.ConnectedShapes.Contains(endpoint.ConnectedShape));

            // Any connected wires should have been merged by now
            foreach (Sketch.Shape shape in Shape.ConnectedShapes)
            {
                if (shape != Shape)
                    myAssert(shape.Type != Domain.LogicDomain.WIRE);
            }

            // Every endpoint should be in _endPoints
            _endPoints = new HashSet<Sketch.EndPoint>();
            foreach (Sketch.EndPoint endpoint in Shape.Endpoints)
                _endPoints.Add(endpoint);

        }

        #endregion

        #region GETTERS

        /// <summary>
        /// Gets whether or not this wire mesh has a source
        /// </summary>
        public bool HasSource
        {
            get
            {
                return (_sourceComponent != null);
            }
        }

        /// <summary>
        /// Gets the circuit component this wire mesh is getting a value from
        /// </summary>
        public CircuitComponent Source
        {
            get { return _sourceComponent; }
        }

        /// <summary>
        /// Gets all the components that depend on the value / 
        /// signal from this wire-mesh, i.e. all the components 
        /// that have this wire-mesh has an input.
        /// </summary>
        public List<CircuitComponent> Dependents
        {
            get { return _dependentComponents; }
        }

        /// <summary>
        /// Generates and returns a list of all the circuit
        /// components connected to this wire-mesh.
        /// </summary>
        public List<CircuitComponent> ConnectedComponents
        {
            get
            {
                List<CircuitComponent> components =
                    new List<CircuitComponent>(_dependentComponents);

                components.Add(_sourceComponent);

                return components;
            }
        }

        /// <summary>
        /// Gets all the endpoints of the wiremesh
        /// </summary>
        public HashSet<Sketch.EndPoint> Endpoints
        {
            get { return _endPoints; }
        }

        /// <summary>
        /// Gets the output index this wire is connected to
        /// </summary>
        public int SourceIndex
        {
            get { return _sourceIndex; }
        }

        #endregion
    }
}
