using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sketch;
using Sketch.Operations;
using Domain;
using Data;
using ContextDomain;
using RecognitionInterfaces;
using Recognizers;
using Featurefy;

namespace Refiner
{

    #region Helper Class (RunConnectorOperation)

    class RunConnectorOperation : StandardSketchOperation
    {
        private FeatureSketch _sketch;
        private Connector _connector;

        public RunConnectorOperation(FeatureSketch sketch, Connector connector)
            :base(sketch.Sketch)
        {
            _sketch = sketch;
            _connector = connector;
        }

        protected override bool modifiesTypes()
        {
            return false;
        }

        protected override void performActual()
        {
            _connector.process(_sketch);
        }
    }
    
    #endregion

    #region Helper Class (RelabelShapeOperation)

    public class RelabelShapeOperation : StandardSketchOperation
    {
        
        private Shape _shape;
        private RecognitionResult _recognition;

        public RelabelShapeOperation(Sketch.Sketch sketch, Shape shape, RecognitionResult recognition)
            :base(sketch)
        {
            _shape = shape;
            _recognition = recognition;
        }

        protected override bool modifiesGeometry()
        {
            return false;
        }

        protected override bool modifiesConnections()
        {
            return false;
        }

        protected override void performActual()
        {
            _recognition.ApplyToShape(_shape);
        }

    }


    #endregion

    #region Helper Class (Phantom Shape)

    /// <summary>
    /// A phantom shape lets us make temporary shapes nondestructively.
    /// </summary>
    class PhantomShape : Shape
    {
        /// <summary>
        /// When a phantom shape acquires a substroke, it does not update
        /// the parent shape. Thus, phantom shapes contain substrokes without
        /// the substrokes knowing about it.
        /// </summary>
        /// <param name="substroke"></param>
        /// <returns></returns>
        protected override void AcquireSubstroke(Substroke substroke)
        {
        }
    }

    #endregion

    #region Helper Class (Substroke Collection)

    class SubstrokeCollection : IEnumerable<Substroke>
    {
        private List<Substroke> _substrokes;
        private int hash;
        public SubstrokeCollection(IEnumerable<Substroke> substrokes)
        {
            _substrokes = new List<Substroke>(substrokes);
            hash = 0;
            foreach (Substroke s in _substrokes)
                hash ^= s.GetHashCode();
        }
        public List<Substroke> Substrokes
        {
            get { return _substrokes; }
        }
        public int Count
        {
            get { return _substrokes.Count; }
        }
        public override int GetHashCode()
        {
            return hash;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _substrokes.GetEnumerator();
        }
        public IEnumerator<Substroke> GetEnumerator()
        {
            return _substrokes.GetEnumerator();
        }
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SubstrokeCollection))
                return false;

            if (obj == this)
                return true;

            SubstrokeCollection collection = (SubstrokeCollection)obj;
            if (collection.Count != Count)
                return false;

            for (int i = 0; i < Count; i++)
                if (collection._substrokes[i].Id != _substrokes[i].Id)
                    return false;

            return true;
        }
        public static implicit operator SubstrokeCollection(Shape s)
        {
            return new SubstrokeCollection(s.SubstrokesL);
        }
        public static implicit operator SubstrokeCollection(List<Substroke> s)
        {
            return new SubstrokeCollection(s);
        }
    }

    #endregion

    /// <summary>
    /// This class generates potential modifications for the search refiner. It uses domain-specific knowledge
    /// relating to the circuit domain, so it is circuit-domain-only.
    /// 
    /// In order to interface with the search refiner, we define an energy function E : Sketch -> double
    /// which we are trying to maximize (see computeEnergy below).
    /// 
    /// </summary>
    public class CircuitSketchModificationProducer : ISketchModificationProducer
    {

        #region Constants

        /// <summary>
        /// Shapes will not attempt to steal substrokes more distant than STROKE_STEAL_THRESHOLD.
        /// </summary>
        const double STROKE_STEAL_THRESHOLD = 20;

        /// <summary>
        /// Modifications with benefits less than this will be culled. 
        /// Set this to double.NegativeInfinity if you want to emit all
        /// possible modifications.
        /// </summary>
        const double BENEFIT_CUTOFF = 0.0;

        /// <summary>
        /// Turn on/off debugging messages
        /// </summary>
        private static readonly bool debug = false;

        #endregion

        #region Internals

        private Classifier _classifier;
        private ExtendedRecognizer _gateRecognizer;
        private TextRecognizer _textRecognizer;
        private Recognizer _wireRecognizer;
        private Connector _connector;
        private CircuitDomain _domain;
        private SmartCache<Substroke, string> _classifications;
        private Dictionary<SubstrokeCollection, Dictionary<ShapeType, RecognitionResult>> _identificationResults;

        #endregion

        #region Constructor

        /// <summary>
        /// Construct a sketch modification producer in the circuit domain using the given classifier and recognizer.
        /// It finds the circuit domain using CircuitDomain.GetInstance().
        /// </summary>
        public CircuitSketchModificationProducer(
            Classifier classifier, 
            ExtendedRecognizer gateRecognizer, 
            Connector connector,
            TextRecognizer textRecognizer = null, 
            Recognizer wireRecognizer = null)
        {
            _domain = CircuitDomain.GetInstance();
            _classifier = classifier;
            _gateRecognizer = gateRecognizer;
            _textRecognizer = (textRecognizer == null) ? new TextRecognizer() : textRecognizer;
            _wireRecognizer = (wireRecognizer == null) ? new WireRecognizer() : wireRecognizer;
            _connector = connector;

            // caches are initialized in Start(FeatureSketch)
        }

        #endregion

        #region Start (sets up caches)

        /// <summary>
        /// Sets up the caches for a given featureSketch. This step is very important, since without caching
        /// the search refiner is intolerably slow.
        /// </summary>
        /// <param name="featureSketch"></param>
        public virtual void Start(Featurefy.FeatureSketch featureSketch)
        {
            _classifications = new SmartCache<Substroke, string>(
                s => { 
                    return _classifier.classify(s, featureSketch); 
                });
            _identificationResults = new Dictionary<SubstrokeCollection, Dictionary<ShapeType, RecognitionResult>>();
        }

        #endregion

        #region Recognition

        /// <summary>
        /// Compute the probability that a given set of substrokes has the given type.
        /// </summary>
        /// <param name="substrokes"></param>
        /// <param name="type"></param>
        /// <param name="featureSketch"></param>
        /// <returns>a pair containing the recognition probability and the orientation</returns>
        private RecognitionResult computeRecognitionProbabilityForTextOrWire(SubstrokeCollection substrokes, ShapeType type, FeatureSketch featureSketch)
        {
            double probability = 0;
            double orientation = 0;

            PhantomShape shape = new PhantomShape();
            shape.AddSubstrokes(substrokes);

            if (LogicDomain.IsWire(type))
            {
                // the probability it is a wire is defined as
                // (# substrokes classified as wires) / (total # substrokes)

                int numSubstrokes = substrokes.Count;
                int numWireSubstrokes = 0;

                foreach (Substroke substroke in substrokes)
                    if (_classifications[substroke] == LogicDomain.WIRE_CLASS)
                        numWireSubstrokes++;

                probability = (double)numWireSubstrokes / numSubstrokes;
                return new RecognitionResult(LogicDomain.WIRE, probability, orientation);
            }
            else if (LogicDomain.IsText(type))
            {
                // the probability it is text is defined as
                // (# substrokes classified as text) / (total # substrokes)

                int numSubstrokes = substrokes.Count;
                int numTextSubstrokes = 0;

                foreach (Substroke substroke in substrokes)
                    if (_classifications[substroke] == LogicDomain.TEXT_CLASS)
                        numTextSubstrokes++;

                probability = (double)numTextSubstrokes / numSubstrokes;
                return new TextRecognitionResult(probability, _textRecognizer.read(shape));
            }

            return null;

        }

        /// <summary>
        /// For each type, get an estimate of the probability that the given shape is of the given type
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="shape"></param>
        /// <param name="types"></param>
        /// <param name="featureSketch"></param>
        /// <returns></returns>
        private Dictionary<ShapeType, RecognitionResult> computeRecognitionProbabilities(
            SubstrokeCollection substrokes, 
            FeatureSketch featureSketch)
        {
            
            PhantomShape shape = new PhantomShape();
            shape.AddSubstrokes(substrokes);

            Dictionary<ShapeType, RecognitionResult> results = Data.Utils.replaceValues(
                _gateRecognizer.RecognitionResults(shape, LogicDomain.Gates), 
                r => { return (RecognitionResult)r; });

            foreach (ShapeType type in LogicDomain.Types)
            {
                if (!results.ContainsKey(type))
                    results.Add(type, computeRecognitionProbabilityForTextOrWire(substrokes, type, featureSketch));
            }

            return results;
        }

        /// <summary>
        /// Get an estimate of the probability that the given shape is of the given type
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="type"></param>
        /// <param name="orientation">[out] the best orientation for the given type</param>
        /// <returns>the probability</returns>
        private RecognitionResult RecognizeAsType(
            SubstrokeCollection substrokes, 
            ShapeType type,
            FeatureSketch featureSketch)
        {
            if (!_identificationResults.ContainsKey(substrokes))
                _identificationResults.Add(substrokes, computeRecognitionProbabilities(substrokes, featureSketch));
            return _identificationResults[substrokes][type];
        }

        /// <summary>
        /// Suppose that the given substrokes form a shape. What is the most likely type for it?
        /// </summary>
        /// <param name="substrokes"></param>
        /// <param name="featureSketch"></param>
        /// <returns></returns>
        private RecognitionResult Identify(SubstrokeCollection substrokes, Featurefy.FeatureSketch featureSketch)
        {
            RecognitionResult best = null;
            foreach (ShapeType type in LogicDomain.Types)
            {
                RecognitionResult result = RecognizeAsType(substrokes, type, featureSketch);
                double prob = result.Confidence;
                if (best == null || prob > best.Confidence)
                    best = result;
            }
            return best;
        }

        #endregion

        #region Alternate types for a Shape

        /// <summary>
        /// Get the list of all possible types for a shape, excluding the shape's current type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<ShapeType> AlternateTypes(ShapeType type)
        {
            return LogicDomain.Types.FindAll(delegate(ShapeType t) { return t != type; });
        }

        #endregion

        #region Find things in a Sketch

        /// <summary>
        /// Get a set of all wire endpoints that are missing connections.
        /// </summary>
        /// <param name="sketch"></param>
        /// <returns></returns>
        private List<EndPoint> findWireEndpointsMissingConnections(Sketch.Sketch sketch)
        {
            List<EndPoint> wiresMissingConnections = new List<EndPoint>();
            List<Shape> nonWiresMissingWireConnections = new List<Shape>();
            foreach (Shape shape in sketch.Shapes)
                if (shape.Type == LogicDomain.WIRE)
                    foreach (EndPoint endpoint in shape.Endpoints)
                        if (endpoint.ConnectedShape == null)
                            wiresMissingConnections.Add(endpoint);
            return wiresMissingConnections;
        }

        /// <summary>
        /// Get a set of all non-wire shapes whose contexts would be improved if they were connected to another wire.
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="closestContexts"></param>
        /// <returns></returns>
        private List<Shape> findNonWiresMissingConnections(Sketch.Sketch sketch, Dictionary<Shape, Tuple<double, ConnectionContext>> closestContexts)
        {
            List<Shape> nonWiresMissingWireConnections = new List<Shape>();
            foreach (Shape shape in sketch.Shapes)
            {
                if (shape.Type != LogicDomain.WIRE)
                {
                    Tuple<double, ConnectionContext> currentContext = closestContexts[shape];
                    Tuple<double, ConnectionContext> potentialContext = _domain.ClosestContextIfConnectedToType(shape, LogicDomain.WIRE_CLASS);

                    // If we would be better off connecting this to a wire, let's mark it as such
                    // Also, if we *could* make this connection without hurting things, let's do it
                    // (if only to fix the missing wire endpoint connection)
                    if (potentialContext.Item1 >= currentContext.Item1)
                        nonWiresMissingWireConnections.Add(shape);
                }
            }
            return nonWiresMissingWireConnections;
        }
        
        /// <summary>
        /// Find the set of all substrokes close to a given shape, but not farther away than the given threshold.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="sketch"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private List<Substroke> findSubstrokesCloseTo(Shape shape, Sketch.Sketch sketch, double threshold)
        {

            // TODO: This could be optimized by caching an adjacency matrix that would allow us to instantly
            // look up the distance between two substrokes.

            var result =
                from substroke in sketch.SubstrokesL
                where (substroke.ParentShape != shape && Sketch.Sketch.MinDistance(shape.Points, substroke.Points) < threshold)
                select substroke;

            return new List<Substroke>(result);
        }

        #endregion

        #region Energy Function

        /// <summary>
        /// Compute the energy function that we are trying to maximize.
        /// </summary>
        /// <param name="sketch"></param>
        /// <returns>an unbounded double representing the energy of the sketch</returns>
        private double computeEnergy(FeatureSketch featureSketch)
        {
            Sketch.Sketch sketch = featureSketch.Sketch;
            featureSketch.hasConsistentSubstrokes();
            sketch.CheckConsistency();
            double energy = 0;
            var shapes = sketch.ShapesL;
            int numShapes = shapes.Count;
            int numGates = shapes.FindAll(s => LogicDomain.IsGate(s.Type)).Count;
            foreach (Shape shape in shapes)
            {

                // Add energy for every connection
#if false
                // This is a bad idea. It favors interpretations with more connections, which basically means
                // that everything should alternate wire-text-wire-text-wire-...
                foreach (Shape connected in shape.ConnectedShapes)
                {
                    if (connected == shape)
                        continue;
                    if (connected.Type != LogicDomain.WIRE)
                        continue;
                    double connectionDistance = double.PositiveInfinity;
                    foreach (EndPoint endpoint in connected.Endpoints)
                        connectionDistance = Math.Min(shape.minDistanceTo(endpoint.X, endpoint.Y), connectionDistance);
                    connectionDistance = Math.Max(connectionDistance, 0.001); // avoid problems when connection distance is close to zero
                    energy += 1 + 1 / connectionDistance;
                }
#endif

                // Add the context match score
                energy += (double)_domain.ClosestContext(shape).Item1 / numShapes;

                // Get recognition results
                RecognitionResult result = RecognizeAsType(shape, shape.Type, featureSketch);
                double confidence = result.Confidence;

                // Add the recognition score
                energy += confidence / numShapes;

#if false
                // Gate orientation also contributes
                if (LogicDomain.IsGate(shape.Type))
                {
                    // Determine the recognizer's and the connector's orientation values, 
                    // in the range [0, 2Pi]
                    double orientation = result.Orientation;
                    double otherOrientation = _domain.OrientShape(shape, sketch);

                    // Orientation might be off by PI...
                    double dist1 = Math.Abs(otherOrientation - orientation);
                    double dist2 = Math.Abs(otherOrientation - Math.PI - orientation);
                    double dist = Math.Min(dist1, dist2);

                    // Add orientation score
                    double twoPI = 2 * Math.PI;
                    energy += (1 - (dist / twoPI)) / numGates;
                }
#endif

            }
            return energy;
        }

        #endregion

        #region Main Method: Get a list of sketch modifications

        public List<SketchModification> SketchModifications(Featurefy.FeatureSketch featureSketch)
        {

            Sketch.Sketch sketch = featureSketch.Sketch;

            if (debug)
                Console.WriteLine("Sketch Modifications:");

            // Used to assemble the list of results
            List<SketchModification> results = new List<SketchModification>();

            // Precompute closest contexts for each shape
            Dictionary<Shape, Tuple<double, ConnectionContext>> closestContexts = new Dictionary<Shape, Tuple<double, ConnectionContext>>();
            foreach (Shape shape in sketch.Shapes)
                closestContexts.Add(shape, _domain.ClosestContext(shape));

            // ==========================================================================================================

            /* 
             * Operation zero: running the connector is ALWAYS an option.
             */

            //results.Add(new SketchModification(sketch, new RunConnectorOperation(featureSketch, _connector), computeEnergy));

            // ==========================================================================================================

            /* 
             * First things first: missing connections
             * This takes care of obvious connector problems. If there is a wire with a dangling endpoint and a
             * shape that would be better off connected to a wire, we make that connection. The benefit is:
             *    benefit = 1 / distance
             * where "distance" is the minimum distance from the dangling endpoint to the shape. This will favor
             * close connections over distant ones.
             */

            List<EndPoint> wiresMissingConnections = findWireEndpointsMissingConnections(sketch);
            List<Shape> nonWiresMissingWireConnections = findNonWiresMissingConnections(sketch, closestContexts);
            foreach (EndPoint wireEndpoint in wiresMissingConnections)
            {
                foreach (Shape shape in nonWiresMissingWireConnections)
                {
                    Shape wire = wireEndpoint.ParentShape;
                    if (debug)
                        Console.WriteLine("ACTION (connect wire endpoint): " + sketch + ", " + wire + ", " + wireEndpoint + ", " + shape);
                    var op = new ConnectEndpointOperation(sketch, wireEndpoint, shape);
                    var modification = new SketchModification(featureSketch, op, computeEnergy);
                    results.Add(modification);
                    
                }
            }

            // ==========================================================================================================

            /*
             * Second: relabeling
             * Now we go through every shape and see if its context would be better matched as a different shape.
             * If so, we can change the shape. The benefit is the % improvement in context score plus the %
             * improvement in recognition quality.
             */

            foreach (Shape shape in sketch.Shapes)
            {
                if (shape.AlreadyLabeled)
                    continue;

                Tuple<double, ConnectionContext> currentContext = closestContexts[shape];
                List<ShapeType> allTypes = LogicDomain.Types;

                foreach (ShapeType otherType in AlternateTypes(shape.Type))
                {
                    if (debug)
                        Console.WriteLine("ACTION (relabel shape): " + shape + ", " + otherType);
                    var op = new RelabelShapeOperation(sketch, shape, RecognizeAsType(shape, otherType, featureSketch));
                    var modification = new SketchModification(featureSketch, op, computeEnergy);
                    results.Add(modification);
                }
            }

            // ==========================================================================================================

            /*
             * Third: stroke steal
             * This works as follows:
             * 
             * For every shape, get the set of closeSubstrokes (within a certain threshold).
             *     For every substroke in closeSubstrokes
             *         generate a steal modification (substroke --> shape)
             *             
             * The steal modifications should have their benefit based on
             *    (1) connection contexts
             *    (2) recognition quality
             */
            
            foreach (Shape thief in sketch.Shapes)
            {
                if (thief.AlreadyLabeled)
                    continue;

                List<Substroke> closeSubstrokes = findSubstrokesCloseTo(thief, sketch, STROKE_STEAL_THRESHOLD);

                foreach (Substroke gem in closeSubstrokes) // thiefs steal gems
                {

                    Shape victim = gem.ParentShape; // thiefs steal from victims

                    if (victim.AlreadyLabeled)
                        continue;

                    // find the thief's new type
                    var newThiefSubstrokes = new List<Substroke>(thief.SubstrokesL);
                    newThiefSubstrokes.Add(gem);
                    RecognitionResult newThiefRecognition = Identify(newThiefSubstrokes, featureSketch);

                    if (debug)
                        Console.WriteLine("ACTION (steal stroke): " + thief + ", " + victim + ", " + gem);
                    var stealOp = new StrokeStealOperation(sketch, thief, gem);
                    var relabelThiefOp = new RelabelShapeOperation(sketch, thief, newThiefRecognition);
                    var runConnectorOp = new RunConnectorOperation(featureSketch, _connector);
                    ISketchOperation op;

                    // if the victim will still be around after the steal
                    if (victim.Substrokes.Length > 1)
                    {
                        var newVictimSubstrokes = new List<Substroke>(victim.SubstrokesL);
                        newVictimSubstrokes.Remove(gem);
                        RecognitionResult newVictimRecognition = Identify(newVictimSubstrokes, featureSketch);

                        var relabelVictimOp = new RelabelShapeOperation(sketch, victim, newVictimRecognition);

                        op = new CompoundSketchOperation(stealOp, relabelThiefOp, relabelVictimOp, runConnectorOp);
                    }
                    else
                    {
                        op = new CompoundSketchOperation(stealOp, relabelThiefOp, runConnectorOp);
                    }
                    var modification = new SketchModification(featureSketch, op, computeEnergy);
                    results.Add(modification);

                }
            }

            if (debug && results.Count == 0)
                Console.WriteLine("(none)");

            // Keep only the ones greater than the cutoff
            results = results.FindAll(r => { return r.benefit() > BENEFIT_CUTOFF; });

            return results;
        }

        #endregion

    }
}
