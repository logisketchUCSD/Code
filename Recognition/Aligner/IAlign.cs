using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Aligner
{
	/// <summary>
	/// Interface for shape alignment outside of the standard image-based congealer
	/// </summary>
	public interface IAlign
	{
		/// <summary>
		/// Prealign a group of strokes
		/// </summary>
		/// <param name="strokes">A sketch object containing what
		/// we hope is a single shape</param>
		/// <returns>The correctly aligned shape</returns>
		Shape align(Shape shape);
	}
}
