using RecognitionInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Refiner
{
    /// <summary>
    /// Marks all already recognized shapes as recognized so that you don't change them.
    /// </summary>
    public class RecognizedMarker : IRecognitionStep
    {


        public string ProgressString
        {
            get { return "Marking shapes as recognized"; }
        }

        public void process(Featurefy.FeatureSketch featureSketch)
        {
            Sketch.Sketch sketch = featureSketch.Sketch;
            foreach (Sketch.Shape shape in sketch.Shapes)
            {
                if (shape.Type != new Domain.ShapeType())
                {
                    shape.AlreadyLabeled = true;
                    shape.AlreadyGrouped = true;
                }
            }
        }
    }
}
