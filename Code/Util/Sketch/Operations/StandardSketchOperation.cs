using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sketch.Operations
{
    /// <summary>
    /// A standard sketch operation encapsulates most of the saving that needs to be performed so
    /// subclasses don't need to write their own undo functionality. Subclasses should implement
    /// performActual() instead of perform().
    /// </summary>
    public abstract class StandardSketchOperation : ISketchOperation
    {

        private Sketch _sketch;
        private SavedSketchConnections _connections;
        private SavedSketchGeometry _geometry;
        private SavedSketchTypes _types;

        /// <summary>
        /// Initialize a standard sketch operation with the given sketch
        /// </summary>
        /// <param name="sketch"></param>
        public StandardSketchOperation(Sketch sketch)
        {
            _sketch = sketch;
        }

        /// <summary>
        /// Determine whether this operation modifies sketch geometry. Override to change its value.
        /// </summary>
        /// <returns></returns>
        protected virtual bool modifiesGeometry()
        {
            return true;
        }

        /// <summary>
        /// Determine whether this operation modifies sketch connections. Override to change its value.
        /// </summary>
        /// <returns></returns>
        protected virtual bool modifiesConnections()
        {
            return true;
        }

        /// <summary>
        /// Determine whether this operation modifies shape/substroke types. Override to change its value.
        /// </summary>
        /// <returns></returns>
        protected virtual bool modifiesTypes()
        {
            return true;
        }

        /// <summary>
        /// Saves as much information as necessary (determined using the modifies___() functions)
        /// and then calls performActual().
        /// </summary>
        public virtual void perform()
        {
            if (modifiesConnections())
                _connections = new SavedSketchConnections(_sketch);
            if (modifiesGeometry())
                _geometry = new SavedSketchGeometry(_sketch);
            if (modifiesTypes())
                _types = new SavedSketchTypes(_sketch);
            performActual();
        }

        /// <summary>
        /// Subclasses should override this to actually perform their changes.
        /// </summary>
        protected abstract void performActual();

        /// <summary>
        /// Undo this action. Restores all the data it saved.
        /// </summary>
        public virtual void undo()
        {
            if (modifiesGeometry())
                _geometry.restore();
            if (modifiesConnections())
                _connections.restore();
            if (modifiesTypes())
                _types.restore();
        }

        /// <summary>
        /// Get the sketch associated with this operation.
        /// </summary>
        public Sketch MySketch
        {
            get { return _sketch; }
        }

    }
}
