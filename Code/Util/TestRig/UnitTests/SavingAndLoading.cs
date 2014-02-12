using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecognitionInterfaces;
using Domain;

namespace UnitTests
{
    [TestClass]
    public class SavingAndLoading
    {

        [TestMethod]
        public void TestXMLConversion()
        {
            const string filename = "tmp.xml";
            RecognitionResult type = new RecognitionResult(LogicDomain.AND, 0.9, 0.33);

            Sketch.Project project1 = new Sketch.Project(Sketches.newValidSketch());;

            foreach (Sketch.Shape shape in project1.sketch.Shapes)
                type.ApplyToShape(shape);

            ConverterXML.SaveToXML writer = new ConverterXML.SaveToXML(project1);
            writer.WriteXML(filename);

            ConverterXML.ReadXML reader = new ConverterXML.ReadXML(filename);
            Sketch.Sketch sketch2 = reader.Sketch;

            sketch2.CheckConsistency();

            Assert.IsTrue(project1.sketch.Equals(sketch2), "Original sketch is not equal to the loaded sketch");
            Assert.IsTrue(sketch2.Equals(project1.sketch), "Loaded sketch is not equal to the original");

            foreach (Sketch.Shape shape in sketch2.Shapes)
            {
                Assert.AreEqual(type.Type, shape.Type, "Shape types are not preserved across save/load");
                Assert.IsTrue(Math.Abs(type.Confidence - shape.Probability) < 0.0001, "Shape confidences are not preserved across save/load");
                Assert.IsTrue(Math.Abs(type.Orientation - shape.Orientation) < 0.0001, "Shape orientations are not preserved across save/load");
            }

        }

    }
}
