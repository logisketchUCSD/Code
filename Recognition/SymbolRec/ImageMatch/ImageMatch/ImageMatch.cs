using System;
using System.Collections.Generic;
using System.Text;
using SymbolRec.Image;

namespace ImageMatch
{
    /// <summary>
    /// Framework for making DefinitionImages, creating training, and running training
    /// </summary>
    public class ImageMatch
    {
        #region INTERNALS

        /// <summary>
        /// All of the files needed to do symbol recognition
        /// </summary>
        private SymbolFiles m_symbolFiles;

        /// <summary>
        /// Width and height of image
        /// </summary>
        private int m_width, m_height;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="type">Symbol set to use</param>
        /// <param name="width">Width of images</param>
        /// <param name="height">Height of images</param>
        /// The location of the gate files is hard-coded in GateFiles.cs
        public ImageMatch(string type, int width, int height)
        {
            switch (type)
            {
                case "GATE":
                    //Note: This class hard-codes the location of the gate journal files.
                    m_symbolFiles = new GateFiles(); 
                    break;
                case "PARTIAL":
                    m_symbolFiles = new PartialFiles();
                    break;
                default:
                    throw new Exception("Specify either GATE or PARTIAL");
            }
            m_width = width;
            m_height = height;
        }

        #endregion

        /// <summary>
        /// Create the DefinitionImages
        /// </summary>
        public void createDefinitionSymbols()
        {
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Creating definition symbols...                     |");
            Console.WriteLine("----------------------------------------------------");

            CreateAverageImage ci;
            int i, len = m_symbolFiles.NumSymbols;
            for (i = 0; i < len; ++i)
            {
                ci = new CreateAverageImage(m_symbolFiles.DefinitionFiles[i], m_symbolFiles.Names[i], m_width, m_height);
                ci.write();
            }            
        }

        /// <summary>
        /// Create the training file
        /// </summary>
        public void createTrainingFile()
        {
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Creating training data...                          |");
            Console.WriteLine("----------------------------------------------------");

            CreateTrainingFile ct = new CreateTrainingFile(m_symbolFiles, m_width, m_height);
        }

        /// <summary>
        /// Run default training
        /// </summary>
        public void train()
        {
            //use this grid search
            train(20, -6.0, 20.0, 20, 7.0, -19.0, 5, 5);
        }

        /// <summary>
        /// Run specific training
        /// </summary>
        /// <param name="cSteps">Number of steps between cStart and cEnd</param>
        /// <param name="cStart">Initial cost parameter</param>
        /// <param name="cEnd">Final cost parameter</param>
        /// <param name="gSteps">Number of steps between gStarn and gEnd</param>
        /// <param name="gStart">Initial gamma parameter</param>
        /// <param name="gEnd">Final gamma parameter</param>
        /// <param name="numGrid">Number of times to perform grid search</param>
        /// <param name="numCross">Number of times to run cross validation</param>
        public void train(int cSteps, double cStart, double cEnd, int gSteps, double gStart, double gEnd, int numGrid, int numCross)
        {
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Training...                                        |");
            Console.WriteLine("----------------------------------------------------");

            Svm.TrainSVM train =
                new Svm.TrainSVM(m_symbolFiles.TrainFile, m_symbolFiles.ModelFile);

            //use probabilities
            train.setParam(true);

            //use this grid search
            train.setParam(cSteps, cStart, cEnd, gSteps, gStart, gEnd, numGrid, numCross);

            //train!
            train.start();
        }

        /// <summary>
        /// Classify stuff :p (not implemented)
        /// </summary>
        public void classify()
        {
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Classifying... (nothing)                           |");
            Console.WriteLine("----------------------------------------------------");

            //Svm.ClassifyGate classify = new Svm.ClassifyGate(m_symbolFiles.ModelFile);
        }

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //args[0] is type of symbols. Should be GATE or PARTIAL now
            //args[1] is width, args[2] is height
            ImageMatch im = new ImageMatch(args[0], Convert.ToInt32(args[1]), Convert.ToInt32(args[2]));

            int i, len = args.Length;
            for (i = 3; i < len; ++i)
            {
                switch (args[i])
                {
                    case "makesymbols":
                        im.createDefinitionSymbols();
                        break;
                    case "maketrain":
                        im.createTrainingFile();
                        break;
                    case "train":

                        int cSteps, gSteps, numGrid, numCross;
                        double cStart, cEnd, gStart, gEnd;
                        try
                        {
                            cSteps = Convert.ToInt32(args[i + 1]);
                            cStart = Convert.ToDouble(args[i + 2]);
                            cEnd = Convert.ToDouble(args[i + 3]);
                            gSteps = Convert.ToInt32(args[i + 4]);
                            gStart = Convert.ToDouble(args[i + 5]);
                            gEnd = Convert.ToDouble(args[i + 6]);
                            numGrid = Convert.ToInt32(args[i + 7]);
                            numCross = Convert.ToInt32(args[i + 8]);
                            i += 8;
                        }
                        catch
                        {
                            cSteps = 20;
                            cStart = -6.0;
                            cEnd = 20.0;
                            gSteps = 20;
                            gStart = 7.0;
                            gEnd = -19.0;
                            numGrid = 5;
                            numCross = 5;
                        }

                        im.train(cSteps, cStart, cEnd, gSteps, gStart, gEnd, numGrid, numCross);
                                                
                        break;
                    case "classify":
                        im.classify();
                        break;

                    default:
                        break;
                }
            }

            Console.Write("Press any key to continue...");
            Console.ReadLine();
        }
    }
        
}
