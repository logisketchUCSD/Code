using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]

    [DeploymentItem("settings.txt")]
    [DeploymentItem("Initialization Files\\FeatureListGroup.txt")]
    [DeploymentItem("Initialization Files\\FeatureListSingle.txt")]
    [DeploymentItem("CircuitDomain.txt")]
    [DeploymentItem("SubRecognizers", "SubRecognizers")]
    public class RecognitionManagerTest
    {
        private RecognitionManager.RecognitionManager recognitionManager;

        [TestInitialize]
        public void Initialize()
        {
            recognitionManager = newRecognitionManager();
        }

        [TestMethod]
        public void TestSetup()
        {
            Assert.IsNotNull(recognitionManager);
        }

        #region Helpers
        private RecognitionManager.RecognitionManager newRecognitionManager()
        {
            return new RecognitionManager.RecognitionManager();
        }
        #endregion
    }
}
