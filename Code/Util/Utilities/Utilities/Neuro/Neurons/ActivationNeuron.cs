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
	/// Activation neuron
	/// </summary>
	/// 
	/// <remarks>Activation neuron computes weighted sum of its inputs, adds
	/// threshold value and then applies activation function. The neuron is
	/// usually used in multi-layer neural networks.</remarks>
	/// 
    [Serializable()]
    public class ActivationNeuron : Neuron, ISerializable
	{
		/// <summary>
		/// Threshold value
		/// </summary>
		/// 
		/// <remarks>The value is added to inputs weighted sum.</remarks>
		/// 
		protected double threshold = 0.0f;

		/// <summary>
		/// Activation function
		/// </summary>
		/// 
		/// <remarks>The function is applied to inputs weighted sum plus
		/// threshold value.</remarks>
		/// 
		protected IActivationFunction function = null;

		/// <summary>
		/// Threshold value
		/// </summary>
		/// 
		/// <remarks>The value is added to inputs weighted sum.</remarks>
		/// 
		public double Threshold
		{
			get { return threshold; }
			set { threshold = value; }
		}

		/// <summary>
		/// Neuron's activation function
		/// </summary>
		/// 
		public IActivationFunction ActivationFunction
		{
			get { return function; }
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ActivationNeuron"/> class
		/// </summary>
		/// 
		/// <param name="inputs">Neuron's inputs count</param>
		/// <param name="function">Neuron's activation function</param>
		/// 
		public ActivationNeuron( int inputs, IActivationFunction function ) : base( inputs )
		{
			this.function = function;
		}

		/// <summary>
		/// Randomize neuron 
		/// </summary>
		/// 
		/// <remarks>Calls base class <see cref="Neuron.Randomize">Randomize</see> method
		/// to randomize neuron's weights and then randomize threshold's value.</remarks>
		/// 
		public override void Randomize( )
		{
			// randomize weights
			base.Randomize( );
			// randomize threshold
			threshold = rand.NextDouble( ) * ( randRange.Length ) + randRange.Min;
		}

		/// <summary>
		/// Computes output value of neuron
		/// </summary>
		/// 
		/// <param name="input">Input vector</param>
		/// 
		/// <returns>Returns neuron's output value</returns>
		/// 
		/// <remarks>The output value of activation neuron is equal to value
		/// of nueron's activation function, which parameter is weighted sum
		/// of its inputs plus threshold value. The output value is also stored
		/// in <see cref="Neuron.Output">Output</see> property.</remarks>
		/// 
		public override double Compute( double[] input )
		{
			// check for corrent input vector
			if ( input.Length != inputsCount )
				throw new ArgumentException( );

			// initial sum value
			double sum = 0.0;

			// compute weighted sum of inputs
			for ( int i = 0; i < inputsCount; i++ )
			{
				sum += weights[i] * input[i];
			}
			sum += threshold;

			return ( output = function.Function( sum ) );
		}

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public ActivationNeuron(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            threshold = (double)info.GetValue("threshold", typeof(double));
            function = (IActivationFunction)info.GetValue("function", typeof(IActivationFunction));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("threshold", threshold);
            info.AddValue("function", function);

            base.GetObjectData(info, ctxt);
        }

        #endregion
	}
}
