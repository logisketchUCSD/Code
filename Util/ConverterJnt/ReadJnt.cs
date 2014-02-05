/*
 * File: ReadJnt.cs
 *
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Ink;
using Sketch;

namespace ConverterJnt
{
    /// <summary>
    /// ReadJnt class.
    /// </summary>
    public class ReadJnt
    {
        #region INTERNALS

        // This is the standard sample rate according to http://msdn.microsoft.com/msdnmag/issues/03/10/TabletPC/default.aspx
        private const float SAMPLE_RATE = 133.0f;

        private Sketch.Sketch sketch;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Constructor, creates an empty ReadJnt instance that
        /// can function as an converter for ink to sketch data.
        /// <see cref="ReadJnt.InkStroke2SketchStroke()"/>
        /// </summary>
        public ReadJnt()
        {
            sketch = null;
        }
        
        /// <summary>
        /// Constructor, creates a Sketch from the first page of the file
        /// </summary>
        /// <param name="filename">The name of the file</param>
        public ReadJnt(string filename)
            : this(filename, 1)
        {
        }


        /// <summary>
        /// Constructor, creates a Sketch from the page indicated 
        /// </summary>
        /// <param name="filename">The name of the file</param>
        /// <param name="pageNumber">The page number</param>
        public ReadJnt(string filename, int pageNumber)
        {
            sketch = new Sketch.Sketch();
            sketch.Units = "himetric";

            try
            {
                this.ConvertToSketch(filename, pageNumber);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException);
            }
        }

        #endregion

        #region GET

        /// <summary>
        /// Get GetSketch
        /// </summary>
        public Sketch.Sketch Sketch
        {
            get
            {
                return this.sketch;
            }
        }


        /// <summary>
        /// Converts a single Microsoft Ink stroke to a Sketch stroke.
        /// </summary>
        /// <param name="inkStroke">The Microsoft Ink stroke to convert</param>
        /// <returns>A Sketch stroke.</returns>
        /// <seealso cref="ReadJnt.StripStrokeData">This function wraps StripStrokeData()</seealso>
        public Sketch.Stroke InkStroke2SketchStroke(Microsoft.Ink.Stroke inkStroke) 
        {
            return InkStroke2SketchStroke(inkStroke, null, false);
        }
        
        /// <summary>
        /// Converts a single Microsoft Ink stroke to a Sketch stroke.  Supports using a
        /// transformation matrix
        /// </summary>
        /// <param name="inkStroke">The Microsoft Ink stroke to convert</param>
        /// <param name="transf">Transformation matrix for stroke shifting</param>
        /// <param name="shift">True iff the transformation shift matrix should be used</param>
        /// <returns>A Sketch stroke.</returns>
        /// <seealso cref="ReadJnt.StripStrokeData">This function wraps StripStrokeData()</seealso>
        public Sketch.Stroke InkStroke2SketchStroke(Microsoft.Ink.Stroke inkStroke, Matrix transf, bool shift)
        {
            return StripStrokeData(inkStroke, transf, shift);
        }

        

        #endregion

        #region DATA MUNGING

        private void ConvertToSketch(string filename, int pageNumber)
        {
            ByteHolder[] strokes;
            InkHolder[] inks;

            Stream inkStream = File.OpenRead(filename);
            XmlDocument xmlDocument = ReadJnt.GetMicrosoftXmlFromJournal(inkStream);

            List<Sketch.Stroke> convertedStrokes = new List<Sketch.Stroke>();

            // The conversion process basically extracts Ink information
            // and puts it into our own Point/Stroke format.
            // The conversion process uses Converter.Converter.
            // Seek that file for specifics.
            strokes = ExtractXmlInkObject(xmlDocument, pageNumber);
            inks = CreateStrokes(strokes);
            convertedStrokes = ReadJnt.CreateConvertedStrokes(inks);

            this.sketch.AddStrokes(convertedStrokes);
        }


        /// <summary>
        /// Extracts InkObjects from a Microsoft JNT.XML file, which store information about Ink.Stroke data.
        /// Converts each InkObject from a Base64 string into a byte array.
        /// </summary>
        /// <param name="xmlDoc">A Microsoft JNT.XML file that contains Ink data to be extracted.</param>
        /// <param name="pageNumber">Page number to extract</param>
        /// <returns>The Ink data of the document stored in an array of byte arrays</returns>
        private ByteHolder[] ExtractXmlInkObject(XmlDocument xmlDoc, int pageNumber)
        {
            //Please refer to http://support.microsoft.com/default.aspx?scid=kb;en-us;318545 for help
            XmlNamespaceManager manager = new XmlNamespaceManager(xmlDoc.NameTable);
            manager.AddNamespace("sketch", "urn:schemas-microsoft-com:tabletpc:journalreader");

            //This is an xpath query to extract all the InkObjects from the specific JournalPage
            XmlNodeList inkNodes = xmlDoc.SelectNodes("//sketch:JournalPage[@Number='" + pageNumber.ToString() + "']//sketch:InkObject", manager);

            ByteHolder[] toReturn = new ByteHolder[inkNodes.Count];


            // Create an array of byte arrays
            //byte [][] textFromInkObjs = new byte[inkNodes.Count][];

            // Cycle through the inkNodes and pull out each InkObject
            int i;
            int j;
            int length = inkNodes.Count;
            for (i = 0; i < length; ++i)
            {
                // Get the matrix scalartransform data
                XmlNode transform = inkNodes[i].ParentNode.FirstChild;
                toReturn[i].isShifted = false;
                if (transform.Name == "ScalarTransform")
                {
                   toReturn[i].isShifted = true;
                    float[] m = new float[9];
                    for (j = 1; j <= 9; ++j)
                        m[j - 1] = Convert.ToSingle(transform.Attributes["Mat" + j.ToString()].Value);

                    // We have to scale the last two arguments by 2.54 so that the shifting transformation works correctly
                    // There are other examples of code that does this (InkExplorer), but it is undocumented.
                    // All we really know is that in order to get shifting/scaling to work, you need to divide the
                    // shift by 2.54
                    toReturn[i].shift = new Matrix(m[0], m[1], m[3], m[4], (int)(m[2] / 2.54), (int)(m[5] / 2.54));
                }

                // Grab the Base64 string from the InkObject and convert it to a byte array
                // textFromInkObjs[count] = Convert.FromBase64String(inkNode.InnerText);
                toReturn[i].inkBytes = Convert.FromBase64String(inkNodes[i].InnerText);
            }

            return toReturn;
            //return textFromInkObjs;
        }


        /// <summary>
        /// Loads the byte array representation of a document's Ink objects into an array of actual Ink objects.
        /// </summary>
        /// <param name="strokeData">This array of byte arrays stores all of the stroke data contained in the document being converted.</param>
        /// <returns>An array of Ink objects that have been loaded with the data saved in the passed byte arrays.</returns>
        private InkHolder[] CreateStrokes(ByteHolder[] strokeData)
        {
            InkHolder[] inkData = new InkHolder[strokeData.Length];

            int length = strokeData.Length;
            for (int i = 0; i < length; ++i)
            {
                inkData[i].inkInfo = new Ink();
                inkData[i].inkInfo.Load(strokeData[i].inkBytes);
                inkData[i].isShifted = strokeData[i].isShifted;
                inkData[i].shift = strokeData[i].shift;
            }

            return inkData;
        }


        /// <summary>
        /// This function loops though each stroke contained in each Ink object in the document
        /// and uses the stripStrokeData function to create a
        /// Converter.Stroke for every Ink.Stroke.
        /// </summary>
        /// <param name="inkData">inkData is an array of Ink objects that together store all of the stroke data
        /// contained in the document under consideration.</param>
        /// <returns>ArrayList of Converter.Stroke that contains all of the data about the document's strokes.</returns>
        public static List<Sketch.Stroke> CreateConvertedStrokes(InkHolder[] inkData)
        {
            List<Sketch.Stroke> strokes = new List<Sketch.Stroke>();

            int i;
            int j;
            int inkLength = inkData.Length;
            int strokeLength;

            for (i = 0; i < inkLength; ++i)
            {
                strokeLength = inkData[i].inkInfo.Strokes.Count;

                for (j = 0; j < strokeLength; ++j)
                    strokes.Add(ReadJnt.StripStrokeData(inkData[i].inkInfo.Strokes[j],
                        inkData[i].shift, inkData[i].isShifted));
            }

            return strokes;
        }


        /// <summary>
        /// Takes an Ink.Stroke and outputs an internal representation that can 
        /// then be output to an XML file
        /// </summary>
        /// <param name="inkStroke">Ink.Stroke which will have extracted from it the information necessary to store a Stroke in MIT XML.</param>
        /// <param name="transf">Transformation matrix to shift the stroke by</param>
        /// <param name="shift">Boolean for if we should shift by the transformation matrix</param>
        /// <returns>PointsAndShape object that stores the extracted information.</returns>
        public static Sketch.Stroke StripStrokeData(Microsoft.Ink.Stroke inkStroke, Matrix transf, bool shift)
        {
            int i;
            int length; 

            // Get the timestamp for the function using an undocumented GUID (Microsoft.Ink.StrokeProperty.TimeID) to
            // get the time as a byte array, which we then convert to a long
            ulong theTime;
            if (inkStroke.ExtendedProperties.Contains(Microsoft.Ink.StrokeProperty.TimeID) 
                && (inkStroke.ExtendedProperties[Microsoft.Ink.StrokeProperty.TimeID] != null))
            {
                byte[] timeBits = inkStroke.ExtendedProperties[Microsoft.Ink.StrokeProperty.TimeID].Data as byte[];

                // Filetime format
                ulong fileTime = BitConverter.ToUInt64(timeBits, 0);

                // MIT time format
                theTime = (fileTime - 116444736000000000) / 10000;
                //string time = theTime.ToString();	
            }
            else
            {
                //theTime = (ulong)System.DateTime.Now.Ticks;
                //theTime = ((ulong)System.DateTime.Now.Ticks - 116444736000000000) / 10000;
                theTime = ((ulong)DateTime.Now.ToFileTime() - 116444736000000000) / 10000;
            }
           
            // Grab X and Y
            int[] xData = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.X);
            int[] yData = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.Y);

            if (shift)
            {
                // Shift X and Y according to transformation matrix
                System.Drawing.Point[] pointData = new System.Drawing.Point[xData.Length];
                length = xData.Length;
                for (i = 0; i < length; i++)
                {
                    pointData[i] = new System.Drawing.Point(xData[i], yData[i]);
                }

                transf.TransformPoints(pointData);
                
                //Console.WriteLine("x: " + (pointData[0].X - xData[0]) + " y: " + (pointData[0].Y - yData[0]));

                for (i = 0; i < length; i++)
                {
                    xData[i] = pointData[i].X;
                    yData[i] = pointData[i].Y;
                }
            }

            Microsoft.Ink.DrawingAttributes drawingAttributes = inkStroke.DrawingAttributes;
            System.Drawing.Rectangle boundingBox = inkStroke.GetBoundingBox();

            // Grab the color
            int color = drawingAttributes.Color.ToArgb();

            // THIS IS DRAWING POINT (PENTIP) HEIGHT AND WIDTH... WE DONT HAVE ANYWHERE TO PUT IT...
            //string height = inkStroke.DrawingAttributes.Height.ToString();
            //string width = inkStroke.DrawingAttributes.Width.ToString();
            float penWidth = drawingAttributes.Width;
            float penHeight = drawingAttributes.Height;

            // Grab height and width of total stroke
            //float height = boundingBox.Height;
            //float width = boundingBox.Width;

            // Grab penTip
            string penTip = drawingAttributes.PenTip.ToString();

            // Grab raster
            string raster = drawingAttributes.RasterOperation.ToString();


            //float x = Convert.ToSingle(boundingBox.X);
            //float y = Convert.ToSingle(boundingBox.Y);

            //float leftx = Convert.ToSingle(boundingBox.Left);
            //float topy = Convert.ToSingle(boundingBox.Top);
            
            // If the pressure data is included take it, otherwise set to 255/2's
            // Not all pressure data is in the range of 0 - 255. Some tablets
            // register pressure in the range 0 - 1023, while others are 0 - 32768
            int[] pressureData;
            if (ReadJnt.IsPropertyIncluded(inkStroke, Microsoft.Ink.PacketProperty.NormalPressure))
            {
                pressureData = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.NormalPressure);
                int max = 0;
                foreach (int p in pressureData)
                    max = Math.Max(max, p);

                if (max > 1024) // Tablet reading in range 0 - 32767
                {
                    double conversion = 255.0 / 32767.0;
                    List<int> temp = new List<int>(pressureData.Length);
                    foreach (int p in pressureData)
                        temp.Add((int)(p * conversion));

                    pressureData = temp.ToArray();
                }
                else if (max > 255) // Tablet reading in range 0 - 1023
                {
                    double conversion = 255.0 / 1023.0;
                    List<int> temp = new List<int>(pressureData.Length);
                    foreach (int p in pressureData)
                        temp.Add((int)(p * conversion));

                    pressureData = temp.ToArray();
                }
            }
            else
            {
                pressureData = new int[xData.Length];
                length = pressureData.Length;
                for (i = 0; i < length; ++i)
                    pressureData[i] = (255 - 0) / 2;
            }

            // If the time data is included take it, otherwise set to the timestamp plus and increment
            ulong[] adjustedTime = new ulong[xData.Length];
            int[] timerTick;
            if (ReadJnt.IsPropertyIncluded(inkStroke, Microsoft.Ink.PacketProperty.TimerTick))
            {
                timerTick = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.TimerTick);
                length = timerTick.Length;
                for (i = 0; i < length; i++)
                {
                    // This may be incorrect, we never encountered this because Microsoft Journal doesn't save TimerTick data.
                    // We know that the timestamp of a stroke is made when the pen is lifted.

                    // Add the time of the stroke to each offset
                    adjustedTime[i] = theTime + (ulong)timerTick[i] * 10000;
                }
            }
            else
            {
                timerTick = new int[xData.Length];
                length = timerTick.Length;
                for (i = 0; i < length; i++)
                {
                    // We believe this to be the standard sample rate.  The multiplication by 1,000 is to convert from
                    // seconds to milliseconds.
                    //
                    // Our time is in the form of milliseconds since Jan 1, 1970
                    //
                    // NOTE: The timestamp for the stroke is made WHEN THE PEN IS LIFTED
                    adjustedTime[i] = theTime - (ulong)((1 / SAMPLE_RATE * 1000) * (length - i));
                }
            }

            // Create the array of ID's as new Guid's
            Guid[] idData = new Guid[xData.Length];
            length = idData.Length;
            for (i = 0; i < length; i++)
            {
                idData[i] = Guid.NewGuid();
            }

            // Create the internal representation
            Sketch.Stroke converterStroke = new Sketch.Stroke();
            converterStroke.Name = "stroke";
            converterStroke.Time = (ulong)theTime;
            converterStroke.Source = "Converter";
            

            // Add all of the points to the stroke

            length = inkStroke.PacketCount;
            List<Point> pointsToAdd = new List<Point>();
            for (i = 0; i < length; i++)
            {
                float currX = Convert.ToSingle(xData[i]);
                float currY = Convert.ToSingle(yData[i]);

                XmlStructs.XmlPointAttrs attrs = new XmlStructs.XmlPointAttrs(
                    currX, currY, 
                    Convert.ToUInt16(pressureData[i]), 
                    Convert.ToUInt64(adjustedTime[i]), 
                    "point", idData[i]);

                Sketch.Point toAdd = new Sketch.Point(attrs);
            }


            //NOTE: X, Y, WIDTH, HEIGHT done later... boundingbox seems to be off
            Sketch.Substroke substroke = new Sketch.Substroke(pointsToAdd);
            substroke.Name = "substroke";
            substroke.Color = color;
            substroke.PenTip = penTip;
            substroke.PenWidth = penWidth;
            substroke.PenHeight = penHeight;
            substroke.Raster = raster;
            substroke.Source = "ConverterJnt";
            substroke.Start = idData[0];
            substroke.End = idData[idData.Length - 1];

            converterStroke.AddSubstroke(substroke);
            return converterStroke;
        }


        /// <summary>
        /// Given a Stroke and a GUID, checks the Stroke's PacketDescription to see if the field corresponding to the GUID is there.
        /// </summary>
        /// <param name="stroke">Stroke to have it's PacketDescription checked</param>
        /// <param name="pp">GUID of the field that are checking to see if the PacketDescription contains.</param>
        /// <returns>bool that is true if the Stroke's PacketDescription has a field corresponding with the GUID.</returns>
        public static bool IsPropertyIncluded(Microsoft.Ink.Stroke stroke, Guid pp)
        {
            foreach (Guid g in stroke.PacketDescription)
            {
                if (g == pp)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Return the number of Journal pages from a XmlDocument (Microsoft format)
        /// </summary>
        /// <param name="xmlDoc">XmlDocument from the Journal component</param>
        /// <returns>Number of pages</returns>
        public static int NumberOfPages(XmlDocument xmlDoc)
        {
            //Please refer to http://support.microsoft.com/default.aspx?scid=kb;en-us;318545 for help
            XmlNamespaceManager manager = new XmlNamespaceManager(xmlDoc.NameTable);
            manager.AddNamespace("sketch", "urn:schemas-microsoft-com:tabletpc:journalreader");

            XmlNode pageNode = xmlDoc.SelectSingleNode("//sketch:JournalPage[last()]", manager);
            System.Xml.XmlAttribute attribute = pageNode.Attributes["Number"];

            return Convert.ToInt32(attribute.Value);
        }


        /// <summary>
        /// Return the number of Journal pages from a XmlDocument (Microsoft format)
        /// </summary>
        /// <param name="filename">XML document's filename</param>
        /// <returns>Number of pages</returns>
        public static int NumberOfPages(string filename)
        {
            Stream inkStream = File.OpenRead(filename);
            XmlDocument xmlDoc = ReadJnt.GetMicrosoftXmlFromJournal(inkStream);

            //Please refer to http://support.microsoft.com/default.aspx?scid=kb;en-us;318545 for help
            XmlNamespaceManager manager = new XmlNamespaceManager(xmlDoc.NameTable);
            manager.AddNamespace("sketch", "urn:schemas-microsoft-com:tabletpc:journalreader");

            XmlNode pageNode = xmlDoc.SelectSingleNode("//sketch:JournalPage[last()]", manager);
            System.Xml.XmlAttribute attribute = pageNode.Attributes["Number"];

            return Convert.ToInt32(attribute.Value);
        }


        /// <summary>
        /// This code is coppied verbatim from an example on MSDN by Casey Chesnut 
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dntablet/html/tbconJournXML.asp
        /// 
        /// It uses Microsoft's JournalReader.ReadFromStream method to change a binary JNT file 
        /// into Microsoft's XML format
        /// </summary>
        /// <param name="stream">Stream containing a binary JNT file.</param>
        /// <returns>XmlDocument in Microsoft's format, based on information from JNT file.</returns>
        private static XmlDocument GetMicrosoftXmlFromJournal(Stream stream)
        {
            Stream xmlStream = Microsoft.Ink.JournalReader.ReadFromStream(stream);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlStream);
            xmlStream.Close();
            return xmlDoc;
        }


        #endregion

        #region HOLDERS

        /// <summary>
        /// ByteHolder initially holds all of the data from InkObjects in the Microsoft XML, in addition to a transformation matrix, if it exists
        /// </summary>
        public struct ByteHolder
        {
            /// <summary>
            /// Holds a byte array that corresponds to an ink object
            /// </summary>
            public byte[] inkBytes;

            /// <summary>
            /// Tells us if this ink object has been shifted
            /// </summary>
            public bool isShifted;

            /// <summary>
            /// If the ink has been shifted, this stores the affine matrix that encodes
            /// the shifting information
            /// </summary>
            public Matrix shift;
        }


        /// <summary>
        /// InkHolder stores the loaded version of Ink objects, once they have been converted out of their byte[] form.  It also contains a transformation
        /// matrix, if it exists
        /// </summary>
        public struct InkHolder
        {
            /// <summary>
            /// Holds ink
            /// </summary>
            public Ink inkInfo;

            /// <summary>
            /// Tells us if this ink object has been shifted
            /// </summary>
            public bool isShifted;

            /// <summary>
            /// If the ink has been shifted, this stores the affine matrix that encodes 
            /// the shifting information
            /// </summary>
            public Matrix shift;
        }


        #endregion
    }
}
