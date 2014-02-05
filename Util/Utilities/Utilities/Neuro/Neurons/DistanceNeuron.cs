// AForge Neural Net Library
//
// Copyright © Andrew Kirillov, 2005-2006
// andrew.kirillov@gmail.com
//
// Modified by Eric Peterson to include Serialization

namespace Utilities.Neuro
{
	using System;
    using System.Runtime.Serialization;

	/// <summary>
	/// Distance neuron
	/// </summary>
	/// 
	/// <remarks>Distance neuron computes its output as distance between
	/// its weights and inputs. The neuron is usually used in Kohonen
	/// Self Organizing Map.</remarks>
	[Serializable()]
	public class DistanceNeuron : Neuron, ISerializable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DistanceNeuron"/> class
		/// </summary>
		/// 
		/// <param name="inputs">Neuron's inputs count</param>
		/// 
		public DistanceNeuron( int inputs ) : base( inputs ) { }

	
		/// <summary>
		/// Computes output value of neuron
		/// </summary>
		/// 
		/// <param name="input">Input vector</param>
		/// 
		/// <returns>The output value of distance neuron is equal to distance
		/// between its weights and inputs - sum of absolute differences.
		/// The output value is also stored in <see cref="Neuron.Output">Output</see>
		/// property.</returns>
		/// 
		public override double Compute( double[] input )
		{
			output = 0.0;

			// compute distance between inputs and weights
			for ( int i = 0; i < inputsCount; i++ )
			{
				output += Math.Abs( weights[i] - input[i] );
			}
			return output;
		}

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public DistanceNeuron(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
        }

        #endregion
	}
}
