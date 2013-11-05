using System;
using System.Collections.Generic;
using Sketch;
using MathNet.Numerics.LinearAlgebra;
using Files;

namespace SymbolRec.Image
{
    /// <summary>
    /// Stores 4 types of images:
    /// 
    /// Stores the main image.
    /// Stores the polar image.
    /// Stores the main transform.
    /// Stores the polar transform.
    /// 
    /// Note that these may also be average images.
    /// </summary>
    public class DefinitionImage
    {
        #region INTERNALS

        /// <summary>
        /// Width of images
        /// </summary>
        private int m_width;

        /// <summary>
        /// Height of images
        /// </summary>
        private int m_height;

        private static List<DefinitionImage> m_matches = new List<DefinitionImage>();

        private static readonly string[] m_matchesNames = new string[] { "and", "nand", "nor", "not", "or" };

        public enum NodeType { DISTANCE, PIXEL };


        private bool DEBUG = false;

        /// <summary>
        /// Stores the original image / average of images
        /// </summary>
        private Image           m_image;

        /// <summary>
        /// Stores the polar image / average of polar images
        /// </summary>
        private PolarImage      m_polar;

        /// <summary>
        /// Stores the transform of the original image / average transform of images
        /// </summary>
        private TransformImage  m_imageTransform;

        /// <summary>
        /// Stores the transform of the polar image / average transform of polar images
        /// </summary>
        private TransformImage  m_polarTransform;

        private Image[] m_rotatedImages;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Constructor, default initializes images to 0.0
        /// </summary>
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        public DefinitionImage(int width, int height)
        {
            m_width             = width;
            m_height            = height;

            m_image             = new Image(m_width, m_height, 0.0);
            m_polar             = new PolarImage(m_width, m_width, 0.0);           
            m_imageTransform    = new TransformImage(m_width, m_height, 0.0);
            m_polarTransform    = new TransformImage(m_width, m_height, 0.0);
        }

        /// <summary>
        /// Constructor, no normalization
        /// </summary>
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        /// <param name="substrokes">Substrokes to create the images</param>
        public DefinitionImage(int width, int height, Substrokes substrokes)
            : this(width, height, substrokes, DefinitionImage.m_matches)
        {
        }

        public DefinitionImage(int width, int height, Substrokes substrokes, DefinitionImage matchTo)
            : this(width, height, substrokes, new List<DefinitionImage>(new DefinitionImage[] { matchTo } ))
        {
        }

        public DefinitionImage(int width, int height, Substrokes substrokes, List<DefinitionImage> matches)
        {
            /* Note: this constructor is a special case, which does not normalize the values.
            * This constructor is important, as it is called in addDefinition(Substroke[] substrokes).
            * If we made this constructor more generic, we would crash with a stack overflow.
            * 
            * ie. We cannot simple do this:
            * addDefinition(substrokes);
            */
            
            m_width = width;
            m_height = height;

            m_image = new Image(m_width, m_height, substrokes);
            m_polar = new PolarImage(m_width, m_height, substrokes);
            m_imageTransform = new TransformImage(m_image);
            m_polarTransform = new TransformImage(m_polar);

            int len = matches.Count;
            //Find the rotated image for each
            if (len > 0)
            {
                double[] angles;
                m_polar.findTranslations(matches, out angles);
                
                m_rotatedImages = new Image[len];
                
                int i;
                Substrokes temp;
                for (i = 0; i < len; ++i)
                {
                    temp = substrokes.rotateClone(angles[i]);
                    m_rotatedImages[i] = new Image(m_width, m_height, temp);
                }
            }
        }

        /// <summary>
        /// Constructor, normalization
        /// </summary>
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        /// <param name="Lsubstrokes">List of substrokes used to create average image</param>
        public DefinitionImage(int width, int height, List<Substrokes> Lsubstrokes)
            : this(width, height)
        {
            addDefinition(Lsubstrokes);
        }

        public DefinitionImage(int width, int height, string filename)
            : this(width, height)
        {
            addDefinition(filename);
        }

        public DefinitionImage(int width, int height, List<string> files)
            : this(width, height)
        {
            addDefinition(files);
        }

        #endregion

        #region ADD
        
        public static void AddMatch(DefinitionImage A)
        {
            m_matches.Add(A);
        }

        public static void ClearMatches()
        {
            m_matches.Clear();
        }

        /// <summary>
        /// Add the image created by substrokes into the current image, no normalize
        /// </summary>
        /// <param name="substrokes">Substrokes to add to current image</param>
        private void addDefinition(Substrokes substrokes)
        {            
            DefinitionImage di = new DefinitionImage(m_width, m_height, substrokes);
           
            m_image.matrix.Add(di.m_image.matrix);
            m_polar.matrix.Add(di.m_polar.matrix);
            m_imageTransform.matrix.Add(di.m_imageTransform.matrix);
            m_polarTransform.matrix.Add(di.m_polarTransform.matrix);

            //for single images
            m_rotatedImages = di.m_rotatedImages;

            //This is used to specify that all of the following images should match the first images orientation.
            //This is particularly useful when creating the average image ;)!
            if (m_matches.Count == 0)
                m_matches.Add(di);

            if (true)
            {
                di.Main.writeToBitmap("main" + counter.ToString() + ".bmp");
                di.Polar.writeToBitmap("polar" + counter.ToString() + ".bmp");
            }
            ++counter;

            /* Do not normalize here. 
             * We will do that after the whole list from addDefinition(List<>) has been added.
             */
        }

        private void addDefinition(Substrokes substrokes, string filename)
        {
            /*
            int index = indexOf(filename);
            if (filename != null && index > -1)
            {
                addDefinition(substrokes, DefinitionImage.m_matches[index]);
            }
            else*/
                addDefinition(substrokes);

            /* Do not normalize here. 
             * We will do that after the whole list from addDefinition(List<>) has been added.
             */
        }

        private void addDefinition(Substrokes substrokes, DefinitionImage matchTo)
        {
            DefinitionImage di = new DefinitionImage(m_width, m_height, substrokes, matchTo);

            m_image.matrix.Add(di.m_image.matrix);
            m_polar.matrix.Add(di.m_polar.matrix);
            m_imageTransform.matrix.Add(di.m_imageTransform.matrix);
            m_polarTransform.matrix.Add(di.m_polarTransform.matrix);

            if (true)
                di.Main.writeToBitmap("out" + counter.ToString() + ".bmp");
            ++counter;
        }

        /// <summary>
        /// Add the images created by substrokes into the current image and normalize
        /// </summary>
        /// <param name="Lsubstrokes">Substrokes to add to the current image</param>
        private void addDefinition(List<Substrokes> Lsubstrokes)
        {
            int i, len = Lsubstrokes.Count;
            for (i = 0; i < len; ++i)
                addDefinition(Lsubstrokes[i]);
            normalize();
        }

        /// <summary>
        /// Add the image created by the file into the current image, no normalize
        /// </summary>
        /// <param name="filename">File to add</param>
        private void addDefinition(string filename)
        {
            if (DEBUG)
                Console.WriteLine(filename);

            switch (FUtil.FileType(filename))
            {
				case Filetype.XML:
                    loadXML(filename);
                    break;

				case Filetype.JOURNAL:
                    loadJournal(filename);
                    break;

				case Filetype.MATRIX:
                    loadMatrix(filename);
                    break;

				case Filetype.OTHER:
                default:
                    throw new Exception("Bad filetype specified, " + filename);
            }
            /* Do not normalize here. 
             * We will do that after the whole list from addDefinition(List<>) has been added.
             */
        }

        /// <summary>
        /// Add the image created by the files into the current image and normalize
        /// </summary>
        /// <param name="files">Files to add</param>
        private void addDefinition(List<string> files)
        {
            int i, len = files.Count;
            for (i = 0; i < len; ++i)
                addDefinition(files[i]);
            normalize();
        }

        #endregion

        #region READ / WRITE

        #region READ

        /// <summary>
        /// Load in the image matrix specified. (Must be in the order: main, polar, main transform, polar transform)
        /// </summary>
        /// <param name="filename">An image matrix file</param>
        private void loadMatrix(string filename)
        {
            System.IO.TextReader tr = new System.IO.StreamReader(filename);

            Image image = new Image(m_width, m_height);

            //Get the image
            tr = image.LoadImageMatrix(tr);
            m_image.matrix.Add(image.matrix);

            try
            {
                //Get the polar
                tr = image.LoadImageMatrix(tr);
                m_polar.matrix.Add(image.matrix);

                //Get the image transform
                tr = image.LoadImageMatrix(tr);
                m_imageTransform.matrix.Add(image.matrix);

                //Get the polar transform
                tr = image.LoadImageMatrix(tr);
                m_polarTransform.matrix.Add(image.matrix);
            }
            catch
            {
                //Get the image transform from the original image
                m_imageTransform.matrix.Add((new TransformImage(image)).matrix);
            }

            tr.Close();
        }

        /// <summary>
        /// Load in the image matrix by getting the substrokes from the file
        /// </summary>
        /// <param name="filename">XML file to grab substrokes from</param>
        private void loadXML(string filename)
        {
            Substrokes substrokes = new Substrokes((new ConverterXML.ReadXML(filename)).Sketch.Substrokes);
            //addDefinition(substrokes, filename);
            addDefinition(substrokes);
        }

        /// <summary>
        /// Load in the image matrix by getting the substrokes from the file
        /// </summary>
        /// <param name="filename">JNT file to grab substrokes from</param>
        private void loadJournal(string filename)
        {
            Substrokes substrokes = new Substrokes((new ConverterJnt.ReadJnt(filename)).Sketch.Substrokes);
            //addDefinition(substrokes, filename);
            addDefinition(substrokes);
        }

        #endregion

        #region WRITE

        /// <summary>
        /// Write out an image matrix file (ext .imat or .amat)
        /// </summary>
        /// <param name="filename">File to write out to</param>
        public void writeToFile(string filename)
        {
            System.IO.TextWriter tw = new System.IO.StreamWriter(filename);
            writeToFile(tw).Close();
        }

        /// <summary>
        /// Write out an image matrix file
        /// </summary>
        /// <param name="tw"></param>
        /// <returns></returns>
        private System.IO.TextWriter writeToFile(System.IO.TextWriter tw)
        {
            //Write the main image
            tw = m_image.writeToFile(tw);

            //Write the polar image
            tw = m_polar.writeToFile(tw);

            //Write the main transform
            tw = m_imageTransform.writeToFile(tw);

            //Write the polar transform
            tw = m_polarTransform.writeToFile(tw);
            return tw;
        }

        /// <summary>
        /// Create bitmap representations of the images.
        /// Creates main, polar, main transform, and polar transform bitmaps.
        /// </summary>
        /// <param name="filestart">Start of the filename. ie (and, or, cool)</param>
        /// <param name="ext">File ext. ie (bmp)</param>
        public void writeToBitmap(string filestart, string ext)
        {
            //Write the main image
            m_image.writeToBitmap(filestart + ".main." + ext);

            //Write the polar image
            m_polar.writeToBitmap(filestart + ".polar." + ext);

            //Write the main transform
            m_imageTransform.writeToBitmap(filestart + ".main.transform." + ext);

            //Write the polar transform
            m_polarTransform.writeToBitmap(filestart + ".polar.transform." + ext);
        }

        #endregion

        #endregion

        #region GETTERS

        /// <summary>
        /// Get the main image
        /// </summary>
        public Image Main
        {
            get
            {
                return m_image;
            }
        }

        /// <summary>
        /// Get the polar image
        /// </summary>
        public PolarImage Polar
        {
            get
            {
                return m_polar;
            }
        }

        /// <summary>
        /// Get the main transform
        /// </summary>
        public TransformImage MainTransform
        {
            get
            {
                return m_imageTransform;
            }
        }

        /// <summary>
        /// Get the polar transform
        /// </summary>
        public TransformImage PolarTransform
        {
            get
            {
                return m_polarTransform;
            }
        }

        #endregion

        #region MISC

        /// <summary>
        /// Normalize all of the images by their maximum values.
        /// </summary>
        private void normalize()
        {
			normalize(m_image.matrix);
			normalize(m_polar.matrix);
			normalize(m_imageTransform.matrix);
			normalize(m_polarTransform.matrix);
        }

		/// <summary>
		/// Normalize a matrix by dividing through by its maximum value
		/// </summary>
		/// <param name="m">The matrix to normalize</param>
		private void normalize(Matrix m)
		{
			double max = 0.0;
			foreach (double[] row in m.CopyToJaggedArray())
			{
				foreach (double pt in row)
				{
					double ipt = Math.Abs(pt);
					if (ipt > max)
						max = ipt;
				}
			}
			m.Multiply(1.0 / max);
		}

        private int indexOf(string filename)
        {
            int one = filename.LastIndexOf('\\');
            int two = filename.LastIndexOf('/');
            int index = Math.Max(one, two);

            int i, len = DefinitionImage.m_matches.Count;
            for (i = 0; i < len; ++i)
                if (filename.Substring(index + 1, filename.Length - index - 1).StartsWith(DefinitionImage.m_matchesNames[i]))
                    return i;
            return -1;
        }

        private static int counter = 0;

        #endregion
    }
}