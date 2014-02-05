/*
 * Alexa Keizur and Alice Paul
 * Summer 2010
 * 
 * Selection template for built-in lasso selection
 */
using System;
using System.Collections.Generic;
using System.Text;
using SketchPanelLib;
using System.Drawing;
using Microsoft.Ink;

namespace SelectionManager
{
    public class SelectorTemplate
    {

        #region Internals

        protected SketchPanel sketchPanel;     // The SketchPanel that will display and record our selected Strokes
        
        public bool selectionMade;          // Once a selection is made no more are allowed

        public bool makingSelection;         // When the stylus goes down, a selection starts
        
        public bool subscribed;            // We don't want to subscribe or unsubscribe more than we need to

        #endregion

        #region Constructor

        public SelectorTemplate( ref SketchPanel sp)
        {
            this.sketchPanel = sp;
            this.selectionMade = false;
        }

        public SelectorTemplate()
        {
            // Do nothing
        }

        #endregion

        #region Subscription

        /// <summary>
        /// Default will change the panel to editing mode
        /// Overrides will use events in the panel (mouse up, mouse down, etc.) to determine selection
        /// </summary>
        public virtual void SubscribeToPanel()
        {
            if (subscribed) return;
            subscribed = true;

            sketchPanel.InkCanvas.StylusDown += new System.Windows.Input.StylusDownEventHandler(InkCanvas_StylusDown);
            sketchPanel.InkCanvas.StylusUp += new System.Windows.Input.StylusEventHandler(InkCanvas_StylusUp);
            sketchPanel.InkCanvas.StylusLeave += new System.Windows.Input.StylusEventHandler(InkCanvas_StylusUp);

            sketchPanel.EditingMode = System.Windows.Controls.InkCanvasEditingMode.Select;
            sketchPanel.EditingModeInverted = System.Windows.Controls.InkCanvasEditingMode.None;

            selectionMade = false;
            makingSelection = false;
        }

        /// <summary>
        /// Clear selection and unsubscribe any events
        /// </summary>
        public virtual void UnsubscribeFromPanel()
        {
            if (sketchPanel == null)
                return;

            sketchPanel.EnableDrawing();

            if (!subscribed)
                return;

            subscribed = false;

            Clear();
            sketchPanel.InkCanvas.StylusDown -= new System.Windows.Input.StylusDownEventHandler(InkCanvas_StylusDown);
            sketchPanel.InkCanvas.StylusUp -= new System.Windows.Input.StylusEventHandler(InkCanvas_StylusUp);
            sketchPanel.InkCanvas.StylusLeave -= new System.Windows.Input.StylusEventHandler(InkCanvas_StylusUp);

            selectionMade = false;
        }

        #endregion

        #region Selection Tools

        /// <summary>
        /// Clears the selection of strokes
        /// </summary>
        public void Clear()
        {
            sketchPanel.InkCanvas.Select(new System.Windows.Ink.StrokeCollection());
        }

        // TO DO: FIGURE OUT SCALES AND ROTATES IN STROKECOLLECTIONS

        /// <summary>
        /// Rotates the strokes
        /// </summary>
        /// <param name="degrees">Degrees to rotate strokes</param>
        /// <param name="point">Point to rotate strokes around</param>
        public void Rotate(float degrees, System.Drawing.Point point)
        {
            //sketchPanel.InkPicture.GetSelectedStrokes().Transform = 
        }

        /// <summary>
        /// Scales the Strokes collection in the X and Y dimensions
        /// </summary>
        /// <param name="scaleX">Scale factor for the width</param>
        /// <param name="scaleY">Scale factor for the height</param>

        public void Scale(float scaleX, float scaleY)
        {
            //sketchPanel.InkPicture.GetSelectedStrokes().Scale(scaleX, scaleY);
        }

        /// <summary>
        /// Move the Strokes collection
        /// </summary>
        /// <param name="offsetX">Amount to move x coordinate of strokes</param>
        /// <param name="offsetY">Amount to move y coordinate of strokes</param>
        public void Move(float offsetX, float offsetY)
        {
            //sketchPanel.InkPicture.Selection.Move(offsetX, offsetY);
        }

        #endregion

        #region Events

        protected virtual void InkCanvas_StylusDown(object sender, System.Windows.Input.StylusDownEventArgs e)
        {
            BeginSelection(e.GetPosition(sketchPanel.InkCanvas));
        }

        protected virtual void InkCanvas_StylusUp(object sender, System.Windows.Input.StylusEventArgs e)
        {
            EndSelection(e.GetPosition(sketchPanel.InkCanvas));
        }

        public virtual void BeginSelection(System.Windows.Point point)
        {
            this.makingSelection = true;
        }

        public virtual void EndSelection(System.Windows.Point point)
        {
            if (this.makingSelection)
                this.selectionMade = true;
        }

        #endregion

        #region Getters/Setters

        /// <summary>
        /// Gets ans sets the ink canvas's selected strokes
        /// </summary>
        public System.Windows.Ink.StrokeCollection Selection
        {
            get
            {
                return sketchPanel.InkCanvas.GetSelectedStrokes();
            }
            set
            {
                sketchPanel.InkCanvas.Select(value);
            }
        }

        public System.Windows.Rect BoundingBox
        {
            get
            {
                if (sketchPanel.InkCanvas.GetSelectedStrokes().Count == 0)
                    return new System.Windows.Rect();
                return sketchPanel.InkCanvas.GetSelectionBounds();
            }
        }

        #endregion

        #region Adding/Removing Strokes

        public void Add(System.Windows.Ink.Stroke s)
        {
            sketchPanel.InkCanvas.GetSelectedStrokes().Add(s);
        }

        public void Add(System.Windows.Ink.StrokeCollection s)
        {
            sketchPanel.InkCanvas.GetSelectedStrokes().Add(s);
        }

        private void Remove(System.Windows.Ink.Stroke s)
        {
            sketchPanel.InkCanvas.GetSelectedStrokes().Remove(s);
        }

        private void Remove(System.Windows.Ink.StrokeCollection s)
        {
            sketchPanel.InkCanvas.GetSelectedStrokes().Remove(s);
        }

        #endregion
    }
}
