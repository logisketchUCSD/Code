using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RecognitionManager;
using RecognitionInterfaces;
using Refiner;
using Domain;

namespace TestRig
{
    class PipelineStage : ProcessStage
    {

        private List<RecognitionPipeline> _pipelines;
        private Dictionary<RecognitionPipeline, Utils.Table> _tables;
        private Dictionary<RecognitionPipeline, Utils.ConfusionMatrix<string>> _classificationConfusion;
        private Dictionary<RecognitionPipeline, Utils.ConfusionMatrix<ShapeType>> _recognitionConfusion;
        private Dictionary<RecognitionPipeline, double> _cls;
        private Dictionary<RecognitionPipeline, double> _grp;
        private Dictionary<RecognitionPipeline, double> _rec;
        private Dictionary<RecognitionPipeline, double> _realRec;
        private Dictionary<RecognitionPipeline, double> _srec;
        private int _numTests;

        public PipelineStage()
        {
            name = "Pipeline";
            shortname = "pip";
            outputFiletype = ".csv"; // comma-separated values; readable by Excel

            _numTests = 0;
            _pipelines = new List<RecognitionPipeline>();
            _tables = new Dictionary<RecognitionPipeline, Utils.Table>();
            _classificationConfusion = new Dictionary<RecognitionPipeline, Utils.ConfusionMatrix<string>>();
            _recognitionConfusion = new Dictionary<RecognitionPipeline, Utils.ConfusionMatrix<ShapeType>>();
            _cls = new Dictionary<RecognitionPipeline, double>();
            _grp = new Dictionary<RecognitionPipeline, double>();
            _rec = new Dictionary<RecognitionPipeline, double>();
            _realRec = new Dictionary<RecognitionPipeline, double>();
            _srec = new Dictionary<RecognitionPipeline, double>();
        }

        private static IRecognitionStep getStep(string name)
        {
            Console.WriteLine("Loading stage: " + name);
            switch (name)
            {
                case "cls": return RecognitionPipeline.createDefaultClassifier();
                case "grp": return RecognitionPipeline.createDefaultGrouper();
                case "rec": return RecognitionPipeline.createDefaultRecognizer();
                case "con": return RecognitionPipeline.createDefaultConnector();
                case "ref": return RecognitionPipeline.createDefaultRefiner();
                case "ref_ctx": return new ContextRefiner(ContextDomain.CircuitDomain.GetInstance(), RecognitionPipeline.createDefaultRecognizer());
                case "ref_cctx": return new CarefulContextRefiner(ContextDomain.CircuitDomain.GetInstance(), RecognitionPipeline.createDefaultRecognizer());
                case "ref_steal": return new StrokeStealRefiner(RecognitionPipeline.createDefaultRecognizer(), RecognitionPipeline.createDefaultConnector());
                case "ref_shed": return new StrokeShedRefiner(RecognitionPipeline.createDefaultRecognizer(), RecognitionPipeline.createDefaultConnector());
                case "ref_search":
                    var producer = new CircuitSketchModificationProducer(
                        RecognitionPipeline.createDefaultClassifier(),
                        RecognitionPipeline.createDefaultGateRecognizer(),
                        RecognitionPipeline.createDefaultConnector());
                    return new SearchRefiner(producer);
            }
            TestRig.ExitWithError("No step exists called '"+name+"'! Valid steps are: cls, grp, rec, con, ref, ref_ctx, ref_cctx, ref_steal, ref_shed, ref_search");
            return null;
        }

        private void prepForPipeline(RecognitionPipeline pipeline)
        {
            _pipelines.Add(pipeline);
            Utils.Table table = new Utils.Table(new string[] { 
                "File", 
                "Classification Quality", 
                "Grouping Quality", 
                "Recognition Quality",
                "Real Recognition Quality",
                "Per-substroke Recognition Quality" 
            });
            _tables.Add(pipeline, table);
            _cls.Add(pipeline, 0);
            _grp.Add(pipeline, 0);
            _rec.Add(pipeline, 0);
            _realRec.Add(pipeline, 0);
            _srec.Add(pipeline, 0);
        }

        public override void processArgs(string[] args)
        {
            RecognitionPipeline pipeline = new RecognitionPipeline();

            foreach (string arg in args)
            {
                if (arg == "|")
                {
                    prepForPipeline(pipeline);
                    pipeline = new RecognitionPipeline();
                    continue;
                }

                IRecognitionStep step = getStep(arg);
                if (step != null)
                    pipeline.addStep(step);
                else
                    Console.WriteLine("WARNING: Unused argment for pipeline stage: " + arg);
            }

            prepForPipeline(pipeline);
        }

        public override void start()
        {
            foreach (RecognitionPipeline pipeline in _pipelines)
            {
                _classificationConfusion.Add(pipeline, new Utils.ConfusionMatrix<string>());
                _recognitionConfusion.Add(pipeline, new Utils.ConfusionMatrix<ShapeType>());
            }
        }

        private int pipelineId(RecognitionPipeline pipeline)
        {
            return _pipelines.IndexOf(pipeline);
        }

        public override void run(Sketch.Sketch original, string filename)
        {
            _numTests++;

            foreach (RecognitionPipeline pipeline in _pipelines)
            {
                Sketch.Sketch sketch = original.Clone();
                sketch.RemoveLabelsAndGroups();
                pipeline.process(sketch);

                string sketchFilename = "sketch-" + _numTests + " pipeline-" + pipelineId(pipeline) + ".xml";
                Console.WriteLine("Saving sketch to " + sketchFilename);
                ConverterXML.SaveToXML saver = new ConverterXML.SaveToXML(new Sketch.Project(sketch));
                saver.WriteXML(sketchFilename);

                Utils.SketchComparer comparer = new Utils.SketchComparer(original, sketch);

                double cls = comparer.ClassificationQuality * 100;
                /* This grouping stat is the 
                 *  (# groups found correctly / # correct groups)
                 *  Where # correct groups is the number of groups in
                 *  the correctly recognized sketch.
                 */
                double grp = comparer.GroupingQuality * 100;
                /*This recognition stat is the 
                 * (# groups recognized correctly / # groups found correctly)
                 */
                double rec = comparer.RecognitionQuality * 100;
                /*This is the recognition stat that we really care about,
                 * because it portrays the recognition rate out of all the
                 * groups in the sketch
                 */
                double realRec = comparer.RecognitionQuality * comparer.GroupingQuality *100;
                double srec = comparer.SubstrokeRecognitionQuality * 100;

                Console.WriteLine("--------- Results ---------");
                Console.WriteLine("    Classification: " + cls + "%");
                Console.WriteLine("    Grouping:       " + grp + "%");
                Console.WriteLine("    Recognition out of correctly grouped groups:    " + rec + "%");
                Console.WriteLine("    Recognition out of all the groups:    " + realRec + "%");
                Console.WriteLine("    Substroke Rec.: " + srec + "%");

                _tables[pipeline].addResult(new string[] { 
                    filename, 
                    "" + cls, 
                    "" + grp, 
                    "" + rec,
                    "" + realRec,
                    "" + srec 
                });

                _classificationConfusion[pipeline].AddResults(comparer.ClassificationConfusion);
                _recognitionConfusion[pipeline].AddResults(comparer.RecognitionConfusion);

                _cls[pipeline] += cls;
                _grp[pipeline] += grp;
                _rec[pipeline] += rec;
                _realRec[pipeline] += realRec;
                _srec[pipeline] += srec;
            }
        }

        public override void finalize(TextWriter handle, string path)
        {
            Utils.Table overall = new Utils.Table(new string[] { 
                "Pipeline", 
                "Classification Quality", 
                "Grouping Quality", 
                "Recognition Quality",
                "Real Recognition Quality",
                "Per-substroke Recognition Quality"
            });
            handle.WriteLine("Overall results");
            foreach (RecognitionPipeline pipeline in _pipelines)
            {
                overall.addResult(new string[] {
                    "" + pipelineId(pipeline),
                    "" + _cls[pipeline] / _numTests,
                    "" + _grp[pipeline] / _numTests,
                    "" + _rec[pipeline] / _numTests,
                    "" + _realRec[pipeline] / _numTests,
                    "" + _srec[pipeline] / _numTests
                });
            }
            overall.writeCSV(handle);
            handle.WriteLine();

            foreach (RecognitionPipeline pipeline in _pipelines)
            {
                handle.WriteLine("Pipeline " + pipelineId(pipeline));
                _classificationConfusion[pipeline].writeCSV(handle);
                _recognitionConfusion[pipeline].writeCSV(handle);
                _tables[pipeline].writeCSV(handle);
                handle.WriteLine();
            }
        }

    }
}
