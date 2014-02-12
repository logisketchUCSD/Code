using System;
using System.Collections;
using System.IO;

namespace TestRig
{
	/// <summary>
	/// Represents a stage in the execution stack.
	/// </summary>
	public abstract class ProcessStage
	{

		internal bool verbose				= false;

		/// <summary>
		/// An English name for the module. Used when printing out error messages.
		/// </summary>
		public string name					= "Default";
		
        /// <summary>
		/// A three- or four-letter abbreviation of your module's name. This is appended to the end of 
		/// intermediary filenames, e.g. myfile.frag.xml
		/// </summary>
		public string shortname				= "dflt";
		
        /// <summary>
		/// If defined, TestRig will only load files that end in filetype.
		/// </summary>
		public string inputFiletype         = null;

        /// <summary>
        /// TestRig will call "writeToFile" on a file of this type for this ProcessStage. Default: ".txt"
        /// </summary>
        public string outputFiletype = ".txt";

		/// <summary>
		/// This function is called if the user attempts to pass any arguments to this module.
		/// Default behavior: Exit with error
		/// </summary>
		/// <param name="args">An array of string arguments passed to the module. Only arguments
		/// specifically passed to the module are contained here.</param>
		public virtual void processArgs(string[] args)
		{
			for(int i=0; i<args.Length; i++)
			{
				switch(args[i])	
				{
						// add cases here
					default:
						TestRig.ExitWithError("(" + this.name + ") Unrecognized flag: " + args[i]);
						break;
				}
			}
		}

        /// <summary>
        /// Called after processArgs but before run is ever called.
        /// </summary>
        public virtual void start() { }


        /// <summary>
        /// This is called on each input file as TestRig iterates over the list.
        /// </summary>
        /// <param name="sketch">a Sketch with the correct labels/groupings</param>
        /// <param name="filename">the file that the Sketch was loaded from</param>
        public abstract void run(Sketch.Sketch sketch, string filename);

		/// <summary>
		/// Called on the last stage after processing the last file in a run.  Called on 
        /// every stage after its last input file if the intermediaries flag is set.
		/// </summary>
        /// <param name="handle">TextWriter to write output to.</param>
		/// <param name="path">The path to the file e.g. "C:\folder\" where you can write auxillary files</param>
        public abstract void finalize(TextWriter handle, string path);

		/// <summary>
		/// Called after writeToFile(...). Most useful for test or analysis modules.
		/// Default behavior: do nothing
		/// </summary>
		public virtual void finalize()	
		{
			return;
		}
	}
}
