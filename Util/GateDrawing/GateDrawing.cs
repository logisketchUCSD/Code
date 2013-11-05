using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace GateDrawing
{

    public class GateDrawing
    {
        #region Internals

        /// <summary>
        /// Thickness of outline if we're using a dashed pen
        /// </summary>
        private const double dashedPenThickness = 2.0;

        /// <summary>
        /// Thickness of outline if we're using a solid pen
        /// </summary>
        private const double solidPenThickness = 1.0;

        /// <summary>
        /// Affects "pointy-ness" of OR, NOR, XOR, and XNOT gates.
        /// 0 means that they look like ANDs with curved backs.  30 is decently pointy.
        /// </summary>
        private const int tilt = 0;

        /// <summary>
        /// Affects the depth of the back-curves of OR related gates.
        /// Should be somewhere within 0 and 3.0, exclusive, with higher numbers being deeper curves
        /// </summary>
        private const double backCurveDepth = 2.9;

        /// <summary>
        /// The ratio of a gate's width to the space between the line behind Xor and Xnor and the gate itself
        /// </summary>
        private const double widthToSpace = 6.0;

        /// <summary>
        /// The ratio of a gate's overall width to its not-bubble radius
        /// </summary>
        private const double widthToRadius = 8.0;

        /// <summary>
        /// Set to true if rotation should be in effect
        /// </summary>
        private bool rotateGates;

        /// <summary>
        /// Set to true if we want to round to the nearest 90 degrees
        /// </summary>
        private bool snapRotation;

        /// <summary>
        /// Locks the aspect ratio of each drawing if set to true
        /// </summary>
        private bool lockRatio;

        /// <summary>
        ///  Text for the user that will tell them how to improve their
        ///  gate drawing (proportion-wise)
        /// </summary>
        private string drawingAdvice;

        /// <summary>
        /// Threshold for how much the gate drawing can vary in ratio
        /// from the corresponding gate with perfect dimensions
        /// 
        /// Only necessary if we are not hooking into relabeling event
        /// </summary>
        private const double higherThreshold = 0.8;
        private const double lowerThreshhold = 1.2;

        /// <summary>
        /// Drawing advice messages
        /// </summary>
        private const string DRAW_MESSAGE = " Try drawing ";
        private const string DRAW_MESSAGE0 = " gates skinnier (smaller in the vertical direction) next time.";
        private const string DRAW_MESSAGE1 = " gates longer (larger in the horizontal direction) next time.";
        private const string DRAW_MESSAGE2 = " gates fatter (larger in the vertical direction) next time.";
        private const string DRAW_MESSAGE3 = " gates shorter (smaller in the horizontal direction) next time.";
        private const string DRAW_MESSAGE4 = " You might need to draw the back of ";
        private const string DRAW_MESSAGE5 = " gates curvier next time.";
        
        #endregion

        #region Constructor and Main Method

        /// <summary>
        /// This constructor sets some bools, nothing else.
        /// </summary>
        public GateDrawing()
        {
            rotateGates = true;
            lockRatio = true;
            snapRotation = false;
            drawingAdvice = "";
        }

        /// <summary>
        /// Creates an image of the gate at the given point  
        /// </summary>
        /// <param name="gate">A string with the gate type</param>
        /// <param name="bounds">The bounds for the gate</param>
        /// <param name="filled">Whether the gate should be filled with a color or not</param>
        /// <param name="dashed">Whether the line of the gate should be dashed or not</param>
        /// <param name="orientation">The orientation of the shape, in radians</param>
        public GeometryDrawing DrawGate(ShapeType gate, Rect bounds, bool filled, bool dashed, double orientation = 0)
        {
            #region Get brush and pen
            Brush fillBrush;
            Pen pen = new Pen();

            // Fill the gate with its specific color, if needed
            if (filled)
                fillBrush = (Brush)new SolidColorBrush(gate.Color);
            else
                fillBrush = Brushes.Transparent;

            // Get the pen, do we want it dashed or not?
            if (dashed)
            {
                pen.DashStyle = DashStyles.Dash;
                pen.Brush = Brushes.Gray;
                pen.Thickness = dashedPenThickness;
            }
            else
            {
                pen.DashStyle = DashStyles.Solid;
                pen.Brush = Brushes.Black;
                pen.Thickness = solidPenThickness;
            }
            #endregion

            #region Rotate Setup

            // If we don't want to rotate the gates, just set the orientation to 0.
            if (!RotateGates) orientation = 0;

            // Convert orientation in radians to degrees between 0 and 360.
            orientation = Math.IEEERemainder(orientation * 180 / Math.PI, 360);

            if (snapRotation)
            {
                // Round to the nearest 90.
                orientation = (int)(90 * Math.Round(orientation / 90));
            }

            // If we're not horizontal, change dimensions now so we don't distort the shape later.
            Rect drawBounds = bounds;
            if (orientation % 180 == 90)
            {
                drawBounds.Width = bounds.Height;
                drawBounds.Height = bounds.Width;

                // Keep the center point at the same place, as we're rotating around it.
                drawBounds.X = bounds.X + bounds.Width / 2 - drawBounds.Width / 2;
                drawBounds.Y = bounds.Y + bounds.Height / 2 - drawBounds.Height / 2;
            }

            #endregion

            #region Lock Ratio

            if (lockRatio)
            {
                // Get ratio guidelines from practice window
                double width = 100, height = 70;

                // Fix the width value based on what type
                // of gate we have
                if (gate == LogicDomain.AND)
                    width = width - width / (widthToRadius * 2);
                else if (gate == LogicDomain.OR)
                    width = width - width / (widthToRadius * 2) - width / widthToSpace;
                else if (gate == LogicDomain.NOR)
                    width = width - width / widthToSpace;
                else if (gate == LogicDomain.XOR)
                    width = width - width / (widthToRadius * 2);
                else if (gate == LogicDomain.NOTBUBBLE)
                    width = height;

                // Ratio of new "perfect" width of the rect (length of the gate)
                // to the "perfect" height of the rect (width of the gate)
                double perfectDimensionRatio = width / height;

                // Ratio of the width of the rect (length of the gate)
                // to the height of the rect (width of the gate)
                double actualDimensionRatio = drawBounds.Width / drawBounds.Height;

                // Assign the advice for the particular drawing

                // Gate is too wide or too short, so give the appropriate advice
                if (actualDimensionRatio * lowerThreshhold < perfectDimensionRatio)
                    drawingAdvice = DRAW_MESSAGE + gate.ToString() + DRAW_MESSAGE0;
                //drawingAdvice = DRAW_MESSAGE + gate.ToString() + DRAW_MESSAGE1;

                // Gate is too narrow or too long, so give the appropriate advice
                else if (actualDimensionRatio * higherThreshold > perfectDimensionRatio)
                    drawingAdvice = DRAW_MESSAGE + gate.ToString() + DRAW_MESSAGE2;
                //drawingAdvice = DRAW_MESSAGE + gate.ToString() + DRAW_MESSAGE3;

                else if (gate != LogicDomain.AND && gate != LogicDomain.NOT &&
                    gate != LogicDomain.NOTBUBBLE && gate != LogicDomain.NAND)
                    // Only talk about curviness if the gate could possibly be curvier
                    drawingAdvice = DRAW_MESSAGE4 + gate.ToString() + DRAW_MESSAGE5;

                else
                    // No drawing advice!
                    drawingAdvice = "";

                //*************************************************************
                // Old way of scaling gates:
                // Contain new bounds within original bounds
                // When the width is too large (greater than what 
                // makes a perfect width/height ratio), resize the 
                // sketched gate width to make it the perfect ratio
                //if (drawBounds.Width > drawBounds.Height * widthToHeight)
                    //drawBounds.Width = drawBounds.Height * widthToHeight;
                //else
                // Otherwise, decrease the length (height) of the sketched gate
                // to be the perfect ratio
                // drawBounds.Height = drawBounds.Width/perfectDimensionRatio;
                //**************************************************************

                // The height of the template should be what it would be if
                // it were a perfect ratio of width to height.
                // The length of the ghost gate (width) stays the same.
                drawBounds.Height = drawBounds.Width / perfectDimensionRatio;

                // OR, comment out the above and uncomment out below if you
                // want to fix the dimensions this way:
                // The width of the template (the length of the ghost gate)
                // should be what it would be if it were a perfect ratio of
                // width to height.
                // The height of the template stays the same.
                //drawBounds.Width = perfectDimensionRatio * drawBounds.Height;
            }

            #endregion

            #region Draw Gates

            // Create the gate that was specified
            System.Windows.Media.GeometryDrawing drawnGate;
            if (gate == LogicDomain.AND) drawnGate = DrawAnd(drawBounds, fillBrush, pen);
            else if (gate == LogicDomain.NAND) drawnGate = DrawNand(drawBounds, fillBrush, pen);
            else if (gate == LogicDomain.OR) drawnGate = DrawOr(drawBounds, fillBrush, pen);
            else if (gate == LogicDomain.NOR) drawnGate = DrawNor(drawBounds, fillBrush, pen);
            else if (gate == LogicDomain.XOR) drawnGate = DrawXor(drawBounds, fillBrush, pen);
            else if (gate == LogicDomain.XNOR) drawnGate = DrawXnor(drawBounds, fillBrush, pen);
            else if (gate == LogicDomain.NOT) drawnGate = DrawNot(drawBounds, fillBrush, pen);
            else if (gate == LogicDomain.NOTBUBBLE) drawnGate = DrawNotBubble(drawBounds, fillBrush, pen);
            else if (gate == LogicDomain.SUBCIRCUIT) drawnGate = DrawSubCircuit(drawBounds, fillBrush, pen);
            else drawnGate = null;

            #endregion

            #region Rotate Shapes

            // Don't do these calculations if we don't need to
            if (orientation != 0)
            {
                TransformGroup transforms = new TransformGroup();

                // Rotate around the center of the shape
                transforms.Children.Add(new RotateTransform(orientation, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));

                drawnGate.Geometry.Transform = transforms;
            }

            #endregion

            return drawnGate;
        }

        #endregion

        #region Drawing Individual Gates

        /// <summary>
        /// Draws an image of an AND gate
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawAnd(Rect bounds, Brush fillBrush, Pen pen)
        {
            LineGeometry line = new LineGeometry(bounds.TopLeft, bounds.BottomLeft);
            ArcSegment arc = new ArcSegment(bounds.BottomLeft, new Size(bounds.Width, bounds.Height / 2), 0, true, SweepDirection.Clockwise, true);
            PathGeometry shape = new PathGeometry();
            PathFigure path = new PathFigure();
            path.Segments.Add(arc);
            path.StartPoint = bounds.TopLeft;

            shape.AddGeometry(line);
            shape.Figures.Add(path);

            return new GeometryDrawing(fillBrush, pen, shape);
        }

        /// <summary>
        /// Draws an image of a NOT gate
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawNot(Rect bounds, Brush fillBrush, Pen pen)
        {
            double circleRadius = bounds.Width / widthToRadius;

            LineSegment line1 = new LineSegment(bounds.BottomLeft, true);
            LineSegment line2 = new LineSegment(new Point(bounds.Right - circleRadius * 2, bounds.Top + bounds.Height / 2), true);
            LineSegment line3 = new LineSegment(bounds.TopLeft, true);
            PathGeometry shape = new PathGeometry();
            PathFigure path = new PathFigure();

            path.StartPoint = bounds.TopLeft;
            path.Segments.Add(line1);
            path.Segments.Add(line2);
            path.Segments.Add(line3);
            shape.Figures.Add(path);

            EllipseGeometry circle = new EllipseGeometry(new Point(bounds.Right - circleRadius, bounds.Top + bounds.Height / 2), circleRadius, circleRadius);
            shape.AddGeometry(circle);

            return new GeometryDrawing(fillBrush, pen, shape);
        }

        /// <summary>
        /// Draws an image of an OR gate
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawOr(Rect bounds, Brush fillBrush, Pen pen)
        {
            // Back of the gate
            ArcSegment arc1 = new ArcSegment(bounds.BottomLeft, new Size(bounds.Height * backCurveDepth, bounds.Height), 
                0, false, SweepDirection.Clockwise, true);
            // Right half
            ArcSegment arc2 = new ArcSegment(new Point(bounds.Right, bounds.Top + bounds.Height / 2), new Size(bounds.Width, bounds.Height / 2),
                360-tilt, false, SweepDirection.Counterclockwise, true);
            // Left half
           ArcSegment arc3 = new ArcSegment(bounds.TopLeft, new Size(bounds.Width, bounds.Height / 2), 
               tilt, false, SweepDirection.Counterclockwise, true);

            PathFigure path = new PathFigure();
            path.StartPoint = bounds.TopLeft;
            path.Segments.Add(arc1);
            path.Segments.Add(arc2);
            path.Segments.Add(arc3);

            PathGeometry shape = new PathGeometry();
            shape.Figures.Add(path);

            return new GeometryDrawing(fillBrush, pen, shape);
        }

        /// <summary>
        /// Draws an image of a XOR gate
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawXor(Rect bounds, Brush fillBrush, Pen pen)
        {
            double space = bounds.Width / widthToSpace;

            ArcSegment arc1 = new ArcSegment(new Point(bounds.Left + space, bounds.Bottom), new Size(bounds.Height * backCurveDepth, bounds.Height), 0, false, SweepDirection.Clockwise, true);
            ArcSegment arc2 = new ArcSegment(new Point(bounds.Right, bounds.Top + bounds.Height / 2), new Size(bounds.Width, bounds.Height / 2),
                360-tilt, false, SweepDirection.Counterclockwise, true);
            ArcSegment arc3 = new ArcSegment(new Point(bounds.Left + space, bounds.Top), new Size(bounds.Width, bounds.Height / 2), tilt, false, SweepDirection.Counterclockwise, true);
            ArcSegment arc4 = new ArcSegment(bounds.BottomLeft, new Size(bounds.Height * backCurveDepth, bounds.Height), 0, false, SweepDirection.Clockwise, true);

            PathFigure path1 = new PathFigure();
            path1.StartPoint = new Point(bounds.Left + space, bounds.Top);
            path1.Segments.Add(arc1);
            path1.Segments.Add(arc2);
            path1.Segments.Add(arc3);

            PathFigure path2 = new PathFigure();
            path2.StartPoint = bounds.TopLeft;
            path2.Segments.Add(arc4);
            path2.IsFilled = false;

            PathGeometry shape = new PathGeometry();
            shape.Figures.Add(path1);
            shape.Figures.Add(path2);

            return new GeometryDrawing(fillBrush, pen, shape);
        }

        /// <summary>
        /// Draws an image of a XNOR gate
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawXnor(Rect bounds, Brush fillBrush, Pen pen)
        {
            double space = bounds.Width / widthToSpace;
            double radius = bounds.Width / widthToRadius;

            ArcSegment arc1 = new ArcSegment(new Point(bounds.Left + space, bounds.Bottom), new Size(bounds.Height * backCurveDepth, bounds.Height), 0, false, SweepDirection.Clockwise, true);
            ArcSegment arc2 = new ArcSegment(new Point(bounds.Right - radius * 2, bounds.Top + bounds.Height / 2), new Size(bounds.Width, bounds.Height / 2),
                360-tilt, false, SweepDirection.Counterclockwise, true);
            ArcSegment arc3 = new ArcSegment(new Point(bounds.Left + space, bounds.Top), new Size(bounds.Width, bounds.Height / 2), tilt, false, SweepDirection.Counterclockwise, true);
            ArcSegment arc4 = new ArcSegment(bounds.BottomLeft, new Size(bounds.Height * backCurveDepth, bounds.Height), 0, false, SweepDirection.Clockwise, true);

            PathFigure path1 = new PathFigure();
            path1.StartPoint = new Point(bounds.Left + radius, bounds.Top);
            path1.Segments.Add(arc1);
            path1.Segments.Add(arc2);
            path1.Segments.Add(arc3);

            PathFigure path2 = new PathFigure();
            path2.StartPoint = bounds.TopLeft;
            path2.Segments.Add(arc4);
            path2.IsFilled = false;

            PathGeometry shape = new PathGeometry();
            shape.Figures.Add(path1);
            shape.Figures.Add(path2);

            EllipseGeometry circle = new EllipseGeometry(new Point(bounds.Right - radius, bounds.Top + bounds.Height / 2), radius, radius);
            shape.AddGeometry(circle);

            return new GeometryDrawing(fillBrush, pen, shape);
        }

        /// <summary>
        /// Draws an image of a NOR gate
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawNor(Rect bounds, Brush fillBrush, Pen pen)
        {
            double radius = bounds.Width / widthToRadius;

            ArcSegment arc1 = new ArcSegment(bounds.BottomLeft, new Size(bounds.Height * backCurveDepth, bounds.Height), 0, false, SweepDirection.Clockwise, true);
            ArcSegment arc2 = new ArcSegment(new Point(bounds.Right - radius * 2, bounds.Top + bounds.Height / 2), new Size(bounds.Width, bounds.Height / 2),
                360-tilt, false, SweepDirection.Counterclockwise, true);
            ArcSegment arc3 = new ArcSegment(bounds.TopLeft, new Size(bounds.Width, bounds.Height / 2), tilt, false, SweepDirection.Counterclockwise, true);

            PathFigure path = new PathFigure();
            path.StartPoint = bounds.TopLeft;
            path.Segments.Add(arc1);
            path.Segments.Add(arc2);
            path.Segments.Add(arc3);

            PathGeometry shape = new PathGeometry();
            shape.Figures.Add(path);

            EllipseGeometry circle = new EllipseGeometry(new Point(bounds.Right - radius, bounds.Top + bounds.Height / 2), radius, radius);
            shape.AddGeometry(circle);

            return new GeometryDrawing(fillBrush, pen, shape);
        }

        /// <summary>
        /// Draws an image of an NAND gate
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawNand(Rect bounds, Brush fillBrush, Pen pen)
        {
            double circleRadius = bounds.Width / widthToRadius;

            LineGeometry line = new LineGeometry(bounds.TopLeft, bounds.BottomLeft);
            ArcSegment arc = new ArcSegment(bounds.BottomLeft, new Size(bounds.Width - circleRadius * 2, bounds.Height / 2),
                    0, true, SweepDirection.Clockwise, true);
            PathGeometry shape = new PathGeometry();
            PathFigure path = new PathFigure();
            path.Segments.Add(arc);
            path.StartPoint = bounds.TopLeft;
            EllipseGeometry circle = new EllipseGeometry(new Point(bounds.Right - circleRadius, bounds.Top + bounds.Height / 2), circleRadius, circleRadius);

            shape.AddGeometry(line);
            shape.AddGeometry(circle);
            shape.Figures.Add(path);

            return new GeometryDrawing(fillBrush, pen, shape);
        }

        /// <summary>
        /// Creates an image of a NotBubble
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawNotBubble(Rect bounds, Brush fillBrush, Pen pen)
        {
            return new GeometryDrawing(fillBrush, pen,
                        new EllipseGeometry(new Point(bounds.Left+bounds.Width/2, bounds.Top+bounds.Height/2), bounds.Height / 2, bounds.Width / 2));
        }

       
        /// <summary>
        /// Creates an image of an SubCircuit
        /// </summary>
        /// <param name="bounds">The desired bounds of the image</param>
        /// <param name="fillBrush">The brush for the interior of the shape</param>
        /// <param name="pen">The pen for the borders of the shape</param>
        /// <returns>A GeometryDrawing of the gate</returns>
        private GeometryDrawing DrawSubCircuit(Rect bounds, Brush fillBrush, Pen pen)
        {
            double radius = Math.Min(bounds.Height, bounds.Width) / 2;
            
            PathGeometry shape = new PathGeometry();
            RectangleGeometry rect = new RectangleGeometry(bounds);

            shape.AddGeometry(rect);

            return new GeometryDrawing(fillBrush, pen, shape);
        }

        #endregion

        #region Getters and Setters

        /// <summary>
        /// Gets and sets whether we're supposed to be locking the drawing ratio
        /// </summary>
        public bool LockDrawingRatio
        {
            get
            {
                return lockRatio;
            }
            set
            {
                lockRatio = value;
            }
        }


        /// <summary>
        /// Gets and sets whether we're supposed to be rotating gates.
        /// </summary>
        public bool RotateGates
        {
            get
            {
                return rotateGates;
            }
            set
            {
                rotateGates = value;
            }
        }

        /// <summary>
        /// Gets and sets whether we're snapping gates to the nearest 90 degree angle
        /// </summary>
        public bool SnapRotation
        {
            get
            {
                return snapRotation;
            }
            set
            {
                snapRotation = value;
            }
        }

        /// <summary>
        /// Gets the drawingAdvice to display to the screen
        /// </summary>
        public string DrawingAdvice
        {
            get
            {
                return drawingAdvice;
            }
        }
        #endregion
    }
}
