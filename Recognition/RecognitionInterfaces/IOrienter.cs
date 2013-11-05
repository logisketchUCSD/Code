using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RecognitionInterfaces
{

    /// <summary>
    /// Defines the interface for objects that can orient shapes.
    /// </summary>
    public interface IOrienter
    {

        /// <summary>
        /// Orient a single shape.
        /// 
        /// Precondition: shape.Type is a valid ShapeType
        /// 
        /// Postcondition: shape.Orientation is a valid radian number
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="featureSketch"></param>
        void orient(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch);

    }
}
