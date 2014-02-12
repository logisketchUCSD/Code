using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Data;
using Sketch;

namespace UnitTests
{
    [TestClass]
    public class SmartCacheTests
    {

        private int timesNumSubstrokesCalled;

        private int numSubstrokes(Shape shape)
        {
            timesNumSubstrokesCalled++;
            return shape.SubstrokesL.Count;
        }

        [TestMethod]
        public void TestSmartCacheMutableObjects()
        {

            Shape shape1 = Shapes.newValidShape();
            Shape shape2 = Shapes.newValidShape();
            shape1.AddSubstroke(Shapes.newValidSubstroke());

            int shape1Value = numSubstrokes(shape1);
            int shape2Value = numSubstrokes(shape2);

            timesNumSubstrokesCalled = 0;

            SmartCache<Shape, int> cache = new SmartCache<Shape, int>(numSubstrokes);

            Assert.AreEqual(0, timesNumSubstrokesCalled, "SmartCache called the computation function in its constructor!?");

            Assert.AreEqual(shape1Value, cache[shape1], "SmartCache did not compute shape1 value correctly");
            Assert.AreEqual(shape2Value, cache[shape2], "SmartCache did not compute shape2 value correctly");

            Assert.AreEqual(shape1Value, cache[shape1], "SmartCache did not save shape1 value correctly");
            Assert.AreEqual(shape2Value, cache[shape2], "SmartCache did not save shape2 value correctly");

            Assert.AreEqual(2, timesNumSubstrokesCalled, "SmartCache didn't call the computation function twice!?");

            shape1.AddSubstroke(Shapes.newValidSubstroke());
            shape1Value = shape1.SubstrokesL.Count;

            Assert.AreEqual(shape1Value, cache[shape1], "SmartCache did not recompute shape1 value correctly");
            Assert.AreEqual(shape2Value, cache[shape2], "SmartCache did not save shape2 value correctly");

            Assert.AreEqual(3, timesNumSubstrokesCalled, "SmartCache didn't recompute shape1 value!?");

        }

        private int timesPairSumCalled;

        private int pairSum(Tuple<int, int> p)
        {
            timesPairSumCalled++;
            return p.Item1 + p.Item2;
        }

        [TestMethod]
        public void TestSmartCacheRefs()
        {

            var cache = new SmartCache<Tuple<int, int>, int>(pairSum);

            timesPairSumCalled = 0;
            var p1 = Tuple.Create(1, 2);

            // Sanity check
            Assert.AreEqual(cache[p1], 3, "Computed value is incorrect");
            Assert.AreEqual(1, timesPairSumCalled, "Cache called pair sum too many times");
            Assert.AreEqual(cache[p1], 3, "Cached value is incorrect");
            Assert.AreEqual(1, timesPairSumCalled, "Cache called pair sum too many times");

            // Identical pairs should give identical results but should be recomputed
            Assert.AreEqual(cache[Tuple.Create(1, 2)], 3, "Cached value is incorrect");
            Assert.AreEqual(2, timesPairSumCalled, "Identical objects should be recomputed if they are a physically different object");

        }


        private static int numObjectsFinalized;

        private class IntRef : IMutable
        {
            public event EventHandler ObjectChanged;
            private int _value;
            public IntRef(int v) { _value = v; }
            public int Value 
            {
                get { return _value; }
                set
                {
                    if (ObjectChanged != null)
                        ObjectChanged(this, new EventArgs());
                    _value = value;
                }
            }
            ~IntRef()
            {
                SmartCacheTests.numObjectsFinalized++;
            }
        }

        [TestMethod]
        public void TestSmartCacheGC()
        {
            var cache = new SmartCache<IntRef, int>(v => { return v.Value; });

            int value = 5;
            var key = new IntRef(value);

            // Sanity check
            Assert.AreEqual(value, cache[key]);
            key.Value = value = 15;
            Assert.AreEqual(value, cache[key]);

            // Let's reclaim the key.
            numObjectsFinalized = 0;
            key = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Make sure the value was reclaimed
            Assert.AreEqual(1, numObjectsFinalized, "Cache retained value when it should have been collected");
        }

    }
}
