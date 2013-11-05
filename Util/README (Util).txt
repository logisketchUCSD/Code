#--------------------------------------------------------------------------#
################################### UTIL ###################################
#--------------------------------------------------------------------------#

This directory holds various utility classes, some of which are libraries
used by Recognition and FrontEnd, while others are testing, training, and
analysis libraries.

CircuitSimLib: Library for a graph-based circuit that contains CircuitElements
	and connections. Also contains truth table values.

CommandManagement: The frame work for command input in the labeler and
	SketchPanel. A wrapper that can be used to provide undo and redo
	functionality to arbitrary commands

ConverterJnt: Microsoft Windows Journal file-type handling. Currently
	read-only.

ConverterXML: MIT XML file-type handling. Read/write

DisplayManager: Manages color display of strokes, circuit feedback mechanisms, and 
	displaying the label of a recognized shape.

DomainInfo: Creates the domain info, including colors and labels, of a domain
	by reading in a domain file.

EditMenu: Menu for all edit/correction actions.

Files: Contains utility subroutines for manipulating files and filetypes.
	See the README in the subdirectory for more information.

InkToSketchWPF: Used to convert tablet ink from Windows.Ink to Sketch.Sketch.
	Keeps the InkCanvas for display and the featurified sketch for recognition
	in sync.

MathNet.Iridium-2008.4.14.425: The Iridium math library for C#. Includes
	good matrix support, probability distributions, statistical functions,
	and many other useful math things. Note: this is an import of an
	external open-source project, but must be manually updated when new
	releases are made. To update, go to mathnet.opensourcedotnet.info and
	download the latest copy. You will have to update all references and
	solution files to point to the new copy. 

	If you need math functionality, it's in this project.

Metrics: A set of classes currently used for image distance

SimulationManager: Handles the interface of simulation, including the truth table,
	clean circuit, and circuit value toggles. 

Sketch: Holds strokes, substrokes, and point data about a sketch. Arguably
	the most important class in this project.

TestRig: The main test framework for this project. Includes modules for
	testing single-stroke labeling, grouping, shape recognition, and
	fragmentation. Designed to be extensible and fast. For more information,
	see the TestRig node on twiki.

Weka: contains WekaWrap and related files. Used by the StrokeClassifier and
	the StrokeGrouper to make decisions. 
