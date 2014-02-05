using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TestRig.Utils
{
    class Table
    {

        private List<string> _headers;
        private List<string[]> _results;

        public Table(IEnumerable<string> headers)
        {
            _headers = new List<string>(headers);
            _results = new List<string[]>();
        }

        public void addResult<T>(IEnumerable<T> result)
        {
            List<string> strings = new List<string>();
            foreach (T o in result)
            {
                strings.Add(o.ToString());
            }

            _results.Add(strings.ToArray());
        }

        public void writeCSV(System.IO.TextWriter tw)
        {
            // Writes a CSV (comma-separated values) file which can be read by Excel.

            bool first = true;

            foreach (string h in _headers)
            {
                if (!first)
                    tw.Write(",");
                first = false;
                tw.Write(h);
            }
            tw.WriteLine();

            foreach (string[] result in _results)
            {
                first = true;
                foreach (string value in result)
                {
                    if (!first)
                        tw.Write(",");
                    first = false;
                    tw.Write(value);
                }
                tw.WriteLine();
            }
        }

    }
}
