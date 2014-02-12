using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Domain
{
    /// <summary>
    /// ShapeType is a container class for types to help eliminate magic strings.
    /// </summary>
    [Serializable]
    public class ShapeType : ISerializable
    {
        #region Constants

        const string UNKNOWN = "Unknown";
        static Color DEFAULT_COLOR = Colors.Black;

        #endregion

        #region Internals

        string _name;
        string _class;
        Color _color;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The string name of the type.</param>
        /// <param name="myClass">The string class of the type.</param>
        /// <param name="myColor">The System.Drawing.Color of the type.</param>
        public ShapeType(string name, string myClass, Color myColor)
        {   
            _name = name;
            _class = myClass;
            _color = myColor;
        }

        /// <summary>
        /// No argument constructor creates an "Unknown" ShapeType.
        /// </summary>
        public ShapeType()
        {
            _name = UNKNOWN;
            _class = UNKNOWN;
            _color = DEFAULT_COLOR;
        }

        #endregion

        #region Public Methods

        public static bool operator ==(ShapeType lhs, ShapeType rhs)
        {
            if ((object)lhs == null && (object)rhs == null)
                return true;
            if ((object)lhs == null ^ (object)rhs == null)
                return false;
            return (lhs.Name == rhs.Name);
        }

        public static bool operator !=(ShapeType lhs, ShapeType rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ShapeType)) 
                return false;
            ShapeType s = (ShapeType)obj;
            return (s == this);
        }

        #endregion

        #region Getters and Setters

        /// <summary>
        /// Getter for the name of the type.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Getter for the class of the type.
        /// </summary>
        public string Classification
        {
            get { return _class; }
        }

        public Color Color
        {
            get { return _color; }
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion

        #region Serialization

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("Name", _name);
            info.AddValue("Class", _class);
            info.AddValue("ColorA", _color.ScA);
            info.AddValue("ColorB", _color.ScB);
            info.AddValue("ColorG", _color.ScG);
            info.AddValue("ColorR", _color.ScR);
        }

        public ShapeType(SerializationInfo info, StreamingContext ctxt)
        {
            _name = (string)info.GetValue("Name", typeof(string));
            _class = (string)info.GetValue("Class", typeof(string));
            float colorA = (float)info.GetValue("ColorA", typeof(float));
            float colorB = (float)info.GetValue("ColorB", typeof(float));
            float colorG = (float)info.GetValue("ColorG", typeof(float));
            float colorR = (float)info.GetValue("ColorR", typeof(float));
            _color = System.Windows.Media.Color.FromScRgb(colorA, colorR, colorG, colorB);
        }

        #endregion
    }
}
