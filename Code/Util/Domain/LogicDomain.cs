using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Domain
{
    public class LogicDomain : Domain
    {

        #region Internals

            #region Classifications
        public static readonly string GATE_CLASS = "Gate";
        public static readonly string TEXT_CLASS = "Text";
        public static readonly string WIRE_CLASS = "Wire";
            #endregion

            #region Types
        public static ShapeType AND = new ShapeType("AND", GATE_CLASS, Colors.Brown);
        public static ShapeType OR = new ShapeType("OR", GATE_CLASS, Colors.Green);
        public static ShapeType NOT = new ShapeType("NOT", GATE_CLASS, Colors.Purple);
        public static ShapeType NOR = new ShapeType("NOR", GATE_CLASS, Colors.HotPink);
        public static ShapeType XOR = new ShapeType("XOR", GATE_CLASS, Colors.DarkCyan);
        public static ShapeType XNOR = new ShapeType("XNOR", GATE_CLASS, Colors.MediumOrchid);
        public static ShapeType NAND = new ShapeType("NAND", GATE_CLASS, Colors.DarkOrange);
        public static ShapeType NOTBUBBLE = new ShapeType("NotBubble", GATE_CLASS, Colors.Gold);

        public static ShapeType SUBCIRCUIT = new ShapeType("SubCircuit", GATE_CLASS, Colors.BlanchedAlmond);

        public static ShapeType TEXT = new ShapeType("Text", TEXT_CLASS, Colors.Salmon);
        public static ShapeType WIRE = new ShapeType("Wire", WIRE_CLASS, Colors.Blue);
            #endregion

            #region Class Lists
        public static readonly List<ShapeType> Gates = new List<ShapeType> { AND, NAND, OR, NOR, XOR, XNOR, NOT, NOTBUBBLE, SUBCIRCUIT };
        public static readonly List<ShapeType> Texts = new List<ShapeType> { TEXT };
        public static readonly List<ShapeType> Wires = new List<ShapeType> { WIRE };
        public static readonly List<ShapeType> Types = new List<ShapeType>(Gates.Union(Texts).Union(Wires));
            #endregion

        #endregion

        #region Public Methods

        public static ShapeType getType(string type)
        {
            string typeLower = type.ToLower();
            switch (typeLower)
            {
                case "and": return AND;
                case "or": return OR;
                case "not": return NOT;
                case "nor": return NOR;
                case "xor": return XOR;
                case "xnor": return XNOR;
                case "nand": return NAND;
                case "notbubble": return NOTBUBBLE;

                case "subcircuit": return SUBCIRCUIT;

                case "text": return TEXT;
                case "label": return TEXT; // these two are to support old code where
                case "io": return TEXT;    // naming conventions were super inconsistant

                case "wire": return WIRE;

                default: return new ShapeType();
            }
        }

        #endregion

        #region IsClass getters

        public static bool IsGate(ShapeType type)
        {
            return (type.Classification == GATE_CLASS);
        }

        public static bool IsWire(ShapeType type)
        {
            return (type.Classification == WIRE_CLASS);
        }

        public static bool IsText(ShapeType type)
        {
            return (type.Classification == TEXT_CLASS);
        }

        #endregion

    }
}
