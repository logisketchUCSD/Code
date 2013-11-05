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

namespace ProgressWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            statusStripLabel.Text = "Starting Recognition...";
        }

        /// <summary>
        /// Set the current status in the progress window.
        /// </summary>
        /// <param name="s">A string with the current status</param>
        public void setText(string s) 
        {
            statusStripLabel.Text = s;
        }

        /// <summary>
        /// Returns the current value of the statusStripLabel
        /// </summary>
        /// <returns></returns>
        public string getText()
        {
            return statusStripLabel.Text;
        }

        /// <summary>
        /// Set the current value of the progress bar
        /// </summary>
        /// <param name="n">An int between 0 and 100</param>
        public void setProgress(int n)
        {
            progressBar.Value = n;
        }
    }
}
