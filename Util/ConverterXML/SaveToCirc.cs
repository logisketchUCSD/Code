using Domain;
using System;
using System.Collections;
using System.Xml;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Text;
using CircuitSimLib;

namespace ConverterXML
{
    /// <summary>
    /// Provides functionality for saving to LogiSim format. Reverse-engineered 
    /// from examining .circ files that Logisim 2.7.1 produced 
    /// (basically a special type of XML file). July 2011.
    /// </summary>
    public class SaveToCirc
    {
        #region INTERNALS

        private Circuit circuit;

        private Sketch.Project project;

        private StringWriter stringWriter;

        private XmlTextWriter writer;

        #endregion

        #region LogiSim exporting

        /// <summary>
        /// Writes the complete LogiSim representation to the given filepath.
        /// </summary>
        public static void LogiSimExport(Sketch.Project project, Circuit circuit, 
            string filepath)
        {
            XmlTextWriter textWriter = new XmlTextWriter(filepath, System.Text.Encoding.UTF8);

            textWriter.Formatting = System.Xml.Formatting.Indented;
            textWriter.WriteStartElement("project");

            // Specify LogiSim configuration and main project.
            SpecifyLibraries(textWriter);
            textWriter.WriteStartElement("main");
            textWriter.WriteAttributeString("name", project.UniqueIdentifier);
            textWriter.WriteEndElement();
            SpecifyTools(textWriter);

            // Write project information.
            textWriter.WriteRaw(writeSubProjects(project));
            if (circuit != null) textWriter.WriteRaw(SubcircuitInfo(project, circuit));
            textWriter.WriteEndElement();
            textWriter.Close();
        }

        /// <summary>
        /// Sets configuration details for LogiSim menus.
        /// </summary>
        private static void SpecifyLibraries(XmlTextWriter textWriter)
        {
            textWriter.WriteStartElement("lib");
            textWriter.WriteAttributeString("desc", "#Wiring");
            textWriter.WriteAttributeString("name", "0");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("lib");
            textWriter.WriteAttributeString("desc", "#Gates");
            textWriter.WriteAttributeString("name", "1");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("lib");
            textWriter.WriteAttributeString("desc", "#Plexers");
            textWriter.WriteAttributeString("name", "2");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("lib");
            textWriter.WriteAttributeString("desc", "#Arithmetic");
            textWriter.WriteAttributeString("name", "3");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("lib");
            textWriter.WriteAttributeString("desc", "#Memory");
            textWriter.WriteAttributeString("name", "4");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("lib");
            textWriter.WriteAttributeString("desc", "#I/O");
            textWriter.WriteAttributeString("name", "5");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("lib");
            textWriter.WriteAttributeString("desc", "#Base");
            textWriter.WriteAttributeString("name", "6");
            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("name", "Text Tool");

            textWriter.WriteStartElement("a");
            textWriter.WriteAttributeString("name", "text");
            textWriter.WriteAttributeString("val", "");
            textWriter.WriteEndElement();
            textWriter.WriteStartElement("a");
            textWriter.WriteAttributeString("name", "font");
            textWriter.WriteAttributeString("val", "SansSerif plain 12");
            textWriter.WriteEndElement();
            textWriter.WriteStartElement("a");
            textWriter.WriteAttributeString("name", "halign");
            textWriter.WriteAttributeString("val", "center");
            textWriter.WriteEndElement();
            textWriter.WriteStartElement("a");
            textWriter.WriteAttributeString("name", "valign");
            textWriter.WriteAttributeString("val", "base");
            textWriter.WriteEndElement();

            textWriter.WriteEndElement();
            textWriter.WriteEndElement();
        }

        /// <summary>
        /// Sets configuration details for LogiSim tools.
        /// </summary>
        private static void SpecifyTools(XmlTextWriter textWriter)
        {
            // Tool mappings
            textWriter.WriteStartElement("mappings");
            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "6");
            textWriter.WriteAttributeString("map", "Button2");
            textWriter.WriteAttributeString("name", "Menu Tool");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "6");
            textWriter.WriteAttributeString("map", "Ctrl Button1");
            textWriter.WriteAttributeString("name", "Menu Tool");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "6");
            textWriter.WriteAttributeString("map", "Button3");
            textWriter.WriteAttributeString("name", "Menu Tool");
            textWriter.WriteEndElement();
            textWriter.WriteEndElement();

            // Toolbars
            textWriter.WriteStartElement("toolbar");

            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "6");
            textWriter.WriteAttributeString("name", "Poke Tool");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "6");
            textWriter.WriteAttributeString("name", "Edit Tool");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "6");
            textWriter.WriteAttributeString("name", "Text Tool");
            textWriter.WriteEndElement();

            // Input pin
            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "0");
            textWriter.WriteAttributeString("name", "Pin");
            textWriter.WriteStartElement("a");
            textWriter.WriteAttributeString("name", "tristate");
            textWriter.WriteAttributeString("val", "false");
            textWriter.WriteEndElement();
            textWriter.WriteEndElement();

            // Output pin
            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "0");
            textWriter.WriteAttributeString("name", "Pin");
            textWriter.WriteStartElement("a");
            textWriter.WriteAttributeString("name", "output");
            textWriter.WriteAttributeString("val", "true");
            textWriter.WriteEndElement();
            textWriter.WriteEndElement();

            // Tools (gates) menu
            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "1");
            textWriter.WriteAttributeString("name", "NOT Gate");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "1");
            textWriter.WriteAttributeString("name", "AND Gate");
            textWriter.WriteEndElement();

            textWriter.WriteStartElement("tool");
            textWriter.WriteAttributeString("lib", "1");
            textWriter.WriteAttributeString("name", "OR Gate");
            textWriter.WriteEndElement();

            textWriter.WriteEndElement();
        }

        private static string writeSubProjects(Sketch.Project project)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            List<Sketch.Project> allSubprojects = project.AllSubprojects;
            foreach (Sketch.Project sub in allSubprojects)
                builder.Append(sub.saveToCircDoc);
            return builder.ToString();
        }

        #endregion

        #region Saving Subcircuits Info
        /// <summary>
        /// Returns a LogiSim string representing the project's top-level circuit.
        /// </summary>
        public static string SubcircuitInfo(Sketch.Project project, Circuit circuit)
        {
            SaveToCirc saver = new SaveToCirc(project, circuit);
            return saver.stringWriter.ToString();
        }

        /// <summary>
        /// Import the circuit and project into our instance of SaveToCirc.
        /// </summary>
        private SaveToCirc(Sketch.Project project, Circuit circuit)
        {
            this.circuit = circuit;
            this.project = project; // Need Project to get CircuitElement -> Subproject correspondence.

            this.stringWriter = new StringWriter();
            this.writer = new XmlTextWriter(stringWriter);
            writer.Formatting = System.Xml.Formatting.Indented;

            writeCircuit();
        }

        #region Writing circuit elements

        /// <summary>
        /// Writes the Circuit to the file.
        /// </summary>
        public void writeCircuit()
        {
            writer.WriteStartElement("circuit");
            writer.WriteAttributeString("name", project.UniqueIdentifier);

            writer.WriteStartElement("a");
            writer.WriteAttributeString("name", project.UniqueIdentifier);
            writer.WriteEndElement();

            foreach (var shapeAndElement in circuit.ShapesToElements)
            {
                CircuitElement element = shapeAndElement.Value;
                if (element is INPUT)
                    addInput((INPUT)element);
                else if (element is OUTPUT)
                    addOutput((OUTPUT)element);
                else if (element is Gate)
                    addGate((Gate)element);
                else
                    throw new Exception("invalid type");
                writeWiresIntoElement(element);
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes all the incoming wires for the given element.
        /// </summary>
        private void writeWiresIntoElement(CircuitElement element)
        {
            foreach (var inputIndexAndOutputPort in element.InputPorts)
            {
                var outputPort = inputIndexAndOutputPort.Value;
                Point startPoint = getOutpoint(outputPort.Item1, outputPort.Item2);

                int inputIndex = inputIndexAndOutputPort.Key;
                Point endPoint = getInpoint(element, inputIndex);
                addWire(startPoint, endPoint);
            }
        }

        /// <summary>
        /// Create wires according to LogiSim specs (no diagonals)
        /// </summary>
        private void addWire(Point start, Point end)
        {
            // Convert to LogiSim points first
            start = roundPointToLogisimPoint(start);
            end = roundPointToLogisimPoint(end);

            if (start == end)
                return;

            // already a straight line
            else if (start.X == end.X || start.Y == end.Y)
            {
                writer.WriteStartElement("wire");
                writer.WriteAttributeString("from", getLocationAsString(start));
                writer.WriteAttributeString("to", getLocationAsString(end));
                writer.WriteEndElement();
            }

            // split the wire into three linear segments
            else
            {
                Point mid1 = new Point((start.X + end.X) / 2, start.Y);
                Point mid2 = new Point((start.X + end.X) / 2, end.Y);
                addWire(start, mid1);
                addWire(mid1, mid2);
                addWire(mid2, end); 
            }
        }

        /// <summary>
        /// Writes the gate in Logisim format
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="gate"></param>
        private void addGate(Gate gate)
        {
            writer.WriteStartElement("comp");
            if (gate.GateType != LogicDomain.SUBCIRCUIT) 
                writer.WriteAttributeString("lib", "1");

            writer.WriteAttributeString("loc", getLocationAsString(getLocation(gate)));
            writer.WriteAttributeString("name", getElementType(gate)); // This specifies the type of the gate.

            writeOrientation(gate.Angle);
            writeElementName(gate.Name);

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the output in Logisim format
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="output"></param>
        private void addOutput(OUTPUT output)
        {
            writer.WriteStartElement("comp");

            // Declare that it's a Pin
            writer.WriteAttributeString("lib", "0");
            writeElementLocation(output);
            writer.WriteAttributeString("name", "Pin");

            writeElementName(output.Name);
            writeOrientation(180); // Orientation defaults to West for output.

            // Declare that it's an output
            writer.WriteStartElement("a");
            writer.WriteAttributeString("name", "output");
            writer.WriteAttributeString("val", "true");
            writer.WriteEndElement();

            writer.WriteEndElement();
        }


        /// <summary>
        /// Writes an input in Logisim format
        /// </summary>
        public void addInput(INPUT input)
        {
            writer.WriteStartElement("comp");

            writer.WriteAttributeString("lib", "0");
            writer.WriteAttributeString("loc", getLocationAsString(getLocation(input)));
            writer.WriteAttributeString("name", "Pin");

            writeElementName(input.Name);

            writer.WriteStartElement("a");
            writer.WriteAttributeString("name", "tristate");
            writer.WriteAttributeString("val", "false");
            writer.WriteEndElement();

            writeOrientation(0); // Orientation defaults to East for input.

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the element's orientation.
        /// </summary>
        private void writeOrientation(int angle)
        {
            writer.WriteStartElement("a");
            writer.WriteAttributeString("name", "facing");

            string orientation = null;
            switch (angle)
            {
                case 0:
                    orientation = "east";
                    break;
                case 90:
                    orientation = "south";
                    break;
                case 180:
                    orientation = "west";
                    break;
                case 270:
                    orientation = "north";
                    break;
                default:
                    throw new Exception("unknown orientation!");
            }

            writer.WriteAttributeString("val", orientation);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the element's name.
        /// </summary>
        private void writeElementName(string name)
        {
            writer.WriteStartElement("a");
            writer.WriteAttributeString("name", "label");
            writer.WriteAttributeString("val", name);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the element's location.
        /// </summary>
        /// <param name="element"></param>
        private void writeElementLocation(CircuitElement element)
        {
            writer.WriteAttributeString("loc", getLocationAsString(getLocation(element)));
        }


        /// <summary>
        /// Get the location of the input port for the element and given input index.
        /// </summary>
        private Point getInpoint(CircuitElement element, int inputNo)
        {
            if (element is INPUT)
                throw new Exception("Input shouldn't be getting an input!");

            else if (element is OUTPUT)
            {
                if (inputNo != 0) throw new Exception("Output should have only one input!");
                return getLocation(element);
            }

            else if (element is NOT || element is NOTBUBBLE)
            {
                if (inputNo != 0) throw new Exception("Not should only have one input!");
                return getLocation(element) + Width(element) * outToIn(element.Angle);
            }

            else if (inputNo == 0)
            {
                // Calculate the offset between the location of the element and the first input.
                // The offset is based on the difference between the number of inputs and the number of outputs.
                // (the height of the gate depends on max(numInputs, numOutputs), and the inputs and outputs are
                // centred vertically.

                // In LogiSim, every gate (except NOT and subcircuits) has five inputs and one output.
                int numInputs = 5;
                int numOutputs = 1;
                if (element is SubCircuit)
                {
                    numInputs = element.InputPorts.Count;
                    numOutputs = element.Outputs.Count;
                }
                int diff = (numInputs - numOutputs) / 2;

                return getLocation(element) + Width(element) * outToIn(element.Angle) - diff * 10 * inToIn(element.Angle);
            }

            else // inputNo > 0
            {
                return getInpoint(element, inputNo - 1) + 10 * inToIn(element.Angle);
            }
        }

        /// <summary>
        /// Get the location of the output port for the given element and given output index.
        /// </summary>
        private Point getOutpoint(CircuitElement element, int outputNo)
        {
            if (outputNo == 0)
                return getLocation(element); // In Logisim, the first output's location is the gate's location.
            else
                return getOutpoint(element, outputNo - 1) + 10 * inToIn(element.Angle);
        }

        /// <summary>
        /// We place the gate at the location of its first out point (basically the point
        /// due East of its center).
        /// </summary>
        private Point getLocation(CircuitElement element)
        {
            // Arbitrarily choose the element's location. We could even use the element's bounds' center...
            // Inputs and outputs locate themselves relative to this point.
            return roundPointToLogisimPoint(element.OutPoint(0)); 
        }

        /// <summary>
        /// Convert a Point into a XML-acceptable coordinate pair (example: (x, y)).
        /// </summary>
        private string getLocationAsString(Point point)
        {
            Point logisimPoint = roundPointToLogisimPoint(point);
            Tuple<int, int> intCoords = Tuple.Create((int)logisimPoint.X, (int)logisimPoint.Y);
            return intCoords.ToString();
        }

        /// <summary>
        /// Returns a new Point on a 10x10 grid closest to the given point.
        /// </summary>
        private Point roundPointToLogisimPoint(Point point)
        {
            // LogiSim wants the coords to be multiples of 10
            int x = (int)(point.X / 10) * 10;
            int y = (int)(point.Y / 10) * 10;
            return new Point(x, y);
        }

        /// <summary>
        /// Return the width of the CircuitElement. Necessary for calculating location of inputs
        /// relative to the gate.
        /// </summary>
        private int Width(CircuitElement element)
        {
            if (element is INPUT || element is OUTPUT)
                return 20;
            else if (element is SubCircuit)
                return 30;
            else if (element is NOT || element is NOTBUBBLE)
                return 30;
            else if (element is AND || element is OR)
                return 50;
            else if (element is NAND || element is NOR || element is XOR)
                return 60;
            else if (element is XNOR)
                return 70;
            else
                throw new Exception("Unknown element width!");
        }

        /// <summary>
        /// Get the LogiSim name for the gate from the ShapeType.
        /// NOTE: Subcircuits' name need to be the name of the original circuit (as saved in LogiSim).
        /// </summary>
        private string getElementType(Gate gate)
        {
            ShapeType type = gate.GateType;

            // If it's a subcircuit, use the subproject's unique identifier.
            if (type == LogicDomain.SUBCIRCUIT)
            {
                int subCircuitNumber = ((SubCircuit)gate).shape.SubCircuitNumber;
                return project.subProjectLookup[subCircuitNumber].UniqueIdentifier;
            }

            else // Write it like Logisim wants it.
            {
                if (type == LogicDomain.NOT || type == LogicDomain.NOTBUBBLE)
                    return "NOT Gate";
                else if (type == LogicDomain.AND)
                    return "AND Gate";
                else if (type == LogicDomain.OR)
                    return "OR Gate";
                else if (type == LogicDomain.NOR)
                    return "NOR Gate";
                else if (type == LogicDomain.XOR)
                    return "XOR Gate";
                else if (type == LogicDomain.XNOR)
                    return "XNOR Gate";
                else if (type == LogicDomain.NAND)
                    return "NAND Gate";
                else
                    throw new Exception("Unknown gate type!");
            }
        }

        /// <summary>
        /// A unit vector representing the direction between the inputs and the outputs.
        /// </summary>
        private System.Windows.Vector outToIn(int angle)
        {
            if (angle % 90 != 0) 
                throw new Exception("angle should be a right angle!");

            if (angle == 0)
                return new Vector(-1, 0);
            else if (angle == 90)
                return new Vector(0, -1);
            else
                return -outToIn(angle - 180);
        }

        /// <summary>
        /// A unit vector representing the direction between successive inputs (or outputs).
        /// </summary>
        private System.Windows.Vector inToIn(int angle)
        {
            if (angle == 0)
                return new Vector(0, 1);
            else if (angle == 90)
                return new Vector(-1, 0);
            else
                return -inToIn(angle - 180);
        }

        #endregion


        #endregion

    }
}