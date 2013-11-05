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

namespace NotesWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Internals

        private InkCanvas notesPanel;

        #endregion

        public MainWindow(ref InkCanvas notesPanel)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // Log error
                System.Console.WriteLine(ex.InnerException.Message);
                //System.Console.WriteLine(ex.ListTrace);
            }
            
            // Set up notes panel
            this.notesPanel = notesPanel;
            this.notesPanel.Height = notesDock.ActualHeight;
            this.notesPanel.Width = notesDock.ActualWidth;
            if (!notesDock.Children.Contains(this.notesPanel))
                notesDock.Children.Add(this.notesPanel);

            // Set Editing Modes
            notesPanel.EditingMode = InkCanvasEditingMode.Ink;
            notesPanel.EditingModeInverted = InkCanvasEditingMode.EraseByStroke;
        }

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
                //System.Console.WriteLine(ex.ListTrace);
            }

            // Set up notes panel
            this.notesPanel = new InkCanvas();
            this.notesPanel.Height = notesDock.ActualHeight;
            this.notesPanel.Width = notesDock.ActualWidth;
            if (!notesDock.Children.Contains(this.notesPanel))
                notesDock.Children.Add(this.notesPanel);

            // Set Editing Modes
            notesPanel.EditingMode = InkCanvasEditingMode.Ink;
            notesPanel.EditingModeInverted = InkCanvasEditingMode.EraseByStroke;
        }

        public void Clear_ButtonClick(object sender, RoutedEventArgs e)
        {
            notesPanel.Strokes.Clear();
        }

        public void Window_SizeChanged(object sender, RoutedEventArgs e)
        {
            notesPanel.Height = notesDock.ActualHeight;
            notesPanel.Width = notesDock.ActualWidth;
        }


        public void Window_Closed(object sender, EventArgs e)
        {
            notesDock.Children.Remove(notesPanel);
        }

    }
}
