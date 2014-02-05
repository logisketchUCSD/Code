/*
 * File: ReadInk.cs
 * 
 * Author: Andrew Danowitz
 * Harvey Mudd College, Claremont, CA 91711
 * Sketchers 2007.
 * 
 * Description: Converts a Microsoft Ink object into a Sketch object.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code my cause.
 */

using System;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;

using ConverterJnt;


namespace InkToSketchWPF
{
    public class ReadInk
    {
        #region INTERNAL

        private Sketch.Sketch sketch;

        #endregion

        #region CONSTRUCTORS

        ///  <summary>
        ///  Constructor, creates a Sketch from the first page of the file
        ///  </summary>
        ///  <param name="ink">The name of the ink object</param>
        public ReadInk(Ink ink)
        {
            //Code block taken from ConverterJnt
            sketch = new Sketch.Sketch();
            sketch.XmlAttrs.Id = System.Guid.NewGuid();
            sketch.XmlAttrs.Units = "himetric";
            
            //Sends Microsoft.Ink object to be converted into a sketch
            try
            {
                this.ConvertToSketch(ink);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException);
            }
        }

        #endregion

        #region DATA MUNGING

        /// <summary>
        /// Strips stroke data from the Microsoft Ink object and adds them to the ReadInk
        /// object's sketch.
        /// </summary>
        /// <param name="ink">A Microsoft Ink object</param>
        private void ConvertToSketch(Ink ink)
        {
            List<Sketch.Stroke> strokes;

            //Strips the Microsoft stroke data from ink and converts it into Sketch friendly
            //stroke objects
            strokes = CreateConvertedStrokes(ink);

            //Adds strokes to the sketch
            this.sketch.AddStrokes(strokes);
        }

        /// <summary>
        /// Removes stroke data from a Microsoft Ink object and converts it into a sketch
        /// friendly format
        /// </summary>
        /// <param name="ink">A Microsoft Ink object</param>
        /// <returns>An ArrayList containing Sketch stroke data</returns>
        private List<Sketch.Stroke> CreateConvertedStrokes(Ink ink)
        {
            List<Sketch.Stroke> strokes = new List<Sketch.Stroke>();

            ReadJnt readJnt = new ReadJnt();

            //Goes through the ink object, converts each stroke to a sketch stroke and adds
            //it to the List of strokes
            foreach (Microsoft.Ink.Stroke istroke in ink.Strokes)
            {
                strokes.Add(readJnt.InkStroke2SketchStroke(istroke, null, false));
            }

            return strokes;
        }

        #endregion

        #region GET

        ///  <summary>
        ///  Get Sketch
        ///  </summary>
        public Sketch.Sketch Sketch
        {
            get
            {
                return this.sketch;
            }
        }

        #endregion
    }
}
