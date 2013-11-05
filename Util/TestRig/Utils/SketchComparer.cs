using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sketch;
using Domain;
using Data;

namespace TestRig.Utils
{

    /// <summary>
    /// Can be used to compare two sketches.
    /// </summary>
    class SketchComparer
    {

        #region Internals

        /// <summary>
        /// There are inconsistent rules for how wires should be grouped. As of this
        /// writing (7/12/11), connected wires MUST be part of the same shape. This
        /// rule is not honored by many of our older test files, however, and so it
        /// is often appropriate to just ignore wire grouping problems.
        /// </summary>
        private const bool IGNORE_WIRE_GROUPING_PROBLEMS = true;

        /// <summary>
        /// Some classifications (like "Unknown") can be found in older labeled sketches
        /// but are entirely irrelevant. Items in this list are compared against the
        /// lowercased classification, so everything in it should be all lower-case.
        /// </summary>
        private static readonly string[] IGNORE_CLASSIFICATIONS = new string[] { "unknown" };

        private Sketch.Sketch _original;
        private Sketch.Sketch _toCompare;
        private Lazy<double> _classificationQuality;
        private Lazy<double> _groupingQuality;
        private Lazy<List<Tuple<Shape, ShapeType>>> _recognitionMistakes;
        private Lazy<double> _recognitionQuality;
        private Lazy<double> _substrokeRecognitionQuality;
        private Lazy<ConfusionMatrix<string>> _classificationConfusionMatrix;
        private Lazy<ConfusionMatrix<ShapeType>> _recognitionConfusionMatrix;

        #endregion

        #region Constructor

        public SketchComparer(Sketch.Sketch original, Sketch.Sketch toCompare)
        {
            _original = original;
            _toCompare = toCompare;
            _recognitionConfusionMatrix = new Lazy<ConfusionMatrix<ShapeType>>(computeRecognitionConfusionMatrix);
            _classificationConfusionMatrix = new Lazy<ConfusionMatrix<string>>(computeClassificationConfusionMatrix);
            _classificationQuality = new Lazy<double>(computeClassificationQuality);
            _groupingQuality = new Lazy<double>(computeGroupingQuality);
            _recognitionMistakes = new Lazy<List<Tuple<Shape, ShapeType>>>(computeRecognitionMistakes);
            _recognitionQuality = new Lazy<double>(computeRecognitionQuality);
            _substrokeRecognitionQuality = new Lazy<double>(computeSubstrokeRecognitionQuality);
        }

        #endregion

        #region Computation

        private double computeClassificationQuality()
        {
            int totalStrokes = 0;
            int totalCorrect = 0;

            foreach (Substroke correctStroke in _original.Substrokes)
            {
                if (IGNORE_CLASSIFICATIONS.Contains(correctStroke.Classification.ToLower()))
                    continue;

                string originalClass = correctStroke.Classification;
                totalStrokes++;

                Substroke resultStroke = _toCompare.SubstrokesL.Find(delegate(Substroke s) { return s.GeometricEquals(correctStroke); });
                string resultClass = resultStroke.Classification;

                if (resultClass == originalClass)
                    totalCorrect++;
            }

            return (double)totalCorrect / totalStrokes;
        }

        private double computeGroupingQuality()
        {
            int totalGroups = 0;
            int correctGroups = 0;

            foreach (Sketch.Shape correctShape in _original.Shapes)
            {
                if (IGNORE_WIRE_GROUPING_PROBLEMS && correctShape.Classification == LogicDomain.WIRE_CLASS)
                    continue;

                ShapeType originalType = correctShape.Type;
                totalGroups++;

                if (_toCompare.ShapesL.Exists(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); }))
                {
                    correctGroups++;
                }
            }

            return (double)correctGroups / totalGroups;
        }

        private List<Tuple<Shape, ShapeType>> computeRecognitionMistakes()
        {
            List<Tuple<Shape, ShapeType>> result = new List<Tuple<Shape, ShapeType>>();

            foreach (Sketch.Shape correctShape in _original.Shapes)
            {

                ShapeType originalType = correctShape.Type;

                if (originalType.Equals(new ShapeType())) // skip unidentified shapes
                    continue;

                if (_toCompare.ShapesL.Exists(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); }))
                {
                    Sketch.Shape resultShape = _toCompare.ShapesL.Find(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); });
                    ShapeType resultType = resultShape.Type;

                    if (resultType != originalType)
                        result.Add(Tuple.Create(resultShape, originalType));

                }
            }

            return result;
        }

        private double computeRecognitionQuality()
        {
            int totalShapes = 0;
            int correctShapes = 0;

            foreach (Sketch.Shape correctShape in _original.Shapes)
            {

                ShapeType originalType = correctShape.Type;

                if (originalType.Equals(new ShapeType())) // skip unidentified shapes
                    continue;

                if (_toCompare.ShapesL.Exists(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); }))
                {
                    Sketch.Shape resultShape = _toCompare.ShapesL.Find(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); });
                    ShapeType resultType = resultShape.Type;

                    totalShapes++;

                    if (resultType == originalType)
                        correctShapes++;

                }
            }

            return (double)correctShapes / totalShapes;
        }

        private double computeSubstrokeRecognitionQuality()
        {
            int totalStrokes = 0;
            int totalCorrect = 0;

            foreach (Substroke correctStroke in _original.Substrokes)
            {
                if (IGNORE_CLASSIFICATIONS.Contains(correctStroke.Classification.ToLower()))
                    continue;

                ShapeType originalType = correctStroke.ParentShape.Type;
                totalStrokes++;

                Substroke resultStroke = _toCompare.SubstrokesL.Find(delegate(Substroke s) { return s.GeometricEquals(correctStroke); });
                ShapeType resultType = resultStroke.ParentShape.Type;

                if (originalType == resultType)
                    totalCorrect++;
            }

            return (double)totalCorrect / totalStrokes;
        }

        private ConfusionMatrix<ShapeType> computeRecognitionConfusionMatrix()
        {
            ConfusionMatrix<ShapeType> result = new ConfusionMatrix<ShapeType>();

            foreach (Sketch.Shape correctShape in _original.Shapes)
            {
                if (IGNORE_CLASSIFICATIONS.Contains(correctShape.Classification.ToLower()))
                    continue;

                ShapeType originalType = correctShape.Type;

                if (originalType.Equals(new ShapeType())) // skip unidentified shapes
                    continue;

                if (_toCompare.ShapesL.Exists(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); }))
                {
                    Sketch.Shape resultShape = _toCompare.ShapesL.Find(delegate(Sketch.Shape s) { return s.GeometricEquals(correctShape); });
                    ShapeType resultType = resultShape.Type;

                    // Record stats
                    result.increment(originalType, resultType);

                }
            }

            return result;
        }

        private ConfusionMatrix<string> computeClassificationConfusionMatrix()
        {
            ConfusionMatrix<string> result = new ConfusionMatrix<string>();

            foreach (Substroke correctStroke in _original.Substrokes)
            {
                if (IGNORE_CLASSIFICATIONS.Contains(correctStroke.Classification.ToLower()))
                    continue;

                string originalClass = correctStroke.Classification;

                Substroke resultStroke = _toCompare.SubstrokesL.Find(delegate(Substroke s) { return s.GeometricEquals(correctStroke); });
                string resultClass = resultStroke.Classification;

                // Record stats
                result.increment(originalClass, resultClass);
            }

            return result;
        }

        #endregion

        #region Accessors

        public ConfusionMatrix<string> ClassificationConfusion
        {
            get 
            { 
                return _classificationConfusionMatrix.Value; 
            }
        }

        public double ClassificationQuality
        {
            get
            {
                return _classificationQuality.Value;
            }
        }

        public double GroupingQuality
        {
            get
            {
                return _groupingQuality.Value;
            }
        }

        public ConfusionMatrix<ShapeType> RecognitionConfusion
        {
            get
            {
                return _recognitionConfusionMatrix.Value;
            }
        }

        public List<Tuple<Shape, ShapeType>> RecognitionMistakes
        {
            get
            {
                return _recognitionMistakes.Value;
            }
        }

        public double RecognitionQuality
        {
            get
            {
                return _recognitionQuality.Value;
            }
        }

        public double SubstrokeRecognitionQuality
        {
            get
            {
                return _substrokeRecognitionQuality.Value;
            }
        }

        #endregion

    }
}
