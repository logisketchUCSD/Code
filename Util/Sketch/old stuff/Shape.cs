/**
 * File: Shape.cs
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
	/// A shape element in sketch.
	/// </summary>
	public class Shape : AbstractXML
	{
		//Optional
		//Shapes are may be (and are primarily) composed of Arg subcomponents that
		//reference the type of Arg ("Point") and the id
		//which is the GUID.

		/// <summary>
		/// An arg element in sketch.
		/// </summary>
		public class Arg : AbstractXML
		{
			private string type, id;

			/// <summary>
			/// Construct an Arg class.
			/// </summary>
			/// <param name="type">tring. type of the argument (e.g., "Point")</param>
			/// <param name="id">UID. UUID of the argument</param>
			public Arg(string type, string id)
			{
				this.type = type;
				this.id = id;			
			}

			/// <summary>
			/// Write this element out to a document
			/// </summary>
			/// <param name="xmlDocument">The document to write out to</param>
			public override void writeXML(XmlTextWriter xmlDocument)
			{
				xmlDocument.WriteStartElement("arg");
				xmlDocument.WriteAttributeString("type", type);
				xmlDocument.WriteString(id);
				xmlDocument.WriteEndElement();
			}

			/// <summary>
			/// Get Type
			/// </summary>
			public string Type
			{
				get
				{
					return type;
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
		}

		//Optional
		//Shapes sometimes are composed of Alias subcomponents that
		//reference the type of Alias ("Point"), the name of the Alias ("head"),
		//and the id which is the GUID.
		
		/// <summary>
		/// An alias element in sketch.
		/// </summary>
		public class Alias : AbstractXML
		{
			private string type, name, id;

			/// <summary>
			/// Construct an Alias class.
			/// </summary>
			/// <param name="type">string. type of the alias (e.g., "Point")</param>
			/// <param name="name">string. the alias of the alias (e.g., "head")</param>
			/// <param name="id">UUID. UUID of the alias</param>
			public Alias(string type, string name, string id)
			{
				this.type = type;
				this.name = name;
				this.id = id;
			}
			
			/// <summary>
			/// Write this element out to a document
			/// </summary>
			/// <param name="xmlDocument">The document to write out to</param>
			public override void writeXML(XmlTextWriter xmlDocument)
			{
				xmlDocument.WriteStartElement("alias");
				xmlDocument.WriteAttributeString("type", type);
				xmlDocument.WriteAttributeString("name", name);
				xmlDocument.WriteString(id);
				xmlDocument.WriteEndElement();
			}

			/// <summary>
			/// Get Type
			/// </summary>
			public string Type
			{
				get
				{
					return type;
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
		}
		
		/// <summary>
		/// Properties of a shape
		/// </summary>
		protected string id, name, time, type, author, color, height, width, area, laysInk, 
			orientation, penTip, raster, substrokeOf, p1, p2, x, y, text, leftx, topy,
			control1, control2, start, end, source;

		/// <summary>
		/// An array of arguments
		/// </summary>
		protected ArrayList args;

		/// <summary>
		/// An array of aliases
		/// </summary>
		protected ArrayList aliases;

		/// <summary>
		/// The different types of shapes
		/// </summary>
		public enum ShapeType { STROKE = 1, SUBSTROKE = 2, CONVERTED = 3, LABELED = 4, CLUSTERED = 5 };

		/// <summary>
		/// Construct a Shape class. 
		/// </summary>
		/// <param name="id">UUID. shape UUID</param>
		/// <param name="name">string. name of the shape.</param>
		/// <param name="time">positive integer. time shape was created in milliseconds since 1/1/1970 UTC</param>
		/// <param name="type">string. The type of the shape (e.g., "Stroke")</param>
		public Shape(string id, string name, string time, string type)
		{
			this.id = id;
			this.name = name;
			this.time = time;
			this.type = type;

			args = new ArrayList();
			aliases = new ArrayList();
		}

		/// <summary>
		/// Computes whether the given shape is of type shapeType
		/// </summary>
		/// <param name="shapeType">The ShapeType</param>
		/// <returns>Whether the shape matches with the type</returns>
		public bool matches(ShapeType shapeType)
		{
			string type;
			string source;
			switch(shapeType)
			{
				case(ShapeType.STROKE):
					//If we are a stroke and are not null
					type = this.Type;
					return (type.ToLower().Equals("stroke") && this.SubstrokeOf == null);

				case(ShapeType.SUBSTROKE):
					//If we are a stroke or substroke and we have a parent
					type = this.Type;
					return ((type.ToLower().Equals("stroke") || type.ToLower().Equals("substroke")) && this.SubstrokeOf != null);
		
				case(ShapeType.CONVERTED):
					 source = this.Source;
					return source.ToLower().StartsWith("converter");

				case(ShapeType.LABELED):
					source = this.Source;
					return source.ToLower().StartsWith("labeler");

				case(ShapeType.CLUSTERED):
					source = this.Source;
					return source.ToLower().StartsWith("cluster");

				default:
					return false;
			}
		}

		/// <summary>
		/// Write this element out to a document
		/// </summary>
		/// <param name="xmlDocument">The document to write out to</param>
		public override void writeXML(XmlTextWriter xmlDocument)
		{
			xmlDocument.WriteStartElement("shape");

			xmlDocument.WriteAttributeString("id", id);
			xmlDocument.WriteAttributeString("name", name);
			xmlDocument.WriteAttributeString("time", time);
			xmlDocument.WriteAttributeString("type", type);

			if(author != null)
				xmlDocument.WriteAttributeString("author", author);
			if(color != null)
				xmlDocument.WriteAttributeString("color", color);
			if(height != null)
				xmlDocument.WriteAttributeString("height", height);
			if(width != null)
				xmlDocument.WriteAttributeString("width", width);
			if(area != null)
				xmlDocument.WriteAttributeString("area", area);
			if(laysInk != null)
				xmlDocument.WriteAttributeString("laysInk", laysInk);
			if(orientation != null)
				xmlDocument.WriteAttributeString("orientation", orientation);
			if(penTip != null)
				xmlDocument.WriteAttributeString("penTip", penTip);
			if(raster != null)
				xmlDocument.WriteAttributeString("raster", raster);
			if(substrokeOf != null)
				xmlDocument.WriteAttributeString("substrokeOf", substrokeOf);
			if(p1 != null)
				xmlDocument.WriteAttributeString("p1", p1);
			if(p2 != null)
				xmlDocument.WriteAttributeString("p2", p2);
			if(x != null)
				xmlDocument.WriteAttributeString("x", x);
			if(y != null)
				xmlDocument.WriteAttributeString("y", y);
			if(text != null)
				xmlDocument.WriteAttributeString("text", text);
			if(leftx != null)
				xmlDocument.WriteAttributeString("leftx", leftx);
			if(topy != null)
				xmlDocument.WriteAttributeString("topy", topy);
			if(control1 != null)
				xmlDocument.WriteAttributeString("control1", control1);
			if(control2 != null)
				xmlDocument.WriteAttributeString("control2", control2);
			if(start != null)
				xmlDocument.WriteAttributeString("start", start);
			if(end != null)
				xmlDocument.WriteAttributeString("end", end);
			if(source != null)
				xmlDocument.WriteAttributeString("source", source);


			foreach(Arg a in args)
				a.writeXML(xmlDocument);

			foreach(Alias a in aliases)
				a.writeXML(xmlDocument);

			xmlDocument.WriteEndElement();
		}
		
		/// <summary>
		/// Get Args
		/// </summary>
		public ArrayList Args
		{
			get
			{
				return args;
			}
		}

		/// <summary>
		/// Get Aliases
		/// </summary>
		public ArrayList Aliases
		{
			get
			{
				return aliases;
			}
		}

		/// <summary>
		/// Get or set Id
		/// </summary>
		public string Id
		{
			get
			{
				return id;
			}

			set
			{
				id = value;
			}
		}

		/// <summary>
		/// Get or set Name
		/// </summary>
		public string Name
		{
			get
			{
				return name;
			}

			set
			{
				name = value;
			}
		}

		/// <summary>
		/// Get or set Time
		/// </summary>
		public string Time
		{
			get
			{
				return time;
			}

			set
			{
				time = value;
			}
		}

		/// <summary>
		/// Get or set Type
		/// </summary>
		public string Type
		{
			get
			{
				return type;
			}

			set
			{
				type = value;
			}
		}

		/// <summary>
		/// Get or set Author
		/// </summary>
		public string Author
		{
			get
			{
				return author;
			}
			
			set 
			{
				author = value;
			}
		}

		/// <summary>
		/// Get or set Color
		/// </summary>
		public string Color
		{
			get
			{
				return color;
			}

			set
			{
				color = value;
			}
		}

		/// <summary>
		/// Get or set Height
		/// </summary>
		public string Height
		{
			get
			{
				return height;
			}

			set
			{
				height = value;
			}
		}

		/// <summary>
		/// Get or set Width
		/// </summary>
		public string Width
		{
			get
			{
				return width;
			}

			set 
			{
				width = value;
			}
		}

		/// <summary>
		/// Get or set Area
		/// </summary>
		public string Area
		{
			get
			{
				return area;
			}

			set
			{
				area = value;
			}
		}

		/// <summary>
		/// Get or set LaysInk
		/// </summary>
		public string LaysInk
		{
			get
			{
				return laysInk;
			}

			set
			{
				laysInk = value;
			}
		}

		/// <summary>
		/// Get or set Orientation
		/// </summary>
		public string Orientation
		{
			get
			{
				return orientation;
			}

			set
			{
				orientation = value;
			}
		}

		/// <summary>
		/// Get or set PenTip
		/// </summary>
		public string PenTip
		{
			get
			{
				return penTip;
			}
		
			set
			{
				penTip = value;
			}
		}

		/// <summary>
		/// Get or set Raster
		/// </summary>
		public string Raster
		{
			get
			{
				return raster;
			}
		
			set
			{
				raster = value;
			}
		}

		/// <summary>
		/// Get or set SubstrokeOf
		/// </summary>
		public string SubstrokeOf
		{
			get
			{
				return substrokeOf;
			}
		
			set
			{
				substrokeOf = value;
			}
		}

		/// <summary>
		/// Get or set P1
		/// </summary>
		public string P1
		{
			get
			{
				return p1;
			}
		
			set
			{
				p1 = value;
			}
		}

		/// <summary>
		/// Get or set P2
		/// </summary>
		public string P2
		{
			get
			{
				return p2;
			}
		
			set
			{
				p2 = value;
			}
		}

		/// <summary>
		/// Get or set X
		/// </summary>
		public string X
		{
			get
			{
				return x;
			}
		
			set
			{
				x = value;
			}
		}

		/// <summary>
		/// Get or set Y
		/// </summary>
		public string Y
		{
			get
			{
				return y;
			}
		
			set
			{
				y = value;
			}
		}

		/// <summary>
		/// Get or set Text
		/// </summary>
		public string Text
		{
			get
			{
				return text;
			}
		
			set
			{
				text = value;
			}
		}

		/// <summary>
		/// Get or set LeftX
		/// </summary>
		public string LeftX
		{
			get
			{
				return leftx;
			}
		
			set
			{
				leftx = value;
			}
		}

		/// <summary>
		/// Get or set TopY
		/// </summary>
		public string TopY
		{
			get
			{
				return topy;
			}
			
			set
			{
				topy = value;
			}
		}

		/// <summary>
		/// Get or set Control1
		/// </summary>
		public string Control1
		{
			get
			{
				return control1;
			}

			set
			{
				control1 = value;
			}
		}

		/// <summary>
		/// Get or set Control2
		/// </summary>
		public string Control2
		{
			get
			{
				return control2;
			}

			set
			{
				control2 = value;
			}
		}

		/// <summary>
		/// Get or set Start
		/// </summary>
		public string Start
		{
			get
			{
				return start;
			}

			set
			{
				start = value;
			}
		}
		
		/// <summary>
		/// Get or set End
		/// </summary>
		public string End
		{
			get
			{
				return end;
			}
			
			set
			{
				end = value;
			}
		}
		
		/// <summary>
		/// Get or set Source
		/// </summary>
		public string Source
		{
			get
			{
				return source;
			}

			set
			{
				source = value;
			}
		}

		/// <summary>
		/// Compute an array of all the argument ID's
		/// </summary>
		/// <returns>An array of ID's</returns>
		public ArrayList getArgIds()
		{
			ArrayList ret = new ArrayList();
			ArrayList args = this.Args;
			foreach (Arg a in args) 
			{
				if (a.Type == "Point" || a.Type == "point") 
				{
					ret.Add(a.Id);
				}
			}
			return ret;
		}	
	}
}
