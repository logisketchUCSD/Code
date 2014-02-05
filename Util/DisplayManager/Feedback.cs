using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Collections;

using Sketch;
using SketchPanelLib;


namespace DisplayManager
{
    #region Feedback Framework

    /// <summary>
    /// A FeedbackMechanism applies a visual transformation to a sketch.  A 
    /// feedback mechanism might recolor Ink strokes, replace Ink strokes with symbols,
    /// label Ink strokes with textual labels, etc.  A FeedbackMechanism is similar to a
    /// Visitor (see Design Patterns).
    /// 
    /// This generic (abstract) Feedback Mechanism that subscribes to a SketchPanel and 
    /// (possibly) loads a domain mapping Sketch symbols to colors.  Feedback can be triggered
    /// abitrarily, but usually FeedbackMechanisms are sensative to the several events that 
    /// SketchPanels publish to (such as the Zoom Event or the File Loaded Event).  Subclassers
    /// should implement the SubscribeToPanel() and UnsubscribeFromPanel() methods in order
    /// to hook into these events.
    /// 
    /// Feedback should run in the same thread as the front end.  
    /// </summary>
    public abstract class FeedbackMechanism : SketchPanelSubscriber
    {

        protected bool subscribed = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FeedbackMechanism()
            : base()
        {
        }

        /// <summary>
        /// Constructor.  Subscribes to parent panel.
        /// </summary>
        /// <param name="parentPanel">the parent SketchPanel</param>
        public FeedbackMechanism(ref SketchPanel parentPanel)
            : this(ref parentPanel, null)
        {
        }

        /// <summary>
        /// Constructor.  Subscribes to parent panel events and 
        /// loads target domain file.
        /// </summary>
        /// <param name="parentPanel"></param>
        public FeedbackMechanism(ref SketchPanel parentPanel, string domainFilePath)
            : base(ref parentPanel)
        {
            // nothing to do!
        }


        public bool Subscribed
        {
            get
            {
                return subscribed;
            }
        }
    }

    #endregion

    #region Sample (Stroke Coloring) Feedback Mechanism

    /// <summary>
    /// Colors Ink strokes that correspond to Sketch
    /// symbols using a domain file that maps between symbol
    /// names (labels) and colors.
    /// </summary>
    public class ColorFeedback : FeedbackMechanism
    {
        /// <summary>
        /// Constructors
        /// </summary>
        public ColorFeedback()
            : base() {}

        public ColorFeedback(ref SketchPanel parentPanel)
            : base(ref parentPanel) {}

        public ColorFeedback(ref SketchPanel parentPanel, string domainFilePath)
            : base(ref parentPanel, domainFilePath) {}

        /// <summary>
        /// <see cref="SketchPanelSubscriber.SubscribeToPanel"/>
        /// </summary>
        public override void SubscribeToPanel(ref SketchPanel parentPanel)
        {
            if (subscribed) return;
            base.SubscribeToPanel(ref parentPanel);
  
            sketchPanel.SketchFileLoaded += new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
            subscribed = true;
        }

        /// <summary>
        /// <see cref="SketchPanelSubscriber.UnsubscribeFromPanel"/>
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            if (!subscribed) return;
            sketchPanel.SketchFileLoaded -= new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
            subscribed = false;
        }

        /// <summary>
        /// DEBUG TEMP HACK
        /// </summary>
        //void InkPicture_Resize(object sender, EventArgs e)
        //{
        //    colorStrokes();
        //}

        /// <summary>
        /// Attempts to color the Ink strokes using the domain when a file is loaded
        /// </summary>
        private void sketchPanel_SketchFileLoaded()
        {
            colorStrokes();
        }

        /// <summary>
        /// Colors Ink strokes in the parent SketchPanel using the domain.
        /// </summary>
        protected virtual void colorStrokes()
        {
            // Attempt to color all Ink strokes
            foreach (System.Windows.Ink.Stroke iStroke in sketchPanel.InkCanvas.Strokes)
            {
                // Get corresponding label
                Substroke sStroke = sketchPanel.InkSketch.GetSketchSubstrokeByInkId((String)iStroke.GetPropertyData(idGuid));
                Domain.ShapeType label = sStroke.Type;
                //Console.WriteLine("String label: " + sStroke.FirstLabel+" Color: "+mDomain.GetColor(label));

                // Color the stroke; if the stroke is not in the domain, then we
                // color the stroke black.
                iStroke.DrawingAttributes.Color = label.Color;
            }

            // Request repaint
            sketchPanel.InkCanvas.InvalidateVisual();
            sketchPanel.InkCanvas.UpdateLayout();
        }
    }

    #endregion
}
