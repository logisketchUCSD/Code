using Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Sketch;
using Utilities.Concurrency;

namespace TestRig
{
    /// <summary>
    /// A platform for testing modular code on large bodies of data.
    /// </summary>
    class TestRig
    {
        
        #region Main

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                printUsage();
            }
            else
            {
                TestRig theRig = new TestRig(args);
                theRig.run();
            }
        }

        #endregion

        #region Internals

        // The Sketch to use
        private Sketch.Sketch sketch;

        // Should each stage output to a file?
        private bool generateIntermediaries;
        private string intermediaryPath;

        // Where each Stage should write its output
        private string outputPath;

        // The execution queue
        private Queue<ProcessStage> executionQueue = new Queue<ProcessStage>();

        // Used for "Do not warn again"
        private List<string> doNotWarn = new List<string>();

        // List of files to iterate over
        private List<string> files = new List<string>();

        // Output to console if true, file if false
        private bool consoleOutput = false;

        // Verbose output?
        private bool verbose = false;

        // Custom locations to write stage output to
        private Dictionary<ProcessStage, TextWriter> outputLocations;

        // True if we should continue even when a process stage throws an error
        private bool catchExceptions = false;

        // True if we should pause at the end of the test and ask the user to press
        // enter to continue.
        private bool pause = true;

        #endregion

        #region Constructors

        public TestRig()
        {
            generateIntermediaries = false;
            intermediaryPath = "intermediaries\\";

            outputPath = "output\\";

            outputLocations = new Dictionary<ProcessStage, TextWriter>();
        }

        public TestRig(string[] args)
            : this()
        {
            ParseArgs(args);
        }

        /// <summary>
        /// Initialize default execution queue.
        /// </summary>
        private void initDefaultExecutionQueue()
        {
            executionQueue.Enqueue(getStage("c"));
            executionQueue.Enqueue(getStage("g"));
        }

        #endregion

        #region Getting Stages from strings

        /// <summary>
        /// Get the stage for a particular name.
        /// </summary>
        /// <param name="name">a 1-letter code identifying the stage</param>
        /// <returns>a new instance of the stage</returns>
        private ProcessStage getStage(string name)
        {
            switch (name)
            {
                case "c": return new ClassifyStage();
                case "g": return new GroupStage();
                case "y": return new SymbolStage();
                case "a": return new AllStage();
                case "u": return new UserHoldoutStage();
                case "p": return new PipelineStage();
                case "f": return new FeatureSketchStage();
                case "i": return new AdaptiveImage();
                case "v": return new GateTimeVarianceStage();
                case "t": return new AIRPhaseOut();
            }
            return null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the output location for a specific stage. NOTE: the writer will
        /// be closed when the stage finishes writing.
        /// </summary>
        /// <param name="stage">the stage to set</param>
        /// <param name="writer">the object for the stage to write to</param>
        public void setOutputLocation(ProcessStage stage, TextWriter writer)
        {
            outputLocations.Add(stage, writer);
        }

        /// <summary>
        /// Add a stage to the execution queue.
        /// </summary>
        /// <param name="stage">the stage to add</param>
        public void addStage(ProcessStage stage)
        {
            executionQueue.Enqueue(stage);
        }

        /// <summary>
        /// Get a timestamp that can be embedded in a file name.
        /// </summary>
        /// <returns>a string with no slashes or other special characters</returns>
        public static string fileTimeStamp()
        {
            DateTime now = DateTime.Now;
            return String.Format("{0:yyyy-MM-dd_hh-mm-ss}", now);
        }

        #endregion

        #region Adding files to process

        /// <summary>
        /// Add a file to the list of files to process.
        /// </summary>
        /// <param name="name">path to the file</param>
        /// <returns>True if file was added successfully, or false if the file does not exist</returns>
        public bool AddFile(string name)
        {
            if (System.IO.File.Exists(name))
            {
                files.Add(name);
                return true;
            }
            return false;
        }

        #endregion

        #region Running tests

        /// <summary>
        /// Run the given stage on a sketch. This method is guaranteed not to modify
        /// the given sketch.
        /// </summary>
        /// <param name="stage">the stage</param>
        /// <param name="sketch">the sketch</param>
        /// <param name="filename">the name of the file the sketch was loaded from</param>
        /// <param name="writeIntermediary">true if intermediate files should be written</param>
        private void runStageOnSketch(ProcessStage stage, Sketch.Sketch sketch, string filename, bool writeIntermediary)
        {
            Sketch.Sketch mutable = new Sketch.Sketch(sketch);
            if (verbose) Console.WriteLine("Executing stage " + stage.name);

            stage.run(mutable, filename);

            if (writeIntermediary)
            {
                TextWriter handle;
                if (consoleOutput) handle = Console.Out;
                else
                {
                    handle = new StreamWriter(intermediaryPath + "\\" +
                        stage.shortname + "_" + fileTimeStamp() + stage.outputFiletype);
                }
                stage.finalize(handle, intermediaryPath);
                if (!consoleOutput) handle.Close();
            }
        }

        /// <summary>
        /// Run the TestRig
        /// </summary>
        public void run()
        {

            DateTime startTime = DateTime.Now;

            // Start loading all sketches asynchronously
            AsyncProducer<string, Sketch.Sketch> sketches = new AsyncProducer<string, Sketch.Sketch>(files, delegate(string file) { return loadFile(file); });

            // Print the files to be loaded
            Console.WriteLine("Files to be processed ("+files.Count+"):");
            foreach (string filename in files)
                Console.WriteLine("    " + filename);

            ProcessStage stage = null;

            foreach (ProcessStage theStage in executionQueue)
            {
                theStage.start();
            }

            for (int i = 0; i < files.Count; i++)
            {
                string Filename = files[i];
                #region EXECUTE STAGE QUEUE

                // Get a local copy of the execution queue to mess with
                Queue<ProcessStage> localExecQueue = new Queue<ProcessStage>(executionQueue);

				string File_path = System.IO.Path.GetDirectoryName(Filename);
				string File_name = System.IO.Path.GetFileNameWithoutExtension(Filename);
				string File_extn = System.IO.Path.GetExtension(Filename);
				if (File_extn == "")
					ExitWithError(Filename + " does not have an extension.");


                string pointer = "---(" + (i+1) + "/" + files.Count + ")------------------------> ";
                string name = File_name + File_extn;
                string top = "";
                int len = pointer.Length + name.Length + 1;
                for (int m = 0; m < len; ++m)
                    top += '_';

                Console.WriteLine(top);
                Console.WriteLine(pointer + File_name + File_extn + " |");
                Console.WriteLine(top + "|");

                sketch = sketches.Consume();
                
                while (localExecQueue.Count != 0)
                {
                    stage = localExecQueue.Dequeue();
                    bool writeIntermediary = generateIntermediaries && localExecQueue.Count != 0 && i == files.Count - 1;

                    if (catchExceptions)
                    {
                        try
                        {
                            runStageOnSketch(stage, sketch, Filename, writeIntermediary);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                    else
                    {
                        runStageOnSketch(stage, sketch, Filename, writeIntermediary);
                    }

                }

                // Write output
                if (i == files.Count - 1)
                {
                    foreach (ProcessStage outputStage in executionQueue)
                    {

                        TextWriter outHandle = null;
                        string outputFile = "";
                        try
                        {
                            if (consoleOutput) outHandle = Console.Out;
                            else
                            {
                                if (outputLocations.ContainsKey(outputStage))
                                {
                                    outHandle = outputLocations[outputStage];
                                }
                                else
                                {
                                    outputFile = outputPath + outputStage.shortname +
                                        "_" + fileTimeStamp();
                                    string outputFileName = outputFile + outputStage.outputFiletype;
                                    if (System.IO.File.Exists(outputFileName))
                                        outputFileName = outputFile + "_0" + outputStage.outputFiletype;
                                    outHandle = new StreamWriter(outputFileName);
                                    Console.WriteLine("Writing output to file " + outputFileName);
                                }
                            }
                            outputStage.finalize(outHandle, outputPath);
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                            Console.WriteLine("Due to various errors, there will be no output.");
                        }
                        finally
                        {
                            if (!consoleOutput && outHandle != null) outHandle.Close();
                        }
                    }
                }

                #endregion
            }


            Console.WriteLine("------------------------------------");
            DateTime endTime = DateTime.Now;
            TimeSpan elapsedTime = endTime - startTime;
            Console.WriteLine("Execution took " + elapsedTime.TotalSeconds + "s");

            // Cleanup
            // Allow each stage to do some post-processing (usually only necessary for test stages)
            while (executionQueue.Count != 0)
            {
                ProcessStage current_stage = (ProcessStage)executionQueue.Dequeue();
                current_stage.finalize();
            }

            if (pause)
            {
                Console.WriteLine("Press ENTER to continue...");
                Console.ReadLine();
            }

        }

        #endregion

        #region Loading sketch files

        /// <summary>
		/// If the module is first in the execution queue, this function will be called on a series of 
		/// filenames.
		/// Default behavior: Standard loading behavior for .jnt and .xml files
		/// </summary>
		/// <param name="filename">A filename to load</param>
		/// <returns>The sketch that is loaded.</returns>
		private static Sketch.Sketch loadFile(string filename)
		{
            Sketch.Sketch sketch;

            // Load
			if (Files.FUtil.FileType(filename) == Files.Filetype.XML)
			{
				try
				{
					ConverterXML.ReadXML xmlReader = new ConverterXML.ReadXML(filename);
					sketch = xmlReader.Sketch;
				}
				catch (Exception e)
				{
                    Console.Error.WriteLine("oops, caught exception {0}", e);
                    return null;
				}
			}
			else if (Files.FUtil.FileType(filename) == Files.Filetype.JOURNAL)
			{
				int numPages = ConverterJnt.ReadJnt.NumberOfPages(filename);

				if (numPages > 1)
				{
					Console.WriteLine("WARNING: Detected more than one page in journal file '" + filename + "'. Just using the first one...");
				}
				ConverterJnt.ReadJnt ink = new ConverterJnt.ReadJnt(filename, 1);
				sketch = ink.Sketch;
			}
			else
			{
				Console.WriteLine("WARNING: {0} is an unsupported filetype", filename);
				return null;
			}

            List<Sketch.Substroke> substrokesToRemove = new List<Sketch.Substroke>();

            // Fill in stroke classifications based on shape types, since this info 
            // isn't saved in the XML or JNT files.
            foreach (Sketch.Shape shape in sketch.Shapes)
            {
                if (shape.Type.Equals(new ShapeType()))
                {
                    Console.WriteLine("Ignoring shape with unknown type: " + shape);
                    substrokesToRemove.AddRange(shape.Substrokes);
                }
                else
                {
                    // In making this assignment we update the classification for the
                    // substrokes and the shape itself.
                    shape.Type = shape.Type;

                    // Make absolutely sure we are free to alter the shape
                    shape.AlreadyLabeled = false;
                    shape.AlreadyGrouped = false;
                    shape.AlreadyOriented = false;
                }
            }

            // Remove shapes with unknown types
            foreach (Substroke substroke in substrokesToRemove)
                sketch.RemoveSubstroke(substroke);

            sketch.CheckConsistency();

            return sketch;
		}

		#endregion

		#region User Interaction

		/* parse command line arguments */
        private void ParseArgs(string[] args)
        {
            int i = 0;
            int numFilesToUse = -1;

            Dictionary<string, ProcessStage> stages = new Dictionary<string, ProcessStage>();

            //----- Stage Selection (has to happen before other flags are processed)
            if (args[0] == "-s")
            {
                #region STAGE SELECTION

                i++;
                if (i == args.Length)
                    ExitWithError("You must specify set of stages to use");

                executionQueue = new Queue<ProcessStage>();
                string stage_string = args[i];

                for (int n = 0; n < stage_string.Length; n++)
                {
                    string stage_char = stage_string.Substring(n, 1);
                    ProcessStage stage = getStage(stage_char);
                    if (stage == null)
                        ExitWithError("Unrecognized stage flag: " + stage_char);
                    stages[stage_char] = stage;
                    Console.WriteLine("Enqueued stage: " + stage.name);
                    executionQueue.Enqueue(stage);
                }
                i++;

                #endregion
            }

            for (; i < args.Length; i++)
            {
                #region PROCESS STAGE-SPECIFIC FLAGS

                if (args[i].StartsWith("["))
                {
                    #region SANITY CHECK - ARG LENGTH
                    if (args[i].Length < 2)
                    {
                        ExitWithError("You must supply a stage identifier after \"[\" ");
                    }
                    #endregion

                    string stage_flag = args[i].Substring(1, args[i].Length - 1);

                    #region SANITY CHECK - STAGE EXISTS
                    ProcessStage stage = stages[stage_flag];
                    if (stage == null)
                    {
                        ExitWithError("Unrecognized stage flag: " + stage_flag);
                    }
                    #endregion

                    i++;
                    // Group all of the arguments to pass into an ArrayList
                    ArrayList stage_args = new ArrayList();
                    for (; i < args.Length; i++)
                    {
                        if (args[i].EndsWith("]"))
                        {
                            if (args[i].Length > 1)		// just ignore it if args[i] = "]"
                            {
                                stage_args.Add(args[i].Substring(0, args[i].Length - 1));
                            }
                            i++;
                            break;		// we're done
                        }
                        else
                        {
                            stage_args.Add(args[i]);
                        }
                    }

                    string[] stage_args_array = (string[])stage_args.ToArray(typeof(string));

                    stage.processArgs(stage_args_array);

                    if (i == args.Length)
                        break;
                }

                #endregion

                switch (args[i])
                {
                    case "-v":
                        #region VERBOSE OUTPUT
                        verbose = true;
						foreach (ProcessStage ps in executionQueue)
						{
							ps.verbose = true;
						}
                        break;
                        #endregion

                    case "-catch":
                        #region CATCH EXCEPTIONS
                        catchExceptions = true;
                        break;
                        #endregion

                    case "-nopause":
                        #region DO NOT PAUSE AT END
                        pause = false;
                        break;
                        #endregion

                    case "-c":
                        #region CONSOLE OUTPUT
                        consoleOutput = true;
                        break;
                        #endregion

                    case "-i":
                        #region ACTIVATE INTERMEDIARIES

                        if (executionQueue.Count > 2)
                            generateIntermediaries = true;
                        else
                            Console.WriteLine("WARNING: There are no intermediate stages. Ignoring -i flag...");
                        break;

                        #endregion

                    case "-I":
                        #region SET INTERMEDIARIES DIRECTORY

                        i++;
                        if (i == args.Length)
                            ExitWithError("You must specify a directory");

                        if (executionQueue.Count < 2)
                        {
                            Console.WriteLine("WARNING: There are no intermediate stages. Ignoring -I flag...");
                            break;
                        }

                        generateIntermediaries = true;

                        intermediaryPath = args[i];

                        if (!intermediaryPath.EndsWith("\\"))
                            intermediaryPath += "\\";

                        break;

                        #endregion

                    case "-d":
                        #region LOAD FILES FROM DIRECTORY

                        // Get directory arg
                        i++;
                        if (i == args.Length)
                            ExitWithError("You must specify a directory");

                        string directoryArg = args[i];
                        i++;

                        if (!System.IO.Directory.Exists(directoryArg))
                            ExitWithError("Cannot find directory: " + directoryArg);

                        bool excluding = false;
                        bool containsing = false;

                        List<string> contains_keywords = new List<string>();
                        List<string> exclude_keywords = new List<string>();

                        // Look for other flags
                        bool stop = false;
                        for (; i < args.Length; i++)
                        {
                            char[] delimeter = { ',' };
                            switch (args[i])
                            {
                                case "-contains":
                                    i++;
                                    if (i == args.Length)
                                        ExitWithError("You must specify a filename contains string");
                                    containsing = true;

                                    contains_keywords.AddRange(args[i].Split(delimeter));

                                    break;
                                case "-exclude":
                                    i++;
                                    if (i == args.Length)
                                        ExitWithError("You must specify a filename exclusion string");
                                    excluding = true;

                                    exclude_keywords.AddRange(args[i].Split(delimeter));

                                    break;
                                default:
                                    // Unrecognized flag! go back to main recog loop
                                    stop = true;
                                    break;
                            }

                            if (stop)
                                break;
                        }

                        string search_pattern = "*";

                        string[] dirFiles = System.IO.Directory.GetFiles(directoryArg, search_pattern);
                        //string[] dirFiles = System.IO.Directory.GetFiles(directoryArg);

                        foreach (string file in dirFiles)
                        {
                            bool passesExclusion = true;
                            bool passesInclusion = true;
                            if (excluding)
                            {
                                foreach (string exclude_string in exclude_keywords)
                                {
                                    if (file.IndexOf(exclude_string) >= 0)
                                    {
                                        passesExclusion = false;
                                        break;
                                    }
                                }
                            }
                            if (containsing)
                            {
                                foreach (string contains_string in contains_keywords)
                                {
                                    if (file.IndexOf(contains_string) < 0)
                                    {
                                        passesInclusion = false;
                                        break;
                                    }
                                }
                            }

                            if (passesExclusion && passesInclusion)
                            {
                                //Console.WriteLine("Adding " + file);
                                files.Add(file);
                            }
                        }
                        break;

                        #endregion

                    case "-f":
                        #region LOAD FILES LISTED IN TEXT FILE

                        // get file arg
                        i++;
                        if (i == args.Length)
                            ExitWithError("You must specify a file to load target filenames from.");

                        string fileList = args[i];

                        if (!System.IO.File.Exists(fileList))
                            ExitWithError("Could not find file: " + fileList);

                        // Read in lines from the file
                        System.IO.StreamReader reader = new System.IO.StreamReader(fileList);

                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (System.IO.File.Exists(line))
                                files.Add(line);
                            else
                                Console.WriteLine("WARNING: Cannot find " + line + "\n" +
                                                  "Ignoring it for now.");
                        }
                        break;

                        #endregion

                    case "-o":
                        #region OUTPUT SPECIFICATION

                        i++;
                        if (i == args.Length)
                            ExitWithError("You must specify an output directory.");
                        outputPath = args[i];

                        if (!outputPath.EndsWith("\\"))
                            outputPath += "\\";

                        break;

                        #endregion

                    case "-n":
                        #region USE ONLY N FILES
                        i++;
                        if (i == args.Length)
                            ExitWithError("You must specify how many files to keep (-n [number])");
                        string numStr = args[i];
                        int n;
                        if (int.TryParse(numStr, out n))
                            numFilesToUse = n;
                        else
                            ExitWithError("Argument '-n' must be followed by a number");
                        #endregion
                        break;

                    case "-r":
                        #region SEARCH RECURSIVELY
                        i++;
                        if (i == args.Length)
                            ExitWithError("You must specify a directory (-r [dir])");

                        string da = args[i];

                        files.AddRange(System.IO.Directory.GetFiles(da, "*", System.IO.SearchOption.AllDirectories));
                        break;
                        #endregion

                    default:
                        // Assume that it's a filename
                        #region ADD FILENAME

                        string filename = args[i];

                        if (System.IO.File.Exists(filename))
                            files.Add(filename);
                        else
                            ExitWithError("Cannot find file '" + filename + "'");

                        break;

                        #endregion
                }
            }

            #region POSTPROCESSING
            // Postprocessing
            // Things we can't think about until we've processed all the flags

            // files
            if (files.Count == 0)
            {
                Console.WriteLine("WARNING: No input files specified. Doing nothing...");
                //exitWithError("You must designate at least one source file.");
            }
            else
            {
                if (generateIntermediaries && !System.IO.Directory.Exists(intermediaryPath))
                {
                    warnAndConfirm("intermediaries_args", "Intermediary directory " + intermediaryPath + " doesn't exist. Create it?");
                    System.IO.Directory.CreateDirectory(intermediaryPath);
                }

                if (!System.IO.Directory.Exists(outputPath))
                {
                    warnAndConfirm("output_args", "Output directory " + outputPath + " doesn't exist. Create it?");
                    System.IO.Directory.CreateDirectory(outputPath);
                }
            }

            if (executionQueue.Count == 0)
            {
                initDefaultExecutionQueue();
            }

            // If we only wanted to run a limited test, keep a random subset of the files
            if (numFilesToUse >= 0 && numFilesToUse < files.Count)
            {
                MathNet.Numerics.Combinatorics.RandomShuffle(files);
                List<string> newFiles = new List<string>(numFilesToUse);
                for (i = 0; i < numFilesToUse; i++)
                    newFiles.Add(files[i]);
                files = newFiles;
            }

            // Sort the input files
            files.Sort();
            #endregion
        }

        /// <summary>
        /// Prints out errorMessage, prints a small help message, and closes the program.
        /// </summary>
        /// <param name="errorMessage">The error message that is printed. Is preceded by "ERROR: "</param>
        public static void ExitWithError(string errorMessage)
        {
            Console.WriteLine("ERROR: " + errorMessage);
            printHelp();
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
            Environment.Exit(2);
        }

        /// <summary>
        /// Prints out a warning and asks the user if they want to continue (answers are yes, no, yes to all).
        /// Returns immediately if the user answers yes, quits the program if the user answers no.
        /// If the user answers yes to all, further warnings from the same referrer will return immediately.
        /// </summary>
        /// <param name="referrer">A string idetifying the piece of code throwing the warning. This is used by the yes to all functionality</param>
        /// <param name="warning">Print out "WARNING: " + warning string</param>
        public void warnAndConfirm(string referrer, string warning)
        {
            Console.WriteLine("WARNING: " + warning);

            if (doNotWarn.Contains(referrer))
            {
                return;
            }

            Console.WriteLine("   Continue? (Y)es, (N)o, Yes to (A)ll");
            Console.Write(">");
            string response = Console.ReadLine();

            switch (response.ToLower())
            {
                case "y":
                case "yes":
                    // do nothing
                    break;
                case "n":
                case "no":
                    Console.WriteLine("Closing...");
                    Environment.Exit(0);
                    break;
                case "a":
                case "all":
                case "yestoall":
                    doNotWarn.Add(referrer);
                    break;
            }
        }

        static void printHelp()
        {
            Console.WriteLine("Call with no flags for usage information.");
        }

        static void printUsage()
        {
            #region USAGE INFORMATION
            Console.WriteLine(@"
  
  Usage:	TestRig.exe [-s stage(s)] [module-specific flags] [-i] source(s) 
            [-o destination]
 
  -s stage(s) - Run a specific (set of) stage(s)
        c      : classification stage
        g      : grouping stage
        y      : symbol recognition stage
        a      : run all of the above stages in sequence (do not combine with other stages)
        d      : debug stage (arbitrary code)
  NOTE: You can specify multiple stages, e.g. -s flg
        If you do so, each stage will be run in isolation.
        To run multiple stages in sequence on the same file,
        use the 'a' stage.

  NOTE: The -s argument must be first.
 
  module-specific flags - pass specific instructions to a stage
     [stageA -flag1 input1 -flag2 input2.. ] [stageB -flag1...
     NOTE: the brackets [] above do not denote optional attribtues
       stageA, stageB               : the stage you want to pass the args to
     Example: TestRig.exe -s g [g -color] myfile.xml 

 -v            - verbose output
 -i            - generate intermediary files (raw .xml, .labeled.xml, etc.) in 
                    the source's directory.
 -I directory  - specify the directory in which to place intermediary files
 -c            - output to standard out instead of files
 
 source(s)        - specify which files to process
    filename.extn                   :   a single filename, there can be any 
                                        number of these
    -d directory [-contains string] :   use all files in directory. If -c is
                                        specified, then only filenames that
                                        contain string will be processed
    -d directory [-exclude string]  :   same as above, except exclude filenames
                                        containing string
    -f listfile                     :   read in filenames from listfile. One
                                        file per line.
    NOTE: These can be combined with one another
 
 -o destination	- specify a destination directory into which to place files. 
			 ");
            #endregion
		}

		#endregion 

        #region Getters and Setters

        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; }
        }

        public bool Pause
        {
            get { return pause; }
            set { pause = value; }
        }

        #endregion
    }
}