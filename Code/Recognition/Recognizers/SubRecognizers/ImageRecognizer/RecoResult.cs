using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubRecognizer
{
    public enum ResultType
    {
        FUSION,
        POLAR,
        PARTIAL_HAUSDORFF,
        MOD_HAUSDORFF,
        TANIMOTO,
        YULE
    }

    [Serializable]
    public class RecoResult : ICloneable
    {
       /// <summary>
        /// Stores the information for this class. 
        /// All SymbolRank lists should always be sorted.
        /// </summary>
        private Dictionary<ResultType, List<SymbolRank>> _results;

        public RecoResult()
        {
            _results = new Dictionary<ResultType, List<SymbolRank>>();
            foreach (ResultType type in Enum.GetValues(typeof(ResultType)))
                _results.Add(type, new List<SymbolRank>());
        }

        public object Clone()
        {
            RecoResult result = (RecoResult)this.MemberwiseClone();
            result._results = new Dictionary<ResultType,List<SymbolRank>>();
            foreach (KeyValuePair<ResultType, List<SymbolRank>> pair in _results)
            {
                result._results.Add(pair.Key, new List<SymbolRank>());
                foreach (SymbolRank sr in pair.Value)
                    result._results[pair.Key].Add((SymbolRank)sr.Clone());
            }

            return result;
        }

        public void Add(ResultType type, SymbolRank result)
        {
            _results[type].Add(result);
        }

        internal void AddRange(ResultType type, IEnumerable<SymbolRank> results)
        {
            _results[type].AddRange(results);
        }

        public void AddAll(RecoResult otherResults)
        {
            foreach (ResultType type in Enum.GetValues(typeof(ResultType)))
            {
                _results[type].AddRange(otherResults._results[type]);
            }
        }

        public SymbolRank Best(ResultType type)
        {
            return SortedResults(type).First();
        }

        public SymbolRank Best(ShapeType shapetype, ResultType resulttype)
        {
            foreach (SymbolRank sr in SortedResults(resulttype))
            {
                if (sr.SymbolType == shapetype)
                    return sr;
            }
            return null;
        }

        public SymbolRank Worst(ResultType type)
        {
            return SortedResults(type).Last();
        }

        /// <summary>
        /// Normalizes the distance of an existing symbol rank in relation to the values of
        /// a particular type.
        /// </summary>
        /// <param name="type">The type to normalize in relation to.</param>
        /// <param name="SR">The SymbolRank to normalize.</param>
        /// <returns>A copy of the SymbolRank, except the distance has been normalized to the range [0,1].</returns>
        public double Normalize(ResultType type, SymbolRank SR)
        {

            double min = Best(type).Distance;
            double max = Worst(type).Distance;

            if (Math.Abs(max - min) < double.Epsilon)
                if (SR.Distance >= min && SR.Distance <= max)
                    return 1;
                else
                    return double.PositiveInfinity;

            return (SR.Distance - min) / (max - min);

        }

        public SymbolRank getSR(ResultType type, BitmapSymbol bs)
        {
            foreach (SymbolRank sr in SortedResults(type))
                if (sr.Symbol.ID == bs.ID)
                    return sr;

            throw new Exception("RecoResult could not find the specified BitmapSymbol in the list of " 
                + type.ToString() + " results.");
        }

        public List<SymbolRank> BestN(ResultType type, int topN)
        {
            topN = Math.Min(topN, _results[type].Count);
            return SortedResults(type).Take(topN).ToList();
        }

        public List<SymbolRank> SortedResults(ResultType type)
        {
            return (from result in _results[type]
                    orderby result.Distance ascending
                    select result).ToList();
        }
    }
}
