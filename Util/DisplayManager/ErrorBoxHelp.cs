using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using CircuitParser;

namespace DisplayManager
{
    public class ErrorBoxHelp
    {
        public ParseError ParseError;
        private System.Windows.Rect errShape;
        private System.Windows.Shapes.Rectangle helpBox;
        private SketchPanelLib.SketchPanel panel;
        private bool Showing;

        /// <summary>
        /// Has information for highlighting a parse error on the sketchPanel
        /// </summary>
        /// <param name="error"></param>
        /// <param name="sketchPanel"></param>
        public ErrorBoxHelp (ParseError error, SketchPanelLib.SketchPanel sketchPanel)
        {
            ParseError = error;
            panel = sketchPanel;

            errShape = error.Where.Bounds;
            errShape.X -= 10;
            errShape.Y -= 10;
            errShape.Width += 20;
            errShape.Height += 20;
            helpBox = new System.Windows.Shapes.Rectangle();
            
            helpBox.Height = errShape.Height;
            helpBox.Width = errShape.Width;
            InkCanvas.SetTop(helpBox, errShape.Y);
            InkCanvas.SetLeft(helpBox, errShape.X);

            helpBox.Fill = Brushes.Yellow;
            helpBox.Opacity = .4;

            Showing = false;
        }

        /// <summary>
        /// The actual drawing of the highlighted box onto the screen
        /// </summary>
        public void ShowBox()
        {
            if (Showing) return;
            Showing = true;

            // Add rect to Picture
            panel.InkCanvas.Children.Add(helpBox);
            helpBox.Visibility = System.Windows.Visibility.Visible;
        }
        /// <summary>
        /// removes the box
        /// </summary>
        public void HideBox()
        {
            if (!Showing) return;
            Showing = false;

            helpBox.Visibility = System.Windows.Visibility.Hidden;
            panel.InkCanvas.Children.Remove(helpBox);
        }
    }
}
