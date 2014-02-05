using System;
using System.Collections.Generic;
using Sketch;
using MathNet.Numerics.LinearAlgebra;

namespace SymbolRec.Image
{
    /// <summary>
    /// Stores the polar version of an image.
    /// The "magic" is done in GetCoordinates, which performs the conversion to polar coordinates
    /// </summary>
    public class PolarImage : Image
    {
        #region CONSTRUCTORS

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        public PolarImage(int width, int height)
            : base(width, height)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <param name="fill">Default value of each pixel</param>
        public PolarImage(int width, int height, double fill)
            : base(width, height, fill)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <param name="substrokes">Substrokes to convert into the image</param>
        public PolarImage(int width, int height, Substrokes substrokes)
            : base(width, height, substrokes)
        {
        }

        #endregion

        #region TRANSLATION

        /// <summary>
        /// Translates to the best image
        /// </summary>
        /// <param name="A">Image to match to</param>
        /// <param name="angle">The angle in radians that the image was translated</param>
        /// <param name="bestDist">The distance of the best match</param>
        /// <returns>The index of the best image</returns>
        public int translateToMatch(DefinitionImage[] A, out double angle, out double bestDist)
        {
            //Find the best amount to move cols
            int colsToTranslate;
            int index = findBestTranslation(A, out colsToTranslate, out bestDist);

            //Translate the results back onto ourself
            Image temp = new Image(this);
			TranslateMatrix(colsToTranslate, ref temp.matrix, ref matrix);

            angle = colsToAngle(colsToTranslate);

            return index;
        }

        /// <summary>
        /// Translate this image
        /// </summary>
        /// <param name="cols"></param>
        public void translate(int cols)
        {
            //Translate the results back onto ourself
            Image temp = new Image(this);
			TranslateMatrix(cols, ref temp.matrix, ref matrix);
        }

        /// <summary>
        /// Finds the amount to translate each column, and translates that amount.
        /// </summary>
        /// <param name="A">Image to match to</param>
        /// <param name="angle">The angle in radians that the image was translated</param>
        /// <param name="bestDist">The dist of the best match</param>
        public void translateToMatch(DefinitionImage A, out double angle, out double bestDist)
        {
            //Find the best amount to move cols
            int colsToTranslate;
            findBestTranslation(A, out colsToTranslate, out bestDist);

            translate(colsToTranslate);

            angle = colsToAngle(colsToTranslate);
        }

        /// <summary>
        /// Trys to rotate to match each of the images.
        /// </summary>
        /// <param name="A">Images to match to</param>
        /// <param name="bestCol">Best amount to move the cols</param>
        /// <param name="bestDist">Dist achieved when moved bestCol</param>
        /// <returns>Index of the best image to use</returns>
        public int findBestTranslation(DefinitionImage[] A, out int bestCol, out double bestDist)
        {
            double dist;
            bestDist = double.PositiveInfinity;
            bestCol = -1;
            int bestIndex = -1;

            int i, col, len = A.Length;
            for (i = 0; i < len; ++i)
            {
                findBestTranslation(A[i], out col, out dist);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCol = col;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        /// <summary>
        /// Finds the amount to rotate against each of the DefinitionImages.
        /// </summary>
        /// <param name="A">DefinitionImages to rotate to</param>
        /// <param name="angles">Returns the angles that best match the DefinitionImages</param>
        public void findTranslations(List<DefinitionImage> A, out double[] angles)
        {
            int i, col, len = A.Count;
            angles = new double[len];
            double dist;

            for (i = 0; i < len; ++i)
            {
                findBestTranslation(A[i], out col, out dist);
                angles[i] = colsToAngle(col);
            }
        }

        /// <summary>
        /// Finds the amount to move each column by translating all of the columns and finding the best one.
        /// </summary>
        /// <param name="A">Image to match to</param>
        /// <param name="cols">Number of columns to best translation</param>
        /// <param name="dist">Best distance</param>
        public void findBestTranslation(DefinitionImage A, out int cols, out double bestDist)
        {
            double dist;
            
            bestDist = double.PositiveInfinity;
            cols = -1;

            Image translated = new Image(this);

            Metrics.ImageDistance id;

            //Go over the potential column translations
            int i, len = _width;
            for (i = 0; i < len; ++i)
            {
                //Store the translation in translated
				TranslateMatrix(i, ref matrix, ref translated.matrix);
                

                //dist = id.distance(Metrics.ImageDistance.DIRECTEDMODIFIEDHAUSDORFF_AB);
                //dist = id.distance(Metrics.ImageDistance.TANIMOTO) + id.distance(Metrics.ImageDistance.YULE);
                //dist = id.distance(Metrics.ImageDistance.VERTICAL);
                //dist = id.distance(Metrics.ImageDistance.DIRECTEDHAUSDORFF_AB);
                //dist = id.distance(Metrics.ImageDistance.OVERLAPPING_AB);
                //dist = id.distance(Metrics.ImageDistance.HORIZONTAL);
                //id = new Metrics.ImageDistance(translated, A.Polar);
                //dist = id.distance(Metrics.ImageDistance.TANIMOTO) * id.distance(Metrics.ImageDistance.YULE);
                id = new Metrics.ImageDistance(translated, A.PolarTransform);
                dist = id.distance(Metrics.ImageDistance.DIRECTEDMODIFIEDHAUSDORFF_AB);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    cols = i;
                }
            }            
        }

        /// <summary>
        /// Finds the angle in radians corresponding to the number of columns
        /// </summary>
        /// <param name="cols">The number of columns</param>
        /// <returns>Radian degrees to which it was rotated</returns>
        private double colsToAngle(double cols)
        {
            return (cols / _width) * 2 * Math.PI;
        }

        #endregion

		/// <summary>
		/// Re-implementation of function removed from Iridium. Gods know what it does...
		/// </summary>
		private void TranslateMatrix(int cols, ref Matrix m1, ref Matrix m2)
		{
			double[] column = new double[m1.RowCount];
			for (int i = 0; i < m1.ColumnCount; ++i)
			{
				column = getMatrixColumn(m1, i);
				setMatrixColumn(ref m2, (i + cols) % m1.ColumnCount, column);
			}
		}

		private double[] getMatrixColumn(MathNet.Numerics.LinearAlgebra.Matrix m, int idx)
		{
			double[] res = new double[m.RowCount];
			for (int i = 0; i < m.RowCount; ++i)
				res[i] = m[i, idx];
			return res;
		}

		private void setMatrixColumn(ref MathNet.Numerics.LinearAlgebra.Matrix m, int idx, double[] vals)
		{
			for(int i = 0; i < m.RowCount; ++i)
				m[i, idx] = vals[i];
		}

		private double[] getMatrixRow(MathNet.Numerics.LinearAlgebra.Matrix m, int idx)
		{
			double[] res = new double[m.ColumnCount];
			for (int i = 0; i < m.ColumnCount; ++i)
				res[i] = m[idx, i];
			return res;
		}

		#region CONVERSIONS

		private void PixelToPolar(Point center, float span, Point pixelPoint, out double r, out double theta)
        {
            int row, col;
            PixelToInk(center, span, pixelPoint, out row, out col);
            InkToPolar(center, span, col, row, out r, out theta);
        }

        private void InkToPolar(Point center, float span, int x, int y, out double r, out double theta)
        {
            float xDiff = (x - center.X) / span;
            float yDiff = (y - center.Y) / span;

            r = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
            theta = Math.Atan2(yDiff, xDiff);
            ScalePolar(ref r, ref theta);
        }

        private void ScalePolar(ref double r, ref double theta)
        {
            //ERROR if r = 1:
            //from [0, 1] to [0, _height]
            r *= (_height - 1.0);
            
            //ERROR if theta = pi:
            //from [-pi, pi] to [0, _width]
            theta = 0.5 * (theta + Math.PI) / Math.PI * (_width - 1.0);

           
        }

        protected override void GetCoordinates(Point center, float span, Point point, out int row, out int col)
        {
            double radius, theta;
            InkToPolar(center, span, (int)point.X, (int)point.Y, out radius, out theta);
            row = (int)radius;
            col = (int)theta;

            if (row > 31 || col > 31)
                return;
        }

        #endregion
    }
}
