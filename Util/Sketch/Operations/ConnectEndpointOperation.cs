using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sketch.Operations
{

    /// <summary>
    /// A modification that connects an endpoint to another shape.
    /// </summary>
    public class ConnectEndpointOperation : StandardSketchOperation
    {

        private EndPoint _endpoint;
        private Shape _toConnect;

        /// <summary>
        /// Initialize a modification to connect "endpoint" to "destination."
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="endpoint"></param>
        /// <param name="destination"></param>
        public ConnectEndpointOperation(Sketch sketch, EndPoint endpoint, Shape destination)
            :base(sketch)
        {
            _endpoint = endpoint;
            _toConnect = destination;
        }

        /// <summary>
        /// Does not modify geometry.
        /// </summary>
        /// <returns></returns>
        protected override bool modifiesGeometry()
        {
            return false;
        }

        /// <summary>
        /// Does not modify types.
        /// </summary>
        /// <returns></returns>
        protected override bool modifiesTypes()
        {
            return false;
        }

        /// <summary>
        /// Connects the shapes and the endpoint's connected shape.
        /// </summary>
        protected override void performActual()
        {
            MySketch.connectShapes(_endpoint.ParentShape, _toConnect);
            _endpoint.ConnectedShape = _toConnect;
        }

    }
}
