using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Data;

namespace Sketch.Operations
{
    /// <summary>
    /// Saves all the sketch connections.
    /// </summary>
    class SavedSketchConnections
    {
        private Sketch _sketch;
        private HashSet<Tuple<Shape, Shape>> _shapeConnections;
        private List<Tuple<EndPoint, Shape>> _endpointConnections;
        public SavedSketchConnections(Sketch sketch)
        {
            _sketch = sketch;
            _shapeConnections = new HashSet<Tuple<Shape, Shape>>();
            _endpointConnections = new List<Tuple<EndPoint, Shape>>();

            foreach (Shape shape in _sketch.Shapes)
            {
                foreach (Shape connected in shape.ConnectedShapes)
                    _shapeConnections.Add(Tuple.Create(shape, connected));
                foreach (EndPoint endpoint in shape.Endpoints)
                    _endpointConnections.Add(Tuple.Create(endpoint, endpoint.ConnectedShape));
            }
        }
        public void restore()
        {
            foreach (Shape shape in _sketch.Shapes)
                shape.ClearConnections();

            foreach (var pair in _shapeConnections)
                _sketch.connectShapes(pair.Item1, pair.Item2);
            foreach (var pair in _endpointConnections)
                pair.Item1.ConnectedShape = pair.Item2;
        }
    }
}
