using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Runtime.Serialization;
using Sketch;
using Utilities.Concurrency;
using Data;

namespace Featurefy
{

    /// <summary>
    /// The Intersection Sketch class computes and stores features regarding intersections
    /// in a sketch.
    /// </summary>
    class IntersectionSketch
    {

        #region Constants

        private const string NUM_LL_INTERSECTIONS = "Number of 'LL' Intersections";
        private const string NUM_LX_INTERSECTIONS = "Number of 'LX' Intersections";
        private const string NUM_XL_INTERSECTIONS = "Number of 'XL' Intersections";
        private const string NUM_XX_INTERSECTIONS = "Number of 'XX' Intersections";

        #endregion

        #region Member Variables

        /// <summary>
        /// Prints debug statements iff debug == true
        /// </summary>
        private bool debug = false;

        /// <summary>
        /// The sketch that this instance tracks
        /// </summary>
        private Sketch.Sketch m_Sketch;

        /// <summary>
        /// All the substrokes in this sketch
        /// </summary>
        private List<Substroke> m_Strokes;

        /// <summary>
        /// Lookup table for a stroke intersections. This is effectively a 2D array, where
        /// each entry contains a Future (a value that can be computed asynchronously). Each 
        /// future computes an intersection pair for the two indexed strokes.
        /// </summary>
        private Dictionary<Substroke, Dictionary<Substroke, Future<IntersectionPair>>> m_Stroke2Intersections;

        /// <summary>
        /// Bounding boxes for each stroke, 
        /// stored so that they don't need to be recalculated
        /// </summary>
        private SmartCache<Substroke, System.Drawing.RectangleF> m_Boxes;

        /// <summary>
        /// Lines connecting points in each stroke, 
        /// stored so that they don't need to be recalculated
        /// </summary>
        private SmartCache<Substroke, List<Line>> m_Lines;

        /// <summary>
        /// This cache is managed by FeatureSketch. We just keep a reference to it.
        /// </summary>
        private SmartCache<Substroke, double> m_ExtensionLengthsExtreme;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor. Does not add any substrokes from the sketch; you must do that yourself after construction.
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="extensionLengthsExtreme"></param>
        public IntersectionSketch(Sketch.Sketch sketch, SmartCache<Substroke, double> extensionLengthsExtreme)
        {
            m_Sketch = sketch;

            double avgArcLength = GetAvgArcLength(sketch);

            m_Strokes = new List<Substroke>();
            m_Boxes = new SmartCache<Substroke, System.Drawing.RectangleF>(computeBoundingBox);
            m_Lines = new SmartCache<Substroke, List<Line>>(computeLines);

            m_ExtensionLengthsExtreme = extensionLengthsExtreme;

            m_Stroke2Intersections = new Dictionary<Substroke, Dictionary<Substroke, Future<IntersectionPair>>>();
        }

        private double GetAvgArcLength(Sketch.Sketch sketch)
        {
            if (sketch.SubstrokesL.Count == 0)
                return 0.0;

            double sum = 0.0;
            foreach (Substroke stroke in sketch.SubstrokesL)
                sum += stroke.SpatialLength;

            return sum / sketch.SubstrokesL.Count;
        }

        #endregion

        #region Computation Methods

        /// <summary>
        /// Compute everything
        /// </summary>
        internal void WaitForAll()
        {
            m_Boxes.ComputeAll(m_Strokes);
            m_Lines.ComputeAll(m_Strokes);
            foreach (var dictionary in m_Stroke2Intersections.Values)
                Future.WaitForAll(dictionary.Values);
        }

        private List<Line> computeLines(Substroke substroke)
        {
            List<Line> lines = Compute.getLines(substroke.PointsL);

            Line[] endLines = getEndLines(lines, m_ExtensionLengthsExtreme[substroke]);

            lines.Insert(0, endLines[0]);
            lines.Add(endLines[1]);

            return lines;
        }

        private System.Drawing.RectangleF computeBoundingBox(Substroke substroke)
        {
            return Compute.BoundingBox(m_Lines[substroke]);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the given stroke to the intersection sketch
        /// </summary>
        /// <param name="stroke"></param>
        public void AddStroke(Substroke stroke)
        {

            if (m_Strokes.Contains(stroke))
                return;

            if (m_Stroke2Intersections.ContainsKey(stroke))
                return;

            m_Stroke2Intersections.Add(stroke, new Dictionary<Substroke, Future<IntersectionPair>>());
            foreach (Substroke s in m_Strokes)
            {
                Future<IntersectionPair> pair = getPair(stroke, s);
                m_Stroke2Intersections[s].Add(stroke, pair);
                m_Stroke2Intersections[stroke].Add(s, pair);
            }

            m_Strokes.Add(stroke);

        }

        /// <summary>
        /// Removes the given stroke from the intersection sketch.
        /// 
        /// Precondition: the given stroke is already in this sketch.
        /// </summary>
        /// <param name="stroke"></param>
        public void RemoveStroke(Substroke stroke)
        {
            if (!m_Strokes.Contains(stroke))
                throw new ArgumentException("The stroke " + stroke + " is not in this IntersectionSketch!");

            m_Strokes.Remove(stroke);
            m_Stroke2Intersections.Remove(stroke);

            foreach (Substroke s in m_Strokes)
            {
                m_Stroke2Intersections[s].Remove(stroke);
            }
        }

        /// <summary>
        /// Updates the intersection sketch
        /// </summary>
        /// <param name="stroke"></param>
        public void UpdateIntersections(Substroke stroke)
        {
            RemoveStroke(stroke);
            AddStroke(stroke);
        }

        /// <summary>
        /// Gets the end lines of the intersection sketch
        /// </summary>
        /// <param name="a"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private static Line[] getEndLines(List<Line> a, double d)
        {
            Line[] lines = new Line[2];
            int limit = 10;

            // New Method Stuff
            System.Drawing.PointF lineStart = new System.Drawing.PointF();
            System.Drawing.PointF lineEnd = new System.Drawing.PointF();
            if (a.Count > 0)
            {
                lineStart = a[0].EndPoint1;
                lineEnd = a[a.Count - 1].EndPoint2;
            }
            // End New Method Stuff

            if (a.Count > limit)
            {
                lines[0] = new Line(a[limit].EndPoint1, a[0].EndPoint1, true);
                lines[1] = new Line(a[a.Count - 1 - limit].EndPoint1, a[a.Count - 1].EndPoint2, true);
            }
            else if (a.Count > 0)
            {
                lines[0] = new Line(a[a.Count - 1].EndPoint2, a[0].EndPoint1, true);
                lines[1] = new Line(a[0].EndPoint1, a[a.Count - 1].EndPoint2, true);
                //return lines;
            }
            else
            {
                lines[0] = new Line(new System.Drawing.PointF(0.0f, 0.0f), new System.Drawing.PointF(0.0f, 0.0f), true);
                lines[1] = new Line(new System.Drawing.PointF(0.0f, 0.0f), new System.Drawing.PointF(0.0f, 0.0f), true);
                return lines;
            }

            lines[0].extend(d);
            lines[1].extend(d);

            // New Method Stuff
            Line line1 = new Line(lines[0].EndPoint2, lineStart, true);
            Line line2 = new Line(lines[1].EndPoint2, lineEnd, true);

            return new Line[2] { line1, line2 };
            // End New Method Stuff

            //return lines;
        }

        #endregion

        #region Getters

        /// <summary>
        /// Get the IntersectionPair objects associated with a pair of substrokes
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        private Future<IntersectionPair> getPair(Substroke s1, Substroke s2)
        {
            // These are outside the delegate because (a) they are cached and therefore
            // usually very fast, and (b) they are not threadsafe.
            System.Drawing.RectangleF boundingBox1 = m_Boxes[s1];
            System.Drawing.RectangleF boundingBox2 = m_Boxes[s2];
            List<Line> lines1 = m_Lines[s1];
            List<Line> lines2 = m_Lines[s2];

            // Compute the intersectionpair on a background thread
            return new Future<IntersectionPair>(delegate()
            {
                return new IntersectionPair(s1, s2, boundingBox1, boundingBox2, lines1, lines2);
            });
        }

        /// <summary>
        /// Get the dictionary of intersections for the given stroke
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        public Dictionary<string, int> GetIntersectionCounts(Substroke stroke)
        {
            if (!m_Stroke2Intersections.ContainsKey(stroke))
                throw new ArgumentException("The given stroke is not tracked by this intersection sketch");

            double avgArcLength = GetAvgArcLength(m_Sketch);
            double dA = Math.Min(avgArcLength, (stroke.SpatialLength + avgArcLength) / 2) * Compute.THRESHOLD;

            Dictionary<Substroke, Future<IntersectionPair>> intersections = m_Stroke2Intersections[stroke];
            Dictionary<string, int> counts = new Dictionary<string, int>(4);
            counts.Add(NUM_LL_INTERSECTIONS, 0);
            counts.Add(NUM_LX_INTERSECTIONS, 0);
            counts.Add(NUM_XL_INTERSECTIONS, 0);
            counts.Add(NUM_XX_INTERSECTIONS, 0);

            foreach (Future<IntersectionPair> futurePair in intersections.Values)
            {
                IntersectionPair pair = futurePair.Value;

                double[] endDistances = new double[4];
                endDistances[0] = Compute.EuclideanDistance(pair.StrokeA.Points[0], pair.StrokeB.Points[0]);
                endDistances[1] = Compute.EuclideanDistance(pair.StrokeA.Points[0], pair.StrokeB.Points[pair.StrokeB.Points.Length - 1]);
                endDistances[2] = Compute.EuclideanDistance(pair.StrokeA.Points[pair.StrokeA.Points.Length - 1], pair.StrokeB.Points[0]);
                endDistances[3] = Compute.EuclideanDistance(pair.StrokeA.Points[pair.StrokeA.Points.Length - 1], pair.StrokeB.Points[pair.StrokeB.Points.Length - 1]);

                Substroke otherStroke = pair.StrokeA;
                if (stroke == otherStroke)
                    otherStroke = pair.StrokeB;

                double dB = Math.Min(avgArcLength, (otherStroke.SpatialLength + avgArcLength) / 2) * Compute.THRESHOLD;

                double d = Math.Min(dA, dB);

                if (pair.IsEmpty)
                {
                    foreach (double dist in endDistances)
                        if (dist < d)
                            counts[NUM_LL_INTERSECTIONS]++;

                    continue;
                }

                List<string> endIntersections = new List<string>();
                foreach (Intersection intersection in pair.Intersections)
                {
                    float a = intersection.GetIntersectionPoint(stroke);
                    float b = intersection.GetOtherStrokesIntersectionPoint(stroke);

                    if (a == -1.0f || b == -1.0f)
                        continue;
                    
                    bool aL = false;
                    bool bL = false;
                    bool aL1 = false;
                    bool aL2 = false;
                    bool bL1 = false;
                    bool bL2 = false;

                    double d_over_A = dA / stroke.SpatialLength;
                    double d_over_B = dB / otherStroke.SpatialLength;

                    if (a < -d_over_A || a > (1.0 + d_over_A) ||
                        b < -d_over_B || b > (1.0 + d_over_B))
                        continue;

                    if ((a <= d_over_A && a >= -d_over_A))
                        aL1 = true;
                    else if ((a >= (1.0 - d_over_A) && (a <= 1.0 + d_over_A)))
                        aL2 = true;
                    if (aL1 || aL2)
                        aL = true;

                    if ((b <= d_over_B && b >= -d_over_B))
                        bL1 = true;
                    else if ((b >= (1.0 - d_over_B) && (b <= 1.0 + d_over_B)))
                        bL2 = true;
                    if (bL1 || bL2)
                        bL = true;

                    string type;

                    if (aL && bL)
                    {
                        type = "LL";
                        counts[NUM_LL_INTERSECTIONS]++;
                        if (aL1 && bL1)
                            endIntersections.Add("a1b1");
                        else if (aL1 && bL2)
                            endIntersections.Add("a1b2");
                        else if (aL2 && bL1)
                            endIntersections.Add("a2b1");
                        else if (aL2 && bL2)
                            endIntersections.Add("a2b2");
                    }
                    else if (aL)
                    {
                        type = "LX";
                        counts[NUM_LX_INTERSECTIONS]++;
                    }
                    else if (bL)
                    {
                        type = "XL";
                        counts[NUM_XL_INTERSECTIONS]++;
                    }
                    else
                    {
                        type = "XX";
                        counts[NUM_XX_INTERSECTIONS]++;
                    }

                    if (debug)
                        Console.WriteLine("{6}: a = {7}, dA = {0}, aInt = {2}, d/a = {4}, b = {8}, dB = {1}, bInt = {3}, d/b = {5}",
                            dA.ToString("#0"), dB.ToString("#0"),
                            a.ToString("#0.00"), b.ToString("#0.00"),
                            d_over_A.ToString("#0.00"), d_over_B.ToString("#0.00"),
                            type, stroke.SpatialLength.ToString("#0"), otherStroke.SpatialLength.ToString("#0"));
                
                }

                if (endDistances[0] < d && !endIntersections.Contains("a1b1"))
                    counts[NUM_LL_INTERSECTIONS]++;
                if (endDistances[1] < d && !endIntersections.Contains("a1b2"))
                    counts[NUM_LL_INTERSECTIONS]++;
                if (endDistances[2] < d && !endIntersections.Contains("a2b1"))
                    counts[NUM_LL_INTERSECTIONS]++;
                if (endDistances[3] < d && !endIntersections.Contains("a2b2"))
                    counts[NUM_LL_INTERSECTIONS]++;

            }

            return counts;
        }

        /// <summary>
        /// Gets the strokes in the intersection sketch
        /// </summary>
        public List<Substroke> Strokes
        {
            get { return m_Strokes; }
        }

        #endregion

    }
}
