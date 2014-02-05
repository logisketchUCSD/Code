/*
 * File: XmlStructs.cs
 *
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Collections.Generic;

namespace Sketch
{
	/// <summary>
	/// XmlStructs. Contains structs holding variables for relevant XML attributes.
	/// </summary>
	public abstract class XmlStructs
	{
		/// <summary>
		/// XML attribute struct for the Sketch data
		/// </summary>
		[Serializable]
		public struct XmlSketchAttrs
		{
			#region INTERNALS

			/// <summary>
			/// The GUID 
			/// </summary>
            private Guid _ID;

            /// <summary>
            /// A descriptor for the sketch.
            /// </summary>
            private string _name;

			/// <summary>
			/// Units of the Sketch
			/// <example>Units = "pixels"</example>
			/// </summary>
            public string _units;

			#endregion

			#region METHODS

            /// <summary>
            /// Create a new XmlSketchAttrs struct. You should always
            /// use this method instead of the default constructor. 
            /// The default units are "pixels".
            /// </summary>
            /// <param name="id">If you need to initialize a set of
            /// attributes with a specific ID, use this parameter.
            /// If null is passed, a new ID is created.</param>
            /// <returns></returns>
            public static XmlSketchAttrs CreateNew(string id = null)
            {
                XmlSketchAttrs attrs = new XmlSketchAttrs();
                attrs._units = "pixels";
                attrs._name = "unknown";
                attrs._ID = (id == null) ? Guid.NewGuid() : new Guid(id);
                return attrs;
            }

			/// <summary>
			/// Get the attribute names.
			/// </summary>
			/// <returns>A string array of attribute names</returns>
			public string[] getAttributeNames()
			{
				return new string[] { "id", "units", "name"};
			}


			/// <summary>
			/// Get the attribute values.
			/// </summary>
			/// <returns>An object array of attribute values</returns>
			public object[] getAttributeValues()
			{
				return new object[] { _ID, _units, _name };
			}

			
			/// <summary>
			/// Clone method.
			/// </summary>
			/// <returns>A new XmlSketchAttrs struct copying the old one's values</returns>
			public XmlSketchAttrs Clone()
			{
				XmlSketchAttrs temp = new XmlSketchAttrs();
				temp._ID = this.Id;
				temp._units = this._units;
				return temp;
			}

            /// <summary>
            /// The unique ID for this object, immutable.
            /// </summary>
            public Guid Id
            {
                get { return _ID; }
            }

            /// <summary>
            /// The unique ID for this object (mutable).
            /// </summary>
            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

			#endregion
		}

		
		/// <summary>
		/// XML attribute struct for the Shape data
		/// </summary>
		[Serializable]
		public struct XmlShapeAttrs
		{
			#region DATA

			/// <summary>
			/// GUID of the Shape
			/// </summary>
            private Guid _Id;

			/// <summary>
			/// Name of the Shape
			/// <example>shape</example>
			/// <example>point</example>
			/// </summary>
            public string Name;
			
			/// <summary>
			/// Time of the Shape
			/// </summary>
            public ulong? Time;

			/// <summary>
			/// Type of the Shape
			/// <example>AND</example>
			/// </summary>
            public string Type;

			/// <summary>
			/// Author GUID of the Shape
			/// </summary>
            public Guid? Author;

			/// <summary>
			/// Color of the Shape
			/// </summary>
            public int? Color;

			/// <summary>
			/// Height of the Shape
			/// </summary>
            public float? Height;

			/// <summary>
			/// Width of the Shape
			/// </summary>
            public float? Width;

			/// <summary>
			/// Area (Height x Width) of the Shape
			/// </summary>
            public float? Area;

            /// <summary>
            /// Orientation of the Shape (in radians)
            /// </summary>
            public float? Orientation;

			/// <summary>
			/// No clue what this does. TODO: Read the MITXML spec
			/// </summary>
            public bool? LaysInk;

			/// <summary>
			/// Pen tip used to draw the Shape
			/// </summary>
            public string PenTip;

			/// <summary>
			/// Are you raster?
			/// </summary>
            public string Raster;

			/// <summary>
			/// What stroke are you a subtroke of?
			/// </summary>
            public Guid? SubstrokeOf;

			/// <summary>
			/// P1
			/// </summary>
            public string P1;
			
			/// <summary>
			/// P2
			/// </summary>
            public string P2;

			/// <summary>
			/// X-Coordinate of the Shape
			/// </summary>
            public float? X;

			/// <summary>
			/// Y-Coordinate of the Shape
			/// </summary>
            public float? Y;

			/// <summary>
			/// Associated text
			/// </summary>
            public string Text;
			/// <summary>
			/// LeftX
			/// </summary>
            public float? LeftX;
			/// <summary>
			/// TopY
			/// </summary>
            public float? TopY;
			/// <summary>
			/// Control1
			/// </summary>
            public string Control1;
			/// <summary>
			/// Control 2
			/// </summary>
            public string Control2;
			/// <summary>
			/// Starting substroke
			/// </summary>
            public Guid? Start;
			/// <summary>
			/// Ending substroke
			/// </summary>
            public Guid? End;
			/// <summary>
			/// Source (i.e., who made this label?)
			/// </summary>
            public string Source;
			/// <summary>
			/// Pen width setting
			/// </summary>
            public float? PenWidth;
			/// <summary>
			/// Pen height setting
			/// </summary>
            public float? PenHeight;

			/// <summary>
			/// Type Probability of the Shape
			/// </summary>
            public float? Probability;

            /// <summary>
            /// Classification of the shape 
            /// </summary>
            public string Classification;

            /// <summary>
            /// Probability of the classification
            /// </summary>
            public float? ClassificationBelief;

            /// <summary>
            /// The dictionary of connected shapes.
            /// </summary>
            public HashSet<Shape> Connections;

            #endregion

            #region CONSTRUCTORS

            /// <summary>
            /// Construct a new shape attributes struct with the given ID.
            /// </summary>
            /// <param name="id"></param>
            public XmlShapeAttrs(Guid id)
                :this()
            {
                _Id = id;
                
                Connections = new HashSet<Shape>();
            }

            /// <summary>
            /// Create a new XmlShapeAttrs object with some fields
            /// initialized. The default constructor should never be used;
            /// only this method. It is here because C# does not allow
            /// structs to have zero-argument constructors.
            /// </summary>
            /// <returns>a new XmlShapeAttrs struct</returns>
            public static XmlShapeAttrs CreateNew()
            {
                XmlShapeAttrs attrs = new XmlShapeAttrs();
                attrs._Id = Guid.NewGuid();
                attrs.Connections = new HashSet<Shape>();
                return attrs;
            }

			/// <summary>
			/// Copy constructor. Note that this does not clone the other's
            /// ID, but instead obtains a new unique ID.
			/// </summary>
			/// <param name="other">The source</param>
            public XmlShapeAttrs(XmlShapeAttrs other)
            {
                _Id = Guid.NewGuid();
                Name = other.Name;
                Time = other.Time;
                Type = other.Type;
                Author = other.Author;
                Color = other.Color;
                Height = other.Height;
                Width = other.Width;
                Area = other.Area;
                LaysInk = other.LaysInk;
                Orientation = other.Orientation;
                PenTip = other.PenTip;
                Raster = other.Raster;
                SubstrokeOf = other.SubstrokeOf;
                P1 = other.P1;
                P2 = other.P2;
                X = other.X;
                Y = other.Y;
                Text = other.Text;
                LeftX = other.LeftX;
                TopY = other.TopY;
                Control1 = other.Control1;
                Control2 = other.Control2;
                Start = other.Start;
                End = other.End;
                Source = other.Source;
                PenWidth = other.PenWidth;
                PenHeight = other.PenHeight;
                Probability = other.Probability;
                Classification = other.Classification;
                ClassificationBelief = other.ClassificationBelief;
                Connections = other.Connections;
            }

            #endregion

            #region METHODS

            /// <summary>
			/// Get the attribute names.
			/// </summary>
			/// <returns>A string array of attribute names</returns>
			public string[] getAttributeNames()
			{
				return new string[] { "type", "name", "id", "time", "x", "y", "probability", "author", "color", 
										"height", "width", "area", "laysInk", "orientation", "penTip", "penWidth", 
                                        "penHeight", "raster", "substrokeOf", "p1", "p2", "text", "leftx", 
                                        "topy", "control1", "control2", "start", "end", "source", "classification", 
                                        "classificationBelief", "connections" };
			}

			
			/// <summary>
			/// Get the attribute values.
			/// </summary>
			/// <returns>An object array of attribute values</returns>
			public object[] getAttributeValues()
			{
                // Get the connections as a string
                string connectionsString = "";
                bool first = true;
                foreach (Shape shape in Connections)
                {
                    if (!first)
                        connectionsString += ",";
                    first = false;
                    connectionsString += shape.Id;
                }

				return new object[] {	Type, Name, Id, Time, X, Y, Probability, Author, Color,
										Height, Width, Area, LaysInk, Orientation, PenTip, PenWidth,
                                        PenHeight, Raster, SubstrokeOf, P1, P2, Text, LeftX, 
                                        TopY, Control1, Control2, Start, End, Source, Classification, 
                                        ClassificationBelief, connectionsString };
			}

			
			/// <summary>
			/// Clone method.
			/// </summary>
			/// <returns>A new XmlShapeAttrs struct copying the old one's values</returns>
			public XmlShapeAttrs Clone()
			{
				return new XmlShapeAttrs(this);
			}

			
			#endregion

            #region GETTERS

            /// <summary>
            /// Get the ID associated with this struct. Does not
            /// support set; IDs are immutable.
            /// </summary>
            public Guid Id
            {
                get { return _Id; }
            }

            #endregion
        }

		
		/// <summary>
		/// XML attribute struct for the Point data
		/// </summary>
		[Serializable]
		public struct XmlPointAttrs
		{
			#region	INTERNALS

			/// <summary>
			/// X-Coordinate of the Shape
			/// </summary>
            public readonly float X;
			/// <summary>
			/// Y-Coordinate of the Shape
			/// </summary> 
            public readonly float Y;

			/// <summary>
			/// Pen Pressure of the Point
			/// </summary>
            public readonly float? Pressure;

			/// <summary>
			/// Time the Point was drawn
			/// </summary>
            public readonly ulong? Time;

			/// <summary>
			/// Name of the Point
			/// </summary>
            public readonly string Name;

			/// <summary>
			/// The Point's GUID
			/// </summary>
            public readonly Guid Id;

			#endregion

			#region METHODS

            /// <summary>
            /// Construct a new set of XML point attributes
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="pressure"></param>
            /// <param name="time"></param>
            /// <param name="name"></param>
            /// <param name="id"></param>
            public XmlPointAttrs(float x, float y, float? pressure = null, ulong? time = null, string name = null, Guid? id = null)
            {
                X = x;
                Y = y;
                Pressure = pressure;
                Time = time;
                Name = name;
                Id = (id == null) ? Guid.NewGuid() : id.Value;
            }

			/// <summary>
			/// Get the attribute names.
			/// </summary>
			/// <returns>A string array of attribute names</returns>
			public string[] getAttributeNames()
			{
				return new string[] { "x", "y", "pressure", "time", "name", "id" };
			}

			
			/// <summary>
			/// Get the attribute values.
			/// </summary>
			/// <returns>An object array of attribute values</returns>
			public object[] getAttributeValues()
			{
				return new object[] { X, Y, Pressure, Time, Name, Id };
			}

			
			/// <summary>
			/// Clone method.
			/// </summary>
			/// <returns>A new XmlPointAttrs struct copying the old one's values</returns>
			public XmlPointAttrs Clone()
			{
                // Observe that we do, in fact, clone the ID. Because points are immutable, we can do that.
				return new XmlPointAttrs(X, Y, Pressure, Time, Name, Id);
			}
			
			#endregion		
		}
	}
}
