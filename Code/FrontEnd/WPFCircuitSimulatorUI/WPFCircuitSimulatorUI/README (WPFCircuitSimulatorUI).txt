WPFCircuitSimulatorUI
---------------------

The WPFCircuitSimulatorUI is the front end to the Circuit Simulator application. This project sits atop over
a dozen other projects in order to provide a user interface for the truth table, sketch, and circuit 
recognition as well as simulation frameworks.  To use the GUI, simply start an instance. Current bugs
and use can be found in Papers. 


Files:
------

Main Gui Files:
   MainWindow.xaml.cs - Main program (entry point for executable)
   MainWindow.xaml - Designer file for MainWindow
   ProjectManagerForm.cs and ProjectManagerForm.Designer.cs - WPF Control for
   	managing project files (e.g. Sketches and Xilinx project files)


Constants/Domain Files:
   CircuitColorDomain.txt- Specifies colors for each element in our circuit domain for recognition display.
   digital_domain.txt - Specifies the different elements of our domain (gates, wires, labels etc.)
   FilenameConstants.cs - Stores file paths of config files and default path
	names for generated files
   
Recognizer Files: (Not currently used)
   CircuitRecRecognizer.cs -- A SketchRecognizer that uses CircuitRec to
   	recognize labeled circuits
   FlowRecognizer.cs -- A SketchRecognizer that uses Flow to recognize (label)
	sketches
   TruthTableRecognizer.cs -- A SketchRecognizer that uses TruthTables to
   	recognize Truth Tables.  Both labeles truth table sketches while also
	returns the Pins/inputs and outputs for a truth table.
   NaiveRecognizers.cs -- Collection of test/debug recognizers to support
	development

Debug Test Sketch Files (not needed to run)
CircuitRecTestSketch* -- Labeled sketches for use in testing CircuitRec and
CircuitRec-related GUI features
TestSketch.xml -- A labeled test sketch given as the default result by one
of the basic naive test recognizers
