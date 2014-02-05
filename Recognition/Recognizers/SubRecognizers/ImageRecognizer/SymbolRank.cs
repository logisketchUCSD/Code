using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace SubRecognizer
{
    [Serializable]
    [DebuggerDisplay("Name: {SymbolName}, Distance = {_dist}")]
    public class SymbolRank : ICloneable
    {
        private double _dist;
        private BitmapSymbol _symbol;
        private double _bestOrient;

        public SymbolRank()
        {
            _dist = double.MaxValue;
            _symbol = new BitmapSymbol();
            _bestOrient = 0.0;
        }

        public SymbolRank(double distance, BitmapSymbol symbol)
        {
            _dist = distance;
            _symbol = symbol;
            _bestOrient = 0.0;
        }

        public SymbolRank(double distance, BitmapSymbol symbol, double bestOrientation)
        {
            _dist = distance;
            _symbol = symbol;
            _bestOrient = bestOrientation;
        }

        public object Clone()
        {
            SymbolRank rank = (SymbolRank)this.MemberwiseClone();
            rank._symbol = (BitmapSymbol)this._symbol.Clone();

            return rank;
        }

        public double Distance
        {
            get { return _dist; }
            set { _dist = value; }
        }

        public string SymbolName
        {
            get { return _symbol.Name; }
        }

        public ShapeType SymbolType
        {
            get { return _symbol.SymbolType; }
        }

        public BitmapSymbol Symbol
        {
            get { return _symbol; }
            set { _symbol = value; }
        }

        public double BestOrientation
        {
            get { return _bestOrient; }
            set { _bestOrient = value; }
        }
    }
}
