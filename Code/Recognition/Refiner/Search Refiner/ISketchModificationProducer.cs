using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Refiner
{
    public interface ISketchModificationProducer
    {

        /// <summary>
        /// Always called at least once before any calls to SketchModifications(...).
        /// Subsequent calls to SketchModifications will always use the same FeatureSketch.
        /// </summary>
        /// <param name="featureSketch"></param>
        void Start(Featurefy.FeatureSketch featureSketch);

        /// <summary>
        /// Get a list of valid modifications for the given sketch. If any of the modifications
        /// are executed, no guarantee is provided that the remaining ones will work. This
        /// method must be called again to obtain another list.
        /// </summary>
        /// <param name="sketch">the sketch to check</param>
        /// <returns>a list of possible modifications</returns>
        List<SketchModification> SketchModifications(Featurefy.FeatureSketch featureSketch);

    }
}
