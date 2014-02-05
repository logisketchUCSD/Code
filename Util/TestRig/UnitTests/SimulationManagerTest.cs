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
    public class SimulationManagerTest
    {
        private SimulationManager.SimulationManager simulationManager;
        private InkToSketchWPF.InkCanvasSketch inkCanvasSketch;
        private SketchPanelLib.SketchPanel panel;

        [TestInitialize]
        public void Initialize()
        {
            panel = SketchPanelTest.newSketchPanel();
            inkCanvasSketch = panel.InkSketch;
            simulationManager = new SimulationManager.SimulationManager(ref panel);
        }

        [TestMethod]
        public void TestSetup()
        {
            Assert.IsNotNull(simulationManager);
        }

        [TestMethod]
        public void TestLogiSimString()
        {
        }
    }
}
