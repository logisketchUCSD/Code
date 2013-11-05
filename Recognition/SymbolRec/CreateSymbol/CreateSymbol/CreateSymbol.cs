using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using NeighborhoodMap;
using ConverterXML;
using SymbolRec.Image;
using System.IO;

namespace CreateSymbol
{
    /// <summary>
    /// Creates training data for the symbol recognition.
    /// </summary>
    public class CreateSymbol
    {
        #region Internals
        /// <summary>
        /// A labeled symbol with context
        /// </summary>
        private Sketch.Sketch sketch;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for CreateSymbol.
        /// </summary>
        /// <param name="sketch">The current sketch.</param>
        public CreateSymbol(Sketch.Sketch sketch)
        {
            this.sketch = sketch;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates training data for the entire symbol with no context.
        /// </summary>
        /// <param name="filename">Filename of the current sketch.</param>
        /// <param name="start">Number of the file.</param>
        private void noContext(string filename, string start)
        {
            string path = Path.GetFullPath(filename);
            string dir = Path.GetDirectoryName(path);

            List<Substroke> subs = new List<Substroke>();

            // Go through all the shapes and add all the substrokes to a list except if the shape is context
            foreach (Shape shape in this.sketch.Shapes)
            {
                if (shape.XmlAttrs.Type != "context")
                {
                    foreach (Substroke sub in shape.SubstrokesL)
                    {
                        subs.Add(sub);
                    }
                }
            }

            // Create the windowed image (which does not actually window anything since all the substrokes
            // are keys/not only neighbors)
            WindowedImage image = new WindowedImage(32, 32);
            image.CreateImage(subs, subs, CropMethod.DISTANCE);

            // Write the training file
            write(image.Cropped.SubStrokes, dir + "\\" + start + ".non.xml");

            //image.DI.writeToBitmap(dir + "\\" + start, "non.bmp");
            //image.DI.writeToFile(dir + "\\" + start + ".non.imat");
            //image.writeToBitmap(dir + "\\" + start + ".non.bmp");
            //image.writeToFile(dir + "\\" + start + ".non.imat").Close();
        }

        /// <summary>
        /// Creates training data for the entire symbol with cropped context.
        /// </summary>
        /// <param name="neighborhood">The neighborhood mapping of substrokes to neighboring substrokes of the sketch.</param>
        /// <param name="filename">The filename of the current sketch.</param>
        /// <param name="start">Number of the file.</param>
        private void croppedContextForSymbol(Neighborhood neighborhood, string filename, string start)
        {
            string path = Path.GetFullPath(filename);
            string dir = Path.GetDirectoryName(path);

            // Get all of the shapes that are not context
            List<Shape> nonContext = new List<Shape>();
            foreach (Shape shape in this.sketch.Shapes)
            {
                if (shape.XmlAttrs.Type != "context")
                    nonContext.Add(shape);
            }

            // Get all of the neighbors of the non-context shapes (which may include context)
            // and make sure only to add each substroke once
            List<Substroke> neighbors = new List<Substroke>();
            List<Substroke> keys = new List<Substroke>();
            foreach (Shape shape in nonContext)
            {
                foreach (Substroke sub in shape.SubstrokesL)
                {
                    keys.Add(sub);
                    foreach (Substroke neighbor in neighborhood.Graph[sub.XmlAttrs.Id.Value])
                    {
                        if (!neighbors.Contains(neighbor))
                            neighbors.Add(neighbor);
                    }
                }
            }

            // Create a windowed image that will include cropped context
            WindowedImage image = new WindowedImage(32, 32);
            image.CreateImage(keys, neighbors, CropMethod.DISTANCE);

            // Write the training file
            write(image.Cropped.SubStrokes, dir + "\\" + start + ".con.xml");

            //image.DI.writeToBitmap(dir + "\\" + start, "con.bmp");
            //image.DI.writeToFile(dir + "\\" + start + ".con.imat");
            //image.writeToBitmap(dir + "\\" + start + ".con.bmp");
            //image.writeToFile(dir + "\\" + start + ".con.imat").Close();
        }

        /// <summary>
        /// Create training data for a shape with its cropped context.
        /// </summary>
        /// <param name="neighborhood">The neighborhood mapping of substrokes to neighboring substrokes of the sketch.</param>
        /// <param name="filename">Filename of the current sketch.</param>
        /// <param name="start">Number of the file.</param>
        /// <param name="userID">User ID (usually four numbers in the filename)</param>
        private void croppedContextForShape(Neighborhood neighborhood, string filename, string start, string userID)
        {
            Sketch.Sketch croppedContextSketch = new Sketch.Sketch(this.sketch);
            string path = Path.GetFullPath(filename);
            string dir = Path.GetDirectoryName(path);

            // Go through every shape that is not context and write training data for each shape individually
            foreach (Shape shape in this.sketch.Shapes)
            {
                if (shape.XmlAttrs.Type != "context")
                {
                    // Get all of the neighbors of the shape
                    List<Substroke> neighbors = new List<Substroke>();
                    foreach (Substroke sub in shape.Substrokes)
                    {
                        foreach (Substroke neighbor in neighborhood.Graph[sub.XmlAttrs.Id.Value])
                        {
                            if (!neighbors.Contains(neighbor))
                                neighbors.Add(neighbor);
                        }
                    }

                    // Create new directory for the shape type if it does not exist
                    string newDirectory = dir + "\\" + shape.XmlAttrs.Type;
                    if (!Directory.Exists(newDirectory))
                    {
                        Directory.CreateDirectory(newDirectory);
                    }

                    // Create new directory for the userID if it does not exist 
                    string userDirectory = newDirectory + "\\" + userID;
                    if (!Directory.Exists(userDirectory))
                    {
                        Directory.CreateDirectory(userDirectory);
                    }

                    // Create the windowed image of the shape and its context
                    WindowedImage image = new WindowedImage(32, 32);
                    image.CreateImage(shape.SubstrokesL, neighbors, CropMethod.DISTANCE);

                    // Write the training file
                    write(image.Cropped.SubStrokes, userDirectory + "\\" + shape.XmlAttrs.Type + "." + start + ".con.xml");

                    //image.DI.writeToBitmap(userDirectory + "\\" + start + "." + shape.XmlAttrs.Type, "con.bmp");
                    //image.DI.writeToFile(userDirectory + "\\" + start + "." + shape.XmlAttrs.Type + ".con.imat");
                    //image.writeToBitmap(userDirectory + "\\" + start + "." + shape.XmlAttrs.Type + ".con.bmp");
                    //image.writeToFile(userDirectory + "\\" + start + "." + shape.XmlAttrs.Type + ".con.imat").Close();

                    // Create the windowed image of the shape without context
                    image = new WindowedImage(32, 32);
                    image.CreateImage(shape.SubstrokesL, shape.SubstrokesL, CropMethod.DISTANCE);

                    // Write the training file
                    write(image.Cropped.SubStrokes, userDirectory + "\\" + shape.XmlAttrs.Type + "." + start + ".non.xml");

                    //image.DI.writeToBitmap(userDirectory + "\\" + start + "." + shape.XmlAttrs.Type, "non.bmp");
                    //image.DI.writeToFile(userDirectory + "\\" + start + "." + shape.XmlAttrs.Type + ".non.imat");
                    //image.writeToBitmap(userDirectory + "\\" + start + "." + shape.XmlAttrs.Type + ".non.bmp");
                    //image.writeToFile(userDirectory + "\\" + start + "." + shape.XmlAttrs.Type + ".non.imat").Close();
                }
            }
        }

        /// <summary>
        /// Writes the training data in XML format.
        /// </summary>
        /// <param name="subs">The substrokes to be written.</param>
        /// <param name="filename">The name of the file to be written to.</param>
        private void write(Substroke[] subs, string filename)
        {
            // Make a new sketch
            Sketch.Sketch sketch = new Sketch.Sketch();
            sketch.XmlAttrs.Id = System.Guid.NewGuid();
            Sketch.Stroke stroke;

            // Initialize fields of the strokes
            foreach (Substroke s in subs)
            {
                stroke = new Stroke(s);
                stroke.XmlAttrs.Id = System.Guid.NewGuid();
                stroke.XmlAttrs.Time = s.XmlAttrs.Time;
                stroke.XmlAttrs.Name = "stroke";
                stroke.XmlAttrs.Type = "stroke";
                sketch.AddStroke(stroke);
            }
            // Write the XML
            (new ConverterXML.MakeXML(sketch)).WriteXML(filename);
        }

        #endregion

        #region Main

        /// <summary>
        /// Console application entry point to write all of the training data.
        /// </summary>
        /// <param name="args">The arguments provided by the user at the console.</param>
        public static void Main(string[] args)
        {
            List<string> argArray = new List<string>(args);
            int numArgs = argArray.Count;

            string[] files;
            string pattern = "*.xml";

            if(argArray.Contains("-p"))
            {
                int i = argArray.IndexOf("-p");
                if (i + 1 >= argArray.Count)	// Are we in range?
                {
                    Console.Error.WriteLine("No pattern specified.");
                    return;
                }
                else if( argArray[i+1].StartsWith("-") || !argArray[i+1].EndsWith(".xml"))
                {
                    Console.Error.WriteLine("invalid pattern specified.");
                    return;
                }
                else
                {
                    pattern = argArray[i+1];
                }
            }

            // Show how to run the program if there are no arguments provided
            if (numArgs == 0)
            {
                Console.WriteLine("*****************************************************************");
                Console.WriteLine("*** CreateSymbol.exe");
                Console.WriteLine("*** by Sara Sheehan and Matthew Weiner");
                Console.WriteLine("*** Harvey Mudd College, Claremont, CA 91711.");
                Console.WriteLine("*** Sketchers 2007");
                Console.WriteLine("***");
                Console.WriteLine("*** Usage: CreateSymbol.exe (-c | -d directory | -r)");
                Console.WriteLine("*** Usage: CreateSymbol.exe input1.xml [input2.xml ...]");
                Console.WriteLine("***");
                Console.WriteLine("*** -c: convert all files in current directory");
                Console.WriteLine("*** -d directory: convert all files in the specified directory");
                Console.WriteLine("*** -r directory: recursively convert files from the specified directory");
                Console.ReadLine();
                return;
            }
            else if (argArray.Contains("-c")) // Convert everything in this directory
            {
                files = Directory.GetFiles(Directory.GetCurrentDirectory());
            }
            else if (argArray.Contains("-d")) // Convert everything in specified directory
            {
                int index = argArray.IndexOf("-d");
                if (index + 1 >= argArray.Count)	// Are we in range?
                {
                    Console.Error.WriteLine("No directory specified.");
                    return; 
                }
                else if (!Directory.Exists(argArray[index + 1])) // Does dir exist?
                {
                    Console.Error.WriteLine("Directory doesn't exist.");
                    return;
                }
                else
                    files = Directory.GetFiles(argArray[index + 1], "*.xml", SearchOption.TopDirectoryOnly);
            }
            else if (argArray.Contains("-r")) //Recursive from current dir
            {
                int index = argArray.IndexOf("-r");
                if (index + 1 >= argArray.Count) //Are we in range?
                {
                    Console.Error.WriteLine("No directory specified.");
                    return;
                }
                else if (!Directory.Exists(argArray[index + 1])) // Does dir exist?
                {
                    Console.Error.WriteLine("Directory doesn't exist.");
                    return;
                }
                else
                    files = Directory.GetFiles(argArray[index + 1], pattern, SearchOption.AllDirectories);
            }
            else //Convert only the specified files
            {
                files = args;
            }

            foreach (string input in files)
            {
                //We know it ends with .xml from above, or if the user specified it, we should try to use it
                //if (Path.GetExtension(input) == ".xml")
                {
                    // Get information about the filename/path
                    string filename = Path.GetFileName(input);
                    string[] split = filename.Split(new char[] { '.' });
                    string path = Path.GetFullPath(filename);
                    string [] inputSplit = input.Split(new char[] { '\\' });

                    // Status printouts
                    Console.WriteLine(filename);
                    Console.WriteLine(input);
                    Console.WriteLine(path);

                    string newDirectory = "";
                    for (int i = 0; i < inputSplit.Length - 2; i++)
                    {
                        newDirectory += inputSplit[i] + "\\";
                    }

                    // Run the training data creation on the current file if it is labeled
                    if (split[1] == "labeled")
                    {
                        // Load the sketch
                        Sketch.Sketch sketch = (new ReadXML(input)).Sketch;

                        // Create a new sketch to manipulate
                        Sketch.Sketch noContextSketch = new Sketch.Sketch();

                        // Make a new CreateSymbol object
                        CreateSymbol createSymbol = new CreateSymbol(sketch);

                        // Create a neighborhood for this sketch
                        Neighborhood neighborhood = new Neighborhood(sketch);
                        neighborhood.createGraph(ClosenessMeasure.EUCLIDIAN);

                        // Run the training data creation functions
                        createSymbol.noContext(input, split[0]);
                        createSymbol.croppedContextForShape(neighborhood, newDirectory, split[0], inputSplit[inputSplit.Length-2]);
                        createSymbol.croppedContextForSymbol(neighborhood, input, split[0]);
                    }
                }
            }
        }

        #endregion
    }
}
