using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;

namespace Sketch.Operations
{
    /// <summary>
    /// Saves the types and classifications of all sketch substrokes and shapes.
    /// Also saves shape orientations and confidences.
    /// </summary>
    class SavedSketchTypes
    {
        
        private Sketch _sketch;
        private Dictionary<Shape, ShapeType> _shapeTypes;
        private Dictionary<Shape, float> _shapeConfidences;
        private Dictionary<Shape, double> _shapeOrientations;
        private Dictionary<Substroke, string> _substrokeClasses;

        public SavedSketchTypes(Sketch sketch)
        {
            _sketch = sketch;
            _shapeTypes = new Dictionary<Shape, ShapeType>();
            _shapeConfidences = new Dictionary<Shape, float>();
            _shapeOrientations = new Dictionary<Shape, double>();
            _substrokeClasses = new Dictionary<Substroke, string>();

            foreach (Shape shape in _sketch.Shapes)
            {
                _shapeTypes.Add(shape, shape.Type);
                _shapeConfidences.Add(shape, shape.Probability);
                _shapeOrientations.Add(shape, shape.Orientation);
            }
            foreach (Substroke substroke in _sketch.Substrokes)
                _substrokeClasses.Add(substroke, substroke.Classification);
        }

        public void restore()
        {
            foreach (var pair in _shapeTypes)
            {
                Shape s = pair.Key;
                s.Type = pair.Value;
                s.Probability = _shapeConfidences[s];
                s.Orientation = _shapeOrientations[s];
            }
            foreach (var pair in _substrokeClasses)
                pair.Key.Classification = pair.Value;
        }


    }
}
