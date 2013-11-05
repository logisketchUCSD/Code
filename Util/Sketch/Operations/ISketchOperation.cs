using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sketch.Operations
{

    /// <summary>
    /// A sketch operation describes an action on a sketch which can be undone.
    /// </summary>
    public interface ISketchOperation
    {

        /// <summary>
        /// Perform the operation.
        /// </summary>
        void perform();

        /// <summary>
        /// Undo the operation.
        /// 
        /// Precondition: perform() was called already.
        /// </summary>
        void undo();

    }

}
