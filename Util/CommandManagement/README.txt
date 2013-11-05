#--------------------------------------------------------------------------#
############################ COMMAND MANAGEMENT ############################
#--------------------------------------------------------------------------#

This project is a framework for command implementation in our UIs. The
important classes are CommandManager and Command. CommandManager is what the
UI should instantiate to have command management functionality. Any commands
that should go on the undo/redo stack and such should go through
CommandManager.

The commands themselves should inherit from the Command class in this
project. Some examples of these commands can be seen in the
Labeler\CommandList folder.

Note: the behavior is a little non-ideal for commands that are not undoable,
in that the CommandManager will actually undo the previous command (before
that one). This should probably be fixed at some point.
