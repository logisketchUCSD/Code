/*
 * File: Image.cs
 *
 * Author: Originally by dsmith, Modified heavily by James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007-2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 * 
 */

#define WEIGHTEDCENTER //Use the weighted center, comment out to use the normal center

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Sketch;
using Files;

namespace SymbolRec.Image
{
    /// <summary>
    /// This class stores the pixel based image
    /// </summary>
    public class Image
	{
		#region Internals

		/// <summary>
        /// Width of the image
        /// </summary>
        protected int _width;

        /// <summary>
        /// Height of the image
        /// </summary>
        protected int _height;       

        /// <summary>
        /// Stores the pixel of the image
        /// </summary>
        internal Matrix matrix;

		/// <summary>
		/// Transforms applied to this image
		/// </summary>
		private Matrix _transforms;

		/// <summary>
		/// Cached Hausdorff Distances
		/// </summary>
		private Matrix _cached_distances = null;

		/// <summary>
		/// Parameters for coordinate transforms
		/// </summary>
		private Point _weighted_center;
		private float span;

		/// <summary>
		/// Default parameters
		/// </summary>
		private static int DEFAULT_WIDTH = 64;
		private static int DEFAULT_HEIGHT = 64;
		private static double DEFAULT_FILL = 0.0;

		/// <summary>
		/// What is the value at which a pixel is considered to be 'on' (this lets us apply low-pass filters to our data)
		/// </summary>
		public static double ON_THRESHOLD = 0.5;

		public static double OFF_THRESHOLD = 0.4;

		#endregion

		#region Constructors

		public Image(Image image)
        {
			_height = image.Height;
			_width = image.Width;
			matrix = image.matrix.Clone();
            if (image._cached_distances != null)
            {
                _cached_distances = image._cached_distances.Clone();
            }
            else
            {
                _cached_distances = null;
            }
        }

        /// <summary>
        /// Default constructor. Creates a 32x32 pixel image
        /// </summary>
        public Image() 
            : this(DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_FILL) 
        { 
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        public Image(int width, int height) 
            : this(width, height, DEFAULT_FILL)
        {
        }
        
		/// <summary>
		/// Creates an image from a group of substrokes
		/// </summary>
		/// <param name="width">The width of the image in pixels</param>
		/// <param name="height">The height of the image in pixels</param>
		/// <param name="substrokes">The group of substrokes to be rasterized</param>
        public Image(int width, int height, Substrokes substrokes)
            : this(width, height, DEFAULT_FILL)
        {
            LoadImage(substrokes);
        }

		/// <summary>
		/// Constructs an Image from a jagged array of pixels
		/// </summary>
		/// <param name="pixels">The image</param>
        public Image(bool[][] pixels)
        {
			_height = pixels.Length;
			_width = pixels[0].Length;
			matrix = new Matrix(_height, _width);
			for (int row = 0; row < _height; ++row)
			{
				for (int col = 0; col < _width; ++col)
				{
					matrix[row, col] = Convert.ToDouble(pixels[row][col]);
				}
			}
		}

        
		/// <summary>
		/// Constructs an image from a jagged array of pixels
		/// </summary>
		/// <param name="pixels">The source image</param>
        public Image(double[][] pixels)
        {
			_height = pixels.Length;
			_width = pixels[0].Length;
			matrix = new Matrix(_height, _width);
			for (int row = 0; row < _height; ++row)
			{
				for (int col = 0; col < _width; ++col)
				{
					matrix[row, col] = pixels[row][col];
				}
			}
        }

		/// <summary>
		/// Constructs an image from a Matrix
		/// </summary>
		/// <param name="pixels">The source image</param>
		public Image(Matrix pixels)
		{
			_height = pixels.RowCount;
			_width = pixels.ColumnCount;
			matrix = pixels.Clone();
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        /// <param name="fill">Default value for each pixel</param>
        public Image(int width, int height, double fill)
        {
			_width = width;
			_height = height;
			matrix = new Matrix(height, width, fill);
		}

        /// <summary>
        /// Create a new Image from a shape
        /// </summary>
        /// <param name="width">Width of the generated image</param>
        /// <param name="height">Height of the generated image</param>
        /// <param name="s">The shape</param>
        public Image(int width, int height, Shape s)
            : this(width, height, new Substrokes(s.SubstrokesL))
        {
            // Nothing to do here
        }

		#endregion

		#region File handling

		private void LoadImage(Substrokes substrokes)
        {
            AddPixels(substrokes);
        }
       
		/// <summary>
		/// Loads an image from a file
		/// </summary>
		/// <param name="filename">The source filename</param>
        public void LoadImage(string filename)
        {
			if (!System.IO.File.Exists(filename))
				throw new System.IO.FileNotFoundException("Could not access file {0} to load sketch", filename);

            switch(FUtil.FileType(filename))
            {
                case Filetype.XML:
                    LoadImageSketch(filename);
                    break;
				case Filetype.JOURNAL:
                    LoadImageJournal(filename);
                    break;
				case Filetype.MATRIX:
                    LoadImageMatrix(filename).Close();
                    break;
				case Filetype.OTHER:
                default:
                    throw new Exception("Bad filetype specified, " + filename);
            }
        }

        private void LoadImageJournal(string filename)
        {
            LoadImage(new Substrokes((new ConverterJnt.ReadJnt(filename)).Sketch.Substrokes));
        }

        private void LoadImageSketch(string filename)
        {
            LoadImage(new Substrokes((new ConverterXML.ReadXML(filename)).Sketch.Substrokes));
        }

        protected System.IO.TextReader LoadImageMatrix(string filename)
        {
            System.IO.TextReader tr = new System.IO.StreamReader(filename);
            return LoadImageMatrix(tr);
        }          

        internal virtual System.IO.TextReader LoadImageMatrix(System.IO.TextReader tr)
        {
            string[] line;
            line = tr.ReadLine().Split();
            if (Convert.ToInt32(line[0]) != _width || Convert.ToInt32(line[1]) != _height)
            {
                Console.Error.WriteLine("Could not load file, height or width does not match");
                return tr;
            }

            //Get the image matrix
            int r, c;
            string l;
            for (r = 0; r < _height; ++r)
            {
                l = tr.ReadLine();
                if (l == null)
                    break;
                line = l.Split();

                for (c = 0; c < _width; ++c)
                {
                    matrix[r, c] = Convert.ToDouble(line[c]);
                }
            }
			_cached_distances = null;
            return tr;
		}

		/// <summary>
		/// Write out this Image to a file
		/// </summary>
		/// <param name="filename">The file to write to</param>
		/// <returns>A TextWriter</returns>
		public System.IO.TextWriter writeToFile(string filename)
		{
			System.IO.TextWriter tw = new System.IO.StreamWriter(filename);
			tw = writeToFile(tw);
			return tw;
		}

		/// <summary>
		/// Write this image out to a file using the given TextWriter
		/// </summary>
		/// <param name="tw"></param>
		/// <returns></returns>
		public virtual System.IO.TextWriter writeToFile(System.IO.TextWriter tw)
		{
			//width height
			tw.WriteLine(_width.ToString() + " " + _height.ToString());

			//write image matrix
			int row, col;
			for (row = 0; row < _height; ++row)
			{
				for (col = 0; col < _width; ++col)
				{
					tw.Write(this.matrix[row, col] + " ");
				}
				tw.WriteLine();
			}

			return tw;
		}

		/// <summary>
		/// Write this Image to a file as a PNG
		/// </summary>
		/// <param name="filename">The file to write to</param>
		public virtual void writeToBitmap(string filename)
		{
			System.Drawing.Bitmap bitmap = getThisAsBitmap();

			bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
		}

		#endregion

		#region Misc

		public void FillInLine(int row1, int col1, int row2, int col2)
        {
            float slope = (float)(row1 - row2) / (col1 - col2);

            int r, c;
            int minCol = col1 < col2 ? col1 : col2;
            int maxCol = col1 > col2 ? col1 : col2;
            int minRow = row1 < row2 ? row1 : row2;
            int maxRow = row1 > row2 ? row1 : row2;

            //If there is greater variation in the cols
            if (maxCol - minCol > maxRow - minRow)
            {
                for (c = minCol + 1; c < maxCol; ++c)
                {
                    // y = m(x - x1) + y1
                    r = (int)(slope * (c - col1) + row1);
                    this.matrix[r, c] = 1.0;
                }
            }
            //If there is greater variation in the rows
            else
            {
                for (r = minRow + 1; r < maxRow; ++r)
                {
                    // x = (1/m)(y - y1) + x1
                    c = (int)((1.0f / slope) * (r - row1) + col1);
                    this.matrix[r, c] = 1.0;
                }
            }
        }


		/// <summary>
		/// Recalculate the calculated Hausdorff distances
		/// </summary>
		private void calculate_cached_distances()
		{
			_cached_distances = new Matrix(_height, _width);
			for (int y = 0; y < _height; ++y)
			{
				for (int x = 0; x < _width; ++x)
				{
					double shortest = Double.PositiveInfinity;
					for (int yB = 0; yB < _height; ++yB)
					{
						for (int xB = 0; xB < _width; ++xB)
						{
							if (matrix[yB, xB] > ON_THRESHOLD)
							{
								// Calculate the distance
								double d = Math.Sqrt(Math.Pow(x - xB, 2) + Math.Pow(y - yB, 2));
								if (d < shortest)
									shortest = d;
							}
						}
					}
					_cached_distances[y, x] = shortest;
				}
			}
		}

		/// <summary>
		/// Get the strokespace Point that corresponds to a given pixel
		/// </summary>
		/// <param name="x">The X-coordinate of the pixel in this image</param>
		/// <param name="y">The Y-coordinate of the pixel in this image</param>
		/// <returns>A point in strokespace approximately where that pixel was</returns>
		/// <remarks>Takes into account any transforms applied to this image</remarks>
		public Point PixelToPoint(int px, int py)
		{
			// Undo transforms
			Matrix v = new Matrix(3, 1, 0);
			v[0, 0] = px;
			v[1, 0] = py;
			Matrix t;
			if (_transforms != null)
				t = _transforms.Inverse();
			else
				t = Matrix.Identity(3, 3);
			v = t * v;
			double npx = v[0, 0];
			double npy = v[1, 0];
			// Then transform back to strokespace
			float x = (float)(((npx / _width) - 0.5f) * span + _weighted_center.X);
			float y = (float)(((npy / _height) - 0.5f) * span + _weighted_center.Y);
			// And return the slightly fuzzified point
			return new Point(x, y, (float)matrix[py, px]);
		}

		/// <summary>
		/// Adds a group of substrokes to the image
		/// </summary>
		/// <param name="substrokes">The substrokes to add</param>
        protected virtual void AddPixels(Substrokes substrokes)
        {
            #if WEIGHTEDCENTER
            float max = substrokes.MaxToWeightedCenter;
            Point center = substrokes.WeightedCenter;
            #else
            float max = substrokes.MaxToCenter;
            Point center = substrokes.Center;
            #endif

			_weighted_center = center;
            
            span = 4 * max + 1.0f;

			int row = -1, col = -1, rowprev = -1, colprev = -1;
            Point[] points;
            //Point center = substrokes.Center;
            for (int i = 0; i < substrokes.Length; ++i)
            {
				points = substrokes[i].Points;
                for (int j = 0; j < points.Length; ++j)
                {
                    //override GetCoordinates in derived children
                    //to get different coordinate system, such as 
                    //polar, as in PolarImage
                    GetCoordinates(center, span, points[j], out row, out col);
                    this.matrix[row, col] = 1.0;             

                    //If this pixel and previous pixel do not touch, add a straight line
                    //If the pixels are too far, dont connect them
                    if ((Math.Abs(col - colprev) > 1 || Math.Abs(row - rowprev) > 1) && j > 0 
                        && Math.Abs(col - colprev) < _width / 2.0 && Math.Abs(row - rowprev) < _height / 2.0)
                    {
                        FillInLine(row, col, rowprev, colprev);
                    }
                    
                    rowprev = row;
                    colprev = col;
                }
            }
			_cached_distances = null;
        }

        protected virtual void GetCoordinates(Point center, float span, Point point, out int row, out int col)
        {
            InkToPixel(center, span, point, out row, out col);
        }

        protected void InkToPixel(Point center, float span, Point inkPoint, out int row, out int col)
        {
            float xDiff = (inkPoint.X - center.X) / span;
            float yDiff = (inkPoint.Y - center.Y) / span;

            //Needs to be square... this will not work if WIDTH != HEIGHT
			if (_width != _height)
				throw new Exception("Image must be square to correctly calculate InkToPixel");
            col = (int)((xDiff + 0.5f) * _width); //Cannot used 0.5f other wise col / row is capable of being out of bounds
            row = (int)((yDiff + 0.5f) * _height); //Can use 0.5f if span = 2*max + 1 in AddPixels (not tested)   
        }

        /// <summary>
        /// Inverse of InkToPixel
        /// </summary>
        /// <param name="center"></param>
        /// <param name="span"></param>
        /// <param name="pixelPoint"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        protected void PixelToInk(Point center, float span, Point pixelPoint, out int row, out int col)
        {
            col = (int)((pixelPoint.X / _width - 0.5f) * span + center.X);
            row = (int)((pixelPoint.Y / _height - 0.5f) * span + center.Y);
		}

		/// <summary>
		/// Get a string representation of this Image
		/// </summary>
		/// <returns></returns>
        public override string ToString()
        {
			return matrix.ToString();
        }

		/// <summary>
		/// Deep-copy this image using the copy constructor
		/// </summary>
		/// <returns>The copy of this image</returns>
		public Image Clone()
		{
			return new Image(this);
		}

		/// <summary>
		/// Convert this image to a bitmap
		/// </summary>
		/// <returns></returns>
        public System.Drawing.Bitmap getThisAsBitmap()
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(_width, _height);

            int row, col;
            int colorR, colorG, colorB;
            for (row = 0; row < _height; ++row)
            {
                for (col = 0; col < _width; ++col)
                {
                    if (double.IsNaN(this.matrix[row, col]))
                        colorR = 0;
                    else
                        colorR = (int)(Math.Sqrt(this.matrix[row, col]) * 255);
                    colorG = 45;// 255 - (int)(this.matrix[row, col] * 255);
                    colorB = 35;// 255 - (int)(this.matrix[row, col] * 255);

					if (colorR > 255)
						colorR = 255;
					if (colorB > 255)
						colorB = 255;
					if (colorG > 255)
						colorG = 255;

                    bitmap.SetPixel(col, row, System.Drawing.Color.FromArgb(colorR, colorG, colorB));
                }
            }
            return bitmap;
        }

        public System.Drawing.Bitmap getThisAsBitmapExpanded()
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(_width, _height);

            int row, col;
            int colorR, colorG, colorB;
            for (row = 0; row < _height; ++row)
            {
                for (col = 0; col < _width; ++col)
                {
                    colorG = 45;// 255 - (int)(this.matrix[row, col] * 255);
                    colorB = 35;// 255 - (int)(this.matrix[row, col] * 255);
                    if (double.IsNaN(this.matrix[row, col]))
                    {
                        colorR = 0;
                        bitmap.SetPixel(col, row, System.Drawing.Color.FromArgb(colorR, colorG, colorB));
                    }
                    else
                    {
                        colorR = (int)(Math.Sqrt(this.matrix[row, col]) * 255);
                        for (int k = -1; k <= 1; k++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                if (col + k < 0 || col + k >= _width || row + j < 0 || row + j >= _height)
                                    continue;
                                bitmap.SetPixel(col + k, row + j, System.Drawing.Color.FromArgb(colorR, colorG, colorB));
                            }
                        }
                    }


                }
            }
            return bitmap;
        }

        /// <summary>
        /// Calculate the minimum distance between this imag and a given point, using
		/// cached values
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>The euclidian distance away from neariest point</returns>
        internal virtual double minDistance(int x, int y)
        {
			if (_cached_distances == null)
				calculate_cached_distances();
			return _cached_distances[y, x];
		}

		#endregion

		#region Transformations

		/// <summary>
		/// Apply an arbitrary 3x3 transformation matrix
		/// </summary>
		/// <param name="transform">The matrix to transform by</param>
		/// <returns>The new image</returns>
		public Image ApplyTransform(Matrix transform)
		{
			if (transform.ColumnCount != 3 || transform.RowCount != 3)
				throw new ArgumentException("Matrix must be a 3x3");

			if (_transforms == null)
				_transforms = Matrix.Identity(3, 3);
			else
				_transforms = transform * _transforms;

			Matrix newM = new Matrix(_height, _width);
			Matrix pvector = new Matrix(3, 1, 1);
			Matrix nvector = new Matrix(3, 1, 1);
			for (int row = 0; row < _height; ++row)
			{
				for (int col = 0; col < _height; ++col)
				{
					// If the pixel isn't white
					if (matrix[row, col] != 0.0)
					{
						/* Vector
						 * [ x ]
						 * [ y ]
						 * [ 1 ]
						 */
						pvector[0, 0] = col;
						pvector[1, 0] = row;
						nvector = transform * pvector;
						int x = (int)nvector[0, 0];
						int y = (int)nvector[1, 0];
						// Don't do anything else if this transform took us outside of our image space
						if (x > (_width - 1) || y > (_height - 1) || x < 0 || y < 0)
							continue;
						newM[y, x] += matrix[row, col];
					}
				}
			}
			return new Image(newM);
		}

		/// <summary>
		/// First transforms the Image so that its (0,0) is the bounding-box center, then applies the transform, then transforms back to normal coordinates.
		/// </summary>
		/// <param name="transform">The transform to apply</param>
		/// <returns>The new image</returns>
		public Image ApplyTransformAtCenter(Matrix transform)
		{
			if (transform.ColumnCount != 3 || transform.RowCount != 3)
				throw new ArgumentException("Matrix must be a 3x3");
			return ApplyTransform( WeightedCenterTransformMatrix * transform * WeightedCenterInverseTransformMatrix);
		}

		/// <summary>
		/// Rotates this image by some angle theta and returns the result
		/// </summary>
		/// <param name="theta">The angle (in radians) by which to rotate</param>
		/// <param name="x_center">The x center coordinate</param>
		/// <param name="y_center">The y center coordinate</param>
		/// <returns>The rotated image</returns>
		public Image rotate(double theta, double x_center, double y_center)
		{
			Matrix rotationWarp = Matrix.Identity(3, 3);
			rotationWarp[0, 0] = Math.Cos(theta);
			rotationWarp[0, 1] = -Math.Sin(theta);
			rotationWarp[0, 2] = x_center - x_center * Math.Cos(theta) + y_center * Math.Sin(theta);
			rotationWarp[1, 0] = Math.Sin(theta);
			rotationWarp[1, 1] = Math.Cos(theta);
			rotationWarp[1, 2] = y_center - y_center * Math.Cos(theta) - x_center * Math.Sin(theta);
			return ApplyTransform(rotationWarp);
		}
	
		#endregion

		#region Accessors

		/// <summary>
		/// Height of this image, in pixels
		/// </summary>
		public int Height
		{
			get
			{
				return _height;
			}
		}

		/// <summary>
		/// Width of this image, in pixels
		/// </summary>
		public int Width
		{
			get
			{
				return _width;
			}
		}

		/// <summary>
		/// Returns position vectors for the non-white points in this image (i.e., those with
		/// a value of >0.0). The vectors are of the form [x, y].
		/// </summary>
		public IEnumerable<int[]> BlackPoints
		{
			get
			{
				for(int row = 0; row < _height; ++row)
				{
					for (int col = 0; col < _height; ++col)
					{
						if (matrix[row, col] > 0)
							yield return new int[] { col, row };
					}
				}
			}
		}
		
		/// <summary>
		/// Returns a position vector for the weighted center of this image.
		/// The position vector is in the form [ x , y ]
		/// </summary>
		public double[] WeightedCenter
		{
			get
			{
				double centroid_x = 0.0;
				double centroid_y = 0.0;
				int count = 0;
				foreach (int[] bp in BlackPoints)
				{
					centroid_x += bp[0];
					centroid_y += bp[1];
					++count;
				}
				if (count == 0)
					return new double[] { _width / 2.0, _height / 2.0 };
				return new double[] { centroid_x / count, centroid_y / count };
			}
		}

		/// <summary>
		/// Returns a position vector for the non-weighted center of this image
		/// The position vector is in the form [ x , y ]
		/// </summary>
		public double[] Center
		{
			get
			{
				return new double[] { _width / 2.0, _height / 2.0 };
			}
		}

		/// <summary>
		/// Returns the 3x3 matrix that will transform points to have center of (0,0)
		/// </summary>
		internal Matrix CenterTransformMatrix
		{
			get
			{
				Matrix m = Matrix.Identity(3, 3);
				m[0, 2] = Center[0];
				m[1, 2] = Center[1];
				return m;
			}
		}

		/// <summary>
		/// Returns the 3x3 matrix that will transform points to normal coordinates
		/// </summary>
		internal Matrix CenterInverseTransformMatrix
		{
			get
			{
				Matrix m = Matrix.Identity(3, 3);
				m[0, 2] = -Center[0];
				m[1, 2] = -Center[1];
				return m;
			}
		}

		/// <summary>
		/// Returns the 3x3 matrix that will transform points to have weighted center of (0,0)
		/// </summary>
		internal Matrix WeightedCenterTransformMatrix
		{
			get
			{
				Matrix m = Matrix.Identity(3, 3);
				m[0, 2] = WeightedCenter[0];
				m[1, 2] = WeightedCenter[1];
				return m;
			}
		}

		/// <summary>
		/// Returns the 3x3 matrix that will transform points to normal coordinates
		/// </summary>
		internal Matrix WeightedCenterInverseTransformMatrix
		{
			get
			{
				Matrix m = Matrix.Identity(3, 3);
				m[0, 2] = -WeightedCenter[0];
				m[1, 2] = -WeightedCenter[1];
				return m;
			}
		}

		/// <summary>
		/// Gets a jagged array of doubles from this image
		/// </summary>
		public double[][] DArray
		{
			get
			{
				double[][] darray = new double[_height][];
				for (int row = 0; row < matrix.RowCount; ++row)
				{
					double[] r = new double[_width];
					for (int col = 0; col < matrix.ColumnCount; ++col)
						r[col] = matrix[row, col];
					darray[row] = r;
				}
				return darray;
			}
		}

		/// <summary>
		/// Gets a jagged array of bools from this image. A pixel is on (True) if it's not white
		/// </summary>
		public bool[][] BArray
		{
			get
			{
				bool[][] barray = new bool[_height][];
				for (int row = 0; row < matrix.RowCount; ++row)
				{
					bool[] r = new bool[_width];
					for (int col = 0; col < matrix.ColumnCount; ++col)
					{
						r[col] = (matrix[row, col] > 0);
					}
					barray[row] = r;
				}
				return barray;
			}
		}

		/// <summary>
		/// Returns a row of the image
		/// </summary>
		/// <param name="idx">The index of the row to return</param>
		/// <returns>A double-array of a row of the matrix</returns>
		/// <seealso cref="DArray"/>
		public double[] Row(int idx)
		{
			double[] r = new double[matrix.ColumnCount];
			for (int col = 0; col < matrix.ColumnCount; ++col)
				r[col] = matrix[idx, col];
			return r;
		}

		/// <summary>
		/// Returns a bool row of the image
		/// </summary>
		/// <param name="idx">The index of the row to return</param>
		/// <returns>A bool-array of a row of the image</returns>
		/// <seealso cref="BArray"/>
		public bool[] BRow(int idx)
		{
			bool[] r = new bool[matrix.ColumnCount];
			for (int col = 0; col < matrix.ColumnCount; ++col)
				r[col] = (matrix[idx, col] > 0.0);
			return r;
		}

		/// <summary>
		/// Returns a column of the image
		/// </summary>
		/// <param name="row">The index of the column to return</param>
		/// <returns>A double-array of a column of the image</returns>
		public double[] Col(int col)
		{
			double[] c = new double[matrix.RowCount];
			for (int row = 0; row < matrix.RowCount; ++row)
				c[row] = matrix[row, col];
			return c;
		}

		/// <summary>
		/// Returns a bool column of the image
		/// </summary>
		/// <param name="row">The index of the column to return</param>
		/// <returns>A bool-array of a column of the image</returns>
		public bool[] BCol(int col)
		{
			bool[] c = new bool[matrix.RowCount];
			for (int row = 0; row < matrix.RowCount; ++row)
				c[row] = (matrix[row, col] > 0.0);
			return c;
		}

		/// <summary>
		/// Iterate through the points in the image
		/// </summary>
		public IEnumerable<double> Points
		{
			get
			{
				for (int row = 0; row < matrix.RowCount; ++row)
				{
					for (int col = 0; col < matrix.ColumnCount; ++col)
					{
						yield return matrix[row, col];
					}
				}
			}
		}

		/// <summary>
		/// Iterate through the bool points in the image
		/// </summary>
		public IEnumerable<bool> BPoints
		{
			get
			{
				for (int row = 0; row < matrix.RowCount; ++row)
				{
					for (int col = 0; col < matrix.ColumnCount; ++col)
					{
						yield return (matrix[row, col] > 0.0);
					}
				}
			}
		}

		/// <summary>
		/// Returns a matrix representation of the image
		/// </summary>
		public Matrix MPoints
		{
			get
			{
				return matrix.Clone();
			}
		}

		/// <summary>
		/// Accessor for points in the image
		/// </summary>
		/// <param name="x">The X-coordinate</param>
		/// <param name="y">The Y-coordiante</param>
		public double this[int x, int y]
		{
			get
			{
				return matrix[y, x];
			}
			set
			{
				matrix[y, x] = Convert.ToDouble(value);
				ClearCache();
			}
		}
		#endregion

		/// <summary>
		/// Reset the cached distances
		/// </summary>
		public void ClearCache()
		{
			_cached_distances = null;
		}
	}
}
