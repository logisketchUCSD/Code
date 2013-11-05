using System;
using System.Collections.Generic;
using System.Text;
using SymbolRec.Image;

namespace ImageMatch
{
    /// <summary>
    /// Creates the average image
    /// </summary>
    public class CreateAverageImage
    {
        /// <summary>
        /// Width of image
        /// </summary>
        private int m_width;

        /// <summary>
        /// Height of image
        /// </summary>
        private int m_height;

        /// <summary>
        /// Name of average image
        /// </summary>
        private string m_name;

        /// <summary>
        /// Files used to create average image
        /// </summary>
        private List<string> m_files;

        /// <summary>
        /// The average image created
        /// </summary>
        private DefinitionImage di;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="files">Files to use</param>
        /// <param name="name">Name of symbol</param>
        /// <param name="width">Width of image</param>
        /// <param name="height">Height of image</param>
        public CreateAverageImage(List<string> files, string name, int width, int height)
        {
            m_files = files;
            m_name = name;
            m_width = width;
            m_height = height;            
        }

        /// <summary>
        /// Creates the average image
        /// </summary>
        /// <returns>Definition Image / Average image</returns>
        public DefinitionImage getDefinition()
        {
            di = new DefinitionImage(m_width, m_height, m_files);
            return di;
        }

        /// <summary>
        /// Write to matrix file and bmp
        /// </summary>
        public void write()
        {
            writeToFile();
            writeToBitmap();
        }

        /// <summary>
        /// Write to matrix file
        /// </summary>
        public void writeToFile()
        {
            if (di == null)
                getDefinition();
            di.writeToFile(m_name + ".amat");
        }

        /// <summary>
        /// Write to bitmap file
        /// </summary>
        public void writeToBitmap()
        {
            if (di == null)
                getDefinition();
            di.writeToBitmap(m_name, "bmp");
        }   
    }
}
