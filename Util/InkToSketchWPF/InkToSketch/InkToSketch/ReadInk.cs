/*
 * File: ReadInk.cs
 * 
 * Author: Andrew Danowitz
 * Harvey Mudd College, Claremont, CA 91711
 * Sketchers 2007.
 * 
 * Description: Converts a Microsoft Ink object into a Sketch object.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code my cause.
 */

using System;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;



namespace msInkToHMCSketch
{
    public class ReadInk
    {
        #region INTERNALS

        // This is the standard sample rate according to http://msdn.microsoft.com/msdnmag/issues/03/10/TabletPC/default.aspx
        private const float SAMPLE_RATE = 133.0f;

        private Sketch.Sketch sketch;

        #endregion

        #region CONSTRUCTORS

        ///  <summary>
        ///  Constructor, creates a Sketch from the first page of the file
        ///  </summary>
        ///  <param name="ink">The name of the ink object</param>
        public ReadInk(Ink ink)
        {
            //Code block taken from ConverterJnt
            sketch = new Sketch.Sketch();
            sketch.XmlAttrs.Id = System.Guid.NewGuid();
            sketch.XmlAttrs.Units = "himetric";
            
            //Sends Microsoft.Ink object to be converted into a sketch
            try
            {
                this.ConvertToSketch(ink);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException);
            }
        }

        #endregion

        #region GET

        ///  <summary>
        ///  Get GetSketch
        ///  </summary>
        public Sketch.Sketch Sketch
        {
            get
            {
                return this.sketch;
            }
        }

        /// <summary>
        /// Converts a single Microsoft Ink stroke to a Sketch stroke.  Setting transf to null
        /// and shift to false will result in returning a simple conversion of Ink stroke to
        /// Sketch stroke.  
        /// </summary>
        /// <param name="inkStroke">The Microsoft Ink stroke to convert</param>
        /// <param name="transf">Transformation matrix for stroke shifting</param>
        /// <param name="shift">True iff the transformation shift matrix should be used</param>
        /// <returns>A Sketch stroke.</returns>
        /// <seealso cref="ReadJnt.StripStrokeData">This function wraps for StripStrokeData()</seealso>
        public Sketch.Stroke InkStroke2SketchStroke(Microsoft.Ink.Stroke inkStroke, Matrix transf, bool shift)
        {
            return StripStrokeData(inkStroke, transf, shift);
        }

        #endregion

        #region DATA MUNGING

        /// <summary>
        /// Strips stroke data from the Microsoft Ink object and adds them to the ReadInk
        /// object's sketch.
        /// </summary>
        /// <param name="ink">A Microsoft Ink object</param>
        private void ConvertToSketch(Ink ink)
        {
            ArrayList strokes;

            //Strips the Microsoft stroke data from ink and converts it into Sketch friendly
            //stroke objects
            strokes = CreateConvertedStrokes(ink);

            //Adds strokes to the sketch
            this.sketch.AddStrokes(strokes);
        }

        /// <summary>
        /// Removes stroke data from a Microsoft Ink object and converts it into a sketch
        /// friendly format
        /// </summary>
        /// <param name="ink">A Microsoft Ink object</param>
        /// <returns>An ArrayList containing Sketch stroke data</returns>
        public static ArrayList CreateConvertedStrokes(Ink ink)
        {
            ArrayList strokes = new ArrayList();

            int i;
            int strokeLength;

            strokeLength = ink.Strokes.Count;

            //Goes through the ink object, converts each stroke to a sketch stroke and adds
            //it to the ArrayList
            for (i = 0; i < strokeLength; ++i)
            {
                strokes.Add(StripStrokeData(ink.Strokes[i],null,false));
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
                
            if (inkStroke.ExtendedProperties[Microsoft.Ink.StrokeProperty.TimeID] != null)
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
                theTime = (ulong)System.DateTime.Now.Ticks;
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

            // Grab height and width of total stroke
            float height = boundingBox.Height;
            float width = boundingBox.Width;

            // Grab penTip
            string penTip = drawingAttributes.PenTip.ToString();

            // Grab raster
            string raster = drawingAttributes.RasterOperation.ToString();


            float x = Convert.ToSingle(boundingBox.X);
            float y = Convert.ToSingle(boundingBox.Y);

            //float leftx = Convert.ToSingle(boundingBox.Left);
            //float topy = Convert.ToSingle(boundingBox.Top);



            // If the pressure data is included take it, otherwise set to 255/2's
            int[] pressureData;
            if (IsPropertyIncluded(inkStroke, Microsoft.Ink.PacketProperty.NormalPressure))
                pressureData = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.NormalPressure);
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
            if (IsPropertyIncluded(inkStroke, Microsoft.Ink.PacketProperty.TimerTick))
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
            converterStroke.XmlAttrs.Id = System.Guid.NewGuid();
            converterStroke.XmlAttrs.Name = "stroke";
            converterStroke.XmlAttrs.Time = (ulong)theTime;
            converterStroke.XmlAttrs.Type = "stroke";
            converterStroke.XmlAttrs.Source = "Converter";

            Sketch.Substroke substroke = new Sketch.Substroke();
            substroke.XmlAttrs.Id = System.Guid.NewGuid();
            substroke.XmlAttrs.Name = "substroke";
            substroke.XmlAttrs.Time = theTime;
            substroke.XmlAttrs.Type = "substroke";
            substroke.XmlAttrs.Color = color;
            substroke.XmlAttrs.Height = height;
            substroke.XmlAttrs.Width = width;
            substroke.XmlAttrs.PenTip = penTip;
            substroke.XmlAttrs.Raster = raster;
            substroke.XmlAttrs.X = x;
            substroke.XmlAttrs.Y = y;
            //substroke.XmlAttrs.LeftX =		leftx;
            //substroke.XmlAttrs.TopY =		topy;
            substroke.XmlAttrs.Source = "ConverterJnt";
            substroke.XmlAttrs.Start = idData[0];
            substroke.XmlAttrs.End = idData[idData.Length - 1];

            // Add all of the points to the stroke			
            length = inkStroke.PacketCount;
            for (i = 0; i < length; i++)
            {
                Sketch.Point toAdd = new Sketch.Point();
                toAdd.XmlAttrs.Name = "point";
                toAdd.XmlAttrs.X = Convert.ToSingle(xData[i]);
                toAdd.XmlAttrs.Y = Convert.ToSingle(yData[i]);
                toAdd.XmlAttrs.Pressure = Convert.ToUInt16(pressureData[i]);
                toAdd.XmlAttrs.Time = Convert.ToUInt64(adjustedTime[i]);
                toAdd.XmlAttrs.Id = idData[i];

                substroke.AddPoint(toAdd);
            }

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

        #endregion

    }
}
