/*
 * File: MakeHHRECO.cs
 *
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Xml;
using Sketch;
using System.Collections.Generic;

namespace ConverterXML
{
    /// <summary>
    /// Converts a Sketch to the HHRECO format
    /// </summary>
    public class MakeHHRECO
    {
        private Sketch.Sketch sketch;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sketch"></param>
        public MakeHHRECO(Sketch.Sketch sketch)
        {
            this.sketch = sketch;
        }

        /// <summary>
        /// Creates an xml document that HHRECO can use to learn
        /// </summary>
        /// <param name="filename"></param>
        public void writeXML(string filename)
        {
            //Create filename for the XML document
            XmlTextWriter xmlDocument = new XmlTextWriter(filename, System.Text.Encoding.ASCII);//, System.Text.Encoding.UTF8);

            //Use indentation
            xmlDocument.Formatting = System.Xml.Formatting.Indented;

            //Create the XML document
            xmlDocument.WriteStartDocument();

            //<MSTrainingModel>
            xmlDocument.WriteStartElement("MSTrainingModel");

            Sketch.Shape[] shapes = this.sketch.Shapes;
            List<string> labels = this.sketch.LabelStrings;


            //Loop through the labels
            for (int i = 0; i < labels.Count; ++i)
            {
                //<type name="label">
                xmlDocument.WriteStartElement("type");
                xmlDocument.WriteAttributeString("name", labels[i]);

                //Loop through the shapes
                for (int j = 0; j < shapes.Length; ++j)
                {
                    // If we are not at the correct label....
                    if (!((string)shapes[j].XmlAttrs.Type).ToLower().Equals(labels[i].ToLower()))
                        continue;

                    //<example label="+" numStrokes="#">
                    xmlDocument.WriteStartElement("example");
                    xmlDocument.WriteAttributeString("label", "+");
                    xmlDocument.WriteAttributeString("numStrokes", shapes[j].Substrokes.Length.ToString());

                    //Loop through the substrokes
                    for (int k = 0; k < shapes[j].Substrokes.Length; ++k)
                    {
                        string xytime = "";
                        //Loop through the points
                        for (int L = 0; L < shapes[j].Substrokes[k].Points.Length; ++L)
                        {
                            xytime += shapes[j].Substrokes[k].Points[L].X + " " +
                                    shapes[j].Substrokes[k].Points[L].Y + " " +
                                    shapes[j].Substrokes[k].Points[L].Time + " ";
                        }
                        xytime = xytime.Trim();

                        //<stroke points="x y time x y time ... " />
                        xmlDocument.WriteStartElement("stroke");
                        xmlDocument.WriteAttributeString("points", xytime);
                        xmlDocument.WriteEndElement();
                    }

                    //</example>
                    xmlDocument.WriteEndElement();
                }

                //</type>
                xmlDocument.WriteEndElement();
            }

            //</MSTrainingModel>
            xmlDocument.WriteEndElement();

            xmlDocument.WriteEndDocument();
            xmlDocument.Close();
        }
    }
}
