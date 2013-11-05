using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sketch
{
    /// <summary>
    /// A wrapper for information in sketch, circuit, and saveto circ for saving and loading
    /// </summary>
    public class Project
    {
        #region Internals
        /// <summary>
        /// Copy of the sketch for saving and loading
        /// </summary>
        public Sketch sketch;

        /// <summary>
        /// This dictionary will give you a corresponding project (with sketch and circuit)
        /// based on the id of a subcircuit shape. prevents needing to keep around a copy for
        /// each shape and allows to undo and redo commands.
        /// </summary>
        public Dictionary<int, Project> subProjectLookup;

        /// <summary>
        /// A dictionary of the lookups in subProjectLookup of projects that are used in the sketch
        /// to the number of times they are used
        /// </summary>
        public Dictionary<int, int> subProjectsused;

        

        #region Exclusively Saving and Loading
        // The Following internals are not meant to be reliable during the creation of use
        // of a circuit. They are updated and set to be correct only when a project is 
        // loaded or saved, since this is the only time when they are needed. If you
        // plan on using these for other purposes, they are currently set in SaveSketch(string filename)
        // in InkCanvasSketch. You will have to update them else where.
        /// <summary>
        /// dictionary of input to output for this circuit
        /// </summary>
        public Dictionary<List<int>, List<int>> behavior;
        /// <summary>
        /// List of the inputs
        /// </summary>
        public List<string> inputs;
        /// <summary>
        /// list of the outputs
        /// </summary>
        public List<string> outputs;
        /// <summary>
        /// The xmlDoc that has all the information  to export to logisim
        /// </summary>
        public string saveToCircDoc;

        /// <summary>
        /// The following are to save the direction of parsing inputs and outputs in a circuit
        /// which is important for Logisim saving
        /// </summary>
        private int inputDirection;
        private int outputDirection;
        private const int EAST_WEST = 0;
        private const int NORTH_SOUTH = 1;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Create a project without circuit information
        /// </summary>
        /// <param name="sketch"></param>
        public Project(Sketch sketch)
        {
            this.sketch = sketch;

            ClearSubCircuits();
            behavior = new Dictionary<List<int>, List<int>>();
            inputs = new List<string>();
            outputs = new List<string>();
            saveToCircDoc = null;// new System.Xml.XmlTextWriter(new System.IO.StringWriter());
        }
        /// <summary>
        /// Constructor with nothing interesting happening
        /// </summary>
        public Project()
        {
            this.sketch = new Sketch();

            ClearSubCircuits();
            behavior = new Dictionary<List<int>, List<int>>();
            inputs = new List<string>();
            outputs = new List<string>();
            saveToCircDoc = null;// new System.Xml.XmlTextWriter(new System.IO.StringWriter());
        }
        /// <summary>
        /// Create the Project with initial values for all locals
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="behavior"></param>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <param name="saveToCircDoc"></param>
        public Project(Sketch sketch, Dictionary<List<int>, List<int>> behavior, List<string> inputs, List<string> outputs, string saveToCircDoc)
        {
            this.sketch = sketch;

            ClearSubCircuits();
            this.behavior = behavior;
            this.inputs = inputs;
            this.outputs = outputs;
            this.saveToCircDoc = saveToCircDoc;
        }
        #endregion

        #region Dictionary Helpers

        /// <summary>
        /// Tell this that a new subsketch is added, and return the index of the subcircuit lookup
        /// </summary>
        /// <param name="newSub"></param>
        /// <returns></returns>
        public int newSubSketch(Project newSub)
        {
            int newIndex = subProjectLookup.Count;
            // When there is a new embed item added to the list, this should know about it
            // The next lookup number is the current count
            subProjectLookup.Add(newIndex, newSub);
            return newIndex;
        }

        /// <summary>
        /// When a shape is actually added to the sketch with a subcircuit behavior, update 
        /// the count of times it's seen
        /// </summary>
        /// <param name="index"></param>
        public void subCiruitAdded(int index)
        {
            if (subProjectsused.ContainsKey(index))
            {
                subProjectsused[index]++;
            }
            else
            {
                subProjectsused[index] = 1;
            }
        }


        /// <summary>
        /// When a shape is actually removed to the sketch with a subcircuit behavior, update 
        /// the count of times it's seen
        /// </summary>
        /// <param name="index"></param>
        public void subCircuitRemoved(int index)
        {
            subProjectsused[index]--;
            if (subProjectsused[index] == 0)
                subProjectsused.Remove(index);
        }

        /// <summary>
        /// Method used when recognizing to avoid double counting the projects used
        /// </summary>
        public void resetUsedCircuits()
        {
            subProjectsused = new Dictionary<int, int>();
        }

        /// <summary>
        /// When the circuit is made, set all the dictionary information
        /// </summary>
        /// <param name="behavior"></param>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <param name="saveToCircDoc">The information for Logisim exporting</param>
        public void setCircuitInfo(Dictionary<List<int>, List<int>> behavior, List<Shape> inputs, List<Shape> outputs, string saveToCircDoc)//System.Xml.XmlTextWriter saveToCircDoc)
        {
            this.behavior = behavior;
            this.inputs = inputs.ConvertAll(x => { return x.Name; });
            this.outputs = outputs.ConvertAll(x => { return x.Name; });
            this.saveToCircDoc = saveToCircDoc;
        }

        /// <summary>
        /// This function is called when a circuit is constructed and saves the direction that the inputs and ouputs are
        /// parsed in. Each will be a 0 if it was parsed east to west and 1 if north to south, these constants
        /// are at the top of the file.
        /// </summary>
        /// <param name="inputHoriz"></param>
        /// <param name="outputHoriz"></param>
        public void setCircuitDirection(bool inputHoriz, bool outputHoriz)
        {
            if (inputHoriz)
                this.inputDirection = EAST_WEST;
            else
                this.inputDirection = NORTH_SOUTH;
            if (outputHoriz)
                this.outputDirection = EAST_WEST;
            else
                this.outputDirection = NORTH_SOUTH;
        }
        /// <summary>
        /// When the circuit is loaded, update the dicts
        /// </summary>
        /// <param name="behavior"></param>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <param name="saveToCircDoc">The information for Logisim exporting</param>
        public void setCircuitInfo(Dictionary<List<int>, List<int>> behavior, List<string> inputs, List<string> outputs, string saveToCircDoc)//System.Xml.XmlTextWriter saveToCircDoc)
        {
            this.behavior = behavior;
            this.inputs = inputs;
            this.outputs = outputs;
            this.saveToCircDoc = saveToCircDoc;
        }

        /// <summary>
        /// Clear the subcircuit list
        /// </summary>
        public void ClearSubCircuits()
        {
            subProjectsused = new Dictionary<int, int>();
            this.subProjectLookup = new Dictionary<int,Project>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get a string uniquely identifying this project
        /// </summary>
        public string UniqueIdentifier
        {
            get
            {
                return sketch.Name +"-" + sketch.XmlAttrs.Id.ToString();
            }
        }

        /// <summary>
        /// Get a list of all sub-projects for this project. The list does
        /// not contain duplicates.
        /// </summary>
        public List<Project> AllSubprojects
        {
            get
            {
                HashSet<Project> all = new HashSet<Project>(new ProjectIDComparer());
                foreach (Project sub in Subprojects)
                {
                    all.Add(sub);
                    all.Union(sub.AllSubprojects, new ProjectIDComparer());
                }

                return new List<Project>(all);
            }
        }

        /// <summary>
        /// Get a list of all sub-projects in this project. The list may contain
        /// duplicates.
        /// </summary>
        private List<Project> Subprojects
        {
            get
            {
                List<Project> usedProjects = new List<Project>();
                foreach (var tagAndProject in subProjectLookup)
                {
                    int usages;
                    subProjectsused.TryGetValue(tagAndProject.Key, out usages);
                    if (usages > 0) usedProjects.Add(tagAndProject.Value);
                }
                return usedProjects;
            }
        }

        #region Helpers for enumerating sub-projects
        private static void append(List<Project> first, List<Project> second)
        {
            foreach (Project value in first)
                append(value, second);
        }

        private static void append(Project value, List<Project> second)
        {
            if (!second.Contains(value, new ProjectIDComparer()))
                second.Add(value);
        }

        class ProjectIDComparer : IEqualityComparer<Project>
        {
            public bool Equals(Project first, Project second)
            {
                return (first.UniqueIdentifier == second.UniqueIdentifier);
            }

            public int GetHashCode(Project project)
            {
                return project.UniqueIdentifier.GetHashCode();
            }
        }
        #endregion

        #endregion

    }
}
