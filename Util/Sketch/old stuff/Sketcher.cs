/**
 * File: Sketcher.cs
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
	/// A sketcher element in sketch.
	/// </summary>
	public class Sketcher : AbstractXML
	{
		/// <summary>
		/// A dpi element in sketcher.
		/// </summary>
		public class Dpi : AbstractXML
		{
			private string x, y;

			/// <summary>
			/// Create a Dpi class.
			/// </summary>
			/// <param name="xdpi">decimal. dpi for x axis</param>
			/// <param name="ydpi">decimal. dpi for y axis</param>
			public Dpi(string xdpi, string ydpi)
			{
				this.x = xdpi;
				this.y = ydpi;
			}

			/// <summary>
			/// Write this element out to a document
			/// </summary>
			/// <param name="xml">The document to write out to</param>
			public override void writeXML(XmlTextWriter xml)
			{
				if(x != null && y != null)
				{
					xml.WriteStartElement("dpi");
					xml.WriteAttributeString("x", x);
					xml.WriteAttributeString("y", y);
					xml.WriteEndElement();
				}
			}

			/// <summary>
			/// Get X
			/// </summary>
			public string X
			{
				get
				{
					return x;
				}
			}
			
			/// <summary>
			/// Get Y
			/// </summary>
			public string Y
			{
				get
				{
					return y;
				}
			}
		}


		private string id, nickname;

		private Dpi dpi;

		/// <summary>
		/// Create a Sketcher class.
		/// </summary>
		/// <param name="id">the id of the sketcher</param>
		public Sketcher(string id)
		{
			this.id = id;
		}

		/// <summary>
		/// Write this element out to a document
		/// </summary>
		/// <param name="xml">The document to write out to</param>
		public override void writeXML(XmlTextWriter xml)
		{
			xml.WriteStartElement("sketcher");
			
			xml.WriteStartElement("id");
			xml.WriteString(id);
			xml.WriteEndElement();
			
			if(nickname != null)
			{
				xml.WriteStartElement("nickname");
				xml.WriteString(nickname);
				xml.WriteEndElement();
			}

			if(dpi != null)
				dpi.writeXML(xml);

			xml.WriteEndElement();			
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
		/// Get or set Nickname
		/// </summary>
		public string Nickname
		{
			get
			{
				return nickname;
			}
			set
			{
				nickname = value;
			}
		}

		/// <summary>
		/// Get or set DPI
		/// </summary>
		public Dpi DPI
		{
			get
			{
				return dpi;
			}

			set
			{
				dpi = value;
			}
		}
	}
}
