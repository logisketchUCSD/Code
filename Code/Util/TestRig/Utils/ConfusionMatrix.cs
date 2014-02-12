using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestRig.Utils
{

    /// <summary>
    /// Represents a confusion matrix.
    /// </summary>
    /// <typeparam name="T">the type of the keys (this[T][T] is an int)</typeparam>
    class ConfusionMatrix<T>
    {

        private HashSet<T> _keys = new HashSet<T>();
        private Dictionary<T, Dictionary<T, int>> _confusion = new Dictionary<T, Dictionary<T, int>>();

        /// <summary>
        /// Ensures that _confusion[key1][key2] exists. Sets it to 0 if it does not.
        /// </summary>
        /// <param name="key1">first key</param>
        /// <param name="key2">second key</param>
        private void ensureKeyExists2D(T key1, T key2)
        {
            _keys.Add(key1);
            _keys.Add(key2);
            if (!_confusion.ContainsKey(key1))
            {
                _confusion.Add(key1, new Dictionary<T, int>());
            }
            if (!_confusion[key1].ContainsKey(key2))
            {
                _confusion[key1].Add(key2, 0);
            }
        }

        /// <summary>
        /// Add one to the given index
        /// </summary>
        /// <param name="correct">the correct answer</param>
        /// <param name="result">the answer given</param>
        public void increment(T correct, T result)
        {
            ensureKeyExists2D(correct, result);
            _confusion[correct][result]++;
        }

        /// <summary>
        /// Add the results from another confusion matrix of the same type
        /// </summary>
        /// <param name="other"></param>
        public void AddResults(ConfusionMatrix<T> other)
        {
            foreach (KeyValuePair<T, Dictionary<T, int>> rowPair in other._confusion)
            {
                T row = rowPair.Key;
                foreach (KeyValuePair<T, int> entry in rowPair.Value)
                {
                    T col = entry.Key;
                    ensureKeyExists2D(row, col);
                    _confusion[row][col] += entry.Value;
                }
            }
        }

        /// <summary>
        /// Write this matrix in CSV format
        /// </summary>
        /// <param name="tw"></param>
        public void writeCSV(System.IO.TextWriter tw)
        {

            // confusion matrix
            List<T> headers = new List<T>(_keys);
            headers.Sort(delegate(T one, T two) { return one.ToString().CompareTo(two.ToString()); });

            tw.Write("Columns are original cassifications and rows are actual classifications. (So matrix[col=x][row=y] is how many x's were misclassified as y's.)");
            foreach (T key2 in headers)
            {
                tw.Write("," + key2);
            }
            tw.WriteLine();

            foreach (T result in headers)
            {
                tw.Write(result);
                foreach (T correct in headers)
                {
                    ensureKeyExists2D(correct, result);
                    tw.Write("," + _confusion[correct][result]);
                }
                tw.WriteLine();
            }
        }

    }

}
