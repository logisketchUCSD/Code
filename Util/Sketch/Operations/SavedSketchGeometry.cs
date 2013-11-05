using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sketch.Operations
{
    /// <summary>
    /// Saves sketch geometry (which substrokes belong to which shapes)
    /// </summary>
    class SavedSketchGeometry
    {
        private Sketch _sketch;
        private Dictionary<Shape, List<Substroke>> _shapeGeometry;
        public SavedSketchGeometry(Sketch sketch)
        {
            _sketch = sketch;
            _shapeGeometry = new Dictionary<Shape, List<Substroke>>();
            foreach (Shape shape in sketch.Shapes)
                _shapeGeometry.Add(shape, new List<Substroke>(shape.SubstrokesL));
        }
        public void restore()
        {
            _sketch.clearShapes();
            foreach (var pair in _shapeGeometry)
            {
                Shape shape = pair.Key;
                List<Substroke> substrokes = pair.Value;
                shape.AddSubstrokes(substrokes);
                _sketch.AddShape(shape);
            }
        }
    }
}
