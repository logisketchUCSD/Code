using Domain;
using Sketch;
using RecognitionInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Refiner
{
    /// <summary>
    /// Group notbubbles into their respective shapes (i.e., an AND gate next to a NOTBUBBLE should be a NAND gate).
    /// NOTE: currently broken. Input not bubbles are treated no differently than output not bubbles.
    /// </summary>
    public class GroupNotBubble : IRecognitionStep
    {
        private Sketch.Sketch _sketch;


        public string ProgressString
        {
            get { return "Refining (not bubbles)"; }
        }

        public void process(Featurefy.FeatureSketch featureSketch)
        {
            _sketch = featureSketch.Sketch;
            List<Shape> notbubbles = Data.Utils.filter(_sketch.Shapes, delegate (Shape s) { return s.Type == LogicDomain.NOTBUBBLE; });
            foreach (Shape bubble in notbubbles)
                foreach (Shape connected in bubble.ConnectedShapes)
                    if (tryToMerge(bubble, connected))
                        break;
        }

        /// <summary>
        /// Attempts to merge the designated not bubble with the designated shape.
        /// 
        /// Only works if connected is a gate that can have a not bubble on the front of it.
        /// </summary>
        /// <param name="bubble">The not bubble in question.</param>
        /// <param name="connected">The connected shape to try to merge with.</param>
        /// <returns>Whether or not the shapes were able to merge.</returns>
        private bool tryToMerge(Shape bubble, Shape connected)
        {
            if (bubble.Type != LogicDomain.NOTBUBBLE)
            {
                Console.WriteLine("You thought " + bubble.Name + " was a NotBubble, but it's not!");
                return false;
            }

            if (connected.Type.Classification != LogicDomain.GATE_CLASS)
                return false;

            ShapeType newType = new ShapeType();

            if (connected.Type == LogicDomain.AND)
                newType = LogicDomain.NAND;
            else if (connected.Type == LogicDomain.OR)
                newType = LogicDomain.NOR;
            else if (connected.Type == LogicDomain.XOR)
                newType = LogicDomain.XNOR;

            if (newType != new ShapeType())
            {
                _sketch.mergeShapes(connected, bubble);

                #if (DEBUG)
                    Console.WriteLine("Adding a NotBubble changed a " + connected.Type + " to a " + newType);
                #endif

                connected.Type = newType;
                return true;
            }

            return false;
        }
    }
}
