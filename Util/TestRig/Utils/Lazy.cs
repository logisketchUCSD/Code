using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestRig.Utils
{

    delegate T LazyDelegate<T>();

    /// <summary>
    /// Manages and caches a value which is not computed until asked for.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class Lazy<T>
    {

        private LazyDelegate<T> _computer;
        private T _value;
        private bool _hasValue;

        public Lazy(LazyDelegate<T> del)
        {
            _computer = del;
            _hasValue = false;
        }

        public T Value
        {
            get
            {
                if (_hasValue)
                    return _value;
                _value = _computer();
                _hasValue = true;
                return _value;
            }
        }

    }

}
