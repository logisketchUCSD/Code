using Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;


namespace Utilities.NaiveBayes
{
    /// <summary>
    /// Discrete or Discretized values only.
    /// This NaiveBayes Classifier must be initialized with some 
    /// values that have already been calculated, probably in the
    /// NaiveBayesUpdateable, i.e. it does NOT have training methods.
    /// </summary>
    [Serializable]
    public class NaiveBayes
    {
        #region Constants and Parameters

        const double MIN_PROBABILITY = 0.0001;

        #endregion

        #region Member Variables

        /// <summary>
        /// All classes known to the classifier
        /// </summary>
        List<ShapeType> _classes;

        /// <summary>
        /// All features to be used in classification
        /// </summary>
        List<string> _features;

        /// <summary>
        /// Feature Probability Distribution is the calculated probability
        /// of observing a feature's value given a class. The layer structure 
        /// is as follows:
        ///     (1): Feature Name to (2)
        ///     (2): Feature Value to (3)
        ///     (3): Class Name to Probability
        /// 
        /// Usage to get the probability that (Feature Fname) = (Value Vvalue), given 
        /// that the (Class = Cname): 
        ///     double prob_F_given_C = _FeatureProbabilityDistribution[Fname][Vvalue][Cname];
        /// </summary>
        Dictionary<string, Dictionary<object, Dictionary<ShapeType, double>>> _FeatureProbabilityDistribution;

        /// <summary>
        /// The likelihood that a given feature will have a specific value.
        /// Layer structure is as follows:
        ///     (1): Feature Name to (2)
        ///     (2): Feature Value to Probability
        /// 
        /// Usage to get the probability that (Feature Fname) = (Value Vvalue):
        ///     double prob_F = _allLikelihoods[Fname][Vvalue];
        /// </summary>
        Dictionary<string, Dictionary<object, double>> _allLikelihoods;

        /// <summary>
        /// Prior probability of each class
        /// </summary>
        Dictionary<ShapeType, double> _priors;

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor, only to be used temporarily.
        /// </summary>
        public NaiveBayes()
        {
            _classes = new List<ShapeType>();
        }

        /// <summary>
        /// Constructor which takes arguments from the NaiveBayesUpdateable.
        /// Has everything needed for classification.
        /// </summary>
        /// <param name="classes">Names of all the classes</param>
        /// <param name="features">Names of all the features</param>
        /// <param name="priorsPerClass">Prior probabilities for a class</param>
        /// <param name="allLikelyhoods">Probabilities of each feature = value (p_(F))</param>
        /// <param name="likelyhoodPerClass">Feature Value Probability Distribution (p_(F|C)</param>
        public NaiveBayes(List<ShapeType> classes, List<string> features,
            Dictionary<ShapeType, double> priorsPerClass,
            Dictionary<string, Dictionary<object, double>> allLikelyhoods,
            Dictionary<string, Dictionary<object, Dictionary<ShapeType, double>>> likelyhoodPerClass)
        {
            _classes = classes;
            _features = features;
            _priors = priorsPerClass;
            _allLikelihoods = allLikelyhoods;
            _FeatureProbabilityDistribution = likelyhoodPerClass;
        }

        #endregion

        #region Classification

        /// <summary>
        /// Classifies an instance based on its feature values
        /// </summary>
        /// <param name="featureValues">All computed values for this instance</param>
        /// <returns>Ranked list of expected classes, with their "probability"</returns>
        public Dictionary<ShapeType, double> Classify(Dictionary<string, object> featureValues)
        {
            Dictionary<ShapeType, double> classLikelihoods = new Dictionary<ShapeType, double>(_classes.Count);

            // The evidence probably isn't needed, but I put it in just in case. 
            // The reason it may not be needed is that it's a constant across
            // all classes, and since we are meerly trying to find the most 
            // probable class, it shouldn't effect things. However I think it 
            // may effect the probability distribution for the classes.
            // evidence is the product of all p_(F)'s
            double evidence = 0.0;
            foreach (string fName in _features)
            {
                object value = featureValues[fName];
                double p = MIN_PROBABILITY;
                if (_allLikelihoods[fName].ContainsKey(value))
                    p = _allLikelihoods[fName][value];
                evidence += Math.Log(p);
            }
            evidence = Math.Exp(evidence);

            // Go through each class and calculate it's posterior probability
            foreach (ShapeType cls in _classes)
            {
                double prior;
                bool fPrior = _priors.TryGetValue(cls, out prior);

                if (!fPrior)
                    continue;

                // likelihood meaning the product of all p_(F|C)'s
                double likelihood = 0.0;
                foreach (string fName in _features)
                {
                    object value = featureValues[fName];
                    double p = MIN_PROBABILITY;
                    if (_FeatureProbabilityDistribution[fName].ContainsKey(value) 
                        && _FeatureProbabilityDistribution[fName][value].ContainsKey(cls))
                        p = _FeatureProbabilityDistribution[fName][value][cls];
                    likelihood += Math.Log(p);
                }
                likelihood = Math.Exp(likelihood);

                double posterior = likelihood * prior;// / evidence;
                classLikelihoods.Add(cls, posterior);
            }

            // classLikelihoods is NOT sorted, so create a new Dictionary that is sorted
            Dictionary<ShapeType, double> output = new Dictionary<ShapeType, double>(classLikelihoods.Count);
            List<double> scores = new List<double>(classLikelihoods.Values);
            scores.Sort();

            // Sort() does lowest -> highest, so we want to reverse the order
            scores.Reverse();


            double sum = 0.0;
            foreach (double s in scores)
                sum += s;

            foreach (double s in scores)
                foreach (KeyValuePair<ShapeType, double> pair in classLikelihoods)
                    if (pair.Value == s && !output.ContainsKey(pair.Key))
                        output.Add(pair.Key, pair.Value / sum);

            return output;
        }

        #endregion

        #region Getters & Setters

        public List<ShapeType> Classes
        {
            get { return _classes; }
        }

        #endregion

        #region Serialization
        
        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public NaiveBayes(SerializationInfo info, StreamingContext ctxt)
        {
            // Get the values from info and assign them to the appropriate properties
            _features = (List<string>)info.GetValue("features", typeof(List<string>));
            _allLikelihoods = (Dictionary<string, Dictionary<object, double>>)
                       info.GetValue("allLikelihoods", typeof(Dictionary<string, Dictionary<object, double>>));
            _FeatureProbabilityDistribution = (Dictionary<string, Dictionary<object, Dictionary<ShapeType, double>>>)
                        info.GetValue("FeatureProbabilityDistribution", typeof(Dictionary<string, Dictionary<object, Dictionary<string, double>>>));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("features", _features);
            info.AddValue("allLikelihoods", _allLikelihoods);
            info.AddValue("FeatureProbabilityDistribution", _FeatureProbabilityDistribution);
        }

        #endregion
    }
}
