using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Utilities.Neuro.Learning;

namespace Utilities.Neuro
{
    [Serializable()]

    /// <summary>
    /// Information about the network, such as:
    /// Domain
    /// NetworkID
    /// Type (All, UserHoldout, SketchHoldout)
    /// If Type == UserHoldout --> UserName and UserID
    /// If Type == SketchHoldout --> UserName, UserID, SketchName, SketchID
    /// DateTime of creation
    /// Training Errors for epochs that are multiples of 100
    /// Teacher = Learning Method used
    /// </summary>
    public class NeuralNetworkInfo : ISerializable
    {
        #region Member Variables

        /// <summary>
        /// Domain for the network
        /// </summary>
        private string m_DomainName;

        /// <summary>
        /// Unique ID for the network
        /// </summary>
        private Guid m_NetworkId;

        /// <summary>
        /// Type for the network (All, UserHoldout, SketchHoldout)
        /// </summary>
        private string m_Type;

        /// <summary>
        /// Name for user if Type == UserHoldout or SketchHoldout
        /// </summary>
        private string m_UserName;

        /// <summary>
        /// Unique ID for the user if Type == UserHoldout or SketchHoldout
        /// </summary>
        private Guid m_UserId;

        /// <summary>
        /// Name for the sketch if Type == SketchHoldout
        /// </summary>
        private string m_SketchName;

        /// <summary>
        /// Unique ID for the sketch if Type == SketchHoldout
        /// </summary>
        private Guid m_SketchId;

        /// <summary>
        /// Date and Time that the network was created
        /// </summary>
        private DateTime m_DateTime;

        /// <summary>
        /// Training Error for the network
        /// </summary>
        private Dictionary<int, double> m_NetworkTrainingErrorValues;

        /// <summary>
        /// Name of teacher being used for learning
        /// </summary>
        private string m_Teacher;

        /// <summary>
        /// Number of epochs to train the network for
        /// </summary>
        private int m_NumTrainingEpochs = 1000;

        /// <summary>
        /// Learning Rate for the teacher
        /// </summary>
        private double m_LearningRate = 0.3;

        /// <summary>
        /// Momentum for the teacher
        /// </summary>
        private double m_Momentum = 0.3;

        /// <summary>
        /// Number of inputs for the network
        /// </summary>
        private int m_NumInputs;

        /// <summary>
        /// Number of neurons in each layer of the network
        /// </summary>
        private int[] m_Layers;

        /// <summary>
        /// Activation Function to use for neurons
        /// </summary>
        private IActivationFunction m_ActivationFunction;

        /// <summary>
        /// Teacher to use for learning
        /// </summary>
        private ISupervisedLearning m_LearningMethod;

        private List<string> m_FeatureNames;

        private List<string> m_OutputTypeNames;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// Domain = Type = "None"
        /// </summary>
        public NeuralNetworkInfo()
        {
            m_ActivationFunction = new SigmoidFunction();
            m_DomainName = "None";
            m_NetworkId = Guid.NewGuid();
            m_Type = "None";
            m_DateTime = DateTime.Now;
            m_NetworkTrainingErrorValues = new Dictionary<int, double>();
            m_Teacher = "BackPropagation";
            m_NumInputs = 1;
            m_Layers = new int[1] { 1 };
            m_FeatureNames = new List<string>();
            m_OutputTypeNames = new List<string>();
        }

        public NeuralNetworkInfo(NeuralNetworkInfo info)
        {
            m_ActivationFunction = info.m_ActivationFunction;
            m_DomainName = info.m_DomainName;
            m_FeatureNames = info.m_FeatureNames;
            m_Layers = info.m_Layers;
            m_LearningMethod = info.m_LearningMethod;
            m_LearningRate = info.m_LearningRate;
            m_Momentum = info.m_Momentum;
            m_NetworkId = Guid.NewGuid();
            m_NetworkTrainingErrorValues = new Dictionary<int, double>();
            m_NumInputs = info.m_NumInputs;
            m_NumTrainingEpochs = info.m_NumTrainingEpochs;
            m_OutputTypeNames = info.m_OutputTypeNames;
            m_SketchId = info.m_SketchId;
            m_SketchName = info.m_SketchName;
            m_Teacher = info.m_Teacher;
            m_Type = info.m_Type;
            m_UserId = info.m_UserId;
            m_UserName = info.m_UserName;
        }

        /// <summary>
        /// Constructor which allows initialization of domain and type
        /// Teacher by default is BackPropagation
        /// </summary>
        /// <param name="domain">Domain Name</param>
        /// <param name="type">Type for training (All, UserHoldout, SketchHoldout)</param>
        public NeuralNetworkInfo(string domain, string type)
        {
            m_ActivationFunction = new SigmoidFunction();
            m_DomainName = domain;
            m_NetworkId = Guid.NewGuid();
            m_Type = type;
            m_DateTime = DateTime.Now;
            m_NetworkTrainingErrorValues = new Dictionary<int, double>();
            m_Teacher = "BackPropagation";
            m_FeatureNames = new List<string>();
            m_OutputTypeNames = new List<string>();
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public NeuralNetworkInfo(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            m_DomainName = (string)info.GetValue("Domain", typeof(string));
            m_NetworkId = (Guid)info.GetValue("NetworkID", typeof(Guid));
            m_Type = (string)info.GetValue("Type", typeof(string));
            m_UserName = (string)info.GetValue("UserName", typeof(string));
            m_UserId = (Guid)info.GetValue("UserID", typeof(Guid));
            m_SketchName = (string)info.GetValue("SketchName", typeof(string));
            m_SketchId = (Guid)info.GetValue("SketchID", typeof(Guid));
            m_DateTime = (DateTime)info.GetValue("DateTime", typeof(DateTime));
            m_NetworkTrainingErrorValues = (Dictionary<int, double>)info.GetValue("TrainingError", typeof(Dictionary<int, double>));
            m_Teacher = (string)info.GetValue("Teacher", typeof(string));
            m_NumTrainingEpochs = (int)info.GetValue("NumTrainingEpochs", typeof(int));
            m_LearningRate = (double)info.GetValue("LearningRate", typeof(double));
            m_Momentum = (double)info.GetValue("Momentum", typeof(double));
            m_NumInputs = (int)info.GetValue("NumInputs", typeof(int));
            m_Layers = (int[])info.GetValue("Layers", typeof(int[]));
            m_ActivationFunction = (IActivationFunction)info.GetValue("ActivationFunction", typeof(IActivationFunction));
            m_LearningMethod = (ISupervisedLearning)info.GetValue("LearningMethod", typeof(ISupervisedLearning));
            try
            {
                m_FeatureNames = (List<string>)info.GetValue("FeatureNames", typeof(List<string>));
                m_OutputTypeNames = (List<string>)info.GetValue("OutputClassNames", typeof(List<string>));
            }
            catch (Exception e)
            {
                Console.WriteLine("You're using an old network that doesn't have some variables saved...but it's probably okay.");
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("Domain", m_DomainName);
            info.AddValue("NetworkID", m_NetworkId);
            info.AddValue("Type", m_Type);
            info.AddValue("UserName", m_UserName);
            info.AddValue("UserID", m_UserId);
            info.AddValue("SketchName", m_SketchName);
            info.AddValue("SketchID", m_SketchId);
            info.AddValue("DateTime", m_DateTime);
            info.AddValue("TrainingError", m_NetworkTrainingErrorValues);
            info.AddValue("Teacher", m_Teacher);
            info.AddValue("NumTrainingEpochs", m_NumTrainingEpochs);
            info.AddValue("LearningRate", m_LearningRate);
            info.AddValue("Momentum", m_Momentum);
            info.AddValue("NumInputs", m_NumInputs);
            info.AddValue("Layers", m_Layers);
            info.AddValue("ActivationFunction", m_ActivationFunction);
            info.AddValue("LearningMethod", m_LearningMethod);
            info.AddValue("FeatureNames", m_FeatureNames);
            info.AddValue("OutputClassNames", m_OutputTypeNames);
        }

        #endregion

        #region Getters

        /// <summary>
        /// Domain for the network
        /// </summary>
        public string DomainName
        {
            get { return m_DomainName; }
            set { m_DomainName = value; }
        }

        /// <summary>
        /// Unique ID for the network
        /// </summary>
        public Guid NetworkID
        {
            get { return m_NetworkId; }
            set { m_NetworkId = value; }
        }

        /// <summary>
        /// Type for the network (All, UserHoldout, SketchHoldout)
        /// </summary>
        public string Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        /// <summary>
        /// Name for user if Type == UserHoldout or SketchHoldout
        /// </summary>
        public string UserName
        {
            get { return m_UserName; }
            set { m_UserName = value; }
        }

        /// <summary>
        /// Unique ID for the user if Type == UserHoldout or SketchHoldout
        /// </summary>
        public Guid UserID
        {
            get { return m_UserId; }
            set { m_UserId = value; }
        }

        /// <summary>
        /// Name for the sketch if Type == SketchHoldout
        /// </summary>
        public string SketchName
        {
            get { return m_SketchName; }
            set { m_SketchName = value; }
        }

        /// <summary>
        /// Unique ID for the sketch if Type == SketchHoldout
        /// </summary>
        public Guid SketchID
        {
            get { return m_SketchId; }
            set { m_SketchId = value; }
        }

        /// <summary>
        /// Date and Time that the network was created
        /// </summary>
        public DateTime DateTime
        {
            get { return m_DateTime; }
            set { m_DateTime = value; }
        }

        /// <summary>
        /// Training Error for the network
        /// </summary>
        public Dictionary<int, double> NetworkTrainingErrorValues
        {
            get { return m_NetworkTrainingErrorValues; }
            set { m_NetworkTrainingErrorValues = value; }
        }

        /// <summary>
        /// Name of teacher being used for learning
        /// </summary>
        public string Teacher
        {
            get { return m_Teacher; }
            set { m_Teacher = value; }
        }

        /// <summary>
        /// Number of epochs to train the network for
        /// </summary>
        public int NumTrainingEpochs
        {
            get { return m_NumTrainingEpochs; }
            set { m_NumTrainingEpochs = value; }
        }

        /// <summary>
        /// Learning Rate for the teacher
        /// </summary>
        public double LearningRate
        {
            get { return m_LearningRate; }
            set { m_LearningRate = value; }
        }

        /// <summary>
        /// Momentum for the teacher
        /// </summary>
        public double Momentum
        {
            get { return m_Momentum; }
            set { m_Momentum = value; }
        }

        /// <summary>
        /// Number of inputs for the network
        /// </summary>
        public int NumInputs
        {
            get { return m_NumInputs; }
            set { m_NumInputs = value; }
        }

        /// <summary>
        /// Number of neurons in each layer of the network
        /// </summary>
        public int[] Layers
        {
            get { return m_Layers; }
            set { m_Layers = value; }
        }

        /// <summary>
        /// Activation Function to use for neurons
        /// </summary>
        public IActivationFunction ActivationFunction
        {
            get { return m_ActivationFunction; }
            set { m_ActivationFunction = value; }
        }

        /// <summary>
        /// Teacher to use for learning
        /// </summary>
        public ISupervisedLearning LearningMethod
        {
            get { return m_LearningMethod; }
            set { m_LearningMethod = value; }
        }

        public List<string> FeatureNames
        {
            get { return m_FeatureNames; }
            set { m_FeatureNames = value; }
        }

        public List<string> OutputTypeNames
        {
            get { return m_OutputTypeNames; }
            set { m_OutputTypeNames = value; }
        }

        public string GetClass(int maxOutputNodeNumber)
        {
            if (m_OutputTypeNames != null && m_OutputTypeNames.Count > maxOutputNodeNumber)
                return m_OutputTypeNames[maxOutputNodeNumber];
            else
                return "N/A";
        }

        #endregion
    }
}
