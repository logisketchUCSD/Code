using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RecognitionInterfaces
{

    /// <summary>
    /// The Classifier is responsible for assigning classes to substrokes.
    /// </summary>
    public abstract class Classifier : IRecognitionStep
    {

        public string ProgressString
        {
            get { return "Classifying"; }
        }

        /// <summary>
        /// Classifies all the substrokes in a sketch.
        /// 
        /// Postcondition: substroke.Classification is "Wire," "Gate," "Label," or "Unknown" for
        /// every substroke in the sketch. Strokes are not reclassified if they have a parent
        /// shape with AlreadyLabeled set to true.
        /// </summary>
        /// <param name="sketch">the sketch to use</param>
        public virtual void process(Featurefy.FeatureSketch sketch)
        {
            process(sketch, sketch.Sketch.Substrokes);
        }

        /// <summary>
        /// Classifies only the given substrokes.
        /// 
        /// Postcondition: substroke.Classification is "Wire," "Gate," "Label," or "Unknown" for
        /// every substroke in the list. Strokes are not reclassified if they have a parent
        /// shape with AlreadyLabeled set to true.
        /// </summary>
        /// <param name="sketch">the sketch to use</param>
        /// <param name="featureSketch">the featureSketch to use</param>
        public virtual void process(Featurefy.FeatureSketch featureSketch, IEnumerable<Sketch.Substroke> substrokes)
        {
            foreach (Sketch.Substroke substroke in substrokes)
            {
                Sketch.Shape parent = substroke.ParentShape;
                if (parent != null && parent.AlreadyLabeled)
                    continue;

                substroke.Classification = classify(substroke, featureSketch);
            }
        }

        /// <summary>
        /// Classify a substroke (e.g., as "Wire," "Gate," or "Label"). Does not change the substroke, but
        /// returns the classification.
        /// </summary>
        /// <param name="featureSketch">the FeatureSketch containing information about this substroke</param>
        /// <param name="substroke">the substroke to classify</param>
        /// <returns>the most likely classification</returns>
        public abstract string classify(Sketch.Substroke substroke, Featurefy.FeatureSketch featureSketch);

    }

}
