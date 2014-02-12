using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Utilities.NaiveBayes
{
    /// <summary>
    /// This is a Naive Bayesian Classifier that holds each of its 
    /// training instances so that it is updateable. This classifier 
    /// is fairly generic in that feature values are objects - that is
    /// they aren't hard-coded to be int/string/etc. If you are going to 
    /// be saving the classifier, make sure all object types you use are 
    /// serializable. As the number of training examples grows it becomes 
    /// more cumbersome to save and load these classifiers since all examples 
    /// are saved, for this reason you can get a NaiveBayes which is not 
    /// updateable, but is pretty light-weight. This NaiveBayes is just the
    /// classifier which has pre-computed values.
    /// </summary>
    [Serializable]
    public class NaiveBayesUpdateable
    {
        #region Constants and Parameters

        /// <summary>
        /// Use this probability to make sure that we don't run into
        /// 0.0 or 1.0 probabilities which would dominate classification
        /// </summary>
        private const double MIN_PROBABILITY = 0.0001;

        #endregion

        #region Member Variables

        /// <summary>
        /// The actual classifier 
        /// </summary>
        NaiveBayes _classifier;

        /// <summary>
        /// List of all examples used to train the classifier
        /// Key = class name
        /// Value = all the feature values
        /// </summary>
        List<KeyValuePair<string, Dictionary<string, object>>> _examples;

        /// <summary>
        /// List of all the feature names used in the classifier
        /// </summary>
        List<string> _featureNames;

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor.
        /// Should not really be used, unless you're replacing it later.
        /// </summary>
        public NaiveBayesUpdateable()
        {
            _examples = new List<KeyValuePair<string, Dictionary<string, object>>>();
        }

        /// <summary>
        /// Constructor, in order to create this updateable classifier we 
        /// need to have a list of the features we can expect from examples
        /// that will be added.
        /// </summary>
        /// <param name="featureNames">List of all features' names</param>
        public NaiveBayesUpdateable(List<string> featureNames)
        {
            _examples = new List<KeyValuePair<string, Dictionary<string, object>>>();
            _featureNames = featureNames;
        }

        #endregion

        #region Interface Functions

        /// <summary>
        /// Add a training example to the classifier
        /// </summary>
        /// <param name="className">The className is the actual class - expected outcome of the classifier</param>
        /// <param name="features">Feature values indexed by their names</param>
        public void AddExample(ShapeType className, Dictionary<string, object> features)
        {
            bool good = true;
            if (features.Count != _featureNames.Count)
                good = false;

            foreach (string featureName in _featureNames)
                if (!features.ContainsKey(featureName))
                    good = false;

            if (good)
                _examples.Add(new KeyValuePair<string, Dictionary<string, object>>(className.Name, features));
        }

        /// <summary>
        /// Essentially re-trains the classifier using the stored instances
        /// </summary>
        public void UpdateClassifier()
        {
            // List of each class - go through all examples once to get complete list of classes
            List<ShapeType> classes = new List<ShapeType>();
            foreach (KeyValuePair<string, Dictionary<string, object>> example in _examples)
                if (!classes.Contains(LogicDomain.getType(example.Key)))
                    classes.Add(LogicDomain.getType(example.Key));


            #region Initialize Probability stuff
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            // 4 Pieces of information needed to calculate all 
            // feature likelyhoods given class

            // needed for priors and feature value likelyhood
            int numExamples = _examples.Count;
            if (numExamples == 0)
                return;

            // Prior probabilities of a class
            // Initialize them all to have 0.0 prior probability
            Dictionary<ShapeType, double> Priors = new Dictionary<ShapeType, double>();
            foreach (ShapeType cls in classes)
                Priors.Add(cls, 0.0);

            // Likelyhood of a feature value
            // Initialize all top level dictionary entries
            Dictionary<string, Dictionary<object, double>> FeatureValue_Likelyhood =
                new Dictionary<string, Dictionary<object, double>>();
            foreach (string feature in _featureNames)
                FeatureValue_Likelyhood.Add(feature, new Dictionary<object, double>());

            // Likelyhood of a class given a feature value
            // Initialize all top and 2nd level dictionary entries
            Dictionary<string, Dictionary<object, Dictionary<ShapeType, double>>> Class_Given_FeatureValue_Likelyhood =
                new Dictionary<string, Dictionary<object, Dictionary<ShapeType, double>>>();
            foreach (string feature in _featureNames)
                Class_Given_FeatureValue_Likelyhood.Add(feature, new Dictionary<object, Dictionary<ShapeType, double>>());
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            #endregion


            #region Initialize Counting Stuff
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            // Counts necessary to calculate likelyhoods

            // How many occurances there are of each class - for prior probabilities
            // Initialize all the class counts to 0
            Dictionary<string, int> Count_Class = new Dictionary<string, int>();
            foreach (ShapeType cls in classes)
                Count_Class.Add(cls.Name, 0);

            // For FeatureValueLikelyhood 
            //   - Count of the number of times Feature_i = f
            // Initialize all top level dictionary entries
            Dictionary<string, Dictionary<object, int>> Count_Feature_Eq_f =
                new Dictionary<string, Dictionary<object, int>>(_featureNames.Count);
            foreach (string feature in _featureNames)
                Count_Feature_Eq_f.Add(feature, new Dictionary<object, int>());

            // Given a class, how many times was this value observed
            // For ClassGivenFeatureLikelyhood
            //   - Count of the number of times Class_j = c AND Feature_i = f
            // Initialize all top and 2nd level dictionary entries
            // First dictionary = feature name to 2nd dictionary
            // Second dictionary = feature value to 3rd dictionary
            // Third dictionary = class name to occurance count
            Dictionary<string, Dictionary<object, Dictionary<string, int>>> Count_Class_Eq_c_given_Feature_Eq_f =
                new Dictionary<string, Dictionary<object, Dictionary<string, int>>>();
            foreach (string feature in _featureNames)
                Count_Class_Eq_c_given_Feature_Eq_f.Add(feature, new Dictionary<object, Dictionary<string, int>>());
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            #endregion


            #region Go through every single example and count feature occurances and class occurances
            foreach (KeyValuePair<string, Dictionary<string, object>> example in _examples)
            {
                string className = example.Key;
                Count_Class[className]++;

                Dictionary<string, object> features = example.Value;

                // Count the number of occurances of each feature value.
                foreach (string fName in _featureNames)
                {
                    // feature value
                    object value;

                    if (features.TryGetValue(fName, out value))
                    {
                        // count of instances of this value across all classes
                        Dictionary<object, int> count;
                        if (Count_Feature_Eq_f.TryGetValue(fName, out count))
                        {
                            if (count.ContainsKey(value))
                                count[value]++;
                            else
                                count.Add(value, 1);
                        }

                        // count of instances of this value in this class ONLY
                        Dictionary<string, int> countPerClass;
                        if (Count_Class_Eq_c_given_Feature_Eq_f[fName].TryGetValue(value, out countPerClass))
                        {
                            if (countPerClass.ContainsKey(className))
                                countPerClass[className]++;
                            else
                                countPerClass.Add(className, 1);
                        }
                        else
                        {
                            Dictionary<string, int> clsCount = new Dictionary<string, int>();
                            clsCount.Add(className, 1);
                            Count_Class_Eq_c_given_Feature_Eq_f[fName].Add(value, clsCount);
                        }
                    }
                }
            }
            #endregion


            #region Calculate all the probabilities

            // Get the prior probabilities for each class
            foreach (ShapeType cls in classes)
            {
                int count;
                if (Count_Class.TryGetValue(cls.Name, out count))
                {
                    double prior = (double)count / numExamples;
                    Priors[cls] = prior;
                }
            }

            // Likelyhood for feature value throughout all classes
            foreach (string fName in _featureNames)
            {
                Dictionary<object, int> count_for_Feature_i;
                if (Count_Feature_Eq_f.TryGetValue(fName, out count_for_Feature_i))
                {
                    foreach (KeyValuePair<object, int> pair in count_for_Feature_i)
                    {
                        double p_F = (double)pair.Value / numExamples;
                        p_F = Math.Min(Math.Max(p_F, MIN_PROBABILITY), 1.0 - MIN_PROBABILITY);
                        if (FeatureValue_Likelyhood[fName].ContainsKey(pair.Key))
                            FeatureValue_Likelyhood[fName][pair.Key] = p_F;
                        else
                            FeatureValue_Likelyhood[fName].Add(pair.Key, p_F);
                    }
                }
            }

            // Likelyhood for feature value per class
            foreach (string fName in _featureNames)
            {
                foreach (KeyValuePair<object, Dictionary<string, int>> pair in Count_Class_Eq_c_given_Feature_Eq_f[fName])
                {
                    object value = pair.Key;
                    Class_Given_FeatureValue_Likelyhood[fName].Add(value, new Dictionary<ShapeType, double>());
                    double p_F = FeatureValue_Likelyhood[fName][value];


                    int sum = 0;
                    foreach (KeyValuePair<string, int> clsCount in pair.Value)
                        sum += clsCount.Value;

                    foreach (ShapeType cls in classes)
                    {
                        if (pair.Value.ContainsKey(cls.Name))
                        {
                            double p_C = Priors[cls];
                            double v = (double)pair.Value[cls.Name] / sum;
                            double p_C_given_F = Math.Min(Math.Max(v, MIN_PROBABILITY), 1.0 - MIN_PROBABILITY);
                            double p_F_given_C = p_C_given_F * p_F / p_C;
                            Class_Given_FeatureValue_Likelyhood[fName][value].Add(cls, p_F_given_C);
                        }
                        else
                            Class_Given_FeatureValue_Likelyhood[fName][value].Add(cls, MIN_PROBABILITY);
                    }
                }
            }


            #endregion

            // Sort the dictionaries...for fun and ease of reading when debugging
            Dictionary<string, Dictionary<object, double>> fvl = new Dictionary<string, Dictionary<object, double>>();
            foreach (KeyValuePair<string, Dictionary<object, double>> pair in FeatureValue_Likelyhood)
            {
                List<object> v_keys = new List<object>(pair.Value.Keys);
                v_keys.Sort();
                Dictionary<object, double> v = new Dictionary<object, double>();
                foreach (object value in v_keys)
                    v.Add(value, pair.Value[value]);

                fvl.Add(pair.Key, v);
            }

            List<ShapeType> p_keys = new List<ShapeType>(Priors.Keys);
            p_keys.Sort();
            Dictionary<ShapeType, double> p = new Dictionary<ShapeType, double>();
            foreach (ShapeType key in p_keys)
                p.Add(key, Priors[key]);


            Dictionary<string, Dictionary<object, Dictionary<ShapeType, double>>> cgfvl = new Dictionary<string, Dictionary<object, Dictionary<ShapeType, double>>>();
            foreach (KeyValuePair<string, Dictionary<object, Dictionary<ShapeType, double>>> pair in Class_Given_FeatureValue_Likelyhood)
            {
                List<object> v_keys = new List<object>(pair.Value.Keys);
                v_keys.Sort();
                Dictionary<object, Dictionary<ShapeType, double>> v = new Dictionary<object, Dictionary<ShapeType, double>>();
                foreach (object value in v_keys)
                    v.Add(value, pair.Value[value]);

                cgfvl.Add(pair.Key, v);
            }


            // Update the Classifier
            _classifier = new NaiveBayes(classes, _featureNames, p, fvl, cgfvl);
        }

        #endregion

        #region Classification / Recognition

        /// <summary>
        /// Classifies an instance based on a set of feature values
        /// </summary>
        /// <param name="instance">Feature values, indexed by their name</param>
        /// <returns>Ranked results of the classification, class name to "probability"</returns>
        public Dictionary<ShapeType, double> Classify(Dictionary<string, object> instance)
        {
            return Classifier.Classify(instance);
        }

        #endregion

        #region Getters

        public NaiveBayes Classifier
        {
            get 
            {
                if (_classifier == null)
                    UpdateClassifier();

                return _classifier; 
            }
        }

        public List<KeyValuePair<string, Dictionary<string, object>>> Examples
        {
            get { return _examples; }
        }

        #endregion

        #region Serialization
        
        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public NaiveBayesUpdateable(SerializationInfo info, StreamingContext ctxt)
        {
            // Get the values from info and assign them to the appropriate properties
            _classifier = (NaiveBayes)info.GetValue("classifier", typeof(NaiveBayes));
            _featureNames = (List<string>)info.GetValue("featureNames", typeof(List<string>));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("classifier", _classifier);
            info.AddValue("featureNames", _featureNames);
        }

        #endregion

    }
}
