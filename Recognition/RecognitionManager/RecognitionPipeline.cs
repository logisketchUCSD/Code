using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RecognitionInterfaces;
using Featurefy;

namespace RecognitionManager
{
    /// </summary>
    /// <summary>
    /// Callback type
    /// </summary>
    /// <param name="step"></param>
    /// <param name="progress"></param>
    public delegate void StepStarted(IRecognitionStep step, double progress);


    #region SketchProcessor class

    /// <summary>
    /// An object that allows fine control over the recognition process
    /// </summary>
    public class SketchProcessor
    {
        private FeatureSketch _sketch;
        private List<IRecognitionStep> _steps;
        private int _i;

        internal SketchProcessor(IEnumerable<IRecognitionStep> steps, FeatureSketch sketch)
        {
            _sketch = sketch;
            _steps = new List<IRecognitionStep>(steps);
            _i = 0;
        }

        /// <summary>
        /// Determine whether the pipeline is finished
        /// </summary>
        /// <returns></returns>
        public bool isFinished()
        {
            return _i == _steps.Count;
        }

        /// <summary>
        /// Execute the next step of the pipeline. This should only be called if isFinished()
        /// returns true.
        /// </summary>
        public void executeNextStep()
        {
            var step = NextStep;
            step.process(_sketch);
            _i++;
        }

        /// <summary>
        /// Get the next step to be executed.  This should only be called if isFinished()
        /// returns true.
        /// </summary>
        public IRecognitionStep NextStep
        {
            get { return _steps[_i]; }
        }

        /// <summary>
        /// Get the percent through the pipeline (in the range [0, 1]).
        /// </summary>
        public double Progress
        {
            get { return (double)_i / _steps.Count; }
        }
    }

    #endregion

    /// <summary>
    /// The RecognitionPipeline is responsible for handling the steps in the recognition process. It
    /// aims to be an easy-to-use class that can run recognition steps for you.
    /// </summary>
    public class RecognitionPipeline : IRecognitionStep
    {

        #region Default Steps
        
#if AIR_OFF
        private static readonly Lazy<ExtendedRecognizer> gateRecognizer = new Lazy<ExtendedRecognizer>(delegate() { return Recognizers.ImageRecognizer.Load(AppDomain.CurrentDomain.BaseDirectory + @"\\SubRecognizers\ImageRecognizer\Image.ir"); });
#else
        private static readonly Lazy<ExtendedRecognizer> gateRecognizer = new Lazy<ExtendedRecognizer>(delegate() { return Recognizers.AdaptiveImageRecognizer.LoadDefault(); });
#endif

        private static readonly Lazy<ContextDomain.ContextDomain> defaultDomain = new Lazy<ContextDomain.ContextDomain>(delegate() { return ContextDomain.CircuitDomain.GetInstance(); });
        private static readonly Lazy<Classifier> defaultClassifier = new Lazy<Classifier>(delegate() { return new StrokeClassifier.StrokeClassifier(); });
        private static readonly Lazy<Grouper> defaultGrouper = new Lazy<Grouper>(delegate() { return new StrokeGrouper.StrokeGrouper(); });
        private static readonly Lazy<Recognizer> defaultRecognizer = new Lazy<Recognizer>(delegate() { return new Recognizers.UniversalRecognizer(null, null, gateRecognizer.Value); });
        private static readonly Lazy<Orienter> defaultOrienter = new Lazy<Orienter>(delegate() { return new Orienter(Recognizers.ImageRecognizer.Load(AppDomain.CurrentDomain.BaseDirectory + @"\\SubRecognizers\ImageRecognizer\Image.ir")); });
        private static readonly Lazy<Connector> defaultConnector = new Lazy<Connector>(delegate() { return new Connector(defaultDomain.Value); });
        private static readonly Lazy<RecognitionPipeline> defaultRefiner = new Lazy<RecognitionPipeline>(delegate()
        {
            RecognitionPipeline refinement = new RecognitionPipeline();
#if USE_SEARCH_REFINEMENT
            Refiner.ISketchModificationProducer producer = new Refiner.CircuitSketchModificationProducer(defaultClassifier.Value, gateRecognizer.Value, defaultConnector.Value);
            refinement.addStep(new Refiner.SearchRefiner(producer));
#else
            refinement.addStep(new Refiner.CarefulContextRefiner(defaultDomain.Value, defaultRecognizer.Value));
#endif
            //refinement.addStep(new Refiner.StrokeStealRefiner(connector, recognizer));
            //refinement.addStep(new Refiner.StrokeShedRefiner(connector, recognizer));
            refinement.addStep(new Refiner.UniqueNamer());
            
            /*
             * The GroupNotBubble stage is turned off because it is too limited. We allow notbubbles to be connected
             * as inputs to gates as well as outputs, and if you have a notbubble as an input to an AND gate it should
             * not become a NAND gate. This problem cannot be fixed until the refiner has access to information about
             * whether connections are inputs or outputs.
             */
            //refinement.addStep(new Refiner.GroupNotBubble());

            refinement.addStep(new Refiner.RecognizedMarker());
            return refinement;
        });

        /// <summary>
        /// Create a default recognition pipeline with the following steps:
        ///   1: Classify Single Strokes
        ///   2: Group Strokes into Shapes
        ///   3: Recognize Shapes
        ///   4: Connect Shapes
        ///   5: Refine Recognition
        /// </summary>
        public static RecognitionPipeline createDefaultPipeline(Dictionary<string, string> settings)
        {
            RecognitionPipeline result = new RecognitionPipeline();
            result.addStep(new WaitForFeatureSketch());
            result.addStep(createDefaultClassifier());
            result.addStep(createDefaultGrouper());
            result.addStep(createDefaultRecognizer());
            result.addStep(createDefaultConnector());
            result.append(createDefaultRefiner());
            return result;
        }

        /// <summary>
        /// The pipeline for recognizing on the fly
        /// </summary>
        /// <returns></returns>
        public static RecognitionPipeline GetOnFlyPipeline()
        {
            Recognizer recognizer = createDefaultRecognizer();

            RecognitionPipeline result = new RecognitionPipeline();
            result.addStep(createDefaultClassifier());
            result.addStep(createDefaultGrouper());
            result.addStep(recognizer);
            return result;
        }

        /// <summary>
        /// Return the classifier that should be used by the main UI program.
        /// </summary>
        /// <returns>a new classifier</returns>
        public static Classifier createDefaultClassifier()
        {
            return defaultClassifier.Value;
        }

        /// <summary>
        /// Return the grouper that should be used by the main UI program.
        /// </summary>
        /// <returns>a new grouper</returns>
        public static Grouper createDefaultGrouper()
        {
            return defaultGrouper.Value;
        }

        /// <summary>
        /// Return the gate-only recognizer that should be used by the main UI program.
        /// </summary>
        /// <returns>a new recognizer</returns>
        public static ExtendedRecognizer createDefaultGateRecognizer()
        {
            return gateRecognizer.Value;
        }

        /// <summary>
        /// Return the recognizer that should be used by the main UI program. Recognizes
        /// wires, gates, and text.
        /// </summary>
        /// <returns>a new recognizer</returns>
        public static Recognizer createDefaultRecognizer()
        {
            return defaultRecognizer.Value;
        }

        /// <summary>
        /// Return the orienter that should be used by the main UI program.
        /// </summary>
        /// <returns></returns>
        public static Orienter createDefaultOrienter()
        {
            return defaultOrienter.Value;
        }

        /// <summary>
        /// Return the connector that should be used by the main UI program.
        /// </summary>
        /// <returns>a new connector</returns>
        public static Connector createDefaultConnector()
        {
            return defaultConnector.Value;
        }

        /// <summary>
        /// Return the domain that should be used by the main UI program
        /// </summary>
        /// <returns></returns>
        public static ContextDomain.ContextDomain createDefaultDomain()
        {
            return defaultDomain.Value;
        }

        /// <summary>
        /// Return the refiner that should be used by the main UI program.
        /// </summary>
        /// <returns>a new refiner</returns>
        public static RecognitionPipeline createDefaultRefiner()
        {
            return defaultRefiner.Value;
        }

        #endregion

        #region Internals

        /// <summary>
        /// The list of steps performed by this pipeline.
        /// </summary>
        private List<IRecognitionStep> _steps;

        #endregion

        #region Constructor

        /// <summary>
        /// Create an empty RecognitionPipeline.
        /// </summary>
        public RecognitionPipeline()
        {
            _steps = new List<IRecognitionStep>();
        }

        /// <summary>
        /// Create a recognition pipeline from the given steps.
        /// </summary>
        /// <param name="steps"></param>
        public RecognitionPipeline(params IRecognitionStep[] steps)
        {
            _steps = new List<IRecognitionStep>(steps);
        }

        #endregion

        #region Managing the Pipeline

        /// <summary>
        /// Add a step to the pipeline.
        /// </summary>
        /// <param name="step">the step to add</param>
        public virtual void addStep(IRecognitionStep step)
        {
            _steps.Add(step);
        }

        /// <summary>
        /// Append all the steps from another pipeline to this one.
        /// </summary>
        /// <param name="other"></param>
        public virtual void append(RecognitionPipeline other)
        {
            _steps.AddRange(other._steps);
        }

        #endregion

        #region Processing a Sketch

        public virtual SketchProcessor getProcessor(FeatureSketch featureSketch)
        {
            return new SketchProcessor(_steps, featureSketch);
        }

        /// <summary>
        /// Process a sketch. This runs every recognition step on the sketch in
        /// the order they were added to this pipeline. This method does nothing
        /// if no steps have been added.
        /// </summary>
        /// <param name="featureSketch">the sketch to process</param>
        public virtual void process(FeatureSketch featureSketch)
        {
            SketchProcessor processor = getProcessor(featureSketch);
            while (!processor.isFinished())
                processor.executeNextStep();
        }

        /// <summary>
        /// Process an ordinary sketch by first creating a feature sketch for it.
        /// This method calls process using the created feature sketch, then
        /// returns the feature sketch, in case you want to use it. The given 
        /// ordinary sketch is modified in the process.
        /// </summary>
        /// <param name="sketch">the ordinary sketch to use</param>
        /// <returns>the featureSketch used during processing</returns>
        public FeatureSketch process(Sketch.Sketch sketch)
        {
            FeatureSketch featureSketch = FeatureSketch.MakeFeatureSketch(new Sketch.Project(sketch));
            process(featureSketch);
            return featureSketch;
        }

        /// <summary>
        /// Process an ordinary sketch by first creating a feature sketch for it.
        /// This method calls process using the created feature sketch, then
        /// returns the feature sketch, in case you want to use it. The given 
        /// ordinary sketch is modified in the process.
        /// </summary>
        /// <param name="project">the sketch project to use</param>
        /// <returns>the featureSketch used during processing</returns>
        public FeatureSketch process(Sketch.Project project)
        {
            FeatureSketch featureSketch = FeatureSketch.MakeFeatureSketch(project);
            process(featureSketch);
            return featureSketch;
        }

        #endregion

        #region Progress String

        public string ProgressString
        {
            get { return "Running pipeline"; }
        }

        #endregion

    }
}
