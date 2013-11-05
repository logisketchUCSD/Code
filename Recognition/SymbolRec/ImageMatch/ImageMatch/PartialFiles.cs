using System;
using System.Collections.Generic;
using System.Text;

namespace ImageMatch
{
    /// <summary>
    /// Represents all of the information necessary to create everything for partial gate recognition
    /// </summary>
    class PartialFiles : SymbolFiles
    {
        #region INTERNALS

        /// <summary>
        /// Number of symbols
        /// </summary>
        private const int NUM = 4;

        /// <summary>
        /// Path information, etc...
        /// </summary>
        private const string
            BACKLINE = "backline",
            BACKARC = "backarc",
            FRONTARC = "frontarc",
            BUBBLE = "bubble",

            UNKNOWN = "other",

            DIR = @"c:\Documents and Settings\student\My Documents\Trunk\Data\Context Symbols",
            SEP = @"\",
            EXT = "*.non.xml",
            REC = "-r",
            INPATH = "-inPath";

        /// <summary>
        /// Filenames for all of the symbols
        /// </summary>
        private List<string>
            m_backline,
            m_backarc,
            m_frontarc,
            m_bubble,
            m_unknown;

        /// <summary>
        /// Default Construtor
        /// </summary>
        public PartialFiles()
        {
            this.m_modelFile = "partial.model";
            this.m_trainFile = "partial.train";

            m_backline = null;
            m_backarc = null;
            m_frontarc = null;
            m_bubble = null;
            m_unknown = null;
        }

        #endregion

        #region IMPORTANT OVERRIDES

        /// <summary>
        /// Get Names of symbols
        /// </summary>
        public override List<string> Names
        {
            get
            {
                if (this.m_names == null)
                {
                    this.m_names = new List<string>(NUM);
                    this.m_names.Add(BACKLINE);
                    this.m_names.Add(BACKARC);
                    this.m_names.Add(FRONTARC);
                    this.m_names.Add(BUBBLE);
                }
                return this.m_names;
            }
        }

        /// <summary>
        /// Get the filenames for creating definition symbols
        /// </summary>
        public override List<List<string>> DefinitionFiles
        {
            get
            {
                if (this.m_definitionFiles == null)
                {
                    this.m_definitionFiles = new List<List<string>>(NUM);
                    this.m_definitionFiles.Add(GetBackLine);
                    this.m_definitionFiles.Add(GetBackArc);
                    this.m_definitionFiles.Add(GetFrontArc);
                    this.m_definitionFiles.Add(GetBubble);
                }
                return this.m_definitionFiles;
            }
        }

        /// <summary>
        /// Get the filenames for training
        /// </summary>
        public override List<List<string>> TrainingFiles
        {
            get
            {
                if (this.m_trainingFiles == null)
                {
                    this.m_trainingFiles = new List<List<string>>(NUM + 1);
                    this.m_trainingFiles.Add(GetBackLine);
                    this.m_trainingFiles.Add(GetBackArc);
                    this.m_trainingFiles.Add(GetFrontArc);
                    this.m_trainingFiles.Add(GetBubble);
                    this.m_trainingFiles.Add(GetUnknown);
                }
                return this.m_trainingFiles;
            }
        }

        /// <summary>
        /// Get model filename
        /// </summary>
        public override string ModelFile
        {
            get { return m_modelFile; }
        }

        /// <summary>
        /// Get train filename
        /// </summary>
        public override string TrainFile
        {
            get { return m_trainFile; }
        }

        #endregion

        #region GET GATES

        /// <summary>
        /// Get BackLine filenames
        /// </summary>
        private List<string> GetBackLine
        {
            get
            {
                if (m_backline == null)
                {
                    m_backline = (new Files.Files(new string[] { DIR, EXT, REC, INPATH, BACKLINE })).GetFiles;
                }
                return m_backline;
            }
        }

        /// <summary>
        /// Get BackArc filenames
        /// </summary>
        private List<string> GetBackArc
        {
            get
            {
                if (m_backarc == null)
                {
                    m_backarc = (new Files.Files(new string[] { DIR, EXT, REC, INPATH, BACKARC })).GetFiles;
                }
                return m_backarc;
            }
        }

        /// <summary>
        /// Get FrontArc filenames
        /// </summary>
        private List<string> GetFrontArc
        {
            get
            {
                if (m_frontarc == null)
                {
                    m_frontarc = (new Files.Files(new string[] { DIR, EXT, REC, INPATH, FRONTARC })).GetFiles;
                }
                return m_frontarc;
            }
        }

        /// <summary>
        /// Get Bubble filenames
        /// </summary>
        private List<string> GetBubble
        {
            get
            {
                if (m_bubble == null)
                {
                    m_bubble = (new Files.Files(new string[] { DIR, EXT, REC, INPATH, BUBBLE })).GetFiles;
                }
                return m_bubble;
            }
        }

        /// <summary>
        /// Get Unkown filenames
        /// </summary>
        private List<string> GetUnknown
        {
            get
            {
                if (m_unknown == null)
                {
                    m_unknown = (new Files.Files(new string[] { DIR, EXT, REC, INPATH, UNKNOWN })).GetFiles;
                }
                return m_unknown;
            }
        }

        #endregion
    }
}
