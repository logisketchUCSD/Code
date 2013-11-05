/*
 * File: Files.cs
 *
 * Author: Originally Sketchers 2007, modified by James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007-2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 * 
 */

using System.IO;
using System.Collections.Generic;

namespace Files
{
    /// <summary>
    /// Class searches and returns files given specific path, queries, inclusions, and exclusions.
    /// </summary>
    public class Files
    {
        /// <summary>
        /// List of files found
        /// </summary>
        private List<string> m_files;

        /// <summary>
        /// Process the given file
        /// </summary>
        /// <param name="file">Name of file to process</param>
        public delegate void Handler(string file);

        /// <summary>
        /// Get files related to the following format:
        /// 
        /// path query (-r | -d) [ (-inPath | -exPath | -inName | -exName) limiter ]*
        /// 
        /// or
        /// 
        /// path file0 [ file1 [file2 ...]].
        /// 
        /// path: path of directory
        /// 
        /// query: limiter, often a file format, ie *.jnt, *.xml ...
        /// 
        /// -r: recursive
        /// -d: top
        /// 
        /// 
        /// Example:
        /// 
        /// ./data *.jnt -r -exPath devin
        /// 
        /// ./files *.xml -d 
        /// 
        /// c:/cool/mix *.xml -r -inPath rectangle -exName square
        /// 
        /// ./smith file1.xml file2.jnt file3.txt file4.yay 
        /// 
        /// </summary>
        /// <param name="args"></param>
        public Files(string[] args)
        {
            int i, len = args.Length;
            if (len > 1)
            {                
                //Get directory
                string directory = args[0];
                if (!Directory.Exists(directory))
                    return;

                //Get file extension (*.jnt, *.xml, *.etc...)
                string ext = args[1];
                if (!ext.StartsWith("*"))
                {
                    List<string> files = new List<string>(len - 1);
                    for (i = 1; i < len; ++i)
                    {
                        if(Directory.Exists(directory + args[i]))
                            files.Add(directory + args[i]);
                    }
                    m_files = files;                
                }
                else if (len > 2)
                {
                    //Get the search type
                    SearchOption? option = null;
                    switch (args[2])
                    {
                        //Only the top directory
                        case "-d":
                            option = SearchOption.TopDirectoryOnly;
                            break;
                        //Recursive from directory
                        case "-r":
                            option = SearchOption.AllDirectories;
                            break;
                        default:
                            throw new System.ArgumentException("Missing -d or -r");
                    }
                    if (!option.HasValue)
                        return;

                    m_files = new List<string>(Directory.GetFiles(directory, ext, option.Value));
                    if (m_files.Count > 0)
                    {
                        //Process limiters
                        string type, limiter;
                        for (i = 3; i < len; ++i)
                        {
                            type = args[i];
                            ++i;
                            limiter = args[i];

                            switch (type)
                            {
                                case "-inPath":
                                    m_files = includeInPath(m_files, limiter);
                                    break;

                                case "-exPath":
                                    m_files = excludeInPath(m_files, limiter);
                                    break;
                                
                                case "-inName":
                                    m_files = includeInName(m_files, limiter);
                                    break;

                                case "-exName":
                                    m_files = excludeInName(m_files, limiter);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    throw new System.ArgumentException("Wrong number of arguments");
                }
            }
        }

        /// <summary>
        /// Run the Handler against all of the files found
        /// </summary>
        /// <param name="handler"></param>
        public void process(Handler handler)
        {
            int i, len = m_files.Count;
            for (i = 0; i < len; ++i)
            {
                handler.Invoke(m_files[i]);
            }
        }

        /// <summary>
        /// Compute whether each file path contains limiter
        /// </summary>
        /// <param name="files">Strings to process</param>
        /// <param name="limiter">String must include this</param>
        /// <returns>Strings that contain the limiter</returns>
        private List<string> includeInPath(List<string> files, string limiter)
        {
            int i, len = files.Count;
            List<string> nFiles = new List<string>(len);
            for (i = 0; i < len; ++i)
            {
                if (files[i].Contains(limiter))
                    nFiles.Add(files[i]);
            }
            return nFiles;
        }

        /// <summary>
        /// Compute whether each file name contains limiter
        /// </summary>
        /// <param name="files">Strings to process</param>
        /// <param name="limiter">Strings name must include this</param>
        /// <returns>Strings whose name contains the limiter</returns>
        private List<string> includeInName(List<string> files, string limiter)
        {
            int i, len = files.Count;
            List<string> nFiles = new List<string>(len);
            for (i = 0; i < len; ++i)
            {
                if(pathToFilename(files[i]).Contains(limiter))
                    nFiles.Add(files[i]);
            }
            return nFiles;
        }

        /// <summary>
        /// Compute whether each file path contains limiter
        /// </summary>
        /// <param name="files">Strings to process</param>
        /// <param name="limiter">String must not include this</param>
        /// <returns>Strings that do not contain the limiter</returns>
        private List<string> excludeInPath(List<string> files, string limiter)
        {
            int i, len = files.Count;
            List<string> nFiles = new List<string>(len);
            for (i = 0; i < len; ++i)
            {
                if (!files[i].Contains(limiter))
                    nFiles.Add(files[i]);
            }
            return nFiles;
        }

        /// <summary>
        /// Compute whether each file name contains limiter
        /// </summary>
        /// <param name="files">Strings to process</param>
        /// <param name="limiter">Strings name must not include this</param>
        /// <returns>Strings whose name does not contain the limiter</returns>
        private List<string> excludeInName(List<string> files, string limiter)
        {
            int i, len = files.Count;
            List<string> nFiles = new List<string>(len);
            for (i = 0; i < len; ++i)
            {
                if (!pathToFilename(files[i]).Contains(limiter))
                    nFiles.Add(files[i]);
            }
            return nFiles;
        }

        /// <summary>
        /// Compute the name of a file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string pathToFilename(string path)
        {
			return System.IO.Path.GetFileName(path);
        }

        /// <summary>
        /// Get usage
        /// </summary>
        /// <returns>Usage string</returns>
        public string usage()
        {
            string use;
            use =  "  ---------------------------------------\n";
            use += " /|         Files                       /|\n";
            use += "/ |     By Sketchers 2007              / |\n";
            use += "--------------------------------------/  |\n";
            use += "| arguments:                          |  |\n";
            use += "|                                     |  |\n";
            use += "| path query (-r | -d)                |  |\n";
            use += "| [ (-inPath | -exPath                |  |\n";
            use += "|    -inName | -exName) limiter ]*    |  |\n";
            use += "|                                     |  /\n";
            use += "|                                     | /\n";
            use += "| path file0 [ file1 [file2 ...]]     |/\n";
            use += "--------------------------------------\n";

            return use;
        }

        /// <summary>
        /// Files
        /// </summary>
        public List<string> GetFiles
        {
            get
            {
                return m_files;
            }
        }
    }
}
