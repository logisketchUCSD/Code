using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Featurefy
{
    /// <summary>
    /// A class for the end points of strokes.
    /// </summary>
    [Serializable]
    class EndPoint
    {
        private Substroke m_Stroke;
        private bool m_End;
        private Point m_Pt;
        private List<EndPoint> m_AttachedEndpoints;

        /// <summary>
        /// Constructor
        /// </summary>
        public EndPoint(Sketch.EndPoint endpoint, bool isEnd)
        {
            m_Stroke = endpoint.ParentSub;
            m_End = isEnd;
            m_Pt = endpoint;
            m_AttachedEndpoints = new List<EndPoint>();
        }

        /// <summary>
        /// Attatch two endpoints
        /// </summary>
        /// <param name="endPoint">Endpoint to attatch</param>
        public void AddAttachment(EndPoint endPoint)
        {
            m_AttachedEndpoints.Add(endPoint);
        }

        /// <summary>
        /// Get the associated substroke
        /// </summary>
        public Substroke Stroke
        {
            get { return m_Stroke; }
        }

        /// <summary>
        /// Get the associated point
        /// </summary>
        public Point Point
        {
            get { return m_Pt; }
        }

        /// <summary>
        /// Get the endpoints attatched to this one
        /// </summary>
        public List<EndPoint> Attachments
        {
            get { return m_AttachedEndpoints; }
        }

        /// <summary>
        /// Get the end of this endpoint
        /// </summary>
        public bool End
        {
            get { return m_End; }
        }
    }
}
