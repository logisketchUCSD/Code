using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CircuitParser
{
    class LogicGate : CircuitComponent
    {

        #region CONSTRUCTOR

        /// <summary>
        /// Constructs a new, unconnected logic gate 
        /// associated with a given shape.
        /// </summary>
        /// <param name="shape">The shape associated
        /// with the new logic gate</param>
        public LogicGate(Sketch.Shape shape) : base(shape)
        {
            // Do nothing!
        }

        #endregion

        #region DETERMINE INTPUT OR OUTPUT

        /// <summary>
        /// Determines whether a given endpoint of a wire would connect
        /// to the invoking logic gate as an input or output. 
        /// 
        /// Precondition: the underlying shape has a valid Orientation
        /// </summary>
        /// <param name="wireEnd">The wire endpoint to analyze</param>
        /// <returns>True if the endpoint would be an input, 
        /// false if it would be an output</returns>
        public bool ShouldBeInput(Sketch.EndPoint wireEnd)
        {
            // Get the x-coordinate the logic gate would have if it were
            // rotated about the origin until it was perfectly horizontal,
            // pointing left to right.
            double adjGateCenterX =
                Shape.Centroid.X * Math.Cos(Shape.Orientation) +
                Shape.Centroid.Y * Math.Sin(Shape.Orientation);

            // Get the x-coordinate of the wire endpoint if it underwent
            // the same transformation.
            double adjWireEndX =
                wireEnd.X * Math.Cos(Shape.Orientation) +
                wireEnd.Y * Math.Sin(Shape.Orientation);

            // Determine whether the wire endpoint is to the left or right
            // of the gate's center, which likewise corresponds to whether
            // it's an input or output.
            return (adjGateCenterX > adjWireEndX);
        }

        #endregion
    }
}
