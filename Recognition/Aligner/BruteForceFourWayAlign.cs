using System;
using System.Collections.Generic;
using System.Text;

namespace Aligner
{
	public class BruteForceFourWayAlign : IAlign
	{
		#region INTERNALS

		private SymbolRec.Image.Image _trained;

		#endregion

		/// <summary>
		/// Constructs a new BFFWA
		/// </summary>
		/// <param name="trainedBitmap">A trained AvgImage</param>
		public BruteForceFourWayAlign(SymbolRec.Image.Image trainedBitmap)
		{
			_trained = trainedBitmap;
		}

		/// <summary>
		/// Align a shape by brute-forcing through 90-degree rotations
		/// and seeing which one yields the best results
		/// </summary>
		/// <param name="toalign">The shape to align (will not be mutated)</param>
		/// <returns>The aligned shape</returns>
		public Sketch.Shape align(Sketch.Shape toalign)
		{
			Sketch.Shape aligned = toalign.Clone();
			SymbolRec.Image.Image i = new SymbolRec.Image.Image(_trained.Width, _trained.Height, new SymbolRec.Substrokes(aligned.SubstrokesL));
			Metrics.ImageDistance d = new Metrics.ImageDistance(_trained, i);
			int rotbest = 0;
			double dbest = d.ModifiedHausdorff;
			for (int rotation = 90; rotation < 360; rotation += 90)
			{
				aligned.rotate(DtR(rotation));
				i = new SymbolRec.Image.Image(_trained.Width, _trained.Height, new SymbolRec.Substrokes(toalign.SubstrokesL));
				d = new Metrics.ImageDistance(_trained, i);
				double dnew = d.ModifiedHausdorff;
				if (dnew < dbest)
				{
					dbest = dnew;
					rotbest = rotation;
				}
				aligned.rotate(-DtR(rotation));
			}
			aligned.rotate(DtR(rotbest));
			return aligned;
		}

		/// <summary>
		/// Returns the correct CongealParameters that will yield a best-aligned shape.
		/// Aligns by brute-forcing through 90-degree rotations
		/// </summary>
		/// <param name="toalign">The image to align. Will not be modified (i.e., aligned) by this function</param>
		/// <returns>The correct number of degrees to rotate</returns>
		public double align(SymbolRec.Image.Image toalign)
		{
			SymbolRec.Image.Image rotated;
			Metrics.ImageDistance d = new Metrics.ImageDistance(_trained, toalign);
			int rotbest = 0;
			double dbest = d.ModifiedHausdorff;
			for (int rotation = 90; rotation < 360; rotation += 90)
			{
				rotated = toalign.rotate(DtR(rotation), toalign.WeightedCenter[0], toalign.WeightedCenter[1]);
				d = new Metrics.ImageDistance(_trained, rotated);
				double dnew = d.ModifiedHausdorff;
				if (dnew < dbest)
				{
					dbest = dnew;
					rotbest = rotation;
				}
			}
			return rotbest;
		}

		#region Utility

		/// <summary>
		/// Convert degrees to radians
		/// <param name="degrees">Degrees</param>
		/// </summary>
		private double DtR(double degrees)
		{
			return degrees * Math.PI / 180.0;
		}

		#endregion
	}
}
