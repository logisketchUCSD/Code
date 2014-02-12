using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using ConverterXML;
using Sketch;
using Featurefy;
using System.Windows.Ink;
using System.Windows.Input;

namespace InkToSketchWPF
{
    /// <summary>
    /// Binds a Microsoft Ink object and a HMC Sketch object together.
    /// July 2011: We primarily work with instances of its subclass, InkCanvasSketch.
    /// </summary>
    public class InkSketch
    {
        #region Internals


        public System.Windows.Ink.Stroke NullStroke = null;

        protected Featurefy.FeatureSketch mFeatureSketch;

        /// <summary>
        /// Ink Stroke IDs to Sketch Substrokes
        /// </summary>
        protected Dictionary<String, Guid?> ink2sketchStr;

        /// <summary>
        /// Sketch Substrokes to Ink Stroke IDs
        /// </summary>
        protected Dictionary<Guid?, String> sketchStr2ink;

        /// <summary>
        /// Map of Sketch Substroke IDs to actual Substrokes
        /// </summary>
        protected Dictionary<Guid?, Substroke> substrokeIdMap;

        /// <summary>
        /// True iff the InkSketch is currently recording Ink strokes
        /// to the sketch.
        /// </summary>
        protected bool recording;

        /// <summary>
        /// Default units for sketch
        /// </summary>
        public static string DefaultSketchUnits = "pixel";

        /// <summary>
        ///  The Guid string that will be used to add time data to each stroke
        /// </summary>
        protected const string dtGuidString = "03457307-3475-3450-3035-640435034540";

        /// <summary>
        ///  The Guid string that will be used to identify the stroke
        /// </summary>
        protected const string idGuidString = "03457307-3475-3450-3035-640435034542";

        /// <summary>
        /// The Guid for dtGuidString
        /// </summary>
        protected Guid dtGuid;

        /// <summary>
        /// The Guid for idGuidString
        /// </summary>
        protected Guid idGuid;

        #endregion



    }
}
