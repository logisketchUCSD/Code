using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using MathNet.Numerics.LinearAlgebra;

namespace SymbolRec.Image
{
	/// <summary>
	/// Class that seems to cache distances
	/// </summary>
    public class TransformImage : Image
    {
        public TransformImage(int width, int height)
            : base(width, height, 0.0)
        {
        }

        public TransformImage(int width, int height, double fill)
            : base(width, height, fill)
        {
        }

        public TransformImage(int width, int height, Substrokes substrokes)
            : this(new Image(width, height, substrokes))
        {
        }

        public TransformImage(Image image) 
            : base(image.Width, image.Height, double.PositiveInfinity)
        {
            computeTransform(image);
        }

        private void computeTransform(Image A)
        {
            int row, col;
            for (row = 0; row < Height; ++row)
            {
                for (col = 0; col < Width; ++col)
                {
                    minDistance(col, row, A);
                }
            }
        }

        private double minDistance(int x, int y, Image A)
        {
            if (double.IsPositiveInfinity(matrix[y, x]))
            {
                matrix[y, x] = A.minDistance(x, y);
            }

            return matrix[y, x];
        }

        internal override double minDistance(int x, int y)
        {
            return matrix[y, x];
        }
    }
}
