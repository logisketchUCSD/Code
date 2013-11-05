using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace Utilities.Concurrency
{
    public delegate T ProducerFunction<S,T>(S source);

    /// <summary>
    /// An AsyncProducer runs a given function on an enumerable set of source objects
    /// on a background thread. It keeps a fixed-size buffer storing results waiting
    /// to be consumed. It is useful if you need to produce a large number of objects
    /// asynchronously, but you will only be iterating over them in order.
    /// 
    /// For further reading:
    /// http://en.wikipedia.org/wiki/Producer-consumer_problem
    /// 
    /// Sample usage (converts numbers to strings in the background):
    /// 
    /// int[] numbers = new int[] { 1, 2, 3, 4 };
    /// AsyncProducer<int,string> converter = new AyncProducer<int, string>(
    ///     numbers,
    ///     delegate(int i) { return i.ToString(); });
    /// foreach (int n in numbers)
    /// {
    ///     string result = converter.Consume;
    ///     Console.WriteLine(result);
    /// }
    /// </summary>
    /// <typeparam name="S">the type of the source objects</typeparam>
    /// <typeparam name="T">the type of objects produced</typeparam>
    public class AsyncProducer<S,T>
    {

        private const int DEFAULT_BUFFER_SIZE = 10;

        private T[] _data;
        private int _head, _tail;

        private Semaphore _full;
        private Semaphore _empty;

        private IEnumerable<S> _sources;
        private ProducerFunction<S, T> _func;

        /// <summary>
        /// Constructs an asynchronous producer with the default buffer size.
        /// </summary>
        /// <param name="sources">the source objects</param>
        /// <param name="func">the function mapping source objects to results</param>
        public AsyncProducer(IEnumerable<S> sources, ProducerFunction<S,T> func)
            : this(sources, func, DEFAULT_BUFFER_SIZE)
        {
            // calls the main constructor
        }

        /// <summary>
        /// Constructs an asynchronous producer with the given buffer size.
        /// </summary>
        /// <param name="sources">the source objects</param>
        /// <param name="func">the function mapping source objects to results</param>
        /// <param name="capacity">the size of the buffer</param>
        public AsyncProducer(IEnumerable<S> sources, ProducerFunction<S,T> func, int capacity)
        {
            _data = new T[capacity];
            _head = _tail = 0; // insert at tail, remove from head

            _full  = new Semaphore(0, capacity);        // 0 out of capacity full slots
            _empty = new Semaphore(capacity, capacity); // capacity out of capacity empty slots

            _sources = sources;
            _func = func;

            // start async thread
            ThreadPool.QueueUserWorkItem(new WaitCallback(run));
        }

        /// <summary>
        /// Called asynchronously. Performs the actual producing, in order.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void run(Object stateInfo)
        {
            foreach (S source in _sources)
            {
                T value = _func(source);  // produce value
                _empty.WaitOne();         // wait for an empty slot
                _data[_tail] = value;     // insert value
                _tail++;                  // increment tail
                _tail %= _data.Length;    // wrap around if necessary
                _full.Release();          // mark slot as full
            }
        }

        /// <summary>
        /// Get the next value out of this producer. Call this repeatedly
        /// to obtain all the values. You should only call this once for
        /// every object in the list of sources.
        /// </summary>
        /// <returns></returns>
        public T Consume()
        {
            T value;
            _full.WaitOne();         // wait for filled slot
            value = _data[_head];    // get value
            _head++;                 // increment head
            _head %= _data.Length;   // wrap around if necessary
            _empty.Release();        // mark slot as empty
            return value;
        }

    }
}
