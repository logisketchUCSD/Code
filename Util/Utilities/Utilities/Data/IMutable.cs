using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Data
{

    /// <summary>
    /// Defines an interface for mutable objects, allowing others
    /// to listen for changes.
    /// </summary>
    public interface IMutable
    {

        event EventHandler ObjectChanged;

    }

}
