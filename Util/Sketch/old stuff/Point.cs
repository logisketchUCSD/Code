/**
 * File: Point.cs
 * 
 * Notes: Part of the namespace to be used with JntToXml
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 */

using System;
using System.Collections;
using System.Xml;

namespace Converter
{
	/// <summary>
	/// A point element in a sketch
	/// </summary>
	public class Point : AbstractXML
	{
		private string x, y, pressure, time, id, name;

		/// <summary>
		/// Construct a new Point class.
		/// </summary>
		/// <param name="x">decimal. x coordinate</param>
		/// <param name="y">decimal. y coordinate</param>
		/// <param name="pressure">decimal. pressure for the point (0-255 on Tablet PC)</param>
		/// <param name="time">positive integer. time point was created in milliseconds since 1/1/1970 UTC</param>
		/// <param name="id">UUID. UUID for point</param>
		/// <param name="name">string. name of the point</param>
		public Point(string x, string y, string pressure, string time, string id, string name)
		{
			this.x = x;
			this.y = y;
			this.pressure = pressure;
			this.time = time;
			this.id = id;
			this.name = name;
		}

		/// <summary>
		/// Write this element out
		/// </summary>
		/// <param name="xmlDocument">The document to write out to</param>
		public override void writeXML(XmlTextWriter xmlDocument)
		{
			xmlDocument.WriteStartElement("point");
			xmlDocument.WriteAttributeString("x", x);
			xmlDocument.WriteAttributeString("y", y);
			xmlDocument.WriteAttributeString("pressure", pressure);
			xmlDocument.WriteAttributeString("time", time);
			xmlDocument.WriteAttributeString("id", id);
			xmlDocument.WriteAttributeString("name", name);
			xmlDocument.WriteEndElement();
		}
		
		/// <summary>
		/// Compute the distance between this point and another point
		/// </summary>
		/// <param name="p">Another point</param>
		/// <returns>The distance between them</returns>
		public double distance(Point p)
		{
			double x2 = Math.Pow(Convert.ToInt32(this.x) - Convert.ToInt32(p.x), 2.0);
			double y2 = Math.Pow(Convert.ToInt32(this.y) - Convert.ToInt32(p.y), 2.0);
            return Math.Sqrt(x2 + y2);
		}

		/// <summary>
		/// Get X
		/// </summary>
		public double X
		{
			get
			{
				return Convert.ToDouble(x);
			}
		}

		/// <summary>
		/// Get Y
		/// </summary>
		public double Y
		{
			get
			{
				return Convert.ToDouble(y);
			}
		}

		/// <summary>
		/// Get Pressure
		/// </summary>
		public ushort Pressure
		{
			get
			{
				return Convert.ToUInt16(pressure);
			}
		}

		/// <summary>
		/// Get Time
		/// </summary>
		public ulong Time
		{
			get
			{
				return Convert.ToUInt64(time);
			}
		}

		/// <summary>
		/// Get Id
		/// </summary>
		public string Id
		{
			get
			{
				return id;
			}
		}

		/// <summary>
		/// Get Name
		/// </summary>
		public string Name
		{
			get
			{
				return name;
			}
		}
	}
}
