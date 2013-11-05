using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Utilities.Concurrency
{

    public delegate T FutureDelegate<T>();

    delegate void ComputeDelegate();

    public abstract class Future
    {
        /// <summary>
        /// Wait for all the futures in a collection to finish.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="futures"></param>
        public static void WaitForAll<T>(IEnumerable<Future<T>> futures)
        {
            foreach (var future in futures)
                future.WaitForValue();
        }
    }

    /// <summary>
    /// A Future represents the result from an asynchronous operation.
    /// 
    /// Example usage:
    /// 
    /// Future<int> f = new Future<int>(delegate() { return 1 + 2; });
    /// ... do some stuff while 1+2 is computed asynchronously ...
    /// int result = f.Value; // blocks until 1+2 finishes computing
    /// </summary>
    public class Future<T>
    {

        /// <summary>
        /// The delegate which performs the computation.
        /// </summary>
        private FutureDelegate<T> _computer;

        /// <summary>
        /// The computed value.
        /// </summary>
        private T _value;

        /// <summary>
        /// The exception that was thrown during computation, or null
        /// if no exception was thrown.
        /// </summary>
        private Exception _exception;

        /// <summary>
        /// This semaphore will be locked when the Future is constructed and will
        /// only become unlocked once run() completes (when _value is computed).
        /// </summary>
        private Semaphore _hasValue;

        /// <summary>
        /// Construct a new future. This constructor immediately spawns a new thread
        /// to compute the value of this future. If any exceptions are thrown, they
        /// are caught and thrown again when the result is retreived. 
        /// </summary>
        /// <param name="del">the delegate that computes the value of this future</param>
        public Future(FutureDelegate<T> del)
        {
            _computer = del;
            _hasValue = new Semaphore(0, 1); // locked semaphore
            _exception = null;
            ThreadPool.QueueUserWorkItem(new WaitCallback(run));
        }

        /// <summary>
        /// Construct a future whose value is already known. This constructor
        /// does not create a background process, and the future is effectively
        /// just a wrapper for the value.
        /// </summary>
        /// <param name="value">the value</param>
        public Future(T value)
        {
            _value = value;
            _hasValue = new Semaphore(1, 1); // unlocked semaphore
            _exception = null;
        }

        /// <summary>
        /// Usually called asynchronously. Calls the delegate to compute the
        /// future's value. Upon completion, unlocks _hasValue.
        /// </summary>
        /// <param name="stateInfo">the state info</param>
        private void run(Object stateInfo)
        {
            try
            {
                _value = _computer();
            }
            catch (Exception e)
            {
                _exception = e;
            }
            _hasValue.Release();
        }

        /// <summary>
        /// Wait for the value to be computed.
        /// </summary>
        public void WaitForValue()
        {
            // Lock and unlock the semaphore. This operation will block if _hasValue
            // is already locked (that is to say, if we are still computing the value).
            _hasValue.WaitOne();
            _hasValue.Release();
        }

        /// <summary>
        /// Get the value of this future. If the thread has not finished computing, this
        /// property will block until the value is computed. If the computation thread
        /// threw any exceptions during its execution, they are thrown again here.
        /// </summary>
        public T Value
        {
            get 
            {
                WaitForValue();

                // Throw the exception if one was generated.
                if (_exception != null)
                    throw _exception;

                // Return the computed value.
                return _value; 
            }
        }

    }
}
