// This can be used to load and save projects, which include an xml file, 
// a verilog file, and a xilnix file. This is still slightly buggy and only 
// the xml file is currently used, so this is currently excluded from the project.
//  ~ Alexa  Keizur, Summer 2010

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
using System.IO;

namespace WPFCircuitSimulatorUI
{
    public delegate void ProjectSavingHandler();
    public delegate void ProjectOpeningHandler();

    /// <summary>
    /// Interaction logic for ProjectManagerForm.xaml
    /// </summary>
    public partial class ProjectManagerForm : Window
    {
        #region Constants

        public static string XMLFileFilter = "MIT XML sketches (*.xml)|*.xml";
        public static string JNTFileFilter = "Microsoft Windows Journal Files (*.jnt)|*.jnt";
        public static string XilinxFileFilter = "Xilinx project files (*.ise)|*.ise";
        public static string VerilogFileFilter = "Verilog module files (*.v)|*.v";
        public static string AllFileFilter = "All files (*.*)|*.*";

        public static string NullFilePathStr = "< No file selected >";

        public enum ProjectManagerContext
        {
            /// <summary>
            /// User wants to simulate, but project files are not set.
            /// </summary>
            Simulate,

            /// <summary>
            /// User wants to create a new project.
            /// </summary>
            New,

            /// <summary>
            /// User wants to save unsaved work.
            /// </summary>
            Save,

            /// <summary>
            /// User wants to open a new project.
            /// </summary>
            Open,

            /// <summary>
            /// User wants to edit project properties.
            /// </summary>
            Edit,
        };

        public static string OnSimulateInstructions = "Your project is not yet saved.  Please write in a project name " +
            "or tap OK to use the default project name.";

        public static string OnSaveInstructions = "One or more files in your project are not yet saved.  Please write in " +
            "a new project name or use the current project name.";

        public static string OnOpenInstructions = "Please write in a project name to open or click browse to " +
            "select project file(s).  Selecting one project file will load all the files in that project if " +
            "other project files can be found.";

        public static string OnNewProjectInstructions = "Please write in a project name or tap OK to use the default.";

        public static string OnEditProjectInstructions = "Change the project name by writing in the field below (also changes " +
            "where files will be saved).";

        #endregion

        #region Internals

        /// <summary>
        /// 0 - circuit
        /// 1 - input
        /// 2 - notes
        /// 3 - xilinx
        /// 4 - verlilog
        /// </summary>
        private string[] origFilepaths;
        private string origProjectRoot;
        private string origProjectName;

        private string currentProjectRoot;
        private string currentProjectName;
        private ProjectManagerContext dialogMode;

        public ProjectSavingHandler ProjectSaving;
        public ProjectOpeningHandler ProjectOpening;

        #endregion

        #region Constructor and Launcher

        public ProjectManagerForm()
        {
            InitializeComponent();

            currentProjectRoot = FilenameConstants.DefaultProjectRoot;
            currentProjectName = FilenameConstants.DefaultProjectName;

            projectFile.Text = currentProjectRoot + currentProjectName;
            projectRoot.Text = currentProjectRoot;

            xilFile.Text = NullFilePathStr;
            verilogFile.Text = NullFilePathStr;

            projectName.Text = currentProjectName;
        }

        public bool? LaunchProjectManager(ProjectManagerContext context)
        {
            // Store original file paths
            origFilepaths = new string[2];
            origFilepaths[0] = xilFile.Text;
            origFilepaths[1] = verilogFile.Text;
            origProjectName = currentProjectName;
            origProjectRoot = currentProjectRoot;

            this.dialogMode = context;

            if (context == ProjectManagerContext.New)
            {
                currentProjectRoot = FilenameConstants.DefaultProjectRoot;
                currentProjectName = FilenameConstants.DefaultProjectName;
                SetFilePathsFromProjectName(currentProjectName);

                projectName.Background = Brushes.LightGray;
                instructionsBox.Text = OnNewProjectInstructions;
            }
            else if (context == ProjectManagerContext.Open)
            {
                projectName.Background = Brushes.White;
                instructionsBox.Text = OnOpenInstructions;
            }
            else if (context == ProjectManagerContext.Save)
            {
                projectName.Background = Brushes.LightGray;
                instructionsBox.Text = OnSaveInstructions;
            }
            else if (context == ProjectManagerContext.Edit)
            {
                projectName.Background = Brushes.White;
                instructionsBox.Text = OnEditProjectInstructions;
            }
            else if (context == ProjectManagerContext.Simulate)
            {
                projectName.Background = Brushes.LightGray;
                instructionsBox.Text = OnSimulateInstructions;
            }

            try { return this.ShowDialog(); }
            catch 
            {
                MessageBox.Show("The system has encountered an error and cannot operate."
                    + " Try using the Load Sketch or Save Sketch options instead.");
                return false;
            }
        }

        #endregion

        #region File path selection helpers

        private bool findFileDialog(string promptText, string fileFilter, string defaultExt)
        {
            // The kind of dialog we want depends on the context
            Microsoft.Win32.FileDialog browserDialog;
            if (this.dialogMode.Equals(ProjectManagerContext.New) || this.dialogMode.Equals(ProjectManagerContext.Save) || this.dialogMode.Equals(ProjectManagerContext.Simulate))
                browserDialog = new Microsoft.Win32.SaveFileDialog();
            else
                browserDialog = new Microsoft.Win32.OpenFileDialog();
            
            // Adjust the browser dialog properties
            browserDialog.Title = promptText;
            browserDialog.Filter = fileFilter;
            browserDialog.AddExtension = true;
            browserDialog.ValidateNames = true;
            browserDialog.CheckFileExists = true;
            browserDialog.RestoreDirectory = false;
            browserDialog.DefaultExt = defaultExt;
            browserDialog.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();

            if (browserDialog.ShowDialog() != true)
            {
                return false;
            }

            String chosenFile = browserDialog.FileName;

            if (defaultExt == FilenameConstants.DefaultXilinxProjectExtension)
            {
                xilFile.Text = chosenFile;
            }
            else if (defaultExt == FilenameConstants.DefaultVerilogModuleExtension)
            {
                verilogFile.Text = chosenFile;
            }
            else
            {
                currentProjectRoot = Path.GetDirectoryName(chosenFile);
                currentProjectName = Path.GetFileNameWithoutExtension(chosenFile);
                SetFilePathsFromProjectName(currentProjectName);
            }

            return true;
        }

        /// <summary>
        /// Buggy, not used
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extensionToStrip"></param>
        private void loadAllFilenames(string path, string extensionToStrip)
        {
            if (!path.Contains(extensionToStrip))
            {
                Console.WriteLine("ERR: File path does not contain extension: " + path + ", " + extensionToStrip);
                return;
            }

            string filename = Path.GetFileName(path);
            int index = filename.LastIndexOf(extensionToStrip);

            if (index <= 0)
                return;

            currentProjectName = filename.Remove(index);

            string projDir = Path.GetDirectoryName(path);
            int dIndex = projDir.LastIndexOf(currentProjectName);
            
            if (dIndex <= 0)
                return;

            currentProjectRoot = projDir.Remove(dIndex);

            projectName.Text = currentProjectName;

            ValidateFilesExist(true);

            // Don't need to warn the user if xilinx file cannot be loaded
            xilFile.Background = Brushes.White;
        }

        private void createMissingFiles()
        {
            if (!File.Exists(xilFile.Text))
            {
                String parentFolder = Path.GetDirectoryName(xilFile.Text);
                if (!Directory.Exists(parentFolder))
                    Directory.CreateDirectory(parentFolder);
                System.IO.File.Create(xilFile.Text);

                xilFile.Background = Brushes.White;
            }

            if (!File.Exists(verilogFile.Text))
            {
                String parentFolder = Path.GetDirectoryName(verilogFile.Text);
                if (!Directory.Exists(parentFolder))
                    Directory.CreateDirectory(parentFolder);
                File.Create(verilogFile.Text);

                verilogFile.Background = Brushes.White;
            }
        }

        /// <summary>
        /// Highlights invalid filepaths
        /// </summary>
        /// <returns>True iff all file paths have valid formatting.</returns>
        public bool ValidateFilepaths(bool highlightInvalidFiles)
        {
            bool valid = true;

            if (xilFile.Text.Equals(NullFilePathStr) ||
                !xilFile.Text.Contains(FilenameConstants.DefaultXilinxProjectExtension))
            {
                valid = false;
                if (highlightInvalidFiles)
                    xilFile.Background = Brushes.LightSalmon;
            }

            if (verilogFile.Text.Equals(NullFilePathStr) ||
                !verilogFile.Text.Contains(FilenameConstants.DefaultVerilogModuleExtension))
            {
                valid = false;
                if (highlightInvalidFiles)
                    verilogFile.Background = Brushes.LightSalmon;
            }

            if (currentProjectRoot.Contains(" "))
            {
                valid = false;

                if (highlightInvalidFiles)
                {
                    xilFile.Background = Brushes.LightSalmon;
                    verilogFile.Background = Brushes.LightSalmon;
                }
            }

            return valid;
        }

        /// <summary>
        /// Validates that the specified file paths in the FilepathBoxes exist.  
        /// Optionally colors erronenous FilepathBoxes LightSalmon.
        /// </summary>
        /// <param name="highlightInvalidFiles">True iff non-existant file paths should be
        /// highlighted LightSalmon</param>
        /// <returns>True iff all file paths specified exist.</returns>
        public bool ValidateFilesExist(bool highlightInvalidFiles)
        {
            bool valid = true;

            if (!System.IO.File.Exists(xilFile.Text))
            {
                valid = false;
                if (highlightInvalidFiles)
                    xilFile.Background = Brushes.LightSalmon;
            }

            if (!System.IO.File.Exists(verilogFile.Text))
            {
                valid = false;
                if (highlightInvalidFiles)
                    verilogFile.Background = Brushes.LightSalmon;
            }

            return valid;
        }

        public void SetFilePathsFromProjectName(string projName)
        {

            if (currentProjectRoot[currentProjectRoot.Length - 1] != '\\')
                currentProjectRoot += '\\';

            // Set project root and folder
            currentProjectName = projName;
            projectName.Text = currentProjectName;
            projectRoot.Text = currentProjectRoot;

            projectFile.Text = currentProjectRoot + currentProjectName;

            // Set default file paths
            xilFile.Text =
                projectFile.Text + FilenameConstants.DefaultXilinxProjectExtension;
            verilogFile.Text =
                projectFile.Text + FilenameConstants.DefaultVerilogModuleExtension;

            // Set default file path box colors
            xilFile.Background = Brushes.White;
            verilogFile.Background = Brushes.White;
        } 

        #endregion

        #region Properties

        /// <summary>
        /// The full path of the Xilinx Project file
        /// </summary>
        public string XilinxProjectPath
        {
            get
            {
                if (xilFile.Text.Equals(NullFilePathStr))
                {
                    return null;
                }
                else
                {
                    return xilFile.Text;
                }
            }
        }

        /// <summary>
        /// The full path of the Verilog Module file
        /// </summary>
        public string VerilogModulePath
        {
            get
            {
                if (verilogFile.Text.Equals(NullFilePathStr))
                {
                    return null;
                }
                else
                {
                    return verilogFile.Text;
                }
            }
        }

        /// <summary>
        /// The current project name
        /// </summary>
        public string CurrentProjectName
        {
            get
            {
                return currentProjectName;
            }
            set
            {
                SetFilePathsFromProjectName(value);
            }
        }

        /// <summary>
        /// The current project root directory (e.g. C:\).  
        /// The project directory is contained within this directory.
        /// </summary>
        public string CurrentProjectRoot
        {
            get
            {
                return currentProjectRoot;
            }
            set
            {
                currentProjectRoot = value;
                SetFilePathsFromProjectName(currentProjectName);
            }
        }

        /// <summary>
        /// The full path of the project directory (e.g. C:\New_proj\).
        /// </summary>
        public string CurrentProjectFileBase
        {
            get
            {
                return currentProjectRoot + currentProjectName;
            }
            set
            {
                currentProjectRoot = Path.GetDirectoryName(value);
                currentProjectName = Path.GetFileNameWithoutExtension(value);
                SetFilePathsFromProjectName(currentProjectName);
            }
        }

        #endregion

        #region Events

        private void projectName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentProjectName == projectName.Text) return;

            projectName.Background = Brushes.White;

            // Get project name and format it
            string projName = projectName.Text;
            projName = projName.Replace(" ", "");

            currentProjectName = projName;

            if (currentProjectRoot != null)
                SetFilePathsFromProjectName(currentProjectName);
        }

        private void projectRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentProjectRoot == projectRoot.Text) return;

            currentProjectRoot = projectRoot.Text;

            if (currentProjectRoot[currentProjectRoot.Length - 1] != '\\')
                currentProjectRoot += '\\';

            if (currentProjectName != null)
                SetFilePathsFromProjectName(currentProjectName);
        }

        private void setProjectRoot_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog browserDialog = new System.Windows.Forms.FolderBrowserDialog();
            browserDialog.ShowNewFolderButton = true;
            browserDialog.Description = "Please choose a root directory for the project (e.g., \"C:\\\" or \"H:\\\").  " +
                "A folder for the project will be created within this root directory.  Please ensure " +
                "that the full path of the project directory (e.g. \"C:\\My_project\\\" does not contain spaces.";
            browserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

            if (browserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentProjectRoot = browserDialog.SelectedPath + "\\";
                SetFilePathsFromProjectName(currentProjectName);
            }
        }

        private void xilinxBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string openFileText = "Please select a Xilinx Project File...";
            findFileDialog(openFileText, XilinxFileFilter, FilenameConstants.DefaultXilinxProjectExtension);

            xilFile.Background = Brushes.White;
        }

        private void verilogBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string openFileText = "Please select a Verilog Module File...";
            findFileDialog(openFileText, VerilogFileFilter, FilenameConstants.DefaultVerilogModuleExtension);

            verilogFile.Background = Brushes.White;
        }
        
        private void projectNameBrowseButton_Click(object sender, EventArgs e)
        {
            String promptText = "Please choose a sketch file";
            String filter = XMLFileFilter + '|' + JNTFileFilter + '|' + AllFileFilter;
            findFileDialog(promptText, filter, FilenameConstants.DefaultXMLExtention);

            projectName.Background = Brushes.White;
        }
        
        private void okButton_Click(object sender, EventArgs e)
        {
            // Validate files
            if (!ValidateFilepaths(true))
            {
                if (currentProjectRoot.Contains(" "))
                {
                    MessageBox.Show("Error: project file path cannot contain spaces.  " +
                        "Xilinx cannot open project files that reside in directories whose " +
                        "names contain spaces.  Please choose a project root path whos name " +
                        "does not contain spaces.");
                }
                else
                {
                    MessageBox.Show("Error: at least one file path is invalid");
                }

                return;
            }

            if (!ValidateFilesExist(true))
            {
                string message = "One or more project files do not yet exist.  Would you like to " +
                    "create them?";
                string title = "Confirm File Save";
                MessageBoxResult result = MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    createMissingFiles();
                }
            }

            if (ProjectSaving != null && !this.dialogMode.Equals(ProjectManagerContext.Open))
                ProjectSaving();
            else if (ProjectOpening != null)
                ProjectOpening();

            this.Hide();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            // Restore old file paths
            xilFile.Text = origFilepaths[0];
            verilogFile.Text = origFilepaths[1];
            currentProjectRoot = origProjectRoot;
            currentProjectName = origProjectName;

            this.Hide();
        }

        #endregion
    }
}
