using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Domain;
using Sketch;

namespace SubRecognizer
{
    public class ImageRecognitionResult : RecognitionInterfaces.RecognitionResult
    {
        private Dictionary<ShapeType, double> _alternateTypes;
        private string _templateName;
        private System.Drawing.Bitmap _templateBitmap;
        private double _partialHausdorff, _modifiedHausdorff, _yule, _tanimoto;

        public ImageRecognitionResult(
            ShapeType type,
            double partialHausdorff,
            double modifiedHausdorff,
            double yule,
            double tanimoto,
            double confidence,
            double orientation,
            Dictionary<ShapeType, double> alternateTypes,
            string templateName,
            System.Drawing.Bitmap templateBitmap)
            : base(type, confidence, orientation)
        {
            _partialHausdorff = partialHausdorff;
            _modifiedHausdorff = modifiedHausdorff;
            _yule = yule;
            _tanimoto = tanimoto;
            _alternateTypes = alternateTypes;
            _templateName = templateName;
            _templateBitmap = templateBitmap;
        }

        public override void ApplyToShape(Shape s)
        {
            base.ApplyToShape(s);
            s.AlternateTypes = _alternateTypes;
            s.TemplateName = _templateName;
            s.TemplateDrawing = _templateBitmap;
        }

        public double PartialHausdorff
        {
            get { return _partialHausdorff; }
        }

        public double ModifiedHausdorff
        {
            get { return _modifiedHausdorff; }
        }

        public double Yule
        {
            get { return _yule; }
        }

        public double Tanimoto
        {
            get { return _tanimoto; }
        }

        public Dictionary<ShapeType, double> AlternateTypes
        {
            get { return _alternateTypes; }
        }

        public string TemplateName
        {
            get { return _templateName; }
        }

        public System.Drawing.Bitmap TemplateBitmap
        {
            get { return _templateBitmap; }
        }

    }
}
