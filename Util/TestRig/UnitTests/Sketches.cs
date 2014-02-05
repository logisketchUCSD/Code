using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sketch;

namespace UnitTests
{
    [TestClass]
    public class Sketches
    {

        public static Sketch.Sketch newValidSketch()
        {
            Sketch.Sketch sketch = new Sketch.Sketch();

            sketch.AddShape(Shapes.newValidShape());
            sketch.AddShape(Shapes.newValidShape());
            sketch.AddShape(Shapes.newValidShape());

            sketch.CheckConsistency();
            return sketch;
        }

        [TestMethod]
        public void TestEqualsAndClone()
        {

            Sketch.Sketch sketch1 = newValidSketch();
            sketch1.connectShapes(sketch1.Shapes[0], sketch1.Shapes[1]);

            sketch1.CheckConsistency();

            Sketch.Sketch sketch2 = sketch1.Clone();

            sketch1.CheckConsistency();
            sketch2.CheckConsistency();

            Assert.IsTrue(sketch1.Equals(sketch2), "Original sketch reports it is not equal to the clone");
            Assert.IsTrue(sketch2.Equals(sketch1), "Cloned sketch reports it is not equal to the original");

            Assert.IsFalse(sketch1 == sketch2, "Cloned sketch is strictly equal to original");

        }

        [TestMethod]
        public void TestStandardMerge()
        {

            Shape shape1 = Shapes.newValidShape();
            Shape shape2 = Shapes.newValidShape();
            Shape shape3 = Shapes.newValidShape();

            Sketch.Sketch sketch = new Sketch.Sketch();

            sketch.CheckConsistency();

            sketch.AddShape(shape1);
            sketch.AddShape(shape2);
            sketch.AddShape(shape3);

            sketch.connectShapes(shape1, shape3);
            sketch.connectShapes(shape2, shape3);

            Assert.IsTrue(sketch.AreConnected(shape1, shape3), "Shape 1 is not connected to shape 3");
            Assert.IsTrue(sketch.AreConnected(shape2, shape3), "Shape 2 is not connected to shape 3");

            sketch.CheckConsistency();

            Assert.IsTrue(sketch.containsShape(shape1), "Sketch does not contain shape 1");
            Assert.IsTrue(sketch.containsShape(shape2), "Sketch does not contain shape 2");

            sketch.mergeShapes(shape1, shape2);

            Assert.IsTrue(sketch.AreConnected(shape1, shape3), "Shape 1 is not connected to shape 3 (after merge)");
            Assert.IsFalse(sketch.AreConnected(shape1, shape1), "Shape 1 is connected to itself (after merge)");
            Assert.IsFalse(sketch.AreConnected(shape1, shape2), "Shape 1 is connected to shape 2 (after merge)");

            Assert.IsTrue(sketch.containsShape(shape1), "Sketch does not contain shape 1 (after merge)");
            Assert.IsFalse(sketch.containsShape(shape2), "Sketch contains shape 2 (after merge)");

        }

    }
}
