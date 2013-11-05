using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities.Concurrency;

namespace CircuitSimLib
{
    public class TruthTable
    {
        #region Internals

        /// <summary>
        /// List of Shapes corresponding to inputs
        /// </summary>
        private List<Sketch.Shape> inputShapes;

        /// <summary>
        /// List of Shapes corresponding to outputs
        /// </summary>
        private List<Sketch.Shape> outputShapes;

        /// <summary>
        /// List of the Output circuit elements
        /// </summary>
        private List<OUTPUT> outputs;

        /// <summary>
        /// Dictionary, the key is a list of input values, the value is a list of output values 
        /// </summary>
        private Future<Dictionary<List<int>, List<int>>> truthTableDictionary;

        /// <summary>
        /// Similar to the dictionary, but requires no key (must know num inputs to decode)
        /// </summary>
        private List<List<int>> truthTableOutputs;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for a truth table, given a circuit, List of inputs, and List of outputs
        /// </summary>
        /// <param name="circuit"></param>
        /// <param name="inputs"></param>
        /// <param name="heads"></param>
        public TruthTable(Circuit circuit)
        {
            truthTableOutputs = new List<List<int>>();
            this.inputShapes = circuit.InputShapes;
            this.outputShapes = circuit.OutputShapes;
            this.outputs = circuit.Outputs;

            // Create the dictionary
            int numInputs = inputShapes.Count;

            truthTableDictionary =  new Future<Dictionary<List<int>, List<int>>>(delegate()
            {
                return calculate(circuit);
            });
        }


        #endregion

        #region Future work

        private Dictionary<List<int>, List<int>> calculate(Circuit circuit)
        {
            Dictionary<List<int>, List<int>> results = new Dictionary<List<int>, List<int>>();
            List<Sketch.Shape> sortedInputs = circuit.InputShapes;
            if (sortedInputs.Count == 0)
                return results;
            for (int i = 0; i < Math.Pow(2, sortedInputs.Count); i++)
            {
                // Push all input values to the circuit
                List<int> key = toBinary(i, sortedInputs.Count);
                for (int input = 0; input < sortedInputs.Count; input++)
                {
                    circuit.setInputValue(sortedInputs[input], key[input]);
                }

                // Pull out the output values
                List<int> value = new List<int>();
                foreach (OUTPUT output in outputs)
                    value.Add(output.calculate()[0]);

                results.Add(key, value);
                truthTableOutputs.Add(new List<int>(key.Concat(value)));
            }
            return results;
        }
        #endregion

        #region Getters and Setters

        /// <summary>
        /// returns a List of strings that represent the header of a truth table
        /// </summary>
        public List<Sketch.Shape> TruthTableHeader
        {
            get
            {
                return new List<Sketch.Shape>(this.inputShapes.Concat(this.outputShapes));
            }
        }

        /// <summary>
        /// returns a Dictionary with keys as input combinations and values as output value(s)
        /// </summary>
        public Dictionary<List<int>, List<int>> TruthTableDictionary
        {
            get
            {
                return this.truthTableDictionary.Value;
            }
        }

        /// <summary>
        /// returns a list with lists of input and output values
        /// </summary>
        public List<List<int>> TruthTableOutputs
        {
            get
            {
                return truthTableOutputs;
            }
        }

        /// <summary>
        /// returns a list with lists of inputs
        /// </summary>
        public List<Sketch.Shape> Inputs
        {
            get
            {
                return this.inputShapes;
            }
        }

        /// <summary>
        /// returns a list with lists of outputs
        /// </summary>
        public List<Sketch.Shape> Outputs
        {
            get
            {
                return this.outputShapes;
            }
        }

        #endregion

        #region Helper Method

        /// <summary>
        /// helper method that converts decimal to binary with the correct amount of placeholders
        /// </summary>
        /// <param name="num"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public List<int> toBinary(int num, int size)
        {
            List<int> ans = new List<int>();
            int[] lol = new int[size];
            for (int i = 0; i < lol.Length; i++)
            {
                if (num > 0)
                {
                    lol[i] = num % 2;
                    num = num / 2;
                }
                else
                {
                    lol[i] = 0;
                }
            }
            for (int i = lol.Length - 1; i > -1; i--)
            {
                ans.Add(lol[i]);
            }
            return ans;
        }
        #endregion
    }
}
