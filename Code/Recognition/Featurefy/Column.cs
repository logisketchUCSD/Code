using System;
using System.Collections.Generic;
using System.Text;

namespace Featurefy
{
    /// <summary>
    /// USED FOR TRUTH TABLES!
    /// Goals for this class are to classify the shapes in a truth table into columns
    /// </summary>
    class Column
    {
        /// <summary>
        /// data member sketch
        /// </summary>
        private Sketch.Sketch sketch;

        /// <summary>
        /// constructor for column; creates a blank sketch
        /// </summary>
        public Column()
        {
            this.sketch = new Sketch.Sketch();
        }

        /// <summary>
        /// finds the midpoint of a shape
        /// </summary>
        /// <param name="sh"></param>
        /// <returns></returns>
        public Sketch.Point shapeMidPoint(Sketch.Shape sh)
        {
            Sketch.Point p = new Sketch.Point();
            p.XmlAttrs.X = (double)sh.XmlAttrs.X + (double)sh.XmlAttrs.Width / 2;
            p.XmlAttrs.Y = (double)sh.XmlAttrs.Y + (double)sh.XmlAttrs.Height / 2;

            return p;
        }
    }
}
