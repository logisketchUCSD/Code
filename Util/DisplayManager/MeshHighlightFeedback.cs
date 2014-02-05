using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Drawing.Drawing2D;
using System.Windows.Controls;
using System.Windows.Ink;

using Sketch;
using SketchPanelLib;

namespace DisplayManager
{
    #region Feedback Mechanism

    /// <summary>
    /// Highlights the mesh surrounding the current element over which
    /// the stylus is hovering.  Requires a CircuitParser instance for
    /// Circuit Structure data.
    /// </summary>
    public class MeshHighlightingFeedback : FeedbackMechanism
    {

        #region Internals

        /// <summary>
        /// Distance threshold for firing highlight feedback
        /// </summary>
        public const int MeshHighlightDistanceThreshold = 200;

        /// <summary>
        /// Thickening factor.  Highlighted strokes are thickened by
        /// this factor, where 1.0 = 0% thickening or 1.5 = 50% 
        /// thickening.
        /// </summary>
        public const float MeshHighlightingThickeningFactor = 2F;

        /// <summary>
        /// The strokes that are currently highlighted
        /// </summary>
        private HashSet<System.Windows.Ink.Stroke> highlightedStrokes;

        /// <summary>
        /// True iff highlighting is enabled
        /// </summary>
        private bool highlightingEnabled = true;

        /// <summary>
        /// The shape the current highlighting is centered around.
        /// </summary>
        private Shape currentHighlightedShape;

        #endregion

        #region Constructors and Intialization

        /// <summary>
        /// Constructors
        /// </summary>
        public MeshHighlightingFeedback()
            : base() { Clear(); }

        public MeshHighlightingFeedback(ref SketchPanel parentPanel)
            : base(ref parentPanel) { Clear(); }

        /// <summary>
        /// <see cref="FeedbackMechanism.SubscribeToPanel"/>
        /// </summary>
        public override void SubscribeToPanel(ref SketchPanel parentPanel)
        {
            base.SubscribeToPanel(ref parentPanel);

            SubscribeToPanel();
        }

        private void SubscribeToPanel()
        {
            if (subscribed) return;
            subscribed = true;

            sketchPanel.InkCanvas.StylusInAirMove += new System.Windows.Input.StylusEventHandler(InkCanvas_StylusMove);
            sketchPanel.SketchFileLoaded += new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
        }

        /// <summary>
        /// <see cref="FeedbackMechanism.UnsubscribeFromPanel"/>
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            if (!subscribed) return;
            subscribed = false;

            Clear();

            sketchPanel.InkCanvas.StylusInAirMove -= new System.Windows.Input.StylusEventHandler(InkCanvas_StylusMove);
            sketchPanel.SketchFileLoaded -= new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
        }

        /// <summary>
        /// Clears/initializes the bookkeeping data structures
        /// </summary>
        protected void Clear()
        {
            if (highlightedStrokes == null)
                highlightedStrokes = new HashSet<System.Windows.Ink.Stroke>();
            unHighlightStrokes();
        }

        #endregion

        #region Ink and Panel Hooks

        /// <summary>
        /// Unhighlights strokes and clears this feedback mechanism whenever a file is loaded
        /// </summary>
        void sketchPanel_SketchFileLoaded()
        {
            Clear();
        }

        /// <summary>
        /// Invokes Mesh Highlighting depending on mouse position.  
        /// 
        /// Main function for invoking this feedback mechanism.
        /// </summary>
        private void InkCanvas_StylusMove(object sender, System.Windows.Input.StylusEventArgs e)
        {
            if (!highlightingEnabled)
                return; // Do not highlight

            Shape shapeToHighlight = sketchPanel.Sketch.shapeAtPoint(e.GetPosition(sketchPanel.InkCanvas).X, e.GetPosition(sketchPanel.InkCanvas).Y, 50);
            if (shapeToHighlight == currentHighlightedShape) return;

            unHighlightStrokes();

            highlightShape(shapeToHighlight, true);
            currentHighlightedShape = shapeToHighlight;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Enables or disables mesh highlighting
        /// </summary>
        public bool HighlightingEnabled
        {
            get
            {
                return highlightingEnabled;
            }

            set
            {
                if (value)
                {
                    highlightingEnabled = true;
                }
                else
                {
                    unHighlightStrokes();
                    highlightingEnabled = false;
                }
            }
        }

        #endregion

        #region Mesh Highlighting Helper Functions

        /// <summary>
        /// Highlights a single shape and its connected shapes if HighlightConnected is set to true
        /// </summary>
        /// <param name="shape">The shape to highlight</param>
        /// <param name="HighlightConnected">Whether or not this shape's connected shapes should also be highlighted</param>
        private void highlightShape(Shape shape, bool HighlightConnected)        
        {
            if (shape == null)
                return;

            if (HighlightConnected)
                foreach (Shape connected in shape.ConnectedShapes)
                    highlightShape(connected, false);

            if (!shape.AlreadyLabeled) return;

            foreach (Substroke sub in shape.Substrokes)
                highlightStroke(sub);
        }

        /// <summary>
        /// Highlights the single Ink stroke that corresponds to the 
        /// given substroke.
        /// 
        /// Helper Function.
        /// </summary>
        /// <param name="substroke">The substroke to highlight.</param>
        private void highlightStroke(Substroke substroke)
        {
            // Get corresponding Ink stroke
            System.Windows.Ink.Stroke iStroke = sketchPanel.InkSketch.GetInkStrokeBySubstroke(substroke);

            if (iStroke == null || highlightedStrokes.Contains(iStroke))
                return; // Can't highlight this stroke

            iStroke.DrawingAttributes.Width *= MeshHighlightingThickeningFactor;
            iStroke.DrawingAttributes.Height *= MeshHighlightingThickeningFactor;

            // Update records
            highlightedStrokes.Add(iStroke);
        }

        /// <summary>
        /// Unhighlights all strokes that are currently highlighted.
        /// 
        /// Main function for unhighlighting strokes.
        /// </summary>
        private void unHighlightStrokes()
        {
            foreach (System.Windows.Ink.Stroke iStroke in highlightedStrokes)
                unHighlightStroke(iStroke);

            currentHighlightedShape = null;
            highlightedStrokes.Clear();
        }

        /// <summary>
        /// Unhighlights a single Ink stroke
        /// </summary>
        /// <param name="iStroke">The stroke to unhighlight</param>
        private void unHighlightStroke(System.Windows.Ink.Stroke iStroke)
        {
            if (iStroke == null || iStroke.DrawingAttributes == null)
                return; // Can't unhighlight this stroke

            iStroke.DrawingAttributes.Width /=  MeshHighlightingThickeningFactor;
            iStroke.DrawingAttributes.Height /= MeshHighlightingThickeningFactor;
        }

        #endregion
    }

    #endregion
}
