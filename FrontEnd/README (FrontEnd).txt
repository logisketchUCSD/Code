#--------------------------------------------------------------------------#
################################ Front Ends ################################
#--------------------------------------------------------------------------#

These are end-user products that let our work interact with the world at
large. At least, that's the hope. Most of these projects depend extensively
on projects in Recognition and Util.



NotesWindow: A window that contains a sketching surface (InkCanvas) and a clear button. For use in 
	WPFCircuitSimulatorUI for user notes while drawing/recognizing circuits.

SelectionManager: A manager that hooks into a SketchPanelWPF to allow editing/error-correction
	of the panel's sketch.  

SketchPanelWPF: A resuabel Windows Controls Component for building SketchRecognition GUIs.
	Contains an InkCanvas for drawing as well as an InkSketch, which coordinates a featurified
	version of the sketch (for recognition) with the sketch on the screen.

WPFCircuitSimulatorUI: An all-inclusive UI for sketching, recognizing, and simulating
	circuits.
