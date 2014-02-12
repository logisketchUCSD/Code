using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Utilities
{
    public class WekaWrap
    {
        //Each entry maps an int to a classifier name and its required parameters
        Dictionary<int, String> Classifiers = new Dictionary<int,string>();
        Dictionary<int, String> ClassArgs = new Dictionary<int, string>();
        
        //Location of weka.jar
        string WEKA_Classpath = "\"C:\\Program Files\\Weka-3-6\\weka.jar\"";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arffFile">example: @"C:\Users\jhero\Desktop\gag.arff"</param>
        /// <param name="modelPath">example: @"C:\Users\jhero\Desktop\model"</param>
        /// <param name="classifier"></param>
        /// <param name="numFeatures">Number of features, INCLUDING the class</param>
        public static void Train(string arffFile, string modelPath, string classifier, int numFeatures)
        {
            int cls = GetClassifier(classifier);
            WekaWrap p = new WekaWrap();
            p.train(arffFile, modelPath, cls, numFeatures);
        }

        public static List<string> Classify(string arffFile, string modelPath, string classifier, int numFeatures)
        {
            int cls = GetClassifier(classifier);
            WekaWrap p = new WekaWrap();
            return p.test(arffFile, modelPath, cls, numFeatures);
        }

        static int GetClassifier(string classifier)
        {
            int cls = 1;
            switch (classifier.ToLower())
            {
                case "j48":
                    cls = 1;
                    break;
                case "naivebayes":
                    cls = 2;
                    break;
                case "mlp":
                    cls = 3;
                    break;
                case "adaboost_j48":
                    cls = 4;
                    break;
                case "adaboost_stump":
                    cls = 5;
                    break;
                default:
                    break;
            }

            return cls;
        }

        /** 
         * Populate the classifiers dictionary. 
         * The strings can be found in the WEKA GUI from the first line of output when running a classifier.
         *   1 - J48 Tree
         *   2 - Naive Bayes
         *   3 - MLP
         *   4 - Ada Boost J48
         *   5 - Ada Boost Decision Stump
         */
        public WekaWrap()
        {
            Classifiers.Add(1, "weka.classifiers.trees.J48");
            ClassArgs.Add(1, "-C 0.25 -M 2");
            
            Classifiers.Add(2, "weka.classifiers.bayes.NaiveBayes");
            ClassArgs.Add(2, "");
            
            Classifiers.Add(3, "weka.classifiers.functions.MultilayerPerceptron");
            ClassArgs.Add(3, "-L 0.3 -M 0.2 -N 500 -V 0 -S 0 -E 20 -H a");
            
            Classifiers.Add(4, "weka.classifiers.meta.AdaBoostM1");
            ClassArgs.Add(4, "-P 100 -S 1 -I 10 -W weka.classifiers.trees.J48 -- -C 0.25 -M 2");
            
            Classifiers.Add(5, "weka.classifiers.meta.AdaBoostM1");
            ClassArgs.Add(5, "-P 100 -S 1 -I 10 -W weka.classifiers.trees.DecisionStump");
        }

        //Runs weka with the given command and arguments. Returns the predicted classes.
        public Process run_weka(string cmd, string args)
        {
            Process weka = new Process();
            weka.StartInfo.FileName = cmd; //Sets the program to run
            weka.StartInfo.Arguments = args; //Set the command line arguments
            weka.StartInfo.RedirectStandardOutput = true; //Redirects std_out to this program
            weka.StartInfo.UseShellExecute = false; //Used to redirect std_out

            //Try to start WEKA. Exit on error
            if (!weka.Start())
            {
                throw (new Exception("Error starting WEKA from the command line"));
            }

            return weka;
        }

        public void train(string arff_file, string model_file, int classifier_type, int Num_Features)
        {
            // Create the command line string to be called 
            string cmd = "java";

            string args = " -classpath " + //This is a java program
                            WEKA_Classpath + " " + //Set the class path
                            Classifiers[classifier_type] + " " + //Classifiers contains the classifier type
                            "-p " + Num_Features + " " + //Only output the predictions. Requires the number of features, including the class feature
                            "-t \"" + arff_file + "\" " + // + //The arff file containing training instances
                            "-d \"" + model_file + "\" " + //Where to save the resulting classifier model
                            ClassArgs[classifier_type]; //Classifier arguments

            Process weka = new Process();
            weka.StartInfo.FileName = cmd; //Sets the program to run
            weka.StartInfo.Arguments = args; //Set the command line arguments
            weka.StartInfo.RedirectStandardOutput = true; //Redirects std_out to this program
            weka.StartInfo.UseShellExecute = false; //Used to redirect std_out
            weka.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            //Try to start WEKA. Exit on error
            if (!weka.Start())
            {
                throw (new Exception("Error starting WEKA from the command line"));
            }

            int n = 0;
            while (!System.IO.File.Exists(model_file) && !weka.HasExited)
            {
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine("Sleeping {0}", n++);
            }
            System.Threading.Thread.Sleep(2000);
            Console.WriteLine("Sleeping {0}", n++);

            if (!weka.HasExited)
                weka.CloseMainWindow();
        }

        /** Uses WEKA to test the arff_file using the supplied model file */
        public List<string> test(string arff_file, string model_file, int classifier_type, int Num_Features)
        {
            List<string> results = new List<string>();

            string cmd = "java";

            string args = " -classpath " + //This is a java program
                            WEKA_Classpath + " " + //Set the class path
                            Classifiers[classifier_type] + " " + //Classifiers contains the classifier type and its options
                            "-l \"" + model_file + "\" " + //Where to load classifier model
                            "-T \"" + arff_file + "\" " + // + //The arff file containing testing instances
                            "-p " + Num_Features + " "; //Only output the predictions. Requires the number of features, including the class feature

            Process weka = new Process();
            weka.StartInfo.FileName = cmd; //Sets the program to run
            weka.StartInfo.Arguments = args; //Set the command line arguments
            weka.StartInfo.RedirectStandardOutput = true; //Redirects std_out to this program
            weka.StartInfo.UseShellExecute = false; //Used to redirect std_out
            weka.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            weka.StartInfo.CreateNoWindow = true;

            //Try to start WEKA. Exit on error
            if (!weka.Start())
            {
                throw (new Exception("Error starting WEKA from the command line"));
            }

            //Get a handle to the std_out
            StreamReader weka_out = weka.StandardOutput;

            string line = null;
            while ((line = weka_out.ReadLine()) != null)
            {
                string[] split = line.Split(new char[] { ':' });
                
                //Skip lines not containing predicted output
                if(split.Length != 3) 
                    continue;

                string clss = split[2].Split(new char[]{' '})[0];
                results.Add(clss);
            }
            if (!weka.HasExited)
                weka.CloseMainWindow();

            return results;
        }

        public static void createWekaARFFfile(string filename, List<string> featureNames, List<double[]> featureValues, List<string> classNames)
        {
            StreamWriter writer = new StreamWriter(filename);

            writer.WriteLine("@RELATION data");
            foreach (string att in featureNames)
            {
                writer.WriteLine("@ATTRIBUTE '" + att + "' NUMERIC");
            }
            writer.Write("@ATTRIBUTE 'class' {");
            for (int i = 0; i < classNames.Count; i++)
            {
                if (i != classNames.Count - 1)
                    writer.Write(classNames[i] + ",");
                else
                    writer.WriteLine(classNames[i] + "}");
            }
            //if (featureNames.Contains("Minimum Distance"))
                //writer.WriteLine("@ATTRIBUTE 'class' {Join,NoJoin,Ignore}");//,Ignore
            //else
                //writer.WriteLine("@ATTRIBUTE 'class' {Gate,Wire,Label}");

            writer.WriteLine();

            writer.WriteLine("@DATA");

            foreach (double[] value in featureValues)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (i < value.Length)
                        writer.Write(value[i] + ",");
                    else
                        writer.WriteLine(value[i]);
                }
                writer.WriteLine("?");
            }

            writer.Close();
        }
    }
    
}
