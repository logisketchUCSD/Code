using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Featurefy;
using Domain;
using Sketch;

namespace RecognitionInterfaces
{
    public class Connector : IRecognitionStep
    {

        public string ProgressString
        {
            get { return "Connecting"; }
        }

        /// <summary>
        /// The domain this connector will use
        /// </summary>
        private ContextDomain.ContextDomain _domain;

        /// <summary>
        /// Create a new connector in the specified domain
        /// </summary>
        /// <param name="domain">the domain to use</param>
        public Connector(ContextDomain.ContextDomain domain)
        {
            _domain = domain;
        }

        /// <summary>
        /// Connects all shapes in a sketch as well as possible.
        /// 
        /// Precondition: the shapes of the sketch have identified types.
        /// 
        /// Postconditions: 
        ///    - the shapes are connected and oriented according to the context domain.
        ///    - no wires are connected to each other; connected wires are a single shape
        /// </summary>
        /// <param name="featureSketch">the sketch to connect</param>
        public virtual void process(Featurefy.FeatureSketch featureSketch)
        {
            recomputeConnectedShapes(featureSketch.Sketch.Shapes, featureSketch.Sketch);
        }

        /// <summary>
        /// Recalculate the connectedShapes of the given shape,
        /// and correctly update all related shapes.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="sketch"></param>
        public void recomputeConnectedShapes(Sketch.Shape shape, Sketch.Sketch sketch)
        {
            recomputeConnectedShapes(new Sketch.Shape[] { shape }, sketch);
        }

        /// <summary>
        /// Recalculate the connectedShapes of every shape in a given list,
        /// and correctly update all related shapes.
        /// </summary>
        /// <param name="shapeList">the list of shapes to reconnect</param>
        /// <param name="featureSketch">the sketch the shapes belong to</param>
        public void recomputeConnectedShapes(IEnumerable<Sketch.Shape> shapeList, Sketch.Sketch sketch)
        {
            // Keep a unique list of shapes
            HashSet<Sketch.Shape> allRelevantShapes = new HashSet<Sketch.Shape>(shapeList);

            // Add connected shapes to the list that needs to change
            foreach (Sketch.Shape shape in shapeList)
                foreach (Sketch.Shape connected in shape.ConnectedShapes)
                    allRelevantShapes.Add(connected);

            // clear ALL connections first
            foreach (Sketch.Shape shape in allRelevantShapes)
                shape.ClearConnections();

            sketch.CheckConsistency();

            // connect every shape
            foreach (Sketch.Shape shape in allRelevantShapes)
                connect(shape, sketch);


            // Make sure connected wires are the same shape
            Tuple<Sketch.Shape, Sketch.Shape> pair = null;
            while ((pair = wiresToMerge(allRelevantShapes)) != null)
            {
                sketch.mergeShapes(pair.Item1, pair.Item2);
                allRelevantShapes.Remove(pair.Item2);
                sketch.connectShapes(pair.Item1, pair.Item1); ;
            }

            sketch.CheckConsistency();

#if DEBUG
            foreach (Shape wire in sketch.Shapes)
            {
                if (!LogicDomain.IsWire(wire.Type))
                    continue;
                foreach (Shape shape in wire.ConnectedShapes)
                {
                    if (LogicDomain.IsWire(shape.Type) && shape != wire)
                        throw new Exception("Found two connected wires (" + wire + " and " + shape + ") that were not merged!");
                    bool found = false;
                    foreach (EndPoint endpoint in wire.Endpoints)
                    {
                        if (endpoint.ConnectedShape == shape)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        throw new Exception("The wire " + wire + " is connected to " + shape + " as a connected shape, but not by an endpoint!");
                }
            }
#endif

        }

        /// <summary>
        /// If there are two wires connected to each other in the sketch, this function
        /// returns them. Otherwise it returns null.
        /// </summary>
        /// <param name="sketch">the sketch to check</param>
        /// <returns></returns>
        private Tuple<Sketch.Shape, Sketch.Shape> wiresToMerge(IEnumerable<Sketch.Shape> shapes)
        {
            foreach (Sketch.Shape shape in shapes)
            {
                if (shape.Type != LogicDomain.WIRE)
                    continue;

                foreach (Sketch.Shape connectedShape in shape.ConnectedShapes)
                {
                    if (connectedShape.Type == LogicDomain.WIRE && shape != connectedShape)
                        return new Tuple<Sketch.Shape, Sketch.Shape>(shape, connectedShape);
                }
            }
            return null;
        }

        /// <summary>
        /// Find and make all necessary connections from one shape in a sketch.
        /// 
        /// Precondition: 
        ///    - no shapes in the sketch have connections to shapes that are missing
        /// </summary>
        /// <param name="shape">the shape whose conenctions will be updated</param>
        /// <param name="featureSketch">the sketch that the shape belongs to</param>
        public virtual void connect(Sketch.Shape shape, Sketch.Sketch sketch)
        {
            _domain.ConnectShape(shape, sketch);
            sketch.CheckConsistency();
        }

        /// <summary>
        /// Get the context domain for this connector. Does not support set.
        /// </summary>
        public ContextDomain.ContextDomain Domain
        {
            get { return _domain; }
        }

    }
}
