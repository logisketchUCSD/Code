// AForge Library
//
// Copyright © Andrew Kirillov, 2006
// andrew.kirillov@gmail.com
//

namespace Utilities
{
	using System;
    using System.Runtime.Serialization;

    [Serializable()]

	/// <summary>
	/// Represents an integer range with minimum and maximum values
	/// </summary>
	public class IntRange : ISerializable
	{
		private int min, max;

		/// <summary>
		/// Minimum value
		/// </summary>
		public int Min
		{
			get { return min; }
			set { min = value; }
		}

		/// <summary>
		/// Maximum value
		/// </summary>
		public int Max
		{
			get { return max; }
			set { max = value; }
		}

		/// <summary>
		/// Length of the range (deffirence between maximum and minimum values)
		/// </summary>
		public int Length
		{
			get { return max - min; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IntRange"/> class
		/// </summary>
		/// 
		/// <param name="min">Minimum value of the range (inclusive)</param>
		/// <param name="max">Maximum value of the range (inclusive)</param>
		public IntRange( int min, int max )
		{
			this.min = min;
			this.max = max;
		}

		/// <summary>
		/// Check if the specified value is inside this range
		/// </summary>
		/// 
		/// <param name="x">Value to check</param>
		/// 
		/// <returns><b>True</b> if the specified value is inside this range or
		/// <b>false</b> otherwise.</returns>
		/// 
		public bool Contains( int x )
		{
			return ( ( x >= min ) && ( x <= max ) );
		}

		/// <summary>
		/// Check if the specified range is inside this range
		/// </summary>
		/// 
		/// <param name="range">Range to check</param>
		/// 
		/// <returns><b>True</b> if the specified range is inside this range or
		/// <b>false</b> otherwise.</returns>
		/// 
		public bool Contains( IntRange range )
		{
			return ( ( Contains( range.min ) ) && ( Contains( range.max ) ) );
		}

		/// <summary>
		/// Check if the specified range overlaps with this range
		/// </summary>
		/// 
		/// <param name="range">Range to check for overlapping</param>
		/// 
		/// <returns><b>True</b> if the specified range overlaps with this range or
		/// <b>false</b> otherwise.</returns>
		/// 
		public bool IsOverlapping( IntRange range )
		{
			return ( ( Contains( range.min ) ) || ( Contains( range.max ) ) );
        }

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public IntRange(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            min = (int)info.GetValue("min", typeof(int));
            max = (int)info.GetValue("max", typeof(int));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("min", min);
            info.AddValue("max", max);
        }

        #endregion
    }
}
