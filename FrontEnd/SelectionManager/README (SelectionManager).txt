Selection Manager
-----------------

The SelectionManager class is the editing and correction interface for SketchPanel.
The SelectionManager subscribes to the panel to access the InkCanvas being drawn on
and its events. When the user hovers the stylus pen over the InkCanvas, hover widgets
appear, allowing the user to tap into selection, edit, or label mode. After the end of
an event, the user is back in ink mode. 

Files:
------

CommandList: Contains the Command for redrawing (for use by the CommandManager)
Images: Contains the images for the hover widgets
DragBoxSelect.cs: A SelectorTemplate that uses a drag box to select strokes that
	have a certain percentage within the box. Tap selection is also enabled.
HoverCrossManager.cs: Inherits from SelectionManager but there are no widgets. Strokes
	are selected/deselected by crossing them in the hover space.
HoverCrossSelect.cs: A SelectorTemplate that allows crossing to select and unselect 
	strokes. 
RedrawingTool.cs: A tool to re-recognize strokes by retracing them to replace the 
	strokes below, automatically grouping and re-recognizing the new strokes.
SelectionManager.cs: Main framework class.
SelectorTemplate.cs: A base class for selection mechanisms. Uses the system's 
	default lasso select.


KnownBugs:
----------
