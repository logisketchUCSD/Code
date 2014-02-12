using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Runtime.Serialization;
using Sketch;
using Utilities;
using Utilities.Concurrency;

namespace Featurefy
{

    /// <summary>
    /// Pairwise feature sketch class
    /// </summary>
    [Serializable]
    class PairwiseFeatureSketch : ISerializable
    {
        #region Member Variables

        /// <summary>
        /// Sketch
        /// </summary>
        private Sketch.Sketch m_Sketch;

        /// <summary>
        /// All Strokes in the Pairwise Feature Sketch
        /// </summary>
        private List<Substroke> m_Strokes;

        /// <summary>
        /// List of Features between a given pair of strokes
        /// </summary>
        private List<Future<FeatureStrokePair>> m_AllFeaturePairs;

        /// <summary>
        /// Lookup for a stroke
        /// </summary>
        private Dictionary<Substroke, List<Future<FeatureStrokePair>>> m_Stroke2Pairs;

        private Dictionary<StrokePair, Future<FeatureStrokePair>> m_Pair2FeaturePair;

        private Dictionary<string, bool> m_FeaturesToUse;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="FeaturesToUse"></param>
        public PairwiseFeatureSketch(Sketch.Sketch sketch, Dictionary<string, bool> FeaturesToUse)
        {
            m_Sketch = sketch;
            m_FeaturesToUse = FeaturesToUse;
            m_Strokes = new List<Substroke>();
            m_Stroke2Pairs = new Dictionary<Substroke, List<Future<FeatureStrokePair>>>();
            m_Pair2FeaturePair = new Dictionary<StrokePair, Future<FeatureStrokePair>>();
            m_AllFeaturePairs = new List<Future<FeatureStrokePair>>();

            foreach (Substroke stroke in m_Sketch.Substrokes)
            {
                if (!m_Strokes.Contains(stroke))
                    m_Strokes.Add(stroke);

                List<Future<FeatureStrokePair>> pairs = new List<Future<FeatureStrokePair>>(m_Strokes.Count);
                foreach (Substroke s in m_Strokes)
                {
                    if (stroke != s)
                    {
                        StrokePair strokePair = new StrokePair(s, stroke);
                        Future<FeatureStrokePair> pair = createFeatureStrokePair(strokePair);
                        pairs.Add(pair);

                        if (!m_Pair2FeaturePair.ContainsKey(strokePair))
                            m_Pair2FeaturePair.Add(strokePair, pair);

                        m_AllFeaturePairs.Add(pair);

                        if (m_Stroke2Pairs.ContainsKey(s))
                            m_Stroke2Pairs[s].Add(pair);
                    }
                }

                if (!m_Stroke2Pairs.ContainsKey(stroke))
                    m_Stroke2Pairs.Add(stroke, pairs);
            } 
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds stroke to the pairwise feature sketch
        /// </summary>
        /// <param name="stroke"></param>
        public void AddStroke(Substroke stroke)
        {
            if (m_Strokes.Contains(stroke))
                return;

            if (m_Stroke2Pairs.ContainsKey(stroke))
                return;

            m_Stroke2Pairs.Add(stroke, new List<Future<FeatureStrokePair>>());

            foreach (Substroke s in m_Strokes)
            {
                StrokePair strokePair = new StrokePair(s, stroke);
                Future<FeatureStrokePair> pair = createFeatureStrokePair(strokePair);
                m_Stroke2Pairs[stroke].Add(pair);
                m_Pair2FeaturePair.Add(strokePair, pair);
                m_AllFeaturePairs.Add(pair);
                m_Stroke2Pairs[s].Add(pair);
            }

            m_Strokes.Add(stroke);
        }

        /// <summary>
        /// Removes the given stroke from the pairwise feature sketch
        /// </summary>
        /// <param name="stroke"></param>
        public void RemoveStroke(Substroke stroke)
        {
            if (m_Strokes.Contains(stroke))
                m_Strokes.Remove(stroke);

            if (m_Stroke2Pairs.ContainsKey(stroke))
            {
                foreach (Future<FeatureStrokePair> pair in m_Stroke2Pairs[stroke])
                {
                    m_AllFeaturePairs.Remove(pair);
                }

                m_Stroke2Pairs.Remove(stroke);
            }

            List<StrokePair> toRemove = new List<StrokePair>();
            foreach (KeyValuePair<StrokePair, Future<FeatureStrokePair>> pair in m_Pair2FeaturePair)
                if (pair.Key.Includes(stroke))
                    toRemove.Add(pair.Key);

            foreach (StrokePair pair in toRemove)
                if (m_Pair2FeaturePair.ContainsKey(pair))
                    m_Pair2FeaturePair.Remove(pair);
        }

        private void FindPairInfo(List<Substroke> strokes, int indexOfStroke)
        {
            if (indexOfStroke >= strokes.Count)
                return;

            Substroke stroke = strokes[indexOfStroke];
            lock (stroke)
            {
                int size = Math.Max(1, indexOfStroke - 1);
                List<Future<FeatureStrokePair>> pairs = new List<Future<FeatureStrokePair>>(size);
                for (int i = 0; i < indexOfStroke; i++)
                {
                    Substroke s = strokes[i];
                    lock (s)
                    {
                        try
                        {
                            StrokePair strokePair = new StrokePair(s, stroke);
                            Future<FeatureStrokePair> pair = createFeatureStrokePair(strokePair);
                            pairs.Add(pair);
                            
                            lock (m_Pair2FeaturePair)
                            {
                                if (!m_Pair2FeaturePair.ContainsKey(strokePair))
                                    m_Pair2FeaturePair.Add(strokePair, pair);
                            }
                            lock (m_AllFeaturePairs)
                            {
                                m_AllFeaturePairs.Add(pair);
                            }
                            lock (m_Stroke2Pairs)
                            {
                                if (m_Stroke2Pairs.ContainsKey(s))
                                    m_Stroke2Pairs[s].Add(pair);
                            }
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("PairwiseSketch FindInfo: " + exc.Message);
                            //throw exc;
                        }
                    }
                }

                if (!m_Strokes.Contains(stroke))
                    m_Strokes.Add(stroke);

                if (!m_Stroke2Pairs.ContainsKey(stroke))
                    m_Stroke2Pairs.Add(stroke, pairs);
            }
        }

        #endregion

        #region Getters

        private Future<FeatureStrokePair> createFeatureStrokePair(StrokePair strokePair)
        {
            return new Future<FeatureStrokePair>(delegate()
            {
                FeatureStrokePair pair = new FeatureStrokePair(strokePair, m_FeaturesToUse);
                return pair;
            });
        }

        /// <summary>
        /// Returns the minimum distance from the stroke
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        public double MinDistanceFromStroke(Substroke stroke)
        {
            double min = double.PositiveInfinity;
            if (m_Stroke2Pairs.ContainsKey(stroke))
            {
                List<Future<FeatureStrokePair>> pairs = m_Stroke2Pairs[stroke];
                foreach (Future<FeatureStrokePair> pair in pairs)
                    min = Math.Min(min, pair.Value.SubstrokeDistance.Min);
            }

            return min;
        }

        /// <summary>
        /// Returns the minumum distance between the given stroke and a stroke from the same class
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        public double MinDistanceFromStrokeSameClass(Substroke stroke)
        {
            double min = double.PositiveInfinity;
            if (m_Stroke2Pairs.ContainsKey(stroke))
            {
                List<Future<FeatureStrokePair>> pairs = m_Stroke2Pairs[stroke];
                foreach (Future<FeatureStrokePair> futurePair in pairs)
                {
                    FeatureStrokePair pair = futurePair.Value;
                    if (pair.Item1.Classification == pair.Item2.Classification)
                        min = Math.Min(min, pair.SubstrokeDistance.Min);
                }
            }

            return min;
        }

        /// <summary>
        /// Get the strokes in the sketch
        /// </summary>
        public List<Substroke> Strokes
        {
            get { return m_Strokes; }
        }

        /// <summary>
        /// Get the dictionary of substrokes and distances
        /// </summary>
        internal Dictionary<Substroke, List<SubstrokeDistance>> AllDistances
        {
            get 
            {
                Dictionary<Substroke, List<SubstrokeDistance>> distances = new Dictionary<Substroke, List<SubstrokeDistance>>();

                foreach (KeyValuePair<Substroke, List<Future<FeatureStrokePair>>> pair in m_Stroke2Pairs)
                {
                    List<SubstrokeDistance> d = new List<SubstrokeDistance>();

                    foreach (Future<FeatureStrokePair> val in pair.Value)
                        d.Add(val.Value.SubstrokeDistance);

                    distances.Add(pair.Key, d);
                }

                return distances;
            }
        }

        /// <summary>
        /// Get the list of feature pairs
        /// </summary>
        public List<FeatureStrokePair> AllFeaturePairs
        {
            get
            {
                // Extract all the values from the futures, and return a list of them. The expectation is that if you are
                // asking for AllFeaturePairs, you are probably going to use ALL the feature pairs.
                return m_AllFeaturePairs.ConvertAll<FeatureStrokePair>(delegate(Future<FeatureStrokePair> pair) { return pair.Value; });
            }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public PairwiseFeatureSketch(SerializationInfo info, StreamingContext context)
        {
            m_AllFeaturePairs = (List<Future<FeatureStrokePair>>)info.GetValue("AllPairs", typeof(List<FeatureStrokePair>));
            m_Sketch = (Sketch.Sketch)info.GetValue("Sketch", typeof(Sketch.Sketch));
            m_Stroke2Pairs = (Dictionary<Substroke, List<Future<FeatureStrokePair>>>)info.GetValue("Stroke2Pairs", typeof(Dictionary<Substroke, List<Future<FeatureStrokePair>>>));
            m_Pair2FeaturePair = (Dictionary<StrokePair, Future<FeatureStrokePair>>)info.GetValue("Pair2FeaturePair", typeof(Dictionary<StrokePair, Future<FeatureStrokePair>>));
            m_Strokes = (List<Substroke>)info.GetValue("Strokes", typeof(List<Substroke>));
        }

        /// <summary>
        /// Get the pairwise feature sketch info
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("AllPairs", m_AllFeaturePairs);
            info.AddValue("Sketch", m_Sketch);
            info.AddValue("Stroke2Pairs", m_Stroke2Pairs);
            info.AddValue("Pair2FeaturePair", m_Pair2FeaturePair);
            info.AddValue("Strokes", m_Strokes);
        }

        #endregion

        #region Wait for computation

        /// <summary>
        /// Compute everything
        /// </summary>
        internal void WaitForAll()
        {
            foreach (var future in m_AllFeaturePairs)
                future.WaitForValue();
            foreach (var futures in m_Stroke2Pairs.Values)
                Future.WaitForAll(futures);
            foreach (var future in m_Pair2FeaturePair.Values)
                future.WaitForValue();
        }

        #endregion

    }
}
