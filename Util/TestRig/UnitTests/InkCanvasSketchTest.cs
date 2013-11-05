using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities;

namespace UnitTests
{
    [TestClass]
    [DeploymentItem("settings.txt")]
    [DeploymentItem("Initialization Files\\FeatureListGroup.txt")]
    [DeploymentItem("Initialization Files\\FeatureListSingle.txt")]
    public class InkCanvasSketchTest
    {
        private InkToSketchWPF.InkCanvasSketch inkCanvasSketch;

        [TestInitialize()]
        public void Initialize()
        {
            inkCanvasSketch = newInkCanvasSketch();
        }

        [TestCleanup()]
        public void Cleanup()
        {
            inkCanvasSketch = null;
        }

        [TestMethod]
        public void TestSetUp()
        {
            Assert.IsNotNull(inkCanvasSketch, "inkcanvassketch should have been initialized");
            Assert.IsTrue(inkCanvasSketch.FeatureSketch.hasConsistentSubstrokes(), "on creation, data structures should be consistent");
            Assert.IsTrue(inkCanvasSketch.areDictionarySizesConsistent() == 0, "on creation, dictionaries should not contain anything");
        }

        [TestMethod]
        public void TestAddStroke()
        {
            Stroke firstStroke = newStroke();
            inkCanvasSketch.AddStroke(firstStroke);
            Assert.IsTrue(inkCanvasSketch.areDictionarySizesConsistent() == 1, "dictionary size should be 1 after adding 1 stroke");
            Assert.IsTrue(inkCanvasSketch.FeatureSketch.hasConsistentSubstrokes(), "after a stroke is added, data structures should be consistent");
            Assert.IsTrue(inkCanvasSketch.InkCanvas.Strokes.Contains(firstStroke), "after a stroke is added, the inkcanvas should contain it");
        }

        [TestMethod]
        public void TestDeleteStroke()
        {
            Stroke firstStroke = newStroke();
            inkCanvasSketch.AddStroke(firstStroke);
            inkCanvasSketch.DeleteStroke(firstStroke);
            Assert.IsTrue(inkCanvasSketch.areDictionarySizesConsistent() == 0, "dictionary size should be 0 after deleting the only stroke");
            Assert.IsTrue(inkCanvasSketch.FeatureSketch.hasConsistentSubstrokes(), "after the only stroke is deleted, data structures should be consistent");
            Assert.IsFalse(inkCanvasSketch.InkCanvas.Strokes.Contains(firstStroke), "after the only stroke is deleted, the inkcanvas shouldn't contain it");
        }

        [TestMethod]
        public void TestUpdateStrokeWithUnclassifiedStroke()
        {
            Stroke firstStroke = newStroke();
            inkCanvasSketch.AddStroke(firstStroke);
            inkCanvasSketch.UpdateInkStroke(firstStroke);
            Assert.IsTrue(inkCanvasSketch.Sketch.CheckConsistency(), "sketch should be consistent after updating a stroke");
            Assert.IsTrue(inkCanvasSketch.areDictionarySizesConsistent() == 1, "dictionary size should be 1 after updating the only stroke");
            Assert.IsTrue(inkCanvasSketch.FeatureSketch.hasConsistentSubstrokes(), "after updating a stroke, data structures should be consistent");
            Assert.IsTrue(inkCanvasSketch.InkCanvas.Strokes.Contains(firstStroke), "after updating a stroke, the inkcanvas shouldn't contain it");
        }

        [TestMethod]
        public void TestUpdateStrokeWithClassifiedStroke()
        {
            List<Sketch.Shape> modifiedShapes;
            // Add one stroke, classify it, and update it
            Stroke firstStroke = newStroke();
            inkCanvasSketch.AddStroke(firstStroke);
            Sketch.Substroke oldSubstroke = inkCanvasSketch.GetSketchSubstrokeByInk(firstStroke);
            inkCanvasSketch.Sketch.MakeNewShapeFromSubstrokes(out modifiedShapes, 
                Data.Utils.singleEntryList(inkCanvasSketch.GetSketchSubstrokeByInk(firstStroke)), Domain.LogicDomain.AND, 0);
            inkCanvasSketch.UpdateInkStroke(firstStroke);
            Sketch.Substroke newSubstroke = inkCanvasSketch.GetSketchSubstrokeByInk(firstStroke);

            Assert.IsTrue(inkCanvasSketch.Sketch.CheckConsistency(), "sketch should be consistent after updating a stroke");
            Assert.IsTrue(inkCanvasSketch.areDictionarySizesConsistent() == 1, "dictionary size should be 1 after updating the only stroke");
            Assert.IsTrue(inkCanvasSketch.FeatureSketch.hasConsistentSubstrokes(), "after updating a stroke, data structures should be consistent");
            Assert.IsTrue(inkCanvasSketch.InkCanvas.Strokes.Contains(firstStroke), "after updating a stroke, the inkcanvas should still contain it");
            Assert.AreEqual(inkCanvasSketch.GetSketchSubstrokeByInk(firstStroke).ParentShape.Type, Domain.LogicDomain.AND, "updating should preserve shapes");

        }

        [TestMethod]
        public void TestLoadWithString()
        {

        }
        #region Helpers

        public static InkToSketchWPF.InkCanvasSketch newInkCanvasSketch()
        {
            // Manually construct the necessary data members
            Sketch.Sketch sketch = new Sketch.Sketch();
            Featurefy.FeatureSketch featureSketch = UnitTests.FeaturefyOperations.newValidFeatureSketch(new Sketch.Project(sketch));
            InkCanvas inkcanvas = new InkCanvas();

            return new InkToSketchWPF.InkCanvasSketch(inkcanvas, featureSketch);
        }

        public static Stroke newStroke()
        {
            return newStroke(new Tuple<double, double>(0, 0), new Tuple<double, double>(0.1, 0.1));
        }
   
        public static Stroke newStroke(Tuple<double, double> start, Tuple<double, double> end)
        {
            StylusPointCollection points = new StylusPointCollection();
            for (double pointNo = 0; pointNo <= 100; pointNo++)
            {
                double pointX = start.Item1 + pointNo / 100 * end.Item1;
                double pointY = start.Item2 + pointNo / 100 * end.Item2;
                points.Add(new StylusPoint(pointX, pointY));
            }
            return new Stroke(points, new DrawingAttributes());
        }
        
        #endregion

    }
}
