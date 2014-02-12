using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommandManagement;
using System.Windows.Ink;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

namespace EditMenu.CommandList
{
    public delegate void EmbedCallback(Sketch.Shape associatedShape, bool addToList);
    public class AddSubCircuitCmd : Command
    {
        /// <summary>
        /// Where the subCircuit should be added
        /// </summary>
        private Point addPoint;

        /// <summary>
        /// For if we need to use the callback to add to the embed list
        /// </summary>
        bool addToList;

        /// <summary>
        /// For adding the stroke to the sketch
        /// </summary>
        private SketchPanelLib.SketchPanel sketchPanel;

        /// <summary>
        /// The tag number associated with this project. It is the tag number of the button
        /// when added. if the project doesn't have this in the subproject list, it needs to be
        /// added.
        /// </summary>
        private int tagNumber;

        /// <summary>
        /// Keep around the stroke to delete on undo
        /// </summary>
        private Stroke storedStroke;

        /// <summary>
        /// Callback to main to add to the embed list and create the circuit
        /// </summary>
        public event EmbedCallback embedCallback;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="sketch"></param>
        /// <param name="stroke"></param>
        public AddSubCircuitCmd(ref SketchPanelLib.SketchPanel sketchPanel, Point addPoint, int tagNumber, bool addToList = false)
        {
            isUndoable = true;
            storedStroke = null;
            this.sketchPanel = sketchPanel;
            this.addToList = addToList;
            this.addPoint = addPoint;
            this.tagNumber = tagNumber;
        }

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public override string Type()
        {
            return "AddSubCircuit";
        }
        /// <summary>
        /// Add the stroke
        /// </summary>
        public override bool Execute()
        {
            //Make the stroke on the screen
            if (storedStroke == null)
                storedStroke = addSubCircuitBox(addPoint);
            else
                sketchPanel.InkSketch.AddStroke(storedStroke);
            Sketch.Substroke associatedSub = sketchPanel.InkSketch.GetSketchSubstrokeByInk(storedStroke);

            //Label the stroke as a subcircuit and give it the tag number for project lookup
            // We dont want this on the command stack because the undo of this will handle everything we want
            ApplyLabelCmd applyLabel = new ApplyLabelCmd(sketchPanel, new StrokeCollection(new Stroke[] { storedStroke }),
                Domain.LogicDomain.SUBCIRCUIT, true, true, tagNumber);
            applyLabel.Execute();
            Sketch.Shape associatedShape = associatedSub.ParentShape;
            associatedShape.SubCircuitNumber = tagNumber;
            
            //color and select the stroke
            storedStroke.DrawingAttributes.Color = associatedShape.Type.Color;
            sketchPanel.InkCanvas.Select(new StrokeCollection(new Stroke[] { storedStroke }));

            //Add to the embed list if you need to, only if you embed with the widget and it's not already in the list
            if (embedCallback != null)
            {
                embedCallback(associatedShape, addToList);
                //make sure we don't add it to the list again
                embedCallback = null;
                addToList = false;
            }

            return true;
        }

        /// <summary>
        /// draws the box for the subircuit on the screen
        /// will only be called on the first execute of this command, since after this point it will
        /// store a stroke, so that it works in conjunction with the stored stroke in moveresizecmd
        /// </summary>
        private Stroke addSubCircuitBox(Point addpoint)
        {
            //Make a box at the point with these specifications
            int width = 100;
            int height = 70;
            System.Windows.Input.StylusPointCollection line = new System.Windows.Input.StylusPointCollection();
            //Need to create a lot of points so that the sub circuit will select correctly
            for (double m = 0; m <= 1; m += 0.01)
            {
                double midX = addpoint.X + m * (width);
                double midY = addpoint.Y;
                line.Add(new System.Windows.Input.StylusPoint(midX, midY));
            }
            for (double m = 0; m <= 1; m += 0.01)
            {
                double midX = addpoint.X + width;
                double midY = addpoint.Y + m * (height);
                line.Add(new System.Windows.Input.StylusPoint(midX, midY));
            }
            for (double m = 0; m <= 1; m += 0.01)
            {
                double midX = (addpoint.X + width) + m * (-width);
                double midY = addpoint.Y + height;
                line.Add(new System.Windows.Input.StylusPoint(midX, midY));
            }
            for (double m = 0; m <= 1; m += 0.01)
            {
                double midX = addpoint.X;
                double midY = (addpoint.Y + height) + m * (-height);
                line.Add(new System.Windows.Input.StylusPoint(midX, midY));
            }
            Stroke newStroke = new Stroke(line);
            sketchPanel.InkSketch.AddStroke(newStroke);
            return newStroke;

        }

        /// <summary>
        /// Unexecute the Command
        /// </summary>
        public override bool UnExecute()
        {
            //All you need to do is remove the stroke, since this deletes the shape in sketch
            //and calls all the appropriate callbacks.
            sketchPanel.InkSketch.DeleteStroke(storedStroke);
            return true;
        }     
    }
}
