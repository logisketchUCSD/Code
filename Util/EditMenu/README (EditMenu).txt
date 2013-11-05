Edit Menu
---------

The edit menu creates the context sensitive menu (a WPF custom control) for all edit/correction commands.
The menu allows the user to copy, cut, paste, delete, label, group, undo, and redo. The
SelectionManager handles bringing up and closing the EditMenu. When a button is hit, the 
appropriate command is executed using the CommandManager.

Files:
------

EditMenu.cs - Main framework class.
LabelMenu.cs - A WPF control containing all possible labels in the domain to allow the user to
	specify the desired recognition label of a group of strokes. 
CommandList - Contains the Commands for applying and removing labels.
Images - Images for the undo/redo buttons
