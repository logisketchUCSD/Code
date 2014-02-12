using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;

namespace Sketch.Operations
{
    /// <summary>
    /// A sketch operation that transfers a substroke from one shape to another.
    /// </summary>
    public class StrokeStealOperation : StandardSketchOperation
    {

        private Shape _thief;
        private Substroke _gem;

        /// <summary>
        /// Initialize a new stroke steal operation.
        /// </summary>
        /// <param name="sketch">the relevant sketch</param>
        /// <param name="thief">the shape stealing a stroke</param>
        /// <param name="gem">the stroke it is stealing</param>
        public StrokeStealOperation(
            Sketch sketch, 
            Shape thief, 
            Substroke gem)
            :base(sketch)
        {
            _thief = thief;
            _gem = gem;
        }

        /// <summary>
        /// The stroke steal does not modify shape types.
        /// </summary>
        /// <returns></returns>
        protected override bool modifiesTypes()
        {
            return false;
        }

        /// <summary>
        /// Make the substroke trade.
        /// </summary>
        protected override void performActual()
        {
            foreach (EndPoint endpoint in _gem.Endpoints)
                endpoint.ConnectedShape = null;
            Shape victim = _gem.ParentShape;
            if (victim != null)
                victim.RemoveSubstroke(_gem);
            _thief.AddSubstroke(_gem);
            if (victim != null && victim.Substrokes.Length == 0)
            {
                MySketch.RemoveShape(victim);
            }
            MySketch.CheckConsistency();
        }

    }
}
