using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TestRig.Utils;
using Sketch;
using Featurefy;

namespace TestRig
{

    /// <summary>
    /// Writes the pairwise feature information from a sketch to a file.
    /// </summary>
    class FeatureSketchStage : ProcessStage
    {

        private List<string> _featuresSingle;
        private List<string> _featuresPair;

        private Table _resultsSingle;
        private Table _resultsPairs;

        public FeatureSketchStage()
        {

            name = "Feature Sketch Stage";
            shortname = "fsk";
            outputFiletype = ".csv";

            Sketch.Sketch sketch = new Sketch.Sketch();
            FeatureSketch fsketch = FeatureSketch.MakeFeatureSketch(new Sketch.Project(sketch));

            Dictionary<string, bool> featuresSingleEnabled = fsketch.FeatureListSingle;
            _featuresSingle = Data.Utils.filter(featuresSingleEnabled.Keys, delegate(string s) { return featuresSingleEnabled[s]; });

            Dictionary<string, bool> featuresPairEnabled = fsketch.FeatureListPair;
            _featuresPair = Data.Utils.filter(featuresPairEnabled.Keys, delegate(string s) { return featuresPairEnabled[s]; });

            _resultsSingle = new Table(_featuresSingle);
            _resultsPairs = new Table(_featuresPair);

        }

        public override void run(Sketch.Sketch sketch, string filename)
        {

            FeatureSketch fsketch = FeatureSketch.MakeFeatureSketch(new Sketch.Project(sketch));

            Dictionary<Substroke, string> classifications = new Dictionary<Substroke, string>();
            foreach (Substroke substroke in sketch.Substrokes)
                classifications.Add(substroke, substroke.Classification);

            Dictionary<string, Dictionary<FeatureStrokePair, double[]>> pair2values;

            pair2values = fsketch.GetValuesPairwise(classifications);

            foreach (KeyValuePair<string, Dictionary<FeatureStrokePair, double[]>> pair in pair2values)
            {
                string classification = pair.Key;
                Dictionary<FeatureStrokePair, double[]> features = pair.Value;

                Console.WriteLine(classification + ":");

                foreach (KeyValuePair<FeatureStrokePair, double[]> pair2 in features)
                {

                    Substroke stroke1 = pair2.Key.Item1;
                    Substroke stroke2 = pair2.Key.Item2;
                    double[] featureValues = pair2.Value;

                    _resultsPairs.addResult(featureValues);

                }

            }

        }

        public override void finalize(System.IO.TextWriter handle, string path)
        {
            _resultsPairs.writeCSV(handle);
        }

    }
}
