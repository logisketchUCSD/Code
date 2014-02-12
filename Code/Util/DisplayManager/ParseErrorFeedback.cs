using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Ink;

using SketchPanelLib;
using CircuitParser;
using Sketch;

namespace DisplayManager
{
    /// <summary>
    /// Highlights parse errors from the CircuitRec Recognizer.
    /// 
    /// Draws labels to highlight errors and highlights erroneous strokes as red
    /// when possible.
    /// </summary>
    public class ParseErrorFeedback : FeedbackMechanism
    {
        #region Internals

        /// <summary>
        /// True iff error highlighting is enabled.
        /// </summary>
        private bool highlightingEnabled;

        /// <summary>
        /// Points to list of parse errors in current
        /// CircuitRec result.  This mechanism highlights these
        /// errors.
        /// Not currently used.
        /// </summary>
        //private List<ParseError> errorList;

        /// <summary>
        /// Map of highlighted stroke IDs to unhighlighted (default) colors
        /// </summary>
        private Dictionary<String, Color> strokeId2Color;

        /// <summary>
        /// Map of ink Stroke Ids to Labels, used for deleting stroke/label pairs
        /// </summary>
        private Dictionary<String, System.Windows.Controls.Label> iStroke2Label;

        /// <summary>
        /// List of all error labels instantiated
        /// </summary>
        private List<System.Windows.Controls.Label> errorLabels;

        #endregion

        #region Constructors and Intialization

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ParseErrorFeedback()
            : base() 
        {
            highlightingEnabled = true;
        }

        /// <summary>
        /// Constructor. Subscribes to parentPanel.
        /// </summary>
        public ParseErrorFeedback(SketchPanel parentPanel)
            : base(parentPanel)
        {
            highlightingEnabled = true;
        }

        /// <summary>
        /// <see cref="FeedbackMechanism.SubscribeToPanel"/>
        /// </summary>
        public override void SubscribeToPanel(SketchPanel parentPanel)
        {
            sketchPanel = parentPanel;

            sketchPanel.ResultReceived += new RecognitionResultReceivedHandler(sketchPanel_ResultReceived);
            sketchPanel.SketchFileLoaded += new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
            sketchPanel.InkCanvas.StrokeErasing += new InkCanvasStrokeErasingEventHandler(InkPicture_StrokesDeleting);
            sketchPanel.InkTransformed += new InkTransformedEventHandler(sketchPanel_InkTransformed);
        }

        /// <summary>
        /// <see cref="FeedbackMechanism.UnsubscribeFromPanel"/>
        /// </summary>
        public override void UnsubscribeFromPanel()
        {
            sketchPanel.ResultReceived -= new RecognitionResultReceivedHandler(sketchPanel_ResultReceived);
            sketchPanel.SketchFileLoaded -= new SketchFileLoadedHandler(sketchPanel_SketchFileLoaded);
            sketchPanel.InkCanvas.StrokeErasing -= new InkCanvasStrokeErasingEventHandler(InkPicture_StrokesDeleting);
            sketchPanel.InkTransformed -= new InkTransformedEventHandler(sketchPanel_InkTransformed);
        }

        #endregion

        #region Ink and Panel Hooks

        /// <summary>
        /// Initializes Error highlighting.
        /// </summary>
        /// <param name="result">The current recognition result.</param>
        private void sketchPanel_ResultReceived(SketchPanelLib.RecognitionResult result)
        {
            // Nothing for now - Should begin parsing
        }

        /// <summary>
        /// Reinitializes error labels when the sketch is zoomed or strokes are moved.
        /// </summary>
        private void sketchPanel_InkTransformed(System.Windows.Rect originalBoundingBox, 
            System.Windows.Rect newBoundingBox, System.Windows.Ink.StrokeCollection iStrokes, InkTransformEventType eventType)
        {
            // Not currently using error list.
            //if (this.errorList != null)
            //    initErrors();
        }

        /// <summary>
        /// Initializes error-showing-related fields and builds error lables from
        /// internal errorList.
        /// 
        /// Precondition: this.errorList is set and is unconsolidated
        /// </summary>
        private void initErrors()
        {
            // Hide all current errors
            hideAllErrors();

            // Init data structures produced through highlighting
            strokeId2Color = new Dictionary<String, Color>();
            iStroke2Label = new Dictionary<String, System.Windows.Controls.Label>();
            errorLabels = new List<System.Windows.Controls.Label>();

            // Build and show the errors
            buildErrorLabels();
            showAllErrors();
        }

        /// <summary>
        /// Clears this feedback mechanism whenever a file is loaded
        /// </summary>
        private void sketchPanel_SketchFileLoaded()
        {
            hideAllErrors();
        }

        /// <summary>
        /// Deletes an error label when its corresponding strokes are erased.
        /// </summary>
        private void InkPicture_StrokesDeleting(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            System.Windows.Ink.Stroke iStroke = e.Stroke;

            if (iStroke2Label == null)
                return;

            if (iStroke2Label.ContainsKey((String)iStroke.GetPropertyData(idGuid)))
            {
                System.Windows.Controls.Label label = iStroke2Label[(String)iStroke.GetPropertyData(idGuid)];

                label.Visibility = System.Windows.Visibility.Visible;
                sketchPanel.InkCanvas.Children.Remove(label);

                errorLabels.Remove(label);

                sketchPanel.InkCanvas.InvalidateArrange();
            }

        }

        #endregion

        #region Parse Error Label Building

        /// <summary>
        /// Builds all error labels and prepares errors for showing
        /// Does not do anything right now
        /// </summary>
        private void buildErrorLabels()
        {
            // Not currently uing error list
            
            /*List<ParseError> consolidatedErrorList = consolidateErrors(errorList);

            foreach (ParseError e in consolidatedErrorList)
            {
                // Collect error substrokes and Ink Strokes
                List<Substroke> errorSubStrs = getErrorSubstrokes(e);
                List<System.Windows.Ink.Stroke> iStrokes = new List<System.Windows.Ink.Stroke>();
                foreach (Substroke s in errorSubStrs)
                {
                    String iStrId = sketchPanel.InkSketch.GetInkStrokeBySubstrokeId(s.XmlAttrs.Id);
                    System.Windows.Ink.Stroke iStr = sketchPanel.InkSketch.GetInkStrokeById(iStrId);
                    if (iStr != null)
                    {
                        iStrokes.Add(iStr);

                        // Record original stroke color
                        if (!strokeId2Color.ContainsKey((String)iStr.GetPropertyData(idGuid)))
                            strokeId2Color.Add((String)iStr.GetPropertyData(idGuid), iStr.DrawingAttributes.Color);
                    }
                }

                // Determine message label position
                System.Drawing.Point labelPosition = getLabelPosition(e, iStrokes);

                // Create a new label
                System.Windows.Controls.Label newLabel = createErrorLabel(e.ToString(), labelPosition);

                // Record new label
                errorLabels.Add(newLabel);

                // Record Ink Stroke to Error Label mapping
                foreach (System.Windows.Ink.Stroke iStroke in iStrokes)
                {
                    if (!iStroke2Label.ContainsKey((String)iStroke.GetPropertyData(idGuid)))
                        iStroke2Label.Add((String)iStroke.GetPropertyData(idGuid), newLabel);
                }
            }
            */
        }

        /// <summary>
        /// Consolidates error Messages of errors referencing the same element(s).
        /// 
        /// Helper Function
        /// </summary>
        /// <param name="errorList">The list of Parse errors to consolidate</param>
        /// <returns>A list of consolidated Parse errors.</returns>
        private static List<ParseError> consolidateErrors(List<ParseError> errorList)
        {
            
            List<ParseError> newErrorList = new List<ParseError>();
            Dictionary<object, int> errorMap = new Dictionary<object, int>();

            // Fill consolidation map
            foreach (ParseError e in errorList)
            {
                // See if error overlaps
                int errId = -1;
                foreach (object o in e.errors)
                {
                    if (errorMap.ContainsKey(o))
                    {
                        errId = errorMap[o];
                        break;
                    }
                }
                
                // Consolidate error
                if (errId != -1)
                {
                    foreach (object o in e.errors)
                    {
                        if (!errorMap.ContainsKey(o))
                            errorMap.Add(o, errId);
                    }

                    e.errors.AddRange(newErrorList[errId].errors);

                    ParseError newErr = new ParseError(e.errors,
                        newErrorList[errId].Message + "\n" + e.Message);
                    newErrorList[errId] = newErr;
                }
                else
                {
                    newErrorList.Add(e);
                    foreach (object o in e.errors)
                        errorMap.Add(o, newErrorList.Count - 1);
                }
            }

            // Return new error list
            return newErrorList;
        }

        /// <summary>
        /// Calculates the position of an error label based upon the bounding box of the elements
        /// causing the error.
        /// 
        /// Helper Function
        /// </summary>
        private System.Drawing.Point getLabelPosition(ParseError e, List<System.Windows.Ink.Stroke> iStrokes)
        {
            if (e.errors.Count == 0)
            {
                // TEMP HACK return the origin (or near it) when there are no associated elements
                return new System.Drawing.Point(10, 10);
            }

            // Get bounding box of strokes in screen space
            System.Drawing.Point bbUpperLeft;
            System.Drawing.Point bbLowerRight;
            System.Windows.Ink.StrokeCollection strokes = new System.Windows.Ink.StrokeCollection();
           
            foreach (System.Windows.Ink.Stroke s in iStrokes)
            {
               strokes.Add(s);
            }

            System.Windows.Rect strokesBB = strokes.GetBounds();
            bbUpperLeft = new System.Drawing.Point((int)strokesBB.X, (int)strokesBB.Y);
            bbLowerRight = new System.Drawing.Point((int)strokesBB.Right, (int)strokesBB.Bottom);

            // return position just to the right of the bounding box
            System.Drawing.Point returnPoint =
                new System.Drawing.Point(bbLowerRight.X + 10, bbUpperLeft.Y + 10);

            return returnPoint;
        }

        /// <summary>
        /// Returns a list of all the substrokes referenced by the given Parse Error.
        /// 
        /// Helper Function.
        /// </summary>
        private static List<Substroke> getErrorSubstrokes(ParseError e)
        {
            List<Substroke> elementStrokes = new List<Substroke>();
            foreach (object o in e.errors)
            {
                if (o == null)
                {
                    continue;
                }
                if (o is Shape)
                {
                    elementStrokes.AddRange(((Shape)o).Substrokes);
                }
                if (o is Wire)
                {
                    elementStrokes.Add(((Wire)o).Substroke);
                }
                if (o is Mesh)
                {
                    Mesh m = (Mesh)o;
                    foreach (Wire w in m.AllConnectedWires)
                    {
                        elementStrokes.Add(w.Substroke);
                    }
                }
                if (o is BaseSymbol)
                {
                    elementStrokes.AddRange(((BaseSymbol)o).Substrokes);
                }
            }

            return elementStrokes;
        }
        
        /// <summary>
        /// Creates a label for given error message at drawPoint.
        /// 
        /// Helper Function.
        /// </summary>
        private static System.Windows.Controls.Label createErrorLabel(string message, System.Drawing.Point drawPoint)
        {
            System.Windows.Controls.Label label = new System.Windows.Controls.Label();
            label.Content = message;
            InkCanvas.SetTop(label, drawPoint.Y);
            InkCanvas.SetLeft(label, drawPoint.X);
            label.ClipToBounds = false;
            label.BorderThickness = new System.Windows.Thickness(1);
            label.IsEnabled = true;
            label.Visibility = System.Windows.Visibility.Visible;

            return label;
        }

        #endregion

        #region Parse Error Label Showing

        /// <summary>
        /// Shows all Error labels when enabled
        /// </summary>
        private void showAllErrors()
        {
            if (!highlightingEnabled)
                return;

            // Show error labels
            if (errorLabels != null)
            {
                foreach (System.Windows.Controls.Label label in errorLabels)
                {
                    sketchPanel.InkCanvas.Children.Add(label);
                    label.Visibility = System.Windows.Visibility.Visible;
                }
            }

            if (strokeId2Color != null)
            {
                // Highlight strokes
                foreach (String id in strokeId2Color.Keys)
                {
                    setInkStrokeColor(id, (Color)ColorConverter.ConvertFromString("Red"));
                }
            }
        }

        /// <summary>
        /// Gets the stroke refered to by the stroke ID
        /// </summary>
        /// <param name="strokeID"></param>A string Guid? of a stroke
        /// <returns></returns>
        private System.Windows.Ink.Stroke strokeFromString(String strokeId)
        {
            return sketchPanel.InkSketch.GetInkStrokeById(strokeId);
        }

        /// <summary>
        /// Sets the color of Ink Stroke with Id iStrokeId to Color c
        /// 
        /// Helper Function
        /// </summary>
        private void setInkStrokeColor(String iStrokeId, Color c)
        {
            System.Windows.Ink.Stroke iStroke = strokeFromString(iStrokeId);

            if (iStroke != null)
            {
                iStroke.DrawingAttributes.Color = c;

                // Invalidate highlighted stroke
                //int padding = highlightThicknessHeight > highlightThicknessWidth ? 
                //        highlightThicknessHeight : highlightThicknessWidth;
                sketchPanel.InvalidateStroke(iStroke);
            }
        }

        

        #endregion

        #region Parse Error Hiding

        /// <summary>
        /// UnHighlights all errors.
        /// </summary>
        private void hideAllErrors()
        {
            if (errorLabels != null)
            {
                // Hide error labels
                foreach (System.Windows.Controls.Label l in errorLabels)
                {
                    sketchPanel.InkCanvas.Children.Remove(l);
                    l.Visibility = System.Windows.Visibility.Hidden;
                }
            }

            if (strokeId2Color != null)
            {
                // UnHighlight strokes
                foreach (String id in strokeId2Color.Keys)
                {
                    setInkStrokeColor(id, strokeId2Color[id]);
                }
            }
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
                highlightingEnabled = value;
                if (highlightingEnabled)
                    showAllErrors();
                else
                    hideAllErrors();
            }
        }

        #endregion
    }
}
