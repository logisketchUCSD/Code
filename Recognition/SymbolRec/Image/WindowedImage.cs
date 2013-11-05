using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using Metrics;

namespace SymbolRec.Image
{
    /// <summary>
    /// Represents the various types of cropping metrics.
    /// </summary>
    public enum CropMethod
    {
        SQUARE,
        RECTANGLE,
        DISTANCE
    }

    /// <summary>
    /// Windows the neighborhood of a list of substrokes so that the context of the list of substrokes can be determined.
    /// </summary>
    public class WindowedImage : Image
    {
        #region Internals

        /// <summary>
        /// Threshold for cropping substrokes using the square and rectangle methods.
        /// </summary>
        private const double BB_THRESHOLD = 0.5;

        /// <summary>
        /// Threshold for cropping substrokes using the distance method.
        /// </summary>
        private const float DISTANCE_THRESHOLD = 200;

        /// <summary>
        /// The largest dimension of the bounding box.
        /// </summary>
        private float span;

        /// <summary>
        /// Width of all the substrokes in keysSub.
        /// </summary>
        private float keysWidth;

        /// <summary>
        /// Height of all the substrokes in keysSub.
        /// </summary>
        private float keysHeight;

        /// <summary>
        /// The substrokes that are being looked at.
        /// </summary>
        private Substrokes keysSub;

        /// <summary>
        /// The cropped neighboring substrokes.
        /// </summary>
        private Substrokes cropped;

		/*
        /// <summary>
        /// The definition image.
        /// </summary>
        private DefinitionImage di;
		*/

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for WindowedImage.
        /// </summary>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        public WindowedImage(int width, int height) : base(width, height)
        {
            this.cropped = new Substrokes();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a windowed image.
        /// </summary>
        /// <param name="keys">The substrokes that are being considered.</param>
        /// <param name="uncropped">The substrokes in the neighborhood of key (key is included in this list)</param>
        /// <param name="cropMethod">An enum that specifies the cropping metric.</param>
        public void CreateImage(List<Substroke> keys, List<Substroke> uncropped, CropMethod cropMethod)
        {
            // Creates a wrapper for the list of substrokes.
            keysSub = new Substrokes(keys);

            // Finds the maximum distance from the weighted center of the keys to the points on the substrokes of keys.
            float maxDist = float.NegativeInfinity;
            foreach (Substroke sub in keysSub.SubStrokes)
            {
                foreach (Point point in sub.PointsL)
                {
                    maxDist = Math.Max(maxDist, (float)(new PointDistance(keysSub.WeightedCenter, point).distance(PointDistance.EUCLIDIAN)));
                }
            }

            // Determines the width and height of keys
            this.keysWidth = keysSub.MaxX - keysSub.MinX;
            this.keysHeight = keysSub.MaxY - keysSub.MinY;

            // Find the appropriate span given the size of the bounding box or the distance threshold
            if (cropMethod == CropMethod.RECTANGLE || cropMethod == CropMethod.SQUARE)
            {
                this.span = Math.Max(2 * Math.Max((float)(keysWidth / 2 + keysWidth * BB_THRESHOLD),
                                       (float)(keysHeight / 2 + keysHeight * BB_THRESHOLD)) + 1.0f, 2 * maxDist + 1.0f);
            }
            else
            {
                this.span = Math.Max(Math.Max(keysWidth, keysHeight) + 2 * DISTANCE_THRESHOLD + 1.0f,
                                     2 * maxDist + 2 * DISTANCE_THRESHOLD + 1.0f);
            }

            this.cropped = this.CropSubstrokesDist(uncropped);

            //Trick it so it does not rotate
            //List<DefinitionImage> matches = new List<DefinitionImage>(0);
            //di = new DefinitionImage(this.iWidth, this.iHeight, this.cropped, matches);
            
            //this.AddPixels(cropped);
        }

        /// <summary>
        /// Crops the substrokes using a square bounding box.
        /// </summary>
        /// <param name="uncropped">The list of uncropped substrokes.</param>
        /// <returns>List of substrokes, including the keys, with only the points that fall
        /// within the bounding box</returns>
        private Substrokes CropSubstrokesSquare(List<Substroke> uncropped)
        {
            // Crops the neighbors of a substroke based on a square bounding box
                int k_minX = (int)(keysSub.WeightedCenter.X - (span - 1) / 2);
                int k_minY = (int)(keysSub.WeightedCenter.Y - (span - 1) / 2);
                int k_maxX = (int)(keysSub.WeightedCenter.X + (span - 1) / 2);
                int k_maxY = (int)(keysSub.WeightedCenter.Y + (span - 1) / 2);

                // Remove the points that fall outside the bounding box
                List<Substroke> cropped = new List<Substroke>();
                foreach (Substroke sub in uncropped)
                    cropped.Add(sub.Clone());

                // Crops the individual points of the substrokes.
                for (int i = 0; i < cropped.Count; i++)
                {
                    foreach (Point point in cropped[i].Points)
                    {
                        if (!(point.X > k_minX && point.X < k_maxX && point.Y > k_minY && point.Y < k_maxY))
                        {
                            cropped[i].PointsL.Remove(point);
                        }
                    }
                }

                return new Substrokes(cropped);
            
        }

        /// <summary>
        /// Windows the neighbors of a substroke using a rectangular window.
        /// </summary>
        /// <param name="uncropped">The current substroke's neighbors</param>
        /// <returns>List of substrokes, including the keys, with only the points that fall
        /// within the bounding box</returns>
        private Substrokes CropSubstrokesRect(List<Substroke> uncropped)
        {
            // Crops the neighbors of a substroke based on a rectangular bounding box
            int k_minX = (int)(keysSub.WeightedCenter.X - (keysWidth / 2 + keysWidth * BB_THRESHOLD));
            int k_minY = (int)(keysSub.WeightedCenter.Y - (keysHeight / 2 + keysHeight * BB_THRESHOLD));
            int k_maxX = (int)(keysSub.WeightedCenter.X + (keysWidth / 2 + keysWidth * BB_THRESHOLD));
            int k_maxY = (int)(keysSub.WeightedCenter.Y + (keysHeight / 2 + keysHeight * BB_THRESHOLD));

            // Remove the points that fall outside the bounding box
            List<Substroke> cropped = new List<Substroke>();
            foreach (Substroke sub in uncropped)
                cropped.Add(sub.Clone());

            // Crops the individual points of the substroke.
            for (int i = 0; i < cropped.Count; i++)
            {
                foreach (Point point in cropped[i].Points)
                {
                    if (!(point.X > k_minX && point.X < k_maxX && point.Y > k_minY && point.Y < k_maxY))
                    {
                        cropped[i].PointsL.Remove(point);
                    }
                }
            }

            return new Substrokes(cropped);
        }

        /// <summary>
        /// Crops the substrokes using a distance threshold.
        /// </summary>
        /// <param name="uncropped">The list of uncropped substrokes.</param>
        /// <returns>List of substrokes, including the keys, with only the points that fall
        /// within the bounding box</returns>
        private Substrokes CropSubstrokesDist(List<Substroke> uncropped)
        {
            // Crops the neighbors of a substroke based on a maximum distance
            // Remove the points that are greater than a set distance away from the substroke
            List<Substroke> cropped = new List<Substroke>();
            foreach (Substroke sub in uncropped)
                cropped.Add(sub.Clone());

            // List<Substroke> miniSubs = new List<Substroke>();

            // Create a list of all the points in the key substrokes
            List<Point> keyPoints = new List<Point>();
            foreach(Substroke key_sub in keysSub.SubStrokes)
            {
                foreach(Point pt in key_sub.Points)
                {
                    keyPoints.Add(pt);
                }
            }

            // Checks if the points in the substroke are close enough to the weighted center.  If they
            // are not, then crop them (the distance is large enough so that the keys will not be cropped.
            foreach (Substroke sub in cropped)
            {
                foreach (Point point in sub.Points)
                {
                    bool isNear = false;
                    foreach (Point keypoint in keyPoints)
                    {
                        PointDistance pd = new PointDistance(point, keypoint);
                        if (pd.distance(PointDistance.EUCLIDIAN) < (double)DISTANCE_THRESHOLD)
                        {
                            isNear = true;
                            break;
                        }
                    }
                    if (!isNear)
                        sub.PointsL.Remove(point);
                }
            }
            
            return new Substrokes(cropped);
        }

        #endregion

        #region Getters

        /// <summary>
        /// The cropped substrokes.
        /// </summary>
        public Substrokes Cropped
        {
            get
            {
                return this.cropped;
            }
        }

		/*
        /// <summary>
        /// The definition image.
        /// </summary>
        public DefinitionImage DI
        {
            get
            {
                return di;
            }
        }
		*/
 
        #endregion
    }
}
