/**
 * File: Sketch.cs
 * 
 * Notes: Part of the namespace to be used with JntToXml
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 */

using System;
using System.Xml;
using System.Collections;

namespace Converter
{
	/// <summary>
	/// The root element of the XML document.
	/// </summary>
	public class Sketch : AbstractXML
	{
		private string id, units, study, domain;

		private ArrayList sketchers, points, shapes; //, edits, speeches, mediaInfos;

		private Hashtable idToPoint;
		private Hashtable idToShape;
		
		/// <summary>
		/// Create a Sketch class.
		/// </summary>
		/// <param name="id">UUID for sketch</param>
		public Sketch(string id)
		{
			this.id = id;

			sketchers = new ArrayList();
			points = new ArrayList();
			shapes = new ArrayList();
			
			/*
			edits = new ArrayList();
			speeches = new ArrayList();
			mediaInfos = new ArrayList();
			*/

			idToPoint = new Hashtable();
			idToShape = new Hashtable();
		}

		/// <summary>
		/// This should be called after points or shapes have been added/deleted/changed in the hashtable
		/// </summary>
		public void updateHashtables()
		{
			idToPoint.Clear();
			idToShape.Clear();

			foreach(Point p in this.Points)
				this.idToPoint.Add(p.Id, p);

			foreach(Shape s in this.Shapes)
			{
				this.idToShape[s.Id] =  s;
			}
		}

		/// <summary>
		/// Get Strokes
		/// </summary>
		public Stroke[] Strokes
		{
			get
			{
				//Create the strokes
				ArrayList strokes = new ArrayList();
				foreach(Shape shape in this.Shapes)
				{
					//Only if it is a stroke
					//Make it a real stroke, not a shape. That is, we need to add points
					if(shape.matches(Shape.ShapeType.STROKE))
					{
						Stroke stroke = new Stroke(shape);
					
						foreach(Shape.Arg arg in shape.Args)
							stroke.addPoint((Point)idToPoint[arg.Id]);
						
						strokes.Add(stroke);
					}
				}

				return (Stroke[])strokes.ToArray(typeof(Stroke));
			}
		}

		/// <summary>
		/// Get LabeledStrokes
		/// </summary>
		public Stroke[] LabeledStrokes
		{
			get
			{
				//Create the strokes
				ArrayList labels = new ArrayList();
				ArrayList strokes = new ArrayList();
				foreach(Shape label in this.Shapes)
				{
					//Only if it is a labeled shape
					if(label.matches(Shape.ShapeType.LABELED))
					{
						foreach(Shape.Arg arg in label.Args)
						{
							string guid = arg.Id;
							Shape shape = (Shape)this.IdToShape[guid];

							//Only if it is a stroke
							//Make it a real stroke, not a shape. That is, we need to add points
							if(shape.matches(Shape.ShapeType.STROKE) || shape.matches(Shape.ShapeType.SUBSTROKE))
							{
								Stroke stroke = new Stroke(shape);
								stroke.Name = label.Name;

								foreach(Shape.Arg arg2 in shape.Args)
									stroke.addPoint((Point)idToPoint[arg2.Id]);
						
								strokes.Add(stroke);
							}
						}
					}
				}

				return (Stroke[])strokes.ToArray(typeof(Stroke));
			}
		}

		/// <summary>
		/// Get Substrokes
		/// </summary>
		public Stroke[] Substrokes
		{
			get
			{
				//Create the strokes
				ArrayList strokes = new ArrayList();
				foreach(Shape shape in this.Shapes)
				{
					//Only if it is a substroke
					//Make it a real stroke, not a shape. That is, we need to add points
					if(shape.matches(Shape.ShapeType.SUBSTROKE))
					{
						Stroke stroke = new Stroke(shape);
					
						foreach(Shape.Arg arg in shape.Args)
							stroke.addPoint((Point)idToPoint[arg.Id]);
						
						strokes.Add(stroke);
					}
				}

				return (Stroke[])strokes.ToArray(typeof(Stroke));
			}
		}

		/// <summary>
		/// Get Converter shapes
		/// </summary>
		public Shape[] Converted
		{
			get
			{
				//Create the strokes
				ArrayList shapes = new ArrayList();
				foreach(Shape shape in this.Shapes)
				{
					//Only if it is a substroke
					//Make it a real stroke, not a shape. That is, we need to add points
					if(shape.matches(Shape.ShapeType.CONVERTED))
					{						
						shapes.Add(shape);
					}
				}

				return (Shape[])shapes.ToArray(typeof(Shape));
			}
		}

		/// <summary>
		/// Get Labeled shapes
		/// </summary>
		public Shape[] Labeled
		{
			get
			{
				//Create the strokes
				ArrayList shapes = new ArrayList();
				foreach(Shape shape in this.Shapes)
				{
					//Only if it is a substroke
					//Make it a real stroke, not a shape. That is, we need to add points
					if(shape.matches(Shape.ShapeType.LABELED))
					{						
						shapes.Add(shape);
					}
				}

				return (Shape[])shapes.ToArray(typeof(Shape));
			}
		}

		/// <summary>
		/// Get Clustered shapes
		/// </summary>
		public Shape[] Clustered
		{
			get
			{
				//Create the strokes
				ArrayList shapes = new ArrayList();
				foreach(Shape shape in this.Shapes)
				{
					//Only if it is a substroke
					//Make it a real stroke, not a shape. That is, we need to add points
					if(shape.matches(Shape.ShapeType.CLUSTERED))
					{						
						shapes.Add(shape);
					}
				}

				return (Shape[])shapes.ToArray(typeof(Shape));
			}
		}

		/// <summary>
		/// Get IdToPoint
		/// </summary>
		public Hashtable IdToPoint
		{
			get
			{
				return this.idToPoint;
			}
		}

		/// <summary>
		/// Get IdToShape
		/// </summary>
		public Hashtable IdToShape
		{
			get
			{
				return this.idToShape;
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
		/// Get or set Units
		/// </summary>
		public string Units
		{
			get
			{
				return units;
			}

			set
			{
				units = value;
			}
		}

		/// <summary>
		/// Get or set Study
		/// </summary>
		public string Study
		{
			get
			{
				return study;
			}

			set
			{
				study = value;
			}
		}

		/// <summary>
		/// Get or set Domain
		/// </summary>
		public string Domain
		{
			get
			{
				return domain;
			}

			set
			{
				domain = value;
			}
		}

		/// <summary>
		/// Get Sketchers
		/// </summary>
		public ArrayList Sketchers
		{
			get
			{
				return sketchers;
			}
		}

		/// <summary>
		/// Get Points
		/// </summary>
		public ArrayList Points
		{
			get
			{
				return points;
			}
		}

		/// <summary>
		/// Get Shapes
		/// </summary>
		public ArrayList Shapes
		{
			get
			{
				return shapes;
			}
		}

		/*
		/// <summary>
		/// Get Edits
		/// </summary>
		public ArrayList Edits
		{
			get
			{
				return edits;
			}
		}

		/// <summary>
		/// Get Speeches
		/// </summary>
		public ArrayList Speeches
		{
			get
			{
				return speeches;
			}
		}

		/// <summary>
		/// Get MediaInfos
		/// </summary>
		public ArrayList MediaInfos
		{
			get
			{
				return mediaInfos;
			}
		}
		*/

		/// <summary>
		/// Write this element out to a document
		/// </summary>
		/// <param name="xmlDocument">The document to write out to</param>
		public override void writeXML(XmlTextWriter xmlDocument)
		{
			//Write the root element
			xmlDocument.WriteStartElement("sketch");
			
			//add the root id
			xmlDocument.WriteAttributeString("id", id);
			
			//Set the units
			if(units != null)
				xmlDocument.WriteAttributeString("units", units);
			else
			{
				units = "himetric";
				xmlDocument.WriteAttributeString("units", units);
			}

			//Set the study
			if(study != null)
			{
				xmlDocument.WriteStartElement("study");
				xmlDocument.WriteString(study);
				xmlDocument.WriteEndElement();
			}
	
			//Set the domain
			if(domain != null)
			{
				xmlDocument.WriteStartElement("domain");
				xmlDocument.WriteString(domain);
				xmlDocument.WriteEndElement();
			}

			//Write out the sketchers
			foreach(Sketcher s in sketchers)
				s.writeXML(xmlDocument);

			//Write out the points
			foreach(Point p in points)
				p.writeXML(xmlDocument);

			//Write out the shapes
			foreach(Shape s in shapes)
				s.writeXML(xmlDocument);

			/*
			foreach(Edit e in edits)
				e.writeXml(xmlDocument);
			
			foreach(Speech s in speeches)
				s.writeXml(xmlDocument);
			
			foreach(MediaInfo m in mediaInfos)
				m.writeXml(xmlDocument);
			*/
		}
		
	}
}
