using System;
using System.Collections.Generic;
using System.Text;

namespace Sketch
{
    /// <summary>
    /// Document class.
    /// </summary>
    public class Document
    {
        #region INTERNALS

        /// <summary>
        /// Different pages in the document, each page is it's own sketch
        /// </summary>
        private Dictionary<int, Sketch> m_Pages;



        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// No argument constructor.
        /// </summary>
        public Document() :
            this(new Sketch())
        {
        }

        /// <summary>
        /// Constructoe
        /// </summary>
        /// <param name="sketch">The associated sketch.</param>
        public Document(Sketch sketch)
        {
            m_Pages = new Dictionary<int, Sketch>();
            m_Pages.Add(1, sketch);
        }

        #endregion
    }
}
