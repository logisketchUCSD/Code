using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using Microsoft.Ink;

using SketchPanelLib;
using Sketch;
using CommandManagement;

namespace SelectionManager
{
    public class HoverCrossManager : SelectionManager 
    {
        #region Internals

        private HoverCrossSelect selector;      // Handles adding and removing strokes from selection

        #endregion

        #region Constructor

        public HoverCrossManager(ref CommandManager commandManager, ref SketchPanel SP) 
            : base(ref commandManager, ref SP)
        {
            // Set the CommandManager, DomainInfo and SketchPanel
            this.commandManager = commandManager;

            this.sketchPanel = SP;

            this.selector = new HoverCrossSelect( ref sketchPanel );

            this.EditMenu = new EditMenu.EditMenu(ref this.sketchPanel, this.commandManager);
        }

        #endregion

        #region Subscription to Panel

        /// <summary>
        /// Subscribes to SketchPanel.  Subscibe only when tool is selected.
        /// <see cref="SketchPanelLib.SketchPanelListener.SubscribeToPanel()"/>
        /// </summary>
        public override void SubscribeToPanel()
        {
            // Subscribe our selector to the panel
            selector.SubscribeToPanel();
            sketchPanel.InkCanvas.SelectionChanged += new EventHandler(InkCanvas_SelectionChanged);
        }

        /// <summary>
        /// Unsubscribes from SketchPanel
        /// <see cref="SketchPanelLib.SketchPanelListener.UnSubscribeToPanel()"/>
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            // Clear selection and unsubscribe from the panel
            selector.UnsubscribeFromPanel();
            sketchPanel.InkCanvas.SelectionChanged -= new EventHandler(InkCanvas_SelectionChanged);
        }

        #endregion

        #region Displaying Menu

        // Displays the context menu when something has been selected
        public void InkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            System.Console.WriteLine("Selection has been changed " + sketchPanel.InkCanvas.GetSelectedStrokes().Count);
            if (sketchPanel.InkCanvas.GetSelectedStrokes().Count > 0)
            {
                this.EditMenu.removeMenu();
                this.EditMenu.displayContextMenu(sketchPanel.InkCanvas.GetSelectionBounds().BottomRight);
            }
            else
                this.EditMenu.removeMenu();
        }

        //Displays the context menu when called from outside explicitly
        public override void DisplayContextMenu(int x, int y)
        {
            EditMenu.displayContextMenu(new System.Windows.Point(x,y));
        }


        //Removes context menu when called from outside explicitly
        public override void RemoveMenu()
        {
            EditMenu.removeMenu();
        }

        #endregion
    }
}
