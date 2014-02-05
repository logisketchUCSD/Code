using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows;

namespace CommandManagement
{
	/// <summary>
	/// Summary description for CommandManager.
	/// </summary>
	public class CommandManager
	{
        /// <summary>
        /// The maximum number of commands that can be undone
        /// </summary>
        private const int MAX_UNDO = 10;

		/// <summary>
		/// List of Commands that can be Undone
		/// </summary>
        private List<Command> undoCommands;

		/// <summary>
		/// List of Commands that can be Redone
		/// </summary>
		private List<Command> redoCommands;

        /// <summary>
        /// Pair containing current cut/copied strokes
        /// </summary>
        private KeyValuePair<StrokeCollection, Rect>? clipboard;

        /// <summary>
        /// A callback for updating the editMenu of the Main window
        /// </summary>
        private Func<bool> updateCallback;

        /// <summary>
        /// A calling for updating the editMenu of the Main Window on moving, resizing, and pasting.
        /// </summary>
        private Func<bool> updateMoveCallback;

        /// <summary>
        /// A boolean indicating whether or not this should be keeping track of the number of times certain commands happen
        /// </summary>
        private bool countCommands;

        /// <summary>
        /// A dictionary of all the commands and how many times they have been seen
        /// </summary>
        private Dictionary<string, int> commandCounts;
		
		/// <summary>
		/// Full constructor.
		/// Utilizes Undo and Redo Lists to handle Commands. Called by Main window
        /// and given a callback to update the screen.
		/// </summary>
		public CommandManager(Func<bool> UpdateCallback = null, Func<bool> UpdateMove = null, bool countCommands = false)
		{
            undoCommands = new List<Command>();
			redoCommands = new List<Command>();
            clipboard = null;
            updateCallback = UpdateCallback;
            updateMoveCallback = UpdateMove;
            this.countCommands = countCommands;

            commandCounts = new Dictionary<string, int>();
		}

		/// <summary>
		/// Executes a Command and places it onto the Undo List, if appropriate.
		/// </summary>
		/// <param name="command">Command to execute</param>
		public void ExecuteCommand(Command command)
		{
            // Record the command we're seeing
            if (countCommands)
            {
                if (!commandCounts.ContainsKey(command.Type()))
                    commandCounts[command.Type()] = 0;
                commandCounts[command.Type()] += 1;
            }

            // Get and set the contents of the clipboard
            clipboard = command.SetClipboard(clipboard);

            // Execute this command
			command.Execute();
			
            // If this is undoable, add it to the stack
			if (command.IsUndoable)
			{
				undoCommands.Add(command);
				redoCommands.Clear();

                if (undoCommands.Count > MAX_UNDO)
                    undoCommands.RemoveAt(0);
			}

            // Call the appropriate callback
            if (command.Type().Equals("MoveResize") || command.Type().Equals("Paste"))
                updateMoveCallback();
            else
                updateCallback();
		}


		/// <summary>
		/// Undos the last (undoable) Command.
		/// </summary>
		/// <returns>True if a Command is unexecuted</returns>
		public bool Undo()
		{
			if (undoCommands.Count > 0)
			{
                int lastIndex = undoCommands.Count - 1;
				Command currCommand = undoCommands[lastIndex];

                if (countCommands)
                {
                    string key = "Undo " + currCommand.Type();
                    if (!commandCounts.ContainsKey(key))
                        commandCounts[key] = 0;
                    commandCounts[key] += 1;
                }
                
                undoCommands.RemoveAt(lastIndex);
                bool success = currCommand.UnExecute();

				redoCommands.Add(currCommand);

                if (redoCommands.Count > MAX_UNDO)
                    redoCommands.RemoveAt(0);

                if (currCommand.Type().Equals("MoveResize") || currCommand.Type().Equals("Paste"))
                    updateMoveCallback();
                else
                    updateCallback();

				return success;
			}
			else
			{
				return false;
			}
		}
		

		/// <summary>
		/// Redos the last undone Command.
		/// </summary>
		/// <returns>True if a Command is re-executed</returns>
		public bool Redo()
		{
			if (redoCommands.Count > 0)
			{
                int lastIndex = redoCommands.Count - 1;
				Command currCommand = redoCommands[lastIndex];
                //Console.WriteLine("Redo Command: " + currCommand.GetType().ToString());

                if (countCommands)
                {
                    string key = "Redo " + currCommand.Type();
                    if (!commandCounts.ContainsKey(key))
                        commandCounts[key] = 0;
                    commandCounts[key] += 1;
                }
                
                redoCommands.RemoveAt(lastIndex);
				bool success = currCommand.Execute();

				undoCommands.Add(currCommand);

                if (undoCommands.Count > MAX_UNDO)
                    undoCommands.RemoveAt(0);

                if (currCommand.Type().Equals("MoveResize") || currCommand.Type().Equals("Paste"))
                    updateMoveCallback();
                else
                    updateCallback();

				return success;
			}
			else
			{
				return false;
			}
		}

        /// <summary>
        /// Undos the last command and eliminates it from the stacks.
        /// </summary>
        public void RemoveLastCommand()
        {
            if (undoCommands.Count > 0)
            {
                undoCommands[undoCommands.Count - 1].UnExecute();
                undoCommands.RemoveAt(undoCommands.Count - 1);
            }
        }

        /// <summary>
        /// Clear all the recorded info in the command manager
        /// </summary>
        public void Clear()
        {
            ClearCounts();
            ClearLists();
        }
	

		/// <summary>
		/// Clears the command stacks and the clipboard.
		/// </summary>
		public void ClearLists()
		{
            clipboard = null;
			undoCommands.Clear();
			redoCommands.Clear();
		}

        /// <summary>
        /// Clears commandCount dictionary
        /// </summary>
        public void ClearCounts()
        {
            commandCounts.Clear();
        }

        ///<summary>
        /// Returns whether the undo stack is empty
        ///</summary>
        public bool UndoValid
        {
            get { return (undoCommands.Count > 0); }
        }

        ///<summary>
        /// Returns whether the undo stack is empty
        ///</summary>
        public bool RedoValid
        {
            get { return (redoCommands.Count > 0); }
        }

        /// <summary>
        /// Returns true if there is nothing on the clipboard
        /// </summary>
        public bool ClipboardEmpty
        {
            get { return clipboard == null; }
        }

        public Dictionary<string, int> CommandCounts
        {
            get { return commandCounts; }
        }

        public bool CountCommands
        {
            get { return countCommands; }
            set
            {
                ClearCounts();
                countCommands = value;
            }
        }
	}
}
