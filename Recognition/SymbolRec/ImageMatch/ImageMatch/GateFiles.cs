using System;
using System.Collections.Generic;
using System.Text;

namespace ImageMatch
{
    /// <summary>
    /// Represents the information necessary to create everything for symbol recognition
    /// </summary>
    class GateFiles : SymbolFiles
    {
        #region INTERNALS

        private bool useContext;

        /// <summary>
        /// Number of symbols
        /// </summary>
        private const int NUM = 5;

        /// <summary>
        /// Information that indicates path, gate names, etc...
        /// Hardcoded info used to get journal files for each gate
        /// </summary>
        private const string 
            AND = "and", 
            NAND = "nand", 
            NOR = "nor", 
            NOT = "not", 
            OR = "or",

            UNKNOWN = "nongate",
            //Old Values
            DIR = @"C:\Users\sketchers\Documents\Trunk\Data\Symbols",
            EXT = "*.jnt",
            
            // Different directories ?= Better data?
           // DIR = @"C:\Users\sketchers\Documents\Trunk\Data\Context Symbols\TEST",
           // EXT = "*.non.xml", // for non context
            //DIR = @"C:\Users\sketchers\Documents\Trunk\Data\Oriented Symbols", 
            //EXT = "*.con.xml", // for context
            SEP = @"\", 
            REC = "-r", 
            INPATH = "-inPath";
        
       

        /// <summary>
        /// A list of filenames (of .jnt files) for each symbol (gate)
        /// </summary>
        private List<string> 
            m_and, 
            m_nand, 
            m_nor, 
            m_not, 
            m_or, 
            m_unknown;

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Default Constructor
        /// </summary>
        public GateFiles()
        {
            this.m_modelFile = "gate.model";
            this.m_trainFile = "gate.train";

            m_and = null;
            m_nand = null;
            m_nor = null;
            m_not = null;
            m_or = null;
            m_unknown = null;
        }

        #endregion

        #region IMPORTANT OVERRIDES
                
        /// <summary>
        /// Get Gate symbols names
        /// </summary>
        public override List<string> Names
        {
            get
            {
                if (this.m_names == null)
                {
                    this.m_names = new List<string>(NUM);
                    this.m_names.Add(AND);
                    this.m_names.Add(NAND);
                    this.m_names.Add(NOR);
                    this.m_names.Add(NOT);
                    this.m_names.Add(OR);
                }
                return this.m_names;
            }
        }

        /// <summary>
        /// Get Gate symbol definition filenames
        /// </summary>
        public override List<List<string>> DefinitionFiles
        {
            get
            {
                if (this.m_definitionFiles == null)
                {
                    this.m_definitionFiles = new List<List<string>>(NUM);
                    this.m_definitionFiles.Add(GetAnd);
                    this.m_definitionFiles.Add(GetNand);
                    this.m_definitionFiles.Add(GetNor);
                    this.m_definitionFiles.Add(GetNot);
                    this.m_definitionFiles.Add(GetOr);
                }
                return this.m_definitionFiles;
            }
        }

        /// <summary>
        /// Get Gate training filenames
        /// </summary>
        public override List<List<string>> TrainingFiles
        {
            get
            {
                if (this.m_trainingFiles == null)
                {
                    this.m_trainingFiles = new List<List<string>>(NUM + 1);
                    this.m_trainingFiles.Add(GetAnd);
                    this.m_trainingFiles.Add(GetNand);
                    this.m_trainingFiles.Add(GetNor);
                    this.m_trainingFiles.Add(GetNot);
                    this.m_trainingFiles.Add(GetOr);
                    this.m_trainingFiles.Add(GetUnknown);
                }
                return this.m_trainingFiles;
            }
        }

        /// <summary>
        /// Get the model filename
        /// </summary>
        public override string ModelFile
        {
            get { return this.m_modelFile; }
        }

        /// <summary>
        /// Get the train filename
        /// </summary>
        public override string TrainFile
        {
            get { return this.m_trainFile; }
        }

        #endregion

        #region GET GATES

        /// <summary>
        /// Get And gate filenames
        /// </summary>
        private List<string> GetAnd
        {
            get
            {
                if (m_and == null)
                {
                    m_and = (new Files.Files(new string[] { DIR + SEP + AND, EXT, REC})).GetFiles;
                }
                return m_and;
            }
        }

        /// <summary>
        /// Get Nand gate filenames
        /// </summary>
        private List<string> GetNand
        {
            get
            {
                if (m_nand == null)
                {
                    m_nand = (new Files.Files(new string[] { DIR + SEP + NAND, EXT, REC })).GetFiles;
                }
                return m_nand;
            }
        }

        /// <summary>
        /// Get Nor gate filenames
        /// </summary>
        private List<string> GetNor
        {
            get
            {
                if (m_nor == null)
                {
                    m_nor = (new Files.Files(new string[] { DIR + SEP + NOR, EXT, REC })).GetFiles;
                }
                return m_nor;
            }
        }

        /// <summary>
        /// Get Not gate filenames
        /// </summary>
        private List<string> GetNot
        {
            get
            {
                if (m_not == null)
                {
                    m_not = (new Files.Files(new string[] { DIR + SEP + NOT, EXT, REC})).GetFiles;
                }
                return m_not;
            }
        }

        /// <summary>
        /// Get Or gate filenames
        /// </summary>
        private List<string> GetOr
        {
            get
            {
                if (m_or == null)
                {
                    m_or = (new Files.Files(new string[] { DIR + SEP + OR, EXT, REC })).GetFiles;
                }
                return m_or;
            }
        }

        /// <summary>
        /// Get Unknown gate filenames
        /// </summary>
        private List<string> GetUnknown
        {
            get
            {
                if (m_unknown == null)
                {
                    m_unknown = (new Files.Files(new string[] { DIR + SEP + UNKNOWN, EXT, REC })).GetFiles;
                }
                if (m_unknown == null)
                {
                    Console.WriteLine("Failed to get definitions for nongates");
                }
                return m_unknown;
            }
        }

        #endregion
    }
}
