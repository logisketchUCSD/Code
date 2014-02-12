InkToSketchWPF:
This is a version of InkToSketch intended for Windows Presentation Foundation
-----------
This project contains a class library intended to provide a mapping between StrokeCollection and 
Sketchers Sketch functionality.  For example, the class library contains an InkSketch class that binds a 
StrokeCollection instance to a Sketch instance, translating stroke adds and deletes to Sketch stroke adds and deletes. 
InkSketch also builds dictionaries mapping Sketch.Substroke Ids to System.Windows.Ink.Stroke Ids; 
many GUI-oriented tasks rely on these mappings to produce visual transformations. The purpose of this project 
is to provide a library that supports StrokeCollection-Sketch interoperability (especially in the context of 
developing WPF InkCanvas GUIs).


Files:
----- 
InkSketch.cs -- Main library class for binding StrokeCollections to Sketch.  Binds a generic StrokeCollection to a 
sketch object.  See class comments for details on functionality.

InkCanvasSketch.cs -- Library class for binding an InkPicture to a Sketch.  Links additional functionality 
between StrokeCollection and Sketch using additional events that InkCanvas provides.  This library class is also the 
current (6/2010) main class that GUI projects like SketchPanel and CircuitSimulatorUI use.  

