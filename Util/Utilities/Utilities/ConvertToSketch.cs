using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    public class ConvertToSketch
    {
        /// <summary>
        /// Creates a Substroke from Microsoft point data
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        static public Sketch.Substroke MakeSubstroke(Microsoft.Ink.Stroke stroke)
        {
            Sketch.Point[] SketchPoints = stripStroke(stroke);
            Sketch.XmlStructs.XmlShapeAttrs XmlAttrs = new Sketch.XmlStructs.XmlShapeAttrs(true);
            XmlAttrs.Time = SketchPoints[0].Time;
            XmlAttrs.Source = "InkOverlay";
            XmlAttrs.PenHeight = stroke.DrawingAttributes.Height;
            XmlAttrs.PenTip = stroke.DrawingAttributes.PenTip.ToString();
            XmlAttrs.PenWidth = stroke.DrawingAttributes.Width;
            XmlAttrs.Color = stroke.DrawingAttributes.Color.ToArgb();
            Sketch.Substroke s = new Sketch.Substroke(SketchPoints, XmlAttrs);

            return s;
        }

        /// <summary>
        /// Grabs the (x, y, time) data from the inkStroke
        /// </summary>
        /// <param name="inkStroke"></param>
        /// <returns></returns>
        static public Sketch.Point[] stripStroke(Microsoft.Ink.Stroke inkStroke)
        {
            ulong theTime;
            int strokeLength = inkStroke.PacketCount;
            ulong[] time = new ulong[strokeLength];

            if (inkStroke.ExtendedProperties.DoesPropertyExist(Utilities.General.theTimeGuid))
            {
                time = (ulong[])inkStroke.ExtendedProperties[Utilities.General.theTimeGuid].Data;
                if (time.Length > 0)
                    theTime = time[0];
                else
                {
                    theTime = ((ulong)DateTime.Now.ToFileTime() - 116444736000000000) / 10000;
                    for (int i = 0; i < strokeLength; i++)
                    {
                        // The multiplication by 1,000 is to convert from
                        // seconds to milliseconds.
                        //
                        // Our time is in the form of milliseconds since Jan 1, 1970
                        //
                        // NOTE: The timestamp for the stroke is made WHEN THE PEN IS LIFTED
                        time[i] = theTime - (ulong)((1 / Utilities.General.SAMPLE_RATE * 1000) * (strokeLength - i));
                    }
                }
            }
            else
            {
                theTime = ((ulong)DateTime.Now.ToFileTime() - 116444736000000000) / 10000;
                for (int i = 0; i < strokeLength; i++)
                {
                    // The multiplication by 1,000 is to convert from
                    // seconds to milliseconds.
                    //
                    // Our time is in the form of milliseconds since Jan 1, 1970
                    //
                    // NOTE: The timestamp for the stroke is made WHEN THE PEN IS LIFTED
                    time[i] = theTime - (ulong)((1 / Utilities.General.SAMPLE_RATE * 1000) * (strokeLength - i));
                }
            }

            bool includePressure = false;
            List<Guid> packetProperties = new List<Guid>(inkStroke.PacketDescription);
            if (packetProperties.Contains(Microsoft.Ink.PacketProperty.NormalPressure))
                includePressure = true;

            int[] x = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.X);
            int[] y = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.Y);
            int[] pressure = new int[x.Length];
            if (includePressure)
                pressure = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.NormalPressure);

            Sketch.Point[] points = new Sketch.Point[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                if (includePressure)
                    points[i] = new Sketch.Point((float)x[i], (float)y[i], (float)pressure[i]);
                else
                    points[i] = new Sketch.Point((float)x[i], (float)y[i]);

                points[i].Time = time[i];
            }

            return points;
        }
    }
}
