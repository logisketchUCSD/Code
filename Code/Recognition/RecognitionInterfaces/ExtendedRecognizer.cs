using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Domain;
using Sketch;

namespace RecognitionInterfaces
{

    /// <summary>
    /// An ExtendedRecognizer is a recognizer that implements some additional functionality.
    /// Specifically, an ExtendedRecognizer can attempt to treat a shape as the given type
    /// and yield recognition results for that type.
    /// </summary>
    public abstract class ExtendedRecognizer : Recognizer
    {

        /// <summary>
        /// Recognize a shape as if it had the given types.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public abstract Dictionary<ShapeType, RecognitionResult> RecognitionResults(Shape shape, IEnumerable<ShapeType> types);

    }

}
