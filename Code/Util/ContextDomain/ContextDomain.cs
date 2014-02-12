/*
 * ContextDomain.cs
 * 
 * Created by Denis Aleshin and Joshua Ehrlich 
 * June 30, 2009
 * 
 * Edited by Sketchers 2010
 * July 19, 2010
 *  
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ContextDomain
{
    /// <summary>
    /// This abstract class generalizes context for different domains.
    /// </summary>
    public abstract class ContextDomain
    {

        /// <summary>
        /// Updates the connections of the given shape.
        /// 
        /// Precondition: the given shape must be in the given sketch
        /// </summary>
        /// <param name="shape1">The shape to connect.</param>
        /// <param name="sketch">The sketch to provide context for the shape.</param>
        public abstract void ConnectShape(Sketch.Shape shape, Sketch.Sketch sketch);

        /// <summary>
        /// Determine the orientation of the given shape.
        /// </summary>
        /// <param name="shape1">The shape to orient.</param>
        /// <param name="sketch">The sketch to provide context for the shape.</param>
        /// <returns>a radian value in the range [0, 2Pi]</returns>
        public abstract double OrientShape(Sketch.Shape shape, Sketch.Sketch sketch);

        /// <summary>
        /// Determine whether the shape is properly connected (given its context). This function uses
        /// shape.ConnectedShapes to determine the shape's neighbors.
        /// </summary>
        /// <param name="shape">The shape we are testing for validity.</param>
        /// <returns>True if the shape is properly connected, or false otherwise.</returns>
        public abstract bool IsProperlyConnected(Sketch.Shape shape);

    }

}
    