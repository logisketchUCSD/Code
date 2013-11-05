using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RecognitionInterfaces;
using Sketch;
using Featurefy;
using ContextDomain;
using Data;

namespace Refiner
{

    #region Helper Classes - Search Methods

    /// <summary>
    /// A search method is responsible for choosing which action to take next from a given list.
    /// </summary>
    interface ISearchMethod
    {

        /// <summary>
        /// Choose the action to take from a list of available actions.
        /// </summary>
        /// <param name="availableActions">the list of available actions</param>
        /// <param name="sketch">the sketch, which will be unmodified at the end of this function</param>
        /// <returns>the action that should be taken, or null if the search should terminate</returns>
        SketchModification chooseAction(
            List<SketchModification> availableActions, 
            Sketch.Sketch sketch);

    }

    /// <summary>
    /// Hill climb search naively chooses the action with the greatest benefit.
    /// </summary>
    class HillClimbSearch : ISearchMethod
    {

        public SketchModification chooseAction(List<SketchModification> availableActions, Sketch.Sketch sketch)
        {

            SketchModification result = null;

            double max = 0;

            foreach (SketchModification action in availableActions)
            {
                double change = action.benefit();
                if (change > max)
                {
                    result = action;
                    max = change;
                }
            }

            return result;

        }

    }

    #endregion

    /// <summary>
    /// The search refiner works on the following premise:
    /// 
    ///     The user drew a sketch that, when correctly recognized,
    ///     is a completely valid circuit.
    /// 
    /// The goal of this refiner, then, is to search for that interpretation.
    /// It requires that the other parts of the recognition process are capable
    /// of leading us in the right direction, but no more than that.
    /// 
    /// This works by using the CorrectnessScore method of a ContextDomain
    /// object to assess the quality of recognition. 
    /// 
    /// The SearchRefiner operates independently of any particular domain.
    /// </summary>
    public class SearchRefiner : IRecognitionStep
    {

        /// <summary>
        /// To prevent looping forever, we always stop after this many iterations.
        /// </summary>
        private const int MAX_ITERATIONS = 50;

        /// <summary>
        /// Turn debugging on or off
        /// </summary>
        private static readonly bool debug = true;

        private ISearchMethod _searchMethod;
        private ISketchModificationProducer _producer;

        /// <summary>
        /// Create a new search refiner for the specified domain
        /// </summary>
        /// <param name="producer"></param>
        public SearchRefiner(ISketchModificationProducer producer)
        {
            _producer = producer;
            _searchMethod = new HillClimbSearch();
        }

        public string ProgressString
        {
            get { return "Running search refiner"; }
        }

        public void process(FeatureSketch featureSketch)
        {
            _producer.Start(featureSketch);

            Sketch.Sketch sketch = featureSketch.Sketch;
            SketchModification actionToTake;

            for (int i = 0; i < MAX_ITERATIONS; i++)
            {
                List<SketchModification> modifications = _producer.SketchModifications(featureSketch);
                actionToTake = _searchMethod.chooseAction(modifications, sketch);
                if (actionToTake == null)
                    break;

                if (debug)
                    Console.WriteLine("Performing action: " + actionToTake);

                actionToTake.perform();
            }
        }

    }

}
