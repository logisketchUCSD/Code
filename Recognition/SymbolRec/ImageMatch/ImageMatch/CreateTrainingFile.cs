using System;
using System.Collections.Generic;
using System.Text;
using SymbolRec.Image;

namespace ImageMatch
{
    /// <summary>
    /// Creates the SVM training file
    /// </summary>
    class CreateTrainingFile
    {
        /// <summary>
        /// Names of symbols
        /// </summary>
        private List<string> m_symbols;

        /// <summary>
        /// Definition Symbols and Traininng Symbols
        /// </summary>
        private List<DefinitionImage> m_defs, m_tran;
 
        /// <summary>
        /// Width and Height
        /// </summary>
        private int m_width, m_height;

        /// <summary>
        /// Training file names
        /// </summary>
        private List<List<string>> m_trainingFiles;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="output">Training file output name</param>
        /// <param name="symbolNames">Names of symbols</param>
        /// <param name="trainingFiles">Training files (in order of symbol names)</param>
        /// <param name="width">Width of image</param>
        /// <param name="height">Heigh of image</param>
        public CreateTrainingFile(string output, List<string> symbolNames, List<List<string>> trainingFiles, int width, int height)
        {
            m_symbols = symbolNames;

            m_trainingFiles = trainingFiles;

            m_width = width;
            m_height = height;

            loadDefinitions();
            makeTraining(output);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileStuff">Specifies files to find</param>
        /// <param name="width">Width of image</param>
        /// <param name="height">Height of image</param>
        public CreateTrainingFile(SymbolFiles fileStuff, int width, int height)
            : this(fileStuff.TrainFile, fileStuff.Names, fileStuff.TrainingFiles, width, height)
        {
        }

        /// <summary>
        /// Load definition images
        /// </summary>
        private void loadDefinitions()
        {            
            DefinitionImage di;

            int i, len = m_symbols.Count;// m_definitionFiles.Count;
            m_defs = new List<DefinitionImage>(len);
            for (i = 0; i < len; ++i)
            {
                //should be normalized already :)
                di = new DefinitionImage(m_width, m_height, m_symbols[i] + ".amat");

                m_defs.Add(di);

                //use this so we can rotate and recognize the training
                DefinitionImage.AddMatch(di);
            }
        }

        /// <summary>
        /// Make the training file
        /// </summary>
        /// <param name="filename">output filename</param>
        private void makeTraining(string filename)
        {
            DefinitionImage di;
            System.IO.StreamWriter sw = new System.IO.StreamWriter(filename);

            List<string> file = new List<string>(1);
            file.Add("");

            int i, j, cat, len2, len = m_trainingFiles.Count;
            
            //Go through the different symbol names
            for (i = 0; i < len; ++i)
            {
                //Go through all of the specific symbols
                if (m_trainingFiles[i] == null)
                {
                    Console.WriteLine("There were no training files at index {0}", i);
                    continue;
                }

                len2 = m_trainingFiles[i].Count;
                for (j = 0; j < len2; ++j)
                {
                    file[0] = m_trainingFiles[i][j];
                    di = new DefinitionImage(m_width, m_height, file);
                    di.Main.writeToBitmap(file[0] + ".bmp");
                    sw = write(di, i + 1, sw);
                    sw.WriteLine();
                }
            }
            sw.Close();
        }

        /// <summary>
        /// Write out to the stream
        /// </summary>
        /// <param name="di">Image to write</param>
        /// <param name="category">Training index</param>
        /// <param name="sw">Stream to write to</param>
        /// <returns>New place in stream</returns>
        private System.IO.StreamWriter write(DefinitionImage di, int category, System.IO.StreamWriter sw)
        {
            return write(di.toNodes(), category, sw);
        }

        /// <summary>
        /// Write out to the stream
        /// </summary>
        /// <param name="nodes">Nodes to write</param>
        /// <param name="category">Training index</param>
        /// <param name="sw">Stream to write to</param>
        /// <returns>New place in stream</returns>
        private System.IO.StreamWriter write(libsvm.svm_node[] nodes, int category, System.IO.StreamWriter sw)
        {
            sw.Write(category);

            int i, len = nodes.Length;
            libsvm.svm_node node;
            for (i = 0; i < len; ++i)
            {
                node = nodes[i];
                sw.Write(" " + node.index.ToString() + ":" + node.value_Renamed.ToString());
            }
            return sw;
        }
    }
}
