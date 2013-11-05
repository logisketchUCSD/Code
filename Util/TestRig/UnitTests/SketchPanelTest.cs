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
    public class SketchPanelTest
    {
        private SketchPanelLib.SketchPanel panel;

        [TestInitialize]
        public void Initialize()
        {
            panel = newSketchPanel();
        }

        [TestMethod]
        public void TestSetup()
        {
            Assert.IsNotNull(panel, "panel should have been set up");
        }

        #region Helpers
        public static SketchPanelLib.SketchPanel newSketchPanel()
        {
            CommandManagement.CommandManager commandManager = new CommandManagement.CommandManager();
            SketchPanelLib.SketchPanel panel = new SketchPanelLib.SketchPanel(commandManager, InkCanvasSketchTest.newInkCanvasSketch());
            return panel;
        }
        #endregion

    }
}
