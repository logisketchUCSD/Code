using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RecognitionInterfaces
{

    /// <summary>
    /// The Recognizer is responsible for identifying shapes after the grouper has run.
    /// Some recognizers can learn from user interaction as well, as the user identifies
    /// errors.
    /// </summary>
    [Serializable]
    public abstract class Recognizer : IRecognitionStep
    {

        public string ProgressString
        {
            get { return "Recognizing"; }
        }

        /// <summary>
        /// Run the recognizer on a sketch.
        /// 
        /// Precondition: the sketch has been grouped into shapes.
        /// 
        /// Postcondition: shape.Type and shape.Probability have been filled in with 
        /// this recognizer's best guess for every shape in the sketch.
        /// </summary>
        /// <param name="featureSketch">the sketch to work on</param>
        public void process(Featurefy.FeatureSketch featureSketch)
        {
            foreach (Sketch.Shape shape in featureSketch.Sketch.Shapes)
            {
                if (shape.AlreadyLabeled || !canRecognize(shape.Classification))
                    continue;
                processShape(shape, featureSketch);
            }
        }

        /// <summary>
        /// Recognizes the shape and then updates the shape's type, probability, and orientation. Subclasses
        /// may set additional properties of the shape as well.
        /// </summary>
        /// <param name="shape">The Shape to recognize</param>
        /// <param name="featureSketch">The FeatureSketch to use</param>
        public void processShape(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            RecognitionResult result = recognize(shape, featureSketch);
            result.ApplyToShape(shape);
        }

        /// <summary>
        /// Recognize a shape.
        /// 
        /// Precondition: all of the substrokes in the given shape are in the given featureSketch
        /// 
        /// Note: this method does not modify the given shape. If you are looking for a side-effecting function, try processShape().
        /// </summary>
        /// <param name="shape">The Shape to recognize</param>
        /// <param name="featureSketch">The FeatureSketch to use</param>
        /// <returns>the recognition result, which includes at a minimum a shape type, a confidence, and an orientation</returns>
        public abstract RecognitionResult recognize(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch);

        /// <summary>
        /// Update the recognizer to learn from an example shape.
        /// 
        /// Precondition: shape has a valid type
        /// </summary>
        /// <param name="shape">the shape to learn</param>
        public virtual void learnFromExample(Sketch.Shape shape) {  }

        /// <summary>
        /// Reset the recognizer.
        /// </summary>
        public virtual void reset() { }

        /// <summary>
        /// If the recognizer learns from user input, it can override this method
        /// to save itself when the program quits so the training is remembered from
        /// one session to the next. Otherwise, this function does nothing.
        /// </summary>
        public virtual void save() { }

        /// <summary>
        /// Determine whether this recognizer can recognize the given class of shapes.
        /// 
        /// In the default implementation this returns true for everything.
        /// </summary>
        /// <param name="classification">the shape class to check</param>
        /// <returns>true if this recognizer can recognize this class of shapes</returns>
        public virtual bool canRecognize(string classification) 
        {
            return true;
        }

    }

}
