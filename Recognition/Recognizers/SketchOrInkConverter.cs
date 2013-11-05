using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.Ink;

namespace Recognizers
{
    /// <summary>
    /// Provides methods to let one convert between something from a Sketch
    /// (such as Shapes and Strokes) and its equivalent in Microsoft.Ink.
    /// </summary>
    class SketchOrInkConverter
    {
        #region Sketch To Ink Conversions

        /// <summary>
        /// Converts a Sketch shape into a collection of Microsoft.Ink strokes.
        /// </summary>
        /// <param name="shape">The shape to convert</param>
        /// <returns>The desired ink strokes</returns>
        public Microsoft.Ink.Strokes convertToInk(Sketch.Shape shape)
        {
            // Create a collection of strokes
            Microsoft.Ink.Ink ink = new Microsoft.Ink.Ink();
            Microsoft.Ink.Strokes inkStrokes = ink.CreateStrokes();

            Sketch.Substroke[] sketchStrokes = shape.Substrokes;

            // Add the substrokes of our format to the strokes collection.
            foreach (Sketch.Substroke sub in sketchStrokes)
                inkStrokes.Add(convertToInk(sub, ink));

            return inkStrokes;
        }

        /// <summary>
        /// Converts a Sketch substroke into a Microsoft.Ink stroke.
        /// </summary>
        /// <param name="substroke">The Sketch substroke to convert</param>
        /// <param name="ink">A Microsoft.Ink Ink object</param>
        /// <returns>The desired ink stroke</returns>
        private Microsoft.Ink.Stroke convertToInk(
            Sketch.Substroke substroke,
            Microsoft.Ink.Ink ink)
        {
            int pointsCount = substroke.Points.Length;
            System.Drawing.Point[] inkPoints = new System.Drawing.Point[pointsCount];

            // Convert the Sketch point format to Microsoft point format
            for (int i = 0; i < pointsCount; i++)
            {
                inkPoints[i] = new System.Drawing.Point(
                    (int)(substroke.Points[i].X),
                    (int)(substroke.Points[i].Y));
            }

            return ink.CreateStroke(inkPoints);
        }

        #endregion

        #region Ink to Sketch Conversions

        #endregion
    }
}
