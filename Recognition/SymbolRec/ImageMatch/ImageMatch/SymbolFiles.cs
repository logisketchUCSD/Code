using System;
using System.Collections.Generic;
using System.Text;

namespace ImageMatch
{
    /// <summary>
    /// Inherit from this class to create all of the information necessary for symbol recognition
    /// </summary>
    abstract class SymbolFiles
    {
        /// <summary>
        /// Names of symbols
        /// </summary>
        protected List<string> m_names;

        /// <summary>
        /// Definition and Training files
        /// </summary>
        protected List<List<string>> m_definitionFiles, m_trainingFiles;
        
        /// <summary>
        /// Training filename and model filename
        /// </summary>
        protected string m_trainFile, m_modelFile;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public SymbolFiles()
        {
            m_names = null;
            m_definitionFiles = null;
            m_trainingFiles = null;
        }
        
        /// <summary>
        /// Get Names of symbols
        /// </summary>
        public abstract List<string> Names { get; }

        /// <summary>
        /// Get filenames for creating definition symbols
        /// </summary>
        public abstract List<List<string>> DefinitionFiles { get; }

        /// <summary>
        /// Get filenames for training files
        /// </summary>
        public abstract List<List<string>> TrainingFiles { get; }

        /// <summary>
        /// Get the model filename
        /// </summary>
        public abstract string ModelFile { get; }

        /// <summary>
        /// Get the train filename
        /// </summary>
        public abstract string TrainFile { get; }

        /// <summary>
        /// Get the number of symbols
        /// </summary>
        public int NumSymbols
        {
            get { return Names.Count; }
        }
    }
}
