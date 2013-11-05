/*
 * File: MakeJnt.cs
 *
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using Microsoft.Ink;
using Sketch;

namespace ConverterJnt
{
    /// <summary>
    /// Summary description for MakeJnt.
    /// </summary>
    public class MakeJnt
    {
        private Ink ink;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sketch"></param>
        public MakeJnt(Sketch.Sketch sketch)
        {
            ink = new Ink();

            Sketch.Substroke[] substrokes = sketch.Substrokes;
            int slength = substrokes.Length;
            int plength;
            int i;
            int k;
            for (i = 0; i < slength; ++i)
            {
                Sketch.Point[] sketchPts = substrokes[i].Points;
                System.Drawing.Point[] simplePts = new System.Drawing.Point[sketchPts.Length];

                plength = simplePts.Length;
                for (k = 0; k < plength; k++)
                {
                    simplePts[k] = new System.Drawing.Point((int)sketchPts[k].X, (int)sketchPts[k].Y);
                }

                ink.CreateStroke(simplePts);
            }
        }


        /// <summary>
        /// Write out to Jnt file
        /// </summary>
        /// <param name="filename"></param>
        public void WriteJnt(string filename)
        {
            throw new Exception("Cannot write to Microsoft Jnt format... they do not support it");
        }


        /// <summary>
        /// Get Ink
        /// </summary>
        public Ink Ink
        {
            get
            {
                return this.ink;
            }
        }
    }
}
