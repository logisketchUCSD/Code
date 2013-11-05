using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sketch.Operations
{
    /// <summary>
    /// An operation that removes a substroke from a shape.
    /// </summary>
    public class RemoveSubstrokeFromShape : StandardSketchOperation
    {

        private Shape _shape;
        private Substroke _substroke;

        /// <summary>
        /// Initialize a substroke removal operation.
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public RemoveSubstrokeFromShape(Sketch sketch, Shape parent, Substroke child)
            :base(sketch)
        {
            _shape = parent;
            _substroke = child;
        }

        /// <summary>
        /// This does not modify shape type information.
        /// </summary>
        /// <returns></returns>
        protected override bool modifiesTypes()
        {
            return false;
        }

        /// <summary>
        /// Remove the substroke and delete the parent shape if it is empty.
        /// </summary>
        protected override void performActual()
        {
            _shape.RemoveSubstroke(_substroke);
            if (_shape.IsEmpty)
                MySketch.RemoveShape(_shape);
        }

    }
}
