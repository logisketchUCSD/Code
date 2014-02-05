using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CircuitParser
{

    /// <summary>
    /// A circuit input supplies a value (0 or 1) to the circuit.
    /// </summary>
    class CircuitInput : CircuitComponent
    {
        #region CONSTRUCTOR

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shape">The shape associated with
        /// the new CircuitInput</param>
        public CircuitInput(Sketch.Shape shape) : base(shape)
        {
            // Do nothing
        }

        #endregion

        #region CONNECT CIRCUIT PARTS

        /// <summary>
        /// Connects a wire-mesh to the invoking circuit input, making
        /// the input the input/source of the wire.
        /// </summary>
        /// <param name="wire">The wire to connect to</param>
        public void Connect(WireMesh wire)
        {
            // If we already connected this wire, we're done!
            if (_outputWires.Contains(wire))
                return;

            _outputWires.Add(wire);

            // Make sure the wire has the same connection
            wire.ConnectSource(this, 0);
        }

        #endregion
    }

    /// <summary>
    /// A circuit output reads a value from the circuit.
    /// </summary>
    class CircuitOutput : CircuitComponent
    {
        #region CONSTRUCTOR

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shape">The shape associated with
        /// the new CircuitOutput</param>
        public CircuitOutput(Sketch.Shape shape) : base(shape)
        {
            // Do Nothing
        }

        #endregion

        #region CONNECT CIRCUIT PARTS

        /// <summary>
        /// Connects a wire-mesh to the invoking circuit output, making
        /// the circuit output's value dependent on this wire.
        /// </summary>
        /// <param name="wire">The wire to connect</param>
        public void Connect(WireMesh wire)
        {
            // If we already connected to this wire, we're done!
            if (_outputWires.Contains(wire) || _inputWires.ContainsKey(wire))
                return;

            // If this wire doesn't come from somewhere, it comes from here.  Otherwise, it gives us value.
            if (!wire.HasSource)
            {
                _outputWires.Add(wire);
                wire.ConnectSource(this, 0);
            }
            else
            {
                _inputWires[wire] = wire.ConnectedEndpoints(this);
                wire.ConnectDependent(this);
            }
        }

        /// <summary>
        /// The wire this output gets its value from
        /// </summary>
        public WireMesh SourceWire
        {
            get
            {
                return _inputWires.Keys.ElementAt(0);
            }
        }

        #endregion
    }

}