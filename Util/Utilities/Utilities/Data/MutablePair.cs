using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Data
{

    public abstract class MutablePair
    {
        /// <summary>
        /// Create a new pair. It may be easier to write
        ///     MutablePair.Create(v1, v2)
        /// than the overly-verbose
        ///     new MutablePair[TypeOne,TypeTwo](v1, v2)
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <typeparam name="Y"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static MutablePair<X, Y> Create<X, Y>(X x, Y y)
        {
            return new MutablePair<X, Y>(x, y);
        }
    }

    public class MutablePair<U,V> : Tuple<U,V>
    {
        #region Internals
        U item1;
        V item2;
        #endregion

        public MutablePair(U a, V b)
            :base(a, b)
        {
            item1 = a;
            item2 = b;
            
        }

        /// <summary>
        /// Get the first value
        /// </summary>
        public new U Item1
        {
            get { return item1; }
            set { item1 = value; }
        }

        /// <summary>
        /// Get the second value
        /// </summary>
        public new V Item2
        {
            get { return item2; }
            set { item2 = value; }
        }

    }
}
