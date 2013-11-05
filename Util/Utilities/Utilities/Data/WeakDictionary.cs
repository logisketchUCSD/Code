using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Data
{


    /// <summary>
    /// A WeakDictionary is a dictionary where keys may be garbage-collected. It is a wrapper
    /// around ConditionalWeakTable (http://msdn.microsoft.com/en-us/library/dd287757.aspx)
    /// that supports more functionality.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class WeakDictionary<K, V>
        where K : class
        where V : class
    {

        private ConditionalWeakTable<K, V> _table;

        /// <summary>
        /// Construct an empty weak dictionary.
        /// </summary>
        public WeakDictionary()
        {
            _table = new ConditionalWeakTable<K, V>();
        }

        /// <summary>
        /// Add an entry to this weak dictionary.
        /// </summary>
        /// <param name="pair"></param>
        public void Add(KeyValuePair<K, V> pair)
        {
            Add(pair.Key, pair.Value);
        }

        /// <summary>
        /// Add an entry to this weak dictionary.
        /// </summary>
        /// <param name="pair"></param>
        public void Add(K key, V value)
        {
            _table.Add(key, value);
        }

        /// <summary>
        /// Try to get a value from the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(K key, out V value)
        {
            return _table.TryGetValue(key, out value);
        }

        /// <summary>
        /// Check to see if a value is stored for the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(K key)
        {
            V value;
            return TryGetValue(key, out value);
        }

        /// <summary>
        /// Remove the given key from the weak dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>true if the key was removed, or false otherwise</returns>
        public bool Remove(K key)
        {
            return _table.Remove(key);
        }

        /// <summary>
        /// Get or set a value from a key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public V this[K key]
        {
            get
            {
                V value;
                if (TryGetValue(key, out value))
                    return value;
                throw new KeyNotFoundException();
            }
            set
            {
                Remove(key);
                Add(key, value);
            }
        }

    }

}
