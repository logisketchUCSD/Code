/*
 * File: SingleStrokeAlign.cs
 *
 * Author: James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SymbolRec;
using Sketch;

namespace Aligner
{
	public class SingleStrokeAlign : IAlign
	{
		/// <summary>
		/// Which feature we should attempt to match to
		/// </summary>
		public enum AlignFeature {
			/// <summary>
			/// The stroke's length as a fraction of th total shape length
			/// </summary>
			Length,
			/// <summary>
			/// The stroke's curvature
			/// </summary>
			Curvature,
			/// <summary>
			/// What the stroke's length rank is (i.e., longest, second-longest, etc)
			/// </summary>
			LengthRank,
			/// <summary>
			/// How much of an angle has this stroke carved out?
			/// </summary>
			AngleTraveled,
			/// <summary>
			/// What is this stroke's angle traveled rank?
			/// </summary>
			AngleTraveledRank
		};

		#region Internals

		private Substroke _template;
		private Shape _shape;
		private Dictionary<AlignFeature, double> _tf;

		#endregion

		/// <summary>
		/// Create a new SSA object to match against a single substroke
		/// </summary>
		/// <param name="ss">The Substroke to match against</param>
		/// <param name="s">The shape containing the important substroke</param>
		/// <param name="f">Which feature should we be aligning on the basis of?
		/// <example>Congeal.Prealign.AlignFeature.Length</example></param>
		public SingleStrokeAlign(Substroke ss, Shape s, AlignFeature f)
			: this(ss, s, new List<AlignFeature>(new AlignFeature[] { f } ))
		{
			// Nothing to do here
		}

		/// <summary>
		/// Construct a new SSA object to match against a single substroke
		/// </summary>
		/// <param name="ss">The substroke to match against</param>
		/// <param name="s">The shape containing the important substroke</param>
		/// <param name="fs">A list of features to align on the basis of. Each feature gets one "vote"</param>
		public SingleStrokeAlign(Substroke ss, Shape s, List<AlignFeature> fs)
		{
			_template = ss;
			_shape = s;
			_tf = new Dictionary<AlignFeature, double>();
			foreach (AlignFeature f in fs)
			{
				_tf.Add(f, double.NaN);
			}
			calculateParam(s);
		}

		public Sketch.Shape align(Sketch.Shape shape)
		{
			Sketch.Shape aligned = shape.Clone();
			string imId = aligned.Id.ToString();

			Dictionary<Substroke, int> votes = new Dictionary<Substroke, int>();

			foreach (Substroke s in aligned.SubstrokesL)
			{
				votes.Add(s, 0);
			}

			foreach (AlignFeature af in _tf.Keys)
			{
				double goodness = Double.PositiveInfinity; // Best match so far
				Substroke lbest = null;
				foreach (Substroke ss in aligned.SubstrokesL)
				{
					double current = match(ss, aligned, af);
					if (current < goodness)
					{
						lbest = ss;
						goodness = current;
					}
				}
				++votes[lbest];
			}
			int maxvote = 0;
			Substroke best = null;
			foreach (KeyValuePair<Substroke, int> kvp in votes)
				if (kvp.Value > maxvote)
				{
					maxvote = kvp.Value;
					best = kvp.Key;
				}

			if (best == null)
				return shape;

			// Step 1 is to uniformly scale so that the keystroke and the match stroke are the same length
			double scaleFactor = _template.SpatialLength / best.SpatialLength;
			aligned.scale(scaleFactor);

			// Next, let's find some points in the template
			Point TTop = _template.PointsL[0];
			Point TBottom;
			findFarthestPointFrom(TTop, _template, out TBottom);
			if (TBottom.Y > TTop.Y)
				swapPts(ref TTop, ref TBottom);

			double TMiddleX = (TTop.X + TBottom.X) / 2.0;
			double TMiddleY = (TTop.Y + TBottom.Y) / 2.0;

			double TCentroidX = _shape.Centroid[0];
			double TCentroidY = _shape.Centroid[1];

			// Now some points in the match
			Point BTop = best.PointsL[0];
			Point BBottom;
			findFarthestPointFrom(BTop, best, out BBottom);
			if (BBottom.Y > BTop.Y)
				swapPts(ref BTop, ref BBottom);

			double BMiddleX = (BTop.X + BBottom.X) / 2.0;
			double BMiddleY = (BTop.Y + BBottom.Y) / 2.0;
			double BCentroidX = aligned.Centroid[0];
			double BCentroidY = aligned.Centroid[1];

			// The first step in alignment is rotation. We want the angle between the line BMiddle-BCentroid and horizontal to
			// be the same as the angle between the line TMiddle-TCentroid and horizontal.
			double ttheta = Math.Atan2(TCentroidY - TMiddleY, TCentroidX- TMiddleX);
			double btheta = Math.Atan2(BCentroidY - BMiddleY, BCentroidX - BMiddleX);
			double deltaTheta = ttheta - btheta;
			aligned.rotate(deltaTheta);
			
			/*
			BMiddleX = (BTop.X + BBottom.X) / 2.0;
			BMiddleY = (BTop.Y + BBottom.Y) / 2.0;
			BCentroidX = shape.Centroid[0];
			BCentroidY = shape.Centroid[1];
			btheta = Math.Atan2(BCentroidY - BMiddleY, BCentroidX - BMiddleX);

			Substroke middleToCenter = new Substroke(new Point[2] {
				new Point((float)BMiddleX, (float)BMiddleY), new Point((float)BCentroidX,(float)BCentroidY)
			}, new XmlStructs.XmlShapeAttrs(true));
			middleToCenter.XmlAttrs.Time = (ulong)DateTime.Now.Ticks;
			aligned.AddSubstroke(middleToCenter);
			// Finally, we want to scale along the Middle-Centroid axis such that the lengths of the Middle-Centroid lines are the same
			scaleFactor = Math.Sqrt(Math.Pow(TCentroidX - TMiddleX, 2) + Math.Pow(TCentroidY - TMiddleY, 2)) / Math.Sqrt(Math.Pow(BCentroidX - BMiddleX, 2) + Math.Pow(BCentroidY - BMiddleY, 2));
			aligned.rotate(-btheta);
			SymbolRec.Image.Image image = new SymbolRec.Image.Image(64, 64, aligned);
			image.writeToBitmap(String.Format("{0}\\output\\shape_{1}_postrotate.png", System.IO.Directory.GetCurrentDirectory(), imId));
			aligned.transform(new MathNet.Numerics.LinearAlgebra.Matrix(new double[][] { new double[] { 1, 0, 0 }, new double[] { 0, scaleFactor, 0 }, new double[] { 0, 0, 1 } }));
			image = new SymbolRec.Image.Image(64, 64, aligned);
			image.writeToBitmap(String.Format("{0}\\output\\shape_{1}_postaxialscale.png", System.IO.Directory.GetCurrentDirectory(), imId));
			aligned.rotate(ttheta);
			image = new SymbolRec.Image.Image(64, 64, aligned);
			image.writeToBitmap(String.Format("{0}\\output\\shape_{1}_postnextrotate.png", System.IO.Directory.GetCurrentDirectory(), imId));
			 */
			return aligned;
		}

		#region Private Utility Functions

		/// <summary>
		/// Calculate the parameter corresponding to our AlignFeature
		/// </summary>
		private void calculateParam(Shape s)
		{
			Featurefy.FeatureStroke fs = new Featurefy.FeatureStroke(_template);
			List<AlignFeature> keys = new List<AlignFeature>(_tf.Count);
			foreach (AlignFeature af in _tf.Keys) keys.Add(af);
			foreach (AlignFeature af in keys)
			{
				switch (af)
				{
					case AlignFeature.Curvature:
						_tf[af] = fs.Curvature.AverageCurvature;
						break;
					case AlignFeature.AngleTraveled:
						_tf[af] = fs.Curvature.TotalAngle;
						break;
					case AlignFeature.AngleTraveledRank:
						List<Substroke> ordered_substrokes = new List<Substroke>(s.SubstrokesL);
						ordered_substrokes.Sort(delegate(Substroke lhs, Substroke rhs)
						{
							Featurefy.FeatureStroke lfs = new Featurefy.FeatureStroke(lhs);
							Featurefy.FeatureStroke rfs = new Featurefy.FeatureStroke(rhs);
							return rfs.Curvature.TotalAngle.CompareTo(lfs.Curvature.TotalAngle);
						});
						_tf[af] = ordered_substrokes.IndexOf(_template);
						break;
					case AlignFeature.Length:
						double len = 0.0;
						foreach (Substroke test in s.SubstrokesL)
							len += test.SpatialLength;
						_tf[af] = _template.SpatialLength / len;
						break;
					case AlignFeature.LengthRank:
						List<Substroke> lr_ordered_substrokes = new List<Substroke>(s.SubstrokesL);
						lr_ordered_substrokes.Sort(delegate(Substroke lhs, Substroke rhs)
							{
								return rhs.SpatialLength.CompareTo(lhs.SpatialLength);
							}
						);
						_tf[af] = lr_ordered_substrokes.IndexOf(_template);
						break;
				}
			}
		}

		/// <summary>
		/// Attempts to match a substroke with the current parameter. Returns
		/// some measure of dissimilarity (that is, smaller numbers are better)
		/// <remarks>Goodness measure is not comparable between AlignFeatures!</remarks>
		/// </summary>
		/// <param name="ss">The substroke to match</param>
		private double match(Substroke ss, Shape sp, AlignFeature af)
		{
			Featurefy.FeatureStroke fs = new Featurefy.FeatureStroke(ss);
			switch (af)
			{
				case AlignFeature.Curvature:
					return Math.Abs(_tf[af] - fs.Curvature.AverageCurvature);

				case AlignFeature.AngleTraveled:
					return Math.Abs(_tf[af] - fs.Curvature.TotalAngle);

				case AlignFeature.AngleTraveledRank:
					List<Substroke> ordered_substrokes = new List<Substroke>(sp.SubstrokesL);
					ordered_substrokes.Sort(delegate(Substroke lhs, Substroke rhs)
					{
						Featurefy.FeatureStroke lfs = new Featurefy.FeatureStroke(lhs);
						Featurefy.FeatureStroke rfs = new Featurefy.FeatureStroke(rhs);
						return rfs.Curvature.TotalAngle.CompareTo(lfs.Curvature.TotalAngle);
					} );
					return Math.Abs(_tf[af] - ordered_substrokes.IndexOf(ss));

				case AlignFeature.Length:
					double total_length = 0.0;
					foreach (Substroke test in sp.SubstrokesL)
					{
						total_length += test.SpatialLength;
					}
					return Math.Abs(ss.SpatialLength / total_length - _tf[af]);

				case AlignFeature.LengthRank:
					List<Substroke> lr_ordered_substrokes = new List<Substroke>(sp.SubstrokesL);
					lr_ordered_substrokes.Sort(delegate(Substroke lhs, Substroke rhs)
						{
							return rhs.SpatialLength.CompareTo(lhs.SpatialLength);
						}
					);
					return Math.Abs(_tf[af] - lr_ordered_substrokes.IndexOf(ss));
			}
			return 0.0;
		}

		/// <summary>
		/// Swap some pointers
		/// </summary>
		/// <param name="lhs">LHS</param>
		/// <param name="rhs">RHS</param>
		private static void swapPts(ref Point lhs, ref Point rhs)
		{
			Point temp;
			temp = lhs;
			lhs = rhs;
			rhs = temp;
		}

		/// <summary>
		/// Find the farthest point in a substroke from a given point
		/// </summary>
		/// <param name="point">The starting point</param>
		/// <param name="inSubstroke">The substroke to look ib</param>
		/// <param name="isPoint">The output point</param>
		/// <returns>The distance between the points</returns>
		private static double findFarthestPointFrom(Point point, Substroke inSubstroke, out Point isPoint)
		{
			double maxDistance = 0.0;
			Point best = null;
			foreach (Point p in inSubstroke.PointsL)
			{
				double distance = point.distance(p);
				if (distance > maxDistance)
				{
					best = p;
					maxDistance = distance;
				}
			}
			if (best != null)
				isPoint = best;
			else
				isPoint = point;
			return maxDistance;
		}

		#endregion
	}
}