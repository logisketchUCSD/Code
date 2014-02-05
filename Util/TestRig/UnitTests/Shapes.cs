using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sketch;

namespace UnitTests
{
    [TestClass]
    public class Shapes
    {

        private static ulong time = 0;
        private static Random random = new Random();

        public static Point newValidPoint()
        {
            Point p = new Point((int)(random.NextDouble() * 100), (int)(random.NextDouble() * 100), 127, ++time);
            return p;
        }

        /// <summary>
        /// The default stroke constructor doesn't initialize all necessary fields.
        /// This method creates a substroke with the required fields filled in.
        /// </summary>
        /// <returns></returns>
        public static Substroke newValidSubstroke()
        {

            Substroke s = new Substroke(new Point[] {
                newValidPoint(),
                newValidPoint(),
                newValidPoint(),
                newValidPoint(),
                newValidPoint()
            });

            s.Name = "substroke_" + (++time);
            s.Classification = "Unknown";

            return s;
        }

        /// <summary>
        /// Creates a new shape with a nonempty list of substrokes.
        /// </summary>
        /// <returns></returns>
        public static Shape newValidShape()
        {
            Substroke s1 = Shapes.newValidSubstroke();
            Substroke s2 = Shapes.newValidSubstroke();
            Substroke s3 = Shapes.newValidSubstroke();
            Shape shape = new Shape(new Substroke[] { s1, s2, s3 });
            shape.Name = "shape_" + (++time);
            return shape;
        }

        [TestMethod]
        public void TestShapeSubstrokeOperations()
        {
            Shape shape = new Shape();
            shape.Type = Domain.LogicDomain.AND;

            Assert.AreEqual(0, shape.SubstrokesL.Count, "New shapes should have 0 substrokes");

            for (int i = 1; i <= 10; i++)
            {
                shape.AddSubstroke(newValidSubstroke());
                Assert.AreEqual(i, shape.SubstrokesL.Count, "Added a substroke; count should be " + i);
            }
        }

        [TestMethod]
        public void TestShapeEquals()
        {

            Shape s1 = new Shape();
            s1.AddSubstroke(newValidSubstroke());
            s1.AddSubstroke(newValidSubstroke());
            s1.AddSubstroke(newValidSubstroke());
            s1.Type = Domain.LogicDomain.AND;

            Shape s2 = s1.Clone();

            Assert.IsTrue(s1.Equals(s2), "Original shape reports it is not equal (.Equals) to the clone");
            Assert.IsTrue(s2.Equals(s1), "Cloned shape reports it is not equal (.Equals) to the original");

            Assert.IsTrue(s1.GeometricEquals(s2), "Original shape reports it is not equal (.GeometricEquals) to the clone");
            Assert.IsTrue(s2.GeometricEquals(s1), "Cloned shape reports it is not equal (.GeometricEquals) to the original");

            Assert.IsFalse(s1 == s2, "Original shape reports it is strictly equal (==) to the clone");
            Assert.IsFalse(s2 == s1, "Cloned shape reports it is strictly equal (==) to the original");

            s2.Type = Domain.LogicDomain.OR;

            Assert.IsFalse(s1.Equals(s2), "Shape equality should depend on type");
            Assert.IsFalse(s2.Equals(s1), "Shape equality should depend on type");

            Assert.IsTrue(s1.GeometricEquals(s2), "Geometric shape equality should not depend on type");
            Assert.IsTrue(s2.GeometricEquals(s1), "Geometric shape equality should not depend on type");

            Assert.IsFalse(s1.Id.Equals(s2.Id), "Two shapes should not have the same ID, even if they are clones");

        }

        [TestMethod]
        public void TestShapesInDictionaries()
        {

            Shape shape = new Shape();
            shape.Type = Domain.LogicDomain.AND;

            // ======================= GetHashCode / Equals =========================

            shape.AddSubstroke(newValidSubstroke());
            shape.AddSubstroke(newValidSubstroke());
            shape.AddSubstroke(newValidSubstroke());

            int hash1 = shape.GetHashCode();
            Shape test1 = shape.Clone();

            shape.AddSubstroke(newValidSubstroke());

            int hash2 = shape.GetHashCode();
            Shape test2 = shape.Clone();

            //Assert.IsTrue(shape == shape, "Shape does not equal itself (==)");
            Assert.IsTrue(shape.Equals(shape), "Shape does not equal itself (.Equals)");
            Assert.AreEqual(hash1, hash2, "Shape hash code should never change");
            Assert.IsFalse(test1 == test2, "Different shapes with different substrokes should not be equal (==)");
            Assert.IsFalse(test1.Equals(test2), "Different shapes with different substrokes should not be equal (.Equals)");

            // ======================= Dictionary =========================

            Dictionary<Shape, int> d = new Dictionary<Shape, int>();
            int value = 0;
            int realValue;
            bool success;

            d.Add(shape, value);

            Assert.IsTrue(d.ContainsKey(shape), "Dictionary reports it does not contain a shape that was just added");
            success = d.TryGetValue(shape, out realValue);
            Assert.IsTrue(success, "Dictionary contains shape but cannot find it");
            Assert.AreEqual(value, realValue, "Dictionary has wrong value for shape");

            shape.AddSubstroke(newValidSubstroke());

            Assert.IsTrue(d.ContainsKey(shape), "After modifying a shape, dictionary no longer contains it");
            success = d.TryGetValue(shape, out realValue);
            Assert.IsTrue(success, "After adding a substroke, dictionary contains shape but cannot find it");
            Assert.AreEqual(value, realValue, "After adding a substroke, dictionary has wrong value for shape");

        }

    }
}
