/*
 * File: FUtil.cs
 *
 * Author: James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 * 
 * This file contains utility functions related to files
 */

using System;
using System.Collections.Generic;

namespace Files
{
	/// <summary>
	/// Filetypes known to us
	/// </summary>
	public enum Filetype
	{
		/// <summary>
		/// MIT XML Sketch Data
		/// </summary>
		XML,
		/// <summary>
		/// Microsoft Journal data
		/// </summary>
		JOURNAL,
		/// <summary>
		/// Custom matrix format
		/// </summary>
		MATRIX,
		/// <summary>
		/// Preprocessed data
		/// </summary>
		PREPROCESSED_DATA,
		/// <summary>
		/// Training files for the Congealer
		/// </summary>
		CONGEALER_TRAINING_DATA,
		/// <summary>
		/// Canonical gate example
		/// </summary>
		CANONICAL_EXAMPLE,
		/// <summary>
		/// DRS Data file
		/// </summary>
		DRS,
		/// <summary>
		/// Featureized sketch
		/// </summary>
		FEATURESKETCH,
		/// <summary>
		/// Unknown
		/// </summary>
		OTHER
	};

	public static class FUtil
	{
		/// <summary>
		/// Get the type of a file. Currently entirely based on extensions
		/// </summary>
		/// <param name="filename">The filename to get the type of</param>
		/// <returns>The file's type</returns>
		public static Filetype FileType(string filename)
		{
			if (filename.EndsWith(".xml"))
				return Filetype.XML;
			else if (filename.EndsWith(".jnt"))
				return Filetype.JOURNAL;
			else if (filename.EndsWith(".amat") || filename.EndsWith(".imat"))
				return Filetype.MATRIX;
			else if (filename.EndsWith(".xtd"))
				return Filetype.PREPROCESSED_DATA;
			else if (filename.EndsWith(".m4a"))
				return Filetype.CONGEALER_TRAINING_DATA;
			else if (filename.EndsWith(".cxtd"))
				return Filetype.CANONICAL_EXAMPLE;
			else if (filename.EndsWith(".fsxml"))
				return Filetype.FEATURESKETCH;
			else
				return Filetype.OTHER;
		}

		/// <summary>
		/// Get the appropriate extension for the given filetype (includes the ".")
		/// </summary>
		/// <param name="ft">The FileType to get the extension for</param>
		/// <returns>The extension corresponding to that filetype</returns>
		public static string Extension(Filetype ft)
		{
			switch (ft)
			{
				case Filetype.XML:
					return ".xml";
				case Filetype.JOURNAL:
					return ".jnt";
				case Filetype.MATRIX:
					return ".amat"; // Do we want .amat or .imat?
				case Filetype.PREPROCESSED_DATA:
					return ".xtd";
				case Filetype.CONGEALER_TRAINING_DATA:
					return ".m4a";
				case Filetype.CANONICAL_EXAMPLE:
					return ".cxtd";
				case Filetype.FEATURESKETCH:
					return ".fsxml";
				default:
					throw new Exception("File type not recognized");
			}
		}

		/// <summary>
		/// Convert from a standard open file filter index to a Filetype object
		/// </summary>
		/// <param name="p">The index</param>
		/// <returns>The corresponding Filetype to p</returns>
		public static Filetype OpenFilterIndexToFileType(int p)
		{
			switch (p)
			{
				case 1:
					return Filetype.XML;
				case 2:
					return Filetype.JOURNAL;
				case 3:
					return Filetype.DRS;
				default:
					return Filetype.OTHER;
			}
		}

		/// <summary>
		/// Convert from a standard save file filter index to a Filetype object
		/// </summary>
		/// <param name="p">The index</param>
		/// <returns>The Filetype corresponding to p</returns>
		public static Filetype SaveFilterIndexToFileType(int p)
		{
			switch (p)
			{
				case 1:
					return Filetype.XML;
				case 2:
					return Filetype.JOURNAL;
				default:
					return Filetype.OTHER;
			}
		}

		/// <summary>
		/// Get the standard "Open File" filter setup
		/// </summary>
		public static string OpenFilter
		{
			get
			{
				return "MIT XML sketches (*.xml)|*.xml" +
				"|Microsoft Windows Journal Files (*.jnt)|*.jnt" +
				"|Magical Mystery DRS File (*.drs)|*.drs";
			}
		}

		/// <summary>
		/// Get the standard "Save File" filter setup
		/// </summary>
		public static string SaveFilter
		{
			get
			{
				return "MIT XML sketch (.xml)|*.xml" +
					"|Microsoft Windows Journal File (.jnt)|*.jnt";
			}
		}

		/// <summary>
		/// Make sure that the given filename ends with the appropriate extension for the given filetype
		/// </summary>
		/// <param name="filename">The filename</param>
		/// <param name="filetype">The filetype of that file</param>
		/// <returns>The corrected file extension</returns>
		public static string EnsureExtension(string filename, Filetype filetype)
		{
			if (filename.EndsWith(Extension(filetype)))
				return filename;
			return filename + Extension(filetype);
		}

        /// <summary>
        /// Get a list of all files in a directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filetype"></param>
        /// <param name="recursive">whether or not to search subfolders as well (default = false)</param>
        /// <returns>a list of filenames</returns>
        public static string[] AllFiles(string path, Filetype filetype, bool recursive = false)
        {
            System.IO.SearchOption options = recursive ? 
                System.IO.SearchOption.AllDirectories : 
                System.IO.SearchOption.TopDirectoryOnly;
            string pattern = "*" + Extension(filetype);
            return System.IO.Directory.GetFiles(path, pattern, options);
        }
	}
}
