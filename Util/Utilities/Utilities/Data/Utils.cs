using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Data
{
    public delegate bool FilterDelegate<T>(T value);
    public delegate T2 MapDelegate<T1,T2>(T1 value);

    /// <summary>
    /// Contains static methods for manipulating lists, dictionaries, etc.
    /// </summary>
    public abstract class Utils
    {

        /// <summary>
        /// Filter the entries of a dictionary by keeping only certain keys.
        /// </summary>
        /// <typeparam name="K">the type of dictionary keys</typeparam>
        /// <typeparam name="V">the type of dictionary values</typeparam>
        /// <param name="dictionary"></param>
        /// <param name="filter">returns true for a given key if that entry is to be included in the result</param>
        /// <returns></returns>
        public static Dictionary<K,V> filterKeys<K,V>(Dictionary<K,V> dictionary, FilterDelegate<K> filter)
        {
            Dictionary<K, V> result = new Dictionary<K, V>();
            foreach (KeyValuePair<K, V> pair in dictionary)
                if (filter(pair.Key))
                    result.Add(pair.Key, pair.Value);
            return result;
        }

        /// <summary>
        /// Filter the entries of a dictionary by keeping only certain values.
        /// </summary>
        /// <typeparam name="K">the type of dictionary keys</typeparam>
        /// <typeparam name="V">the type of dictionary values</typeparam>
        /// <param name="dictionary"></param>
        /// <param name="filter">returns true for a given value if that entry is to be included in the result</param>
        /// <returns></returns>
        public static Dictionary<K, V> filterValues<K, V>(Dictionary<K, V> dictionary, FilterDelegate<V> filter)
        {
            Dictionary<K, V> result = new Dictionary<K, V>();
            foreach (KeyValuePair<K, V> pair in dictionary)
                if (filter(pair.Value))
                    result.Add(pair.Key, pair.Value);
            return result;
        }

        /// <summary>
        /// Filter the entries of a collection by keeping only certain values.
        /// </summary>
        /// <typeparam name="T">the type of objects in the collection</typeparam>
        /// <param name="list"></param>
        /// <param name="filter">returns true for a given value if that entry is to be included in the result</param>
        /// <returns></returns>
        public static List<T> filter<T>(IEnumerable<T> list, FilterDelegate<T> filter)
        {
            List<T> result = new List<T>();
            foreach (T value in list)
                if (filter(value))
                    result.Add(value);
            return result;
        }

        public static Dictionary<K, V2> replaceValues<K, V1, V2>(Dictionary<K, V1> dictionary, MapDelegate<V1, V2> transform)
        {
            Dictionary<K, V2> result = new Dictionary<K, V2>();
            foreach (var pair in dictionary)
                result.Add(pair.Key, transform(pair.Value));
            return result;
        }

        /// <summary>
        /// Create a list contining one element.
        /// </summary>
        /// <typeparam name="T">the type of element</typeparam>
        /// <param name="value">the value to add to the list</param>
        /// <returns>a list containing only the given element</returns>
        public static List<T> singleEntryList<T>(T value)
        {
            List<T> result = new List<T>();
            result.Add(value);
            return result;
        }

        /// <summary>
        /// Search by reference. The default List.Contains() method compares
        /// using .Equals(), but sometimes we need to find an exact reference.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool containsReference<T>(IEnumerable<T> collection, T element)
            where T : class
        {
            foreach (T value in collection)
                if (value == element)
                    return true;
            return false;
        }

        /// <summary>
        /// Determine whether a collection contains duplicate entries.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool containsDuplicates<T>(IEnumerable<T> list)
        {
            HashSet<T> entries = new HashSet<T>();
            foreach (T value in list)
            {
                if (entries.Contains(value))
                    return true;
                else
                    entries.Add(value);
            }
            return false;
        }

        /// <summary>
        /// Small helper method to swap two values.
        /// 
        /// If there's a standard swap method in C#, get rid of this 
        /// method and just use the standard one.
        /// </summary>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        public static void swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        /// <summary>
        /// Given a dictionary from K to V, return a mirror dictionary from
        /// V to K. (That is, if your dictionary maps apples to oranges, the
        /// result will map the same oranges to the same apples.)
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static Dictionary<V, K> flipKeysAndValues<K,V>(Dictionary<K, V> dic)
        {
            Dictionary<V, K> result = new Dictionary<V, K>();
            foreach (var pair in dic)
                result.Add(pair.Value, pair.Key);
            return result;
        }
    }
}