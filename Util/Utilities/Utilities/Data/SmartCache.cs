using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Data
{

    /// <summary>
    /// This class automatically manages the caching of values. Values are computed lazily
    /// when asked for and cached. If the keys implement IMutable, the SmartCache will 
    /// uncache values when the key changes. They will be recomputed when asked for again. 
    /// A WeakDictionary is used as the underlying data structure, so keys can be 
    /// garbage-collected. 
    /// 
    /// WARNING: 
    /// A SmartCache may present a dictionary-like interface, but it is NOT a dictionary. Items are 
    /// keyed by reference, not by hash code or .Equals(), so two objects which are otherwise 
    /// equivalent may have different values.
    /// 
    /// Notes:
    /// Determining whether keys implement IMutable is performed on a per-key basis. So for instance,
    /// let's say you have a Point class and a MutablePoint class that inherits from Point and 
    /// implements IMutable. If you have a SmartCache from Points to ints and you ask for the value 
    /// for a given MutablePoint, the SmartCache WILL listen for changes on the mutable point, even
    /// though ordinary Points do not implement IMutable.
    /// </summary>
    public class SmartCache<K, V>
        where K : class
    {

        // This class is painfully ugly, but it presents a very pretty public interface. As much
        // documentation as possible has been provided for future readers.

        /// <summary>
        /// An immutable reference to an arbitrary value. This class is needed because both keys and 
        /// values in the WeakDictionary must be reference types. It makes sense to enforce this 
        /// restriction for keys in the SmartCache, but not for values.
        /// </summary>
        /// <typeparam name="T">the type of stored value</typeparam>
        private class Ref<T>
        {
            private T _value;
            public Ref(T value) { _value = value; }
            public T Value { get { return _value; } }
        }

        /// <summary>
        /// The actual underlying structure for caching data.
        /// </summary>
        private WeakDictionary<K, Ref<V>> _dic;

        /// <summary>
        /// The function from Ks to Vs which is used to compute values.
        /// </summary>
        private Func<K, V> _compute;

        /// <summary>
        /// Records how many times we returned cached values immediately.
        /// </summary>
        private int _cacheHits;
        
        /// <summary>
        /// Records how many times we had to recompute values.
        /// </summary>
        private int _cacheMisses;

        /// <summary>
        /// Construct a new smart cache that knows how to compute its values.
        /// </summary>
        /// <param name="compute">the function mapping keys to values</param>
        public SmartCache(Func<K, V> compute)
        {
            _compute = compute;
            _dic = new WeakDictionary<K, Ref<V>>();
            _cacheHits = 0;
            _cacheMisses = 0;
        }

        /// <summary>
        /// Try watching for changes on a key. If the key does not implement IMutable,
        /// this method does nothing.
        /// </summary>
        /// <param name="key"></param>
        private void tryWatchForChanges(K key)
        {
            // Horrible horrible runtime type-checking is horrible. However, it
            // allows us great capability.
            if (key is IMutable)
                ((IMutable)key).ObjectChanged += elementChanged;
        }

        /// <summary>
        /// Event handler for when mutable objects change; just remove their
        /// cached values. Subclasses may override this if they have smarter
        /// behavior for this method. (I.e., if objects send data about what
        /// changed, some values may not need to be recomputed from scratch.)
        /// </summary>
        /// <param name="element"></param>
        protected virtual void elementChanged(object element, EventArgs args)
        {
            // This check may not be necessary. Since we only listen to changes
            // on objects of type K, we should be OK. A little safety never
            // hurt anyone though.
            if (element is K)
                _dic.Remove((K)element);
        }

        /// <summary>
        /// Remove a key from this cache. When the key is asked for, its value
        /// will be recomputed.
        /// </summary>
        /// <param name="key"></param>
        public virtual void Remove(K key)
        {
            // Stop listening for changes for this key.
            if (key is IMutable)
                ((IMutable)key).ObjectChanged -= elementChanged;

            // Remove it.
            _dic.Remove(key);
        }

        /// <summary>
        /// Compute the values for all the given keys immediately. This
        /// does not recompute already-cached values.
        /// </summary>
        /// <param name="keys"></param>
        public virtual void ComputeAll(IEnumerable<K> keys)
        {
            V value;
            foreach (K key in keys)
                value = this[key]; // asking for it forces computation
        }

        /// <summary>
        /// Remove all stored values from this cache.
        /// </summary>
        public virtual void Clear()
        {
            // WeakDictionaries cannot store all of their keys (or they wouldn't be weak!)
            // so we cannot iteratively remove all of them. Just creating a new weak
            // dictionary is therefore our only option (and it is much faster, too).
            _dic = new WeakDictionary<K, Ref<V>>();
        }

        /// <summary>
        /// Get or set the value for a given key. If the value has not been computed
        /// yet, it is computed when asked for.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException">thrown if the key is null</exception>
        /// <returns></returns>
        public virtual V this[K key]
        {
            get
            {
                // Make sure the key isn't null
                if (key == null)
                    throw new ArgumentNullException("SmartCache keys cannot be null");

                // If we have a cached value, return it
                Ref<V> value;
                if (_dic.TryGetValue(key, out value))
                {
                    _cacheHits++;
                    return value.Value;
                }

                // Compute the value and store it
                value = new Ref<V>(_compute(key));
                _dic[key] = value;
                _cacheMisses++;
                
                // Watch for changes on the key if it is mutable
                tryWatchForChanges(key);
                return value.Value;
            }
            set
            {
                // Make sure the key isn't null
                if (key == null)
                    throw new ArgumentNullException("SmartCache keys cannot be null");

                if (_dic.ContainsKey(key))
                {
                    // If the key is already in the dictionary, change its value.
                    _dic[key] = new Ref<V>(value);
                }
                else
                {
                    // If the key is not in the dictionary, watch for changes and 
                    // then add it.
                    tryWatchForChanges(key);
                    _dic.Add(key, new Ref<V>(value));
                }
            }
        }
        
        /// <summary>
        /// Determine how many times this cache had to recompute a value. Useful for
        /// determining whether your cache is actually saving you any time.
        /// </summary>
        public int CacheMisses
        {
            get { return _cacheMisses; }
        }

        /// <summary>
        /// Determine how many times this cache returned cached values. Useful for
        /// determining whether your cache is actually saving you any time.
        /// </summary>
        public int CacheHits
        {
            get { return _cacheHits; }
        }

    }

}
