/*
 * File: MakeXml.cs
 *
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace ConverterXML
{
    /// <summary>
    /// MakeXml is the class that provides the interface for creating the XML document.
    /// </summary>
    public class SaveToXML
    {
        #region INTERNALS

        /// <summary>
        /// The Sketch
        /// </summary>
        private Sketch.Project project;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="project">Create SaveToXML from sketch</param>
        public SaveToXML(Sketch.Project project)
        {
            this.project = project;
        }


        #endregion

        #region Setup
        /// <summary>
        /// Write the XML document with the given filename.
        /// </summary>
        /// <param name="filename">Name of the document to create</param>
        public void WriteXML(string filename)
        {
            XmlTextWriter xmlDocument = new XmlTextWriter(filename, System.Text.Encoding.UTF8);
            WriteXML(xmlDocument);
            xmlDocument.Close();
        }

        /// <summary>
        /// Write the XML to the given XmlTextWriter
        /// </summary>
        /// <param name="textWriter">the target XmlTextWriter, which could be a file or string</param>
        private void WriteXML(XmlTextWriter textWriter)
        {
            textWriter.Formatting = System.Xml.Formatting.Indented;
            textWriter.WriteStartDocument();
            SaveToXML.WriteSketch(this.project, textWriter);
            textWriter.WriteEndDocument();
        }
        
        #endregion

        #region writing sketch

        /// <summary>
        /// writes the sketch and its points, substrokes and shapes to the given xml document.
        /// also writes its circuit.
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="circuit"></param>
        /// <param name="xmlDocument"></param>
        private static void WriteSketch(Sketch.Project project, XmlTextWriter xmlDocument)

        {
            xmlDocument.WriteStartElement("sketch");

            string[] sketchAttributeNames = project.sketch.XmlAttrs.getAttributeNames();
            object[] sketchAttributeValues = project.sketch.XmlAttrs.getAttributeValues();

            Sketch.Point[] points = project.sketch.Points;
            Sketch.Shape[] shapes = project.sketch.Shapes;
            Sketch.Stroke[] strokes = project.sketch.Strokes;
            Sketch.Substroke[] substrokes = project.sketch.Substrokes;
            

            int length;
            int i;


            // Write all the attributes
            length = sketchAttributeNames.Length;
            for (i = 0; i < length; ++i)
                if (sketchAttributeValues[i] != null)
                    xmlDocument.WriteAttributeString(sketchAttributeNames[i], sketchAttributeValues[i].ToString());

            // Write all the points
            length = points.Length;
            for (i = 0; i < length; ++i)
                SaveToXML.WritePoint(points[i], xmlDocument);

            // Write all the substrokes
            length = substrokes.Length;
            for (i = 0; i < length; ++i)
                SaveToXML.WriteSubstroke(substrokes[i], xmlDocument);

            // Write all the strokes
            length = strokes.Length;
            for (i = 0; i < length; ++i)
                SaveToXML.WriteStroke(strokes[i], xmlDocument);

            // Write all the shapes
            length = shapes.Length;
            for (i = 0; i < length; ++i)
                SaveToXML.WriteShape(shapes[i], xmlDocument);            

            WriteCircuit(project, xmlDocument);

            WriteSubCircuitInfo(xmlDocument, project);
            xmlDocument.WriteEndElement();
        }

        #region WRITE POINT

        private static void WritePointReference(Sketch.Point point, XmlTextWriter xmlDocument)
        {
            xmlDocument.WriteStartElement("arg");
            xmlDocument.WriteAttributeString("type", "point");
            xmlDocument.WriteString(point.Id.ToString());
            xmlDocument.WriteEndElement();
        }


        private static void WritePoint(Sketch.Point point, XmlTextWriter xmlDocument)
        {
            string[] pointAttributeNames = point.XmlAttrs.getAttributeNames();
            object[] pointAttributeValues = point.XmlAttrs.getAttributeValues();
            int length;
            int i;

            xmlDocument.WriteStartElement("point");

            // Write all the attributes
            length = pointAttributeNames.Length;
            for (i = 0; i < length; ++i)
                if (pointAttributeValues[i] != null)
                    xmlDocument.WriteAttributeString(pointAttributeNames[i], pointAttributeValues[i].ToString());

            xmlDocument.WriteEndElement();
        }


        #endregion

        #region WRITE SUBSTROKE

        private static void WriteSubstrokeReference(Sketch.Substroke substroke, XmlTextWriter xmlDocument)
        {
            xmlDocument.WriteStartElement("arg");

            xmlDocument.WriteAttributeString("type", "substroke");
            xmlDocument.WriteString(substroke.XmlAttrs.Id.ToString());

            xmlDocument.WriteEndElement();
        }


        private static void WriteSubstroke(Sketch.Substroke substroke, XmlTextWriter xmlDocument)
        {
            string[] substrokeAttributeNames = substroke.XmlAttrs.getAttributeNames();
            object[] substrokeAttributeValues = substroke.XmlAttrs.getAttributeValues();

            Sketch.Point[] points = substroke.Points;
            int length;
            int i;

            xmlDocument.WriteStartElement("shape");

            // Write all the attributes
            length = substrokeAttributeNames.Length;
            for (i = 0; i < length; ++i)
                if (substrokeAttributeValues[i] != null)
                    xmlDocument.WriteAttributeString(substrokeAttributeNames[i], substrokeAttributeValues[i].ToString());

            // Write the point references
            length = points.Length;
            for (i = 0; i < length; ++i)
                SaveToXML.WritePointReference(points[i], xmlDocument);

            xmlDocument.WriteEndElement();
        }

        #endregion

        #region WRITE STROKE

        private static void WriteStrokeReference(Sketch.Stroke stroke, XmlTextWriter xmlDocument)
        {
            xmlDocument.WriteStartElement("arg");

            xmlDocument.WriteAttributeString("type", "stroke");//stroke.XmlAttrs.Type
            xmlDocument.WriteString(stroke.XmlAttrs.Id.ToString());

            xmlDocument.WriteEndElement();
        }


        private static void WriteStroke(Sketch.Stroke stroke, XmlTextWriter xmlDocument)
        {
            string[] strokeAttributeNames = stroke.XmlAttrs.getAttributeNames();
            object[] strokeAttributeValues = stroke.XmlAttrs.getAttributeValues();

            Sketch.Substroke[] substrokes = stroke.Substrokes;
            int length;
            int i;

            xmlDocument.WriteStartElement("shape");

            // Write all the attributes
            length = strokeAttributeNames.Length;
            for (i = 0; i < length; ++i)
                if (strokeAttributeValues[i] != null)
                    xmlDocument.WriteAttributeString(strokeAttributeNames[i], strokeAttributeValues[i].ToString());
            // Write the substroke references
            length = substrokes.Length;
            for (i = 0; i < length; ++i)
                SaveToXML.WriteSubstrokeReference(substrokes[i], xmlDocument);

            xmlDocument.WriteEndElement();
        }


        #endregion

        #region WRITE SHAPE

        private static void WriteShape(Sketch.Shape shape, XmlTextWriter xmlDocument)
        {
            string[] shapeAttributeNames = shape.XmlAttrs.getAttributeNames();
            object[] shapeAttributeValues = shape.XmlAttrs.getAttributeValues();

            Sketch.Substroke[] substrokes = shape.Substrokes;
            int length;
            int i;

            xmlDocument.WriteStartElement("shape");

            // Write all the attributes
            length = shapeAttributeNames.Length;
            for (i = 0; i < length; ++i)
                if (shapeAttributeValues[i] != null)
                    xmlDocument.WriteAttributeString(shapeAttributeNames[i], shapeAttributeValues[i].ToString());
            // Write all the substrokes args
            length = substrokes.Length;
            for (i = 0; i < length; ++i)
                SaveToXML.WriteSubstrokeReference(substrokes[i], xmlDocument);

            // Write all of the neighbor args
            foreach (Sketch.Shape neighbor in shape.ConnectedShapes)
            {
                SaveToXML.WriteNeighborReference(neighbor, xmlDocument);
            }
            if (shape.Type == Domain.LogicDomain.SUBCIRCUIT)
            {
                xmlDocument.WriteStartElement("SubCircuit");
                xmlDocument.WriteAttributeString("tagNumber", shape.SubCircuitNumber.ToString());
                xmlDocument.WriteEndElement();
            }


            xmlDocument.WriteEndElement();
        }
        private static void WriteShapeReference(Sketch.Shape shape, XmlTextWriter xmlDocument)
        {
            xmlDocument.WriteStartElement("arg");
            xmlDocument.WriteAttributeString("type", "shape");
            xmlDocument.WriteString(shape.XmlAttrs.Id.ToString());

            xmlDocument.WriteEndElement();
        }

        private static void WriteNeighborReference(Sketch.Shape shape, XmlTextWriter xmlDocument)
        {
            xmlDocument.WriteStartElement("neighbor");
            xmlDocument.WriteString(shape.Id.ToString());
            
            xmlDocument.WriteEndElement();
        }
        #endregion
        #endregion

        #region writing circuit
        /// <summary>
        /// precondition: circuit is not null
        /// </summary>
        /// <param name="circuit"></param>
        /// <param name="xmlDocument"></param>
        private static void WriteCircuit(Sketch.Project project, XmlTextWriter xmlDocument)
        {
            xmlDocument.WriteStartElement("circuit");

            SaveToXML.WriteInputOutput(project.inputs, project.outputs, xmlDocument);
            SaveToXML.WriteBehavior(project.behavior, xmlDocument);
            SaveToXML.WriteLogisim(project.saveToCircDoc, xmlDocument);
            xmlDocument.WriteEndElement();
        }

        /// <summary>
        /// Write the lookup information for subcircuits
        /// </summary>
        /// <param name="xmlDocument"></param>
        private static void WriteSubCircuitInfo(XmlTextWriter xmlDocument, Sketch.Project project)
        {
            xmlDocument.WriteStartElement("SubCircuitLookup");
            foreach (int indexUsed in project.subProjectsused.Keys)
            {
                if (project.subProjectsused[indexUsed] > 0)
                {
                    xmlDocument.WriteStartElement("Number");
                    xmlDocument.WriteAttributeString("lookup", indexUsed.ToString());
                    WriteSketch(project.subProjectLookup[indexUsed], xmlDocument);
                    xmlDocument.WriteEndElement();
                }
            }

            xmlDocument.WriteEndElement();

        }

        /// <summary>
        /// Uses the truthtable passed in and writes the inputs to output mapping as new elements
        /// </summary>
        /// <param name="truthTable"></param>
        /// <param name="xmlDocument"></param>
        private static void WriteBehavior(Dictionary<List<int>, List<int>> behavior, XmlTextWriter xmlDocument)
        {
            xmlDocument.WriteStartElement("behavior");

            foreach (KeyValuePair<List<int>, List<int>> ioPair in behavior)
            {
                string input = "";
                foreach (int i in ioPair.Key)
                    input += i.ToString() + " ";
                xmlDocument.WriteStartElement("input");
                //don't want the last space
                xmlDocument.WriteString(input.Substring(0,input.Length-1));
                xmlDocument.WriteEndElement();

                string output = "";
                foreach (int i in ioPair.Value)
                    output += i.ToString() + " ";
                xmlDocument.WriteStartElement("output");
                //don't want the last space
                xmlDocument.WriteString(output.Substring(0, output.Length - 1));
                xmlDocument.WriteEndElement();
            }
            xmlDocument.WriteEndElement();
           
        }

        private static void WriteLogisim(string LogisimWriter, XmlTextWriter xmlDocument)
        {
            xmlDocument.WriteStartElement("logisimString");
            xmlDocument.WriteString(LogisimWriter);
            xmlDocument.WriteEndElement();
        }

        /// <summary>
        /// Stores the strings that are the names of the inputs and outputs of the circuit
        /// as attribuits of the circuit header seperated by spaces
        /// </summary>
        /// <param name="truthTable"></param>
        /// <param name="xmlDocument"></param>
        private static void WriteInputOutput(List<string> inputs, List<string> outputs, XmlTextWriter xmlDocument)
        {
            string inputString = "";
            foreach (string input in inputs)
            {
                inputString += input + " ";
            }
            // Write the attribute without the last space
            if (inputString != "")
                xmlDocument.WriteAttributeString("inputs", inputString.Substring(0,inputString.Length-1));

            string outputString = "";
            foreach (string output in outputs)
            {
                outputString += output + " ";
            }
            // Write the attribute without the last space
            if (outputString != "")
                xmlDocument.WriteAttributeString("outputs", outputString.Substring(0, outputString.Length - 1));
        }
        #endregion

    }
}
