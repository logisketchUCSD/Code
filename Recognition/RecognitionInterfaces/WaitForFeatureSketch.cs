using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RecognitionInterfaces
{

    public class WaitForFeatureSketch : IRecognitionStep
    {

        public string ProgressString
        {
            get { return "Computing features"; }
        }

        public void process(Featurefy.FeatureSketch featureSketch)
        {
            featureSketch.WaitForAll();
        }

    }

}
