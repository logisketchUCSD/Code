using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sketch.Operations
{
    /// <summary>
    /// Perform several sketch operations at once.
    /// </summary>
    public class CompoundSketchOperation : ISketchOperation
    {

        private List<ISketchOperation> _ops;

        /// <summary>
        /// Initialize a compound sketch operation with the given sketch operations
        /// </summary>
        /// <param name="ops"></param>
        public CompoundSketchOperation(params ISketchOperation[] ops)
        {
            _ops = new List<ISketchOperation>(ops);
        }

        /// <summary>
        /// Initialize a compound sketch operation with the given sketch operations
        /// </summary>
        /// <param name="ops"></param>
        public CompoundSketchOperation(IEnumerable<ISketchOperation> ops)
        {
            _ops = new List<ISketchOperation>(ops);
        }

        /// <summary>
        /// Perform all the sub-operations.
        /// </summary>
        public virtual void perform()
        {
            foreach (var op in _ops)
                op.perform();
        }

        /// <summary>
        /// Undo the sub-operations.
        /// </summary>
        public virtual void undo()
        {
            // we want to undo the operations in reverse order of how they were performed
            _ops.Reverse();
            foreach (var op in _ops)
                op.undo();
            _ops.Reverse();
        }

    }
}
