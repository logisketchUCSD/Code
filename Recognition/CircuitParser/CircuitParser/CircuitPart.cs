using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CircuitParser
{
    /// <summary>
    /// An abstract class for all circuit parts used in the
    /// circuit parser. Allows for specialization of different
    /// kinds of circuit parts, especially when it comes to the 
    /// nature of their input/output connections.
    /// </summary>
    abstract class CircuitPart
    {
        /// <summary>
        /// All circuit parts are associated with a 
        /// shape in the sketch.
        /// </summary>
        private readonly Sketch.Shape _associatedShape;

        /// <summary>
        /// Construct a circuit part for the given shape.
        /// </summary>
        /// <param name="shape">the shape this circuit part represents</param>
        public CircuitPart(Sketch.Shape shape)
        {
            _associatedShape = shape;
        }

        /// <summary>
        /// Get the associated shape.
        /// </summary>
        public Sketch.Shape Shape
        {
            get { return _associatedShape; }
        }

        /// <summary>
        /// Get the name of the Circuit Part, 
        /// which should be unique.
        /// </summary>
        public string Name
        {
            get { return _associatedShape.Name; }
        }

        /// <summary>
        /// Retreive the type of circuit part.
        /// </summary>
        public Domain.ShapeType Type
        {
            get { return Shape.Type; }
        }

        /// <summary>
        /// Gets the bounds of the circuit part.
        /// </summary>
        public System.Windows.Rect Bounds
        {
            get { return _associatedShape.Bounds; }
        }

    }
}

