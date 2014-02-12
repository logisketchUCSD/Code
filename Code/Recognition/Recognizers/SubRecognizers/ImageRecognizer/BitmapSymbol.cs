using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Sketch;
using Utilities;
using MathNet.Numerics.LinearAlgebra;
using System.Diagnostics;
using System.Windows;

namespace SubRecognizer
{
    [Serializable]
    public class BitmapSymbol : ICloneable
    {
        #region Constants

        private const int NUM_TOP_POLAR_TO_KEEP = 10;
        private const int NUM_RECOGNITIONS_TO_RETURN = 5; // must be less than NUM_TOP_POLAR_TO_KEEP
        private double ALLOWED_ROTATION_AMOUNT = 360.0;

        internal const int GRID_X_SIZE = 24;
        internal const int GRID_Y_SIZE = 24;

        private const double POLAR_WEIGHT_DECAY_RATE = 0.1;
        private const double HAUSDORFF_QUANTILE = 0.94;
        private const double OVERLAP_THRESHOLD = 1.0 / 20.0;

        #endregion

        #region Member Variables

        private string _name;
        private ShapeType _type;
        private Guid _id;
        private System.Drawing.Bitmap _bitmap;

        private BitmapPoints _points;

        private List<Coord> _screenCoords;
        private Matrix _sMesh;
        private Matrix _sDTM;

        private List<Coord> _polarCoords;
        private Matrix _pMesh;
        private Matrix _pDTM;

        #endregion

        #region Constructors

        public BitmapSymbol()
        {
            _name = "";
            _type = new ShapeType();
            _id = Guid.NewGuid();
            _points = new BitmapPoints();
            _screenCoords = new List<Coord>();
            _sMesh = new Matrix(GRID_Y_SIZE, GRID_X_SIZE, 0.0);
            _sDTM = new Matrix(GRID_Y_SIZE, GRID_X_SIZE, double.PositiveInfinity);
            _polarCoords = new List<Coord>();
            _pMesh = new Matrix(GRID_Y_SIZE, GRID_X_SIZE, 0.0);
            _pDTM = new Matrix(GRID_Y_SIZE, GRID_X_SIZE, double.PositiveInfinity);
        }

        public BitmapSymbol(List<Substroke> strokes)
            : this()
        {
            foreach (Substroke sub in strokes)
                _points.AddStroke(sub);
            Process();
        }

        public BitmapSymbol(List<Substroke> strokes, ShapeType type, System.Drawing.Bitmap bitmap)
            : this(strokes)
        {
            _bitmap = bitmap;
            _type = type;
            Process();
        }

        public object Clone()
        {
            BitmapSymbol symbol = (BitmapSymbol)this.MemberwiseClone();
            //symbol._results = (RecoResult)this._results.Clone();
            symbol._sMesh = (Matrix)this._sMesh.Clone();
            symbol._sDTM = (Matrix)this._sDTM.Clone();
            symbol._pMesh = (Matrix)this._pMesh.Clone();
            symbol._pDTM = (Matrix)this._pDTM.Clone();
            symbol._points = (BitmapPoints)this._points.Clone();

            symbol._polarCoords = new List<Coord>();
            foreach (Coord cd in this._polarCoords)
                symbol._polarCoords.Add((Coord)cd.Clone());

            symbol._screenCoords = new List<Coord>();
            foreach (Coord cd in this._screenCoords)
                symbol._screenCoords.Add((Coord)cd.Clone());

            return symbol;
        }

        #endregion

        #region Calculations

        /// <summary>
        /// Calculates the dissimilarity between the calling BitmapSymbol and
        /// BitmapSymbol S in terms of their polar coordinates using the
        /// modified Hausdorff distance (aka the mean distance).
        /// </summary>
        private SymbolRank Polar_Mod_Hausdorff(BitmapSymbol S)
        {
            int lower_rot_index = (int)Math.Floor(ALLOWED_ROTATION_AMOUNT * GRID_X_SIZE / 720.0);
            int upper_rot_index = GRID_X_SIZE - lower_rot_index; //trick

            List<Coord> A = new List<Coord>(_polarCoords);
            List<Coord> B = new List<Coord>(S._polarCoords);

            if (A.Count == 0 || B.Count == 0) return new SymbolRank();

            double minA2B = double.PositiveInfinity;
            double distan = 0.0;
            int besti = 0; // the best translation in theta

#if JESSI
            Console.WriteLine("Your gate: ");
            _points.printPrettyMatrix(_pMesh, GRID_X_SIZE, GRID_Y_SIZE);
            //Console.Write(_pMesh.ToString());
            Console.WriteLine();

            Console.WriteLine("Your template: ");
            //Console.Write(S._pMesh.ToString());
            _points.printPrettyMatrix(S._pMesh, GRID_X_SIZE, GRID_Y_SIZE);
            Console.WriteLine();
#endif

            // rotations in screen coordinates are the same as translations in polar coords
            // we have polar coords, so translate in X (theta) until you get the best orientation
            for (int i = 0; i < GRID_X_SIZE; i++)
            {
                if (i > lower_rot_index && i < upper_rot_index)
                    continue;

                distan = 0.0;
                // find the distance from each point in A to the nearest point in B using B_DTM
                foreach (Coord a in A)
                {
                    int y_ind = (int)a.Y;
                    int x_ind = (int)a.X - i; // translate by i on the theta (X) axis
                    if (x_ind < 0) x_ind += GRID_X_SIZE; // make sure we're still on the graph

                    //putting less weight on points that have small rel dist - y 
                    double weight = Math.Pow((double)y_ind, POLAR_WEIGHT_DECAY_RATE);
                    distan += S._pDTM[y_ind, x_ind] * weight;
                }

                // this is the best orientation if the total distance is the smallest
                if (distan < minA2B)
                {
                    minA2B = distan;
                    besti = i;
                }
            }

            // set the best rotation angle (in radians)
            double bestRotAngle = besti * 2.0 * Math.PI / (double)GRID_X_SIZE;

            // we've already found the best orientation
            // find the distance from each point in B to the nearest point in A using A_DTM
            double minB2A = 0.0;
            foreach (Coord b in B)
            {
                int y_ind = (int)b.Y;
                int x_ind = (int)b.X - besti; // slide B back by besti
                if (x_ind < 0) x_ind += GRID_X_SIZE;
                double weight = Math.Pow((double)y_ind, POLAR_WEIGHT_DECAY_RATE);
                minB2A += _pDTM[y_ind, x_ind] * weight;
            }

            minA2B /= (double)A.Count;
            minB2A /= (double)B.Count;

#if JESSI
            Console.WriteLine("Finding best orientation match of your gate and " + S._name);
            Console.WriteLine("The best translation is " + besti + " which is " + bestRotAngle + " radians.");
            Console.WriteLine("A2B distance: " + minA2B + ", B2A distance: " + minB2A);
            //string templateName = S._name;
#endif

            return new SymbolRank(Math.Max(minA2B, minB2A), S, bestRotAngle);
        }

        /// <summary>
        /// Calculates the maximum Y value of a list of coordinates.
        /// </summary>
        /// <param name="points">The list of coordinates.</param>
        /// <returns>The largest Y value.</returns>
        private double Ymax(List<Coord> points)
        {
            double max = double.NegativeInfinity;

            foreach (Coord pt in points)
                max = Math.Max(max, pt.Y);

            return max;
        }

        /// <summary>
        /// Calculates the distance from every point in one BitmapSymbol to the closest point
        /// in another.
        /// </summary>
        private static List<double> directedScreenDistance(BitmapSymbol from, BitmapSymbol to)
        {
            List<double> dist = new List<double>();
            foreach (Coord pt in from._screenCoords)
                dist.Add(to._sDTM[(int)pt.Y, (int)pt.X]);
            return dist;
        }

        /// <summary>
        /// Calculates the Hausdorff distance between the calling BitmapSymbol and the 
        /// BitmapSymbol S.
        /// </summary>
        /// <returns>The maximum of the two partial Hausdorff distances.</returns>
        private SymbolRank Partial_Hausdorff(BitmapSymbol S)
        {
            List<double> distancesAB = directedScreenDistance(this, S);
            List<double> distancesBA = directedScreenDistance(S, this);

            distancesAB.Sort();
            distancesBA.Sort();

            double hAB = double.PositiveInfinity;
            double hBA = double.PositiveInfinity;

            if (distancesAB.Count != 0) hAB = distancesAB[(int)Math.Floor(((distancesAB.Count - 1) * HAUSDORFF_QUANTILE))];
            if (distancesBA.Count != 0) hBA = distancesBA[(int)Math.Floor(((distancesBA.Count - 1) * HAUSDORFF_QUANTILE))];

            return new SymbolRank(Math.Max(hAB, hBA), S);
        }

        /// <summary>
        /// Calculates the average directed distance between the calling BitmapSymbol
        /// and the BitmapSymbol S.
        /// </summary>
        /// <returns>The average distance between the two sets of points.</returns>
        private SymbolRank Modified_Hausdorff(BitmapSymbol S)
        {
            List<double> distancesAB = directedScreenDistance(this, S);
            List<double> distancesBA = directedScreenDistance(S, this);

            double AB = 0.0;
            double BA = 0.0;

            foreach (double dist in distancesAB)
                AB += dist;

            foreach (double dist in distancesBA)
                BA += dist;

            AB /= this._screenCoords.Count;
            BA /= S._screenCoords.Count;

            return new SymbolRank(Math.Max(AB, BA), S);
        }

        /// <summary>
        /// Calculates information about the number of black pixels and how they overlap.
        /// </summary>
        /// <param name="A">A BitmapSymbol</param>
        /// <param name="B">Another BitmapSymbol</param>
        /// <param name="A_count">The number of black pixels in A</param>
        /// <param name="B_count">The number of black pixels in B</param>
        /// <param name="black_overlap">The number of black pixels that "overlap" in the two</param>
        /// <param name="white_overlap">The number of white pixels in A that don't "overlap"
        ///     with a black pixel in B</param>
        private static void Black_White(BitmapSymbol A, BitmapSymbol B, 
                out int A_count, out int B_count, 
                out int black_overlap, out int white_overlap)
        {
            // we consider pixels to be overlapping if they are separated
            // by less than a certain fraction of the image's diagonal length
            double E = OVERLAP_THRESHOLD * Math.Sqrt(Math.Pow(GRID_X_SIZE, 2.0) + Math.Pow(GRID_Y_SIZE, 2.0));

            A_count = B_count = black_overlap = white_overlap = 0; // initialize them all to zero!

            for (int i = 0; i < A._sDTM.ColumnCount; i++)
            {
                for (int j = 0; j < A._sDTM.RowCount; j++)
                {
                    if (A._sDTM[i, j] == 0.0)
                        A_count++;

                    if (B._sDTM[i, j] == 0.0)
                        B_count++;

                    if ((A._sDTM[i, j] == 0.0 && B._sDTM[i, j] < E))
                        black_overlap++;

                    if (A._sDTM[i, j] > 0.0 && B._sDTM[i, j] > 0.0)
                        white_overlap++;
                }
            }


            // We need to do some sanity checking. Because pixels are considered to
            // overlap if they are separated by a certain distance, black_overlap
            // might be as large as max(A_count, B_count). However, we would not
            // expect it to be larger than min(A_count, B_count). The same goes
            // for the white overlap. NOTE: This particular fine point is not
            // mentioned in the paper as far as we can tell, but they do state that
            // the tanimoto coefficient is supposed to be in the range [0, 1], so
            // I feel justified in this.
            int area = GRID_X_SIZE * GRID_Y_SIZE;
            black_overlap = Math.Min(Math.Min(A_count, B_count), black_overlap);
            white_overlap = Math.Min(Math.Min(area - A_count, area - B_count), white_overlap);
        }

        /// <summary>
        /// Calculates the Tanimoto Similarity Coefficient for the calling BitmapSymbol
        /// and the BitmapSymbol S.
        /// </summary>
        private SymbolRank Tanimoto_Distance(BitmapSymbol S)
        {
            int A_count, B_count, black_overlap, white_overlap;

            Black_White(this, S, out A_count, out B_count, out black_overlap, out white_overlap);


            double Tanim = (double)black_overlap / (double)(A_count + B_count - black_overlap);
            double TanimComp = (double)white_overlap / (double)(A_count + B_count - 2 * black_overlap + white_overlap);
            double image_size = (double)_sDTM.ColumnCount * _sDTM.RowCount;
            double p = (double)(A_count + B_count) / (2.0 * image_size);
            // this has magic numbers. Sorry. We didn't make them up. See the image recognition paper, page 6. (Link at top)
            double alpha = 0.75 - 0.25 * p;
            double distance = 1.0 - (alpha * Tanim + (1.0 - alpha) * TanimComp);

            // return the Tanimoto Similarity Coefficient
            return new SymbolRank(distance, S);
        }

        /// <summary>
        /// Calculates the Yule Coefficient (or the coefficient of colligation) for the
        /// the calling BitmapSymbol and the BitmapSymbol S
        /// </summary>
        private SymbolRank Yule_Distance(BitmapSymbol S)
        {
            int A_count, B_count, black_overlap, white_overlap;

            Black_White(this, S, out A_count, out B_count, out black_overlap, out white_overlap);

            double lonely_A = A_count - black_overlap; // the number of black pixels in A that do not have a match in B
            double lonely_B = B_count - black_overlap;
            double overlapping = (double)(black_overlap * white_overlap);
            double numerator = overlapping - lonely_A * lonely_B;
            double denominator = overlapping + lonely_A * lonely_B;

            return new SymbolRank(numerator / denominator, S);
        }

        #endregion

        #region Getters & Setters

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public ShapeType SymbolType
        {
            get { return _type; }
        }

        public Guid ID
        {
            get { return _id; }
        }

        #endregion

        #region Processing

        private void Process()
        {
            _sMesh = _points.QuantizeScreen(GRID_X_SIZE, GRID_Y_SIZE);
            _screenCoords = IndexList(_sMesh);
            ScreenDistanceTransform();

            _pMesh = _points.QuantizePolar(GRID_X_SIZE, GRID_Y_SIZE);
            _polarCoords = IndexList(_pMesh);
            PolarDistanceTransform();
        }

        /// <summary>
        /// Calculates and saves the distance transform map for the screen coordinates.
        /// Uses _sMesh and saves to _sDTM.
        /// A distance transform map is a matrix in which every entry represents the
        /// distance from that point to the nearest point with ink.
        /// </summary>
        private void ScreenDistanceTransform()
        {
            List<Coord> indices = IndexList(_sMesh);

            _sDTM = new Matrix(GRID_Y_SIZE, GRID_X_SIZE, 0.0);

            for (int i = 0; i < GRID_Y_SIZE; i++)
                for (int j = 0; j < GRID_X_SIZE; j++)
                {
                    Coord cp = new Coord(j, i);
                    double mindist = double.PositiveInfinity;

                    foreach (Coord pt in indices)
                    {
                        double distan = cp.distanceTo(pt);
                        if (distan < mindist)
                            mindist = distan;
                    }
                    _sDTM[i, j] = mindist;
                }
        }

        /// <summary>
        /// Calculates and saves the distance transform map for the polar coordinates.
        /// Uses _pMesh and saves to _pDTM.
        /// A distance transform map is a matrix in which every entry represents the
        /// distance from that point to the nearest point with ink.
        /// </summary>
        private void PolarDistanceTransform()
        {
            List<Coord> indices = IndexList(_pMesh);

            for (int i = 0; i < GRID_Y_SIZE; i++)
                for (int j = 0; j < GRID_X_SIZE; j++)
                {
                    double mindist = double.PositiveInfinity;

                    foreach (Coord pt in indices)
                    {
                        // the straight distance between current point and current index
                        double dx = Math.Abs(j - pt.X);
                        double dy = i - pt.Y;
                        double dy2 = dy * dy;
                        double straightDist = Math.Sqrt(dx * dx + dy2);

                        dx = (double)GRID_X_SIZE - dx;
                        double wrapDist = Math.Sqrt(dx * dx + dy2);

                        double distan = Math.Min(straightDist, wrapDist);

                        if (distan < mindist)
                            mindist = distan;
                    }
                    _pDTM[i, j] = mindist;
                }

#if FALSE
            Console.WriteLine("Your polar distance transform map:");
            Console.Write(_pDTM.ToString())
#endif
        }

        /// <summary>
        /// Gets a list of coordinates from a general matrix.
        /// 
        /// Any point in the general matrix that has a value greater than
        /// zero will correspond to an entry in the list of coords.
        /// </summary>
        /// <param name="mesh">The GeneralMatrix to create the list from.</param>
        /// <returns>The list of Coords representing the matrix.</returns>
        private List<Coord> IndexList(Matrix mesh)
        {
            List<Coord> indices = new List<Coord>();
            
            // make a list of the coordinates of the black pixels
            for (int row = 0; row < GRID_Y_SIZE; row++)
                for (int col = 0; col < GRID_X_SIZE; col++)
                    if (mesh[row, col] > 0.0)
                        indices.Add(new Coord(col, row));

            return indices;
        }

        #endregion

        #region Other Functions

        private void Rotate(double theta)
        {
#if JESSI
            Console.WriteLine("Rotating your BitmapSymbol by " + theta + " radians.");
#endif
            _points.Rotate(theta);

            Process();
        }

        public System.Drawing.Bitmap toBitmap()
        {
            return _bitmap;
        }

        #endregion

        #region Recognition

        public RecoResult Recognize(List<BitmapSymbol> defns)
        {
            if (defns.Count == 0)
                throw new ArgumentException("You must provide a nonempty list of templates!");

            int numTopPolarToKeep = Math.Min(NUM_TOP_POLAR_TO_KEEP, defns.Count);
            int numRecognitionsToReturn = Math.Min(NUM_RECOGNITIONS_TO_RETURN, defns.Count);

            RecoResult polarResults = polarRecognition(defns);

#if JESSI
            Console.WriteLine("\nThese templates made it through the polar recognition round:");
            foreach (SymbolRank sr in polarResults.BestN(ResultType.POLAR, NUM_TOP_POLAR_TO_KEEP))
                Console.WriteLine(sr.SymbolName);
#endif

            List<SymbolRank> topPolar = polarResults.BestN(ResultType.POLAR, numTopPolarToKeep);
            RecoResult screenResults = screenRecognition(topPolar);
            RecoResult combinedResults = combineResults(topPolar, screenResults, numRecognitionsToReturn);

#if JESSI
            Console.WriteLine("Your templates have now been reordered by screen recognition:");
            foreach (SymbolRank sr in combinedResults.BestN(ResultType.FUSION, NUM_TOP_POLAR_TO_KEEP))
                Console.WriteLine(sr.SymbolName);
#endif

            combinedResults.AddAll(polarResults);
            combinedResults.AddAll(screenResults);
            return combinedResults;
        }

        private RecoResult polarRecognition(List<BitmapSymbol> defns)
        {
            RecoResult results = new RecoResult();
            foreach (BitmapSymbol bs in defns)
            {
                SymbolRank polar_result = Polar_Mod_Hausdorff(bs);
                results.Add(ResultType.POLAR, polar_result);
            }
            return results;
        }

        private RecoResult screenRecognition(List<SymbolRank> topPolar)
        {
            RecoResult screenResults = new RecoResult();
            foreach (SymbolRank sr in topPolar)
            {

#if JESSI
                Console.WriteLine();
                Console.WriteLine("Doing screen recognition for template " + sr.SymbolName);

#endif
                // clone the BitmapSymbol so that we can rotate it without losing information
                BitmapSymbol clone = (BitmapSymbol)Clone();
                clone.Rotate(-sr.BestOrientation);

                // calculate the data using the rotated clone, but store the output in this symbol's results.
                screenResults.Add(ResultType.PARTIAL_HAUSDORFF, clone.Partial_Hausdorff(sr.Symbol));
                screenResults.Add(ResultType.MOD_HAUSDORFF, clone.Modified_Hausdorff(sr.Symbol));
                screenResults.Add(ResultType.TANIMOTO, clone.Tanimoto_Distance(sr.Symbol));
                screenResults.Add(ResultType.YULE, clone.Yule_Distance(sr.Symbol));
            }
            return screenResults;
        }

        private RecoResult combineResults(List<SymbolRank> topPolar, RecoResult screenResults, int numToReturn)
        {
            List<SymbolRank> fusionResults = new List<SymbolRank>();
            foreach (SymbolRank sr in topPolar)
            {
                SymbolRank part_haus = screenResults.getSR(ResultType.PARTIAL_HAUSDORFF, sr.Symbol);
                double part_haus_distance = screenResults.Normalize(ResultType.PARTIAL_HAUSDORFF, part_haus);

                SymbolRank mod_haus = screenResults.getSR(ResultType.MOD_HAUSDORFF, sr.Symbol);
                double mod_haus_distance = screenResults.Normalize(ResultType.MOD_HAUSDORFF, mod_haus);

                SymbolRank tanim = screenResults.getSR(ResultType.TANIMOTO, sr.Symbol);
                double tanim_distance = screenResults.Normalize(ResultType.TANIMOTO, tanim);

                SymbolRank yule = screenResults.getSR(ResultType.YULE, sr.Symbol);
                double yule_distance = 1 - screenResults.Normalize(ResultType.YULE, yule);

                double distance = part_haus_distance + mod_haus_distance + tanim_distance + yule_distance;

                fusionResults.Add(new SymbolRank(distance, sr.Symbol, sr.BestOrientation));
            }

            // sort
            var sortedResults = from result in fusionResults
                                orderby result.Distance ascending
                                select result;

            RecoResult combinedResults = new RecoResult();
            combinedResults.AddRange(ResultType.FUSION, sortedResults.Take(numToReturn));
            return combinedResults;
        }

        /// <summary>
        /// uses polar coordinate matching to find the orientation of a BitmapSymbol
        /// that makes it best match the given BitmapSymbol template.
        /// </summary>
        /// <param name="defn"></param>
        /// <returns></returns>
        public double bestOrientation(BitmapSymbol defn)
        {
            if (defn == null) return 0.0;

#if JESSI
            Console.WriteLine("Using template " + defn._name + " to orient shape.");
#endif

            return Polar_Mod_Hausdorff(defn).BestOrientation;
        }

        #endregion
    }
}