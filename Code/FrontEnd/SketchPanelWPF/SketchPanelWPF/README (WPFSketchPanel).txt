SketchPanelWPF:
---------------

The SketchPanel project provides a basis for Ink-enabled GUIs using Sketchers Sketch framework. 
The SketchPanel is a WPF custom control that binds an InkCanvas (a WPF control that allows collects
and displays ink) and a Sketch (the featurified version of the InkCanvas strokes with shapes, neighbors
etc. for recognition purposes). Recognizers, managers, and feedback mechanisms subscribe to this panel
to collect updates on the sketch. 

Files:
------

ColorPicker: Contains Microsoft code for a control that allows the user to specify ink color.
CommandList: Contains the Commands of each editing action allowed on a SketchPanel. For use 
	with the CommandManager.
RecognizerInterface.cs: Base class for building Recognizers that process the Sketch embedded in a 
	SketchPanel.  Supports running a recognizer in a separate thread.
SketchPanel.cs: Main framework class.
SketchPanelSubscriber.cs: Base class for building modules that listen to SketchPanel events (e.g. Feedback
	Mechanisms or editing tools).

