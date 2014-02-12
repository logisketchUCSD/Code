using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Sketch;

namespace SubRecognizer
{
    /// <summary>
    /// This recognizer is basically a wrapper around the DollarTemplates, 
    /// which uses the standard Recognize(stroke) function call (Also uses a 
    /// RecognizeAverage(stroke) call for the average templates).
    /// The Dollar Recognizer has both the normal implementation of Dollar
    /// Templates as well as "Averaged" templates. 
    /// </summary>
    [Serializable]
    public class DollarRecognizer : ISerializable
    {

        static List<string> leafLabels = new List<string>(new string[] {
                "BackLine", "FrontArc", "BackArc", "TopArc", "BottomArc",
                "Bubble", "TopLine", "BottomLine", "Triangle", 
                "GreaterThan", "TouchUp", "Junk", 
                "Entire_OR", "Entire_AND" });

        #region Member Variables

        /// <summary>
        /// List of all the Dollar Templates that have been created
        /// using averaged points.
        /// </summary>
        List<DollarTemplateAverage> _averageTemplates;

        /// <summary>
        /// List of all the Dollar Templates (normal).
        /// </summary>
        List<DollarTemplate> _templates;

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DollarRecognizer()
        {
            _averageTemplates = new List<DollarTemplateAverage>();
            _templates = new List<DollarTemplate>();
        }

        /// <summary>
        /// Create a trained dollar recognizer from the given data.
        /// </summary>
        /// <param name="data">a list of labeled sketches</param>
        public DollarRecognizer(List<Shape> data)
            : this()
        {
            bool hasExamples = false;
            foreach (Shape shape in data)
            {
                foreach (Substroke substroke in shape.Substrokes)
                {
                    string label = substroke.Type.Name;
                    if (leafLabels.Contains(label))
                    {
                        AddExample(substroke, label);
                        hasExamples = true;
                    }
                }
            }

            if (!hasExamples)
            {
                throw new Exception(
                    "The dollar recognizer did not find any properly labeled " +
                    "substrokes in the given data set. It requires individually " +
                    "labeled substrokes. See DollarRecognizer.leafLabels for a " +
                    "list of valid substroke labels.");
            }
        }

        #endregion

        #region Interface Functions

        /// <summary>
        /// If there are more than maxTemplatesPerClass in any class, this
        /// function goes through and trims them randomly so that there is 
        /// a tractable number of templates for a given class.
        /// </summary>
        /// <param name="maxTemplatesPerClass">Max number of templates to 
        /// be in any class after trimming</param>
        public void TrimTemplates(int maxTemplatesPerClass)
        {
            List<DollarTemplate> newTemplates = new List<DollarTemplate>();
            Dictionary<string, List<DollarTemplate>> clsToTemplates = new Dictionary<string, List<DollarTemplate>>();

            foreach (DollarTemplate dt in _templates)
            {
                string cls = dt.Name;
                if (clsToTemplates.ContainsKey(cls))
                    clsToTemplates[cls].Add(dt);
                else
                    clsToTemplates.Add(cls, new List<DollarTemplate>(new DollarTemplate[] { dt }));
            }

            foreach (KeyValuePair<string, List<DollarTemplate>> pair in clsToTemplates)
            {
                List<DollarTemplate> temps = pair.Value;
                Random r = new Random();
                List<int> indices = new List<int>();
                int index = 0;
                int count = 0;
                if (temps.Count >= maxTemplatesPerClass)
                {
                    while (indices.Count < maxTemplatesPerClass && count < 10000)
                    {
                        count++;
                        index = r.Next(0, temps.Count - 1);
                        if (!indices.Contains(index))
                        {
                            newTemplates.Add(temps[index]);
                            indices.Add(index);
                        }
                    }
                }
                else
                    foreach (DollarTemplate t in temps)
                        newTemplates.Add(t);
            }

            _templates = newTemplates;
        }

        /// <summary>
        /// Adds another template based on a stroke and it's class (name).
        /// Creates both a normal template and adds that to the overall list, 
        /// as well as an "Average" template (or updates the "Average" template
        /// if it already exists).
        /// </summary>
        /// <param name="stroke">Stroke to create template from</param>
        /// <param name="name">Class name for the stroke</param>
        public void AddExample(Substroke stroke, string name)
        {
            if (stroke.SpatialLength != double.NaN && stroke.SpatialLength > 0.0)
            {
                _templates.Add(new DollarTemplate(stroke.PointsL, name));

                bool foundTemplate = false;
                foreach (DollarTemplateAverage temp in _averageTemplates)
                {
                    if (temp.Name == name)
                    {
                        foundTemplate = true;
                        temp.AddExample(stroke.PointsL);
                    }
                }

                if (!foundTemplate)
                    _averageTemplates.Add(new DollarTemplateAverage(stroke.PointsL, name));
            }
        }

        #endregion

        #region Recognition

        /// <summary>
        /// Gets the result for the best matching "Average" template
        /// </summary>
        /// <param name="stroke">Stroke to be recognized</param>
        /// <returns>Class name of the best match</returns>
        public string RecognizeAverage(Substroke stroke)
        {
            DollarTemplateAverage unknown = new DollarTemplateAverage(stroke.PointsL);
            return unknown.Recognize(_averageTemplates);
        }

        /// <summary>
        /// Gets the result for the best matching template
        /// </summary>
        /// <param name="stroke">Stroke to be recognized</param>
        /// <returns>Class name of the best match</returns>
        public string Recognize(Substroke stroke)
        {
            DollarTemplate unknown = new DollarTemplate(stroke.PointsL);
            return unknown.Recognize(_templates);
        }

        #endregion

        #region Serialization, Saving, and Loading

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public DollarRecognizer(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            _averageTemplates = (List<DollarTemplateAverage>)info.GetValue("averageTemplates", typeof(List<DollarTemplateAverage>));
            _templates = (List<DollarTemplate>)info.GetValue("templates", typeof(List<DollarTemplate>));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("averageTemplates", _averageTemplates);
            info.AddValue("templates", _templates);
        }

        /// <summary>
        /// Serializes the object and saves it to the specified filename
        /// </summary>
        /// <param name="filename">Filename to save the object as</param>
        public void Save(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Create);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }

        /// <summary>
        /// Loads a previously saved DollarRecognizer from the given filename, 
        /// using the deserialization constructor
        /// </summary>
        /// <param name="filename">Filename which is the saved DollarRecognizer</param>
        /// <returns>Re-instantiated DollarRecognzier</returns>
        public static DollarRecognizer Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            DollarRecognizer dollar = (DollarRecognizer)bformatter.Deserialize(stream);
            stream.Close();

            return dollar;
        }

        #endregion
    }
}
