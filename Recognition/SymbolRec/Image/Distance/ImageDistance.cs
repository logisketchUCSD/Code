using System;
using System.Collections.Generic;
using SymbolRec.Image;

namespace Metrics
{
    public class ImageDistance : Distance<Image>
	{
		#region Metrics

		public const int HAUSDORFF = 0;
        public const int MODIFIEDHAUSDORFF = 1;
        public const int TANIMOTO = 2;
        public const int YULE = 3;
        public const int VERTICAL = 4;
        public const int HORIZONTAL = 5;
        public const int COMBO = 6;
        public const int DIRECTEDHAUSDORFF_AB = 7;
        public const int DIRECTEDHAUSDORFF_BA = 8;
        public const int DIRECTEDMODIFIEDHAUSDORFF_AB = 9;
        public const int DIRECTEDMODIFIEDHAUSDORFF_BA = 10;
        public const int OVERLAPPING_AB = 11;
        public const int OVERLAPPING_BA = 12;

		#endregion

		#region Internals

		/// <summary>
		/// How on does a pixel need to be to be on?
		/// </summary>
		private static double ON_THRESHOLD = Image.ON_THRESHOLD;
		private static double OFF_THRESHOLD = Image.OFF_THRESHOLD;

        private double m_hasFactor, m_modFactor, m_tanFactor, m_yulFactor;

		private double? _Hausdorff, _ModifiedHausdorff, _HausdorffAB, _HausdorffBA, _ModifiedHausdorffAB, _ModifiedHausdorffBA;
		private double? _Tanimoto, _Yule, _ThresholdTanimoto, _ThresholdYule;

		/// <summary>
		/// The diagonal length of the images
		/// </summary>
		private int _threshold_distance;

		#endregion

		#region Constructors

		public ImageDistance(Image a, Image b) : base(a, b) 
        {
			// Ensure that all cached values are unset
			_Hausdorff = null;
			_ModifiedHausdorff = null;
			_ModifiedHausdorffAB = null;
			_ModifiedHausdorffBA = null;
			_HausdorffAB = null;
			_HausdorffBA = null;
			_Tanimoto = null;
			_Yule = null;
			_ThresholdTanimoto = null;
			_ThresholdYule = null;

            m_hasFactor = 0.25;
            m_modFactor = 0.25;
            m_tanFactor = 0.25;
            m_yulFactor = 0.25;

			if (a.Width != b.Width || a.Height != b.Height)
				throw new ArgumentException("Images must be of the same size to be compared!");

			_threshold_distance = (int)(Math.Sqrt(Math.Pow(a.Width, 2) + Math.Pow(a.Height, 2)) / 20);
		}

		#endregion

		#region Main Distance Function

		/// <summary>
		/// Get the COMBO distance
		/// </summary>
		/// <returns>The distance</returns>
        public override double distance()
        {
            return distance(COMBO);
        }

		/// <summary>
		/// Get the distance using the specified method
		/// </summary>
		/// <param name="method">The method to use for calculating the distance
		/// <example>ImageDistance.HAUSDORFF</example></param>
		/// <returns>The distance</returns>
        public override double distance(int method)
        {
            switch (method)
            {
                case HAUSDORFF:
					return Hausdorff;

                case MODIFIEDHAUSDORFF:
					return ModifiedHausdorff;

                case TANIMOTO:
					return Tanimoto;
                
                case YULE:
					return Yule;
                
                case VERTICAL:
					return Vertical;

                case HORIZONTAL:
					return Horizontal;
                
                case COMBO:
					return m_hasFactor * Hausdorff + m_modFactor * ModifiedHausdorff
						+ m_tanFactor * Tanimoto + m_yulFactor * Yule;
                
                case DIRECTEDHAUSDORFF_AB:
					return DirectedHausdorff_AB;
                
                case DIRECTEDHAUSDORFF_BA:
					return DIRECTEDHAUSDORFF_BA;

                case DIRECTEDMODIFIEDHAUSDORFF_AB:
					return DirectedModifiedHausdorff_AB;

                case DIRECTEDMODIFIEDHAUSDORFF_BA:
					return DirectedModifiedHausdorff_BA;

                case OVERLAPPING_AB:
					return sumOfOverlapping(ref m_a, ref m_b);

                case OVERLAPPING_BA:
                    return sumOfOverlapping(ref m_b, ref m_a);
                
                default:
                    return double.PositiveInfinity;
            }
		}

		#endregion

		#region Accessors

		public double Hausdorff
		{
			get
			{
				if (_Hausdorff == null)
					_Hausdorff = Math.Max(DirectedHausdorff_AB, DirectedHausdorff_BA);
				return _Hausdorff.Value;
			}
		}

		public double DirectedHausdorff_AB
		{
			get
			{

				if (_HausdorffAB == null)
					_HausdorffAB = directedHausdorff(ref m_a, ref m_b, 0.06);
				return _HausdorffAB.Value;
			}
		}

		public double DirectedHausdorff_BA
		{
			get
			{
				if (_HausdorffBA == null)
					_HausdorffBA = directedHausdorff(ref m_b, ref m_a, 0.06);
				return _HausdorffBA.Value;
			}
		}

		public double ModifiedHausdorff
		{
			get
			{
				if (_ModifiedHausdorff == null)
					_ModifiedHausdorff = Math.Max(DirectedModifiedHausdorff_AB, DirectedModifiedHausdorff_BA);
				return _ModifiedHausdorff.Value;
			}
		}

		public double DirectedModifiedHausdorff_AB
		{
			get
			{
				if (_ModifiedHausdorffAB == null)
					_ModifiedHausdorffAB = directedModifiedHausdorff(ref m_a, ref m_b);
				return _ModifiedHausdorffAB.Value;
			}
		}

		public double DirectedModifiedHausdorff_BA
		{
			get
			{
				if (_ModifiedHausdorffBA == null)
					_ModifiedHausdorffBA = directedModifiedHausdorff(ref m_b, ref m_a);
				return _ModifiedHausdorffBA.Value;
			}
		}

		/// <summary>
		/// Finds the Tanimoto disance (using cached values if applicable)
		/// 
		/// This has been transformed to be a distance measure over [0, 1], where 0 is most similar
		/// </summary>
		public double Tanimoto
		{
			get
			{
				if (_Tanimoto == null)
					calculateTanimotoAndYule();
				return _Tanimoto.Value;
			}
		}

		/// <summary>
		/// Finds the Yule distance (using cached values if applicable)
		/// 
		/// This has been transformed to be a distance measure over [0, 1], where 0 is most similar
		/// </summary>
		public double Yule
		{
			get
			{
				if (_Yule == null)
					calculateTanimotoAndYule();
				return _Yule.Value;
			}
		}

		/// <summary>
		/// Finds a thresholded Tanimoto distance.
		/// 
		/// This has been transformed to be a distance measure over [0, 1], where 0 is most similar
		/// </summary>
		public double ThresholdTanimoto
		{
			get
			{
				if (_ThresholdTanimoto == null)
					calculateThresholdTanimotoAndYule();
				return _ThresholdTanimoto.Value;
			}
		}

		/// <summary>
		/// Finds a thresholded Yule distance.
		/// 
		/// This has been transformed to be a distance measure over [0, 1], where 0 is most similar
		/// </summary>
		public double ThresholdYule
		{
			get
			{
				if (_ThresholdYule == null)
					calculateThresholdTanimotoAndYule();
				return _ThresholdYule.Value;
			}
		}

		/// <summary>
		/// Finds the Vertical distance. This isn't a terribly useful measure of anything.
		/// </summary>
		public double Vertical
		{
			get
			{
				return verticalDistance(m_a.matrix, m_b.matrix);
			}
		}

		/// <summary>
		/// Finds the Horizontal distance. This isn't a terribly useful measure of anything.
		/// </summary>
		public double Horizontal
		{
			get
			{
				return horizontalDistance(m_a.matrix, m_b.matrix);
			}
		}

		#endregion

		#region Computation Functions

		private static double directedHausdorff(ref Image A, ref Image B, double percent)
        {
            List<double> distL = new List<double>();

			bool found = false;
            for (int row = 0; row < A.Height; ++row)
            {
                for (int col = 0; col < A.Width; ++col)
                {
					if (A[col, row] > ON_THRESHOLD)
					{
						found = true;
						distL.Add(B.minDistance(col, row));
					}
                }
            }

			if (!found) return Double.PositiveInfinity;

            distL.Sort();
            int len = distL.Count;
            int K = (int)(len * percent);
            if (K < 1)
                K = 1;

            return distL[len - K];
        }

        private double directedHausdorff(Image A, Image B, int K)
        {
            List<double> distL = new List<double>();
			bool found = false;
            for (int row = 0; row < A.Height; ++row)
            {
				for (int col = 0; col < A.Width; ++col)
				{
					if (A[col, row] > ON_THRESHOLD)
					{
						distL.Add(B.minDistance(col, row));
						found = true;
					}
				}
            }

			if (!found) return Double.PositiveInfinity;
            distL.Sort();
            return distL[distL.Count - K];
        }

        private double directedModifiedHausdorff(ref Image A, ref Image B)
        {
            double sum = 0.0;
            int count = 0;

            for (int row = 0; row < A.Height; ++row)
            {
                for (int col = 0; col < A.Width; ++col)
                {
					if (A[col, row] > ON_THRESHOLD)
                    {
                        sum += B.minDistance(col, row);
                        ++count;
                    }
                }
            }

            return sum / count;
        }

        private void calculateTanimotoAndYule()
        {
			double t, y;
            computeCoefficients(out t, out y);
			_Tanimoto = t;
			_Yule = y;
        }

		private void calculateThresholdTanimotoAndYule()
		{
			//nab = number of overlapping black pixels in A and B
			//na = number of black pixels in A
			//nb = number of black pixels in B
			//n00 = number of overlapping white pixels in A and B
			double nab = 0.0, na = 0.0, nb = 0.0, n00 = 0.0;
			for (int row = 0; row < m_a.Height; ++row)
			{
				for (int col = 0; col < m_a.Width; ++col)
				{
					if (m_a[col, row] > ON_THRESHOLD)
						++na;
					if (m_b[col, row] > ON_THRESHOLD)
						++nb;
					bool wm = false;
					bool bm = false;
					// Search all pixels within _threshold_distance for a match (actually, a little more, since
					// it's easier/faster to search in a square than a circle)
					for (int subCol = _threshold_distance; subCol > -_threshold_distance && bm == false && wm == false && col+subCol > 0 && col+subCol < m_b.Width; --subCol)
					{
						for (int subRow = _threshold_distance; subRow > -_threshold_distance && bm == false && wm == false && row+subRow > 0 && row+subRow < m_b.Height; --subRow)
						{
							if (m_a[col, row] > ON_THRESHOLD && m_b[col + subCol, row + subRow] > ON_THRESHOLD)
								bm = true;
							if (m_b[col, row] < OFF_THRESHOLD && m_b[col + subCol, row + subRow] < OFF_THRESHOLD)
								wm = true;
						}
					}
					if (bm)
						++nab;
					if (wm)
						++n00;
				}
			}
			//from Image-based paper
			//weight towards black pixels
			double alpha = 0.75 - 0.25 * (na + nb) / (2 * m_a.Height * m_a.Width);

			//is [0, 1], where 0 is minimum sim. 1 is max sim.
			_ThresholdTanimoto = (alpha * nab) / (na + nb - nab) + (1.0 - alpha) * n00 / (na + nb - 2 * nab + n00);

			double W = nab * n00;
			double Z = (na - nab) * (nb - nab);

			//is [-1, 1], where -1 is min sim 1 is max sim
			_ThresholdYule = (W - Z) / (W + Z);

			//scale
			_ThresholdTanimoto = 1.0 - _ThresholdTanimoto;
			_ThresholdYule = 1.0 - ((1.0 + _ThresholdYule) * 0.5);
		}

		private void computeCoefficients(out double tanimoto, out double yule)
        {
            //nab = number of overlapping black pixels in A and B
            //na = number of black pixels in A
            //nb = number of black pixels in B
            //n00 = number of overlapping white pixels in A and B
            double nab = 0.0, na = 0.0, nb = 0.0, n00 = 0.0;
            for (int row = 0; row < m_a.Height; ++row)
            {
                for (int col = 0; col < m_a.Width; ++col)
                {
					if (m_a[col, row] > ON_THRESHOLD)
						++na;
					if (m_b[col, row] > ON_THRESHOLD)
						++nb;
					if (m_a[col, row] > ON_THRESHOLD && m_b[col, row] > ON_THRESHOLD)
						++nab;
					if (m_a[col, row] < OFF_THRESHOLD && m_b[col, row] < OFF_THRESHOLD)
						++n00;
                }
            }

            //from Image-based paper
            //weight towards black pixels
            double alpha = 0.75 - 0.25 * (na + nb) / (2 * m_a.Height * m_a.Width);

            //is [0, 1], where 0 is minimum sim. 1 is max sim.
            tanimoto = (alpha * nab) / (na + nb - nab) + (1.0 - alpha) * n00 / (na + nb - 2 * nab + n00);

            double W = nab * n00;
            double Z = (na - nab) * (nb - nab);

            //is [-1, 1], where -1 is min sim 1 is max sim
            yule = (W - Z) / (W + Z);

            //scale
            tanimoto = 1.0 - tanimoto;
            yule = 1.0 - ((1.0 + yule) * 0.5);
        }

        /// <summary>
        /// Returns the total vertical distance that image A is away from image B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B">An average image</param>
        /// <returns></returns>
        private double verticalDistance(MathNet.Numerics.LinearAlgebra.Matrix A, MathNet.Numerics.LinearAlgebra.Matrix B)
        {
            double dist = 0.0;
            int i, j, len = A.RowCount, len2 = A.ColumnCount;

            double[] column = new double[len];

            //go over columns
            for (j = 0; j < len2; ++j)
            {
				column = getMatrixColumn(B, j);

                //go over rows
                for (i = 0; i < len; ++i)
                {
                    //If it is a black pixel
                    if (A[i, j] > ON_THRESHOLD)
                    {
                        dist += verticalDistance(i, column);
                    }
                }
            }
            return dist;
        }

        private double verticalDistance(int row, double[] column)
        {
            double dist = 0.0;
            //double abs;
            int i, len = column.Length;
            for (i = 0; i < len; ++i)
            {
                dist += Math.Abs(row - i) * column[i];
                //abs = Math.Abs(row - i);
                //dist += abs * abs * column[i];
            }
            return dist;
        }

        /// <summary>
        /// Returns the total horizontal distance that image A is away from image B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B">An average image</param>
        /// <returns></returns>
        private double horizontalDistance(MathNet.Numerics.LinearAlgebra.Matrix A, MathNet.Numerics.LinearAlgebra.Matrix B)
        {
            double dist = 0.0;
            int i, j, len = A.ColumnCount, len2 = A.RowCount;

            double[] row = new double[len];

            //go over rows
            for (j = 0; j < len2; ++j)
            {
                row = getMatrixRow(B, j);

                //go over cols
                for (i = 0; i < len; ++i)
                {
                    //If it is a black pixel
                    if (A[j, i] > 0.0)
                    {
                        dist += horizontalDistance(i, row);
                    }
                }
            }
            return dist;
        }

        private double horizontalDistance(int col, double[] row)
        {
            return verticalDistance(col, row);
        }

        private double sumOfOverlapping(ref Image A, ref Image B)
        {
            double dist = 0.0;
            int i, j, len2 = A.Width, len = A.Height;
            for (i = 0; i < len; ++i)
            {
                for (j = 0; j < len2; ++j)
                {
                    if (A.matrix[i, j] > 0.0)
                        dist += B.matrix[i, j];
                }
            }
            return dist;
        }

		private double[] getMatrixColumn(MathNet.Numerics.LinearAlgebra.Matrix m, int idx)
		{
			double[] res = new double[m.RowCount];
			for (int i = 0; i < m.RowCount; ++i)
				res[i] = m[i, idx];
			return res;
		}

		private double[] getMatrixRow(MathNet.Numerics.LinearAlgebra.Matrix m, int idx)
		{
			double[] res = new double[m.ColumnCount];
			for (int i = 0; i < m.ColumnCount; ++i)
				res[i] = m[idx, i];
			return res;
		}

		#endregion
	}
}
