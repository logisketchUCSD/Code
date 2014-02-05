using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

using CombinationRecognizer;
using SubRecognizer;
using Sketch;
using ConverterXML;
using Domain;
using Recognizers;
using RecognitionInterfaces;
using RecognitionManager;
using Featurefy;
using Utilities.Neuro;

namespace TrainRecognizers
{
    class Program
    {

        /// <summary>
        /// Arguments
        ///    0: the directory to find files
        ///    1: directory to find real-world data (recursive)
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length < 2)
                return;

            // Get the list of files
            Console.WriteLine("Finding sketch files...");
            List<string> allSketches = new List<string>(System.IO.Directory.GetFiles(args[0], "*.xml"));
            Console.WriteLine("    found " + allSketches.Count + " sketches");


            // Load all the shapes in all the sketches
            Console.WriteLine("Loading full data set...");
            List<Shape> shapeData = GetShapeData(allSketches);
            Console.WriteLine("    found " + shapeData.Count + " gates");


            // Print classes found
            HashSet<ShapeType> types = new HashSet<ShapeType>();
            foreach (Shape shape in shapeData)
            {
                types.Add(shape.Type);
            }
            Console.WriteLine("Found " + types.Count + " types:");
            foreach (ShapeType type in types)
            {
                Console.WriteLine("    " + type);
            }
            
            // Save all the shapes to images in the "sketches" folder
            string outputPath = @"shapes\"; 
            Console.WriteLine("Saving gates to '" + outputPath + "'...");
            if (!System.IO.Directory.Exists(outputPath))
                System.IO.Directory.CreateDirectory(outputPath);
            foreach (Shape shape in shapeData)
            {
                System.Drawing.Bitmap b = shape.createBitmap(100, 100, true);
                shape.TemplateDrawing = b;
                string filename = String.Format(outputPath + shape.Type + "-{0:x}.png", shape.GetHashCode());
                b.Save(filename);
            }
            Console.WriteLine("    finished saving gates");


            // Train the base recognizers on all the data
            Console.WriteLine("Training recognizers on all data...");

#if false
            Console.WriteLine("    rubine");
            RubineRecognizerUpdateable rubine = new RubineRecognizerUpdateable(shapeData);
            rubine.Save("Rubine.rru");
            rubine.LiteRecognizer.Save("RubineLite.rr");

            Console.WriteLine("    dollar");
            DollarRecognizer dollar = new DollarRecognizer(shapeData);
            dollar.Save("Dollar.dr");

            RubineRecognizerUpdateable rubine = new RubineRecognizerUpdateable();
            rubine.Save("Rubine.rru");
            rubine.LiteRecognizer.Save("RubineLite.rr");

            DollarRecognizer dollar = new DollarRecognizer();
            dollar.Save("Dollar.dr");

            Console.WriteLine("    zernike");
            ZernikeMomentRecognizerUpdateable zernike = new ZernikeMomentRecognizerUpdateable(shapeData);
            zernike.Save("Zernike.zru");
            zernike.LiteRecognizer.Save("ZernikeLite.zr");

#endif

            Console.WriteLine("    adaptive image");
            AdaptiveImageRecognizer adaptiveimage = new AdaptiveImageRecognizer(shapeData);
            adaptiveimage.Save("AdaptiveImage.air");

            Console.WriteLine("    image");
            ImageRecognizer image = new ImageRecognizer(shapeData);
            image.Save("Image.ir");

            Console.WriteLine("    finished training recognizers");

#if false

            RubineRecognizer fullRubine = rubine.LiteRecognizer;
            DollarRecognizer fullDollar = dollar;
            ZernikeMomentRecognizer fullZernike = zernike.LiteRecognizer;

            ImageRecognizer fullImage = image;

            // Split the data up per-user
            Console.WriteLine("Loading per-user data...");
            Dictionary<string, List<Shape>[]> user2data = GetSketchesPerUser(allSketches);
            Console.WriteLine("    found " + user2data.Count + " users");


            // Foreach user: train each of the recognizers and accumulate training data
            // for the combo recognizer
            List<KeyValuePair<ShapeType, Dictionary<string, object>>> data = new List<KeyValuePair<ShapeType, Dictionary<string, object>>>();
            foreach (KeyValuePair<string, List<Shape>[]> pair in user2data)
            {
                string user = pair.Key;

                ////////////////////////////////////////
                ////////////   Train   /////////////////
                ////////////////////////////////////////

                Console.WriteLine("User: " + user);
                List<Shape> trainingSet = pair.Value[0];

#if false
                Console.WriteLine("    rubine");
                rubine = new RubineRecognizerUpdateable(trainingSet);
                rubine.Save("Rubine" + user + ".rru");
                rubine.LiteRecognizer.Save("RubineLite" + user + ".rr");

                Console.WriteLine("    dollar");
                dollar = new DollarRecognizer(trainingSet);
                dollar.Save("Dollar" + user + ".dr");
#else
                rubine = new RubineRecognizerUpdateable();
                rubine.Save("Rubine" + user + ".rru");
                rubine.LiteRecognizer.Save("RubineLite" + user + ".rr");

                dollar = new DollarRecognizer();
                dollar.Save("Dollar" + user + ".dr");
#endif

                Console.WriteLine("    zernike");
                zernike = new ZernikeMomentRecognizerUpdateable(trainingSet);
                zernike.Save("Zernike" + user + ".zru");
                zernike.LiteRecognizer.Save("ZernikeLite" + user + ".zr");

                Console.WriteLine("    image");
                image = new ImageRecognizer(trainingSet);
                image.Save("Image" + user + ".ir");
                fullImage = image;

                ////////////////////////////////////////
                //////////// Evaluate //////////////////
                ////////////////////////////////////////


                List<Shape> testingSet = pair.Value[1];

                // Create the training data for the combo recognizer
                List<KeyValuePair<ShapeType, Dictionary<string, object>>> comboTrainingData = TrainingDataCombo(testingSet, rubine, dollar, zernike, image);
                foreach (KeyValuePair<ShapeType, Dictionary<string, object>> pair2 in comboTrainingData)
                    data.Add(pair2);
            }

            if (data.Count == 0)
                throw new Exception("no data!");

            List<string> features = new List<string>();
            foreach (KeyValuePair<ShapeType, Dictionary<string, object>> instance in data)
                foreach (string feature in instance.Value.Keys)
                    if (!features.Contains(feature))
                        features.Add(feature);

            Console.WriteLine("Found " + data.Count + " data points and " + features.Count + " features.");

            ComboRecognizer combo = new ComboRecognizer(fullRubine, fullDollar, fullZernike, fullImage);
            combo.TrainCombo(features, data);
            combo.Save("Combo.cru");

            Console.WriteLine("Naive bayes updatable has " + combo.ComboClassifier.Examples.Count + " examples.");
            Console.WriteLine("Naive bayes updatable has " + combo.ComboClassifier.Classifier.Classes.Count + " classes:");
            foreach (ShapeType cls in combo.ComboClassifier.Classifier.Classes)
            {
                Console.WriteLine("    " + cls);
            }

#endif

            Console.WriteLine("Training neural image recognizer on real-world data...");
            List<Shape> goodGates; // list of correctly-identified gates
            List<Shape> badGates;  // list of shapes grouped as gates that aren't
            Dictionary<Shape, string> shapeSources; // map of shapes to source filename

            string cacheFile = outputPath + "goodAndBadGates.data";
            if (!System.IO.File.Exists(cacheFile))
            {
                goodGates = new List<Shape>();
                badGates = new List<Shape>();
                shapeSources = new Dictionary<Shape, string>();

                Grouper grouper = RecognitionPipeline.createDefaultGrouper();
                Classifier classifier = RecognitionPipeline.createDefaultClassifier();
                RecognitionPipeline pipeline = new RecognitionPipeline(classifier, grouper);
                var files = Files.FUtil.AllFiles(args[1], Files.Filetype.XML, true);
                Console.WriteLine("    Found " + files.Count() + " real-world sketches");
                int i = 1;
                foreach (string file in files)
                {
                    Console.WriteLine("    Sketch " + i + " / " + files.Count());
                    i++;

                    Sketch.Sketch sketch = new ReadXML(file).Sketch;
                    Sketch.Sketch original = sketch.Clone();

                    sketch.RemoveLabels();
                    sketch.resetShapes();

                    pipeline.process(sketch);

                    foreach (Sketch.Shape shape in sketch.Shapes)
                    {
                        if (shape.Classification != LogicDomain.GATE_CLASS)
                            continue;

                        Shape originalGate = original.ShapesL.Find(delegate(Shape s) { return s.GeometricEquals(shape); });

                        if (originalGate != null && originalGate.Classification == LogicDomain.GATE_CLASS)
                            goodGates.Add(shape);
                        else
                        {
                            // We can't just say "this is a bad gate." If it wasn't found,
                            // the shape might be an XOR gate missing the back, or a NAND
                            // gate missing a bubble. We will apply the following heuristic:
                            //    if all the strokes in the shape are part of the same
                            //    shape in the original sketch and that shape in the
                            //    original sketch is a gate, this is not a bad gate.

                            // a shape consists of one or more substrokes from shapes in the
                            // original, correct sketch
                            HashSet<Shape> originalShapes = new HashSet<Shape>();
                            foreach (Substroke substroke in shape.Substrokes)
                            {
                                Substroke originalSubstroke = original.SubstrokesL.Find(delegate(Substroke s) { return s.GeometricEquals(substroke); });
                                if (originalSubstroke == null)
                                    throw new Exception("A substroke is missing in the original sketch???");
                                if (originalSubstroke.ParentShape != null)
                                    originalShapes.Add(originalSubstroke.ParentShape);
                            }

                            List<Shape> originalShapesL = originalShapes.ToList();
                            if (originalShapesL.Count != 1 || originalShapesL[0].Classification != LogicDomain.GATE_CLASS)
                                badGates.Add(shape);
                        }
                        shapeSources.Add(shape, file);
                    }
                }

                Console.WriteLine("Saving found gates to " + cacheFile);
                var stream = System.IO.File.Open(cacheFile, System.IO.FileMode.Create);
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, Tuple.Create(goodGates, badGates, shapeSources));
                stream.Close();
            }
            else
            {
                Console.WriteLine("Loading good and bad gates from " + cacheFile);
                var stream = System.IO.File.Open(cacheFile, System.IO.FileMode.Open);
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                var data = (Tuple<List<Shape>, List<Shape>, Dictionary<Shape, string>>)bformatter.Deserialize(stream);
                stream.Close();

                goodGates = data.Item1;
                badGates = data.Item2;
                shapeSources = data.Item3;
            }
            Console.WriteLine("    Found " + goodGates.Count + " good gates, " + badGates.Count + " bad gates");

            ImageRecognizer innerNeuralRecgonizer = image;
            string neuralPath = @"neuralResults\";
            string arffFilename = "data.arff";
            if (!System.IO.Directory.Exists(neuralPath))
                System.IO.Directory.CreateDirectory(neuralPath);

            Console.WriteLine("    Writing ARFF file '"+neuralPath + arffFilename +"'...");
            TextWriter arffWriter = new StreamWriter(neuralPath + arffFilename);
            NeuralImageRecognizer.WriteARFF(arffWriter, innerNeuralRecgonizer, goodGates, badGates);
            arffWriter.Close();

            Console.WriteLine("    Training the network...");

            // Network settings -- determined empircally
            NeuralNetworkInfo info = new NeuralNetworkInfo();
            info.Layers = new int[] { 8, 1 };
            info.NumTrainingEpochs = 1000;
            info.LearningRate = 0.05;
            info.Momentum = 0.2;
            NeuralImageRecognizer neuralImage = new NeuralImageRecognizer(innerNeuralRecgonizer, goodGates, badGates, info);

            neuralImage.Save("NeuralImage.nir");


            Console.WriteLine("    Testing the network (results in "+neuralPath+")...");
            neuralImage = NeuralImageRecognizer.Load("NeuralImage.nir");

            TextWriter writer = new StreamWriter(neuralPath + "info.csv");

            writer.WriteLine("Sketch File, Shape Bitmap, Good?, Tanimoto, Yule, Partial Hausdorff, Modified Hausdorff, Output Confidence");

            int falseNegatives = 0;
            foreach (Shape gate in goodGates)
            {
                ImageRecognitionResult result = (ImageRecognitionResult)neuralImage.recognize(gate, null);
                if (result.Confidence < 0.5)
                    falseNegatives++;

                System.Drawing.Bitmap b = gate.createBitmap(100, 100, true);
                string filename = String.Format("good-" + "-{0:x}.png", gate.GetHashCode());
                b.Save(neuralPath + filename);

                writer.WriteLine(shapeSources[gate] + "," + filename + 
                    ", 1, " + 
                    result.Tanimoto + ", " + 
                    result.Yule + ", " + 
                    result.PartialHausdorff + ", " + 
                    result.ModifiedHausdorff + ", " + 
                    result.Confidence);
            }
            Console.WriteLine("    Good gates with low confidence: " + falseNegatives + "/" + (goodGates.Count));

            int falsePositives = 0;
            foreach (Shape gate in badGates)
            {
                ImageRecognitionResult result = (ImageRecognitionResult)neuralImage.recognize(gate, null);
                if (result.Confidence > 0.5)
                    falsePositives++;

                System.Drawing.Bitmap b = gate.createBitmap(100, 100, true);
                string filename = String.Format("bad-" + "-{0:x}.png", gate.GetHashCode());
                b.Save(neuralPath + filename);

                writer.WriteLine(shapeSources[gate] + "," + filename +
                    ", 0, " +
                    result.Tanimoto + ", " +
                    result.Yule + ", " +
                    result.PartialHausdorff + ", " +
                    result.ModifiedHausdorff + ", " +
                    result.Confidence);
            }
            Console.WriteLine("    Bad gates with high confidence: " + falsePositives + "/" + (badGates.Count));
            writer.Close();

            Console.WriteLine("Press ENTER to continue...");
            Console.ReadLine();

        }

        /// <summary>
        /// Get a dictionary mapping users -> array of shape lists.
        /// 
        /// The first entry in the array is training set.
        /// 
        /// The second entry is the testing set.
        /// </summary>
        /// <param name="files">the list of files to read</param>
        /// <returns>the mapping described above</returns>
        private static Dictionary<string, List<Shape>[]> GetSketchesPerUser(List<string> files)
        {
            Dictionary<string, List<string>[]> sketches = new Dictionary<string, List<string>[]>();

            foreach (string f in files)
            {
                string fShort = Path.GetFileName(f);
                string user = fShort.Substring(0, fShort.IndexOf('_')); // everything to first underscore
                if (fShort.Contains("_T"))
                    user += "T";
                else if (fShort.Contains("_P"))
                    user += "P";

                if (!sketches.ContainsKey(user))
                {
                    sketches.Add(user, new List<string>[2]);
                    sketches[user][1] = new List<string>();
                    sketches[user][0] = new List<string>();
                }
               
                if (fShort.Contains("EQ") || fShort.Contains("COPY"))
                    sketches[user][1].Add(f);
                else
                    sketches[user][0].Add(f);
            }

            Dictionary<string, List<Shape>[]> result = new Dictionary<string, List<Shape>[]>();

            foreach (KeyValuePair<string, List<string>[]> pair in sketches)
            {
                string user = pair.Key;
                List<string> trainingSet = pair.Value[0];
                List<string> testingSet  = pair.Value[1];

                result.Add(user, new List<Shape>[] { 
                    GetShapeData(trainingSet),
                    GetShapeData(testingSet)
                });

            }

            return result;
        }

        /// <summary>
        /// Construct the training data for the combo recognizer.
        /// </summary>
        /// <param name="testData">the data to test on</param>
        /// <param name="rubine">the rubine recognizer</param>
        /// <param name="dollar">the dollar recognizer</param>
        /// <param name="zernike">the zernike recognizer</param>
        /// <param name="image">the image recognizer</param>
        /// <returns>a list of tuples (s,f) where s is a shape type and f is a set of features</returns>
        private static List<KeyValuePair<ShapeType, Dictionary<string, object>>> TrainingDataCombo(
            List<Shape> testData, 
            RubineRecognizerUpdateable rubine, 
            DollarRecognizer dollar, 
            ZernikeMomentRecognizerUpdateable zernike,
            ImageRecognizer image)
        {
            List<KeyValuePair<ShapeType, Dictionary<string, object>>> data = new List<KeyValuePair<ShapeType, Dictionary<string, object>>>();

            foreach (Shape shape in testData)
            {
                string z = zernike.Recognize(shape.SubstrokesL);

                List<ShapeType> img = image.Recognize(shape.SubstrokesL);

                List<string> r = new List<string>();
                List<string> dAvg = new List<string>();
                List<string> d = new List<string>();

                foreach (Substroke s in shape.SubstrokesL)
                {
                    r.Add(rubine.Recognize(s));
                    dAvg.Add(dollar.RecognizeAverage(s));
                    d.Add(dollar.Recognize(s));
                }

                Dictionary<string, object> features = ComboRecognizer.GetFeatures(shape.SubstrokesL.Count, z, img, r, dAvg, d);
                data.Add(new KeyValuePair<ShapeType, Dictionary<string, object>>(shape.Type, features));
            }

            return data;
        }

        /// <summary>
        /// Extracts all of the labeled shapes from a set of sketches. It returns only
        /// gates.
        /// </summary>
        /// <param name="sketchFiles">the list of filenames of labeled sketches</param>
        /// <returns>a list of labeled shapes</returns>
        private static List<Shape> GetShapeData(List<string> sketchFiles)
        {
            List<Shape> result = new List<Shape>();

            foreach (string file in sketchFiles)
            {
                Sketch.Sketch sketch = new ReadXML(file).Sketch;
                if (sketch == null)
                    continue;

                foreach (Shape shape in sketch.Shapes)
                {
                    if (shape.LowercasedType == "unknown")
                    {
                        Console.WriteLine("    Found unlabled shape in '" + file + "'");
                        continue;
                    }

                    result.Add(shape);
                }
            }

            // Keep only gates
            List<Shape> filteredData = new List<Shape>();
            foreach (Shape shape in result)
                if (shape.Type.Classification == LogicDomain.GATE_CLASS)
                    filteredData.Add(shape);
            result = filteredData;

            return result;
        }

    }
}
