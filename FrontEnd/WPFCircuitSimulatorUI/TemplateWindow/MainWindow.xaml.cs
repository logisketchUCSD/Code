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

namespace TemplateWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Internals

        /// <summary>
        /// Keep track of the shape that the pen is over to check if the template needs to change
        /// shapes have the matched template as a bitmap
        /// </summary>
        private Sketch.Shape shape;

        #endregion

        #region constructor
        /// <summary>
        /// Constructor for the template window
        /// Initializes the xaml, has a help string, and sets up empty image
        /// </summary>
        public MainWindow()
        {

            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // Log error
                System.Console.WriteLine(ex.InnerException.Message);
            }

            // Set up template panel
            this.shape = new Sketch.Shape();
        }
        #endregion

        #region update template
        /// <summary>
        /// Change the template being drawn if it's a new shape
        /// </summary>
        /// <param name="shape"></param>
        public void updateTemplate(Sketch.Shape shape)
        {
            //Check if it's a new shape we're over
            if (shape != this.shape && shape != null)
            {
                this.shape = shape;
                //Take away the current image if there is one (imageShown in xaml)
                if (dockPanel.Children.Contains(imageShown))
                    dockPanel.Children.Remove(imageShown);
                if (Domain.LogicDomain.IsGate(shape.Type))
                {
                    if (shape.TemplateDrawing != null)
                    {
                        //Valid bit map!
                        BitmapSource source = loadBitmap(shape.TemplateDrawing);
                        //Actually display it
                        imageShown.Source = source;
                        dockPanel.Children.Add(imageShown);
                    }
                }
            }
        }
        
        /// <summary>
        /// Code from the internet for creating a BitMapSource from a bitmap. This is needed
        /// since wpf images can't use bitmaps as a source to display.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }
        #endregion

        #region event handling

        /// <summary>
        /// Handler for resizing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Window_SizeChanged(object sender, RoutedEventArgs e)
        {
            dockPanel.Height = dockPanel.ActualHeight;
            dockPanel.Width = dockPanel.ActualWidth;
        }

        /// <summary>
        /// Get rid of anything being shown when closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Window_Closed(object sender, EventArgs e)
        {
            dockPanel.Children.Clear();
        }

        #endregion
    }
}
