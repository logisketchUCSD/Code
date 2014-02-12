using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sketch;

namespace CircuitParser
{
    public class ParseError
    {
        string _errorExplanation;
        string _userExplanation;
        Sketch.Shape _shape;

        /// <summary>
        /// The parse error class has information about an error that occurs in 
        /// circuitParser. It has the message as well as the shape so that the 
        /// error shape can be highlighted later
        /// </summary>
        /// <param name="errorExplanation"></param>
        /// <param name="shape"></param>
        public ParseError(string errorExplanation, string userExplanation, Sketch.Shape shape)
        {
            _errorExplanation = errorExplanation;
            _userExplanation = userExplanation;
            _shape = shape;
        }
        public ParseError(string errorExplanation, Sketch.Shape shape)
        {
            _errorExplanation = errorExplanation;
            _userExplanation = null;
            _shape = shape;
        }

        public string techReason
        {
            get { return _errorExplanation; }
        }
        public string Explanation
        {
            get{
                if (_userExplanation == null)
                    return _errorExplanation;
                else
                    return _userExplanation; }
        }
        public Sketch.Shape Where
        {
            get { return _shape; }
        }

    }
}
