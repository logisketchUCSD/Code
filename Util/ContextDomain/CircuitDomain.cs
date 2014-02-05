/*
 * CircuitDomain.cs
 * 
 * Created by Denis Aleshin and Joshua Ehrlich 
 * June 30, 2009
 * 
 * Edited by Sketchers 2010
 * July 19, 2010
 *  
 */

using Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Sketch;
using Data;
using Utilities;

namespace ContextDomain
{

    /// <summary>
    /// Basically just a typedef to simplify the code.
    /// </summary>
    public class ConnectionContext : Dictionary<string, IntRange> { }

    /// <summary>
    /// CircuitDomain is the context domain for digital logic circuits.
    /// </summary>
    public class CircuitDomain : ContextDomain
    {

        #region Constants

        private const string FILENAME = "CircuitDomain.txt";
        private const string MAXSTRING = "-";
        private const string MINSTRING = "+";
        private const string EXACTSTRING = "E";

        #endregion

        #region Internals

        /// <summary>
        /// The list of classifications in this domain.
        /// </summary>
        private List<string> _classes;

        /// <summary>
        /// A list of all acceptable contexts for digital circuit elements. In short, it maps:
        /// 
        ///    "shape type" -> list of contexts
        ///    
        /// where a context is a dictionary describing a valid set of connections:
        /// 
        ///    "connected shape classification" -> number of connections in this context
        ///    
        /// where connection type represents "this many or more," "this many or fewer," etc.
        /// 
        /// For instance:
        ///     
        ///   [ "Wire" -> [ ["Wire" -> (2, INF), "Text" -> (0, 0), "Gate" -> (0, 0)],
        ///                 ["Wire" -> (0, 0),   "Text" -> (1, 1), "Gate" -> (1, 1)]] ]
        ///                 
        /// This means that something of type "Wire" has two valid contexts:
        ///     1. connected to 2 or more wires, or
        ///     2. connected to exactly 1 text shape and exactly 1 gate shape
        /// </summary>
        private Dictionary<ShapeType, List<ConnectionContext>> _validContexts;

        #endregion

        #region Constructor and initializers

        private static Lazy<CircuitDomain> instance = new Lazy<CircuitDomain>(delegate() { return new CircuitDomain(); });

        /// <summary>
        /// Circuit domain is a singleton class. Use this method to
        /// get the single instance of it.
        /// </summary>
        /// <returns></returns>
        public static CircuitDomain GetInstance()
        {
            return instance.Value;
        }

        private CircuitDomain()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public CircuitDomain(string directory)
        {
            _classes = new List<string>();
            _classes.Add(LogicDomain.GATE_CLASS);
            _classes.Add(LogicDomain.WIRE_CLASS);
            _classes.Add(LogicDomain.TEXT_CLASS);

            _validContexts = new Dictionary<ShapeType, List<ConnectionContext>>();
            initContexts(directory);
        }

        #endregion

        #region Context Information

        /// <summary>
        /// Our valid contexts is loaded from a file here.
        /// </summary>
        private void initContexts(string directory)
        {
            // Parse the domain file
            System.IO.StreamReader sr = new System.IO.StreamReader(directory + "\\" + FILENAME);

            string line = sr.ReadLine();
            while (line != null)
            {

                if (line == "" || line[0] == '#')
                {
                    line = sr.ReadLine();
                    continue;
                }

                line = line.Trim();
                string[] words = line.Split(' ');
                if (words.Length % 3 != 2)
                {
                    Console.WriteLine("Error in file or processing, skipping this line in " + FILENAME 
                        + ": \"" + line + "\"");
                    line = sr.ReadLine();
                    continue;
                }
                String classification = words[0];
                // Console.WriteLine("the classification is " + classification);
                ShapeType label = Domain.LogicDomain.getType(words[1]);
                if (!(_validContexts.ContainsKey(label)))
                    _validContexts.Add(label, new List<ConnectionContext>());
                _validContexts[label].Add(new ConnectionContext());
                for (int i = 2; i < words.Length - 1; i += 3)
                {
                    int connectionCount = Convert.ToInt32(words[i]);
                    string connectionType = words[i + 1];
                    string clas = words[i + 2];
                    IntRange range;
                    if (connectionType.Equals(MAXSTRING))
                        range = new IntRange(0, connectionCount);
                    else if (connectionType.Equals(MINSTRING))
                        range = new IntRange(connectionCount, int.MaxValue);
                    else  //EXACTSTRING
                        range = new IntRange(connectionCount, connectionCount);
                    // update the dictionary of valid contexts for the current label
                    _validContexts[label][_validContexts[label].Count - 1].Add(clas, range);
                }
                line = sr.ReadLine();
            }
            return;
        }

        /// <summary>
        /// Get the list of valid contexts for a given shape type.
        /// </summary>
        /// <param name="type">the type to check</param>
        /// <returns>a list of valid contexts</returns>
        public List<ConnectionContext> ValidContextsFor(ShapeType type)
        {
            return _validContexts[type];
        }

        #endregion

        #region Orienting Shapes

        /// <summary>
        /// Determine a shape's orientation based on the angles of incoming and outgoing
        /// wires. This method is highly reliable (numerous user studies have shown that
        /// users don't draw wires coming into a gate at meaningless orientations), but
        /// it is sometimes off by 180 degrees (by which I mean Pi, since we are working
        /// in radians).
        /// 
        /// NOTE: Currently, this code is not used. HOWEVER, it is extremely reliable. Often
        /// moreso than the orientation obtained from the image recognizer. The refiner should
        /// be able to use this information.
        /// </summary>
        /// <param name="shape1">The shape to check.</param>
        /// <param name="sketch">The sketch containing the shape.</param>
        public override double OrientShape(Sketch.Shape shape, Sketch.Sketch sketch)
        {
            if (shape.Classification == LogicDomain.GATE_CLASS)
            {
                // The gate's angle based on its connected wires.
                double connectionsAngle = 0;
                double numConnectedWires = 0;

                // Check the slope orientation of adjacent wires
                foreach (Sketch.Shape connectedShape in shape.ConnectedShapes)
                {
                    if (connectedShape.Type.Classification == LogicDomain.WIRE_CLASS)
                    {
                        Sketch.EndPoint endpoint = shape.ClosestEndpointFrom(connectedShape);
                        double slope = endpoint.Slope;
                        // negated since our y-axis is inverted (positive-y is down)
                        connectionsAngle -= Math.Atan(Math.Abs(slope));
                        numConnectedWires++;
                    }
                }

                // Get the average angle
                connectionsAngle = connectionsAngle / numConnectedWires;

                // Connections angle is currently in the range [-Pi, Pi], so add Pi
                return connectionsAngle + Math.PI;
            }
            return 0;
        }

        #endregion

        #region Connecting Shapes

        /// <summary>
        /// Returns a number expressing how closely the given shape matches the given context.
        /// 
        /// Precondition: context is nonempty.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="context"></param>
        /// <param name="neighborTypes">the types of the shapes connected to "shape" 
        /// (note: if the shape is connected to itself, do not include its own classification in this list)</param>
        /// <returns>a double in the range [0, 1], where 1 represents a perfect match</returns>
        public double PercentMatched(Shape shape, ConnectionContext context, List<string> neighborClasses)
        {
            int numCorrect = 0;

            foreach (KeyValuePair<string, IntRange> pair in context)
            {
                string classification = pair.Key;
                IntRange range = pair.Value;

                List<string> connections = neighborClasses.FindAll(delegate(string s) { return s == classification; });

                if (range.Contains(connections.Count))
                    numCorrect++;
            }

            return (double)numCorrect / context.Count;
        }

        /// <summary>
        /// Returns a number expressing how closely the given shape matches the given context.
        /// 
        /// Precondition: context is nonempty.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="context"></param>
        /// <returns>a double in the range [0, 1], where 1 represents a perfect match</returns>
        public double PercentMatched(Shape shape, ConnectionContext context)
        {
            List<string> neighborClasses = shape.ExternalConnectedShapes.ConvertAll(delegate(Shape s) { return s.Classification; });
            return PercentMatched(shape, context, neighborClasses);
        }

        /// <summary>
        /// Get the context that the given shape's connections most closely match.
        /// 
        /// Precondition: _validContexts[shape.Type].Count > 0
        /// </summary>
        /// <param name="shape"></param>
        /// <returns>a pair containing the percentage matched and the closest context</returns>
        public Tuple<double, ConnectionContext> ClosestContext(Shape shape)
        {
            return ClosestContext(shape, _validContexts[shape.Type]);
        }

        /// <summary>
        /// Get the context from the given list that the shape's connections most closely match.
        /// 
        /// Precondition: _validContexts[shape.Type].Count > 0
        /// </summary>
        /// <param name="shape"></param>
        /// <returns>a pair containing the percentage matched and the closest context</returns>
        public Tuple<double, ConnectionContext> ClosestContext(Shape shape, List<ConnectionContext> contexts)
        {
            return ClosestContextIfConnectedToType(shape, null, contexts);
        }
        
        /// <summary>
        /// Suppose the given shape were connected to a shape of the given classification.
        /// This gets the closest connection context under that condition.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="classification"></param>
        /// <returns></returns>
        public Tuple<double, ConnectionContext> ClosestContextIfConnectedToType(Shape shape, string classification)
        {
            return ClosestContextIfConnectedToType(shape, classification, _validContexts[shape.Type]);
        }

        /// <summary>
        /// Suppose the given shape were connected to a shape of the given classification.
        /// This gets the closest connection context under that condition.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="classification"></param>
        /// <param name="contexts">the contexts to check against</param>
        /// <returns></returns>
        public Tuple<double, ConnectionContext> ClosestContextIfConnectedToType(Shape shape, string classification, List<ConnectionContext> contexts)
        {
            List<string> neighborClasses = shape.ExternalConnectedShapes.ConvertAll(delegate(Shape s) { return s.Classification; });

            if (classification != null)
                neighborClasses.Add(classification);

            ConnectionContext bestMatch = contexts[0];
            double bestPerformance = 0;

            foreach (ConnectionContext context in contexts)
            {
                double performance = PercentMatched(shape, context, neighborClasses);
                if (performance > bestPerformance)
                {
                    bestMatch = context;
                    bestPerformance = performance;
                }
            }

            return Tuple.Create(bestPerformance, bestMatch);
        }

        /// <summary>
        /// Determine whether the shape is properly connected (given its context).
        /// </summary>
        /// <param name="shape">The shape we are testing for validity.</param>
        /// <returns>True if the shape is properly connected, or false otherwise.</returns>
        public override bool IsProperlyConnected(Shape shape)
        {
            if (shape.Type == new ShapeType())
                return false;

            Tuple<double, ConnectionContext> match = ClosestContext(shape);
            return (match.Item1 == 1);
        }

        /// <summary>
        /// Connects the given shape to the other shapes in the sketch.
        /// 
        /// Postcondition:
        ///     If the shape is a wire, the following things are true:
        ///       - For every substroke in the wire, both endpoints are connected to the closest shape
        ///       - The shapes the endpoints are connected to are also in the list of connected shapes
        ///       - The shapes that the wire is  connected to also know they are connected to the wire
        ///     If the shape is a NOTBUBBLE
        ///       - It is connected to the two closest shapes??
        /// </summary>
        /// <param name="shape">The shape to check.</param>
        /// <param name="sketch">The sketch containing the shape.</param>
        public override void ConnectShape(Sketch.Shape shape, Sketch.Sketch sketch)
        {
            if (!sketch.ShapesL.Contains(shape))
                throw new ArgumentException("The given shape " + shape + " is not in the given sketch!");

            // Connect wires to adjacent shapes
            if (shape.Type == LogicDomain.WIRE)
            {
                foreach (Sketch.Substroke substroke in shape.Substrokes)
                {
                    // Find the shape closest to the start point of the wire
                    Shape shape2 = findClosest(true, substroke, sketch);
                    if (shape2 != null)
                    {
                        EndPoint start = substroke.Endpoints[0];
                        if (!start.IsConnected)
                        {
                            sketch.connectShapes(shape, shape2);
                            start.ConnectedShape = shape2;
                        }
                    }

                    // Find the shape closest to the end point of the wire
                    Shape shape3 = findClosest(false, substroke, sketch);
                    if (shape3 != null)
                    {
                        EndPoint end = substroke.Endpoints[1];
                        if (!end.IsConnected)
                        {
                            sketch.connectShapes(shape, shape3);
                            end.ConnectedShape = shape3;
                        }
                    }
                }
            }

            // Connect NotBubbles to its two closest shapes.
            // FIXME: This could potentially cause problems. For instance,
            // on the off chance that one of the closest shapes was a wire
            // whose connections were already figured out, this could leave
            // the wire in an inconsistent state.
            else if (shape.Type == LogicDomain.NOTBUBBLE)
            {
                List<Sketch.Shape> shapes = twoClosest(shape, sketch);

                foreach (Shape closeShape in shapes)
                {
                    if (closeShape == null)
                        continue;

                    // FIXME: For now, we're only connecting this notbubble to a wire if that wire's closest endpoint is free.
                    if (closeShape.Type == LogicDomain.WIRE)
                    {
                        EndPoint endpoint = shape.ClosestEndpointFrom(closeShape);
                        if (!endpoint.IsConnected)
                            closeShape.ConnectNearestEndpointTo(shape);
                    }
                    else
                        sketch.connectShapes(closeShape, shape);

                }
            }

#if DEBUG

            foreach (Shape wire in sketch.Shapes)
            {
                if (!LogicDomain.IsWire(wire.Type))
                    continue;
                foreach (Shape connected in wire.ConnectedShapes)
                {
                    bool found = false;

                    // If a wire is connected to something, then either the wire is 
                    // connected by an endpoint to the other shape...
                    foreach (EndPoint endpoint in wire.Endpoints)
                    {
                        if (endpoint.ConnectedShape == connected)
                        {
                            found = true;
                            break;
                        }
                    }

                    // ...or the other shape is connected by an endpoint to the wire
                    foreach (EndPoint endpoint in connected.Endpoints)
                    {
                        if (endpoint.ConnectedShape == wire)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        throw new Exception("The wire " + wire + " is connected to " + connected + 
                            " as a connected shape, but neither has an endpoint connection to the other!");
                }
            }

#endif

        }

        #endregion

        #region Number Inputs and Outputs

        /// <summary>
        /// Returns a pair of the minimum (first) and maximum (second) number of inputs that this gate can have.
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        public IntRange NumberInputs(ShapeType shape)
        {
            if (shape == LogicDomain.SUBCIRCUIT)
                return new IntRange(1, int.MaxValue); // we check validity in checkSubcircuits()
            else if (shape == LogicDomain.NOT || shape == LogicDomain.NOTBUBBLE)
                return new IntRange(1, 1);
            else return new IntRange(1, int.MaxValue);
        }

        /// <summary>
        /// Returns a pair of the minimum (first) and maximum (second) number of outputs that this shape can have.
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        public IntRange NumberOutputs(ShapeType shape)
        {
            if (shape == LogicDomain.SUBCIRCUIT)
                return new IntRange(1, int.MaxValue); // we check validity in checkSubcircuits()
            else if (LogicDomain.IsGate(shape))
                return new IntRange(1, 1);
            return new IntRange(1, int.MaxValue);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Takes a wire and returns the shape that is closest to the end of the wire
        /// along the line shooting off of the end.
        /// </summary>
        /// <param name="beginning">Which end we're looking at.</param>
        /// <param name="wire">The wire we're considering.</param>
        /// <param name="sketch">The sketch providing the context for the wire.</param>
        /// <returns>The shape that's closest to the tip of the wire.</returns>
        private Sketch.Shape findClosest(bool beginning, Sketch.Substroke wire, Sketch.Sketch sketch)
        {
            List<Sketch.Substroke> intersects = new List<Sketch.Substroke>();

            // One method of removing hooks (in situations where the substroke loops around to
            // get closer to its endpoint.
            List<Sketch.Point> points = Featurefy.Compute.DeHook(wire).PointsL;
            points.Sort();
            if (!beginning)
                points.Reverse();

            // First we will generate the line segments shooting off the endpoints of the wires.
            Sketch.LineSegment segment1;
            int count = 20;

            if (points.Count < 2 * count)
                segment1 = new Sketch.LineSegment(points[0], points[points.Count - 1]);
            else
            {
                segment1 = new Sketch.LineSegment(points.GetRange(0, count).ToArray());
            }

            Sketch.Substroke closestSubstroke = null;
            Sketch.Substroke closestLinestroke = null;
            double closestSubstrokeDistance = Double.PositiveInfinity;
            double closestLineDistance = Double.PositiveInfinity;

            // Now, consider every substroke in the sketch.
            foreach (Sketch.Substroke substroke in sketch.SubstrokesL)
                if (substroke.Id != wire.Id)
                    // We want this stroke if it is close to the line shooting out of the end of the wire
                    // Or if it is close enough to the endpoint.
                    foreach (Sketch.Point point in substroke.Points)
                    {
                        double linedistance = point.distance(segment1.getClosestPointOnLine(point));
                        double pointdistance = point.distance(points[0]);
                        if (pointdistance < closestSubstrokeDistance)
                        {
                            closestSubstrokeDistance = point.distance(points[0]);
                            closestSubstroke = substroke;
                        }
                        if (2 * linedistance + pointdistance < closestLineDistance)
                        {
                            // We want to make sure it's on the correct side of the line.
                            Sketch.Point linepoint = segment1.getClosestPointOnLine(point);
                            if ((segment1.StartPoint.X < segment1.EndPoint.X && linepoint.X < segment1.StartPoint.X)
                                || (segment1.StartPoint.X > segment1.EndPoint.X && linepoint.X > segment1.StartPoint.X))
                            {
                                closestLineDistance = 2 * linedistance + pointdistance;
                                closestLinestroke = substroke;
                            }
                        }
                    }


            // Returning the right substroke:

            double thresholdLineDistance = 50.0; // MAGIC NUMBER! A somewhat arbitrary number pulled out of the air.
            double thresholdSubstrokeDistance = 20.0; // MAGIC NUMBER! A somewhat arbitrary number pulled out of the air.
            // The threshold is stricter on substrokes that are nearby but
            // are not in the right direction

            // Substrokes that are close to the projected line get priority.
            if ((closestLineDistance < thresholdLineDistance) &&
                (closestLinestroke != null) &&
                (closestLinestroke.ParentShape != null))
                return closestLinestroke.ParentShape;

            // If the Linestroke wasn't good enough, try the closest substroke. 
            else if ((closestSubstrokeDistance < thresholdSubstrokeDistance) &&
                (closestSubstroke != null) &&
                (closestSubstroke.ParentShape != null))
                return closestSubstroke.ParentShape;

            // We get here if none of the substrokes were close enough
            else
                return null;


            //// Now we need to decide whether we want the closest substroke, or the one closest to the line...
            //if (closestLineDistance / 5 > closestSubstrokeDistance)
            //    if (closestSubstroke == null)
            //        return null;
            //    else
            //        return closestSubstroke.ParentShapes[0];
            //else
            //    if (closestLinestroke == null)
            //        return null;
            //    else
            //        return closestLinestroke.ParentShapes[0];
        }


        /// <summary>
        /// Determines if shape2 is contained in the boundingbox of shape1
        /// more accurately it checks that at most some fraction of the points are not contained
        /// </summary>
        /// <param name="shape2"></param>
        /// <param name="shape1"></param>
        /// <returns></returns>
        private bool contains(Sketch.Shape shape1, Sketch.Shape shape2)
        {
            RectangleF bbox1 = Featurefy.Compute.BoundingBox(shape1.Points.ToArray());
            int badPts = 0;
            foreach (Sketch.Point pt in shape2.Points)
            {
                System.Drawing.PointF pf = pt.SysDrawPointF;
                if (!(bbox1.Contains(pf)))
                    badPts++;
            }
            //THIS IS A MAGIC NUMBER I PULLED OUT OF THE AIR
            double MAXIMUM_BAD_PERCENT = .2;
            if (shape2.Points.Count != 0)
                return MAXIMUM_BAD_PERCENT > badPts / shape2.Points.Count;
            return true;
        }


        /// <summary>
        /// Find the two closest shapes to the given shape in the sketch.
        /// </summary>
        /// <param name="shape">Given shape</param>
        /// <param name="sketch">Sketch we are working with</param>
        /// <returns>List of the two closest shapes to the given shape</returns>
        private List<Sketch.Shape> twoClosest(Sketch.Shape shape, Sketch.Sketch sketch)
        {
            double d1 = double.PositiveInfinity; // closer of the two
            double d2 = double.PositiveInfinity; // farther of the two
            Sketch.Shape closest1 = null;
            Sketch.Shape closest2 = null;

            foreach (Sketch.Shape otherShape in sketch.Shapes)
            {
                if (otherShape != shape) // Make sure the shape isn't close to itself
                {
                    double d = Sketch.Sketch.MinDistance(shape, otherShape);
                    if (d < d1) // then d is also less than d2
                    {
                        d2 = d1;
                        closest2 = closest1;

                        d1 = d;
                        closest1 = otherShape;
                    }
                    else if (d < d2) // if we're here, then d1 < d < d2
                    {
                        d2 = d;
                        closest2 = otherShape;
                    }
                }
            }

            List<Sketch.Shape> closeShapes = new List<Sketch.Shape>();
            closeShapes.Add(closest1);
            closeShapes.Add(closest2);
            return closeShapes;
        }

        #endregion

    }

}
