using System;
using System.Collections.Generic;
using System.Text;


namespace WPFCircuitSimulatorUI
{
    /// <summary>
    /// Constants.
    /// </summary>
    public static class FilenameConstants
    {
        /// <summary>
        /// Default location of saved circuits, to be loaded into the embed panl
        /// </summary>
        public static string PreviouslySavedCircuits =
            AppDomain.CurrentDomain.BaseDirectory + @"..\..\SavedFilePaths.txt";

        /// <summary>
        /// The filepath for our Data folder.
        /// </summary>
        public static string DataRoot = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\..\..\Data\";

        /// <summary>
        /// Default extension for XML files.
        /// </summary>
        public static string DefaultXMLExtension = ".xml";

        /// <summary>
        /// Default extension for logisim files
        /// </summary>
        public static string DefaultLogisimExtension = ".circ";

        /// <summary>
        /// Our program's name, for window title display
        /// </summary>
        public static string ProgramName = "LogiSketch";
    }
}
