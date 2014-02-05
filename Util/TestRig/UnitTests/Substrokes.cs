using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sketch;

namespace UnitTests
{
    [TestClass]
    public class Substrokes
    {

        [TestMethod]
        public void TestSubstrokeEqualsAndClone()
        {

            Substroke s1 = Shapes.newValidSubstroke();
            s1.Classification = Domain.LogicDomain.TEXT_CLASS;

            Substroke s2 = s1.Clone();

            Assert.IsTrue(s1.Equals(s2), "Original substroke reports it is not equal (.Equals) to the clone");
            Assert.IsTrue(s2.Equals(s1), "Cloned substroke reports it is not equal (.Equals) to the original");

            Assert.IsTrue(s1.GeometricEquals(s2), "Original substroke reports it is not equal (.GeometricEquals) to the clone");
            Assert.IsTrue(s2.GeometricEquals(s1), "Cloned substroke reports it is not equal (.GeometricEquals) to the original");

            s2.Classification = Domain.LogicDomain.GATE_CLASS;

            Assert.IsFalse(s1.Equals(s2), "Substroke equality does not depend on classification");
            Assert.IsFalse(s2.Equals(s1), "Substroke equality does not depend on classification");

            Assert.IsTrue(s1.GeometricEquals(s2), "Geometric substroke equality depends on classification");
            Assert.IsTrue(s2.GeometricEquals(s1), "Geometric substroke equality depends on classification");

            Assert.IsFalse(s1.Id.Equals(s2.Id), "Two substrokes should not have the same ID, even if they are clones");

        }

        [TestMethod]
        public void TestSubstrokesInDictionaries()
        {
            Substroke substroke = Shapes.newValidSubstroke();

            Dictionary<Substroke, int> d = new Dictionary<Substroke, int>();
            int value = 0;
            int realValue;
            bool success;

            d.Add(substroke, value);

            Assert.IsTrue(d.ContainsKey(substroke), "Dictionary reports it does not contain a substroke that was just added");
            success = d.TryGetValue(substroke, out realValue);
            Assert.IsTrue(success, "Dictionary contains substroke but cannot find it");
            Assert.AreEqual(value, realValue, "Dictionary has wrong value for substroke");

            substroke.Classification = Domain.LogicDomain.GATE_CLASS;

            Assert.IsTrue(d.ContainsKey(substroke), "After modifying substroke, dictionary no longer contains it");
            success = d.TryGetValue(substroke, out realValue);
            Assert.IsTrue(success, "After adding a substroke, dictionary contains shape but cannot find it");
            Assert.AreEqual(value, realValue, "After adding a substroke, dictionary has wrong value for shape");

        }

    }
}
