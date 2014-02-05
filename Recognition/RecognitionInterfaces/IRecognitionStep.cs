using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RecognitionInterfaces
{

    /// <summary>
    /// Represents a step in the recognition process.
    /// </summary>
    public interface IRecognitionStep
    {

        /// <summary>
        /// Process a sketch.
        /// 
        /// Postcondition: all of this stage's work is finished, and its results have been saved in the sketch.
        /// </summary>
        /// <param name="sketch">the sketch to work on</param>
        void process(Featurefy.FeatureSketch sketch);

        /// <summary>
        /// Get a human-readable string describing what this step is doing (e.g., "Recognizing" or "Classifying")
        /// </summary>
        string ProgressString
        {
            get;
        }

    }
}
