using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    [DeploymentItem("Initialization Files\\FeatureListGroup.txt")]
    [DeploymentItem("Initialization Files\\FeatureListSingle.txt")]
    public class FeaturefyOperations
    {

        public static Featurefy.FeatureSketch newValidFeatureSketch(Sketch.Project project)
        {
            Dictionary<string, string> files = new Dictionary<string, string>();
            files["FeaturesSingle"] = "FeatureListSingle.txt";
            files["FeaturesGroup"] = "FeatureListGroup.txt";
            return Featurefy.FeatureSketch.MakeFeatureSketch(project, files);
        }

        [TestMethod]
        public void FeatureSketchCreationBehavior()
        {

            /*
             * Problem description:
             * When a sketch contains duplicated points, the creation of a FeatureSketch
             * may delete those points.
             */

            Sketch.Sketch sketch = Sketches.newValidSketch();

            // Duplicate a point in the sketch
            Sketch.Shape shape = sketch.Shapes[0];

            List<Sketch.Point> points = new List<Sketch.Point>(shape.Substrokes[0].Points);
            points.Add(points[points.Count - 1]);

            Sketch.Substroke substroke = new Sketch.Substroke(points);
            sketch.AddStroke(new Sketch.Stroke(substroke));
            shape.AddSubstroke(substroke);

            Sketch.Sketch clone = sketch.Clone();

            Assert.IsTrue(sketch.Equals(clone), "Sketch is not equal to its clone");

            Featurefy.FeatureSketch featureSketch = newValidFeatureSketch(new Sketch.Project(sketch));

            Assert.IsTrue(sketch.Equals(clone), "FeatureSketch creation modified original sketch");

        }

    }
}
