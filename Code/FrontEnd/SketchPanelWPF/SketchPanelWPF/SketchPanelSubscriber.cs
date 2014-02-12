using System;
using System.Collections.Generic;
using System.Text;

namespace SketchPanelLib
{
    /// <summary>
    /// A(n abstract) module that subscribes to SketchPanel events.  In reference to the Observer Pattern,
    /// the SketchPanelSubscriber is similar to an Observer where the SketchPanel is similar to a Subject:
    ///    * http://en.wikipedia.org/wiki/Observer_pattern
    /// A SketchPanelSubscriber serves as a base class for building Recognizers, Feedback Mechanisms, and 
    /// other modules that want to listen to SketchPanel events (e.g. sketching tools).
    /// 
    /// Lifecycle:
    ///    * On instantiation, call one of the constructors and use SubscribeToPanel() if necessary.
    ///    * Call UnsubscribeFromPanel() to disconnect this SketchPanelListener from 
    ///      SketchPanel events (stop listening).
    ///    * Call UnsubscribeFromPanel() before calling SketchPanel.InitPanel() or clearing SketchPanel
    ///      in any way that might destroy the event subscriptions contained within SketchPanel.  For 
    ///      example, if a SketchPanelListener subscribes to InkPicture events within a SketchPanel,
    ///      then deleting/reinitializing the InkPicture instance delete the SketchPanelListener's
    ///      event subscriptions in possibly unexpected ways.
    ///    * (Optional?) Call UnsubscribeFromPanel() before explicitely deleting SketchPanelListener.
    /// </summary>
    public abstract class SketchPanelSubscriber
    {
        /// <summary>
        /// The parent SketchPanel to which this subscriber listens
        /// </summary>
        protected SketchPanel sketchPanel;

        /// <summary>
        /// idGuid for going between InkCanvas strokes and InkSketch strokes
        /// </summary>
        protected Guid idGuid;

        /// <summary>
        /// Default Constructor.  Does nothing but initializes this SketchPanelSubscriber.
        /// </summary>
        public SketchPanelSubscriber()
        {
        }

        /// <summary>
        /// Constructor.  Subscribes to the parentPanel and 
        /// initializes this SketchPanelSubscriber.
        /// </summary>
        /// <param name="parentPanel">The parent panel to subscribe to</param>
        public SketchPanelSubscriber(ref SketchPanel parentPanel)
        {
            sketchPanel = parentPanel;
            this.idGuid = parentPanel.InkSketch.IDGuid;
            SubscribeToPanel(ref parentPanel);
        }

        /// <summary>
        /// Subscribes to relevant events on parent SketchPanel (e.g. stroke add/delete events,
        /// zoom events, file load events, recognition result events, etc.)
        /// </summary>
        /// <param name="parentPanel">The parent panel to which we want to attach</param>
        public virtual void SubscribeToPanel(ref SketchPanel parentPanel)
        {
            this.sketchPanel = parentPanel;
        }

        /// <summary>
        /// Unsubscribes to relevant events on current parent SketchPanel.
        /// </summary>
        public abstract void UnsubscribeFromPanel();
    }
}
