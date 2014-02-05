using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RecognitionInterfaces
{

    /// <summary>
    /// The Grouper is responsible for grouping strokes into shapes.
    /// </summary>
    public abstract class Grouper : IRecognitionStep
    {

        public string ProgressString
        {
            get { return "Grouping"; }
        }

        /// <summary>
        /// Clears all the groups in the sketch, then calls group(sketch).
        /// </summary>
        /// <param name="sketch">the sketch to use</param>
        public virtual void process(Featurefy.FeatureSketch sketch)
        {
            sketch.Sketch.resetShapes();
            group(sketch);
        }

        /// <summary>
        /// Group the strokes in a FeatureSketch into shapes. This method
        /// should not change shapes for which UserSpecifiedGroup is true.
        /// 
        /// Precondition: all the substrokes have assigned classifications, and
        /// every substrokes has a parent shape.
        /// 
        /// Postcondition: the sketch has been grouped into shapes.
        /// </summary>
        /// <param name="sketch">the FeatureSketch to use</param>
        public abstract void group(Featurefy.FeatureSketch sketch);

    }

}
