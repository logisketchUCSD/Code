using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sketch;
using Sketch.Operations;

namespace Refiner
{

    /// <summary>
    /// The glorious Search Refiner needs to know what modifications are possible in
    /// a given domain. This interface encapsulates that.
    /// </summary>
    public class SketchModification : ISketchOperation
    {

        Sketch.Sketch _sketch;
        ISketchOperation _op;
        double _benefit;

        /// <summary>
        /// Construct a sketch modification. Computes the benefit of this action
        /// by computing the difference in the given energy function from
        /// performing the given sketch operation.
        /// </summary>
        public SketchModification(Featurefy.FeatureSketch sketch, ISketchOperation op, Func<Featurefy.FeatureSketch, double> energyFunc)
        {
            _sketch = sketch.Sketch;
            _op = op;

            double initialEnergy = energyFunc(sketch);
            op.perform();
            double finalEnergy = energyFunc(sketch);
            op.undo();
            _benefit = finalEnergy - initialEnergy;
        }

        /// <summary>
        /// Perform this modification.
        /// </summary>
        /// <param name="sketch"></param>
        public virtual void perform()
        {
            _op.perform();
        }

        /// <summary>
        /// Undo this modification.
        /// </summary>
        /// <param name="sketch"></param>
        public virtual void undo()
        {
            _op.undo();
        }

        /// <summary>
        /// Determine how good it would be to perform this action. Higher
        /// numbers correspond (roughly) to better actions.
        /// </summary>
        /// <returns></returns>
        public double benefit()
        {
            return _benefit;
        }

        public override string ToString()
        {
            return _op.ToString() + ", " + Math.Round(_benefit, 6);
        }

    }

}
