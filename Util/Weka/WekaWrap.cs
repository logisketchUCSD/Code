/*
 * File: WekaWrap.cs
 *
 * Author: Sketchers 2011
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2011.
 * 
 * For more information on WEKA in general, see:
 * http://en.wikipedia.org/wiki/Weka_(machine_learning)
 * http://weka.wikispaces.com/
 */

using Domain;
using Sketch;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Utilities
{
    public class WekaWrap
    {
        #region Data Members

        /// <summary>
        /// The classifier that WEKA uses to decide everything.
        /// </summary>
        private weka.classifiers.Classifier _classifier;

        /// <summary>
        /// The set of instances that WEKA uses as context for the classifier. 
        /// We don't actually use it directly, but WEKA needs it to stick around.
        /// </summary>
        private weka.core.Instances _dataSet;

        protected bool debug = true;

        #endregion

        #region Constants

        private const string STROKE = "strokeClassifier";
        private const string TEXT_GROUP = "textGrouper";
        private const string WIRE_GROUP = "wireGrouper";
        private const string GATE_GROUP = "gateGrouper";

        /// <summary>
        /// The different classifier types that can be used in WekaWrap.
        /// </summary>
        public enum Classifier
        {
            J48, // J48 decision trees
            NaiveBayes, // a naive bayesian classifier
            MLP, // a multi-layer perceptron
            AdaBoost_J48, // a J48 decision tree implemented with adaboost
            AdaBoost_Stump // a decision stump implemented with adaboost
        }

        private const string GATE = "Gate";
        private const string WIRE = "Wire";
        private const string TEXT = "Text";
        private const string UNKNOWN = "Unknown";

        private const string ARFF = ".arff";
        private const string MODEL = ".model";

        #endregion

        #region Helpers

        /// <summary>
        /// Turns the string name of a classifier into a weka classifier of the appropriate type. Also deals with options.
        /// </summary>
        /// <param name="classifier">The classifier to use.</param>
        /// <returns>A weka compatable classifier</returns>
        private static weka.classifiers.Classifier GetClassifier(Classifier myClassifier)
        {
            switch (myClassifier)
            {
                case Classifier.J48:
                    return new weka.classifiers.trees.J48();
                case Classifier.NaiveBayes:
                    return new weka.classifiers.bayes.NaiveBayes();
                case Classifier.MLP:
                    // http://weka.sourceforge.net/doc/weka/classifiers/functions/MultilayerPerceptron.html#setOptions(java.lang.String[])
                    weka.classifiers.functions.MultilayerPerceptron cls = new weka.classifiers.functions.MultilayerPerceptron();
                    cls.setOptions(new string[] { "-H", "a" });
                    return cls;
                case Classifier.AdaBoost_J48:
                    weka.classifiers.meta.AdaBoostM1 cls_ab_j48 = new weka.classifiers.meta.AdaBoostM1();
                    cls_ab_j48.setOptions(new string[] { "-W", "weka.classifiers.trees.J48" });
                    return cls_ab_j48;
                case Classifier.AdaBoost_Stump:
                    weka.classifiers.meta.AdaBoostM1 cls_ab_stump = new weka.classifiers.meta.AdaBoostM1();
                    cls_ab_stump.setOptions(new string[] { "-W", "weka.classifiers.trees.DecisionStump" });
                    return cls_ab_stump;
                default:
                    Console.WriteLine("You did not specify a supported classifier type.");
                    return null;
            }
        }

        /// <summary>
        /// A static function to create the ARFF and model files for WEKA.
        /// </summary>
        /// <param name="myClassifier">The type of classifier to use.</param>
        /// <param name="fromDirectory">The list of full path filenames to use. Will only consider files ending in .xml </param>
        public static void wekaSetUp(Classifier myClassifier, List<string> filenames)
        {
            createARFFs(filenames);
            WekaWrap strokeClassif = new WekaWrap("strokeClassifier", myClassifier);
            WekaWrap textGrouper = new WekaWrap("textGrouper", myClassifier);
            //WekaWrap wireGrouper = new WekaWrap("wireGrouper", myClassifier);
            WekaWrap gateGrouper = new WekaWrap("gateGrouper", myClassifier);   
        }

        /// <summary>
        /// A static function to creat the ARFF and model files for WEKA.
        /// </summary>
        /// <param name="myClassifier">The type of classifier to use.</param>
        /// <param name="filenames">The list of full path filenames to use. Will only consider files ending .xml</param>
        /// <param name="exclude">wekaSetUp will ignore any files that contain this string. Useful for testing.</param>
        public static void wekaSetUp(Classifier myClassifier, List<string> filenames, string exclude)
        {
            List<string> goodFiles = new List<string>();
            foreach (string file in filenames)
            {
                if (file.Contains(exclude)) { continue; }
                else { goodFiles.Add(file); }
            } 
            wekaSetUp(myClassifier, goodFiles);
        }

        /// <summary>
        /// A static function to create the ARFF and model files for WEKA from all the xml files in a directory.
        /// </summary>
        /// <param name="myClassifier">The type of classifier to use.</param>
        /// <param name="directory">The directory from which you should train the classifiers and grouper.
        ///     Should be full path, ignores any files that don't end with ".xml"</param>
        public static void wekaSetUp(Classifier myClassifier, string directory)
        {
            createARFFs(directory);
            WekaWrap strokeClassif = new WekaWrap("strokeClassifier", myClassifier);
            WekaWrap textGrouper = new WekaWrap("textGrouper", myClassifier);
            //WekaWrap wireGrouper = new WekaWrap("wireGrouper", myClassifier);
            WekaWrap gateGrouper = new WekaWrap("gateGrouper", myClassifier);  
        }

        #endregion

        #region Constructors
        
        /// <summary>
        /// Creates a WekaWrap with a classifier loaded from an existing model file.
        /// </summary>
        /// <param name="modelFile">The file to load the classifier from. Needs to be full path.</param>
        public WekaWrap(string modelFile)
        {
            if (debug) Console.WriteLine("Creating a new WekaWrap for this model: " + modelFile);
            loadModel(modelFile);
        }

        /// <summary>
        /// Creates a WekaWrap with a classifier trained off of an ARFF file and a specified classifier.
        /// </summary>
        /// <param name="ARFFfile">The base filename of the ARFF file to use. Should not have an extension, but should be full path.</param>
        /// <param name="classifier">The type of classifier to use.</param>
        public WekaWrap(string filename, Classifier myClassifier)
        {
            createModel(filename + ARFF, myClassifier);
            saveModel(filename + MODEL);
        }

        #endregion

        #region Classify Using Weka Classifier

        /// <summary>
        /// Uses the classifier to classify an instance (from its featureValues).
        /// </summary>
        /// <param name="featureValues">An array of doubles that describe the instance.</param>
        /// <returns>The string name of the classification of the instance.</returns>
        public string classify(double[] featureValues)
        {
            //if (!classifierBuilt) { _classifier.buildClassifier(_dataSet); classifierBuilt = true; }

            weka.core.Instance inst = new weka.core.Instance(1, featureValues);
            inst.setDataset(_dataSet);
            
            double result = _classifier.classifyInstance(inst);

            weka.core.Attribute attribute = _dataSet.attribute(_dataSet.numAttributes() - 1);
            string resultName = attribute.value((int)result);

            // Get rid of this line once ARFF files are rewritten
            if (resultName == "Label") resultName = "Text";

            //Console.WriteLine(resultName);
            return resultName;
        }

        #endregion

        #region Create ARFF file
        // It should be noted that the functions to create the ARFF files are somewhat of a hack. They work, on my computer.
        // They may also work on yours. Hopefully once the ARFF files are created nobody will ever have to use these functions
        // again, but we're leaving them here anyways. Use at your own risk and feel free to email me if you have questions.
        //      Good luck!  -- Jessi Peck, Sketcher 2011, jpeck@hmc.edu
        /// <summary>
        /// Create ARFF files for the classifier and grouper from a set of sketches
        /// </summary>
        /// <param name="fileList">a list of sketch file names</param>
        public static void createARFFs(List<string> fileList)
        {
            // These dictionaries will store the feature values for each of the ARFF files we're creating
            Dictionary<double[], string> strokeValues = new Dictionary<double[], string>();
            Dictionary<string, Dictionary<double[], string>> groupValues = new Dictionary<string, Dictionary<double[], string>>();
            groupValues.Add(TEXT, new Dictionary<double[], string>());
            groupValues.Add(WIRE, new Dictionary<double[], string>());
            groupValues.Add(GATE, new Dictionary<double[], string>());

            // These lists will store the names of the features for the ARFF files we're creating
            List<string> strokeNames = new List<string>();
            List<string> groupNames = new List<string>();

            // Stores each stroke's classification. 
            Dictionary<Substroke, string> strokeClassifications = new Dictionary<Substroke, string>();

            Dictionary<string, string> settingsFiles = Files.SettingsReader.readSettings();

            bool firstFile = true;

            foreach (string file in fileList)
            {
                if (!file.EndsWith(".xml")) { continue; } // only read .xml files

                //Console.WriteLine("Reading file " + file);

                // Load the sketch from the file and make it into a feature sketch
                Sketch.Sketch sketch = new ConverterXML.ReadXML(file).Sketch;
                Featurefy.FeatureSketch featureSketch = Featurefy.FeatureSketch.MakeFeatureSketch(new Sketch.Project(sketch), settingsFiles);

                // Get featureSketch to tell you the feature values for each individual stroke in the sketch
                Dictionary<Substroke, double[]> classificationDict = new Dictionary<Substroke, double[]>();
                foreach (Substroke substroke in sketch.Substrokes)
                    classificationDict.Add(substroke, featureSketch.GetValuesSingle(substroke));

                // Classify each substroke based on what shape it's a part of
                foreach (Sketch.Shape shape in sketch.Shapes)
                {
                    string classif = shape.Type.Classification;
                    if (classif == UNKNOWN) continue;

                    foreach (Substroke stroke in shape.Substrokes)
                    {
                        strokeValues.Add(classificationDict[stroke], classif);
                        strokeClassifications.Add(stroke, classif);
                    }
                }

                #region Bad XML File Catcher
                // This shouldn't be necessary if your XML files are properly formatted,
                // but it's here just in case.
                List<Substroke> badStrokes = new List<Substroke>();
                foreach (Substroke strokeToCheck in classificationDict.Keys)
                {
                    if (classificationDict[strokeToCheck].Length != 27)
                    {
                        Console.WriteLine("OH MAN. GET VALUES SINGLE FAILS.");
                        Console.WriteLine("Your length of features is " + classificationDict[strokeToCheck].Length + " elements long.");
                        badStrokes.Add(strokeToCheck);
                    }
                }

                foreach (Substroke naughtyStroke in badStrokes)
                {
                    strokeValues.Remove(classificationDict[naughtyStroke]);
                    strokeClassifications.Remove(naughtyStroke);
                    classificationDict.Remove(naughtyStroke);
                    Console.WriteLine("You will never see that stroke again.");
                }
                #endregion
                
                // Get featureSketch to tell you the feature values for each pair of strokes in the sketch
                Dictionary<string, Dictionary<Featurefy.FeatureStrokePair, double[]>> grouperDict = featureSketch.GetValuesPairwise(strokeClassifications);

                // You only need to call this once, might as well be on the first file you do.
                if (firstFile) 
                { 
                    strokeNames = featureNames(featureSketch.FeatureListSingle); // set up the list of feature names for stroke classification
                    groupNames = featureNames(featureSketch.FeatureListPair); // set up the list of feature names for stroke grouping
                    firstFile = false;
                }

                // Adds feature values for grouping to groupValues
                getGroupValues(ref groupValues, grouperDict); 
            }
            // Populate the lists of class names
            List<string> strokeClasses, groupClasses;
            classNames(out strokeClasses, out groupClasses);

            string directory = AppDomain.CurrentDomain.BaseDirectory;

            // write ARFF for stroke classifier
            writeARFFfile(directory + STROKE + ARFF, strokeNames, strokeValues, strokeClasses);

            // write ARFF for stroke grouper x3
            writeARFFfile(directory + TEXT_GROUP + ARFF, groupNames, groupValues[TEXT], groupClasses);
            //writeARFFfile(directory + WIRE_GROUP + ARFF, groupNames, groupValues[WIRE], groupClasses);
            writeARFFfile(directory + GATE_GROUP + ARFF, groupNames, groupValues[GATE], groupClasses);

            //Console.WriteLine("ARFF files created!");
        }

        /// <summary>
        /// Helper for createARFFs. Populates the lists of classes for strokes and groups.
        /// </summary>
        /// <param name="strokeClasses">An empty/uninitialized dictionary to add stroke classifications to.</param>
        /// <param name="groupClasses">An empty/uninitialized dictionary to add group classifications to.</param>
        private static void classNames(out List<string> strokeClasses, out List<string> groupClasses)
        {
            // the possible classifications for strokes
            strokeClasses = new List<string>();
            strokeClasses.Add(GATE);
            strokeClasses.Add(WIRE);
            strokeClasses.Add(TEXT);

            // the possible classifications for grouping
            groupClasses = new List<string>();
            groupClasses.Add("Join");
            groupClasses.Add("NoJoin");
            groupClasses.Add("Ignore");
        }

        private static void getGroupValues(ref Dictionary<string, Dictionary<double[], string>> groupValues, Dictionary<string, Dictionary<Featurefy.FeatureStrokePair, double[]>> grouperDict)
        {
            // the possible classifications for grouping
            const string JOIN = "Join";
            const string NOJOIN = "NoJoin";

            // look at each type of stroke, then look at each pair of stroke in that type and see if they should be joined or not
            foreach (string strokeClass in grouperDict.Keys)
                foreach (Featurefy.FeatureStrokePair strokePair in grouperDict[strokeClass].Keys)
                {
                    bool check = false;
                    Shape parentA = strokePair.Item1.ParentShape;
                    Shape parentB = strokePair.Item2.ParentShape;
                    if (parentA == parentB)
                    {
                        groupValues[strokeClass].Add(grouperDict[strokeClass][strokePair], JOIN);
                        check = true;
                    }
                    if (!check)
                        groupValues[strokeClass].Add(grouperDict[strokeClass][strokePair], NOJOIN); 
                }
        }

        private static List<string> featureNames(Dictionary<string, bool> featureList)
        {
            List<string> features = new List<string>();
            foreach (string name in featureList.Keys)
                if (featureList[name] == true)
                {
                    string myName = name.Replace("'", "");
                    if (!features.Contains(myName))
                        features.Add(myName);
                }
            return features;
        }

        /// <summary>
        /// Creates the four necessary ARFF files from a given directory. (Ignores all non xml files)
        /// </summary>
        /// <param name="fromDirectory">The directory you want to create the ARFF files from.</param>
        public static void createARFFs(string fromDirectory)
        {
            string[] filepaths = System.IO.Directory.GetFiles(fromDirectory);
            List<string> fileList = new List<string>(filepaths);
            createARFFs(fileList);
        }

        /// <summary>
        /// Creates a ARFF file that Weka can learn from.
        /// </summary>
        /// <param name="filename">The location of the file you want to create. Full path required.</param>
        /// <param name="featureNames">The names of the relevant features. The column headings, so to speak.</param>
        /// <param name="featureValues">A list of arrays of doubles representing the features of each item that you're training with.</param>
        /// <param name="classNames">The classes that Weka will be splitting your data into.</param>
        private static void writeARFFfile(string filename, List<string> featureNames, Dictionary<double[], string> featureValues, List<string> classNames)
        {
            //Console.WriteLine("Calling arff writer for " + filename);

            StreamWriter writer = new StreamWriter(filename, false); // write over the file, don't append to it

            // write the header of the file
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
            writer.WriteLine();
            writer.WriteLine("@DATA");

            // Actually write the values to the file
            foreach (double[] value in featureValues.Keys)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (i < value.Length)
                        writer.Write(value[i] + ",");
                    else
                        writer.WriteLine(value[i]);
                }
                writer.WriteLine(featureValues[value]);
            }

            writer.Close();
        }
        #endregion

        #region Model Creating, Saving, Loading

        /// <summary>
        /// Creates a classifier of the desired type from an .arff file
        /// </summary>
        /// <param name="ARFFfile">The arff file to read from. Should be a full path.</param>
        /// <param name="classifier">The type of classifier you want to make.</param>
        /// <returns>The classifier you created</returns>
        public void createModel(string ARFFfile, Classifier myClassifier)
        {
            if (debug) Console.WriteLine("Loading ARFF file " + ARFFfile);

            _classifier = GetClassifier(myClassifier);
            try
            {
                _dataSet = new weka.core.Instances(new java.io.FileReader(ARFFfile));
                if (debug) Console.WriteLine("You have " + _dataSet.numAttributes() + " attributes.");
                _dataSet.setClassIndex(_dataSet.numAttributes() - 1);

                _classifier.buildClassifier(_dataSet);

                if (debug) Console.WriteLine(_classifier.toString());
            }
            catch (Exception e)
            {
                Console.WriteLine("You failed. End of Game. Poor Weka.");
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Saves an existing classifier to a model file.
        /// </summary>
        /// <param name="classif">The classifier to save.</param>
        /// <param name="modelFile">The filename to write to. Should be a full path ending with .model</param>
        public void saveModel(string modelFile)
        {
            weka.core.SerializationHelper.write(modelFile, _classifier);
        }

        /// <summary>
        /// Loads a classifier from a model file.
        /// </summary>
        /// <param name="filename">The filename (full path) that you want to load. Should be an .arff file and a .model file in your working directory.</param>
        public void loadModel(string filename)
        {
            if (debug) Console.WriteLine("Model loading...");
            _classifier = (weka.classifiers.Classifier)weka.core.SerializationHelper.read(filename + MODEL);
            _dataSet = new weka.core.Instances(new java.io.FileReader(filename + ARFF));
            _dataSet.setClassIndex(_dataSet.numAttributes() - 1);

            if (debug) Console.WriteLine("Model locked and loaded!");
        }
        #endregion
    }
}