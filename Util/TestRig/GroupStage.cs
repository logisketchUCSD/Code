/*
 * File: GroupStage.cs
 *
 * Author: James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */


using Domain;
using System;
using System.Collections.Generic;
using RecognitionManager;

using TestRig.Utils;

namespace TestRig
{
    /// <summary>
	/// Represents the grouping stage of stroke processing.
	/// </summary>
	public class GroupStage : ProcessStage
	{
        private Table _results;
        private RecognitionPipeline _pipeline = new RecognitionPipeline();
        private bool _isPure;

        /// <summary>
        /// Create a GroupStage.
        /// </summary>
        public GroupStage()
        {
            name = "Grouper";
            shortname = "grp";
            _results = new Table(new string[] { "Test File", "Total Groups", "Groups Found", "Groups Found Correctly" });
            outputFiletype = ".csv"; // comma-separated values; readable by Excel
            _pipeline = new RecognitionPipeline();
            _isPure = true;
        }

        /// <summary>
        /// Process the arguments for the grouper.
        /// </summary>
        /// <param name="args">the arguments to process</param>
        public override void processArgs(string[] args)
        {
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-pure":           // A pure test is one in which the group stage uses the correct
                        _isPure = true;     // labels in the sketch to assume that a "perfect" classifier has
                        break;              // already been run. In an impure test, we run the classifier first
                    case "-impure":         // and get results for how well the entire pipeline works up to
                        _isPure = false;    // the grouping step.
                        break;
                }
            }
        }

        /// <summary>
        /// Determine whether this stage is pure (whether it classifies first, or assumes perfect classification)
        /// </summary>
        public bool IsPure
        {
            get { return _isPure; }
            set { _isPure = value; }
        }

        /// <summary>
        /// Initializes the pipeline after arguments have been processed.
        /// </summary>
        public override void start()
        {
            if (_isPure)
            {
                Console.WriteLine("Grouping test will be pure");
            }
            else
            {
                Console.WriteLine("Grouping test will be impure");
                _pipeline.addStep(RecognitionPipeline.createDefaultClassifier());
            }
            _pipeline.addStep(RecognitionPipeline.createDefaultGrouper());
        }

        /// <summary>
        /// Run the test on a single sketch.
        /// </summary>
        /// <param name="sketch">the sketch to group</param>
        /// <param name="filename">the name of the file being tested</param>
		public override void run(Sketch.Sketch sketch, string filename)
        {
            Sketch.Sketch original = sketch.Clone();

            // Run the grouper!
            if (!_isPure)
                sketch.RemoveLabels();
            sketch.resetShapes();
            _pipeline.process(sketch);

            // Evaluate the grouper!
            List<Sketch.Shape> result_groups = new List<Sketch.Shape> ( sketch.ShapesL ) ;
            foreach (Sketch.Shape shape in sketch.ShapesL)
                if (shape.Classification == new ShapeType().Classification)
                    result_groups.Remove(shape);

            List<Sketch.Shape> correct_groups = new List<Sketch.Shape> ( original.ShapesL );
            foreach (Sketch.Shape shape in original.ShapesL)
                if (shape.Classification == new ShapeType().Classification)
                    correct_groups.Remove(shape);

            int num_result_groups = result_groups.Count;
            int num_correct_groups = correct_groups.Count;

            int num_found_correctly = 0;
            foreach (Sketch.Shape group in correct_groups)
	        {
                // the grouper doesn't group things that have unknown types. like label boxes. those are a thing of the past.
                if (group.Type == new ShapeType()) 
                    num_correct_groups--;
                if (result_groups.Exists(delegate(Sketch.Shape s) { return s.GeometricEquals(group); }))
                    num_found_correctly++;
            }

            Console.WriteLine("   --> Found " + num_result_groups + " groups; should have found " + num_correct_groups);
            Console.WriteLine("   --> Correctly found " + num_found_correctly + "/" + num_correct_groups);

            // Assemble the results!

            _results.addResult(new string[] {
                filename,
                "" + num_correct_groups,
                "" + num_result_groups,
                "" + num_found_correctly
            });
		}

        /// <summary>
        /// Write the results to a file.
        /// </summary>
        /// <param name="tw">an open handle to the file</param>
        /// <param name="path">the folder the file is in</param>
		public override void finalize(System.IO.TextWriter tw, string path)
		{
            if (_isPure)
                tw.WriteLine("Grouping test was pure.");
            else
                tw.WriteLine("Grouping test was NOT pure.");

            // Writes a CSV (comma-separated values) file which can be read by Excel.
            _results.writeCSV(tw);
        }

    }
}
