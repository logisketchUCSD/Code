using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Featurefy;

namespace Featureizer
{
	/// <summary>
	/// The class that actually does the work for Featureizer
	/// </summary>
	internal class SketchFeatureizer
	{
		#region Internals

		private string _filename;
		private string _outFilename;
		private FeatureSketch _fs;
		private Sketch.Sketch _s;

		#endregion

		#region Constructor

		/// <summary>
		/// Create a featureizer for the given sketch
		/// </summary>
		/// <param name="filename">The filename of the sketc</param>
		public SketchFeatureizer(string filename)
		{
			_filename = filename;
			ConverterXML.ReadXML reader = new ConverterXML.ReadXML(_filename);
			_s = reader.Sketch;
			_outFilename = _filename.Replace(Files.FUtil.Extension(Files.Filetype.XML), Files.FUtil.Extension(Files.Filetype.FEATURESKETCH));
		}

		#endregion

		/// <summary>
		/// Featureize the provided sketch
		/// </summary>
		public void Featureize()
		{
			
			_fs = new FeatureSketch(ref _s);
		}

		/// <summary>
		/// Fragment the sketch
		/// </summary>
		public void Fragment()
		{
			Fragmenter.Fragment.fragmentSketch(_s);
			_outFilename = _outFilename.Replace(Files.FUtil.Extension(Files.Filetype.FEATURESKETCH), ".fragged" + Files.FUtil.Extension(Files.Filetype.FEATURESKETCH));
		}

		/// <summary>
		/// Save the given sketch to the same location as its original name, but with its extension replaced with the featureized sketch extension
		/// </summary>
		public void Save()
		{
			Save(_outFilename);
		}

		/// <summary>
		/// Save the given featureized sketch to the specified filename
		/// </summary>
		/// <param name="fn">The filename to save to</param>
		public void Save(string fn)
		{
			_fs.writeToFile(fn);
		}
	}

	/// <summary>
	/// This classe Featureizes sketches and serializes the <see cref="FeatureSketch" /> and <see cref="FeatureStroke" /> objects
	/// </summary>
	public class Featureizer
	{
		public static void Main(string[] args)
		{
			// Fragment the sketch?
			bool frag = false;

			#region Argument Handling

			if (args.Length < 2)
			{
				printUsage();
				Environment.Exit(1);
			}

			List<string> files = new List<string>();
			// States:
			// 0: looking for flags
			// 1: looking for filenames or flags
			// 2: looking for directories or flags
			uint state = 0;
			foreach (string arg in args)
			{
				if (arg[0] == '-')
				{
					switch (arg[1])
					{
						case 'f':
							state = 1;
							break;
						case 'd':
							state = 2;
							break;
						case 'F':
							frag = true;
							break;
					}
				}
				else
				{
					if (state == 1)
						files.Add(arg);
					else if (state == 2)
						files.AddRange(GetFilesFromDirectory(arg));
				}
			}

			#endregion

			Console.WriteLine("Beginning featureization on {0} files", files.Count);
			if (frag)
				Console.WriteLine("Will be fragmenting all sketches");
			Console.Write("00% Complete");
			int total = files.Count;
			int idx = 0;
			foreach (string filename in files)
			{
				double percent = (double)idx / (double)total;
				Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b");
				if (percent > 0.995)
					Console.Write("\b");
				Console.Write("{0:00%} Complete", percent);
				++idx;
				SketchFeatureizer sf = new SketchFeatureizer(filename);
				if (frag)
					sf.Fragment();
				sf.Featureize();
				sf.Save();
			}
			Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b");
			Console.WriteLine("100% Complete");
		}

		/// <summary>
		/// Print usage information for this program
		/// </summary>
		public static void printUsage()
		{
			Console.WriteLine("Featureizer Usage:");
			Console.WriteLine("\tFeatureizer.exe [-f filename*] [-d directory*]");
			Console.WriteLine();
			Console.WriteLine("Flags:");
			Console.WriteLine("\t -f filename1 ...        Loads the specified files and featureizes them");
			Console.WriteLine("\t -d directory1 ...       Loads the specified directories' .XML files and featureizes them");
			Console.WriteLine("\t -F                      Auto-fragment the sketch before featurizeing");
		}

		#region Helper Functions

		private static List<string> GetFilesFromDirectory(string directory)
		{
			if (!Directory.Exists(directory))
			{
				Console.WriteLine("Directory {0} not found", directory);
				Environment.Exit(2);
			}
			return new List<string>(Directory.GetFiles(directory, "*.xml"));
		}

		#endregion
	}
}
