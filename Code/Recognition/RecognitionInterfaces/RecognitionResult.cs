using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;
using Sketch;

namespace RecognitionInterfaces
{

    /// <summary>
    /// A RecognitionResult represents the result of recognizing a shape. It holds a new type, 
    /// a confidence measurement, and an orientation. It can be applied to a shape using the
    /// ApplyToShape method.
    /// 
    /// <remarks>
    /// You should use the ApplyToShape method whenever you want to apply this recognition result
    /// to a shape, since subclasses may want to set additional properties in addition to type,
    /// confidence, and orientation.
    /// </remarks>
    /// </summary>
    public class RecognitionResult
    {

        private readonly ShapeType _newType;
        private readonly double _confidence;
        private readonly double _orientation;

        /// <summary>
        /// Construct a new recognition result with the given type, confidence, and orientation
        /// </summary>
        /// <param name="type"></param>
        /// <param name="confidence"></param>
        /// <param name="orientation"></param>
        public RecognitionResult(ShapeType type, double confidence, double orientation)
        {
            _newType = type;
            _confidence = confidence;
            _orientation = orientation;
        }

        /// <summary>
        /// Apply this recognition to a shape.
        /// </summary>
        /// <param name="s"></param>
        public virtual void ApplyToShape(Shape s)
        {
            s.Type = _newType;
            s.Probability = (float)_confidence;
            s.Orientation = _orientation;
        }

        /// <summary>
        /// Get the new type.
        /// </summary>
        public ShapeType Type
        {
            get { return _newType; }
        }

        /// <summary>
        /// Get the new confidence.
        /// </summary>
        public double Confidence
        {
            get { return _confidence; }
        }

        /// <summary>
        /// Get the new orientation (in the range [0, 2Pi]).
        /// </summary>
        public double Orientation
        {
            get { return _orientation; }
        }

    }

}
