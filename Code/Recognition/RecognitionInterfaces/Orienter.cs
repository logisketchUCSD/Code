using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RecognitionInterfaces
{
    public class Orienter : IOrienter, IRecognitionStep
    {

        public string ProgressString
        {
            get { return "Orienting"; }
        }

        private IOrienter _orienter;

        public Orienter(IOrienter orienter)
        {
            _orienter = orienter;
        }

        public virtual void process(Featurefy.FeatureSketch featureSketch)
        {
            foreach (Sketch.Shape shape in featureSketch.Sketch.Shapes)
                orient(shape, featureSketch);
        }

        public virtual void orient(Sketch.Shape shape, Featurefy.FeatureSketch featureSketch)
        {
            _orienter.orient(shape, featureSketch);
        }

    }
}
