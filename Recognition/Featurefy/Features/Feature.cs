using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Featurefy
{
    /// <summary>
    /// Feature Class
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{Scope}, {Name}: {Value}")]
    public class Feature// : ISerializable
    {
        /// <summary>
        /// The possible scope types of this feature
        /// </summary>
        public enum Scope { 
            /// <summary>
            /// This feature is single
            /// </summary>
            Single, 
            
            /// <summary>
            /// This feature is static and paired with one other
            /// </summary>
            Pair_Static, 
            
            /// <summary>
            /// This feature is dynamic and paired with one other
            /// </summary>
            Pair_Dynamic, 
            
            /// <summary>
            /// This feature is static and in a group
            /// </summary>
            Multiple_Static, 
            
            /// <summary>
            /// This feature is dynamic and in a group
            /// </summary>
            Multiple_Dynamic };

        #region Constants

        double TINY_VALUE = 0.0001;

        #endregion

        #region Member Variables

        /// <summary>
        /// The name of the feature
        /// </summary>
        protected string m_Name;

        /// <summary>
        /// The feature's value
        /// </summary>
        protected double m_Value;

        /// <summary>
        /// The feature's normalized value
        /// </summary>
        protected double m_NormalizedValue;

        /// <summary>
        /// The normalizer facroe
        /// </summary>
        protected double m_Normalizer;

        /// <summary>
        /// The scope of the feature (default: Multiple_Static)
        /// </summary>
        protected Scope m_Scope = Scope.Multiple_Static;
        
        /// <summary>
        /// The unique ID of the feature
        /// </summary>
        private Guid m_Id;

        #endregion

        #region Constructors

        /// <summary>
        /// Empy constructor
        /// </summary>
        public Feature()
        {
            m_Name = "Base";
            m_Value = 0.0;
            m_Id = Guid.NewGuid();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="scope"></param>
        public Feature(string name, Scope scope)
        {
            m_Name = name;
            m_Scope = scope;
            m_Value = 0.0;
            m_Id = Guid.NewGuid();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="scope"></param>
        public Feature(string name, double value, Scope scope)
        {
            m_Name = name;
            m_Value = value;
            m_Scope = scope;
            m_Id = Guid.NewGuid();
        }

        #endregion
        
        #region Getters

        /// <summary>
        /// Normalize the feature's value
        /// </summary>
        public virtual void Normalize()
        {
            // To avoid division by 0, add a tiny value to the denominator
            m_NormalizedValue = m_Value / (m_Normalizer + TINY_VALUE);
        }

        /// <summary>
        /// Set the normalizer as the given value
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetNormalizer(double value)
        {
            m_Normalizer = value;
        }

        /// <summary>
        /// Get the name of this feature
        /// </summary>
        public virtual string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// Get the value of this feature
        /// </summary>
        public virtual double Value
        {
            get { return m_Value; }
        }

        /// <summary>
        /// Get the normalied value of this feature
        /// </summary>
        public virtual double NormalizedValue
        {
            get 
            {
                Normalize();
                return m_NormalizedValue; 
            }
        }

        /// <summary>
        /// Get the ID of this feature
        /// </summary>
        public Guid Id
        {
            get { return m_Id; }
        }

        /// <summary>
        /// Get the scope of this feature
        /// </summary>
        public Scope scope
        {
            get { return m_Scope; }
        }

        /// <summary>
        /// Get a bool indicating if the feature's scope is single.
        /// </summary>
        public bool IsSingle
        {
            get
            {
                if (m_Scope == Scope.Single)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Get a bool indicateing if this feature's scope is Pair_Static
        /// </summary>
        public bool IsPair
        {
            get
            {
                if (m_Scope == Scope.Pair_Static)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Get a bool indicateing if this feature's scope is Multiple_Static
        /// </summary>
        public bool IsMultiple
        {
            get
            {
                if (m_Scope == Scope.Multiple_Static)
                    return true;
                else
                    return false;
            }
        }

        #endregion

        #region Serialization (Unused)
        /*
        public Feature(SerializationInfo info, StreamingContext context)
        {
            m_Id = (Guid)info.GetValue("Id", typeof(Guid));
            m_Name = (string)info.GetValue("Name", typeof(string));
            m_Scope = (Scope)info.GetValue("Scope", typeof(Scope));
            m_Value = (double)info.GetValue("Value", typeof(double));
            m_NormalizedValue = (double)info.GetValue("NormalizedValue", typeof(double));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", m_Id);
            info.AddValue("Name", m_Name);
            info.AddValue("Scope", m_Scope);
            info.AddValue("Value", m_Value);
            info.AddValue("NormalizedValue", m_NormalizedValue);
        }*/

        #endregion
    }
}
