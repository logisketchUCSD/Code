using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain;

namespace Sketch.Operations
{

    public class RelabelShapeOperation : StandardSketchOperation
    {
        
        private Sketch _sketch;
        private Shape _shape;
        private ShapeType _newType;
        private double _newConfidence, _newOrientation;

        public RelabelShapeOperation(Sketch sketch, Shape shape, ShapeType newType, double newConfidence, double? newOrientation = null)
            :base(sketch)
        {
            _sketch = sketch;
            _shape = shape;
            _newType = newType;
            _newConfidence = newConfidence;
            if (newOrientation == null)
                _newOrientation = shape.Orientation;
            else
                _newOrientation = newOrientation.Value;
        }

        protected override bool modifiesGeometry()
        {
            return false;
        }

        protected override bool modifiesConnections()
        {
            return false;
        }

        protected override void performActual()
        {
            _shape.Type = _newType;
            _shape.Probability = (float)_newConfidence;
            _shape.Orientation = _newOrientation;
        }

    }

}
